using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace QuarryEmpire.EditorTools
{
    /// <summary>
    /// Generează procedural asset-urile vizuale (fără pachete externe / licențe):
    /// mesh-uri cu muchii rotunjite, pietre low-poly, săgeată, inel; sprite-uri UI (dreptunghi rotunjit
    /// 9-slice, cerc, inel, iconuri) și texturi pentru particule; materiale URP mate.
    /// Asset-urile existente sunt actualizate pe loc (GUID-ul rămâne, referințele nu se rup).
    /// </summary>
    public static class VisualAssetFactory
    {
        public const string Root = "Assets/_Game/Art/Visual";
        public const string MeshesDir = Root + "/Meshes";
        public const string TexturesDir = Root + "/Textures";
        public const string UiDir = Root + "/UI";
        public const string MaterialsDir = Root + "/Materials";

        public static void EnsureFolders()
        {
            foreach (string path in new[] { MeshesDir, TexturesDir, UiDir, MaterialsDir })
                EnsureFolder(path);
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            string parent = path.Substring(0, slash);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(slash + 1));
        }

        public static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;

        // ================================================================ Mesh-uri

        /// <summary>Salvează mesh-ul ca asset; dacă există, îi copiază datele (păstrează GUID-ul).</summary>
        public static Mesh SaveMesh(string name, Mesh mesh)
        {
            string path = $"{MeshesDir}/{name}.asset";
            mesh.name = name;
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(mesh, path);
                return mesh;
            }
            existing.Clear();
            existing.vertices = mesh.vertices;
            existing.normals = mesh.normals;
            existing.uv = mesh.uv;
            existing.triangles = mesh.triangles;
            existing.RecalculateBounds();
            EditorUtility.SetDirty(existing);
            Object.DestroyImmediate(mesh);
            return existing;
        }

        /// <summary>
        /// Cutie cu muchii rotunjite: o grilă pe fiecare față, apoi fiecare vârf e „împins” spre
        /// suprafața unei cutii interioare + rază. Normalele sunt netede, deci muchiile prind lumina moale.
        /// Vârfurile sunt doar unde e rotunjire (fețele plate nu au nevoie de grilă deasă): ~380 vârfuri.
        /// </summary>
        public static Mesh RoundedBox(Vector3 size, float radius, int segments = 3)
        {
            Vector3 half = size * 0.5f;
            radius = Mathf.Min(radius, Mathf.Min(half.x, Mathf.Min(half.y, half.z)) * 0.999f);
            Vector3 inner = half - Vector3.one * radius;

            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();

            Vector3[] faceNormals = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            foreach (Vector3 n in faceNormals)
            {
                Vector3 u = Mathf.Abs(n.y) > 0.5f ? Vector3.right : Vector3.Cross(Vector3.up, n);
                Vector3 v = Vector3.Cross(n, u);
                float[] stepsU = BuildSteps(segments, radius / Mathf.Abs(Vector3.Dot(u, half)));
                float[] stepsV = BuildSteps(segments, radius / Mathf.Abs(Vector3.Dot(v, half)));
                int start = vertices.Count;
                for (int y = 0; y < stepsV.Length; y++)
                {
                    for (int x = 0; x < stepsU.Length; x++)
                    {
                        // Punct pe fața cubului [-half, half].
                        Vector3 p = Vector3.Scale(n + u * stepsU[x] + v * stepsV[y], half);
                        Vector3 clamped = new Vector3(
                            Mathf.Clamp(p.x, -inner.x, inner.x),
                            Mathf.Clamp(p.y, -inner.y, inner.y),
                            Mathf.Clamp(p.z, -inner.z, inner.z));
                        Vector3 normal = (p - clamped).normalized;
                        vertices.Add(clamped + normal * radius);
                        normals.Add(normal);
                        uvs.Add(new Vector2((stepsU[x] + 1f) * 0.5f, (stepsV[y] + 1f) * 0.5f));
                    }
                }
                int row = stepsU.Length;
                for (int y = 0; y < stepsV.Length - 1; y++)
                {
                    for (int x = 0; x < row - 1; x++)
                    {
                        int i = start + y * row + x;
                        triangles.Add(i); triangles.Add(i + row); triangles.Add(i + 1);
                        triangles.Add(i + 1); triangles.Add(i + row); triangles.Add(i + row + 1);
                    }
                }
            }

            var mesh = new Mesh();
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            FixWindingByNormals(mesh);
            mesh.RecalculateBounds();
            return mesh;
        }

        // Pași în [-1, 1] pe o axă: deși în zona rotunjită (fracția f de la fiecare capăt), nimic între.
        private static float[] BuildSteps(int segments, float fraction)
        {
            segments = Mathf.Max(1, segments);
            float f = Mathf.Clamp(fraction, 0.001f, 1f);
            var list = new List<float>();
            for (int i = 0; i <= segments; i++) list.Add(-1f + f * i / segments);
            for (int i = segments; i >= 0; i--) list.Add(1f - f * i / segments);
            return list.ToArray();
        }

        /// <summary>Piatră low-poly: icosaedru subdivizat, deformat cu zgomot, cu fețe plate (look stilizat).</summary>
        public static Mesh Rock(int seed, float roughness = 0.22f, float flatten = 0.75f, int subdivisions = 1)
        {
            var random = new System.Random(seed);
            BuildIcosphere(subdivisions, out List<Vector3> points, out List<int> indices);

            // Deformare stabilă (același punct → aceeași deplasare), apoi turtire pe Y.
            var displaced = new Vector3[points.Count];
            float ox = (float)random.NextDouble() * 100f, oz = (float)random.NextDouble() * 100f;
            for (int i = 0; i < points.Count; i++)
            {
                Vector3 p = points[i];
                float noise = Mathf.PerlinNoise(p.x * 1.7f + ox, p.z * 1.7f + oz + p.y);
                float r = 0.5f * (1f + (noise - 0.5f) * 2f * roughness);
                p *= r;
                p.y *= flatten;
                if (p.y < -0.5f * flatten * 0.6f) p.y = -0.5f * flatten * 0.6f; // bază plată, stă pe sol
                displaced[i] = p;
            }
            Mesh mesh = FlatShaded(displaced, indices);
            FixWindingByCentroid(mesh);
            return mesh;
        }

        /// <summary>Inel plat (pe XZ), pentru marcajul pulsatoriu al zonelor.</summary>
        public static Mesh Ring(float innerRadius, float outerRadius, int segments = 48)
        {
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            for (int i = 0; i <= segments; i++)
            {
                float a = i / (float)segments * Mathf.PI * 2f;
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                vertices.Add(dir * innerRadius);
                vertices.Add(dir * outerRadius);
                uvs.Add(new Vector2(i / (float)segments, 0f));
                uvs.Add(new Vector2(i / (float)segments, 1f));
                if (i == segments) continue;
                int b = i * 2;
                triangles.Add(b); triangles.Add(b + 1); triangles.Add(b + 3);
                triangles.Add(b); triangles.Add(b + 3); triangles.Add(b + 2);
            }
            var mesh = new Mesh();
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            var normals = new Vector3[vertices.Count];
            for (int i = 0; i < normals.Length; i++) normals[i] = Vector3.up;
            mesh.normals = normals;
            FixWindingByNormals(mesh);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh FlatShaded(IReadOnlyList<Vector3> points, List<int> indices)
        {
            var vertices = new Vector3[indices.Count];
            var triangles = new int[indices.Count];
            for (int i = 0; i < indices.Count; i++)
            {
                vertices[i] = points[indices[i]];
                triangles[i] = i;
            }
            return new Mesh { vertices = vertices, triangles = triangles };
        }

        // Unity randează doar fața din față a triunghiurilor. Ambele funcții inversează
        // triunghiurile orientate greșit, deci mesh-ul nu poate ieși „pe dos”.

        /// <summary>Orientare după normalele dorite (cutie rotunjită, inel).</summary>
        private static void FixWindingByNormals(Mesh mesh)
        {
            Vector3[] v = mesh.vertices;
            Vector3[] n = mesh.normals;
            int[] t = mesh.triangles;
            for (int i = 0; i < t.Length; i += 3)
            {
                Vector3 face = Vector3.Cross(v[t[i + 1]] - v[t[i]], v[t[i + 2]] - v[t[i]]);
                if (Vector3.Dot(face, n[t[i]] + n[t[i + 1]] + n[t[i + 2]]) < 0f) (t[i + 1], t[i + 2]) = (t[i + 2], t[i + 1]);
            }
            mesh.triangles = t;
        }

        /// <summary>Orientare spre exterior față de centru (forme aproape convexe: pietre), apoi normale plate.</summary>
        private static void FixWindingByCentroid(Mesh mesh)
        {
            Vector3[] v = mesh.vertices;
            int[] t = mesh.triangles;
            Vector3 center = Vector3.zero;
            foreach (Vector3 p in v) center += p;
            center /= Mathf.Max(1, v.Length);
            for (int i = 0; i < t.Length; i += 3)
            {
                Vector3 a = v[t[i]], b = v[t[i + 1]], c = v[t[i + 2]];
                Vector3 face = Vector3.Cross(b - a, c - a);
                if (Vector3.Dot(face, (a + b + c) / 3f - center) < 0f) (t[i + 1], t[i + 2]) = (t[i + 2], t[i + 1]);
            }
            mesh.triangles = t;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }

        private static void BuildIcosphere(int subdivisions, out List<Vector3> points, out List<int> indices)
        {
            float t = (1f + Mathf.Sqrt(5f)) * 0.5f;
            points = new List<Vector3>
            {
                new(-1, t, 0), new(1, t, 0), new(-1, -t, 0), new(1, -t, 0),
                new(0, -1, t), new(0, 1, t), new(0, -1, -t), new(0, 1, -t),
                new(t, 0, -1), new(t, 0, 1), new(-t, 0, -1), new(-t, 0, 1),
            };
            for (int i = 0; i < points.Count; i++) points[i] = points[i].normalized;
            indices = new List<int>
            {
                0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11, 1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
                3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9, 4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1,
            };
            for (int s = 0; s < subdivisions; s++)
            {
                var cache = new Dictionary<long, int>();
                var next = new List<int>();
                var pts = points;
                int Mid(int a, int b)
                {
                    long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
                    if (cache.TryGetValue(key, out int index)) return index;
                    pts.Add(((pts[a] + pts[b]) * 0.5f).normalized);
                    cache[key] = pts.Count - 1;
                    return pts.Count - 1;
                }
                for (int i = 0; i < indices.Count; i += 3)
                {
                    int a = indices[i], b = indices[i + 1], c = indices[i + 2];
                    int ab = Mid(a, b), bc = Mid(b, c), ca = Mid(c, a);
                    next.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
                }
                indices = next;
            }
        }

        // ================================================================ Texturi și sprite-uri

        /// <summary>Distanța cu semn (pixeli) față de formă: negativ = în interior. Antialiasing gratuit.</summary>
        public delegate float Sdf(Vector2 p);

        public static Texture2D RenderSdf(int size, Sdf sdf, Color color, float softness = 1f)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = sdf(new Vector2(x + 0.5f, y + 0.5f));
                    float alpha = Mathf.Clamp01(0.5f - d / Mathf.Max(0.01f, softness));
                    pixels[y * size + x] = new Color(color.r, color.g, color.b, color.a * alpha);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        /// <summary>Scrie PNG-ul și îl importă ca Sprite (cu border pentru 9-slice dacă e cazul).</summary>
        public static Sprite SaveSprite(string name, Texture2D texture, Vector4 border = default, bool mipmaps = false)
        {
            string path = $"{UiDir}/{name}.png";
            WritePng(path, texture);
            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = mipmaps;
                importer.spriteBorder = border;
                importer.spritePixelsPerUnit = 100f;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Compressed;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        public static Texture2D SaveTexture(string name, Texture2D texture, TextureWrapMode wrap, bool mipmaps = true)
        {
            string path = $"{TexturesDir}/{name}.png";
            WritePng(path, texture);
            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                importer.textureType = TextureImporterType.Default;
                importer.alphaIsTransparency = true;
                importer.wrapMode = wrap;
                importer.mipmapEnabled = mipmaps;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Compressed;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static void WritePng(string path, Texture2D texture)
        {
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        // ---- Forme SDF (coordonate în pixeli)

        public static float Circle(Vector2 p, Vector2 c, float r) => (p - c).magnitude - r;

        public static float RoundBox(Vector2 p, Vector2 c, Vector2 halfSize, float r)
        {
            Vector2 q = new Vector2(Mathf.Abs(p.x - c.x), Mathf.Abs(p.y - c.y)) - halfSize + Vector2.one * r;
            return new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - r;
        }

        public static float Segment(Vector2 p, Vector2 a, Vector2 b, float r)
        {
            Vector2 pa = p - a, ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
            return (pa - ba * h).magnitude - r;
        }

        /// <summary>SDF exact pentru poligon (formula Inigo Quilez).</summary>
        public static float Polygon(Vector2 p, Vector2[] v)
        {
            float d = Vector2.Dot(p - v[0], p - v[0]);
            float s = 1f;
            for (int i = 0, j = v.Length - 1; i < v.Length; j = i, i++)
            {
                Vector2 e = v[j] - v[i];
                Vector2 w = p - v[i];
                Vector2 b = w - e * Mathf.Clamp01(Vector2.Dot(w, e) / Vector2.Dot(e, e));
                d = Mathf.Min(d, Vector2.Dot(b, b));
                bool c1 = p.y >= v[i].y, c2 = p.y < v[j].y, c3 = e.x * w.y > e.y * w.x;
                if ((c1 && c2 && c3) || (!c1 && !c2 && !c3)) s = -s;
            }
            return s * Mathf.Sqrt(d);
        }

        public static Vector2[] Star(Vector2 c, float outer, float inner, int points, float rotation = 90f)
        {
            var v = new Vector2[points * 2];
            for (int i = 0; i < v.Length; i++)
            {
                float a = (rotation + i * 180f / points) * Mathf.Deg2Rad;
                float r = i % 2 == 0 ? outer : inner;
                v[i] = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
            }
            return v;
        }

        // ================================================================ Materiale

        public static Material Lit(string name, Color color, float smoothness = 0.12f, float metallic = 0f,
            Color? emission = null, Texture texture = null, Vector2? tiling = null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                            ?? throw new Exception("Lipsește shader-ul URP Lit.");
            Material material = LoadOrCreateMaterial(name, shader);
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", metallic);
            material.SetTexture("_BaseMap", texture);
            Vector2 scale = tiling ?? Vector2.one;
            material.SetTextureScale("_BaseMap", scale);
            if (emission.HasValue)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission.Value);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            else
            {
                material.DisableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", Color.black);
            }
            // Fără reflexii speculare puternice: aspect mat, „de jucărie”.
            material.SetFloat("_SpecularHighlights", smoothness > 0.3f ? 1f : 0f);
            if (smoothness <= 0.3f) material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            else material.DisableKeyword("_SPECULARHIGHLIGHTS_OFF");
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        public enum ParticleBlend { Alpha, Additive }

        public static Material ParticleUnlit(string name, Texture texture, ParticleBlend blend)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                            ?? throw new Exception("Lipsește shader-ul URP Particles/Unlit.");
            Material material = LoadOrCreateMaterial(name, shader);
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", Color.white);
            // Transparent: setăm direct proprietățile și cuvintele-cheie pe care le-ar seta Inspectorul URP.
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", blend == ParticleBlend.Additive ? 2f : 0f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", blend == ParticleBlend.Additive
                ? (float)UnityEngine.Rendering.BlendMode.One
                : (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            EditorUtility.SetDirty(material);
            return material;
        }

        public static Material ParticleLit(string name)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Simple Lit")
                            ?? Shader.Find("Universal Render Pipeline/Particles/Unlit");
            Material material = LoadOrCreateMaterial(name, shader);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Surface", 0f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material LoadOrCreateMaterial(string name, Shader shader)
        {
            string path = $"{MaterialsDir}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }
            return material;
        }
    }
}
