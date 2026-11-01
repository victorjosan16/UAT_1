using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace VoxelBloom
{
    /// <summary>
    /// Builds a single mesh containing only the exposed faces of a voxel grid (hidden faces culled),
    /// with per-vertex colour, ambient occlusion and a tiny deterministic colour jitter.
    /// One mesh per model keeps draw calls at 1 regardless of voxel count.
    /// </summary>
    public sealed class VoxelMeshBuilder
    {
        // face normals and tangent axes chosen so that cross(u, v) == -normal (Unity clockwise winding)
        static readonly Vector3Int[] Normal = { new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0), new Vector3Int(0, 1, 0), new Vector3Int(0, -1, 0), new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1) };
        static readonly Vector3Int[] U = { new Vector3Int(0, 0, 1), new Vector3Int(0, 1, 0), new Vector3Int(1, 0, 0), new Vector3Int(0, 0, 1), new Vector3Int(0, 1, 0), new Vector3Int(1, 0, 0) };
        static readonly Vector3Int[] V = { new Vector3Int(0, 1, 0), new Vector3Int(0, 0, 1), new Vector3Int(0, 0, 1), new Vector3Int(1, 0, 0), new Vector3Int(1, 0, 0), new Vector3Int(0, 1, 0) };
        static readonly int[] SU = { -1, -1, 1, 1 };
        static readonly int[] SV = { -1, 1, 1, -1 };

        readonly List<Vector3> verts = new List<Vector3>(8192);
        readonly List<Vector3> norms = new List<Vector3>(8192);
        readonly List<Color32> cols = new List<Color32>(8192);
        readonly List<Vector2> uvs = new List<Vector2>(8192);
        readonly List<int> tris = new List<int>(12288);
        readonly float[] ao = new float[4];

        public bool AmbientOcclusion = true;

        /// <summary>Rebuilds <paramref name="mesh"/>. Voxel (x,y,z) occupies [x,x+1]*size, centred on X/Z, base at y=0.</summary>
        public void Build(Mesh mesh, sbyte[] cells, int X, int Y, int Z, Color32[] palette, float size)
        {
            verts.Clear(); norms.Clear(); cols.Clear(); uvs.Clear(); tris.Clear();
            var offset = new Vector3(-X * 0.5f, 0f, -Z * 0.5f);
            for (int z = 0; z < Z; z++)
            for (int y = 0; y < Y; y++)
            for (int x = 0; x < X; x++)
            {
                int i = x + X * (y + Y * z);
                int c = cells[i];
                if (c < 0) continue;
                Color32 baseCol = palette[c];
                float jitter = 1f + (Hash(i) - 0.5f) * 0.06f;
                for (int f = 0; f < 6; f++)
                {
                    var n = Normal[f];
                    if (Solid(cells, X, Y, Z, x + n.x, y + n.y, z + n.z)) continue;
                    var u = U[f]; var v = V[f];
                    var center = new Vector3(x + 0.5f, y + 0.5f, z + 0.5f) + (Vector3)n * 0.5f;
                    int start = verts.Count;
                    for (int k = 0; k < 4; k++)
                    {
                        Vector3 p = center + ((Vector3)u * SU[k] + (Vector3)v * SV[k]) * 0.5f;
                        verts.Add((p + offset) * size);
                        norms.Add(n);
                        uvs.Add(new Vector2(SU[k] > 0 ? 1f : 0f, SV[k] > 0 ? 1f : 0f));
                        float shade = jitter;
                        ao[k] = 3;
                        if (AmbientOcclusion)
                        {
                            int ax = x + n.x, ay = y + n.y, az = z + n.z;
                            bool s1 = Solid(cells, X, Y, Z, ax + u.x * SU[k], ay + u.y * SU[k], az + u.z * SU[k]);
                            bool s2 = Solid(cells, X, Y, Z, ax + v.x * SV[k], ay + v.y * SV[k], az + v.z * SV[k]);
                            bool cc = Solid(cells, X, Y, Z, ax + u.x * SU[k] + v.x * SV[k], ay + u.y * SU[k] + v.y * SV[k], az + u.z * SU[k] + v.z * SV[k]);
                            int occ = s1 && s2 ? 0 : 3 - ((s1 ? 1 : 0) + (s2 ? 1 : 0) + (cc ? 1 : 0));
                            ao[k] = occ;
                            shade *= 0.72f + 0.093f * occ;
                        }
                        cols.Add(Scale(baseCol, shade));
                    }
                    // flip the quad diagonal to avoid AO anisotropy
                    if (ao[0] + ao[2] < ao[1] + ao[3])
                    {
                        tris.Add(start + 1); tris.Add(start + 2); tris.Add(start + 3);
                        tris.Add(start + 1); tris.Add(start + 3); tris.Add(start + 0);
                    }
                    else
                    {
                        tris.Add(start); tris.Add(start + 1); tris.Add(start + 2);
                        tris.Add(start); tris.Add(start + 2); tris.Add(start + 3);
                    }
                }
            }
            mesh.Clear();
            mesh.indexFormat = verts.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetColors(cols);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0, true);
        }

        static bool Solid(sbyte[] cells, int X, int Y, int Z, int x, int y, int z)
            => x >= 0 && y >= 0 && z >= 0 && x < X && y < Y && z < Z && cells[x + X * (y + Y * z)] >= 0;

        static float Hash(int i)
        {
            unchecked
            {
                uint h = (uint)i * 2654435761u;
                h ^= h >> 13; h *= 0x5bd1e995; h ^= h >> 15;
                return (h & 0xFFFF) / 65535f;
            }
        }

        static Color32 Scale(Color32 c, float s)
            => new Color32((byte)Mathf.Clamp(c.r * s, 0, 255), (byte)Mathf.Clamp(c.g * s, 0, 255), (byte)Mathf.Clamp(c.b * s, 0, 255), c.a);

        public static Color32[] ToColors(Core.Rgb[] palette)
        {
            var r = new Color32[palette.Length];
            for (int i = 0; i < palette.Length; i++) r[i] = new Color(palette[i].R, palette[i].G, palette[i].B, 1f);
            return r;
        }
    }
}
