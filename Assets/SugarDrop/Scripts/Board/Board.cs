using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SugarDrop
{
    /// <summary>
    /// Tabla din scenă: desenează celulele (cu glazură și ieșirile spre borcan) și piesele, iar schimburile le trimite
    /// motorului de reguli (BlastEngine). Motorul calculează instant toți pașii (schimb, eliminări, căderi, cascade,
    /// piese noi, bomboane care ies), iar tabla îi animă pe rând și anunță jocul prin evenimente.
    ///
    /// Starea tablei e una singură la un moment dat (liberă / piesă selectată / animează): cât animează,
    /// atingerile sunt ignorate, deci acțiunile nu se pot suprapune.
    ///
    /// Mutarea: tragi o piesă spre o vecină (sau atingi o piesă, apoi o vecină). Dacă schimbul nu face nicio linie,
    /// piesele se întorc la loc și mutarea nu se consumă.
    /// </summary>
    public class Board : MonoBehaviour
    {
        private enum Phase { Idle, Selected, Animating }

        [SerializeField] private float cellSize = 0.92f;
        [SerializeField] private float popDuration = 0.22f;
        [SerializeField] private float popStagger = 0.012f;
        [Tooltip("Cât durează căderea pe o celulă (secunde); căderile lungi sunt proporțional mai lungi, dar accelerate.")]
        [SerializeField] private float fallPerCell = 0.07f;
        [Tooltip("Decalajul dintre coloane la cădere (pornesc în val, nu toate deodată).")]
        [SerializeField] private float fallStagger = 0.012f;

        // ------------------------------------------------------------ Evenimente (pentru joc, efecte și sunete)

        /// <summary>O acțiune validă a jucătorului (consumă o mutare).</summary>
        public event Action MoveMade;
        /// <summary>Un schimb fără efect (nicio linie) sau o piesă care nu se poate muta: nu consumă mutarea.</summary>
        public event Action InvalidTap;
        /// <summary>Un pas tocmai a început să fie animat (pentru scor și obiective).</summary>
        public event Action<BoardStep> StepPlayed;
        /// <summary>Linii potrivite eliminate: câte bomboane, unde (lume) și culoarea (efectele cresc cu mărimea).</summary>
        public event Action<int, Vector3, int> GroupBlasted;
        /// <summary>O piesă dispare: culoarea, tipul și poziția (efectul diferă după aromă).</summary>
        public event Action<int, TileKind, Vector3> TileCleared;
        public event Action<TileKind, Vector3, int, List<Vector3>> SpecialActivated;
        public event Action<TileKind, Vector3> SpecialCreated;
        /// <summary>Două piese speciale combinate (pentru efecte mai mari).</summary>
        public event Action<Vector3> ComboTriggered;
        public event Action<Vector3> IceBroken;
        /// <summary>Un obstacol (ladă, ciocolată, lanț) a fost lovit (true = s-a spart).</summary>
        public event Action<Vector3, bool> BiscuitHit;
        public event Action<Vector3, int> FrostingMelted;
        /// <summary>O bomboană-ingredient a ieșit jos: poziția și culoarea (cade apoi în borcan).</summary>
        public event Action<Vector3, int> IngredientExited;
        public event Action<Vector3> ChocolateGrew;
        /// <summary>Sfârșitul unei acțiuni: câte piese au dispărut și unde (pentru puncte și laude).</summary>
        public event Action<int, Vector3> MoveResolved;
        public event Action Shuffled;
        /// <summary>Atacul Boss-ului a lovit o piesă (poziția și noua ei stare: ciocolată, gheață sau lanț).</summary>
        public event Action<Vector3, PieceInfo> BossAttackLanded;

        public bool Busy => _phase == Phase.Animating;
        public bool Stuck => _engine != null && _engine.Stuck;
        public float CellSize => cellSize;
        public int Columns { get; private set; }
        public int Rows { get; private set; }
        public float Width => Columns * cellSize;
        public float Top => _bottomCenter.y + Rows * cellSize;
        public float Bottom => _bottomCenter.y;
        public int FrostingLeft => _engine != null ? _engine.FrostingLeft : 0;

        [Tooltip("Masca tablei, pusă în scenă de setup: așa Unity păstrează componenta SpriteMask în build "
                 + "(altfel, adăugată doar din cod, e eliminată la „Strip Engine Code” și AddComponent întoarce null).")]
        [SerializeField] private SpriteMask boardMask;

        private BlastEngine _engine;
        private bool _masked;
        private readonly Dictionary<int, Tile> _tiles = new();
        private readonly List<Tile> _highlighted = new();
        private SpriteRenderer[,] _glaze;
        private Vector2 _bottomCenter;
        private Phase _phase;
        private Vector2Int _selected;
        private Tile _pressed;
        private int _clearedThisMove;
        private Vector3 _clearedCenter;

        private static readonly Color GlazeOne = new(1f, 1f, 1f, 0.9f);
        private static readonly Color GlazeTwo = new(1f, 0.72f, 0.86f, 0.95f);

        // ------------------------------------------------------------ Construire

        public void Build(LevelConfig config, Vector2 bottomCenter, int seed)
        {
            Clear();
            Columns = config.Columns;
            Rows = config.Rows;
            _bottomCenter = bottomCenter;
            _engine = new BlastEngine(config, seed);
            BuildMask();
            BuildCells(config);
            foreach (PieceInfo piece in _engine.AllPieces()) CreateTile(piece, CellCenter(piece.Cell));
        }

        public void Clear()
        {
            StopAllCoroutines();
            _phase = Phase.Idle;
            foreach (Transform child in transform) Destroy(child.gameObject);
            _tiles.Clear();
            _highlighted.Clear();
            _glaze = null;
            _engine = null;
            _pressed = null;
        }

        /// <summary>Masca tablei: piesele noi care intră de sus și bomboanele care ies jos se văd doar în interior.</summary>
        private void BuildMask()
        {
            SpriteMask mask = boardMask;
            if (mask == null)
            {
                var go = new GameObject("Mask");
                go.transform.SetParent(transform, false);
                mask = go.AddComponent<SpriteMask>();
            }
            // Fără mască (componenta lipsește din build) tabla merge oricum: piesele se văd și în afara ei.
            _masked = mask != null;
            if (!_masked)
            {
                Debug.LogWarning("[SugarDrop] SpriteMask lipsește din build; tabla merge fără mască.");
                return;
            }
            Sprite sprite = SugarSprites.CellMask;
            mask.sprite = sprite;
            Vector2 size = sprite.bounds.size;
            float margin = cellSize * 0.35f;
            Transform t = mask.transform;
            t.position = transform.TransformPoint(new Vector3(_bottomCenter.x, _bottomCenter.y + Rows * cellSize * 0.5f, 0f));
            t.localScale = new Vector3((Width + margin) / size.x, (Rows * cellSize + margin) / size.y, 1f);
        }

        /// <summary>Pătrățelele tablei (doar celulele valide), glazura și săgețile de ieșire spre borcan.</summary>
        private void BuildCells(LevelConfig config)
        {
            _glaze = new SpriteRenderer[Columns, Rows];
            Sprite cell = SugarSprites.CellMask;
            for (int x = 0; x < Columns; x++)
            for (int y = 0; y < Rows; y++)
            {
                var c = new Vector2Int(x, y);
                if (!_engine.IsValid(c)) continue;
                SpriteRenderer back = Sprite("Cell", cell, CellCenter(c), cellSize * 0.97f, 2);
                back.color = (x + y) % 2 == 0 ? new Color(0.4f, 0.19f, 0.47f, 0.5f) : new Color(0.47f, 0.24f, 0.55f, 0.5f);
                int layers = _engine.FrostingAt(c);
                if (layers > 0)
                {
                    _glaze[x, y] = Sprite("Glaze", SugarSprites.Glaze, CellCenter(c), cellSize * 0.97f, 3);
                    _glaze[x, y].color = layers >= 2 ? GlazeTwo : GlazeOne;
                }
            }

            if (config.Ingredients <= 0) return;
            for (int x = 0; x < Columns; x++)
            {
                Vector3 below = CellCenter(new Vector2Int(x, config.ColumnBottom[x])) + Vector3.down * cellSize * 0.62f;
                SpriteRenderer arrow = Sprite("Exit", SugarSprites.Chevron, below, cellSize * 0.42f, 4);
                arrow.color = new Color(1f, 0.85f, 0.4f, 0.95f);
            }
        }

        private SpriteRenderer Sprite(string name, Sprite sprite, Vector3 localPosition, float worldSize, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = Vector3.one * (worldSize / sprite.bounds.size.x);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            return renderer;
        }

        private Tile CreateTile(PieceInfo piece, Vector3 localPosition)
        {
            var go = new GameObject("Tile_" + piece.Id);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = localPosition;
            var tile = go.AddComponent<Tile>();
            tile.Init(cellSize, _masked);
            tile.Id = piece.Id;
            tile.Cell = piece.Cell;
            tile.Setup(piece);
            _tiles[piece.Id] = tile;
            return tile;
        }

        // ------------------------------------------------------------ Coordonate

        /// <summary>Centrul celulei (local); merge și pentru rânduri de deasupra tablei, de unde intră piesele noi.</summary>
        public Vector3 CellCenter(Vector2Int cell) => new(
            _bottomCenter.x + (cell.x - (Columns - 1) * 0.5f) * cellSize,
            _bottomCenter.y + (cell.y + 0.5f) * cellSize,
            0f);

        private Vector3 World(Vector2Int cell) => transform.TransformPoint(CellCenter(cell));

        public bool TryGetCell(Vector2 world, out Vector2Int cell)
        {
            Vector3 local = transform.InverseTransformPoint(world);
            float fx = (local.x - _bottomCenter.x) / cellSize + Columns * 0.5f;
            float fy = (local.y - _bottomCenter.y) / cellSize;
            cell = new Vector2Int(Mathf.FloorToInt(fx), Mathf.FloorToInt(fy));
            return _engine != null && _engine.IsValid(cell);
        }

        private Tile TileAt(Vector2Int cell) =>
            _engine != null && _engine.TryGetPiece(cell, out PieceInfo piece) && _tiles.TryGetValue(piece.Id, out Tile tile) ? tile : null;

        // ------------------------------------------------------------ Degetul pe tablă (doar vizual)

        /// <summary>Degetul apasă o celulă: piesa se ridică puțin (doar vizual).</summary>
        public void Press(Vector2Int? cell)
        {
            if (_pressed != null) _pressed.SetSelected(false);
            _pressed = null;
            if (!cell.HasValue || _engine == null || Busy) return;
            _pressed = TileAt(cell.Value);
            if (_pressed != null) _pressed.SetSelected(true);
        }

        private void Highlight(IEnumerable<Vector2Int> cells)
        {
            foreach (Vector2Int c in cells)
            {
                Tile tile = TileAt(c);
                if (tile == null) continue;
                tile.SetHighlighted(true);
                _highlighted.Add(tile);
            }
        }

        private void ClearHighlights()
        {
            foreach (Tile tile in _highlighted)
                if (tile != null) tile.SetHighlighted(false);
            _highlighted.Clear();
        }

        public bool HasValidMove() => _engine != null && _engine.HasValidMove();

        /// <summary>Cel mai bun schimb: piesa de mutat și celula vecină spre care se mută.</summary>
        public bool TryFindBestHint(Func<int, int> colorWeight, out Vector2Int from, out Vector2Int to)
        {
            from = to = default;
            return _engine != null && _engine.TryFindBestSwap(colorWeight, out from, out to);
        }

        /// <summary>Indiciu: cele două piese de schimbat tremură și strălucesc scurt.</summary>
        public void ShowHint(Func<int, int> colorWeight)
        {
            if (_phase != Phase.Idle || !TryFindBestHint(colorWeight, out Vector2Int from, out Vector2Int to)) return;
            TileAt(from)?.Wiggle();
            TileAt(to)?.Wiggle();
        }

        // ------------------------------------------------------------ Acțiuni

        /// <summary>
        /// Atingere simplă (fără tragere): prima selectează piesa, a doua pe o vecină face schimbul,
        /// pe aceeași piesă sau în altă parte anulează / mută selecția.
        /// </summary>
        public void Tap(Vector2Int cell)
        {
            if (_engine == null || _phase == Phase.Animating) return;
            if (_phase == Phase.Selected)
            {
                Vector2Int selected = _selected;
                CancelSelection();
                if (cell == selected) return;
                if (Mathf.Abs(cell.x - selected.x) + Mathf.Abs(cell.y - selected.y) == 1)
                {
                    Swap(selected, cell);
                    return;
                }
            }
            if (!_engine.TryGetPiece(cell, out PieceInfo piece) || piece.Ice > 0 || piece.Chained
                || piece.Kind == TileKind.Crate || piece.Kind == TileKind.Chocolate)
            {
                TileAt(cell)?.Wiggle();
                return;
            }
            _phase = Phase.Selected;
            _selected = cell;
            Highlight(new[] { cell });
            TileAt(cell)?.Bounce(0.2f);
        }

        /// <summary>Schimbă două piese vecine (tragere sau a doua atingere). Fără linie: se întorc la loc.</summary>
        public void Swap(Vector2Int a, Vector2Int b)
        {
            if (_engine == null || _phase == Phase.Animating) return;
            CancelSelection();
            if (!_engine.IsValid(a) || !_engine.IsValid(b)) return;
            List<BoardStep> steps = _engine.Swap(a, b);
            if (steps == null)
            {
                InvalidTap?.Invoke();
                StartCoroutine(SwapBack(a, b));
                return;
            }
            Run(steps, true);
        }

        public void CancelSelection()
        {
            if (_phase != Phase.Selected) return;
            _phase = Phase.Idle;
            ClearHighlights();
        }

        private void Run(List<BoardStep> steps, bool consumesMove)
        {
            if (steps == null) return;
            ClearHighlights();
            if (consumesMove) MoveMade?.Invoke();
            StartCoroutine(Play(steps));
        }

        /// <summary>Ajutorul „bombă”: explozie 3×3 (nu consumă mutarea).</summary>
        public bool Blast(Vector2Int center)
        {
            CancelSelection();
            if (_phase != Phase.Idle || _engine == null || !_engine.IsValid(center)) return false;
            List<BoardStep> steps = _engine.Blast(center);
            if (steps == null) return false;
            Run(steps, false);
            return true;
        }

        /// <summary>Ajutorul „amestecă”.</summary>
        public bool ShuffleBooster()
        {
            CancelSelection();
            if (_phase != Phase.Idle || _engine == null) return false;
            Run(new List<BoardStep> { _engine.ShuffleStep() }, false);
            return true;
        }

        /// <summary>Finalul dulce: o bomboană devine rachetă.</summary>
        public bool TryMakeRandomSpecial(out Vector3 position)
        {
            position = default;
            if (_engine == null) return false;
            BoardStep step = _engine.MakeRandomSpecial();
            if (step == null) return false;
            PieceInfo piece = step.Changed[0];
            position = World(piece.Cell);
            if (_tiles.TryGetValue(piece.Id, out Tile tile))
            {
                tile.Setup(piece);
                tile.Bounce(0.3f);
            }
            StepPlayed?.Invoke(step);
            SpecialCreated?.Invoke(piece.Kind, position);
            return true;
        }

        /// <summary>Finalul dulce: activează următoarea piesă specială.</summary>
        public bool DetonateNextSpecial()
        {
            CancelSelection();
            if (_phase != Phase.Idle || _engine == null) return false;
            List<BoardStep> steps = _engine.DetonateNextSpecial();
            if (steps == null) return false;
            Run(steps, false);
            return true;
        }

        /// <summary>
        /// Atacul Boss-ului, după ce mutarea jucătorului s-a terminat de tot (tabla e liniștită). Nu consumă mutări.
        /// </summary>
        public bool ApplyBossAttack(BossAttack attack, int strength)
        {
            CancelSelection();
            if (_phase != Phase.Idle || _engine == null) return false;
            Run(_engine.ApplyBossAttack(attack, strength), false);
            return true;
        }

        // ------------------------------------------------------------ Animarea pașilor

        private IEnumerator Play(List<BoardStep> steps)
        {
            _phase = Phase.Animating;
            _clearedThisMove = 0;
            _clearedCenter = Vector3.zero;
            foreach (BoardStep step in steps)
            {
                StepPlayed?.Invoke(step);
                switch (step.Kind)
                {
                    case StepKind.Clear: yield return AnimateClear(step); break;
                    case StepKind.Fall: yield return AnimateFall(step); break;
                    case StepKind.Exit: yield return AnimateExit(step); break;
                    case StepKind.Shuffle: yield return AnimateShuffle(step); break;
                    case StepKind.Grow: yield return AnimateGrow(step); break;
                    case StepKind.BossAttack: yield return AnimateBossAttack(step); break;
                    case StepKind.Swap: yield return AnimateSwap(step); break;
                }
            }
            if (_clearedThisMove > 0) MoveResolved?.Invoke(_clearedThisMove, _clearedCenter / _clearedThisMove);
            _phase = Phase.Idle;
        }

        private IEnumerator AnimateClear(BoardStep step)
        {
            if (step.GroupSize > 0) GroupBlasted?.Invoke(step.GroupSize, World(step.Origin), step.GroupColor);
            if (step.Combo) ComboTriggered?.Invoke(World(step.Origin));
            foreach (Activation activation in step.Activations)
            {
                var targets = new List<Vector3>(activation.Targets.Count);
                foreach (Vector2Int t in activation.Targets) targets.Add(World(t));
                SpecialActivated?.Invoke(activation.Kind, World(activation.Cell), activation.Color, targets);
            }

            // Piesele pocnesc în val, de la celula atinsă spre margini.
            Vector3 origin = World(step.Origin);
            foreach (PieceInfo piece in step.Cleared)
            {
                if (!_tiles.TryGetValue(piece.Id, out Tile tile)) continue;
                _tiles.Remove(piece.Id);
                Vector3 position = tile.transform.position;
                if (piece.Kind == TileKind.Crate || piece.Kind == TileKind.Chocolate) BiscuitHit?.Invoke(position, true);
                else
                {
                    TileCleared?.Invoke(piece.Color, piece.Kind, position);
                    _clearedThisMove++;
                    _clearedCenter += position;
                }
                if (tile == _pressed) _pressed = null;
                GameObject go = tile.gameObject;
                float delay = Vector3.Distance(position, origin) / cellSize * popStagger * 3f;
                StartCoroutine(PopLater(tile, Mathf.Min(delay, 0.25f), () => Destroy(go)));
            }

            foreach (PieceInfo piece in step.ObstacleHits)
            {
                if (!_tiles.TryGetValue(piece.Id, out Tile tile)) continue;
                bool wasIce = tile.Ice > 0;
                bool wasChain = tile.Chained;
                tile.Setup(piece, piece.Kind == TileKind.Crate);
                tile.Wiggle();
                if (wasIce) IceBroken?.Invoke(tile.transform.position);
                else BiscuitHit?.Invoke(tile.transform.position, wasChain);
            }
            for (int i = 0; i < step.FrostingCells.Count; i++)
            {
                Vector2Int c = step.FrostingCells[i];
                int left = step.FrostingLeft[i];
                SpriteRenderer glaze = _glaze[c.x, c.y];
                if (glaze != null)
                {
                    if (left <= 0) glaze.enabled = false;
                    else glaze.color = left >= 2 ? GlazeTwo : GlazeOne;
                }
                FrostingMelted?.Invoke(World(c), left);
            }

            // Piesele speciale noi apar după ce dispare grupul; cele transformate (curcubeu + …) se schimbă pe loc.
            foreach (PieceInfo piece in step.Changed)
            {
                if (!_tiles.TryGetValue(piece.Id, out Tile tile)) tile = CreateTile(piece, CellCenter(piece.Cell));
                else tile.Setup(piece);
                tile.Bounce(0.35f);
                SpecialCreated?.Invoke(piece.Kind, tile.transform.position);
            }

            yield return new WaitForSeconds(popDuration + 0.06f);
        }

        private IEnumerator PopLater(Tile tile, float delay, Action done)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            if (tile != null) tile.Pop(popDuration, done);
        }

        private IEnumerator AnimateFall(BoardStep step)
        {
            float longest = 0f;
            foreach (PieceMove move in step.Moves)
            {
                Tile tile;
                if (move.Spawned)
                {
                    // Piesele noi pornesc de deasupra tablei (ascunse de mască) și apar treptat.
                    tile = CreateTile(move.Piece, CellCenter(move.From));
                    tile.FadeIn(0.15f);
                }
                else if (!_tiles.TryGetValue(move.Piece.Id, out tile)) continue;
                tile.Cell = move.To;
                float cells = Vector2.Distance(move.From, move.To);
                // Căderea: accelerată (rădăcină din distanță), în val de la stânga la dreapta și de jos în sus.
                float duration = fallPerCell * Mathf.Sqrt(cells) * 2.2f;
                float delay = move.To.x * fallStagger + move.To.y * fallStagger * 0.5f;
                tile.MoveTo(CellCenter(move.To), duration, true, delay);
                longest = Mathf.Max(longest, duration + delay);
            }
            yield return new WaitForSeconds(longest + 0.05f);
        }

        private IEnumerator AnimateExit(BoardStep step)
        {
            foreach (PieceInfo piece in step.Exits)
            {
                if (!_tiles.TryGetValue(piece.Id, out Tile tile)) continue;
                _tiles.Remove(piece.Id);
                Vector2Int below = new(piece.Cell.x, piece.Cell.y - 1);
                tile.MoveTo(CellCenter(below), 0.18f, true);
                IngredientExited?.Invoke(World(below), piece.Color);
                GameObject go = tile.gameObject;
                StartCoroutine(PopLater(tile, 0.12f, () => Destroy(go)));
            }
            yield return new WaitForSeconds(0.2f);
        }

        private IEnumerator AnimateShuffle(BoardStep step)
        {
            foreach (PieceInfo piece in step.Changed)
                if (_tiles.TryGetValue(piece.Id, out Tile tile))
                {
                    tile.SetColor(piece.Color);
                    tile.Bounce(0.25f);
                }
            Shuffled?.Invoke();
            yield return new WaitForSeconds(0.35f);
        }

        private const float SwapDuration = 0.16f;

        private IEnumerator AnimateSwap(BoardStep step)
        {
            foreach (PieceMove move in step.Moves)
            {
                if (!_tiles.TryGetValue(move.Piece.Id, out Tile tile)) continue;
                tile.Cell = move.To;
                tile.MoveTo(CellCenter(move.To), SwapDuration);
            }
            yield return new WaitForSeconds(SwapDuration + 0.02f);
        }

        /// <summary>Schimb fără linie: piesele pornesc una spre alta și se întorc la loc.</summary>
        private IEnumerator SwapBack(Vector2Int a, Vector2Int b)
        {
            Tile ta = TileAt(a), tb = TileAt(b);
            if (ta == null || tb == null)
            {
                ta?.Wiggle();
                yield break;
            }
            _phase = Phase.Animating;
            ta.MoveTo(CellCenter(b), SwapDuration);
            tb.MoveTo(CellCenter(a), SwapDuration);
            yield return new WaitForSeconds(SwapDuration + 0.03f);
            if (ta != null) ta.MoveTo(CellCenter(a), SwapDuration);
            if (tb != null) tb.MoveTo(CellCenter(b), SwapDuration);
            yield return new WaitForSeconds(SwapDuration + 0.02f);
            _phase = Phase.Idle;
        }

        /// <summary>Atacul lovește piesele pe rând (ca o ploaie), fiecare tremură când se schimbă.</summary>
        private IEnumerator AnimateBossAttack(BoardStep step)
        {
            const float gap = 0.08f;
            foreach (PieceInfo piece in step.Changed)
            {
                if (!_tiles.TryGetValue(piece.Id, out Tile tile)) continue;
                tile.Setup(piece);
                tile.Wiggle();
                BossAttackLanded?.Invoke(tile.transform.position, piece);
                yield return new WaitForSeconds(gap);
            }
            yield return new WaitForSeconds(0.3f);
        }

        private IEnumerator AnimateGrow(BoardStep step)
        {
            foreach (PieceInfo piece in step.Changed)
                if (_tiles.TryGetValue(piece.Id, out Tile tile))
                {
                    tile.Setup(piece);
                    tile.Bounce(0.3f);
                    ChocolateGrew?.Invoke(tile.transform.position);
                }
            yield return new WaitForSeconds(0.25f);
        }
    }
}
