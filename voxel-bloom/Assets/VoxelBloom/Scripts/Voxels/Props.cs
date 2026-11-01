using UnityEngine;

namespace VoxelBloom
{
    /// <summary>Small procedural voxel props (trees, flowers, clouds) for dioramas.</summary>
    public static class Props
    {
        public static void AddTree(Transform parent, Vector3 pos, float scale, int seed)
        {
            const int X = 7, Y = 11, Z = 7;
            var c = Empty(X * Y * Z);
            for (int y = 0; y < 4; y++) Set(c, X, Y, 3, y, 3, 0);
            var rnd = new System.Random(seed);
            for (int z = 0; z < Z; z++) for (int y = 4; y < Y; y++) for (int x = 0; x < X; x++)
            {
                float d = ((x - 3f) * (x - 3f)) / 9.5f + ((y - 7f) * (y - 7f)) / 12f + ((z - 3f) * (z - 3f)) / 9.5f;
                if (d <= 1f) Set(c, X, Y, x, y, z, (sbyte)(rnd.NextDouble() < 0.25 ? 2 : 1));
            }
            Spawn(parent, "Tree", c, X, Y, Z, new[] { new Color32(140, 92, 52, 255), new Color32(76, 186, 82, 255), new Color32(118, 214, 96, 255) }, pos, scale);
        }

        public static void AddFlower(Transform parent, Vector3 pos, float scale, Color32 petal)
        {
            const int X = 3, Y = 4, Z = 3;
            var c = Empty(X * Y * Z);
            Set(c, X, Y, 1, 0, 1, 0); Set(c, X, Y, 1, 1, 1, 0);
            Set(c, X, Y, 1, 3, 1, 2);
            Set(c, X, Y, 0, 3, 1, 1); Set(c, X, Y, 2, 3, 1, 1); Set(c, X, Y, 1, 3, 0, 1); Set(c, X, Y, 1, 3, 2, 1);
            Set(c, X, Y, 0, 1, 1, 0);
            Spawn(parent, "Flower", c, X, Y, Z, new[] { new Color32(70, 170, 70, 255), petal, new Color32(255, 214, 64, 255) }, pos, scale);
        }

        static sbyte[] Empty(int n) { var a = new sbyte[n]; for (int i = 0; i < n; i++) a[i] = -1; return a; }
        static void Set(sbyte[] c, int X, int Y, int x, int y, int z, sbyte v) => c[x + X * (y + Y * z)] = v;

        static void Spawn(Transform parent, string name, sbyte[] cells, int X, int Y, int Z, Color32[] pal, Vector3 pos, float scale)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer), typeof(VoxelModelView));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.GetComponent<VoxelModelView>().SetModel(cells, X, Y, Z, pal, scale);
        }
    }
}
