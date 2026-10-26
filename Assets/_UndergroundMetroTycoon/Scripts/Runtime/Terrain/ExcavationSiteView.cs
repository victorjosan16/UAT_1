using System.Collections.Generic;
using UndergroundMetroTycoon.Simulation;
using UnityEngine;

namespace UndergroundMetroTycoon.Game
{
    /// <summary>
    /// Renders the authoritative <see cref="ExcavationGrid"/>: bevelled two-tone blocks with small deterministic
    /// variation, pebbles, progressive cracks and damage tint, hit punch, break animation, stratified pit walls,
    /// boundary tape with flags, and the dig-target highlight.
    /// </summary>
    public sealed class ExcavationSiteView : MonoBehaviour
    {
        public const float PlotHalfSize = 5f;
        private const float PlotDepth = 3f;
        private const float PunchDuration = 0.16f;
        private const float BreakDuration = 0.16f;

        private sealed class CellVisual
        {
            public Transform Transform;
            public MeshRenderer Renderer;
            public Vector3 BaseScale;
            public Vector3 BasePosition;
            public Color TopColor;
            public Color SideColor;
            public GameObject Cracks;
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
            _highlightIndex = int.MinValue;
            SetHighlight(-1);
            if (grid == null) return;

            GridGeometry layout = grid.Layout;
            Mesh blockMesh = TerrainBlockMesh.Get();
            for (int i = 0; i < grid.CellCount; i++)
            {
                TerrainCell cell = grid.GetCell(i);
                int hash = Hash(cell.Coord.X, cell.Coord.Layer, cell.Coord.Z);

                // Three subtle tint variants keep the field from looking like a flat checkerboard.
                float variant = 0.94f + (hash % 3) * 0.03f;
                Color top = Palette.Shade(Palette.TerrainTop(cell.Material), variant);
                Color side = Palette.Shade(Palette.TerrainSide(cell.Material), variant);
                if (!cell.IsContractCell)
                {
                    top = Palette.Shade(top, 0.86f);
                    side = Palette.Shade(side, 0.86f);
                }

                float heightJitter = ((hash >> 3) % 5 - 2) * 0.008f;
                Vector3 scale = new Vector3(layout.CellSize * 0.985f, layout.LayerHeight + heightJitter, layout.CellSize * 0.985f);
                Vector3 position = layout.CellCenter(cell.Coord).ToUnity() + new Vector3(0f, heightJitter * 0.5f, 0f);

                var go = new GameObject("Cell_" + cell.Id);
                go.transform.SetParent(_cellRoot, false);
                go.transform.localPosition = position;
                go.transform.localScale = scale;
                go.AddComponent<MeshFilter>().sharedMesh = blockMesh;
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterials = new[] { _materials.Get(top), _materials.Get(side) };
                renderer.shadowCastingMode = cell.Coord.Layer == 0 ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;

                if (cell.Coord.Layer == 0 && cell.IsContractCell && hash % 10 < 3) AddPebbles(go.transform, hash, top);

                var visual = new CellVisual
                {
                    Transform = go.transform,
                    Renderer = renderer,
                    BaseScale = scale,
                    BasePosition = position,
                    TopColor = top,
                    SideColor = side
                };
                _cells.Add(visual);
                ApplyDamage(i);
                go.SetActive(!cell.IsExcavated);
            }

            BuildPitSurroundings(grid);
            BuildBoundary(grid);
        }

        public void OnCellDamaged(TerrainCell cell)
        {
            if (cell.Index >= _cells.Count) return;
            ApplyDamage(cell.Index);
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
            if (show) _highlight.position = _grid.Layout.CellTopCenter(cell.Coord).ToUnity() + Vector3.up * 0.03f;
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
                    // Pop up slightly, then shrink away.
                    float pop = Mathf.Sin((1f - t) * Mathf.PI) * 0.08f;
                    v.Transform.localScale = v.BaseScale * Mathf.Lerp(0.05f, 1.06f, t);
                    v.Transform.localPosition = v.BasePosition + Vector3.up * pop;
                    if (v.Break <= 0f)
                    {
                        v.Transform.gameObject.SetActive(false);
                        v.Transform.localScale = v.BaseScale;
                        v.Transform.localPosition = v.BasePosition;
                        _animating.RemoveAt(k);
                    }
                    continue;
                }
                if (v.Punch > 0f)
                {
                    v.Punch -= dt;
                    float p = Mathf.Sin(Mathf.Clamp01(1f - v.Punch / PunchDuration) * Mathf.PI);
                    v.Transform.localScale = new Vector3(v.BaseScale.x * (1f + 0.04f * p), v.BaseScale.y * (1f - 0.2f * p), v.BaseScale.z * (1f + 0.04f * p));
                    v.Transform.localPosition = v.BasePosition - Vector3.up * (v.BaseScale.y * 0.1f * p);
                    if (v.Punch <= 0f)
                    {
                        v.Transform.localScale = v.BaseScale;
                        v.Transform.localPosition = v.BasePosition;
                        _animating.RemoveAt(k);
                    }
                    continue;
                }
                _animating.RemoveAt(k);
            }

            if (_highlight != null && _highlight.gameObject.activeSelf)
            {
                float pulse = 1f + Mathf.Sin(Time.time * 7f) * 0.035f;
                _highlight.localScale = new Vector3(pulse, 1f, pulse);
            }
        }

        private void Track(int index)
        {
            if (!_animating.Contains(index)) _animating.Add(index);
        }

        private void ApplyDamage(int index)
        {
            TerrainCell cell = _grid.GetCell(index);
            CellVisual v = _cells[index];
            float damage = 1f - cell.Health01;
            if (damage <= 0f) return;

            _block.Clear();
            MaterialLibrary.SetColor(_block, Color.Lerp(v.TopColor, Palette.Shade(v.SideColor, 0.75f), damage * 0.85f));
            v.Renderer.SetPropertyBlock(_block, 0);
            _block.Clear();
            MaterialLibrary.SetColor(_block, Color.Lerp(v.SideColor, Palette.Shade(v.SideColor, 0.7f), damage));
            v.Renderer.SetPropertyBlock(_block, 1);

            if (damage >= 0.35f && v.Cracks == null) v.Cracks = BuildCracks(v.Transform, Hash(cell.Coord.X, cell.Coord.Layer, cell.Coord.Z));
            if (v.Cracks != null) v.Cracks.transform.localScale = Vector3.one * Mathf.Lerp(0.6f, 1f, damage);
        }

        private GameObject BuildCracks(Transform cell, int hash)
        {
            // Children of a non-uniformly scaled block: compensate so the cracks stay thin.
            Transform root = PrimitiveFactory.Pivot("Cracks", cell, new Vector3(0f, 0.5f, 0f));
            Material dark = _materials.Get(Palette.Hex(0x4A2A14));
            float yScale = 0.012f / Mathf.Max(0.01f, cell.localScale.y);
            float angle = (hash % 90) - 45f;
            GameObject a = PrimitiveFactory.Box("CrackA", root, new Vector3(-0.08f, 0f, 0.05f), new Vector3(0.55f, yScale, 0.035f), dark, false);
            a.transform.localRotation = Quaternion.Euler(0f, angle, 0f);
            GameObject b = PrimitiveFactory.Box("CrackB", root, new Vector3(0.14f, 0f, -0.08f), new Vector3(0.32f, yScale, 0.03f), dark, false);
            b.transform.localRotation = Quaternion.Euler(0f, angle + 55f, 0f);
            GameObject c = PrimitiveFactory.Box("CrackC", root, new Vector3(-0.2f, 0f, -0.14f), new Vector3(0.22f, yScale, 0.025f), dark, false);
            c.transform.localRotation = Quaternion.Euler(0f, angle - 60f, 0f);
            return root.gameObject;
        }

        private void AddPebbles(Transform cell, int hash, Color topColor)
        {
            Material light = _materials.Get(Palette.Shade(topColor, 1.12f));
            Material dark = _materials.Get(Palette.Hex(0x8C7A6A));
            float yScale = 1f / Mathf.Max(0.01f, cell.localScale.y);
            int count = 1 + hash % 2;
            for (int i = 0; i < count; i++)
            {
                float x = ((hash >> (i * 4)) % 7 - 3) * 0.1f;
                float z = ((hash >> (i * 4 + 2)) % 7 - 3) * 0.1f;
                float size = 0.07f + (hash >> (i + 5)) % 3 * 0.025f;
                PrimitiveFactory.Create("Pebble", PrimitiveType.Sphere, cell, new Vector3(x, 0.5f, z), new Vector3(size, size * 0.55f * yScale, size), i % 2 == 0 ? dark : light, false);
            }
        }

        private Transform BuildHighlight()
        {
            Transform root = PrimitiveFactory.Pivot("DigHighlight", transform, Vector3.zero);
            Material yellow = _materials.Get(UITheme.Gold);
            const float s = 1.0f, w = 0.07f, h = 0.04f;
            PrimitiveFactory.Box("N", root, new Vector3(0f, 0f, s * 0.5f), new Vector3(s + w, h, w), yellow, false);
            PrimitiveFactory.Box("S", root, new Vector3(0f, 0f, -s * 0.5f), new Vector3(s + w, h, w), yellow, false);
            PrimitiveFactory.Box("E", root, new Vector3(s * 0.5f, 0f, 0f), new Vector3(w, h, s + w), yellow, false);
            PrimitiveFactory.Box("W", root, new Vector3(-s * 0.5f, 0f, 0f), new Vector3(w, h, s + w), yellow, false);
            root.gameObject.SetActive(false);
            return root;
        }

        /// <summary>
        /// Fills the plot around the grid with sand on top and coloured strata below (one band per terrain layer),
        /// so excavated pit walls show the soil layers.
        /// </summary>
        private void BuildPitSurroundings(ExcavationGrid grid)
        {
            GridGeometry l = grid.Layout;
            float minX = l.OriginX, maxX = l.OriginX + grid.Width * l.CellSize;
            float minZ = l.OriginZ, maxZ = l.OriginZ + grid.Length * l.CellSize;
            float centerX = (minX + maxX) * 0.5f, centerZ = (minZ + maxZ) * 0.5f;
            float plotMinX = centerX - PlotHalfSize, plotMaxX = centerX + PlotHalfSize;
            float plotMinZ = centerZ - PlotHalfSize, plotMaxZ = centerZ + PlotHalfSize;
            float top = l.SurfaceY;

            Material sand = _materials.Get(Palette.Sand);
            const float crust = 0.08f;
            Frame("Crust", plotMinX, plotMaxX, plotMinZ, plotMaxZ, minX, maxX, minZ, maxZ, top - crust, top, sand);

            float y = top - crust;
            for (int layer = 0; layer < grid.Layers; layer++)
            {
                TerrainMaterial material = grid.GetCell(0, layer, 0).Material;
                float bandBottom = top - (layer + 1) * l.LayerHeight;
                Color band = Palette.Shade(Palette.TerrainSide(material), layer % 2 == 0 ? 1f : 0.93f);
                Frame("Stratum" + layer, plotMinX, plotMaxX, plotMinZ, plotMaxZ, minX, maxX, minZ, maxZ, bandBottom, y, _materials.Get(band));
                y = bandBottom;
            }
            Material bedrock = _materials.Get(Palette.Hex(0x6E5643));
            Frame("Bedrock", plotMinX, plotMaxX, plotMinZ, plotMaxZ, minX, maxX, minZ, maxZ, top - PlotDepth, y, bedrock);
            FillBox("PitFloor", minX, maxX, minZ, maxZ, top - PlotDepth, grid.FloorY, bedrock);
        }

        /// <summary>Four boxes forming a rectangular ring between the outer plot and the inner grid hole.</summary>
        private void Frame(string name, float oMinX, float oMaxX, float oMinZ, float oMaxZ, float iMinX, float iMaxX, float iMinZ, float iMaxZ,
            float minY, float maxY, Material material)
        {
            FillBox(name + "W", oMinX, iMinX, oMinZ, oMaxZ, minY, maxY, material);
            FillBox(name + "E", iMaxX, oMaxX, oMinZ, oMaxZ, minY, maxY, material);
            FillBox(name + "S", iMinX, iMaxX, oMinZ, iMinZ, minY, maxY, material);
            FillBox(name + "N", iMinX, iMaxX, iMaxZ, oMaxZ, minY, maxY, material);
        }

        private void FillBox(string name, float minX, float maxX, float minZ, float maxZ, float minY, float maxY, Material material)
        {
            if (maxX - minX <= 0.001f || maxZ - minZ <= 0.001f || maxY - minY <= 0.001f) return;
            PrimitiveFactory.Box(name, _decorRoot,
                new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, (minZ + maxZ) * 0.5f),
                new Vector3(maxX - minX, maxY - minY, maxZ - minZ), material, false);
        }

        /// <summary>Yellow/navy tape on posts with orange flags outlining the contract area.</summary>
        private void BuildBoundary(ExcavationGrid grid)
        {
            GridGeometry l = grid.Layout;
            const float margin = 0.38f;
            float minX = l.OriginX - margin, maxX = l.OriginX + grid.Width * l.CellSize + margin;
            float minZ = l.OriginZ - margin, maxZ = l.OriginZ + grid.Length * l.CellSize + margin;
            Material post = _materials.Get(Palette.White);
            Material postCap = _materials.Get(UITheme.Orange);
            Material yellow = _materials.Get(UITheme.Gold);
            Material navy = _materials.Get(UITheme.Navy);

            var corners = new[] { new Vector3(minX, 0f, minZ), new Vector3(maxX, 0f, minZ), new Vector3(maxX, 0f, maxZ), new Vector3(minX, 0f, maxZ) };
            for (int i = 0; i < 4; i++)
            {
                Vector3 a = corners[i];
                Vector3 b = corners[(i + 1) % 4];
                PrimitiveFactory.Create("Post", PrimitiveType.Cylinder, _decorRoot, a + Vector3.up * 0.38f, new Vector3(0.09f, 0.38f, 0.09f), post);
                PrimitiveFactory.Create("PostCap", PrimitiveType.Sphere, _decorRoot, a + Vector3.up * 0.8f, Vector3.one * 0.13f, postCap);
                GameObject flag = PrimitiveFactory.Box("Flag", _decorRoot, a + new Vector3(0.12f, 0.68f, 0f), new Vector3(0.22f, 0.14f, 0.015f), postCap, false);
                flag.AddComponent<AmbientMotion>().Oscillate(Vector3.up, 18f, 2.2f + i * 0.3f);

                float length = Vector3.Distance(a, b);
                int segments = Mathf.Max(2, Mathf.RoundToInt(length / 0.32f));
                Vector3 dir = (b - a) / segments;
                bool alongX = Mathf.Abs(b.x - a.x) > 0.01f;
                for (int s = 0; s < segments; s++)
                {
                    Vector3 center = a + dir * (s + 0.5f) + Vector3.up * (0.56f - Mathf.Sin((s + 0.5f) / segments * Mathf.PI) * 0.05f);
                    Vector3 size = alongX ? new Vector3(dir.magnitude, 0.08f, 0.015f) : new Vector3(0.015f, 0.08f, dir.magnitude);
                    PrimitiveFactory.Box("Tape", _decorRoot, center, size, s % 2 == 0 ? yellow : navy, false);
                }
            }
        }

        private static int Hash(int x, int layer, int z)
        {
            unchecked
            {
                int h = x * 73856093 ^ layer * 19349663 ^ z * 83492791;
                h ^= h >> 13;
                h *= 0x5bd1e995;
                h ^= h >> 15;
                return h & 0x7fffffff;
            }
        }
    }
}
