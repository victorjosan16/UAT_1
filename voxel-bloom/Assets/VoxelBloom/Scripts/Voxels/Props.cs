using System.Collections.Generic;
using UnityEngine;

namespace VoxelBloom
{
    /// <summary>Procedural voxel scenery for dioramas: floating island, trees, flowers, cliffs with a waterfall.</summary>
    public static class Props
    {
        static readonly Color32 Grass = new Color32(112, 206, 84, 255), GrassLight = new Color32(150, 226, 98, 255), GrassDark = new Color32(80, 170, 70, 255);
        static readonly Color32 Dirt = new Color32(170, 116, 72, 255), DirtDark = new Color32(132, 86, 52, 255);
        static readonly Color32 Stone = new Color32(150, 160, 176, 255), StoneDark = new Color32(116, 126, 146, 255);
        static readonly Color32 Water = new Color32(120, 200, 255, 205), WaterDeep = new Color32(80, 166, 240, 205), Foam = new Color32(235, 248, 255, 205);
        static readonly Color32 Trunk = new Color32(140, 92, 52, 255), Leaf = new Color32(76, 186, 82, 255), LeafLight = new Color32(124, 214, 96, 255), LeafDark = new Color32(56, 150, 70, 255);

        sealed class Builder
        {
            public readonly int X, Y, Z; public readonly sbyte[] c; public readonly List<Color32> pal = new List<Color32>();
            public Builder(int x, int y, int z) { X = x; Y = y; Z = z; c = new sbyte[x * y * z]; for (int i = 0; i < c.Length; i++) c[i] = -1; }
            public sbyte P(Color32 col) { int i = pal.FindIndex(p => p.r == col.r && p.g == col.g && p.b == col.b && p.a == col.a); if (i < 0) { pal.Add(col); i = pal.Count - 1; } return (sbyte)i; }
            public void Set(int x, int y, int z, Color32 col) { if (x >= 0 && y >= 0 && z >= 0 && x < X && y < Y && z < Z) c[x + X * (y + Y * z)] = P(col); }
            public bool Has(int x, int y, int z) => x >= 0 && y >= 0 && z >= 0 && x < X && y < Y && z < Z && c[x + X * (y + Y * z)] >= 0;
            public VoxelModelView Spawn(Transform parent, string name, Vector3 pos, float size)
            {
                var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer), typeof(VoxelModelView));
                go.transform.SetParent(parent, false);
                go.transform.localPosition = pos;
                var v = go.GetComponent<VoxelModelView>();
                v.SetModel(c, X, Y, Z, pal.ToArray(), size);
                return v;
            }
        }

        static float Hash(int x, int y, int z) { unchecked { uint h = (uint)(x * 73856093 ^ y * 19349663 ^ z * 83492791); h ^= h >> 13; h *= 0x5bd1e995; return (h & 1023) / 1023f; } }

        /// <summary>Floating grassy island: top at local y = 0, radius in voxels, tapered dirt underside.</summary>
        public static VoxelModelView AddIsland(Transform parent, Vector3 pos, int radius, float size)
        {
            int depth = radius / 2 + 3, X = radius * 2 + 1, Y = depth + 2, Z = X;
            var b = new Builder(X, Y, Z);
            for (int x = 0; x < X; x++) for (int z = 0; z < Z; z++)
            {
                float d = Mathf.Sqrt((x - radius) * (x - radius) + (z - radius) * (z - radius)) + Hash(x, 0, z) * 0.9f;
                if (d > radius) continue;
                // taper the underside: deeper near the centre
                int bottom = Mathf.Clamp(Mathf.RoundToInt((d / radius) * depth * 1.15f), 0, depth);
                for (int y = bottom; y < Y; y++)
                {
                    Color32 col;
                    if (y == Y - 1) col = Hash(x, y, z) < 0.3f ? GrassLight : Grass;
                    else if (y == Y - 2) col = d > radius - 1.5f ? GrassDark : Dirt;
                    else col = Hash(x, y, z) < 0.35f ? DirtDark : Dirt;
                    b.Set(x, y, z, col);
                }
            }
            // grass tufts
            for (int i = 0; i < radius * 2; i++)
            {
                int x = (int)(Hash(i, 7, 3) * X), z = (int)(Hash(3, i, 9) * Z);
                if (b.Has(x, Y - 1, z) && (x - radius) * (x - radius) + (z - radius) * (z - radius) > 25) b.Set(x, Y - 1, z, GrassLight);
            }
            return b.Spawn(parent, "Island", pos - new Vector3(0, Y * size, 0), size);
        }

        /// <summary>Blocky layered tree (trunk + three canopy tiers).</summary>
        public static VoxelModelView AddTree(Transform parent, Vector3 pos, float size, int seed)
        {
            const int X = 11, Y = 16, Z = 11;
            var b = new Builder(X, Y, Z);
            for (int y = 0; y < 6; y++) { b.Set(5, y, 5, Trunk); b.Set(5, y, 6, Trunk); b.Set(6, y, 5, Trunk); b.Set(6, y, 6, Trunk); }
            int[][] tiers = { new[] { 5, 9, 5 }, new[] { 9, 12, 4 }, new[] { 12, 15, 2 } };
            foreach (var t in tiers)
                for (int y = t[0]; y < t[1]; y++) for (int x = 0; x < X; x++) for (int z = 0; z < Z; z++)
                {
                    int r = t[2];
                    if (Mathf.Abs(x - 5.5f) > r + 0.5f || Mathf.Abs(z - 5.5f) > r + 0.5f) continue;
                    bool corner = Mathf.Abs(x - 5.5f) > r - 0.5f && Mathf.Abs(z - 5.5f) > r - 0.5f;
                    if (corner && Hash(x + seed, y, z) < 0.6f) continue;
                    b.Set(x, y, z, y == t[1] - 1 ? LeafLight : (Hash(x, y + seed, z) < 0.25f ? LeafDark : Leaf));
                }
            return b.Spawn(parent, "Tree", pos, size);
        }

        /// <summary>Flower: stem, leaf and a petal cross with a centre. Returns the view (for swaying).</summary>
        public static VoxelModelView AddFlower(Transform parent, Vector3 pos, float size, Color32 petal, Color32 centre)
        {
            const int X = 5, Y = 7, Z = 5;
            var b = new Builder(X, Y, Z);
            var stem = new Color32(70, 170, 70, 255);
            for (int y = 0; y < 4; y++) b.Set(2, y, 2, stem);
            b.Set(1, 1, 2, stem); b.Set(3, 2, 2, stem);
            for (int x = 0; x < X; x++) for (int z = 0; z < Z; z++)
            {
                int dx = Mathf.Abs(x - 2), dz = Mathf.Abs(z - 2);
                if (dx + dz <= 2 && !(dx == 2 && dz == 2)) b.Set(x, 5, z, petal);
            }
            b.Set(2, 5, 2, centre); b.Set(2, 6, 2, centre); b.Set(2, 4, 2, petal);
            return b.Spawn(parent, "Flower", pos, size);
        }

        /// <summary>Background cliffs with a waterfall (animated water via vertex alpha) and a pond.</summary>
        public static VoxelModelView AddCliffWaterfall(Transform parent, Vector3 pos, float size)
        {
            const int X = 26, Y = 18, Z = 10;
            var b = new Builder(X, Y, Z);
            for (int x = 0; x < X; x++) for (int z = 0; z < Z; z++)
            {
                int h = Mathf.RoundToInt(10 + 6 * Mathf.Sin(x * 0.35f) * 0.5f + Hash(x, 0, z) * 2f) - (z < 3 ? 6 : 0);
                if (x >= 11 && x <= 15) h = Mathf.Max(h, 14);
                for (int y = 0; y < Mathf.Min(h, Y); y++) b.Set(x, y, z, y == h - 1 ? Grass : (Hash(x, y, z) < 0.3f ? StoneDark : Stone));
            }
            // waterfall down the front of the high section, pond at the bottom
            for (int y = 0; y < 14; y++) for (int x = 12; x <= 14; x++) b.Set(x, y, 2, y % 3 == 0 ? Foam : Water);
            for (int x = 6; x <= 21; x++) for (int z = 0; z <= 2; z++) b.Set(x, 0, z, z == 0 ? WaterDeep : Water);
            for (int x = 11; x <= 15; x++) b.Set(x, 1, 1, Foam);
            return b.Spawn(parent, "Cliffs", pos, size);
        }

        /// <summary>Display pedestal: purple base with a glowing golden top ring (top surface at local y = 0).</summary>
        public static VoxelModelView AddPedestal(Transform parent, Vector3 pos, int radius, float size)
        {
            int X = radius * 2 + 1, Y = 4, Z = X;
            var b = new Builder(X, Y, Z);
            var purple = new Color32(124, 82, 230, 255); var purpleDark = new Color32(92, 58, 190, 255);
            var gold = new Color32(255, 196, 60, 255); var goldLight = new Color32(255, 226, 120, 255);
            for (int x = 0; x < X; x++) for (int z = 0; z < Z; z++)
            {
                float d = Mathf.Sqrt((x - radius) * (x - radius) + (z - radius) * (z - radius));
                if (d > radius + 0.3f) continue;
                for (int y = 0; y < Y; y++)
                {
                    Color32 c = y == Y - 1 ? (d > radius - 1.6f ? gold : (d > radius - 2.6f ? goldLight : gold)) : (y == Y - 2 && d > radius - 1.2f ? purpleDark : purple);
                    if (y == Y - 1 && d < radius - 2.6f) c = goldLight;
                    b.Set(x, y, z, c);
                }
            }
            return b.Spawn(parent, "Pedestal", pos - new Vector3(0, Y * size, 0), size);
        }

        public static VoxelModelView AddBush(Transform parent, Vector3 pos, float size)
        {
            const int X = 6, Y = 4, Z = 5;
            var b = new Builder(X, Y, Z);
            for (int x = 0; x < X; x++) for (int y = 0; y < Y; y++) for (int z = 0; z < Z; z++)
            {
                float d = ((x - 2.5f) * (x - 2.5f)) / 9f + (y * y) / 12f + ((z - 2f) * (z - 2f)) / 6f;
                if (d <= 1f) b.Set(x, y, z, y == Y - 1 || Hash(x, y, z) < 0.3f ? LeafLight : Leaf);
            }
            return b.Spawn(parent, "Bush", pos, size);
        }
    }

    /// <summary>Gentle sway for flowers/trees in dioramas (one component, no per-object Update).</summary>
    public sealed class DioramaAnimator : MonoBehaviour
    {
        readonly List<Transform> sway = new List<Transform>();
        readonly List<float> phase = new List<float>();
        public void Add(Component c, float ph) { sway.Add(c.transform); phase.Add(ph); }

        void Update()
        {
            if (Services.ReducedEffects) return;
            float t = Time.unscaledTime;
            for (int i = 0; i < sway.Count; i++)
                if (sway[i] != null) sway[i].localRotation = Quaternion.Euler(Mathf.Sin(t * 1.6f + phase[i]) * 4f, 0, Mathf.Sin(t * 1.2f + phase[i] * 1.7f) * 5f);
        }
    }
}
