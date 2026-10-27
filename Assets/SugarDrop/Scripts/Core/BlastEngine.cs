using System;
using System.Collections.Generic;
using UnityEngine;

namespace SugarDrop
{
    /// <summary>
    /// Regulile tablei (pur, fără nimic vizual, testabil în afara Unity), ca în jocurile clasice cu bomboane:
    /// schimbi două piese vecine și liniile de 3+ bomboane la fel (orizontal/vertical) dispar. 4 în linie lasă o
    /// rachetă, liniile în L/T o bombă, 5 în linie un curcubeu. O piesă specială mutată se activează, două speciale
    /// schimbate între ele se combină. Apoi piesele cad, intră piese noi de sus, liniile nou formate dispar în cascadă,
    /// bomboanele-ingredient ies jos spre borcan. Obstacole: gheață (1–3 straturi), lanțuri, lăzi, ciocolată care
    /// crește, glazură pe celule. Fiecare acțiune întoarce pașii pe care tabla din scenă îi animă.
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

            // Tabla pornește fără linii gata făcute (ca în jocurile clasice).
            BreakMatches();
            if (!HasValidMove()) ShuffleStep();
        }

        /// <summary>Recolorează bomboanele libere până nu mai există nicio linie de 3+ (la start și la amestecare).</summary>
        private void BreakMatches()
        {
            for (int guard = 0; guard < 200; guard++)
            {
                List<MatchGroup> groups = FindMatches();
                if (groups.Count == 0) return;
                foreach (MatchGroup g in groups)
                    foreach (Vector2Int c in g.Cells)
                    {
                        Piece p = At(c);
                        if (p.Locked || !HasMatchAt(c)) continue;
                        int start = _random.Next(Config.Colors);
                        for (int i = 0; i < Config.Colors && HasMatchAt(c); i++) p.Color = (start + i) % Config.Colors;
                    }
            }
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

        // ------------------------------------------------------------ Potriviri (3+ în linie)

        /// <summary>Bomboană care poate face parte dintr-o linie (și cele cu gheață/lanț: linia le sparge stratul).</summary>
        private static bool CanMatch(Piece p) => p != null && p.Kind == TileKind.Normal && p.Color >= 0;

        /// <summary>Piesă care se poate muta (schimba cu o vecină): nu obstacolele și nu bomboanele blocate.</summary>
        private static bool CanMove(Piece p) => p != null && !p.Locked && !p.IsBlocker;

        private static bool AreNeighbours(Vector2Int a, Vector2Int b) => Math.Abs(a.x - b.x) + Math.Abs(a.y - b.y) == 1;

        /// <summary>Un grup de bomboane potrivite (una sau mai multe linii lipite, de aceeași culoare).</summary>
        private sealed class MatchGroup
        {
            public readonly List<Vector2Int> Cells = new();
            public int Color;
            public int LongestH, LongestV;
        }

        /// <summary>Lungimea liniei (orizontală sau verticală) de aceeași culoare care trece prin celulă.</summary>
        private int RunLength(Vector2Int c, Vector2Int axis)
        {
            Piece p = At(c);
            if (!CanMatch(p)) return 0;
            int length = 1;
            for (Vector2Int n = c + axis; CanMatch(At(n)) && At(n).Color == p.Color; n += axis) length++;
            for (Vector2Int n = c - axis; CanMatch(At(n)) && At(n).Color == p.Color; n -= axis) length++;
            return length;
        }

        private bool HasMatchAt(Vector2Int c) =>
            RunLength(c, Vector2Int.right) >= BlastRules.MinGroup || RunLength(c, Vector2Int.up) >= BlastRules.MinGroup;

        /// <summary>Toate liniile de 3+ de pe tablă, unite în grupuri (liniile care se ating formează L/T).</summary>
        private List<MatchGroup> FindMatches()
        {
            var h = new int[Columns, Rows];
            var v = new int[Columns, Rows];
            bool any = false;
            foreach (Vector2Int c in ValidCells())
            {
                int lh = RunLength(c, Vector2Int.right), lv = RunLength(c, Vector2Int.up);
                if (lh >= BlastRules.MinGroup) { h[c.x, c.y] = lh; any = true; }
                if (lv >= BlastRules.MinGroup) { v[c.x, c.y] = lv; any = true; }
            }
            var groups = new List<MatchGroup>();
            if (!any) return groups;
            var seen = new HashSet<Vector2Int>();
            foreach (Vector2Int c in ValidCells())
            {
                if (h[c.x, c.y] == 0 && v[c.x, c.y] == 0 || !seen.Add(c)) continue;
                var group = new MatchGroup { Color = At(c).Color };
                var queue = new Queue<Vector2Int>();
                queue.Enqueue(c);
                while (queue.Count > 0)
                {
                    Vector2Int g = queue.Dequeue();
                    group.Cells.Add(g);
                    group.LongestH = Math.Max(group.LongestH, h[g.x, g.y]);
                    group.LongestV = Math.Max(group.LongestV, v[g.x, g.y]);
                    foreach (Vector2Int d in FourWay)
                    {
                        Vector2Int n = g + d;
                        if (!IsValid(n) || h[n.x, n.y] == 0 && v[n.x, n.y] == 0) continue;
                        if (At(n).Color != group.Color || !seen.Add(n)) continue;
                        queue.Enqueue(n);
                    }
                }
                groups.Add(group);
            }
            return groups;
        }

        /// <summary>
        /// Piesa specială dintr-un grup: 5 în linie → curcubeu; linii în L/T → bombă; 4 în linie → rachetă
        /// (perpendiculară pe linie, ca în jocurile clasice); 3 → niciuna.
        /// </summary>
        private static TileKind SpecialFor(MatchGroup g)
        {
            int longest = Math.Max(g.LongestH, g.LongestV);
            if (longest >= 5) return TileKind.Rainbow;
            if (g.LongestH >= BlastRules.MinGroup && g.LongestV >= BlastRules.MinGroup) return TileKind.Bomb;
            if (g.LongestH == 4) return TileKind.RocketV;
            if (g.LongestV == 4) return TileKind.RocketH;
            return TileKind.Normal;
        }

        /// <summary>Obstacolele de pe tablă acum (la start: țintele obiectivelor cu lăzi, gheață, lanțuri, ciocolată).</summary>
        public ObstacleCounts CountObstacles()
        {
            var counts = new ObstacleCounts();
            foreach (Vector2Int c in ValidCells())
            {
                Piece p = At(c);
                if (p == null) continue;
                if (p.Kind == TileKind.Crate) counts.Crates++;
                else if (p.Kind == TileKind.Chocolate) counts.Chocolate++;
                counts.IceLayers += p.Ice;
                if (p.Chained) counts.Chains++;
            }
            return counts;
        }

        public bool IsSpecialAt(Vector2Int c) => At(c) is { IsSpecial: true, Locked: false };

        /// <summary>Schimbul a două piese vecine ar face ceva (o linie de 3+ sau o piesă specială mutată).</summary>
        public bool IsValidSwap(Vector2Int a, Vector2Int b)
        {
            if (!AreNeighbours(a, b)) return false;
            Piece pa = At(a), pb = At(b);
            if (!CanMove(pa) || !CanMove(pb)) return false;
            if (pa.IsSpecial || pb.IsSpecial) return true;
            Exchange(a, b);
            bool match = HasMatchAt(a) || HasMatchAt(b);
            Exchange(a, b);
            return match;
        }

        private void Exchange(Vector2Int a, Vector2Int b) =>
            (_grid[a.x, a.y], _grid[b.x, b.y]) = (_grid[b.x, b.y], _grid[a.x, a.y]);

        /// <summary>Există cel puțin un schimb care face ceva.</summary>
        public bool HasValidMove()
        {
            foreach (Vector2Int c in ValidCells())
            {
                if (IsValidSwap(c, c + Vector2Int.right)) return true;
                if (IsValidSwap(c, c + Vector2Int.up)) return true;
            }
            return false;
        }

        /// <summary>
        /// Cel mai bun schimb pentru obiective (pentru indicii și pentru jucătorul automat din teste): linii lungi
        /// și piese speciale, glazură, culori cerute, obstacole lovite, bomboane-ingredient coborâte.
        /// </summary>
        public bool TryFindBestSwap(Func<int, int> colorWeight, out Vector2Int bestFrom, out Vector2Int bestTo)
        {
            bestFrom = bestTo = default;
            int bestValue = int.MinValue;
            foreach (Vector2Int c in ValidCells())
                foreach (Vector2Int d in new[] { Vector2Int.right, Vector2Int.up })
                {
                    Vector2Int n = c + d;
                    if (!IsValidSwap(c, n)) continue;
                    int value = SwapValue(c, n, colorWeight);
                    if (value <= bestValue) continue;
                    bestValue = value;
                    // Mutăm piesa de sus în jos (sau cea specială): așa arată și indiciul.
                    bool fromTop = n.y > c.y;
                    bestFrom = At(c).IsSpecial ? c : At(n).IsSpecial || fromTop ? n : c;
                    bestTo = bestFrom == c ? n : c;
                }
            return bestValue > int.MinValue;
        }

        private int SwapValue(Vector2Int a, Vector2Int b, Func<int, int> colorWeight)
        {
            Piece pa = At(a), pb = At(b);
            if (pa.IsSpecial || pb.IsSpecial)
            {
                int Weight(Piece p) => p.Kind == TileKind.Rainbow ? 20 : p.Kind == TileKind.Bomb ? 12 : p.IsSpecial ? 8 : 0;
                int special = 10 + Weight(pa) + Weight(pb) + (pa.IsSpecial && pb.IsSpecial ? 25 : 0);
                if (IngredientAbove(a) || IngredientAbove(b)) special += 12;
                return special;
            }
            Exchange(a, b);
            int value = 0;
            foreach (MatchGroup g in FindMatches())
            {
                value += g.Cells.Count * 2 + (colorWeight != null ? colorWeight(g.Color) * g.Cells.Count : 0);
                value += SpecialFor(g) switch { TileKind.Rainbow => 30, TileKind.Bomb => 16, TileKind.Normal => 0, _ => 9 };
                var touched = new HashSet<Vector2Int>();
                foreach (Vector2Int c in g.Cells)
                {
                    value += _frosting[c.x, c.y] * 3;
                    if (At(c).Locked) value += 4;
                    if (IngredientAbove(c)) value += 12;
                    foreach (Vector2Int d in FourWay)
                    {
                        Piece n = At(c + d);
                        if (n == null || !touched.Add(c + d)) continue;
                        if (n.Locked || n.IsBlocker) value += IngredientAbove(c + d) ? 10 : 3;
                    }
                }
            }
            Exchange(a, b);
            // Coborârea directă a unei bomboane-ingredient.
            if (pa.IsIngredient && b.y < a.y || pb.IsIngredient && a.y < b.y) value += 15;
            return value;
        }

        private bool IngredientAbove(Vector2Int c)
        {
            for (int y = c.y + 1; y < Rows; y++)
                if (At(new Vector2Int(c.x, y)) is { IsIngredient: true }) return true;
            return false;
        }

        // ------------------------------------------------------------ Acțiunile jucătorului

        /// <summary>
        /// Schimbă două piese vecine. Linia de 3+ dispare (4 → rachetă, L/T → bombă, 5 → curcubeu), o piesă
        /// specială mutată se activează, două speciale se combină. Apoi cădere și cascade până se liniștește.
        /// Null = schimb invalid (nu face nimic): nu consumă mutarea, tabla îl întoarce înapoi.
        /// </summary>
        public List<BoardStep> Swap(Vector2Int a, Vector2Int b)
        {
            if (!IsValidSwap(a, b)) return null;
            _chocolateHit = false;
            Piece pa = At(a), pb = At(b);
            Exchange(a, b);
            var swap = new BoardStep(StepKind.Swap) { Origin = b };
            swap.Moves.Add(new PieceMove { Piece = Info(pa, b), From = a, To = b });
            swap.Moves.Add(new PieceMove { Piece = Info(pb, a), From = b, To = a });
            var steps = new List<BoardStep> { swap };

            if (pa.IsSpecial && pb.IsSpecial) steps.Add(CombineSpecials(b, a));
            else if (pa.IsSpecial || pb.IsSpecial)
            {
                Vector2Int cell = pa.IsSpecial ? b : a;
                Piece special = At(cell), other = pa.IsSpecial ? pb : pa;
                // Curcubeul ia culoarea bomboanei cu care a fost schimbat.
                if (special.Kind == TileKind.Rainbow && CanMatch(other)) special.Color = other.Color;
                var step = new BoardStep(StepKind.Clear) { Origin = cell };
                Detonate(step, new[] { cell }, new HashSet<Vector2Int>(), new HashSet<Vector2Int>());
                steps.Add(step);
                // Bomboana mutată poate face și ea o linie (după cădere, ca o cascadă).
            }
            else steps.Add(ResolveMatches(FindMatches(), 0, a, b));
            Settle(steps, true);
            return steps;
        }

        /// <summary>
        /// Elimină grupurile potrivite: bomboanele dispar (cele cu gheață/lanț pierd doar un strat), obstacolele
        /// vecine primesc o lovitură, iar grupurile mari lasă o piesă specială (în celula mutată, dacă e în grup).
        /// </summary>
        private BoardStep ResolveMatches(List<MatchGroup> groups, int cascade, Vector2Int? movedA = null, Vector2Int? movedB = null, bool createSpecials = true)
        {
            var step = new BoardStep(StepKind.Clear, cascade);
            var hit = new HashSet<Vector2Int>();
            var matched = new HashSet<Vector2Int>();
            MatchGroup biggest = null;
            foreach (MatchGroup g in groups)
            {
                foreach (Vector2Int c in g.Cells) matched.Add(c);
                if (biggest == null || g.Cells.Count > biggest.Cells.Count) biggest = g;
            }

            var created = new List<(Vector2Int Cell, TileKind Kind, int Color)>();
            foreach (MatchGroup g in groups)
            {
                TileKind special = createSpecials ? SpecialFor(g) : TileKind.Normal;
                if (special == TileKind.Normal) continue;
                created.Add((SpecialCell(g, movedA, movedB), special, g.Color));
            }

            int removed = 0;
            foreach (Vector2Int c in matched)
            {
                Piece p = At(c);
                if (p == null) continue;
                if (p.Locked) Damage(c, p, step, hit);
                else
                {
                    Remove(c, p, step);
                    removed++;
                }
            }
            // Obstacolele vecine primesc câte o lovitură; bomboana de sub un ingredient alăturat e strivită.
            foreach (Vector2Int c in matched)
                foreach (Vector2Int d in FourWay)
                {
                    Vector2Int n = c + d;
                    if (matched.Contains(n)) continue;
                    Piece neighbour = At(n);
                    if (neighbour == null) continue;
                    if (neighbour.IsBlocker || neighbour.Locked) Damage(n, neighbour, step, hit);
                    else if (neighbour.Kind == TileKind.Normal && !neighbour.Locked && At(n + Vector2Int.up) is { IsIngredient: true } && hit.Add(n))
                        Remove(n, neighbour, step);
                }

            foreach ((Vector2Int cell, TileKind kind, int color) in created)
            {
                if (At(cell) != null) continue; // celula a rămas ocupată (bomboană cu gheață)
                Piece piece = NewPiece(kind, kind == TileKind.Rainbow ? color : -1);
                _grid[cell.x, cell.y] = piece;
                step.Changed.Add(Info(piece, cell));
            }

            step.GroupSize = removed;
            step.GroupColor = biggest?.Color ?? -1;
            step.Origin = created.Count > 0 ? created[0].Cell : movedB ?? movedA ?? (biggest != null ? biggest.Cells[biggest.Cells.Count / 2] : default);
            return step;
        }

        /// <summary>Unde apare piesa specială: în celula mutată de jucător, altfel la intersecția liniilor / la mijloc.</summary>
        private Vector2Int SpecialCell(MatchGroup g, Vector2Int? movedA, Vector2Int? movedB)
        {
            foreach (Vector2Int? moved in new[] { movedB, movedA })
                if (moved.HasValue && g.Cells.Contains(moved.Value) && !At(moved.Value).Locked) return moved.Value;
            Vector2Int best = g.Cells[g.Cells.Count / 2];
            int bestScore = -1;
            foreach (Vector2Int c in g.Cells)
            {
                if (At(c).Locked) continue;
                int score = (RunLength(c, Vector2Int.right) >= 3 ? 1 : 0) + (RunLength(c, Vector2Int.up) >= 3 ? 1 : 0);
                if (score <= bestScore) continue;
                bestScore = score;
                best = c;
            }
            return best;
        }

        /// <summary>Două piese speciale schimbate între ele: ambele dispar, efectul e mai mare (combinația).</summary>
        private BoardStep CombineSpecials(Vector2Int target, Vector2Int selected)
        {
            Piece a = At(selected), b = At(target);

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
                    // Bombă + bombă: zonă 5×5.
                    cells.AddRange(Square(target, BlastRules.BombComboSize));
                    step.Activations.Add(new Activation { Kind = TileKind.Bomb, Cell = target, Color = -1, Targets = new List<Vector2Int>(cells) });
                }
            }

            Detonate(step, cells, new HashSet<Vector2Int>(), new HashSet<Vector2Int> { selected, target });
            return step;
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
        /// <param name="kind">Ce piesă specială (rachetă orizontală/verticală sau bombă); null = rachetă aleatoare (finalul dulce).</param>
        public BoardStep MakeRandomSpecial(TileKind? kind = null)
        {
            var candidates = new List<Vector2Int>();
            foreach (Vector2Int c in ValidCells())
                if (At(c) is { CanGroup: true }) candidates.Add(c);
            if (candidates.Count == 0) return null;
            Vector2Int cell = candidates[_random.Next(candidates.Count)];
            Piece p = At(cell);
            p.Kind = kind == TileKind.Bomb ? TileKind.Bomb : _random.Next(2) == 0 ? TileKind.RocketH : TileKind.RocketV;
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
                Settle(steps, false, false);
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
                for (int attempt = 0; attempt < 300; attempt++)
                {
                    Shuffle(colors, _random);
                    // Dacă distribuția nu permite niciun schimb, schimbăm treptat câteva culori.
                    if (attempt > 10) colors[_random.Next(colors.Count)] = _random.Next(Config.Colors);
                    for (int i = 0; i < cells.Count; i++) At(cells[i]).Color = colors[i];
                    BreakMatches();
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
            if (p.Ice > 0)
            {
                p.Ice--;
                step.IceLayersBroken++;
            }
            else if (p.Chained)
            {
                p.Chained = false;
                step.ChainsBroken++;
            }
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

        /// <summary>
        /// Căderi, piese noi, ieșirea bomboanelor și cascadele (liniile formate de piesele căzute dispar și ele),
        /// până tabla se liniștește. Apoi ciocolata poate crește, iar dacă nu mai e niciun schimb posibil, amestecăm.
        /// </summary>
        /// <param name="createSpecials">False la finalul nivelului: cascadele nu mai fac piese speciale noi (finalul se termină).</param>
        private void Settle(List<BoardStep> steps, bool allowGrowth, bool createSpecials = true)
        {
            for (int cascade = 1; cascade < 50; cascade++)
            {
                for (int guard = 0; guard < 30; guard++)
                {
                    BoardStep fall = Gravity();
                    if (fall.Moves.Count > 0) steps.Add(fall);
                    BoardStep exit = Exits();
                    if (exit.Exits.Count == 0) break;
                    steps.Add(exit);
                }
                List<MatchGroup> groups = FindMatches();
                if (groups.Count == 0) break;
                steps.Add(ResolveMatches(groups, cascade, createSpecials: createSpecials));
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

        // ------------------------------------------------------------ Atacurile Boss-ului

        public const int MaxBossChocolate = 8;
        public const int MaxLockedPieces = 12;

        /// <summary>
        /// Atacul unui Boss (după ce acțiunea jucătorului s-a terminat): ciocolată, gheață sau lanțuri pe câteva
        /// bomboane obișnuite, sau amestecare. E limitat (niciodată tabla plină de obstacole) și, dacă după atac
        /// nu mai există mutări, tabla se amestecă — o luptă nu devine niciodată imposibilă.
        /// </summary>
        public List<BoardStep> ApplyBossAttack(BossAttack attack, int strength)
        {
            var steps = new List<BoardStep>();
            if (attack == BossAttack.Shuffle)
            {
                steps.Add(ShuffleStep());
                return steps;
            }

            int chocolate = 0, locked = 0;
            var candidates = new List<Vector2Int>();
            foreach (Vector2Int c in ValidCells())
            {
                Piece p = At(c);
                if (p == null) continue;
                if (p.Kind == TileKind.Chocolate) chocolate++;
                if (p.Locked) locked++;
                // Nu atacăm rândul de sus (de acolo intră piesele noi) și nici piesele speciale.
                if (p.Kind == TileKind.Normal && !p.Locked && c.y < Config.ColumnTop[c.x]) candidates.Add(c);
            }
            int room = attack == BossAttack.ChocolateSpread ? MaxBossChocolate - chocolate : MaxLockedPieces - locked;
            int count = Math.Max(0, Math.Min(strength, room));
            Shuffle(candidates, _random);

            var step = new BoardStep(StepKind.BossAttack);
            for (int i = 0; i < count && i < candidates.Count; i++)
            {
                Vector2Int c = candidates[i];
                Piece p = At(c);
                switch (attack)
                {
                    case BossAttack.ChocolateSpread: Set(p, TileKind.Chocolate, -1, 1); break;
                    case BossAttack.Freeze: p.Ice = strength >= 4 ? 2 : 1; break;
                    default: p.Chained = true; break;
                }
                step.Changed.Add(Info(p, c));
            }
            steps.Add(step);
            if (!HasValidMove()) steps.Add(ShuffleStep());
            return steps;
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

        /// <summary>Pentru teste: câte grupuri de linii de 3+ există acum pe tablă (după o mutare trebuie să fie 0).</summary>
        public int DebugMatchCount() => FindMatches().Count;

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
