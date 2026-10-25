using System;
using System.Collections.Generic;
using UnityEngine;

namespace SugarDrop
{
    /// <summary>
    /// Regulile tablei „atinge și sparge” (pur, fără nimic vizual, testabil în afara Unity):
    /// atingi un grup de cel puțin 3 bomboane la fel, lipite pe orizontală/verticală (căutare în lățime), și tot grupul
    /// dispare. Grupurile mari lasă în locul atins o piesă specială (rachetă 5–6, bombă 7–9, curcubeu 10+).
    /// Piesele speciale se activează la atingere sau se combină cu una vecină. Apoi piesele cad, intră piese noi
    /// de sus, bomboanele-ingredient ies jos spre borcan. Obstacole: gheață (1–3 straturi), lanțuri, lăzi, ciocolată
    /// care crește, glazură pe celule. Fiecare acțiune întoarce pașii pe care tabla din scenă îi animă.
    /// </summary>
    public sealed class BlastEngine
    {
        private sealed class Piece
        {
            public int Id;
            public TileKind Kind;
            public int Color;
            public int Ice;
            public bool Chained;
            public int Hits;

            public bool IsBlocker => Kind == TileKind.Crate || Kind == TileKind.Chocolate;
            public bool IsIngredient => Kind == TileKind.Ingredient;
            public bool IsSpecial => BlastRules.IsSpecial(Kind);
            public bool Locked => Ice > 0 || Chained;
            public bool CanGroup => Kind == TileKind.Normal && !Locked && Color >= 0;
            public bool CanTap => !Locked && (Kind == TileKind.Normal || IsSpecial);
            public bool Falls => !IsBlocker && !Locked;
        }

        private const int MaxIngredientsOnBoard = 3;
        private static readonly Vector2Int[] FourWay = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };

        public readonly LevelConfig Config;
        public int Columns { get; }
        public int Rows { get; }
        /// <summary>Nu mai există nicio mutare posibilă nici după amestecare.</summary>
        public bool Stuck { get; private set; }
        public int FrostingLeft { get; private set; }
        public int IngredientsOnBoard { get; private set; }
        public int IngredientsToSpawn { get; private set; }
        public int IngredientsExited { get; private set; }

        private readonly Piece[,] _grid;
        private readonly int[,] _frosting;
        private readonly System.Random _random;
        private int _nextId = 1;
        private bool _chocolateHit;

        public BlastEngine(LevelConfig config, int pieceSeed)
        {
            Config = config;
            Columns = config.Columns;
            Rows = config.Rows;
            _grid = new Piece[Columns, Rows];
            _frosting = new int[Columns, Rows];
            _random = new System.Random(pieceSeed);
            var layout = new System.Random(config.Seed);

            foreach (Vector2Int c in ValidCells())
                _grid[c.x, c.y] = NewPiece(TileKind.Normal, _random.Next(Config.Colors));

            // Glazura stă grupată într-o zonă (ca un desen pe tablă), nu risipită: celulele cele mai apropiate de un centru.
            var frosted = new HashSet<Vector2Int>();
            if (config.FrostingCells > 0)
            {
                var center = new Vector2(2f + layout.Next(Columns - 4), 2f + layout.Next(Rows - 4));
                List<Vector2Int> byDistance = ValidCells();
                byDistance.Sort((a, b) => Distance(a, center).CompareTo(Distance(b, center)));
                for (int i = 0; i < config.FrostingCells && i < byDistance.Count; i++)
                {
                    Vector2Int c = byDistance[i];
                    _frosting[c.x, c.y] = i < config.DoubleFrosting ? 2 : 1;
                    FrostingLeft += _frosting[c.x, c.y];
                    frosted.Add(c);
                }
            }

            List<Vector2Int> cells = ValidCells();
            Shuffle(cells, layout);
            int index = 0;

            // Obstacolele nu stau în rândul de sus al coloanei (de acolo intră piesele noi) și nici jos, la ieșiri.
            bool Inner(Vector2Int c) => !frosted.Contains(c) && c.y < Config.ColumnTop[c.x] && c.y > Config.ColumnBottom[c.x];
            int crates = 0, chocolate = 0, ice = 0, chains = 0;
            for (; index < cells.Count; index++)
            {
                Vector2Int c = cells[index];
                if (!Inner(c)) continue;
                Piece p = _grid[c.x, c.y];
                if (crates < config.Crates) { Set(p, TileKind.Crate, -1, config.CrateHits); crates++; }
                else if (chocolate < config.Chocolate && c.y < Rows / 2) { Set(p, TileKind.Chocolate, -1, 1); chocolate++; }
                else if (ice < config.Ice) { p.Ice = 1 + layout.Next(Math.Max(1, config.IceLayers)); ice++; }
                else if (chains < config.Chains) { p.Chained = true; chains++; }
                if (crates >= config.Crates && chocolate >= config.Chocolate && ice >= config.Ice && chains >= config.Chains) break;
            }

            // Bomboanele-ingredient pornesc de la mijlocul tablei, pe coloane diferite (drumul până jos e rezonabil).
            IngredientsToSpawn = config.Ingredients;
            var used = new HashSet<int>();
            for (int i = 0; i < Math.Min(MaxIngredientsOnBoard, config.Ingredients); i++)
            {
                int x = layout.Next(Columns);
                if (!ColumnClear(x) || !used.Add(x)) continue;
                Piece p = At(new Vector2Int(x, Math.Min(config.ColumnTop[x], Rows / 2 + 1)));
                if (p == null || p.Kind != TileKind.Normal || p.Locked) continue;
                Set(p, TileKind.Ingredient, _random.Next(6), 0);
                IngredientsToSpawn--;
                IngredientsOnBoard++;
            }

            if (!HasValidMove()) ShuffleStep();
        }

        // ------------------------------------------------------------ Acces

        private static float Distance(Vector2Int a, Vector2 b)
        {
            float dx = a.x - b.x, dy = a.y - b.y;
            return dx * dx + dy * dy;
        }

        public bool IsValid(Vector2Int c) => c.x >= 0 && c.x < Columns && c.y >= 0 && c.y < Rows && Config.IsValidCell(c.x, c.y);
        public int FrostingAt(Vector2Int c) => IsValid(c) ? _frosting[c.x, c.y] : 0;

        public bool TryGetPiece(Vector2Int c, out PieceInfo info)
        {
            Piece p = At(c);
            info = p != null ? Info(p, c) : default;
            return p != null;
        }

        public IEnumerable<PieceInfo> AllPieces()
        {
            for (int x = 0; x < Columns; x++)
            for (int y = 0; y < Rows; y++)
                if (_grid[x, y] != null) yield return Info(_grid[x, y], new Vector2Int(x, y));
        }

        private Piece At(Vector2Int c) => IsValid(c) ? _grid[c.x, c.y] : null;

        private static PieceInfo Info(Piece p, Vector2Int c) =>
            new() { Id = p.Id, Cell = c, Kind = p.Kind, Color = p.Color, Ice = p.Ice, Chained = p.Chained, Hits = p.Hits };

        private Piece NewPiece(TileKind kind, int color, int hits = 0) => new() { Id = _nextId++, Kind = kind, Color = color, Hits = hits };

        private static void Set(Piece p, TileKind kind, int color, int hits)
        {
            p.Kind = kind;
            p.Color = color;
            p.Hits = hits;
            p.Ice = 0;
            p.Chained = false;
        }

        private List<Vector2Int> ValidCells()
        {
            var cells = new List<Vector2Int>();
            for (int x = 0; x < Columns; x++)
            for (int y = 0; y < Rows; y++)
                if (IsValid(new Vector2Int(x, y))) cells.Add(new Vector2Int(x, y));
            return cells;
        }

        private static void Shuffle<T>(List<T> list, System.Random random)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        // ------------------------------------------------------------ Grupuri

        /// <summary>Grupul de bomboane la fel, lipite (sus/jos/stânga/dreapta), care conține celula (căutare în lățime).</summary>
        public List<Vector2Int> GroupAt(Vector2Int cell)
        {
            var group = new List<Vector2Int>();
            Piece start = At(cell);
            if (start == null || !start.CanGroup) return group;
            var seen = new HashSet<Vector2Int> { cell };
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(cell);
            while (queue.Count > 0)
            {
                Vector2Int c = queue.Dequeue();
                group.Add(c);
                foreach (Vector2Int d in FourWay)
                {
                    Vector2Int n = c + d;
                    Piece p = At(n);
                    if (p == null || !p.CanGroup || p.Color != start.Color || !seen.Add(n)) continue;
                    queue.Enqueue(n);
                }
            }
            return group;
        }

        public bool IsSpecialAt(Vector2Int c) => At(c) is { IsSpecial: true, Locked: false };

        /// <summary>Piesele speciale vecine (pentru combinații).</summary>
        public List<Vector2Int> AdjacentSpecials(Vector2Int c)
        {
            var list = new List<Vector2Int>();
            foreach (Vector2Int d in FourWay)
                if (IsSpecialAt(c + d)) list.Add(c + d);
            return list;
        }

        /// <summary>Se poate face ceva: un grup de 3+ sau o piesă specială liberă.</summary>
        public bool HasValidMove()
        {
            var seen = new HashSet<Vector2Int>();
            foreach (Vector2Int c in ValidCells())
            {
                Piece p = At(c);
                if (p == null) continue;
                if (p.IsSpecial && !p.Locked) return true;
                if (!p.CanGroup || seen.Contains(c)) continue;
                List<Vector2Int> group = GroupAt(c);
                if (group.Count >= BlastRules.MinGroup) return true;
                foreach (Vector2Int g in group) seen.Add(g);
            }
            return false;
        }

        /// <summary>Dacă atingerea celulei ar face ceva (grup de 3+ sau piesă specială).</summary>
        public bool IsValidTap(Vector2Int c)
        {
            Piece p = At(c);
            if (p == null || !p.CanTap) return false;
            return p.IsSpecial || GroupAt(c).Count >= BlastRules.MinGroup;
        }

        /// <summary>
        /// Cea mai bună atingere pentru obiective (pentru indicii și pentru jucătorul automat din teste):
        /// grupuri mari, glazură, culori cerute, piese de sub bomboane, obstacole lovite, piese speciale.
        /// </summary>
        public bool TryFindBestTap(Func<int, int> colorWeight, out Vector2Int best)
        {
            best = default;
            int bestValue = int.MinValue;
            var seen = new HashSet<Vector2Int>();
            foreach (Vector2Int c in ValidCells())
            {
                Piece p = At(c);
                if (p == null || !p.CanTap || seen.Contains(c)) continue;
                int value;
                if (p.IsSpecial)
                {
                    value = 12 + (p.Kind == TileKind.Rainbow ? 10 : p.Kind == TileKind.Bomb ? 6 : 3) + AdjacentSpecials(c).Count * 8;
                    if (IngredientAbove(c)) value += 12;
                }
                else
                {
                    List<Vector2Int> group = GroupAt(c);
                    foreach (Vector2Int g in group) seen.Add(g);
                    if (group.Count < BlastRules.MinGroup) continue;
                    value = group.Count * 2 + (colorWeight != null ? colorWeight(p.Color) * group.Count : 0);
                    if (group.Count >= BlastRules.RocketMin) value += 6;
                    var touched = new HashSet<Vector2Int>();
                    foreach (Vector2Int g in group)
                    {
                        value += _frosting[g.x, g.y] * 3;
                        if (IngredientAbove(g)) value += 12; // fiecare celulă de sub o bomboană o coboară
                        foreach (Vector2Int d in FourWay)
                        {
                            Piece n = At(g + d);
                            if (n != null && (n.Locked || n.IsBlocker) && touched.Add(g + d)) value += IngredientAbove(g + d) ? 10 : 3;
                        }
                    }
                    // Atingem bomboana cea mai de jos din grup (piesa specială apare acolo).
                    Vector2Int lowest = group[0];
                    foreach (Vector2Int g in group)
                        if (g.y < lowest.y) lowest = g;
                    if (value > bestValue) best = lowest;
                    bestValue = Math.Max(bestValue, value);
                    continue;
                }
                if (value <= bestValue) continue;
                bestValue = value;
                best = c;
            }
            return bestValue > int.MinValue;
        }

        private bool IngredientAbove(Vector2Int c)
        {
            for (int y = c.y + 1; y < Rows; y++)
                if (At(new Vector2Int(c.x, y)) is { IsIngredient: true }) return true;
            return false;
        }

        // ------------------------------------------------------------ Acțiunile jucătorului

        /// <summary>
        /// Atingerea unei celule: elimină grupul (3+) sau activează piesa specială. Null = atingere invalidă
        /// (grup de 1–2, obstacol, piesă blocată): nu consumă mutarea.
        /// </summary>
        public List<BoardStep> Tap(Vector2Int cell)
        {
            Piece p = At(cell);
            if (p == null || !p.CanTap) return null;
            _chocolateHit = false;
            var steps = new List<BoardStep>();

            if (p.IsSpecial)
            {
                var step = new BoardStep(StepKind.Clear) { Origin = cell };
                Detonate(step, new[] { cell }, new HashSet<Vector2Int>(), new HashSet<Vector2Int>());
                steps.Add(step);
            }
            else
            {
                List<Vector2Int> group = GroupAt(cell);
                if (group.Count < BlastRules.MinGroup) return null;
                steps.Add(BlastGroup(cell, group, p.Color));
            }
            Settle(steps, true);
            return steps;
        }

        private BoardStep BlastGroup(Vector2Int cell, List<Vector2Int> group, int color)
        {
            var step = new BoardStep(StepKind.Clear) { Origin = cell, GroupSize = group.Count, GroupColor = color };
            var hitObstacles = new HashSet<Vector2Int>();
            var inGroup = new HashSet<Vector2Int>(group);
            int minX = cell.x, maxX = cell.x, minY = cell.y, maxY = cell.y;
            foreach (Vector2Int c in group)
            {
                Remove(c, At(c), step);
                minX = Math.Min(minX, c.x);
                maxX = Math.Max(maxX, c.x);
                minY = Math.Min(minY, c.y);
                maxY = Math.Max(maxY, c.y);
            }
            // Obstacolele vecine grupului primesc câte o lovitură (o singură dată pe acțiune).
            foreach (Vector2Int c in group)
                foreach (Vector2Int d in FourWay)
                {
                    Vector2Int n = c + d;
                    if (inGroup.Contains(n)) continue;
                    Piece neighbour = At(n);
                    if (neighbour != null && (neighbour.Locked || neighbour.IsBlocker)) Damage(n, neighbour, step, hitObstacles);
                }

            // Piesa specială apare exact în celula atinsă.
            int width = maxX - minX + 1, height = maxY - minY + 1;
            bool horizontal = width != height ? width > height : _random.Next(2) == 0;
            TileKind special = BlastRules.SpecialForGroup(group.Count, horizontal);
            if (special != TileKind.Normal)
            {
                Piece created = NewPiece(special, special == TileKind.Rainbow ? color : -1);
                _grid[cell.x, cell.y] = created;
                step.Changed.Add(Info(created, cell));
            }
            return step;
        }

        /// <summary>Combină două piese speciale vecine (ambele dispar, efectul e mai mare). Null dacă nu se poate.</summary>
        public List<BoardStep> Combine(Vector2Int selected, Vector2Int target)
        {
            if (Math.Abs(selected.x - target.x) + Math.Abs(selected.y - target.y) != 1) return null;
            Piece a = At(selected), b = At(target);
            if (a == null || b == null || !a.IsSpecial || !b.IsSpecial || a.Locked || b.Locked) return null;
            _chocolateHit = false;

            var step = new BoardStep(StepKind.Clear) { Origin = target, Combo = true };
            var cells = new List<Vector2Int>();
            Piece rainbow = a.Kind == TileKind.Rainbow ? a : b.Kind == TileKind.Rainbow ? b : null;
            Piece other = rainbow == a ? b : a;
            TileKind kindA = a.Kind, kindB = b.Kind;
            Remove(selected, a, step);
            Remove(target, b, step);

            if (rainbow != null && other.Kind == TileKind.Rainbow)
            {
                // Curcubeu + curcubeu: toate bomboanele obișnuite.
                foreach (Vector2Int c in ValidCells())
                    if (At(c) is { Kind: TileKind.Normal }) cells.Add(c);
                step.Activations.Add(new Activation { Kind = TileKind.Rainbow, Cell = target, Color = -1, Targets = new List<Vector2Int>(cells) });
            }
            else if (rainbow != null)
            {
                // Curcubeu + rachetă / bombă: toate bomboanele unei culori devin rachete / bombe și se activează.
                int color = TargetColor(rainbow.Color);
                foreach (Vector2Int c in ValidCells())
                {
                    Piece p = At(c);
                    if (p == null || !p.CanGroup || p.Color != color) continue;
                    p.Kind = other.Kind == TileKind.Bomb ? TileKind.Bomb : _random.Next(2) == 0 ? TileKind.RocketH : TileKind.RocketV;
                    p.Color = -1;
                    step.Changed.Add(Info(p, c));
                    cells.Add(c);
                }
                step.Activations.Add(new Activation { Kind = TileKind.Rainbow, Cell = target, Color = color, Targets = new List<Vector2Int>(cells) });
            }
            else
            {
                bool rocketA = BlastRules.IsRocket(kindA), rocketB = BlastRules.IsRocket(kindB);
                if (rocketA && rocketB)
                {
                    // Rachetă + rachetă: un rând și o coloană.
                    AddRow(cells, target.y);
                    AddColumn(cells, target.x);
                    step.Activations.Add(new Activation { Kind = TileKind.RocketH, Cell = target, Color = -1, Targets = Row(target.y) });
                    step.Activations.Add(new Activation { Kind = TileKind.RocketV, Cell = target, Color = -1, Targets = Column(target.x) });
                }
                else if (rocketA || rocketB)
                {
                    // Rachetă + bombă: trei rânduri și trei coloane.
                    for (int d = -1; d <= 1; d++)
                    {
                        AddRow(cells, target.y + d);
                        AddColumn(cells, target.x + d);
                        step.Activations.Add(new Activation { Kind = TileKind.RocketH, Cell = new Vector2Int(target.x, target.y + d), Color = -1, Targets = Row(target.y + d) });
                        step.Activations.Add(new Activation { Kind = TileKind.RocketV, Cell = new Vector2Int(target.x + d, target.y), Color = -1, Targets = Column(target.x + d) });
                    }
                }
                else
                {
                    // Bombă + bombă: zonă 7×7.
                    cells.AddRange(Square(target, BlastRules.BombComboSize));
                    step.Activations.Add(new Activation { Kind = TileKind.Bomb, Cell = target, Color = -1, Targets = new List<Vector2Int>(cells) });
                }
            }

            Detonate(step, cells, new HashSet<Vector2Int>(), new HashSet<Vector2Int> { selected, target });
            var steps = new List<BoardStep> { step };
            Settle(steps, true);
            return steps;
        }

        /// <summary>Ajutorul „bombă” al jucătorului: explozie 3×3 (nu consumă mutarea).</summary>
        public List<BoardStep> Blast(Vector2Int center)
        {
            if (!IsValid(center)) return null;
            var cells = Square(center, 3);
            var step = new BoardStep(StepKind.Clear) { Origin = center };
            step.Activations.Add(new Activation { Kind = TileKind.Bomb, Cell = center, Color = -1, Targets = new List<Vector2Int>(cells) });
            Detonate(step, cells, new HashSet<Vector2Int>(), new HashSet<Vector2Int>());
            var steps = new List<BoardStep> { step };
            Settle(steps, false);
            return steps;
        }

        /// <summary>Finalul: transformă o bomboană obișnuită, la întâmplare, într-o rachetă.</summary>
        public BoardStep MakeRandomSpecial()
        {
            var candidates = new List<Vector2Int>();
            foreach (Vector2Int c in ValidCells())
                if (At(c) is { CanGroup: true }) candidates.Add(c);
            if (candidates.Count == 0) return null;
            Vector2Int cell = candidates[_random.Next(candidates.Count)];
            Piece p = At(cell);
            p.Kind = _random.Next(2) == 0 ? TileKind.RocketH : TileKind.RocketV;
            p.Color = -1;
            var step = new BoardStep(StepKind.Clear);
            step.Changed.Add(Info(p, cell));
            return step;
        }

        /// <summary>Finalul: activează următoarea piesă specială (de sus în jos). Null dacă nu mai e niciuna.</summary>
        public List<BoardStep> DetonateNextSpecial()
        {
            for (int y = Rows - 1; y >= 0; y--)
            for (int x = 0; x < Columns; x++)
            {
                var c = new Vector2Int(x, y);
                if (!IsSpecialAt(c)) continue;
                var step = new BoardStep(StepKind.Clear) { Origin = c };
                Detonate(step, new[] { c }, new HashSet<Vector2Int>(), new HashSet<Vector2Int>());
                var steps = new List<BoardStep> { step };
                Settle(steps, false);
                return steps;
            }
            return null;
        }

        /// <summary>Amestecă culorile bomboanelor libere (ajutorul „amestecă” sau automat când nu mai sunt mutări).</summary>
        public BoardStep ShuffleStep()
        {
            var cells = new List<Vector2Int>();
            foreach (Vector2Int c in ValidCells())
                if (At(c) is { CanGroup: true }) cells.Add(c);
            var step = new BoardStep(StepKind.Shuffle);
            if (cells.Count >= BlastRules.MinGroup)
            {
                var colors = new List<int>();
                foreach (Vector2Int c in cells) colors.Add(At(c).Color);
                for (int attempt = 0; attempt < 200; attempt++)
                {
                    Shuffle(colors, _random);
                    // Dacă distribuția nu permite niciun grup, schimbăm treptat câteva culori.
                    if (attempt > 10) colors[_random.Next(colors.Count)] = colors[_random.Next(colors.Count)];
                    for (int i = 0; i < cells.Count; i++) At(cells[i]).Color = colors[i];
                    if (HasValidMove()) break;
                }
                foreach (Vector2Int c in cells) step.Changed.Add(Info(At(c), c));
            }
            Stuck = !HasValidMove();
            return step;
        }

        // ------------------------------------------------------------ Explozii

        /// <summary>
        /// Lovește celulele date: bomboanele dispar, piesele speciale se activează (în lanț, fiecare o singură dată),
        /// obstacolele primesc o singură lovitură pe acțiune, glazura scade, bomboanele-ingredient rămân.
        /// </summary>
        private void Detonate(BoardStep step, IEnumerable<Vector2Int> cells, HashSet<Vector2Int> hitObstacles, HashSet<Vector2Int> processed)
        {
            var queue = new Queue<Vector2Int>(cells);
            while (queue.Count > 0)
            {
                Vector2Int c = queue.Dequeue();
                if (!IsValid(c) || !processed.Add(c)) continue;
                Piece p = At(c);
                if (p == null)
                {
                    HitFrosting(c, step);
                    continue;
                }
                if (p.IsIngredient) continue;
                if (p.Locked || p.IsBlocker)
                {
                    Damage(c, p, step, hitObstacles);
                    continue;
                }
                if (p.IsSpecial)
                {
                    List<Vector2Int> targets = Targets(p, c);
                    step.Activations.Add(new Activation { Kind = p.Kind, Cell = c, Color = p.Color, Targets = targets });
                    Remove(c, p, step);
                    foreach (Vector2Int t in targets)
                        if (!processed.Contains(t)) queue.Enqueue(t);
                    continue;
                }
                Remove(c, p, step);
            }
        }

        private List<Vector2Int> Targets(Piece p, Vector2Int c)
        {
            List<Vector2Int> targets = p.Kind switch
            {
                TileKind.RocketH => Row(c.y),
                TileKind.RocketV => Column(c.x),
                TileKind.Bomb => Square(c, BlastRules.BombSize),
                _ => ColorCells(TargetColor(p.Color)),
            };
            targets.Remove(c);
            return targets;
        }

        private List<Vector2Int> Row(int y)
        {
            var list = new List<Vector2Int>();
            AddRow(list, y);
            return list;
        }

        private List<Vector2Int> Column(int x)
        {
            var list = new List<Vector2Int>();
            AddColumn(list, x);
            return list;
        }

        private void AddRow(List<Vector2Int> list, int y)
        {
            for (int x = 0; x < Columns; x++)
                if (IsValid(new Vector2Int(x, y))) list.Add(new Vector2Int(x, y));
        }

        private void AddColumn(List<Vector2Int> list, int x)
        {
            for (int y = 0; y < Rows; y++)
                if (IsValid(new Vector2Int(x, y))) list.Add(new Vector2Int(x, y));
        }

        /// <summary>Pătrat de latură „size” centrat în c, tăiat la marginile tablei.</summary>
        private List<Vector2Int> Square(Vector2Int c, int size)
        {
            var list = new List<Vector2Int>();
            int r = size / 2;
            for (int dx = -r; dx <= r; dx++)
            for (int dy = -r; dy <= r; dy++)
            {
                var cell = new Vector2Int(c.x + dx, c.y + dy);
                if (IsValid(cell)) list.Add(cell);
            }
            return list;
        }

        private List<Vector2Int> ColorCells(int color)
        {
            var list = new List<Vector2Int>();
            foreach (Vector2Int c in ValidCells())
                if (At(c) is { CanGroup: true } p && p.Color == color) list.Add(c);
            return list;
        }

        /// <summary>Culoarea curcubeului: cea din care s-a născut, dacă mai există pe tablă; altfel cea mai des întâlnită.</summary>
        private int TargetColor(int preferred)
        {
            var counts = new int[Math.Max(1, Config.Colors)];
            foreach (Vector2Int c in ValidCells())
                if (At(c) is { CanGroup: true } p && p.Color < counts.Length) counts[p.Color]++;
            if (preferred >= 0 && preferred < counts.Length && counts[preferred] > 0) return preferred;
            int best = 0;
            for (int i = 1; i < counts.Length; i++)
                if (counts[i] > counts[best]) best = i;
            return best;
        }

        /// <summary>O lovitură într-un obstacol (o singură dată pe acțiune): gheață, lanț, ladă sau ciocolată.</summary>
        private void Damage(Vector2Int c, Piece p, BoardStep step, HashSet<Vector2Int> hitObstacles)
        {
            if (!hitObstacles.Add(c)) return;
            if (p.Ice > 0) p.Ice--;
            else if (p.Chained) p.Chained = false;
            else if (p.IsBlocker)
            {
                if (p.Kind == TileKind.Chocolate) _chocolateHit = true;
                p.Hits--;
                if (p.Hits <= 0)
                {
                    _grid[c.x, c.y] = null;
                    step.Cleared.Add(Info(p, c));
                    return;
                }
            }
            else return;
            step.ObstacleHits.Add(Info(p, c));
        }

        private void Remove(Vector2Int c, Piece p, BoardStep step)
        {
            _grid[c.x, c.y] = null;
            step.Cleared.Add(Info(p, c));
            HitFrosting(c, step);
        }

        private void HitFrosting(Vector2Int c, BoardStep step)
        {
            if (_frosting[c.x, c.y] <= 0) return;
            _frosting[c.x, c.y]--;
            FrostingLeft--;
            step.FrostingCells.Add(c);
            step.FrostingLeft.Add(_frosting[c.x, c.y]);
        }

        // ------------------------------------------------------------ Cădere, piese noi, ieșiri

        /// <summary>Căderi, piese noi și ieșirea bomboanelor până tabla se liniștește (fără cascade automate).</summary>
        private void Settle(List<BoardStep> steps, bool allowGrowth)
        {
            for (int guard = 0; guard < 30; guard++)
            {
                BoardStep fall = Gravity();
                if (fall.Moves.Count > 0) steps.Add(fall);
                BoardStep exit = Exits();
                if (exit.Exits.Count == 0) break;
                steps.Add(exit);
            }
            if (allowGrowth) GrowChocolate(steps);
            if (!HasValidMove()) steps.Add(ShuffleStep());
        }

        /// <summary>
        /// Piesele cad în golurile de dedesubt; lăzile, ciocolata și bomboanele blocate (gheață/lanț) opresc căderea.
        /// Golurile de sub ele se umplu pe diagonală din coloanele vecine. Piese noi intră pe sus.
        /// </summary>
        private BoardStep Gravity()
        {
            var step = new BoardStep(StepKind.Fall);
            var index = new Dictionary<int, int>();
            for (int guard = 0; guard < 400; guard++)
            {
                bool moved = FallVertical(step, index);
                moved |= SpawnTop(step, index);
                if (moved) continue;
                if (!SlideDiagonal(step, index)) break;
            }
            return step;
        }

        private bool FallVertical(BoardStep step, Dictionary<int, int> index)
        {
            bool moved = false;
            for (int x = 0; x < Columns; x++)
            for (int y = Config.ColumnBottom[x]; y <= Config.ColumnTop[x]; y++)
            {
                if (_grid[x, y] != null) continue;
                for (int above = y + 1; above <= Config.ColumnTop[x]; above++)
                {
                    Piece p = _grid[x, above];
                    if (p == null) continue;
                    if (!p.Falls) break;
                    Move(p, new Vector2Int(x, above), new Vector2Int(x, y), step, index);
                    moved = true;
                    break;
                }
            }
            return moved;
        }

        private bool SpawnTop(BoardStep step, Dictionary<int, int> index)
        {
            bool spawned = false;
            for (int x = 0; x < Columns; x++)
            {
                int top = Config.ColumnTop[x];
                int y = top;
                while (y >= Config.ColumnBottom[x] && _grid[x, y] == null) y--;
                int count = top - y;
                for (int i = 0; i < count; i++)
                {
                    var cell = new Vector2Int(x, y + 1 + i);
                    Piece p = SpawnPiece(x);
                    _grid[cell.x, cell.y] = p;
                    index[p.Id] = step.Moves.Count;
                    step.Moves.Add(new PieceMove { Piece = Info(p, cell), From = new Vector2Int(x, top + 1 + i), To = cell, Spawned = true });
                    spawned = true;
                }
            }
            return spawned;
        }

        /// <summary>Coloana nu are obstacole care ar ține o bomboană-ingredient pe loc.</summary>
        private bool ColumnClear(int x)
        {
            for (int y = Config.ColumnBottom[x]; y <= Config.ColumnTop[x]; y++)
                if (_grid[x, y] is { } p && (p.IsBlocker || p.Locked)) return false;
            return true;
        }

        private Piece SpawnPiece(int column)
        {
            // Bomboanele-ingredient apar treptat, doar pe coloane fără obstacole: mereu cel puțin una pe tablă.
            if (IngredientsToSpawn > 0 && IngredientsOnBoard < MaxIngredientsOnBoard && ColumnClear(column)
                && (IngredientsOnBoard == 0 || _random.NextDouble() < 0.12))
            {
                IngredientsToSpawn--;
                IngredientsOnBoard++;
                return NewPiece(TileKind.Ingredient, _random.Next(6));
            }
            return NewPiece(TileKind.Normal, _random.Next(Config.Colors));
        }

        private bool SlideDiagonal(BoardStep step, Dictionary<int, int> index)
        {
            for (int x = 0; x < Columns; x++)
            for (int y = Config.ColumnBottom[x]; y <= Config.ColumnTop[x]; y++)
            {
                if (_grid[x, y] != null) continue;
                foreach (int side in new[] { -1, 1 })
                {
                    var source = new Vector2Int(x + side, y + 1);
                    Piece p = At(source);
                    if (p == null || !p.Falls) continue;
                    var below = new Vector2Int(source.x, source.y - 1);
                    if (IsValid(below) && At(below) == null) continue;
                    Move(p, source, new Vector2Int(x, y), step, index);
                    return true;
                }
            }
            return false;
        }

        private void Move(Piece p, Vector2Int from, Vector2Int to, BoardStep step, Dictionary<int, int> index)
        {
            _grid[from.x, from.y] = null;
            _grid[to.x, to.y] = p;
            if (index.TryGetValue(p.Id, out int i))
            {
                PieceMove move = step.Moves[i];
                move.To = to;
                move.Piece.Cell = to;
                step.Moves[i] = move;
                return;
            }
            index[p.Id] = step.Moves.Count;
            step.Moves.Add(new PieceMove { Piece = Info(p, to), From = from, To = to });
        }

        private BoardStep Exits()
        {
            var step = new BoardStep(StepKind.Exit);
            for (int x = 0; x < Columns; x++)
            {
                var c = new Vector2Int(x, Config.ColumnBottom[x]);
                Piece p = At(c);
                if (p == null || !p.IsIngredient) continue;
                _grid[c.x, c.y] = null;
                step.Exits.Add(Info(p, c));
                IngredientsOnBoard--;
                IngredientsExited++;
            }
            return step;
        }

        /// <summary>Dacă acțiunea n-a lovit nicio ciocolată, ciocolata se întinde peste o bomboană vecină.</summary>
        private void GrowChocolate(List<BoardStep> steps)
        {
            if (_chocolateHit) return;
            var options = new List<Vector2Int>();
            foreach (Vector2Int c in ValidCells())
            {
                if (At(c) is not { Kind: TileKind.Chocolate }) continue;
                foreach (Vector2Int d in FourWay)
                    if (At(c + d) is { Kind: TileKind.Normal, Locked: false }) options.Add(c + d);
            }
            if (options.Count == 0) return;
            Vector2Int target = options[_random.Next(options.Count)];
            Piece p = At(target);
            Set(p, TileKind.Chocolate, -1, 1);
            var step = new BoardStep(StepKind.Grow);
            step.Changed.Add(Info(p, target));
            steps.Add(step);
        }

        // ------------------------------------------------------------ Verificări (teste)

        /// <summary>Nicio piesă în afara formei, niciun Id dublat, bomboanele numărate corect.</summary>
        public bool CheckIntegrity(out string problem)
        {
            var ids = new HashSet<int>();
            int ingredients = 0;
            for (int x = 0; x < Columns; x++)
            for (int y = 0; y < Rows; y++)
            {
                Piece p = _grid[x, y];
                if (p == null) continue;
                if (!IsValid(new Vector2Int(x, y)))
                {
                    problem = $"piesă în afara tablei la {x},{y}";
                    return false;
                }
                if (!ids.Add(p.Id))
                {
                    problem = $"Id dublat {p.Id}";
                    return false;
                }
                if (p.IsIngredient) ingredients++;
            }
            if (ingredients != IngredientsOnBoard)
            {
                problem = $"bomboane pe tablă: {ingredients}, numărate: {IngredientsOnBoard}";
                return false;
            }
            problem = null;
            return true;
        }

        public int EmptyCells()
        {
            int count = 0;
            foreach (Vector2Int c in ValidCells())
                if (At(c) == null) count++;
            return count;
        }

        /// <summary>Pentru teste: pune o piesă anume într-o celulă (înlocuiește ce era acolo).</summary>
        public void DebugSet(Vector2Int c, TileKind kind, int color, int ice = 0, bool chained = false, int hits = 0)
        {
            Piece old = At(c);
            if (old != null && old.IsIngredient) IngredientsOnBoard--;
            Piece p = NewPiece(kind, color, hits);
            p.Ice = ice;
            p.Chained = chained;
            _grid[c.x, c.y] = p;
            if (kind == TileKind.Ingredient) IngredientsOnBoard++;
        }

        /// <summary>Pentru teste: umple toată tabla cu o culoare (fără obstacole), ca grupurile să fie previzibile.</summary>
        public void DebugFill(Func<Vector2Int, int> colorAt)
        {
            IngredientsOnBoard = 0;
            foreach (Vector2Int c in ValidCells()) _grid[c.x, c.y] = NewPiece(TileKind.Normal, colorAt(c));
        }
    }
}
