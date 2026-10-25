using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SugarDrop
{
    /// <summary>
    /// Tabla din scenă: desenează celulele (cu glazură și ieșirile spre borcan) și piesele, iar mutările le trimite
    /// motorului de reguli (Match3Engine). Motorul calculează instant toți pașii mutării (eliminări, căderi, piese
    /// noi, cascade, bomboane care ies), iar tabla îi animă pe rând și anunță jocul prin evenimente.
    /// </summary>
    public class Board : MonoBehaviour
    {
        [SerializeField] private float cellSize = 0.92f;
        [SerializeField] private float swapDuration = 0.14f;
        [SerializeField] private float popDuration = 0.22f;
        [SerializeField] private float popStagger = 0.018f;
        [Tooltip("Cât durează căderea pe o celulă (secunde); căderile lungi sunt proporțional mai lungi, dar accelerate.")]
        [SerializeField] private float fallPerCell = 0.07f;

        // ------------------------------------------------------------ Evenimente (pentru joc, efecte și sunete)

        /// <summary>O mutare validă a fost făcută (consumă o mutare).</summary>
        public event Action MoveMade;
        /// <summary>Un schimb fără aliniere (se anulează, nu consumă mutarea).</summary>
        public event Action InvalidSwap;
        /// <summary>Un pas al mutării tocmai a început să fie animat (pentru scor și obiective).</summary>
        public event Action<BoardStep> StepPlayed;
        /// <summary>O piesă dispare: culoarea, tipul și poziția (efectul diferă după aromă).</summary>
        public event Action<int, TileKind, Vector3> TileCleared;
        public event Action<TileKind, Vector3, int, List<Vector3>> SpecialActivated;
        public event Action<TileKind, Vector3> SpecialCreated;
        public event Action<Vector3> IceBroken;
        /// <summary>Un obstacol (biscuit, ciocolată) a fost lovit (true = s-a spart).</summary>
        public event Action<Vector3, bool> BiscuitHit;
        public event Action<Vector3, int> FrostingMelted;
        /// <summary>O bomboană-ingredient a ieșit jos: poziția și culoarea (cade apoi în borcan).</summary>
        public event Action<Vector3, int> IngredientExited;
        public event Action<Vector3> ChocolateGrew;
        /// <summary>Sfârșitul unei mutări: câte piese au dispărut și unde (pentru puncte și laude).</summary>
        public event Action<int, Vector3> MoveResolved;
        public event Action Shuffled;

        public bool Busy { get; private set; }
        public bool Stuck => _engine != null && _engine.Stuck;
        public float CellSize => cellSize;
        public int Columns { get; private set; }
        public int Rows { get; private set; }
        public float Width => Columns * cellSize;
        public float Top => _bottomCenter.y + Rows * cellSize;
        public float Bottom => _bottomCenter.y;
        public int FrostingLeft => _engine != null ? _engine.FrostingLeft : 0;
        /// <summary>Cascada pasului animat acum (pentru multiplicatorul de puncte).</summary>
        public int CurrentCascade { get; private set; }

        private Match3Engine _engine;
        private readonly Dictionary<int, Tile> _tiles = new();
        private SpriteRenderer[,] _glaze;
        private Vector2 _bottomCenter;
        private Tile _selected;
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
            _engine = new Match3Engine(config, seed);
            BuildMask();
            BuildCells(config);
            foreach (PieceInfo piece in _engine.AllPieces()) CreateTile(piece, CellCenter(piece.Cell));
        }

        public void Clear()
        {
            StopAllCoroutines();
            Busy = false;
            // Toți copiii, inclusiv piesele scoase care încă își făceau animația de dispariție.
            foreach (Transform child in transform) Destroy(child.gameObject);
            _tiles.Clear();
            _glaze = null;
            _engine = null;
            _selected = null;
        }

        /// <summary>
        /// Masca tablei: piesele noi care intră de sus și bomboanele care ies jos se văd doar în interiorul tablei.
        /// </summary>
        private void BuildMask()
        {
            var go = new GameObject("Mask");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(_bottomCenter.x, _bottomCenter.y + Rows * cellSize * 0.5f, 0f);
            var mask = go.AddComponent<SpriteMask>();
            mask.sprite = SugarSprites.CellMask;
            Vector2 size = mask.sprite.bounds.size;
            float margin = cellSize * 0.35f;
            go.transform.localScale = new Vector3((Width + margin) / size.x, (Rows * cellSize + margin) / size.y, 1f);
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
            tile.Init(cellSize);
            tile.Id = piece.Id;
            tile.Cell = piece.Cell;
            tile.Setup(piece.Kind, piece.Color, piece.Frozen);
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

        // ------------------------------------------------------------ Selecție și indicii (doar vizual)

        public void Select(Vector2Int? cell)
        {
            Tile tile = cell.HasValue ? TileAt(cell.Value) : null;
            if (tile == _selected) return;
            if (_selected != null) _selected.SetSelected(false);
            _selected = tile;
            if (_selected != null) _selected.SetSelected(true);
        }

        public bool HasValidMove() => _engine != null && _engine.HasValidMove();

        /// <summary>Cea mai bună mutare pentru obiective (colorWeight: cât valorează fiecare culoare).</summary>
        public bool TryFindBestHint(Func<int, int> colorWeight, out Vector2Int a, out Vector2Int b)
        {
            a = b = default;
            return _engine != null && _engine.TryFindBestMove(colorWeight, out a, out b);
        }

        /// <summary>Tremură scurt piesele mutării sugerate.</summary>
        public void ShowHint(Func<int, int> colorWeight)
        {
            if (Busy || !TryFindBestHint(colorWeight, out Vector2Int a, out Vector2Int b)) return;
            TileAt(a)?.Wiggle();
            TileAt(b)?.Wiggle();
        }

        // ------------------------------------------------------------ Acțiuni

        public void TrySwap(Vector2Int a, Vector2Int b)
        {
            if (Busy || _engine == null || !_engine.IsValid(a)) return;
            List<BoardStep> steps = _engine.IsValid(b) ? _engine.Swap(a, b) : null;
            if (steps == null)
            {
                Tile ta = TileAt(a), tb = _engine.IsValid(b) ? TileAt(b) : null;
                if (ta != null && tb != null && _engine.TryGetPiece(a, out PieceInfo pa) && _engine.TryGetPiece(b, out PieceInfo pb)
                    && !pa.Frozen && !pb.Frozen && IsMovable(pa.Kind) && IsMovable(pb.Kind))
                    StartCoroutine(SwapBack(ta, tb));
                else
                {
                    ta?.Wiggle();
                    tb?.Wiggle();
                }
                InvalidSwap?.Invoke();
                return;
            }
            MoveMade?.Invoke();
            StartCoroutine(Play(steps));
        }

        private static bool IsMovable(TileKind kind) => kind != TileKind.Biscuit && kind != TileKind.Chocolate;

        private IEnumerator SwapBack(Tile a, Tile b)
        {
            Busy = true;
            Vector3 pa = a.transform.localPosition, pb = b.transform.localPosition;
            a.MoveTo(pb, swapDuration);
            b.MoveTo(pa, swapDuration);
            yield return new WaitForSeconds(swapDuration);
            a.MoveTo(pa, swapDuration);
            b.MoveTo(pb, swapDuration);
            yield return new WaitForSeconds(swapDuration);
            Busy = false;
        }

        /// <summary>Ajutorul „bombă”: explozie 3×3 (nu consumă mutarea).</summary>
        public bool Blast(Vector2Int center)
        {
            if (Busy || _engine == null || !_engine.IsValid(center)) return false;
            List<BoardStep> steps = _engine.Blast(center);
            if (steps == null) return false;
            StartCoroutine(Play(steps));
            return true;
        }

        /// <summary>Ajutorul „amestecă”.</summary>
        public bool ShuffleBooster()
        {
            if (Busy || _engine == null) return false;
            StartCoroutine(Play(new List<BoardStep> { _engine.ShuffleStep() }));
            return true;
        }

        /// <summary>Finalul dulce: o piesă devine capac dungat.</summary>
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
                tile.Setup(piece.Kind, piece.Color, piece.Frozen);
                tile.Bounce(0.3f);
            }
            StepPlayed?.Invoke(step);
            SpecialCreated?.Invoke(piece.Kind, position);
            return true;
        }

        /// <summary>Finalul dulce: activează următorul capac special.</summary>
        public bool DetonateNextSpecial()
        {
            if (Busy || _engine == null) return false;
            List<BoardStep> steps = _engine.DetonateNextSpecial();
            if (steps == null) return false;
            StartCoroutine(Play(steps));
            return true;
        }

        // ------------------------------------------------------------ Animarea pașilor

        private IEnumerator Play(List<BoardStep> steps)
        {
            Busy = true;
            _clearedThisMove = 0;
            _clearedCenter = Vector3.zero;
            foreach (BoardStep step in steps)
            {
                CurrentCascade = step.Cascade;
                StepPlayed?.Invoke(step);
                switch (step.Kind)
                {
                    case StepKind.Swap: yield return AnimateSwap(step); break;
                    case StepKind.Clear: yield return AnimateClear(step); break;
                    case StepKind.Fall: yield return AnimateFall(step); break;
                    case StepKind.Exit: yield return AnimateExit(step); break;
                    case StepKind.Shuffle: yield return AnimateShuffle(step); break;
                    case StepKind.Grow: yield return AnimateGrow(step); break;
                }
            }
            CurrentCascade = 0;
            if (_clearedThisMove > 0) MoveResolved?.Invoke(_clearedThisMove, _clearedCenter / _clearedThisMove);
            Busy = false;
        }

        private IEnumerator AnimateSwap(BoardStep step)
        {
            if (_tiles.TryGetValue(step.IdAtA, out Tile a))
            {
                a.Cell = step.A;
                a.MoveTo(CellCenter(step.A), swapDuration);
            }
            if (_tiles.TryGetValue(step.IdAtB, out Tile b))
            {
                b.Cell = step.B;
                b.MoveTo(CellCenter(step.B), swapDuration);
            }
            yield return new WaitForSeconds(swapDuration);
        }

        private IEnumerator AnimateClear(BoardStep step)
        {
            foreach (Activation activation in step.Activations)
            {
                var targets = new List<Vector3>(activation.Targets.Count);
                foreach (Vector2Int t in activation.Targets) targets.Add(World(t));
                SpecialActivated?.Invoke(activation.Kind, World(activation.Cell), activation.Color, targets);
            }

            int index = 0;
            foreach (PieceInfo piece in step.Cleared)
            {
                if (!_tiles.TryGetValue(piece.Id, out Tile tile)) continue;
                _tiles.Remove(piece.Id);
                Vector3 position = tile.transform.position;
                if (piece.Kind == TileKind.Biscuit || piece.Kind == TileKind.Chocolate) BiscuitHit?.Invoke(position, true);
                else
                {
                    TileCleared?.Invoke(piece.Color, piece.Kind, position);
                    _clearedThisMove++;
                    _clearedCenter += position;
                }
                if (tile == _selected) _selected = null;
                GameObject go = tile.gameObject;
                StartCoroutine(PopLater(tile, index++ * popStagger, () => Destroy(go)));
            }

            foreach (PieceInfo piece in step.Unfrozen)
                if (_tiles.TryGetValue(piece.Id, out Tile tile))
                {
                    tile.Unfreeze();
                    IceBroken?.Invoke(tile.transform.position);
                }
            foreach (PieceInfo piece in step.BlockerHits)
                if (_tiles.TryGetValue(piece.Id, out Tile tile))
                {
                    tile.ShowCrack();
                    BiscuitHit?.Invoke(tile.transform.position, false);
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

            // Capacele speciale noi (sau piese existente transformate) apar după ce dispar celelalte.
            foreach (PieceInfo piece in step.Changed)
            {
                if (!_tiles.TryGetValue(piece.Id, out Tile tile)) tile = CreateTile(piece, CellCenter(piece.Cell));
                else tile.Setup(piece.Kind, piece.Color, piece.Frozen);
                tile.Bounce(0.3f);
                SpecialCreated?.Invoke(piece.Kind, tile.transform.position);
            }

            yield return new WaitForSeconds(popDuration + Mathf.Min(index, 20) * popStagger);
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
                    // Piesele noi pornesc de deasupra tablei și apar treptat.
                    tile = CreateTile(move.Piece, CellCenter(move.From));
                    tile.FadeIn(0.15f);
                }
                else if (!_tiles.TryGetValue(move.Piece.Id, out tile)) continue;
                tile.Cell = move.To;
                float cells = Vector2.Distance(move.From, move.To);
                float duration = fallPerCell * Mathf.Sqrt(cells) * 2.2f;
                tile.MoveTo(CellCenter(move.To), duration, true);
                longest = Mathf.Max(longest, duration);
            }
            yield return new WaitForSeconds(longest + 0.04f);
        }

        private IEnumerator AnimateExit(BoardStep step)
        {
            foreach (PieceInfo piece in step.Exits)
            {
                if (!_tiles.TryGetValue(piece.Id, out Tile tile)) continue;
                _tiles.Remove(piece.Id);
                tile.MoveTo(CellCenter(new Vector2Int(piece.Cell.x, piece.Cell.y - 1)), 0.18f, true);
                IngredientExited?.Invoke(World(new Vector2Int(piece.Cell.x, piece.Cell.y - 1)), piece.Color);
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

        private IEnumerator AnimateGrow(BoardStep step)
        {
            foreach (PieceInfo piece in step.Changed)
                if (_tiles.TryGetValue(piece.Id, out Tile tile))
                {
                    tile.Setup(piece.Kind, piece.Color, piece.Frozen);
                    tile.Bounce(0.3f);
                    ChocolateGrew?.Invoke(tile.transform.position);
                }
            yield return new WaitForSeconds(0.25f);
        }
    }
}
