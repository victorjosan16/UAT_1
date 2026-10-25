using System;
using System.Collections.Generic;
using UnityEngine;

namespace SugarDrop
{
    /// <summary>
    /// Regulile tablei, fără nimic vizual (se poate testa în afara Unity):
    /// schimbul a două piese, alinierile și piesele speciale, căderea pieselor în golurile rămase, piese noi care
    /// intră de sus, cascade, bomboanele-ingredient care ies jos spre borcan, glazura de pe celule, biscuiții,
    /// gheața și ciocolata care crește. Fiecare acțiune întoarce lista de pași pe care tabla îi animă.
    /// </summary>
    public sealed class Match3Engine
    {
        private sealed class Piece
        {
            public int Id;
            public TileKind Kind;
            public int Color;
            public bool Frozen;
            public int Hits;

            public bool IsBlocker => Kind == TileKind.Biscuit || Kind == TileKind.Chocolate;
            public bool IsIngredient => Kind == TileKind.Ingredient;
            public bool IsSpecial => Kind == TileKind.StripedRow || Kind == TileKind.StripedColumn || Kind == TileKind.Wrapped || Kind == TileKind.Rainbow;
            public bool CanMatch => Color >= 0 && (Kind == TileKind.Normal || Kind == TileKind.StripedRow || Kind == TileKind.StripedColumn || Kind == TileKind.Wrapped);
            public bool CanMove => !Frozen && !IsBlocker;
            // Gheața e doar un înveliș: capacul înghețat nu se poate muta, dar cade odată cu celelalte.
            public bool Falls => !IsBlocker;
        }

        private const int MaxIngredientsOnBoard = 2;
        private const int MaxCascades = 60;

        private static readonly Vector2Int[] FourWay = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };
        private static readonly Vector2Int[] Forward = { Vector2Int.right, Vector2Int.up };

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
        private readonly Func<Vector2Int, int> _colorAt;
        private int _nextId = 1;
        private bool _chocolateHit;

        public Match3Engine(LevelConfig config, int pieceSeed)
        {
            Config = config;
            Columns = config.Columns;
            Rows = config.Rows;
            _grid = new Piece[Columns, Rows];
            _frosting = new int[Columns, Rows];
            _random = new System.Random(pieceSeed);
            _colorAt = MatchColor;
            var layout = new System.Random(config.Seed);

            for (int x = 0; x < Columns; x++)
            for (int y = 0; y < Rows; y++)
            {
                if (!IsValid(new Vector2Int(x, y))) continue;
                int color, tries = 0;
                do color = _random.Next(Config.Colors);
                while (CreatesStartMatch(x, y, color) && ++tries < 30);
                _grid[x, y] = NewPiece(TileKind.Normal, color);
            }

            List<Vector2Int> cells = ValidCells();
            Shuffle(cells, layout);
            int index = 0;

            // Glazura: pe celule obișnuite (nu sub obstacole).
            for (int i = 0; i < config.FrostingCells && index < cells.Count; i++, index++)
            {
                Vector2Int c = cells[index];
                _frosting[c.x, c.y] = i < config.DoubleFrosting ? 2 : 1;
                FrostingLeft += _frosting[c.x, c.y];
            }

            // Obstacolele: nu în rândul de sus al coloanei (de acolo intră piesele noi) și nu jos, la ieșiri.
            bool CanHoldBlocker(Vector2Int c) => _frosting[c.x, c.y] == 0 && c.y < Config.ColumnTop[c.x] && c.y > Config.ColumnBottom[c.x];
            int biscuits = 0, chocolate = 0;
            for (; index < cells.Count && (biscuits < config.Biscuits || chocolate < config.Chocolate); index++)
            {
                Vector2Int c = cells[index];
                if (!CanHoldBlocker(c)) continue;
                if (biscuits < config.Biscuits)
                {
                    SetPiece(c, TileKind.Biscuit, -1, config.BiscuitHits);
                    biscuits++;
                }
                else if (c.y < Rows / 2)
                {
                    SetPiece(c, TileKind.Chocolate, -1, 1);
                    chocolate++;
                }
            }
            int frozen = 0;
            for (; index < cells.Count && frozen < config.Frozen; index++)
            {
                Piece p = At(cells[index]);
                if (p == null || p.Kind != TileKind.Normal) continue;
                p.Frozen = true;
                frozen++;
            }

            // Primele bomboane-ingredient, sus.
            IngredientsToSpawn = config.Ingredients;
            for (int i = 0; i < Math.Min(MaxIngredientsOnBoard, config.Ingredients); i++)
            {
                int x = layout.Next(Columns);
                Vector2Int top = new(x, config.ColumnTop[x]);
                Piece p = At(top);
                if (p == null || p.Kind != TileKind.Normal || p.Frozen) continue;
                SetPiece(top, TileKind.Ingredient, _random.Next(6), 0);
                IngredientsToSpawn--;
                IngredientsOnBoard++;
            }

            // Obstacolele pot crea alinieri accidentale: le recolorăm, apoi ne asigurăm că există mutări.
            for (int guard = 0; guard < 50 && FindGroups().Count > 0; guard++)
                foreach (MatchGroup group in FindGroups())
                    foreach (Vector2Int c in group.Cells)
                        At(c).Color = _random.Next(Config.Colors);
            if (!HasValidMove()) ShuffleStep();
        }

        // ------------------------------------------------------------ Acces

        public bool IsValid(Vector2Int c) => c.x >= 0 && c.x < Columns && c.y >= 0 && c.y < Rows && Config.IsValidCell(c.x, c.y);
        public int FrostingAt(Vector2Int c) => IsValid(c) ? _frosting[c.x, c.y] : 0;

        public bool TryGetPiece(Vector2Int c, out PieceInfo info)
        {
            Piece p = IsValid(c) ? _grid[c.x, c.y] : null;
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
        private int MatchColor(Vector2Int c)
        {
            Piece p = At(c);
            return p != null && p.CanMatch ? p.Color : -1;
        }

        private static PieceInfo Info(Piece p, Vector2Int c) =>
            new() { Id = p.Id, Cell = c, Kind = p.Kind, Color = p.Color, Frozen = p.Frozen, Hits = p.Hits };

        private Piece NewPiece(TileKind kind, int color, int hits = 0) => new() { Id = _nextId++, Kind = kind, Color = color, Hits = hits };

        private void SetPiece(Vector2Int c, TileKind kind, int color, int hits)
        {
            Piece p = _grid[c.x, c.y];
            p.Kind = kind;
            p.Color = color;
            p.Hits = hits;
            p.Frozen = false;
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

        private bool CreatesStartMatch(int x, int y, int color)
        {
            bool horizontal = x >= 2 && MatchColor(new Vector2Int(x - 1, y)) == color && MatchColor(new Vector2Int(x - 2, y)) == color;
            bool vertical = y >= 2 && MatchColor(new Vector2Int(x, y - 1)) == color && MatchColor(new Vector2Int(x, y - 2)) == color;
            return horizontal || vertical;
        }

        /// <summary>Există alinieri nerezolvate pe tablă? (după o mutare ar trebui să fie mereu fals)</summary>
        public bool HasMatches() => FindGroups().Count > 0;

        private List<MatchGroup> FindGroups() => BoardRules.FindGroups(Columns, Rows, _colorAt);

        // ------------------------------------------------------------ Mutări

        public bool IsValidMove(Vector2Int a, Vector2Int b)
        {
            if (Math.Abs(a.x - b.x) + Math.Abs(a.y - b.y) != 1) return false;
            Piece pa = At(a), pb = At(b);
            if (pa == null || pb == null || !pa.CanMove || !pb.CanMove) return false;
            if (IsCombo(pa, pb)) return true;
            Exchange(a, b);
            bool works = BoardRules.IsMatchAt(a, _colorAt) || BoardRules.IsMatchAt(b, _colorAt);
            Exchange(a, b);
            return works;
        }

        private static bool IsCombo(Piece pa, Piece pb)
        {
            if (pa.Kind == TileKind.Rainbow && (pb.CanMatch || pb.Kind == TileKind.Rainbow)) return true;
            if (pb.Kind == TileKind.Rainbow && pa.CanMatch) return true;
            return pa.IsSpecial && pb.IsSpecial;
        }

        public bool HasValidMove() => TryFindMove(out _, out _);

        public bool TryFindMove(out Vector2Int a, out Vector2Int b)
        {
            a = b = default;
            for (int x = 0; x < Columns; x++)
            for (int y = 0; y < Rows; y++)
            foreach (Vector2Int direction in Forward)
            {
                var from = new Vector2Int(x, y);
                if (!IsValidMove(from, from + direction)) continue;
                a = from;
                b = from + direction;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Cea mai bună mutare pentru obiective: piese multe, glazură, culorile cerute, piese de sub bomboane.
        /// colorWeight(culoare) = cât valorează o piesă de acea culoare (pentru nivelurile „strânge culori”).
        /// </summary>
        public bool TryFindBestMove(Func<int, int> colorWeight, out Vector2Int a, out Vector2Int b)
        {
            a = b = default;
            int best = int.MinValue;
            for (int x = 0; x < Columns; x++)
            for (int y = 0; y < Rows; y++)
            foreach (Vector2Int direction in Forward)
            {
                var from = new Vector2Int(x, y);
                Vector2Int to = from + direction;
                if (!IsValidMove(from, to)) continue;
                int value = Estimate(from, to, colorWeight);
                if (value <= best) continue;
                best = value;
                a = from;
                b = to;
            }
            return best > int.MinValue;
        }

        private int Estimate(Vector2Int a, Vector2Int b, Func<int, int> colorWeight)
        {
            if (IsCombo(At(a), At(b))) return 60;
            Exchange(a, b);
            int value = 0;
            foreach (MatchGroup group in FindGroups())
            {
                if (BoardRules.SpecialFor(group) != TileKind.Normal) value += 8;
                foreach (Vector2Int c in group.Cells)
                {
                    value += 1 + _frosting[c.x, c.y] * 3 + (colorWeight != null ? colorWeight(group.Color) : 0);
                    if (IngredientAbove(c)) value += 6;
                    // Un obstacol vecin care ține pe loc o bomboană: merită spart.
                    foreach (Vector2Int d in FourWay)
                        if (At(c + d) is { IsBlocker: true } && IngredientAbove(c + d)) value += 10;
                }
            }
            Exchange(a, b);
            return value;
        }

        private bool IngredientAbove(Vector2Int c)
        {
            for (int y = c.y + 1; y < Rows; y++)
                if (At(new Vector2Int(c.x, y)) is { IsIngredient: true }) return true;
            return false;
        }

        private void Exchange(Vector2Int a, Vector2Int b) => (_grid[a.x, a.y], _grid[b.x, b.y]) = (_grid[b.x, b.y], _grid[a.x, a.y]);

        /// <summary>Schimbul jucătorului. Null dacă nu e o mutare validă (nu consumă mutarea).</summary>
        public List<BoardStep> Swap(Vector2Int a, Vector2Int b, bool allowGrowth = true)
        {
            if (!IsValidMove(a, b)) return null;
            var steps = new List<BoardStep>();
            Exchange(a, b);
            Piece pa = At(a), pb = At(b);
            steps.Add(new BoardStep(StepKind.Swap) { A = a, B = b, IdAtA = pa.Id, IdAtB = pb.Id });
            _chocolateHit = false;

            if (pa.Kind == TileKind.Rainbow || pb.Kind == TileKind.Rainbow)
            {
                bool aIsRainbow = pa.Kind == TileKind.Rainbow;
                Vector2Int rainbowCell = aIsRainbow ? a : b;
                Piece other = aIsRainbow ? pb : pa;
                steps.Add(RainbowCombo(rainbowCell, other));
            }
            else if (pa.IsSpecial && pb.IsSpecial)
            {
                steps.Add(Clear(new HashSet<Vector2Int> { a, b }, null, 0));
            }
            else
            {
                CollectGroups(FindGroups(), a, b, out HashSet<Vector2Int> cells, out var creates);
                steps.Add(Clear(cells, creates, 0));
            }
            Settle(steps, allowGrowth);
            return steps;
        }

        /// <summary>Curcubeu + o culoare: dispar toate piesele de acea culoare. Curcubeu + curcubeu: toată tabla.</summary>
        private BoardStep RainbowCombo(Vector2Int rainbowCell, Piece other)
        {
            bool everything = other.Kind == TileKind.Rainbow;
            var targets = new HashSet<Vector2Int>();
            foreach (Vector2Int c in ValidCells())
            {
                Piece p = At(c);
                if (p == null || c == rainbowCell) continue;
                if (everything ? !p.IsIngredient : p.CanMatch && p.Color == other.Color) targets.Add(c);
            }
            var step = new BoardStep(StepKind.Clear);
            Piece rainbow = At(rainbowCell);
            step.Activations.Add(new Activation { Kind = TileKind.Rainbow, Cell = rainbowCell, Color = everything ? -1 : other.Color, Targets = new List<Vector2Int>(targets) });
            Remove(rainbowCell, rainbow, step);
            ClearInto(step, targets, null);
            return step;
        }

        /// <summary>Ajutorul „bombă”: explozie 3×3 (fără să consume mutarea).</summary>
        public List<BoardStep> Blast(Vector2Int center)
        {
            var cells = new HashSet<Vector2Int>();
            for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                var c = new Vector2Int(center.x + dx, center.y + dy);
                if (IsValid(c)) cells.Add(c);
            }
            if (cells.Count == 0) return null;
            var steps = new List<BoardStep>();
            var step = new BoardStep(StepKind.Clear);
            step.Activations.Add(new Activation { Kind = TileKind.Wrapped, Cell = center, Color = -1, Targets = new List<Vector2Int>(cells) });
            ClearInto(step, cells, null);
            steps.Add(step);
            Settle(steps, false);
            return steps;
        }

        /// <summary>Finalul: transformă o piesă obișnuită, la întâmplare, într-una dungată.</summary>
        public BoardStep MakeRandomSpecial()
        {
            var candidates = new List<Vector2Int>();
            foreach (Vector2Int c in ValidCells())
                if (At(c) is { Kind: TileKind.Normal, Frozen: false }) candidates.Add(c);
            if (candidates.Count == 0) return null;
            Vector2Int cell = candidates[_random.Next(candidates.Count)];
            Piece p = At(cell);
            p.Kind = _random.Next(2) == 0 ? TileKind.StripedRow : TileKind.StripedColumn;
            var step = new BoardStep(StepKind.Clear);
            step.Changed.Add(Info(p, cell));
            return step;
        }

        /// <summary>Finalul: activează următorul capac special (de sus în jos). Null dacă nu mai e niciunul.</summary>
        public List<BoardStep> DetonateNextSpecial()
        {
            for (int y = Rows - 1; y >= 0; y--)
            for (int x = 0; x < Columns; x++)
            {
                var c = new Vector2Int(x, y);
                if (At(c) is not { IsSpecial: true, Frozen: false }) continue;
                var steps = new List<BoardStep> { Clear(new HashSet<Vector2Int> { c }, null, 0) };
                Settle(steps, false);
                return steps;
            }
            return null;
        }

        /// <summary>Amestecă culorile (ajutorul „amestecă” sau automat când nu mai sunt mutări).</summary>
        public BoardStep ShuffleStep()
        {
            var cells = new List<Vector2Int>();
            foreach (Vector2Int c in ValidCells())
                if (At(c) is { Kind: TileKind.Normal, Frozen: false }) cells.Add(c);
            var step = new BoardStep(StepKind.Shuffle);
            if (cells.Count < 3)
            {
                Stuck = !HasValidMove();
                return step;
            }
            var colors = new List<int>();
            foreach (Vector2Int c in cells) colors.Add(At(c).Color);
            for (int attempt = 0; attempt < 200; attempt++)
            {
                Shuffle(colors, _random);
                // Uneori schimbăm și o culoare, ca să nu rămânem blocați cu o distribuție imposibilă.
                if (attempt > 20) colors[_random.Next(colors.Count)] = _random.Next(Config.Colors);
                for (int i = 0; i < cells.Count; i++) At(cells[i]).Color = colors[i];
                if (FindGroups().Count == 0 && HasValidMove()) break;
            }
            foreach (Vector2Int c in cells) step.Changed.Add(Info(At(c), c));
            Stuck = !HasValidMove();
            return step;
        }

        // ------------------------------------------------------------ Rezolvare

        /// <summary>Căderi, piese noi, ieșirea bomboanelor și cascade, până tabla se liniștește.</summary>
        private void Settle(List<BoardStep> steps, bool allowGrowth)
        {
            for (int cascade = 1; cascade < MaxCascades; cascade++)
            {
                for (int guard = 0; guard < 20; guard++)
                {
                    BoardStep fall = Gravity();
                    if (fall.Moves.Count > 0) steps.Add(fall);
                    BoardStep exit = Exits();
                    if (exit.Exits.Count == 0) break;
                    steps.Add(exit);
                }
                List<MatchGroup> groups = FindGroups();
                if (groups.Count == 0) break;
                CollectGroups(groups, null, null, out HashSet<Vector2Int> cells, out var creates);
                steps.Add(Clear(cells, creates, cascade));
            }

            if (allowGrowth) GrowChocolate(steps);
            if (!HasValidMove()) steps.Add(ShuffleStep());
        }

        private void CollectGroups(List<MatchGroup> groups, Vector2Int? swapA, Vector2Int? swapB,
            out HashSet<Vector2Int> cells, out List<(Vector2Int cell, TileKind kind, int color)> creates)
        {
            cells = new HashSet<Vector2Int>();
            creates = new List<(Vector2Int, TileKind, int)>();
            foreach (MatchGroup group in groups)
            {
                TileKind special = BoardRules.SpecialFor(group);
                Vector2Int keep = new(-1, -1);
                if (special != TileKind.Normal)
                {
                    keep = PickSpecialCell(group, swapA, swapB);
                    if (keep.x >= 0)
                    {
                        creates.Add((keep, special, group.Color));
                        // Dacă locul ales era deja special, se activează și apoi apare noul capac.
                        if (At(keep).IsSpecial) cells.Add(keep);
                    }
                }
                foreach (Vector2Int c in group.Cells)
                    if (c != keep) cells.Add(c);
            }
        }

        private Vector2Int PickSpecialCell(MatchGroup group, Vector2Int? swapA, Vector2Int? swapB)
        {
            foreach (Vector2Int? preferred in new[] { swapA, swapB })
                if (preferred.HasValue && group.Cells.Contains(preferred.Value) && !At(preferred.Value).Frozen) return preferred.Value;
            foreach (MatchRun a in group.Runs)
            foreach (MatchRun b in group.Runs)
            {
                if (a.Horizontal == b.Horizontal) continue;
                foreach (Vector2Int c in a.Cells)
                    if (b.Cells.Contains(c) && !At(c).Frozen) return c;
            }
            MatchRun longest = group.Runs[0];
            foreach (MatchRun run in group.Runs)
                if (run.Cells.Count > longest.Cells.Count) longest = run;
            foreach (Vector2Int c in longest.Cells)
                if (!At(c).Frozen) return c;
            return new Vector2Int(-1, -1);
        }

        private BoardStep Clear(HashSet<Vector2Int> cells, List<(Vector2Int cell, TileKind kind, int color)> creates, int cascade)
        {
            var step = new BoardStep(StepKind.Clear, cascade);
            ClearInto(step, cells, creates);
            return step;
        }

        /// <summary>
        /// Elimină celulele date: piesele speciale lovite se activează (în lanț), gheața doar se sparge, glazura scade,
        /// biscuiții și ciocolata vecine sunt lovite, bomboanele-ingredient rămân. La final apar capacele speciale noi.
        /// </summary>
        private void ClearInto(BoardStep step, IEnumerable<Vector2Int> cells, List<(Vector2Int cell, TileKind kind, int color)> creates)
        {
            var queue = new Queue<Vector2Int>(cells);
            var processed = new HashSet<Vector2Int>();
            var blockers = new HashSet<Vector2Int>();
            var keep = new HashSet<Vector2Int>();
            var initial = new HashSet<Vector2Int>(cells);
            if (creates != null)
                foreach (var create in creates)
                    if (!initial.Contains(create.cell)) keep.Add(create.cell);

            while (queue.Count > 0)
            {
                Vector2Int c = queue.Dequeue();
                if (!IsValid(c) || !processed.Add(c) || keep.Contains(c)) continue;
                Piece p = At(c);
                if (p == null)
                {
                    HitFrosting(c, step);
                    continue;
                }
                if (p.IsIngredient) continue;
                if (p.IsBlocker)
                {
                    blockers.Add(c);
                    continue;
                }
                if (p.Frozen)
                {
                    p.Frozen = false;
                    step.Unfrozen.Add(Info(p, c));
                    continue;
                }
                if (p.IsSpecial)
                {
                    List<Vector2Int> targets = SpecialTargets(p, c);
                    step.Activations.Add(new Activation { Kind = p.Kind, Cell = c, Color = p.Color, Targets = targets });
                    foreach (Vector2Int t in targets)
                        if (!processed.Contains(t)) queue.Enqueue(t);
                }
                foreach (Vector2Int d in FourWay)
                    if (At(c + d) is { IsBlocker: true }) blockers.Add(c + d);
                Remove(c, p, step);
            }

            // Biscuiții și ciocolata: o singură lovitură pe eliminare (directă sau de la o aliniere vecină).
            foreach (Vector2Int c in blockers)
            {
                Piece p = At(c);
                if (p == null || !p.IsBlocker) continue;
                if (p.Kind == TileKind.Chocolate) _chocolateHit = true;
                p.Hits--;
                if (p.Hits <= 0) Remove(c, p, step);
                else step.BlockerHits.Add(Info(p, c));
            }

            if (creates == null) return;
            foreach (var (cell, kind, color) in creates)
            {
                Piece p = At(cell);
                if (p == null)
                {
                    p = NewPiece(kind, color);
                    _grid[cell.x, cell.y] = p;
                }
                else
                {
                    p.Kind = kind;
                    p.Color = kind == TileKind.Rainbow ? -1 : color;
                }
                if (kind == TileKind.Rainbow) p.Color = -1;
                step.Changed.Add(Info(p, cell));
            }
        }

        private void Remove(Vector2Int c, Piece p, BoardStep step)
        {
            _grid[c.x, c.y] = null;
            step.Cleared.Add(Info(p, c));
            if (!p.IsBlocker) HitFrosting(c, step);
        }

        private void HitFrosting(Vector2Int c, BoardStep step)
        {
            if (_frosting[c.x, c.y] <= 0) return;
            _frosting[c.x, c.y]--;
            FrostingLeft--;
            step.FrostingCells.Add(c);
            step.FrostingLeft.Add(_frosting[c.x, c.y]);
        }

        private List<Vector2Int> SpecialTargets(Piece p, Vector2Int c)
        {
            var targets = new List<Vector2Int>();
            switch (p.Kind)
            {
                case TileKind.StripedRow:
                    for (int x = 0; x < Columns; x++) targets.Add(new Vector2Int(x, c.y));
                    break;
                case TileKind.StripedColumn:
                    for (int y = 0; y < Rows; y++) targets.Add(new Vector2Int(c.x, y));
                    break;
                case TileKind.Wrapped:
                    for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                        targets.Add(new Vector2Int(c.x + dx, c.y + dy));
                    break;
                case TileKind.Rainbow:
                    int color = MostCommonColor();
                    foreach (Vector2Int cell in ValidCells())
                        if (MatchColor(cell) == color) targets.Add(cell);
                    break;
            }
            targets.RemoveAll(t => t == c || !IsValid(t));
            return targets;
        }

        private int MostCommonColor()
        {
            var counts = new int[Math.Max(1, Config.Colors)];
            int best = 0;
            foreach (Vector2Int c in ValidCells())
            {
                int color = MatchColor(c);
                if (color < 0 || color >= counts.Length) continue;
                counts[color]++;
                if (counts[color] > counts[best]) best = color;
            }
            return best;
        }

        // ------------------------------------------------------------ Gravitație

        /// <summary>
        /// Piesele cad în golurile de dedesubt; pe coloană, obstacolele (biscuit, ciocolată, gheață) opresc căderea.
        /// Golurile de sub obstacole se umplu pe diagonală din coloanele vecine. Piese noi intră pe sus.
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
                    Piece p = SpawnPiece();
                    _grid[cell.x, cell.y] = p;
                    index[p.Id] = step.Moves.Count;
                    step.Moves.Add(new PieceMove { Piece = Info(p, cell), From = new Vector2Int(x, top + 1 + i), To = cell, Spawned = true });
                    spawned = true;
                }
            }
            return spawned;
        }

        private Piece SpawnPiece()
        {
            // Bomboanele-ingredient apar treptat: mereu cel puțin una pe tablă cât mai sunt de adus.
            if (IngredientsToSpawn > 0 && IngredientsOnBoard < MaxIngredientsOnBoard && (IngredientsOnBoard == 0 || _random.NextDouble() < 0.12))
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
                    // Piesa alunecă doar dacă nu are ea însăși un gol direct dedesubt (acolo ar cădea vertical).
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

        /// <summary>Bomboanele ajunse jos (pe ultima celulă a coloanei) ies din tablă, spre borcan.</summary>
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

        /// <summary>Dacă mutarea n-a spart nicio ciocolată, ciocolata se întinde pe o piesă vecină.</summary>
        private void GrowChocolate(List<BoardStep> steps)
        {
            if (_chocolateHit) return;
            var options = new List<(Vector2Int from, Vector2Int to)>();
            foreach (Vector2Int c in ValidCells())
            {
                if (At(c) is not { Kind: TileKind.Chocolate }) continue;
                foreach (Vector2Int d in FourWay)
                    if (At(c + d) is { Kind: TileKind.Normal, Frozen: false }) options.Add((c, c + d));
            }
            if (options.Count == 0) return;
            Vector2Int target = options[_random.Next(options.Count)].to;
            Piece p = At(target);
            p.Kind = TileKind.Chocolate;
            p.Color = -1;
            p.Hits = 1;
            var step = new BoardStep(StepKind.Grow);
            step.Changed.Add(Info(p, target));
            steps.Add(step);
        }

        /// <summary>Verificare internă (pentru teste): nicio piesă în afara formei și niciun Id dublat.</summary>
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

        /// <summary>Celulele valide goale (ar trebui să fie doar sub obstacole, fără sursă).</summary>
        public int EmptyCells()
        {
            int count = 0;
            foreach (Vector2Int c in ValidCells())
                if (At(c) == null) count++;
            return count;
        }
    }
}
