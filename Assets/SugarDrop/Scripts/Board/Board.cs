using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SugarDrop
{
    /// <summary>
    /// Tabla de capace. Regula de bază: schimbi două căsuțe vecine (sau un capac cu o gaură); dacă se formează
    /// un rând/o coloană de 3+ capace de aceeași culoare, ele dispar și lasă GĂURI (nu cad capace noi).
    /// Alinieri mari creează capace speciale:
    ///   4 în linie → dungat (curăță rândul/coloana) · L sau T → ambalat (explozie 3×3) · 5 în linie → curcubeu.
    /// Obstacole: capace înghețate (nu se mută; prima aliniere sparge gheața) și biscuiți (se sparg de alinierile vecine).
    /// </summary>
    public class Board : MonoBehaviour
    {
        private static readonly Vector2Int[] Neighbours = { Vector2Int.right, Vector2Int.up };
        private static readonly Vector2Int[] FourWay = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };

        [SerializeField] private float cellSize = 0.95f;
        [SerializeField] private float swapDuration = 0.14f;
        [SerializeField] private float popDuration = 0.24f;
        [SerializeField] private float popStagger = 0.025f;

        // ------------------------------------------------------------ Evenimente (pentru joc, efecte și sunete)

        /// <summary>O mutare validă a fost făcută (consumă o mutare).</summary>
        public event Action MoveMade;
        /// <summary>Un schimb fără aliniere (se anulează).</summary>
        public event Action InvalidSwap;
        /// <summary>Un capac dispare: culoarea, tipul și poziția lui (efectul diferă după culoare).</summary>
        public event Action<int, TileKind, Vector3> TileCleared;
        /// <summary>Un capac special se activează: tipul, poziția, culoarea și celulele lovite.</summary>
        public event Action<TileKind, Vector3, int, List<Vector3>> SpecialActivated;
        /// <summary>S-a creat un capac special.</summary>
        public event Action<TileKind, Vector3> SpecialCreated;
        public event Action<Vector3> IceBroken;
        /// <summary>Un biscuit a fost lovit (true = s-a spart de tot).</summary>
        public event Action<Vector3, bool> BiscuitHit;
        /// <summary>Sfârșitul unei mutări: câte capace au dispărut în total (pentru mesaje „Super!”).</summary>
        public event Action<int, Vector3> MoveResolved;
        public event Action Shuffled;
        /// <summary>S-a deschis un drum liber de sus până jos (bomboanele pot ajunge în borcan).</summary>
        public event Action ChannelOpened;

        public bool Busy { get; private set; }
        /// <summary>Nu mai există nicio mutare posibilă (nici după amestecare): nivelul nu mai poate continua.</summary>
        public bool Stuck { get; private set; }
        public float CellSize => cellSize;
        public int Columns { get; private set; }
        public int Rows { get; private set; }
        public float Width => Columns * cellSize;
        public float Top => _bottomCenter.y + Rows * cellSize;
        public float Bottom => _bottomCenter.y;

        private Tile[,] _grid;
        private Func<Vector2Int, int> _matchColor;
        private Vector2 _bottomCenter;
        private int _colors;
        private int _clearedThisMove;
        private Vector3 _clearedCenter;

        // ------------------------------------------------------------ Construire

        public void Build(LevelConfig config, Vector2 bottomCenter)
        {
            _matchColor ??= MatchColor;
            Clear();
            Columns = config.Columns;
            Rows = config.Rows;
            _colors = Mathf.Clamp(config.Colors, 2, Palette.TileColorCount);
            _bottomCenter = bottomCenter;
            _grid = new Tile[Columns, Rows];

            // Biscuiții: în rândurile de sus (acolo blochează cel mai mult bomboanele), poziții aleatoare.
            var biscuitCells = new HashSet<Vector2Int>();
            int guard = 0;
            while (biscuitCells.Count < config.Biscuits && guard++ < 500)
            {
                int y = Rows - 1 - Random.Range(0, Mathf.Max(1, Rows / 2));
                biscuitCells.Add(new Vector2Int(Random.Range(0, Columns), y));
            }

            for (int x = 0; x < Columns; x++)
            for (int y = 0; y < Rows; y++)
            {
                var cell = new Vector2Int(x, y);
                if (biscuitCells.Contains(cell))
                {
                    _grid[x, y] = CreateTile(cell, TileKind.Biscuit, -1, false, config.BiscuitHits);
                    continue;
                }
                int color;
                int tries = 0;
                do color = Random.Range(0, _colors);
                while (CreatesStartMatch(x, y, color) && ++tries < 20);
                _grid[x, y] = CreateTile(cell, TileKind.Normal, color, false, 0);
            }

            // Gheața: pe capace normale, aleator.
            var normals = new List<Tile>();
            foreach (Tile tile in _grid)
                if (tile != null && tile.Kind == TileKind.Normal) normals.Add(tile);
            for (int i = 0; i < config.Frozen && normals.Count > 0; i++)
            {
                int index = Random.Range(0, normals.Count);
                Tile tile = normals[index];
                normals.RemoveAt(index);
                tile.Setup(TileKind.Normal, tile.ColorIndex, true);
            }

            EnsurePlayable(false);
            BuildCells();
            UpdateFlow(false);
        }

        public void Clear()
        {
            StopAllCoroutines();
            Busy = false;
            // Toți copiii, inclusiv capacele scoase din grilă care încă își făceau animația de dispariție.
            foreach (Transform child in transform) Destroy(child.gameObject);
            _grid = null;
        }

        private bool CreatesStartMatch(int x, int y, int color)
        {
            bool horizontal = x >= 2 && SameColor(_grid[x - 1, y], color) && SameColor(_grid[x - 2, y], color);
            bool vertical = y >= 2 && SameColor(_grid[x, y - 1], color) && SameColor(_grid[x, y - 2], color);
            return horizontal || vertical;
        }

        private static bool SameColor(Tile tile, int color) => tile != null && tile.CanMatch && tile.ColorIndex == color;

        private Tile CreateTile(Vector2Int cell, TileKind kind, int color, bool frozen, int hits)
        {
            var go = new GameObject($"Tile_{cell.x}_{cell.y}");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = CellCenter(cell);
            var tile = go.AddComponent<Tile>();
            tile.Init(cellSize);
            tile.Setup(kind, color, frozen, hits);
            tile.Cell = cell;
            return tile;
        }

        // ------------------------------------------------------------ Fundalul tablei și drumul bomboanelor

        private static readonly Color CellDark = new(0.4f, 0.19f, 0.47f, 0.45f);
        private static readonly Color CellDarkAlt = new(0.46f, 0.23f, 0.53f, 0.45f);
        private static readonly Color CellReached = new(1f, 0.82f, 0.92f, 0.55f);
        private static readonly Color CellChannel = new(1f, 0.86f, 0.42f, 0.6f);

        private SpriteRenderer[,] _cells;
        private bool[,] _empty;
        private bool[,] _reached;
        private bool _channelOpen;

        /// <summary>Drumul curent e deschis până jos?</summary>
        public bool ChannelOpen => _channelOpen;

        /// <summary>Pătrățele semi-transparente sub fiecare capac (tabla „în tablă”, ca la jocurile de cofetărie).</summary>
        private void BuildCells()
        {
            _cells = new SpriteRenderer[Columns, Rows];
            _empty = new bool[Columns, Rows];
            _reached = new bool[Columns, Rows];
            // Sprite alb: culoarea vine din UpdateFlow (o umbră neagră nu s-ar putea colora).
            Sprite sprite = SugarSprites.CellMask;
            float scale = cellSize * 0.97f / sprite.bounds.size.x;
            for (int x = 0; x < Columns; x++)
            for (int y = 0; y < Rows; y++)
            {
                var go = new GameObject($"Cell_{x}_{y}");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = CellCenter(new Vector2Int(x, y));
                go.transform.localScale = Vector3.one * scale;
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingOrder = 2;
                _cells[x, y] = renderer;
            }
        }

        /// <summary>
        /// Colorează golurile după unde pot curge bomboanele: roz = legat de grămadă, auriu = drum până jos.
        /// Golurile în care bomboanele nu pot ajunge rămân închise la culoare.
        /// </summary>
        private void UpdateFlow(bool notify)
        {
            if (_cells == null) return;
            for (int x = 0; x < Columns; x++)
            for (int y = 0; y < Rows; y++)
                _empty[x, y] = _grid[x, y] == null;
            ChannelRules.Reach(_empty, _reached, out bool open);
            for (int x = 0; x < Columns; x++)
            for (int y = 0; y < Rows; y++)
            {
                Color c = (x + y) % 2 == 0 ? CellDark : CellDarkAlt;
                if (_reached[x, y]) c = open ? CellChannel : CellReached;
                _cells[x, y].color = c;
            }
            bool opened = open && !_channelOpen;
            _channelOpen = open;
            if (opened && notify) ChannelOpened?.Invoke();
        }

        // ------------------------------------------------------------ Curgerea bomboanelor

        /// <summary>
        /// Pentru o bomboană care stă pe capace: centrul (x) celei mai apropiate găuri în care poate aluneca pe
        /// aceeași „podea”, fără capace în cale. Bomboanele de deasupra tablei stau pe rândul de sus.
        /// Fals dacă pe podeaua ei nu există nicio gaură accesibilă.
        /// </summary>
        public bool TryFindDropX(Vector2 world, out float targetX)
        {
            targetX = 0f;
            if (_grid == null || _empty == null) return false;
            float fx = (world.x - _bottomCenter.x) / cellSize + Columns * 0.5f;
            int column = Mathf.Clamp(Mathf.FloorToInt(fx), 0, Columns - 1);
            int row = Mathf.FloorToInt((world.y - _bottomCenter.y) / cellSize);
            int target = ChannelRules.FindDropColumn(_empty, column, row);
            if (target < 0) return false;
            targetX = CellCenter(new Vector2Int(target, 0)).x + transform.position.x;
            return true;
        }

        // ------------------------------------------------------------ Coordonate

        public Vector3 CellCenter(Vector2Int cell) => new(
            _bottomCenter.x + (cell.x - (Columns - 1) * 0.5f) * cellSize,
            _bottomCenter.y + (cell.y + 0.5f) * cellSize,
            0f);

        public bool TryGetCell(Vector2 world, out Vector2Int cell)
        {
            float fx = (world.x - _bottomCenter.x) / cellSize + Columns * 0.5f;
            float fy = (world.y - _bottomCenter.y) / cellSize;
            cell = new Vector2Int(Mathf.FloorToInt(fx), Mathf.FloorToInt(fy));
            return InBounds(cell);
        }

        private bool InBounds(Vector2Int c) => _grid != null && c.x >= 0 && c.y >= 0 && c.x < Columns && c.y < Rows;
        private Tile At(Vector2Int c) => InBounds(c) ? _grid[c.x, c.y] : null;

        /// <summary>Capacele care se mai pot alinia (fără biscuiți).</summary>
        public int RemainingTiles
        {
            get
            {
                int count = 0;
                if (_grid != null)
                    foreach (Tile tile in _grid)
                        if (tile != null && !tile.IsBiscuit) count++;
                return count;
            }
        }

        // ------------------------------------------------------------ Selecție (doar vizual)

        private Tile _selected;

        /// <summary>Evidențiază capacul de sub deget (null = niciunul). Nu schimbă regulile.</summary>
        public void Select(Vector2Int? cell)
        {
            Tile tile = cell.HasValue ? At(cell.Value) : null;
            if (tile == _selected) return;
            if (_selected != null) _selected.SetSelected(false);
            _selected = tile;
            if (_selected != null) _selected.SetSelected(true);
        }

        // ------------------------------------------------------------ Schimb

        public void TrySwap(Vector2Int a, Vector2Int b)
        {
            if (Busy || !InBounds(a) || !InBounds(b)) return;
            Tile ta = At(a), tb = At(b);
            if (ta == null && tb == null) return;
            // Capacele înghețate și biscuiții nu se mută.
            if ((ta != null && !ta.CanMove) || (tb != null && !tb.CanMove))
            {
                ta?.Wiggle();
                tb?.Wiggle();
                InvalidSwap?.Invoke();
                return;
            }
            StartCoroutine(SwapRoutine(a, b));
        }

        private IEnumerator SwapRoutine(Vector2Int a, Vector2Int b)
        {
            Busy = true;
            _clearedThisMove = 0;
            _clearedCenter = Vector3.zero;
            SwapCells(a, b);
            AnimateTo(a);
            AnimateTo(b);
            yield return new WaitForSeconds(swapDuration);

            Tile ta = At(a), tb = At(b);

            // Combinații speciale: curcubeu + orice capac, sau două capace speciale.
            if (ta != null && tb != null && (ta.IsRainbow || tb.IsRainbow))
            {
                MoveMade?.Invoke();
                yield return RainbowSwap(ta.IsRainbow ? ta : tb, ta.IsRainbow ? tb : ta);
                yield return FinishMove();
                yield break;
            }
            if (ta != null && tb != null && ta.IsSpecial && tb.IsSpecial)
            {
                MoveMade?.Invoke();
                yield return ClearRoutine(new HashSet<Vector2Int> { a, b }, null);
                yield return FinishMove();
                yield break;
            }

            List<MatchGroup> groups = FindGroups();
            if (groups.Count == 0)
            {
                // Schimb fără aliniere: se întoarce înapoi și nu consumă mutarea.
                SwapCells(a, b);
                AnimateTo(a);
                AnimateTo(b);
                InvalidSwap?.Invoke();
                yield return new WaitForSeconds(swapDuration);
                Busy = false;
                yield break;
            }

            MoveMade?.Invoke();
            yield return ResolveGroups(groups, a, b);
            yield return FinishMove();
        }

        private IEnumerator FinishMove()
        {
            UpdateFlow(true);
            if (_clearedThisMove > 0) MoveResolved?.Invoke(_clearedThisMove, _clearedCenter / _clearedThisMove);
            EnsurePlayable(true);
            Busy = false;
            yield break;
        }

        private void SwapCells(Vector2Int a, Vector2Int b)
        {
            Tile ta = _grid[a.x, a.y];
            Tile tb = _grid[b.x, b.y];
            _grid[a.x, a.y] = tb;
            _grid[b.x, b.y] = ta;
            if (tb != null) tb.Cell = a;
            if (ta != null) ta.Cell = b;
        }

        private void AnimateTo(Vector2Int cell)
        {
            Tile tile = At(cell);
            if (tile != null) tile.MoveTo(CellCenter(cell), swapDuration);
        }

        // ------------------------------------------------------------ Alinieri și capace speciale

        /// <summary>Culoarea pentru reguli: -1 dacă celula e goală sau capacul nu se poate alinia.</summary>
        private int MatchColor(Vector2Int cell)
        {
            Tile tile = At(cell);
            return tile != null && tile.CanMatch ? tile.ColorIndex : -1;
        }

        private List<MatchGroup> FindGroups() => BoardRules.FindGroups(Columns, Rows, _matchColor);

        private IEnumerator ResolveGroups(List<MatchGroup> groups, Vector2Int swapA, Vector2Int swapB)
        {
            var toClear = new HashSet<Vector2Int>();
            var toCreate = new List<(Vector2Int cell, TileKind kind, int color)>();

            foreach (MatchGroup group in groups)
            {
                TileKind special = BoardRules.SpecialFor(group);
                Vector2Int keep = new(-1, -1);
                if (special != TileKind.Normal)
                {
                    // Capacul special apare unde ai mutat (sau în mijlocul alinierii).
                    keep = PickSpecialCell(group, swapA, swapB);
                    if (keep.x >= 0) toCreate.Add((keep, special, group.Color));
                }
                foreach (Vector2Int cell in group.Cells)
                    if (cell != keep) toClear.Add(cell);
            }

            // Capacul care devine special: dacă era deja special, îl activăm înainte de transformare.
            foreach (var (cell, kind, color) in toCreate)
            {
                Tile tile = At(cell);
                if (tile == null) continue;
                if (tile.IsSpecial) toClear.Add(cell);
            }

            yield return ClearRoutine(toClear, toCreate);
        }

        private Vector2Int PickSpecialCell(MatchGroup group, Vector2Int swapA, Vector2Int swapB)
        {
            foreach (Vector2Int preferred in new[] { swapA, swapB })
                if (group.Cells.Contains(preferred) && At(preferred) != null && !At(preferred).Frozen) return preferred;
            // Celula comună a unui L/T, altfel mijlocul celui mai lung șir.
            foreach (MatchRun a in group.Runs)
            foreach (MatchRun b in group.Runs)
            {
                if (a.Horizontal == b.Horizontal) continue;
                foreach (Vector2Int cell in a.Cells)
                    if (b.Cells.Contains(cell) && !At(cell).Frozen) return cell;
            }
            MatchRun longest = group.Runs[0];
            foreach (MatchRun run in group.Runs)
                if (run.Cells.Count > longest.Cells.Count) longest = run;
            Vector2Int middle = longest.Cells[longest.Cells.Count / 2];
            return At(middle) != null && !At(middle).Frozen ? middle : new Vector2Int(-1, -1);
        }

        // ------------------------------------------------------------ Curățare (cu reacții în lanț)

        /// <summary>
        /// Elimină celulele date. Capacele speciale lovite se activează (și pot activa altele), gheața se sparge
        /// în loc ca acel capac să dispară, iar biscuiții vecini sunt loviți. La final se creează capacele speciale noi.
        /// </summary>
        private IEnumerator ClearRoutine(HashSet<Vector2Int> cells, List<(Vector2Int cell, TileKind kind, int color)> create)
        {
            var queue = new Queue<Vector2Int>(cells);
            var processed = new HashSet<Vector2Int>();
            var directBiscuitHits = new HashSet<Vector2Int>();
            var adjacentBiscuits = new HashSet<Vector2Int>();
            var protectedCells = new HashSet<Vector2Int>();
            if (create != null)
                foreach (var entry in create)
                    if (!cells.Contains(entry.cell)) protectedCells.Add(entry.cell);

            int popIndex = 0;
            while (queue.Count > 0)
            {
                Vector2Int cell = queue.Dequeue();
                if (!processed.Add(cell) || protectedCells.Contains(cell)) continue;
                Tile tile = At(cell);
                if (tile == null) continue;

                if (tile.IsBiscuit)
                {
                    directBiscuitHits.Add(cell);
                    continue;
                }
                if (tile.Frozen)
                {
                    tile.Unfreeze();
                    IceBroken?.Invoke(tile.transform.position);
                    continue;
                }

                if (tile.IsSpecial)
                {
                    List<Vector2Int> affected = SpecialTargets(tile, cell);
                    var positions = new List<Vector3>();
                    foreach (Vector2Int target in affected)
                    {
                        positions.Add(CellCenter(target) + transform.position);
                        if (!processed.Contains(target)) queue.Enqueue(target);
                    }
                    SpecialActivated?.Invoke(tile.Kind, tile.transform.position, tile.ColorIndex, positions);
                }

                foreach (Vector2Int direction in FourWay)
                {
                    Vector2Int neighbour = cell + direction;
                    if (At(neighbour) != null && At(neighbour).IsBiscuit) adjacentBiscuits.Add(neighbour);
                }

                RemoveTile(tile, cell, popIndex++);
            }

            // Biscuiții: o singură lovitură pe mutare, fie directă (explozie), fie de la o aliniere vecină.
            adjacentBiscuits.UnionWith(directBiscuitHits);
            foreach (Vector2Int cell in adjacentBiscuits)
            {
                Tile biscuit = At(cell);
                if (biscuit == null || !biscuit.IsBiscuit) continue;
                bool broken = biscuit.HitBiscuit();
                BiscuitHit?.Invoke(biscuit.transform.position, broken);
                if (broken) RemoveTile(biscuit, cell, popIndex++);
            }

            if (create != null)
                foreach (var (cell, kind, color) in create)
                {
                    Tile tile = At(cell);
                    if (tile == null) continue;
                    tile.Setup(kind, color);
                    tile.Bounce();
                    SpecialCreated?.Invoke(kind, tile.transform.position);
                }

            yield return new WaitForSeconds(popDuration + popIndex * popStagger);
        }

        private void RemoveTile(Tile tile, Vector2Int cell, int index)
        {
            _grid[cell.x, cell.y] = null;
            if (_empty != null) _empty[cell.x, cell.y] = true;
            _clearedThisMove++;
            _clearedCenter += tile.transform.position;
            TileCleared?.Invoke(tile.ColorIndex, tile.Kind, tile.transform.position);
            GameObject go = tile.gameObject;
            StartCoroutine(PopLater(tile, index * popStagger, () => Destroy(go)));
        }

        private IEnumerator PopLater(Tile tile, float delay, Action done)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            if (tile != null) tile.Pop(popDuration, done);
        }

        /// <summary>Celulele lovite de un capac special.</summary>
        private List<Vector2Int> SpecialTargets(Tile tile, Vector2Int cell)
        {
            var targets = new List<Vector2Int>();
            switch (tile.Kind)
            {
                case TileKind.StripedRow:
                    for (int x = 0; x < Columns; x++) targets.Add(new Vector2Int(x, cell.y));
                    break;
                case TileKind.StripedColumn:
                    for (int y = 0; y < Rows; y++) targets.Add(new Vector2Int(cell.x, y));
                    break;
                case TileKind.Wrapped:
                    for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        var c = new Vector2Int(cell.x + dx, cell.y + dy);
                        if (InBounds(c)) targets.Add(c);
                    }
                    break;
                case TileKind.Rainbow:
                    // Activat de altceva: curăță culoarea cea mai des întâlnită.
                    int color = MostCommonColor();
                    for (int x = 0; x < Columns; x++)
                    for (int y = 0; y < Rows; y++)
                        if (SameColor(_grid[x, y], color)) targets.Add(new Vector2Int(x, y));
                    break;
            }
            targets.Remove(cell);
            return targets;
        }

        private int MostCommonColor()
        {
            var counts = new int[Palette.TileColorCount];
            int best = 0;
            foreach (Tile tile in _grid)
            {
                if (tile == null || !tile.CanMatch) continue;
                counts[tile.ColorIndex]++;
                if (counts[tile.ColorIndex] > counts[best]) best = tile.ColorIndex;
            }
            return best;
        }

        /// <summary>Curcubeu schimbat cu un capac: curăță toate capacele de culoarea aceluia (curcubeu + curcubeu = toată tabla).</summary>
        private IEnumerator RainbowSwap(Tile rainbow, Tile other)
        {
            var targets = new HashSet<Vector2Int> { rainbow.Cell };
            bool everything = other.IsRainbow || other.IsBiscuit;
            int color = other.ColorIndex;
            var positions = new List<Vector3>();
            for (int x = 0; x < Columns; x++)
            for (int y = 0; y < Rows; y++)
            {
                Tile tile = _grid[x, y];
                if (tile == null || tile == rainbow) continue;
                if (everything ? !tile.IsBiscuit : tile.ColorIndex == color)
                {
                    targets.Add(new Vector2Int(x, y));
                    positions.Add(tile.transform.position);
                }
            }
            SpecialActivated?.Invoke(TileKind.Rainbow, rainbow.transform.position, color, positions);
            // Curcubeul se consumă aici (nu se mai activează încă o dată în lanț).
            Vector2Int rainbowCell = rainbow.Cell;
            targets.Remove(rainbowCell);
            RemoveTile(rainbow, rainbowCell, 0);
            yield return ClearRoutine(targets, null);
        }

        // ------------------------------------------------------------ Mutări posibile, indicii, amestecare

        private bool MatchAt(Vector2Int cell) => BoardRules.IsMatchAt(cell, _matchColor);

        private bool IsValidMove(Vector2Int a, Vector2Int b)
        {
            Tile ta = At(a), tb = At(b);
            if (ta == null && tb == null) return false;
            if ((ta != null && !ta.CanMove) || (tb != null && !tb.CanMove)) return false;
            if (ta != null && tb != null && (ta.IsRainbow || tb.IsRainbow)) return true;
            if (ta != null && tb != null && ta.IsSpecial && tb.IsSpecial) return true;
            SwapCells(a, b);
            bool works = MatchAt(a) || MatchAt(b);
            SwapCells(a, b);
            return works;
        }

        /// <summary>O mutare care formează o aliniere (pentru indicii și tutorial).</summary>
        public bool TryFindHint(out Vector2Int a, out Vector2Int b)
        {
            a = b = default;
            if (_grid == null) return false;
            for (int x = 0; x < Columns; x++)
            for (int y = 0; y < Rows; y++)
            {
                var from = new Vector2Int(x, y);
                foreach (Vector2Int direction in Neighbours)
                {
                    Vector2Int to = from + direction;
                    if (!InBounds(to) || !IsValidMove(from, to)) continue;
                    a = from;
                    b = to;
                    return true;
                }
            }
            return false;
        }

        public bool HasValidMove() => TryFindHint(out _, out _);

        /// <summary>
        /// Cea mai bună mutare pentru bomboane: cea care deschide (sau adâncește) drumul spre borcan.
        /// La egalitate, cea care elimină mai multe capace.
        /// </summary>
        public bool TryFindBestHint(out Vector2Int a, out Vector2Int b)
        {
            a = b = default;
            if (_grid == null) return false;
            int best = int.MinValue;
            var empty = new bool[Columns, Rows];
            for (int x = 0; x < Columns; x++)
            for (int y = 0; y < Rows; y++)
            {
                var from = new Vector2Int(x, y);
                foreach (Vector2Int direction in Neighbours)
                {
                    Vector2Int to = from + direction;
                    if (!InBounds(to) || !IsValidMove(from, to)) continue;
                    int value = EstimateMove(from, to, empty);
                    if (value <= best) continue;
                    best = value;
                    a = from;
                    b = to;
                }
            }
            return best > int.MinValue;
        }

        private int EstimateMove(Vector2Int a, Vector2Int b, bool[,] empty)
        {
            Tile ta = At(a), tb = At(b);
            // Combinațiile speciale curăță mult: le considerăm foarte bune.
            if (ta != null && tb != null && (ta.IsSpecial && tb.IsSpecial || ta.IsRainbow || tb.IsRainbow)) return 5000;

            // Simulăm schimbul: golurile de după mutare + capacele care ar dispărea.
            SwapCells(a, b);
            for (int x = 0; x < Columns; x++)
            for (int y = 0; y < Rows; y++)
                empty[x, y] = _grid[x, y] == null;
            int cleared = 0;
            foreach (MatchGroup group in FindGroups())
                foreach (Vector2Int cell in group.Cells)
                {
                    Tile tile = At(cell);
                    if (tile == null || tile.Frozen) continue; // gheața doar se sparge
                    empty[cell.x, cell.y] = true;
                    cleared++;
                }
            SwapCells(a, b);
            return ChannelRules.Score(empty) + cleared;
        }

        /// <summary>Tremură scurt piesele unei mutări posibile (indiciu).</summary>
        public void ShowHint()
        {
            if (Busy || !TryFindBestHint(out Vector2Int a, out Vector2Int b)) return;
            At(a)?.Wiggle();
            At(b)?.Wiggle();
        }

        private void EnsurePlayable(bool animate)
        {
            if (RemainingTiles >= 3 && !HasValidMove()) Shuffle(animate);
            Stuck = RemainingTiles < 3 || !HasValidMove();
        }

        /// <summary>Amestecă culorile capacelor care se pot mișca (păstrează capacele speciale, gheața și biscuiții).</summary>
        public void Shuffle(bool animate)
        {
            if (_grid == null) return;
            var tiles = new List<Tile>();
            foreach (Tile tile in _grid)
                if (tile != null && tile.CanMatch && !tile.Frozen) tiles.Add(tile);
            if (tiles.Count < 3) return;

            EnsureThreeOfAColor(tiles);
            var colors = new List<int>();
            foreach (Tile tile in tiles) colors.Add(tile.ColorIndex);

            for (int attempt = 0; attempt < 120; attempt++)
            {
                for (int i = colors.Count - 1; i > 0; i--)
                {
                    int j = Random.Range(0, i + 1);
                    (colors[i], colors[j]) = (colors[j], colors[i]);
                }
                for (int i = 0; i < tiles.Count; i++) tiles[i].SetColor(colors[i]);
                if (FindGroups().Count == 0 && HasValidMove()) break;
            }
            Stuck = !HasValidMove();
            if (!animate) return;
            foreach (Tile tile in tiles) tile.Bounce();
            Shuffled?.Invoke();
        }

        private static void EnsureThreeOfAColor(List<Tile> tiles)
        {
            var counts = new Dictionary<int, int>();
            int best = tiles[0].ColorIndex;
            foreach (Tile tile in tiles)
            {
                counts.TryGetValue(tile.ColorIndex, out int c);
                counts[tile.ColorIndex] = c + 1;
                if (counts[tile.ColorIndex] > counts[best]) best = tile.ColorIndex;
            }
            int missing = 3 - counts[best];
            foreach (Tile tile in tiles)
            {
                if (missing <= 0) break;
                if (tile.ColorIndex == best) continue;
                tile.SetColor(best);
                missing--;
            }
        }

        // ------------------------------------------------------------ Ajutoare

        // ------------------------------------------------------------ Finalul dulce (mutările rămase)

        /// <summary>Transformă un capac obișnuit, la întâmplare, într-unul dungat. Fals dacă nu mai există capace obișnuite.</summary>
        public bool TryMakeRandomSpecial(out Vector3 position)
        {
            position = default;
            if (_grid == null) return false;
            var candidates = new List<Tile>();
            foreach (Tile tile in _grid)
                if (tile != null && tile.Kind == TileKind.Normal && !tile.Frozen && tile.ColorIndex >= 0) candidates.Add(tile);
            if (candidates.Count == 0) return false;
            Tile chosen = candidates[Random.Range(0, candidates.Count)];
            TileKind kind = Random.value < 0.5f ? TileKind.StripedRow : TileKind.StripedColumn;
            chosen.Setup(kind, chosen.ColorIndex);
            chosen.Bounce();
            position = chosen.transform.position;
            SpecialCreated?.Invoke(kind, position);
            return true;
        }

        /// <summary>Activează următorul capac special de pe tablă. Fals dacă nu mai e niciunul (sau tabla e ocupată).</summary>
        public bool DetonateNextSpecial()
        {
            if (Busy || _grid == null) return false;
            for (int y = Rows - 1; y >= 0; y--)
            for (int x = 0; x < Columns; x++)
            {
                Tile tile = _grid[x, y];
                if (tile == null || !tile.IsSpecial || tile.Frozen) continue;
                StartCoroutine(DetonateRoutine(new Vector2Int(x, y)));
                return true;
            }
            return false;
        }

        private IEnumerator DetonateRoutine(Vector2Int cell)
        {
            Busy = true;
            _clearedThisMove = 0;
            _clearedCenter = Vector3.zero;
            yield return ClearRoutine(new HashSet<Vector2Int> { cell }, null);
            yield return FinishMove();
        }

        /// <summary>Ajutorul „bombă”: elimină o zonă 3×3 (sparge gheața, lovește biscuiții, activează speciale).</summary>
        public bool Blast(Vector2Int center)
        {
            if (Busy || !InBounds(center)) return false;
            var cells = new HashSet<Vector2Int>();
            for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                var c = new Vector2Int(center.x + dx, center.y + dy);
                if (At(c) != null) cells.Add(c);
            }
            if (cells.Count == 0) return false;
            StartCoroutine(BlastRoutine(cells, center));
            return true;
        }

        private IEnumerator BlastRoutine(HashSet<Vector2Int> cells, Vector2Int center)
        {
            Busy = true;
            _clearedThisMove = 0;
            _clearedCenter = Vector3.zero;
            var positions = new List<Vector3>();
            foreach (Vector2Int cell in cells) positions.Add(CellCenter(cell) + transform.position);
            SpecialActivated?.Invoke(TileKind.Wrapped, CellCenter(center) + transform.position, -1, positions);
            yield return ClearRoutine(cells, null);
            yield return FinishMove();
        }
    }
}
