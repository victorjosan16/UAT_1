using UnityEngine;

namespace VoxelBloom
{
    /// <summary>
    /// Tiny anti-aliased raster painter based on signed distance shapes. Used to generate every UI
    /// sprite at runtime (icons, rounded panels, ribbons) so the build needs no external art packs.
    /// </summary>
    public sealed class IconPainter
    {
        public readonly int W, H;
        readonly Color[] px;

        public IconPainter(int w, int h) { W = w; H = h; px = new Color[w * h]; }

        public Texture2D ToTexture(string name, bool mip = false)
        {
            var t = new Texture2D(W, H, TextureFormat.RGBA32, mip) { name = name, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            t.SetPixels(px);
            t.Apply(mip, true);
            return t;
        }

        // ------------------------------------------------------------- compositing
        void Blend(int x, int y, Color c, float cov)
        {
            if (cov <= 0f || x < 0 || y < 0 || x >= W || y >= H) return;
            int i = y * W + x;
            float a = c.a * Mathf.Clamp01(cov);
            Color d = px[i];
            float oa = a + d.a * (1 - a);
            if (oa <= 0) return;
            px[i] = new Color((c.r * a + d.r * d.a * (1 - a)) / oa, (c.g * a + d.g * d.a * (1 - a)) / oa, (c.b * a + d.b * d.a * (1 - a)) / oa, oa);
        }

        delegate float Sdf(float x, float y);

        void Fill(Sdf sdf, Color c, float x0, float y0, float x1, float y1, Gradient2 grad = null)
        {
            int ix0 = Mathf.Max(0, Mathf.FloorToInt(x0) - 2), iy0 = Mathf.Max(0, Mathf.FloorToInt(y0) - 2);
            int ix1 = Mathf.Min(W - 1, Mathf.CeilToInt(x1) + 2), iy1 = Mathf.Min(H - 1, Mathf.CeilToInt(y1) + 2);
            for (int y = iy0; y <= iy1; y++)
            for (int x = ix0; x <= ix1; x++)
            {
                float d = sdf(x + 0.5f, y + 0.5f);
                float cov = Mathf.Clamp01(0.5f - d);
                if (cov <= 0) continue;
                Blend(x, y, grad != null ? grad.At(x, y, c) : c, cov);
            }
        }

        public sealed class Gradient2
        {
            public Color Top, Bottom; public float Y0, Y1;
            public Color At(int x, int y, Color fallback) => Color.Lerp(Bottom, Top, Mathf.InverseLerp(Y0, Y1, y));
        }

        public static Gradient2 Vertical(Color top, Color bottom, float y0, float y1) => new Gradient2 { Top = top, Bottom = bottom, Y0 = y0, Y1 = y1 };

        // ------------------------------------------------------------- primitives (y up, pixels)
        public void Circle(float cx, float cy, float r, Color c, Gradient2 g = null)
            => Fill((x, y) => Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r, c, cx - r, cy - r, cx + r, cy + r, g);

        public void Ring(float cx, float cy, float r, float thickness, Color c)
            => Fill((x, y) => Mathf.Abs(Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r) - thickness * 0.5f, c, cx - r - thickness, cy - r - thickness, cx + r + thickness, cy + r + thickness);

        public void RoundRect(float x0, float y0, float x1, float y1, float radius, Color c, Gradient2 g = null)
        {
            float cx = (x0 + x1) * 0.5f, cy = (y0 + y1) * 0.5f, hx = (x1 - x0) * 0.5f, hy = (y1 - y0) * 0.5f;
            radius = Mathf.Min(radius, Mathf.Min(hx, hy));
            Fill((x, y) =>
            {
                float qx = Mathf.Abs(x - cx) - hx + radius, qy = Mathf.Abs(y - cy) - hy + radius;
                float ox = Mathf.Max(qx, 0), oy = Mathf.Max(qy, 0);
                return Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(qx, qy), 0) - radius;
            }, c, x0, y0, x1, y1, g);
        }

        /// <summary>Thick line segment with round caps.</summary>
        public void Line(float ax, float ay, float bx, float by, float thickness, Color c)
        {
            float r = thickness * 0.5f;
            Fill((x, y) =>
            {
                float pax = x - ax, pay = y - ay, bax = bx - ax, bay = by - ay;
                float h = Mathf.Clamp01((pax * bax + pay * bay) / (bax * bax + bay * bay + 1e-6f));
                float dx = pax - bax * h, dy = pay - bay * h;
                return Mathf.Sqrt(dx * dx + dy * dy) - r;
            }, c, Mathf.Min(ax, bx) - r, Mathf.Min(ay, by) - r, Mathf.Max(ax, bx) + r, Mathf.Max(ay, by) + r);
        }

        public void Polyline(Color c, float thickness, params float[] pts)
        {
            for (int i = 0; i + 3 < pts.Length; i += 2) Line(pts[i], pts[i + 1], pts[i + 2], pts[i + 3], thickness, c);
        }

        /// <summary>Filled convex or concave polygon (even-odd) with AA via distance to edges.</summary>
        public void Polygon(Color c, float[] pts, Gradient2 g = null, float expand = 0f)
        {
            int n = pts.Length / 2;
            float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
            for (int i = 0; i < n; i++) { x0 = Mathf.Min(x0, pts[2 * i]); x1 = Mathf.Max(x1, pts[2 * i]); y0 = Mathf.Min(y0, pts[2 * i + 1]); y1 = Mathf.Max(y1, pts[2 * i + 1]); }
            Fill((x, y) =>
            {
                float d = float.MaxValue; bool inside = false;
                for (int i = 0, j = n - 1; i < n; j = i++)
                {
                    float xi = pts[2 * i], yi = pts[2 * i + 1], xj = pts[2 * j], yj = pts[2 * j + 1];
                    float ex = xj - xi, ey = yj - yi, wx = x - xi, wy = y - yi;
                    float h = Mathf.Clamp01((wx * ex + wy * ey) / (ex * ex + ey * ey + 1e-6f));
                    float dx = wx - ex * h, dy = wy - ey * h;
                    d = Mathf.Min(d, dx * dx + dy * dy);
                    if (((yi > y) != (yj > y)) && (x < (xj - xi) * (y - yi) / (yj - yi + 1e-9f) + xi)) inside = !inside;
                }
                return (inside ? -Mathf.Sqrt(d) : Mathf.Sqrt(d)) - expand;
            }, c, x0 - expand, y0 - expand, x1 + expand, y1 + expand, g);
        }

        public static float[] Star(float cx, float cy, float r, float inner = 0.48f, int points = 5)
        {
            var p = new float[points * 4];
            for (int i = 0; i < points * 2; i++)
            {
                float a = Mathf.PI / 2 + i * Mathf.PI / points;
                float rr = i % 2 == 0 ? r : r * inner;
                p[2 * i] = cx + Mathf.Cos(a) * rr;
                p[2 * i + 1] = cy + Mathf.Sin(a) * rr;
            }
            return p;
        }

        /// <summary>Soft blurred rounded rectangle (drop shadows).</summary>
        public void SoftRect(float x0, float y0, float x1, float y1, float radius, float blur, Color c)
        {
            float cx = (x0 + x1) * 0.5f, cy = (y0 + y1) * 0.5f, hx = (x1 - x0) * 0.5f, hy = (y1 - y0) * 0.5f;
            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float qx = Mathf.Abs(x + 0.5f - cx) - hx + radius, qy = Mathf.Abs(y + 0.5f - cy) - hy + radius;
                float d = Mathf.Sqrt(Mathf.Max(qx, 0) * Mathf.Max(qx, 0) + Mathf.Max(qy, 0) * Mathf.Max(qy, 0)) + Mathf.Min(Mathf.Max(qx, qy), 0) - radius;
                float a = 1f - Mathf.SmoothStep(-blur, blur, d);
                Blend(x, y, c, a);
            }
        }
    }
}
