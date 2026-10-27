using System;
using System.Collections.Generic;
using UnityEngine;

namespace SugarDrop
{
    /// <summary>Ce stă într-o celulă din nivelurile de salvare.</summary>
    public enum RescueCell { Empty, Piece, Stone, Cage, Slime }

    /// <summary>Ce s-a întâmplat după o atingere (pentru animații și scor).</summary>
    public sealed class RescueStep
    {
        public readonly List<Vector2Int> Removed = new();
        /// <summary>Culoarea fiecărei bomboane din Removed (aceeași ordine).</summary>
        public readonly List<int> RemovedColors = new();
        public int Color;
        public readonly List<Vector2Int> CageHits = new();
        public readonly List<Vector2Int> CagesOpened = new();
        public readonly List<Vector2Int> SlimeCleared = new();
        public readonly List<Vector2Int> SlimeGrown = new();
        /// <summary>Celulele noi umplute cu sirop și distanța lor față de siropul de dinainte (pentru curgere).</summary>
        public readonly List<(Vector2Int cell, int distance)> Syrup = new();
        public bool Rescued;
    }

    /// <summary>
    /// Motorul nivelurilor „Salvează-l pe Bobo”: piesele nu cad și nu apar altele noi. Atingi un grup de bomboane
    /// de aceeași culoare (chiar și una singură) și dispare; golurile rămân. Siropul din rezervorul de sus curge
    /// prin toate golurile legate de gura lui; când ajunge în celula de jos de deasupra pâlniei, Bobo e salvat.
    /// Obstacole: pietre (nu se sparg), cuști (2 lovituri — de la grupurile vecine sau atingând cușca — apoi bomboana
    /// din ele se eliberează) și slime acru (blochează siropul; dispare când spargi ceva lângă el sau când îl atingi,
    /// și crește la câteva mutări). Atingerea directă a obstacolelor e mai scumpă, dar nu lasă niciodată drumul blocat.
    /// Pur și determinist (aceeași sămânță → aceleași creșteri de slime), deci testabil și folosit de botul de echilibru.
    /// </summary>
    public sealed class RescueEngine
    {
        /// <summary>La câte mutări crește slime-ul acru.</summary>
        public const int SlimeEvery = 3;
        public const int CageHits = 2;

        public readonly int Width;
        public readonly int Height;
        public readonly int InletX;
        public readonly int OutletX;

        private readonly RescueCell[,] _kind;
        private readonly int[,] _color;
        private readonly int[,] _hits;
        private readonly bool[,] _syrup;
        private readonly System.Random _random;
        private int _taps;

        public bool Rescued { get; private set; }
        public int Taps => _taps;

        public RescueEngine(RescueLayout layout)
        {
            Width = layout.Width;
            Height = layout.Height;
            InletX = layout.InletX;
            OutletX = layout.OutletX;
            _kind = (RescueCell[,])layout.Kind.Clone();
            _color = (int[,])layout.Color.Clone();
            _hits = new int[Width, Height];
            _syrup = new bool[Width, Height];
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    if (_kind[x, y] == RescueCell.Cage) _hits[x, y] = CageHits;
            _random = new System.Random(layout.Seed);
            Flow(null);
        }

        private RescueEngine(RescueEngine other)
        {
            Width = other.Width;
            Height = other.Height;
            InletX = other.InletX;
            OutletX = other.OutletX;
            _kind = (RescueCell[,])other._kind.Clone();
            _color = (int[,])other._color.Clone();
            _hits = (int[,])other._hits.Clone();
            _syrup = (bool[,])other._syrup.Clone();
            _random = new System.Random(other._taps * 7919 + 17);
            _taps = other._taps;
            Rescued = other.Rescued;
        }

        public RescueEngine Clone() => new(this);

        public RescueCell Kind(int x, int y) => _kind[x, y];
        public int Color(int x, int y) => _color[x, y];
        public int Hits(int x, int y) => _hits[x, y];
        public bool Syrup(int x, int y) => _syrup[x, y];
        public Vector2Int Inlet => new(InletX, Height - 1);
        public Vector2Int Outlet => new(OutletX, 0);

        private bool Inside(Vector2Int c) => c.x >= 0 && c.y >= 0 && c.x < Width && c.y < Height;

        private static readonly Vector2Int[] Neighbours = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        /// <summary>Grupul de bomboane legate (de aceeași culoare) care conține celula; gol dacă nu e o bomboană.</summary>
        public List<Vector2Int> GroupAt(Vector2Int start)
        {
            var group = new List<Vector2Int>();
            if (!Inside(start) || _kind[start.x, start.y] != RescueCell.Piece) return group;
            int color = _color[start.x, start.y];
            var seen = new bool[Width, Height];
            var stack = new Stack<Vector2Int>();
            stack.Push(start);
            seen[start.x, start.y] = true;
            while (stack.Count > 0)
            {
                Vector2Int c = stack.Pop();
                group.Add(c);
                foreach (Vector2Int d in Neighbours)
                {
                    Vector2Int n = c + d;
                    if (!Inside(n) || seen[n.x, n.y] || _kind[n.x, n.y] != RescueCell.Piece || _color[n.x, n.y] != color) continue;
                    seen[n.x, n.y] = true;
                    stack.Push(n);
                }
            }
            return group;
        }

        /// <summary>Toate grupurile care se pot atinge (câte unul pe grup, cel cu prima celulă găsită).</summary>
        public List<List<Vector2Int>> Groups()
        {
            var groups = new List<List<Vector2Int>>();
            var seen = new bool[Width, Height];
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                {
                    if (seen[x, y] || _kind[x, y] != RescueCell.Piece) continue;
                    List<Vector2Int> group = GroupAt(new Vector2Int(x, y));
                    foreach (Vector2Int c in group) seen[c.x, c.y] = true;
                    groups.Add(group);
                }
            return groups;
        }

        /// <summary>Atinge o celulă: dacă e o bomboană, grupul ei dispare. Null dacă atingerea nu face nimic.</summary>
        public RescueStep Tap(Vector2Int cell)
        {
            if (Rescued || !Inside(cell)) return null;
            RescueCell kind = _kind[cell.x, cell.y];
            if (kind is RescueCell.Cage or RescueCell.Slime)
            {
                // Atingere directă pe obstacol: o lovitură la cușcă, slime-ul se curăță.
                var direct = new RescueStep { Color = -1 };
                Hit(cell, direct);
                return EndTap(direct);
            }
            List<Vector2Int> group = GroupAt(cell);
            if (group.Count == 0) return null;
            var step = new RescueStep { Color = _color[cell.x, cell.y] };
            foreach (Vector2Int c in group)
            {
                _kind[c.x, c.y] = RescueCell.Empty;
                step.Removed.Add(c);
                step.RemovedColors.Add(_color[c.x, c.y]);
            }
            // Vecinii grupului: cuștile primesc o lovitură, slime-ul dispare.
            var touched = new HashSet<Vector2Int>();
            foreach (Vector2Int c in group)
                foreach (Vector2Int d in Neighbours)
                {
                    Vector2Int n = c + d;
                    if (Inside(n) && touched.Add(n)) Hit(n, step);
                }
            return EndTap(step);
        }

        private RescueStep EndTap(RescueStep step)
        {
            _taps++;
            if (_taps % SlimeEvery == 0) GrowSlime(step);
            Flow(step);
            return step;
        }

        private void Hit(Vector2Int c, RescueStep step)
        {
            switch (_kind[c.x, c.y])
            {
                case RescueCell.Cage:
                    _hits[c.x, c.y]--;
                    if (_hits[c.x, c.y] > 0) step.CageHits.Add(c);
                    else
                    {
                        _kind[c.x, c.y] = RescueCell.Piece;
                        step.CagesOpened.Add(c);
                    }
                    break;
                case RescueCell.Slime:
                    _kind[c.x, c.y] = RescueCell.Empty;
                    step.SlimeCleared.Add(c);
                    break;
            }
        }

        /// <summary>O pată de slime se întinde într-o celulă vecină (bomboană sau gol fără sirop).</summary>
        private void GrowSlime(RescueStep step)
        {
            var options = new List<Vector2Int>();
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                {
                    if (_kind[x, y] != RescueCell.Slime) continue;
                    foreach (Vector2Int d in Neighbours)
                    {
                        Vector2Int n = new Vector2Int(x, y) + d;
                        if (!Inside(n) || _syrup[n.x, n.y] || n == Inlet || n == Outlet) continue;
                        RescueCell kind = _kind[n.x, n.y];
                        if ((kind == RescueCell.Piece || kind == RescueCell.Empty) && !options.Contains(n)) options.Add(n);
                    }
                }
            if (options.Count == 0) return;
            Vector2Int grown = options[_random.Next(options.Count)];
            _kind[grown.x, grown.y] = RescueCell.Slime;
            step?.SlimeGrown.Add(grown);
        }

        /// <summary>Siropul umple tot ce e gol și legat de gura rezervorului (căutare în lățime, pentru distanțe).</summary>
        private void Flow(RescueStep step)
        {
            Vector2Int inlet = Inlet;
            if (_kind[inlet.x, inlet.y] != RescueCell.Empty) return;
            var queue = new Queue<(Vector2Int cell, int distance)>();
            if (!_syrup[inlet.x, inlet.y])
            {
                _syrup[inlet.x, inlet.y] = true;
                step?.Syrup.Add((inlet, 0));
            }
            // Pornim din tot siropul existent (distanța 0), ca animația să curgă din frontul lui.
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    if (_syrup[x, y]) queue.Enqueue((new Vector2Int(x, y), 0));
            while (queue.Count > 0)
            {
                (Vector2Int c, int distance) = queue.Dequeue();
                foreach (Vector2Int d in Neighbours)
                {
                    Vector2Int n = c + d;
                    if (!Inside(n) || _syrup[n.x, n.y] || _kind[n.x, n.y] != RescueCell.Empty) continue;
                    _syrup[n.x, n.y] = true;
                    step?.Syrup.Add((n, distance + 1));
                    queue.Enqueue((n, distance + 1));
                }
            }
            if (_syrup[OutletX, 0] && !Rescued)
            {
                Rescued = true;
                if (step != null) step.Rescued = true;
            }
        }

        // ------------------------------------------------------------ Pentru bot și indicii

        /// <summary>
        /// Cât mai e de săpat: cel mai ieftin drum de la sirop (sau gura rezervorului) până la Bobo, unde un gol costă 0,
        /// o bomboană sau slime-ul 1, o cușcă 2 (câte lovituri mai are), iar pietrele nu se pot traversa.
        /// </summary>
        public int PathCost() => Dijkstra(out _);

        /// <summary>Celulele de pe cel mai ieftin drum (de la sirop spre Bobo), fără cele deja goale.</summary>
        public List<Vector2Int> CheapestPath()
        {
            var path = new List<Vector2Int>();
            if (Dijkstra(out Vector2Int[,] previous) >= int.MaxValue / 4) return path;
            var none = new Vector2Int(-1, -1);
            for (Vector2Int c = Outlet; c != none; c = previous[c.x, c.y])
                if (_kind[c.x, c.y] != RescueCell.Empty) path.Insert(0, c);
            return path;
        }

        private int Dijkstra(out Vector2Int[,] previous)
        {
            const int Infinite = int.MaxValue / 4;
            var none = new Vector2Int(-1, -1);
            var cost = new int[Width, Height];
            previous = new Vector2Int[Width, Height];
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                {
                    cost[x, y] = Infinite;
                    previous[x, y] = none;
                }
            var open = new List<Vector2Int>();
            Vector2Int inlet = Inlet;
            int start = CellCost(inlet);
            if (start >= Infinite) return Infinite;
            cost[inlet.x, inlet.y] = start;
            open.Add(inlet);
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    if (_syrup[x, y])
                    {
                        cost[x, y] = 0;
                        open.Add(new Vector2Int(x, y));
                    }
            // Dijkstra simplu (tabla e mică: 81 de celule).
            var done = new bool[Width, Height];
            while (open.Count > 0)
            {
                int best = 0;
                for (int i = 1; i < open.Count; i++)
                    if (cost[open[i].x, open[i].y] < cost[open[best].x, open[best].y]) best = i;
                Vector2Int c = open[best];
                open.RemoveAt(best);
                if (done[c.x, c.y]) continue;
                done[c.x, c.y] = true;
                if (c == Outlet) return cost[c.x, c.y];
                foreach (Vector2Int d in Neighbours)
                {
                    Vector2Int n = c + d;
                    if (!Inside(n) || done[n.x, n.y]) continue;
                    int step = CellCost(n);
                    if (step >= Infinite) continue;
                    int total = cost[c.x, c.y] + step;
                    if (total >= cost[n.x, n.y]) continue;
                    cost[n.x, n.y] = total;
                    previous[n.x, n.y] = c;
                    open.Add(n);
                }
            }
            return Infinite;
        }

        /// <summary>
        /// Puterea lui Bobo: curăță primele celule care mai blochează cel mai bun drum (bomboane, slime, cuști),
        /// fără să coste o mutare și fără să crească slime-ul.
        /// </summary>
        public RescueStep PowerClear(int count)
        {
            if (Rescued) return null;
            List<Vector2Int> path = CheapestPath();
            if (path.Count == 0) return null;
            var step = new RescueStep { Color = -1 };
            for (int i = 0; i < path.Count && i < count; i++)
            {
                Vector2Int c = path[i];
                switch (_kind[c.x, c.y])
                {
                    case RescueCell.Piece:
                        step.Removed.Add(c);
                        step.RemovedColors.Add(_color[c.x, c.y]);
                        break;
                    case RescueCell.Cage:
                        step.CagesOpened.Add(c);
                        break;
                    case RescueCell.Slime:
                        step.SlimeCleared.Add(c);
                        break;
                }
                _kind[c.x, c.y] = RescueCell.Empty;
            }
            Flow(step);
            return step;
        }

        private int CellCost(Vector2Int c) => _kind[c.x, c.y] switch
        {
            RescueCell.Empty => 0,
            RescueCell.Piece => 1,
            RescueCell.Slime => 1,
            RescueCell.Cage => _hits[c.x, c.y] + 1,
            _ => int.MaxValue / 4,
        };

        /// <summary>
        /// Cea mai bună atingere după o regulă simplă: grupul care scurtează cel mai mult drumul siropului
        /// (la egalitate, cel mai mare). Folosită de bot (echilibru) și de indiciu.
        /// </summary>
        public Vector2Int BestTap()
        {
            Vector2Int best = new(-1, -1);
            int bestCost = int.MaxValue, bestSize = -1;
            var candidates = Groups();
            // Și obstacolele care se pot atinge direct (mărime 0: la egalitate, grupurile au prioritate).
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    if (_kind[x, y] is RescueCell.Cage or RescueCell.Slime) candidates.Add(new List<Vector2Int> { new(x, y) });
            foreach (List<Vector2Int> group in candidates)
            {
                RescueEngine copy = Clone();
                RescueStep step = copy.Tap(group[0]);
                if (step == null) continue;
                int cost = copy.Rescued ? -1 : copy.PathCost();
                int size = _kind[group[0].x, group[0].y] == RescueCell.Piece ? group.Count : 0;
                if (cost < bestCost || (cost == bestCost && size > bestSize))
                {
                    bestCost = cost;
                    bestSize = size;
                    best = group[0];
                }
            }
            return best;
        }

        /// <summary>Câte atingeri îi trebuie botului ca să-l salveze pe Bobo (int.MaxValue dacă nu reușește).</summary>
        public static int BotMoves(RescueLayout layout, int limit = 120)
        {
            var engine = new RescueEngine(layout);
            for (int i = 0; i < limit && !engine.Rescued; i++)
            {
                Vector2Int tap = engine.BestTap();
                if (tap.x < 0) return int.MaxValue;
                engine.Tap(tap);
            }
            return engine.Rescued ? engine.Taps : int.MaxValue;
        }
    }

    /// <summary>Tabla de start a unui nivel de salvare (generată determinist din numărul nivelului).</summary>
    public sealed class RescueLayout
    {
        public int Width;
        public int Height;
        public int InletX;
        public int OutletX;
        public int Seed;
        public RescueCell[,] Kind;
        public int[,] Color;
        public int BotMoves;

        private static readonly Dictionary<int, RescueLayout> Cache = new();

        /// <summary>
        /// Nivelul de salvare: 4 culori la început, 5 de la nivelul 150; ziduri de piatră cu goluri (labirintul prin
        /// care trebuie găsit drumul), pietre răzlețe, cuști (unele chiar în golurile zidurilor) și slime, după
        /// dificultate și cât de departe e nivelul. Se verifică cu botul că se poate termina (altfel altă sămânță).
        /// </summary>
        public static RescueLayout For(int level, Difficulty difficulty)
        {
            lock (Cache)
                if (Cache.TryGetValue(level, out RescueLayout cached)) return cached;
            RescueLayout layout = null;
            for (int attempt = 0; attempt < 20; attempt++)
            {
                layout = Generate(level, difficulty, level * 7349 + attempt * 104729 + 11);
                if (layout.BotMoves < int.MaxValue) break;
            }
            lock (Cache) Cache[level] = layout;
            return layout;
        }

        private static RescueLayout Generate(int level, Difficulty difficulty, int seed)
        {
            var random = new System.Random(seed);
            int width = LevelConfig.BoardColumns, height = LevelConfig.BoardRows;
            var layout = new RescueLayout
            {
                Width = width,
                Height = height,
                OutletX = width / 2,
                Seed = seed,
                Kind = new RescueCell[width, height],
                Color = new int[width, height],
            };
            // Gura rezervorului: într-o parte a tablei, ca drumul să aibă de cotit.
            layout.InletX = random.Next(2) == 0 ? random.Next(0, 3) : random.Next(width - 3, width);
            // Primele salvări (din primele ținuturi) au 3 culori, deci grupuri mari și drumuri ușor de găsit.
            int colors = level < 30 ? 3 : level >= 150 ? 5 : 4;
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    layout.Kind[x, y] = RescueCell.Piece;
                    layout.Color[x, y] = random.Next(colors);
                    // Pete de aceeași culoare: o celulă copiază uneori culoarea vecinei (grupuri mai mari, mai „gustoase”).
                    if (x > 0 && random.NextDouble() < 0.17) layout.Color[x, y] = layout.Color[x - 1, y];
                    else if (y > 0 && random.NextDouble() < 0.17) layout.Color[x, y] = layout.Color[x, y - 1];
                }

            int stage = Mathf.Max(0, (level - 100) / 10);
            float hard = difficulty switch
            {
                Difficulty.Easy => 0.6f,
                Difficulty.Hard => 1.3f,
                Difficulty.VeryHard => 1.6f,
                _ => 1f,
            };
            // Înainte de nivelul 101 obstacolele cresc încet, pe ținuturi (labirinturi simple, fără slime).
            bool early = level < LevelConfig.RescueFrom;
            if (early) stage = 0;
            int walls = early ? (level < 40 ? 1 : 2) : Mathf.Clamp(Mathf.RoundToInt((1 + stage / 3f) * hard), 1, 3);
            var gaps = new HashSet<Vector2Int>();
            Walls(layout, random, gaps, walls, difficulty == Difficulty.Easy ? 2 : 1, difficulty is Difficulty.Hard or Difficulty.VeryHard);
            int stones = early ? Mathf.Min(level / 12, 5) : Mathf.RoundToInt((3 + Mathf.Min(stage, 5) * 0.6f) * hard);
            int cages = early ? (level < 15 ? 0 : Mathf.Min(level / 20, 3)) : Mathf.RoundToInt((2 + Mathf.Min(stage, 5) * 0.6f) * hard);
            int slime = level >= 120 ? Mathf.RoundToInt((1 + Mathf.Min(stage / 2, 3)) * hard) : 0;
            Place(layout, random, gaps, RescueCell.Stone, stones);
            Place(layout, random, gaps, RescueCell.Cage, cages);
            Place(layout, random, gaps, RescueCell.Slime, slime);
            layout.BotMoves = RescueEngine.BotMoves(layout);
            return layout;
        }

        /// <summary>
        /// Ziduri orizontale de piatră pe toată lățimea, cu 1–2 goluri; golul unui zid nu stă sub cel de deasupra,
        /// așa că siropul trebuie dus în zigzag. Pe nivelurile grele, un gol e păzit de o cușcă.
        /// </summary>
        private static void Walls(RescueLayout layout, System.Random random, HashSet<Vector2Int> reserved, int count, int gaps, bool cagedGap)
        {
            // Rândurile zidurilor, cu cel puțin un rând liber între ele (pe acolo cotește siropul).
            int top = layout.Height - 3;
            int[] rows = count switch
            {
                1 => new[] { layout.Height / 2 },
                2 => new[] { top - 1, 2 },
                _ => new[] { top, top - 2, top - 4 },
            };
            int previousGap = layout.InletX;
            for (int i = 0; i < rows.Length; i++)
            {
                int y = Mathf.Max(2, rows[i]);
                int gap = random.Next(layout.Width);
                // Golul pe partea cealaltă față de cel de deasupra (drum în zigzag).
                if (Mathf.Abs(gap - previousGap) < 3) gap = previousGap < layout.Width / 2 ? random.Next(layout.Width - 3, layout.Width) : random.Next(0, 3);
                for (int x = 0; x < layout.Width; x++) layout.Kind[x, y] = RescueCell.Stone;
                layout.Kind[gap, y] = cagedGap && i == rows.Length - 1 ? RescueCell.Cage : RescueCell.Piece;
                // Golul și celulele de deasupra/dedesubt rămân libere de obstacole (altfel golul s-ar putea sigila).
                for (int dy = -1; dy <= 1; dy++) reserved.Add(new Vector2Int(gap, y + dy));
                if (gaps > 1)
                {
                    int second = Mathf.Clamp(gap + (gap < layout.Width / 2 ? 4 : -4), 0, layout.Width - 1);
                    layout.Kind[second, y] = RescueCell.Piece;
                    for (int dy = -1; dy <= 1; dy++) reserved.Add(new Vector2Int(second, y + dy));
                }
                previousGap = gap;
            }
        }

        /// <summary>Pune obstacole în celule cu bomboane, nu pe gura rezervorului, deasupra pâlniei sau în golurile zidurilor.</summary>
        private static void Place(RescueLayout layout, System.Random random, HashSet<Vector2Int> reserved, RescueCell kind, int count)
        {
            for (int i = 0, guard = 0; i < count && guard < 400; guard++)
            {
                int x = random.Next(layout.Width), y = random.Next(layout.Height);
                if (layout.Kind[x, y] != RescueCell.Piece || reserved.Contains(new Vector2Int(x, y))) continue;
                if ((x == layout.InletX && y >= layout.Height - 2) || (x == layout.OutletX && y <= 1)) continue;
                layout.Kind[x, y] = kind;
                // O piatră nu are voie să taie ultimul drum dintre rezervor și Bobo.
                if (kind == RescueCell.Stone && !Connected(layout))
                {
                    layout.Kind[x, y] = RescueCell.Piece;
                    continue;
                }
                i++;
            }
        }

        /// <summary>Există un drum (ocolind pietrele) de la gura rezervorului până deasupra pâlniei?</summary>
        private static bool Connected(RescueLayout layout)
        {
            var seen = new bool[layout.Width, layout.Height];
            var queue = new Queue<Vector2Int>();
            var start = new Vector2Int(layout.InletX, layout.Height - 1);
            queue.Enqueue(start);
            seen[start.x, start.y] = true;
            while (queue.Count > 0)
            {
                Vector2Int c = queue.Dequeue();
                if (c.x == layout.OutletX && c.y == 0) return true;
                foreach (Vector2Int d in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
                {
                    Vector2Int n = c + d;
                    if (n.x < 0 || n.y < 0 || n.x >= layout.Width || n.y >= layout.Height || seen[n.x, n.y]) continue;
                    if (layout.Kind[n.x, n.y] == RescueCell.Stone) continue;
                    seen[n.x, n.y] = true;
                    queue.Enqueue(n);
                }
            }
            return false;
        }
    }
}
