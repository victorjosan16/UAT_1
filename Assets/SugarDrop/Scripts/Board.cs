using System;
using System.Collections;
using System.Collections.Generic;
using QuarryEmpire;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SugarDrop
{
    /// <summary>
    /// Tabla de capace colorate. Regula: schimbi două căsuțe vecine (sau un capac cu o gaură);
    /// dacă se formează un rând sau o coloană de 3+ capace de aceeași culoare, ele dispar și lasă GĂURI.
    /// Nu cad capace noi: prin găuri cad bomboanele de deasupra. Dacă schimbul nu formează nimic, se anulează.
    /// </summary>
    public class Board : MonoBehaviour
    {
        private static readonly Vector2Int[] Neighbours = { Vector2Int.right, Vector2Int.up };

        [SerializeField] private float cellSize = 0.95f;
        [SerializeField] private float swapDuration = 0.14f;
        [SerializeField] private float popDuration = 0.22f;
        [SerializeField] private float popStagger = 0.03f;

        /// <summary>O mutare validă a fost făcută (consumă o mutare).</summary>
        public event Action MoveMade;

        /// <summary>Capace eliminate: câte și centrul lor (pentru efecte).</summary>
        public event Action<int, Vector3> TilesCleared;

        public bool Busy { get; private set; }
        public float CellSize => cellSize;
        public int Columns { get; private set; }
        public int Rows { get; private set; }
        public float Width => Columns * cellSize;
        public float Top => _bottomCenter.y + Rows * cellSize;
        public float Bottom => _bottomCenter.y;

        private Tile[,] _grid;
        private Vector2 _bottomCenter;
        private int _colors;
        private Sprite _tileSprite;
        private Sprite _iconSprite;

        // ------------------------------------------------------------ Construire

        public void Build(int columns, int rows, int colors, Vector2 bottomCenter)
        {
            Clear();
            Columns = columns;
            Rows = rows;
            _colors = Mathf.Clamp(colors, 2, Palette.TileColorCount);
            _bottomCenter = bottomCenter;
            _grid = new Tile[columns, rows];
            _tileSprite ??= ProceduralSprites.RoundedRect;
            _iconSprite ??= ProceduralSprites.Circle;

            // Culori aleatoare, fără alinieri gata făcute la început.
            for (int x = 0; x < columns; x++)
            for (int y = 0; y < rows; y++)
            {
                int color;
                int guard = 0;
                do color = Random.Range(0, _colors);
                while (CreatesStartMatch(x, y, color) && ++guard < 20);
                _grid[x, y] = CreateTile(x, y, color);
            }

            EnsurePlayable(false);
        }

        public void Clear()
        {
            StopAllCoroutines();
            Busy = false;
            if (_grid != null)
                foreach (Tile tile in _grid)
                    if (tile != null) Destroy(tile.gameObject);
            _grid = null;
        }

        private bool CreatesStartMatch(int x, int y, int color)
        {
            bool horizontal = x >= 2 && _grid[x - 1, y]?.ColorIndex == color && _grid[x - 2, y]?.ColorIndex == color;
            bool vertical = y >= 2 && _grid[x, y - 1]?.ColorIndex == color && _grid[x, y - 2]?.ColorIndex == color;
            return horizontal || vertical;
        }

        private Tile CreateTile(int x, int y, int color)
        {
            var go = new GameObject($"Tile_{x}_{y}");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = CellCenter(new Vector2Int(x, y));
            var tile = go.AddComponent<Tile>();
            tile.Init(cellSize, color, _tileSprite, _iconSprite);
            tile.Cell = new Vector2Int(x, y);
            return tile;
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

        private bool InBounds(Vector2Int c) => c.x >= 0 && c.y >= 0 && c.x < Columns && c.y < Rows;
        private Tile At(Vector2Int c) => InBounds(c) ? _grid[c.x, c.y] : null;

        public int RemainingTiles
        {
            get
            {
                int count = 0;
                if (_grid != null)
                    foreach (Tile tile in _grid)
                        if (tile != null) count++;
                return count;
            }
        }

        // ------------------------------------------------------------ Schimb

        public void TrySwap(Vector2Int a, Vector2Int b)
        {
            if (Busy || _grid == null || !InBounds(a) || !InBounds(b)) return;
            if (At(a) == null && At(b) == null) return;
            StartCoroutine(SwapRoutine(a, b));
        }

        private IEnumerator SwapRoutine(Vector2Int a, Vector2Int b)
        {
            Busy = true;
            SwapCells(a, b);
            AnimateTo(a);
            AnimateTo(b);
            yield return new WaitForSeconds(swapDuration);

            HashSet<Vector2Int> matches = FindMatches();
            if (matches.Count == 0)
            {
                // Schimb fără aliniere: se întoarce înapoi și nu consumă mutarea.
                SwapCells(a, b);
                AnimateTo(a);
                AnimateTo(b);
                yield return new WaitForSeconds(swapDuration);
                Busy = false;
                yield break;
            }

            MoveMade?.Invoke();
            yield return ClearRoutine(matches);
            EnsurePlayable(true);
            Busy = false;
        }

        private IEnumerator ClearRoutine(HashSet<Vector2Int> cells)
        {
            Vector3 center = Vector3.zero;
            int index = 0;
            foreach (Vector2Int cell in cells)
            {
                Tile tile = At(cell);
                if (tile == null) continue;
                _grid[cell.x, cell.y] = null;
                center += tile.transform.position;
                GameObject go = tile.gameObject;
                // Dispariții ușor decalate, ca un „val”.
                StartCoroutine(PopLater(tile, index++ * popStagger, () => Destroy(go)));
            }
            if (index > 0) TilesCleared?.Invoke(index, center / index);
            yield return new WaitForSeconds(popDuration + index * popStagger);
        }

        private IEnumerator PopLater(Tile tile, float delay, Action done)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            if (tile != null) tile.Pop(popDuration, done);
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

        // ------------------------------------------------------------ Alinieri

        private HashSet<Vector2Int> FindMatches()
        {
            var result = new HashSet<Vector2Int>();
            for (int y = 0; y < Rows; y++) ScanLine(new Vector2Int(0, y), Vector2Int.right, Columns, result);
            for (int x = 0; x < Columns; x++) ScanLine(new Vector2Int(x, 0), Vector2Int.up, Rows, result);
            return result;
        }

        /// <summary>Caută pe o linie șiruri de 3+ capace de aceeași culoare (găurile întrerup șirul).</summary>
        private void ScanLine(Vector2Int start, Vector2Int step, int length, HashSet<Vector2Int> result)
        {
            int i = 0;
            while (i < length)
            {
                Tile first = At(start + step * i);
                if (first == null)
                {
                    i++;
                    continue;
                }
                int run = 1;
                while (i + run < length && At(start + step * (i + run))?.ColorIndex == first.ColorIndex) run++;
                if (run >= 3)
                    for (int k = 0; k < run; k++) result.Add(start + step * (i + k));
                i += run;
            }
        }

        private bool MatchAt(Vector2Int cell)
        {
            Tile tile = At(cell);
            if (tile == null) return false;
            return CountDirection(cell, Vector2Int.left, tile.ColorIndex) + CountDirection(cell, Vector2Int.right, tile.ColorIndex) >= 2
                   || CountDirection(cell, Vector2Int.down, tile.ColorIndex) + CountDirection(cell, Vector2Int.up, tile.ColorIndex) >= 2;
        }

        private int CountDirection(Vector2Int from, Vector2Int direction, int color)
        {
            int count = 0;
            Vector2Int p = from + direction;
            while (At(p)?.ColorIndex == color)
            {
                count++;
                p += direction;
            }
            return count;
        }

        /// <summary>Există măcar un schimb care formează o aliniere?</summary>
        public bool HasValidMove()
        {
            if (_grid == null) return false;
            for (int x = 0; x < Columns; x++)
            for (int y = 0; y < Rows; y++)
            {
                var a = new Vector2Int(x, y);
                foreach (Vector2Int direction in Neighbours)
                {
                    Vector2Int b = a + direction;
                    if (!InBounds(b) || (At(a) == null && At(b) == null)) continue;
                    SwapCells(a, b);
                    bool works = MatchAt(a) || MatchAt(b);
                    SwapCells(a, b);
                    if (works) return true;
                }
            }
            return false;
        }

        // ------------------------------------------------------------ Amestecare

        /// <summary>Dacă nu mai există mutări posibile, amestecă culorile capacelor rămase.</summary>
        private void EnsurePlayable(bool animate)
        {
            if (RemainingTiles < 3 || HasValidMove()) return;
            Shuffle(animate);
        }

        public void Shuffle(bool animate)
        {
            var tiles = new List<Tile>();
            foreach (Tile tile in _grid)
                if (tile != null) tiles.Add(tile);
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
                if (FindMatches().Count == 0 && HasValidMove()) break;
            }
            if (animate)
                foreach (Tile tile in tiles) tile.Bounce();
        }

        /// <summary>Ca să existe o mutare posibilă, trebuie măcar 3 capace de aceeași culoare.</summary>
        private static void EnsureThreeOfAColor(List<Tile> tiles)
        {
            var counts = new Dictionary<int, int>();
            int best = tiles[0].ColorIndex;
            foreach (Tile tile in tiles)
            {
                counts.TryGetValue(tile.ColorIndex, out int c);
                counts[tile.ColorIndex] = c + 1;
                if (counts[tile.ColorIndex] > (counts.TryGetValue(best, out int b) ? b : 0)) best = tile.ColorIndex;
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

        /// <summary>Ajutorul „bombă”: elimină capacele dintr-o zonă 3×3 (folosit din etapa următoare).</summary>
        public void Blast(Vector2Int center)
        {
            if (Busy || _grid == null) return;
            var cells = new HashSet<Vector2Int>();
            for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                var c = new Vector2Int(center.x + dx, center.y + dy);
                if (At(c) != null) cells.Add(c);
            }
            if (cells.Count > 0) StartCoroutine(BlastRoutine(cells));
        }

        private IEnumerator BlastRoutine(HashSet<Vector2Int> cells)
        {
            Busy = true;
            yield return ClearRoutine(cells);
            EnsurePlayable(true);
            Busy = false;
        }
    }
}
