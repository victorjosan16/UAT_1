using System.Collections.Generic;
using UndergroundMetroTycoon.Simulation;
using UnityEngine;

namespace UndergroundMetroTycoon.Game
{
    /// <summary>
    /// Renders the authoritative <see cref="ExcavationGrid"/>: one block per cell, damage tint and hit punch,
    /// break animation, contract boundary tape, pit walls and the dig-target highlight.
    /// </summary>
    public sealed class ExcavationSiteView : MonoBehaviour
    {
        public const float PlotHalfSize = 5f;
        private const float PlotDepth = 3f;
        private const float PunchDuration = 0.16f;
        private const float BreakDuration = 0.14f;

        private sealed class CellVisual
        {
            public Transform Transform;
            public MeshRenderer Renderer;
            public Vector3 BaseScale;
            public Color BaseColor;
            public float Punch;
            public float Break;
        }

        private readonly List<CellVisual> _cells = new List<CellVisual>();
        private readonly List<int> _animating = new List<int>();
        private MaterialPropertyBlock _block;
        private MaterialLibrary _materials;
        private ExcavationGrid _grid;
        private Transform _cellRoot;
        private Transform _decorRoot;
        private Transform _highlight;
        private int _highlightIndex = -1;

        public ExcavationGrid Grid => _grid;

        public void Init(MaterialLibrary materials)
        {
            _materials = materials;
            _block = new MaterialPropertyBlock();
            _highlight = BuildHighlight();
        }

        public void Rebuild(ExcavationGrid grid)
        {
            _grid = grid;
            _cells.Clear();
            _animating.Clear();
            if (_cellRoot != null) Destroy(_cellRoot.gameObject);
            if (_decorRoot != null) Destroy(_decorRoot.gameObject);
            _cellRoot = PrimitiveFactory.Pivot("Cells", transform, Vector3.zero);
            _decorRoot = PrimitiveFactory.Pivot("SiteDecor", transform, Vector3.zero);
            SetHighlight(-1);
            if (grid == null) return;

            GridGeometry layout = grid.Layout;
            Vector3 cellSize = new Vector3(layout.CellSize * 0.97f, layout.LayerHeight * 0.98f, layout.CellSize * 0.97f);
            for (int i = 0; i < grid.CellCount; i++)
            {
                TerrainCell cell = grid.GetCell(i);
                Color baseColor = Palette.Terrain(cell.Material);
                if ((cell.Coord.X + cell.Coord.Z) % 2 == 1) baseColor = Palette.Shade(baseColor, 0.94f);
                if (!cell.IsContractCell) baseColor = Palette.Shade(baseColor, 0.82f);

                GameObject go = PrimitiveFactory.Box("Cell_" + cell.Id, _cellRoot, layout.CellCenter(cell.Coord).ToUnity(), cellSize,
                    _materials.Get(baseColor), cell.Coord.Layer == 0);
                var visual = new CellVisual
                {
                    Transform = go.transform,
                    Renderer = go.GetComponent<MeshRenderer>(),
                    BaseScale = cellSize,
                    BaseColor = baseColor
                };
                _cells.Add(visual);
                ApplyDamageTint(i);
                go.SetActive(!cell.IsExcavated);
            }

            BuildPitSurroundings(grid);
            BuildBoundaryTape(grid);
        }

        public void OnCellDamaged(TerrainCell cell)
        {
            if (cell.Index >= _cells.Count) return;
            ApplyDamageTint(cell.Index);
            if (cell.IsExcavated) return;
            _cells[cell.Index].Punch = PunchDuration;
            Track(cell.Index);
        }

        public void OnCellExcavated(TerrainCell cell)
        {
            if (cell.Index >= _cells.Count) return;
            _cells[cell.Index].Break = BreakDuration;
            Track(cell.Index);
            if (_highlightIndex == cell.Index) SetHighlight(-1);
        }

        public void SetHighlight(int cellIndex)
        {
            if (_highlight == null || _highlightIndex == cellIndex) return;
            _highlightIndex = cellIndex;
            TerrainCell cell = _grid?.GetCell(cellIndex);
            bool show = cell != null && !cell.IsExcavated;
            _highlight.gameObject.SetActive(show);
            if (show) _highlight.position = _grid.Layout.CellTopCenter(cell.Coord).ToUnity() + Vector3.up * 0.02f;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            for (int k = _animating.Count - 1; k >= 0; k--)
            {
                int index = _animating[k];
                CellVisual v = _cells[index];
                if (v.Break > 0f)
                {
                    v.Break -= dt;
                    float t = Mathf.Clamp01(v.Break / BreakDuration);
                    v.Transform.localScale = new Vector3(v.BaseScale.x * t, v.BaseScale.y * t, v.BaseScale.z * t);
                    if (v.Break <= 0f)
                    {
                        v.Transform.gameObject.SetActive(false);
                        v.Transform.localScale = v.BaseScale;
                        _animating.RemoveAt(k);
                    }
                    continue;
                }
                if (v.Punch > 0f)
                {
                    v.Punch -= dt;
                    float p = Mathf.Sin(Mathf.Clamp01(1f - v.Punch / PunchDuration) * Mathf.PI);
                    v.Transform.localScale = new Vector3(v.BaseScale.x * (1f - 0.07f * p), v.BaseScale.y * (1f - 0.18f * p), v.BaseScale.z * (1f - 0.07f * p));
                    if (v.Punch <= 0f)
                    {
                        v.Transform.localScale = v.BaseScale;
                        _animating.RemoveAt(k);
                    }
                    continue;
                }
                _animating.RemoveAt(k);
            }

            if (_highlight != null && _highlight.gameObject.activeSelf)
            {
                float pulse = 1f + Mathf.Sin(Time.time * 7f) * 0.04f;
                _highlight.localScale = new Vector3(pulse, 1f, pulse);
            }
        }

        private void Track(int index)
        {
            if (!_animating.Contains(index)) _animating.Add(index);
        }

        private void ApplyDamageTint(int index)
        {
            TerrainCell cell = _grid.GetCell(index);
            CellVisual v = _cells[index];
            float damage = 1f - cell.Health01;
            Color tint = Color.Lerp(v.BaseColor, Palette.Shade(v.BaseColor, 0.62f), damage);
            _block.Clear();
            MaterialLibrary.SetColor(_block, tint);
            v.Renderer.SetPropertyBlock(damage > 0f ? _block : null);
        }

        private Transform BuildHighlight()
        {
            Transform root = PrimitiveFactory.Pivot("DigHighlight", transform, Vector3.zero);
            Material yellow = _materials.Get(Palette.Yellow);
            const float s = 0.98f, w = 0.06f, h = 0.03f;
            PrimitiveFactory.Box("N", root, new Vector3(0f, 0f, s * 0.5f), new Vector3(s, h, w), yellow, false);
            PrimitiveFactory.Box("S", root, new Vector3(0f, 0f, -s * 0.5f), new Vector3(s, h, w), yellow, false);
            PrimitiveFactory.Box("E", root, new Vector3(s * 0.5f, 0f, 0f), new Vector3(w, h, s), yellow, false);
            PrimitiveFactory.Box("W", root, new Vector3(-s * 0.5f, 0f, 0f), new Vector3(w, h, s), yellow, false);
            root.gameObject.SetActive(false);
            return root;
        }

        /// <summary>Fills the dirt plot around the grid down to below the deepest layer, so pit walls read as solid soil.</summary>
        private void BuildPitSurroundings(ExcavationGrid grid)
        {
            GridGeometry l = grid.Layout;
            float minX = l.OriginX, maxX = l.OriginX + grid.Width * l.CellSize;
            float minZ = l.OriginZ, maxZ = l.OriginZ + grid.Length * l.CellSize;
            float centerX = (minX + maxX) * 0.5f, centerZ = (minZ + maxZ) * 0.5f;
            float plotMinX = centerX - PlotHalfSize, plotMaxX = centerX + PlotHalfSize;
            float plotMinZ = centerZ - PlotHalfSize, plotMaxZ = centerZ + PlotHalfSize;
            float top = l.SurfaceY;
            float bottom = top - PlotDepth;
            Material dirt = _materials.Get(Palette.DirtPlot);

            FillBox("PlotWest", plotMinX, minX, plotMinZ, plotMaxZ, bottom, top, dirt);
            FillBox("PlotEast", maxX, plotMaxX, plotMinZ, plotMaxZ, bottom, top, dirt);
            FillBox("PlotSouth", minX, maxX, plotMinZ, minZ, bottom, top, dirt);
            FillBox("PlotNorth", minX, maxX, maxZ, plotMaxZ, bottom, top, dirt);
            FillBox("PitFloor", minX, maxX, minZ, maxZ, bottom, grid.FloorY, _materials.Get(Palette.Bedrock));
        }

        private void FillBox(string name, float minX, float maxX, float minZ, float maxZ, float minY, float maxY, Material material)
        {
            if (maxX - minX <= 0.001f || maxZ - minZ <= 0.001f || maxY - minY <= 0.001f) return;
            PrimitiveFactory.Box(name, _decorRoot,
                new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, (minZ + maxZ) * 0.5f),
                new Vector3(maxX - minX, maxY - minY, maxZ - minZ), material, false);
        }

        /// <summary>Yellow/black tape and posts outlining the contract area (visual excavation boundary).</summary>
        private void BuildBoundaryTape(ExcavationGrid grid)
        {
            GridGeometry l = grid.Layout;
            const float margin = 0.35f;
            float minX = l.OriginX - margin, maxX = l.OriginX + grid.Width * l.CellSize + margin;
            float minZ = l.OriginZ - margin, maxZ = l.OriginZ + grid.Length * l.CellSize + margin;
            Material post = _materials.Get(Palette.Concrete);
            Material yellow = _materials.Get(Palette.Yellow);
            Material black = _materials.Get(Palette.NavyDeep);

            var corners = new[] { new Vector3(minX, 0f, minZ), new Vector3(maxX, 0f, minZ), new Vector3(maxX, 0f, maxZ), new Vector3(minX, 0f, maxZ) };
            for (int i = 0; i < 4; i++)
            {
                Vector3 a = corners[i];
                Vector3 b = corners[(i + 1) % 4];
                PrimitiveFactory.Create("Post", PrimitiveType.Cylinder, _decorRoot, a + Vector3.up * 0.35f, new Vector3(0.09f, 0.35f, 0.09f), post);
                float length = Vector3.Distance(a, b);
                int segments = Mathf.Max(2, Mathf.RoundToInt(length / 0.35f));
                Vector3 dir = (b - a) / segments;
                bool alongX = Mathf.Abs(b.x - a.x) > 0.01f;
                for (int s = 0; s < segments; s++)
                {
                    Vector3 center = a + dir * (s + 0.5f) + Vector3.up * 0.55f;
                    Vector3 size = alongX ? new Vector3(dir.magnitude, 0.07f, 0.015f) : new Vector3(0.015f, 0.07f, dir.magnitude);
                    PrimitiveFactory.Box("Tape", _decorRoot, center, size, s % 2 == 0 ? yellow : black, false);
                }
            }
        }
    }
}
