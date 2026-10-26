using System.Collections.Generic;
using UnityEngine;

namespace UndergroundMetroTycoon.Game
{
    /// <summary>
    /// Shared bevelled unit block (centred, size 1) with flat-shaded chamfers so every block edge catches the light.
    /// Submesh 0 is the top face plus its bevels (lighter "surface" material); submesh 1 is the sides and bottom.
    /// One mesh instance is shared by every terrain block.
    /// </summary>
    public static class TerrainBlockMesh
    {
        private const float Half = 0.5f;
        private const float Bevel = 0.075f;
        private static Mesh _mesh;

        public static Mesh Get() => _mesh != null ? _mesh : _mesh = Build();

        private static Mesh Build()
        {
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var top = new List<int>();
            var sides = new List<int>();
            const float h = Half, i = Half - Bevel;

            // Six main faces.
            for (int axis = 0; axis < 3; axis++)
            {
                for (int sign = -1; sign <= 1; sign += 2)
                {
                    int u = (axis + 1) % 3, v = (axis + 2) % 3;
                    Vector3 a = Point(axis, sign * h, u, -i, v, -i);
                    Vector3 b = Point(axis, sign * h, u, i, v, -i);
                    Vector3 c = Point(axis, sign * h, u, i, v, i);
                    Vector3 d = Point(axis, sign * h, u, -i, v, i);
                    Vector3 n = Axis(axis) * sign;
                    AddQuad(vertices, normals, axis == 1 && sign > 0 ? top : sides, a, b, c, d, n);
                }
            }

            // Twelve edge chamfers.
            for (int a = 0; a < 3; a++)
            {
                for (int b = a + 1; b < 3; b++)
                {
                    int c = 3 - a - b;
                    for (int sa = -1; sa <= 1; sa += 2)
                    {
                        for (int sb = -1; sb <= 1; sb += 2)
                        {
                            Vector3 p0 = Point3(a, sa * h, b, sb * i, c, -i);
                            Vector3 p1 = Point3(a, sa * h, b, sb * i, c, i);
                            Vector3 p2 = Point3(a, sa * i, b, sb * h, c, i);
                            Vector3 p3 = Point3(a, sa * i, b, sb * h, c, -i);
                            Vector3 n = (Axis(a) * sa + Axis(b) * sb).normalized;
                            bool isTop = (a == 1 && sa > 0) || (b == 1 && sb > 0);
                            AddQuad(vertices, normals, isTop ? top : sides, p0, p1, p2, p3, n);
                        }
                    }
                }
            }

            // Eight corner triangles.
            for (int sx = -1; sx <= 1; sx += 2)
            {
                for (int sy = -1; sy <= 1; sy += 2)
                {
                    for (int sz = -1; sz <= 1; sz += 2)
                    {
                        var p0 = new Vector3(sx * h, sy * i, sz * i);
                        var p1 = new Vector3(sx * i, sy * h, sz * i);
                        var p2 = new Vector3(sx * i, sy * i, sz * h);
                        AddTriangle(vertices, normals, sy > 0 ? top : sides, p0, p1, p2, new Vector3(sx, sy, sz).normalized);
                    }
                }
            }

            var mesh = new Mesh { name = "UMT_TerrainBlock" };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(top, 0);
            mesh.SetTriangles(sides, 1);
            mesh.RecalculateBounds();
            mesh.UploadMeshData(true);
            return mesh;
        }

        private static Vector3 Axis(int axis) => axis == 0 ? Vector3.right : axis == 1 ? Vector3.up : Vector3.forward;

        private static Vector3 Point(int axis, float axisValue, int u, float uValue, int v, float vValue) =>
            Point3(axis, axisValue, u, uValue, v, vValue);

        private static Vector3 Point3(int a, float av, int b, float bv, int c, float cv)
        {
            var p = new Vector3();
            p[a] = av;
            p[b] = bv;
            p[c] = cv;
            return p;
        }

        private static void AddQuad(List<Vector3> vertices, List<Vector3> normals, List<int> triangles, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal)
        {
            AddTriangle(vertices, normals, triangles, a, b, c, normal);
            AddTriangle(vertices, normals, triangles, a, c, d, normal);
        }

        /// <summary>Adds a triangle wound so that it faces <paramref name="normal"/> (Unity: clockwise = front).</summary>
        private static void AddTriangle(List<Vector3> vertices, List<Vector3> normals, List<int> triangles, Vector3 a, Vector3 b, Vector3 c, Vector3 normal)
        {
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), normal) < 0f)
            {
                Vector3 t = b;
                b = c;
                c = t;
            }
            int start = vertices.Count;
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            normals.Add(normal);
            normals.Add(normal);
            normals.Add(normal);
            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
        }
    }
}
