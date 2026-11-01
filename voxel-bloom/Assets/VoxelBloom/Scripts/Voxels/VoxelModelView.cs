using UnityEngine;
using VoxelBloom.Core;

namespace VoxelBloom
{
    /// <summary>Renders one voxel model as a single mesh; supports rebuilds after destruction and voxel picking.</summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class VoxelModelView : MonoBehaviour
    {
        readonly VoxelMeshBuilder builder = new VoxelMeshBuilder();
        Mesh mesh;
        public int X { get; private set; }
        public int Y { get; private set; }
        public int Z { get; private set; }
        public float VoxelSize { get; private set; } = 0.1f;
        public Color32[] Palette { get; private set; }
        sbyte[] cells;
        bool dirty;

        void Awake()
        {
            mesh = new Mesh { name = "VoxelModel" };
            mesh.MarkDynamic();
            GetComponent<MeshFilter>().sharedMesh = mesh;
            var r = GetComponent<MeshRenderer>();
            r.sharedMaterial = VoxelMaterials.Voxel;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        public void SetModel(sbyte[] modelCells, int x, int y, int z, Color32[] palette, float voxelSize)
        {
            X = x; Y = y; Z = z; Palette = palette; VoxelSize = voxelSize;
            cells = (sbyte[])modelCells.Clone();
            builder.AmbientOcclusion = !Services.Initialized || Services.Save.Settings.Quality > 0;
            Rebuild();
        }

        public void SetModel(VoxelGrid grid, float voxelSize)
            => SetModel(grid.CopyCells(), grid.X, grid.Y, grid.Z, VoxelMeshBuilder.ToColors(grid.Palette), voxelSize);

        public void RemoveVoxel(int index)
        {
            if (cells == null || index < 0 || index >= cells.Length) return;
            cells[index] = -1;
            dirty = true;
        }

        /// <summary>Mesh rebuilds are coalesced to at most one per frame.</summary>
        void LateUpdate()
        {
            if (dirty) Rebuild();
        }

        public void Rebuild()
        {
            dirty = false;
            if (cells == null || mesh == null) return;
            builder.Build(mesh, cells, X, Y, Z, Palette, VoxelSize);
        }

        /// <summary>World position of a voxel centre.</summary>
        public Vector3 VoxelWorldPosition(int index)
        {
            int x = index % X, y = (index / X) % Y, z = index / (X * Y);
            var local = new Vector3(x + 0.5f - X * 0.5f, y + 0.5f, z + 0.5f - Z * 0.5f) * VoxelSize;
            return transform.TransformPoint(local);
        }

        public Color32 ColorOf(int paletteIndex) => Palette != null && paletteIndex >= 0 && paletteIndex < Palette.Length ? Palette[paletteIndex] : new Color32(255, 255, 255, 255);

        public Bounds LocalBounds => new Bounds(new Vector3(0, Y * VoxelSize * 0.5f, 0), new Vector3(X, Y, Z) * VoxelSize);

        /// <summary>Ray vs voxel grid (Amanatides-Woo DDA) in model space. Returns the first solid voxel index or -1.</summary>
        public int Raycast(Ray worldRay)
        {
            if (cells == null) return -1;
            Vector3 o = transform.InverseTransformPoint(worldRay.origin) / VoxelSize + new Vector3(X * 0.5f, 0, Z * 0.5f);
            Vector3 d = transform.InverseTransformDirection(worldRay.direction).normalized;
            // clip ray to grid box
            float tMin = 0f, tMax = 1e4f;
            for (int a = 0; a < 3; a++)
            {
                float lo = 0, hi = a == 0 ? X : a == 1 ? Y : Z;
                if (Mathf.Abs(d[a]) < 1e-6f) { if (o[a] < lo || o[a] > hi) return -1; continue; }
                float t1 = (lo - o[a]) / d[a], t2 = (hi - o[a]) / d[a];
                if (t1 > t2) { float t = t1; t1 = t2; t2 = t; }
                tMin = Mathf.Max(tMin, t1); tMax = Mathf.Min(tMax, t2);
                if (tMin > tMax) return -1;
            }
            Vector3 p = o + d * (tMin + 1e-4f);
            int x = Mathf.Clamp(Mathf.FloorToInt(p.x), 0, X - 1), y = Mathf.Clamp(Mathf.FloorToInt(p.y), 0, Y - 1), z = Mathf.Clamp(Mathf.FloorToInt(p.z), 0, Z - 1);
            int sx = d.x > 0 ? 1 : -1, sy = d.y > 0 ? 1 : -1, sz = d.z > 0 ? 1 : -1;
            float nx = d.x > 0 ? x + 1 : x, ny = d.y > 0 ? y + 1 : y, nz = d.z > 0 ? z + 1 : z;
            float tdx = Mathf.Abs(d.x) < 1e-6f ? float.MaxValue : Mathf.Abs(1f / d.x);
            float tdy = Mathf.Abs(d.y) < 1e-6f ? float.MaxValue : Mathf.Abs(1f / d.y);
            float tdz = Mathf.Abs(d.z) < 1e-6f ? float.MaxValue : Mathf.Abs(1f / d.z);
            float tx = Mathf.Abs(d.x) < 1e-6f ? float.MaxValue : (nx - p.x) / d.x;
            float ty = Mathf.Abs(d.y) < 1e-6f ? float.MaxValue : (ny - p.y) / d.y;
            float tz = Mathf.Abs(d.z) < 1e-6f ? float.MaxValue : (nz - p.z) / d.z;
            for (int step = 0; step < X + Y + Z + 3; step++)
            {
                if (x < 0 || y < 0 || z < 0 || x >= X || y >= Y || z >= Z) return -1;
                int i = x + X * (y + Y * z);
                if (cells[i] >= 0) return i;
                if (tx < ty && tx < tz) { x += sx; tx += tdx; }
                else if (ty < tz) { y += sy; ty += tdy; }
                else { z += sz; tz += tdz; }
            }
            return -1;
        }

        void OnDestroy()
        {
            if (mesh != null) Destroy(mesh);
        }
    }
}
