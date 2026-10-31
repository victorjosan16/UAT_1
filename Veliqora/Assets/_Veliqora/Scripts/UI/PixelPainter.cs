using System;
using UnityEngine;

namespace Veliqora.UI
{
    /// <summary>
    /// Tiny CPU rasterizer for PROCEDURAL PLACEHOLDER art (icons, panels, bottles, backgrounds).
    /// Shapes are signed-distance functions in pixel space, composited with anti-aliased alpha-over.
    /// Replace with authored PNGs via the workflow in DESIGN_SPEC.md ("Art replacement").
    /// </summary>
    public sealed class PixelPainter
    {
        public readonly int Width;
        public readonly int Height;
        private readonly Color[] _px;

        public PixelPainter(int width, int height, Color? clear = null)
        {
            Width = width;
            Height = height;
            _px = new Color[width * height];
            var c = clear ?? new Color(1, 1, 1, 0);
            for (int i = 0; i < _px.Length; i++) _px[i] = c;
        }

        public Color Get(int x, int y) => _px[y * Width + x];

        /// <summary>Raw RGBA pixels, bottom row first (Unity texture order).</summary>
        public Color[] Pixels => _px;

        /// <summary>Paints color where sdf &lt; 0, with ~1px anti-aliasing. Optional per-pixel color function.</summary>
        public void Fill(Func<Vector2, float> sdf, Color color, Func<Vector2, Color> shader = null, RectInt? bounds = null, float softness = 1f)
        {
            RectInt b = bounds ?? new RectInt(0, 0, Width, Height);
            int x0 = Mathf.Max(0, b.xMin), x1 = Mathf.Min(Width, b.xMax);
            int y0 = Mathf.Max(0, b.yMin), y1 = Mathf.Min(Height, b.yMax);
            for (int y = y0; y < y1; y++)
            {
                for (int x = x0; x < x1; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    float d = sdf(p);
                    float cov = Mathf.Clamp01(0.5f - d / softness);
                    if (cov <= 0f) continue;
                    var src = shader != null ? shader(p) : color;
                    src.a *= cov;
                    Blend(x, y, src);
                }
            }
        }

        public void Blend(int x, int y, Color src)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height) return;
            int i = y * Width + x;
            var dst = _px[i];
            float a = src.a + dst.a * (1f - src.a);
            if (a <= 1e-5f) { _px[i] = new Color(0, 0, 0, 0); return; }
            _px[i] = new Color(
                (src.r * src.a + dst.r * dst.a * (1f - src.a)) / a,
                (src.g * src.a + dst.g * dst.a * (1f - src.a)) / a,
                (src.b * src.a + dst.b * dst.a * (1f - src.a)) / a,
                a);
        }

        /// <summary>Additive light (screen-like glow): rgb += c.rgb * c.a, clamped. Assumes an opaque canvas.</summary>
        public void Add(int x, int y, Color c)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height) return;
            int i = y * Width + x;
            var d = _px[i];
            _px[i] = new Color(Mathf.Min(1f, d.r + c.r * c.a), Mathf.Min(1f, d.g + c.g * c.a), Mathf.Min(1f, d.b + c.b * c.a), d.a);
        }

        /// <summary>Soft additive radial glow (quadratic falloff), bounded to its radius.</summary>
        public void Glow(Vector2 center, float radius, Color c, float power = 2f)
        {
            int x0 = Mathf.Max(0, (int)(center.x - radius)), x1 = Mathf.Min(Width - 1, (int)(center.x + radius));
            int y0 = Mathf.Max(0, (int)(center.y - radius)), y1 = Mathf.Min(Height - 1, (int)(center.y + radius));
            float inv = 1f / Mathf.Max(0.5f, radius);
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float dx = (x + 0.5f - center.x) * inv, dy = (y + 0.5f - center.y) * inv;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    if (d >= 1f) continue;
                    float a = Mathf.Pow(1f - d, power) * c.a;
                    Add(x, y, new Color(c.r, c.g, c.b, a));
                }
        }

        /// <summary>Multi-stop vertical gradient; stops are (t from bottom 0..1, color), sorted ascending.</summary>
        public void Gradient(params (float t, Color c)[] stops)
        {
            for (int y = 0; y < Height; y++)
            {
                float t = y / (float)(Height - 1);
                var col = stops[stops.Length - 1].c;
                for (int k = 0; k < stops.Length - 1; k++)
                {
                    if (t <= stops[k + 1].t)
                    {
                        float u = Mathf.Clamp01((t - stops[k].t) / Mathf.Max(1e-4f, stops[k + 1].t - stops[k].t));
                        col = Color.Lerp(stops[k].c, stops[k + 1].c, u);
                        break;
                    }
                }
                if (t <= stops[0].t) col = stops[0].c;
                for (int x = 0; x < Width; x++) _px[y * Width + x] = col;
            }
        }

        public void VerticalGradient(Color bottom, Color top)
        {
            for (int y = 0; y < Height; y++)
            {
                var c = Color.Lerp(bottom, top, y / (float)(Height - 1));
                for (int x = 0; x < Width; x++) _px[y * Width + x] = c;
            }
        }

        public Texture2D ToTexture(bool repeat = false, bool mipmaps = false)
        {
            var tex = new Texture2D(Width, Height, TextureFormat.RGBA32, mipmaps)
            {
                wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                name = "Veliqora_Procedural",
            };
            tex.SetPixels(_px);
            tex.Apply(mipmaps, false);
            return tex;
        }

        public Sprite ToSprite(Vector4 border = default, float pixelsPerUnit = 100f)
        {
            var tex = ToTexture();
            return Sprite.Create(tex, new Rect(0, 0, Width, Height), new Vector2(0.5f, 0.5f), pixelsPerUnit, 0, SpriteMeshType.FullRect, border);
        }

        // ---------------- SDF primitives (pixel space) ----------------

        public static float Circle(Vector2 p, Vector2 c, float r) => (p - c).magnitude - r;

        public static float RoundedRect(Vector2 p, Rect r, float radius)
        {
            var c = r.center;
            var h = new Vector2(r.width * 0.5f - radius, r.height * 0.5f - radius);
            var q = new Vector2(Mathf.Abs(p.x - c.x) - h.x, Mathf.Abs(p.y - c.y) - h.y);
            return new Vector2(Mathf.Max(q.x, 0), Mathf.Max(q.y, 0)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0) - radius;
        }

        public static float Segment(Vector2 p, Vector2 a, Vector2 b, float halfWidth)
        {
            var pa = p - a;
            var ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Mathf.Max(1e-5f, Vector2.Dot(ba, ba)));
            return (pa - ba * h).magnitude - halfWidth;
        }

        /// <summary>Arc stroke between angles (radians, CCW from +X).</summary>
        public static float Arc(Vector2 p, Vector2 c, float radius, float a0, float a1, float halfWidth)
        {
            var d = p - c;
            float ang = Mathf.Atan2(d.y, d.x);
            float span = Mathf.Repeat(a1 - a0, Mathf.PI * 2f);
            float rel = Mathf.Repeat(ang - a0, Mathf.PI * 2f);
            if (rel <= span) return Mathf.Abs(d.magnitude - radius) - halfWidth;
            var e0 = c + new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * radius;
            var e1 = c + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * radius;
            return Mathf.Min((p - e0).magnitude, (p - e1).magnitude) - halfWidth;
        }

        /// <summary>Signed distance to a simple polygon (negative inside).</summary>
        public static float Polygon(Vector2 p, Vector2[] v)
        {
            float d = Vector2.Dot(p - v[0], p - v[0]);
            float s = 1f;
            for (int i = 0, j = v.Length - 1; i < v.Length; j = i, i++)
            {
                var e = v[j] - v[i];
                var w = p - v[i];
                var b = w - e * Mathf.Clamp01(Vector2.Dot(w, e) / Mathf.Max(1e-5f, Vector2.Dot(e, e)));
                d = Mathf.Min(d, Vector2.Dot(b, b));
                bool c1 = p.y >= v[i].y, c2 = p.y < v[j].y, c3 = e.x * w.y > e.y * w.x;
                if ((c1 && c2 && c3) || (!c1 && !c2 && !c3)) s = -s;
            }
            return s * Mathf.Sqrt(d);
        }

        public static Vector2[] Star(Vector2 c, float outer, float inner, int points = 5, float rotationDeg = 90f)
        {
            var v = new Vector2[points * 2];
            for (int i = 0; i < v.Length; i++)
            {
                float a = (rotationDeg + i * 180f / points) * Mathf.Deg2Rad;
                float r = i % 2 == 0 ? outer : inner;
                v[i] = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
            }
            return v;
        }

        /// <summary>Teardrop pointing up: circle bottom blended into a point.</summary>
        public static float Droplet(Vector2 p, Vector2 center, float r)
        {
            var q = p - center;
            float circle = q.magnitude - r;
            // Cone from the circle's tangent points up to the tip.
            var tip = new Vector2(0, r * 2.1f);
            var left = new Vector2(-r * 0.86f, r * 0.5f);
            var right = new Vector2(r * 0.86f, r * 0.5f);
            float tri = Polygon(q, new[] { left, right, tip });
            return Mathf.Min(circle, tri);
        }

        public static float Union(float a, float b) => Mathf.Min(a, b);
        public static float Subtract(float a, float b) => Mathf.Max(a, -b);
    }
}
