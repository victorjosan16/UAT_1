using System.Collections.Generic;
using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// Generează un mesh de cub cu muchii rotunjite (stil „premium stylized”).
    /// Fețele plate au un singur pătrat în mijloc; vârfurile sunt concentrate doar pe rotunjiri,
    /// deci mesh-ul rămâne ieftin (~400 de vârfuri) și poate fi refolosit de sute de obiecte.
    /// </summary>
    public static class RoundedBoxMesh
    {
        /// <param name="size">Mărimea totală (1,1,1 = cub unitate).</param>
        /// <param name="radius">Raza rotunjirii (trebuie să fie mai mică decât jumătate din cea mai mică latură).</param>
        /// <param name="segments">Câte segmente are o rotunjire (3 = suficient pe mobil).</param>
        public static Mesh Create(Vector3 size, float radius, int segments = 3, string name = "RoundedBox")
        {
            Vector3 half = size * 0.5f;
            radius = Mathf.Clamp(radius, 0.001f, Mathf.Min(half.x, Mathf.Min(half.y, half.z)) - 0.001f);
            segments = Mathf.Max(1, segments);

            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();

            // Fiecare față: normala și cele două axe din planul ei.
            AddFace(Vector3.right, Vector3.forward, Vector3.up, half, radius, segments, vertices, normals, uvs, triangles);
            AddFace(Vector3.left, Vector3.back, Vector3.up, half, radius, segments, vertices, normals, uvs, triangles);
            AddFace(Vector3.up, Vector3.right, Vector3.forward, half, radius, segments, vertices, normals, uvs, triangles);
            AddFace(Vector3.down, Vector3.right, Vector3.back, half, radius, segments, vertices, normals, uvs, triangles);
            AddFace(Vector3.forward, Vector3.left, Vector3.up, half, radius, segments, vertices, normals, uvs, triangles);
            AddFace(Vector3.back, Vector3.right, Vector3.up, half, radius, segments, vertices, normals, uvs, triangles);

            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }

        /// <summary>Coordonatele de-a lungul unei axe: dese pe rotunjiri, un singur pas pe partea plată.</summary>
        private static List<float> AxisCoordinates(float half, float radius, int segments)
        {
            var coordinates = new List<float>();
            for (int i = 0; i <= segments; i++)
                coordinates.Add(-half + radius * (1f - Mathf.Cos(i / (float)segments * Mathf.PI * 0.5f)));
            for (int i = segments; i >= 0; i--)
                coordinates.Add(half - radius * (1f - Mathf.Cos(i / (float)segments * Mathf.PI * 0.5f)));
            return coordinates;
        }

        private static void AddFace(Vector3 normal, Vector3 axisU, Vector3 axisV, Vector3 half, float radius, int segments,
            List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs, List<int> triangles)
        {
            float halfU = Mathf.Abs(Vector3.Dot(half, axisU));
            float halfV = Mathf.Abs(Vector3.Dot(half, axisV));
            float halfN = Mathf.Abs(Vector3.Dot(half, normal));
            List<float> us = AxisCoordinates(halfU, radius, segments);
            List<float> vs = AxisCoordinates(halfV, radius, segments);
            Vector3 inner = half - Vector3.one * radius;
            int start = vertices.Count;

            foreach (float v in vs)
            {
                foreach (float u in us)
                {
                    Vector3 point = normal * halfN + axisU * u + axisV * v;
                    // Punctul de pe „cubul interior”; vârful final e la distanța „radius” de el.
                    var core = new Vector3(
                        Mathf.Clamp(point.x, -inner.x, inner.x),
                        Mathf.Clamp(point.y, -inner.y, inner.y),
                        Mathf.Clamp(point.z, -inner.z, inner.z));
                    Vector3 direction = (point - core).normalized;
                    vertices.Add(core + direction * radius);
                    normals.Add(direction);
                    uvs.Add(new Vector2((u + halfU) / (2f * halfU), (v + halfV) / (2f * halfV)));
                }
            }

            int columns = us.Count;
            for (int y = 0; y < vs.Count - 1; y++)
            {
                for (int x = 0; x < columns - 1; x++)
                {
                    int a = start + y * columns + x;
                    int b = a + 1;
                    int c = a + columns;
                    int d = c + 1;
                    triangles.Add(a); triangles.Add(c); triangles.Add(b);
                    triangles.Add(b); triangles.Add(c); triangles.Add(d);
                }
            }
        }
    }
}
