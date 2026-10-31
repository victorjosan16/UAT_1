using System;
using UnityEngine;
using Veliqora.UI;

namespace Veliqora.Presentation
{
    /// <summary>
    /// Twelve ORIGINAL dark-fantasy backdrops, painted procedurally (no external images): aurora, crystal cavern,
    /// alchemist lab, glowing forest, neon falls, nebula, abyss, ember peaks, moonlit citadel, frost aurora,
    /// sunken ruins, starlit void. Kept low-contrast in the board area so neon liquids stay readable.
    /// Pure managed code (runs in the offline art preview too).
    /// </summary>
    public static class DarkScenes
    {
        /// <summary>The 12 selectable themes of the design brief, in menu order.</summary>
        public static readonly string[] Ids =
        {
            "midnight_forest", "neon_lab", "crystal_cave", "moonlight_falls", "dark_royal", "mystic_garden",
            "deep_ocean", "galaxy_flow", "enchanted_castle", "volcanic_glow", "cyber_neon", "aurora_night",
        };

        public static PixelPainter Paint(string scene, int w = 540, int h = 960)
        {
            var p = new PixelPainter(w, h, Color.black);
            var rng = new System.Random(Hash(scene));
            switch (scene)
            {
                case "aurora_night": Aurora(p, rng, C("#2BFFB0"), C("#FF3FD8"), C("#0B1B3A"), C("#03050F")); break;
                case "crystal_cave": CrystalCave(p, rng); break;
                case "neon_lab": AlchemyLab(p, rng); break;
                case "midnight_forest": GlowForest(p, rng); break;
                case "moonlight_falls": NeonFalls(p, rng); break;
                case "galaxy_flow": Nebula(p, rng); break;
                case "deep_ocean": Abyss(p, rng); break;
                case "volcanic_glow": EmberPeaks(p, rng); break;
                case "enchanted_castle": MoonCitadel(p, rng); break;
                case "dark_royal": DarkRoyal(p, rng); break;
                case "mystic_garden": MysticGarden(p, rng); break;
                case "cyber_neon": CyberNeon(p, rng); break;
                default: StarlitVoid(p, rng); break;
            }
            Vignette(p, 0.55f);
            return p;
        }

        // ---------------- helpers ----------------

        private static Color C(string hex) => UIStyle.Hex(hex);
        private static Color A(Color c, float a) { c.a = a; return c; }

        private static int Hash(string s)
        {
            unchecked { int h = 23; foreach (char ch in s) h = h * 31 + ch; return h; }
        }

        private static float R(System.Random r) => (float)r.NextDouble();

        private static float Noise1(float x, int seed)
        {
            int i = Mathf.FloorToInt(x);
            float f = x - i, u = f * f * (3 - 2 * f);
            return Mathf.Lerp(H1(i, seed), H1(i + 1, seed), u);
        }

        private static float H1(int x, int seed)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + seed * 668265263);
                h = (h ^ (h >> 13)) * 1274126177u;
                return ((h ^ (h >> 16)) & 0xFFFFFF) / (float)0xFFFFFF;
            }
        }

        private static float H2(int x, int y, int seed) => H1(x * 7919 + y * 104729, seed);

        private static float Noise2(float x, float y, int seed)
        {
            int ix = Mathf.FloorToInt(x), iy = Mathf.FloorToInt(y);
            float fx = x - ix, fy = y - iy;
            float ux = fx * fx * (3 - 2 * fx), uy = fy * fy * (3 - 2 * fy);
            float a = Mathf.Lerp(H2(ix, iy, seed), H2(ix + 1, iy, seed), ux);
            float b = Mathf.Lerp(H2(ix, iy + 1, seed), H2(ix + 1, iy + 1, seed), ux);
            return Mathf.Lerp(a, b, uy);
        }

        private static float Fbm2(float x, float y, int seed, int oct = 4)
        {
            float v = 0, amp = 0.5f, f = 1;
            for (int o = 0; o < oct; o++) { v += amp * Noise2(x * f, y * f, seed + o * 31); f *= 2; amp *= 0.5f; }
            return v;
        }

        private static float Fbm1(float x, int seed, int oct = 4)
        {
            float v = 0, amp = 0.5f, f = 1;
            for (int o = 0; o < oct; o++) { v += amp * Noise1(x * f, seed + o * 13); f *= 2; amp *= 0.5f; }
            return v;
        }

        private static void Stars(PixelPainter p, System.Random r, int count, float minY, float maxBright = 1f)
        {
            for (int i = 0; i < count; i++)
            {
                var c = new Vector2(R(r) * p.Width, minY + R(r) * (p.Height - minY));
                float b = (0.3f + R(r) * 0.7f) * maxBright;
                p.Add((int)c.x, (int)c.y, new Color(1, 1, 1, b));
                if (R(r) < 0.12f) p.Glow(c, 3 + R(r) * 4, new Color(0.8f, 0.85f, 1f, b * 0.6f));
            }
        }

        /// <summary>Fills below a ridge line y(x) with a color (silhouettes).</summary>
        private static void Silhouette(PixelPainter p, Func<int, float> ridge, Color col)
        {
            for (int x = 0; x < p.Width; x++)
            {
                float top = ridge(x);
                int ty = Mathf.Min(p.Height, (int)top);
                for (int y = 0; y < ty; y++) p.Blend(x, y, col);
                if (ty >= 0 && ty < p.Height) p.Blend(x, ty, A(col, col.a * (top - ty)));
            }
        }

        private static void Poly(PixelPainter p, Vector2[] v, Color col)
        {
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            foreach (var q in v) { minX = Mathf.Min(minX, q.x); minY = Mathf.Min(minY, q.y); maxX = Mathf.Max(maxX, q.x); maxY = Mathf.Max(maxY, q.y); }
            p.Fill(q => PixelPainter.Polygon(q, v), col, null, new RectInt((int)minX - 2, (int)minY - 2, (int)(maxX - minX) + 5, (int)(maxY - minY) + 5));
        }

        private static void Vignette(PixelPainter p, float strength)
        {
            var c = new Vector2(p.Width * 0.5f, p.Height * 0.5f);
            float maxD = c.magnitude;
            var px = p.Pixels;
            for (int y = 0; y < p.Height; y++)
                for (int x = 0; x < p.Width; x++)
                {
                    float d = (new Vector2(x, y) - c).magnitude / maxD;
                    float k = 1f - strength * Mathf.Pow(Mathf.Clamp01((d - 0.45f) / 0.55f), 1.5f);
                    var col = px[y * p.Width + x];
                    px[y * p.Width + x] = new Color(col.r * k, col.g * k, col.b * k, col.a);
                }
        }

        // ---------------- scenes ----------------

        private static void Aurora(PixelPainter p, System.Random r, Color c1, Color c2, Color skyTop, Color skyBottom, bool frost = false)
        {
            int w = p.Width, h = p.Height;
            p.Gradient((0f, skyBottom), (0.45f, Color.Lerp(skyBottom, skyTop, 0.5f)), (1f, Color.Lerp(skyTop, Color.black, 0.35f)));
            Stars(p, r, 260, h * 0.35f);
            // Aurora curtains: bright lower edge, rays fading upward.
            for (int band = 0; band < 3; band++)
            {
                float baseY = h * (0.62f + band * 0.07f);
                float phase = band * 1.7f;
                var col = band == 1 ? c2 : c1;
                for (int x = 0; x < w; x++)
                {
                    float cy = baseY + Mathf.Sin(x * 0.010f + phase) * h * 0.05f + (Fbm1(x * 0.01f, band + 3) - 0.5f) * h * 0.08f;
                    float rays = 0.55f + 0.45f * Mathf.Sin(x * 0.21f + band) * Fbm1(x * 0.05f, 9 + band);
                    float intensity = (band == 1 ? 0.55f : 0.75f) * Mathf.Clamp01(Fbm1(x * 0.006f, 21 + band) * 1.6f - 0.2f);
                    for (int dy = -18; dy < 230; dy++)
                    {
                        int y = (int)cy + dy;
                        if (y < 0 || y >= h) continue;
                        float a = dy < 0 ? Mathf.Pow(1f + dy / 18f, 2f) : Mathf.Pow(1f - dy / 230f, 1.8f) * rays;
                        var c = Color.Lerp(col, dy > 120 ? c2 : col, dy / 230f * 0.6f);
                        p.Add(x, y, A(c, a * intensity * 0.85f));
                    }
                }
            }
            // Mountains
            var far = frost ? C("#16304F") : C("#0A1326");
            Silhouette(p, x => h * 0.30f + Mathf.Pow(1f - Mathf.Abs(Fbm1(x * 0.011f, 4) * 2f - 1f), 1.5f) * h * 0.14f, far);
            if (frost)
            {
                // icy rim light on the far range
                for (int x = 0; x < w; x++)
                {
                    float top = h * 0.30f + Mathf.Pow(1f - Mathf.Abs(Fbm1(x * 0.011f, 4) * 2f - 1f), 1.5f) * h * 0.14f;
                    for (int k = 0; k < 4; k++) p.Add(x, (int)top - k, new Color(0.7f, 0.9f, 1f, 0.35f * (1 - k / 4f)));
                }
            }
            Silhouette(p, x => h * 0.24f + Fbm1(x * 0.02f, 7) * h * 0.06f, C("#04070F"));
            // Lake reflection of the aurora
            for (int y = 0; y < (int)(h * 0.2f); y++)
                for (int x = 0; x < w; x += 1)
                {
                    float shimmer = 0.5f + 0.5f * Mathf.Sin(y * 0.9f + x * 0.03f);
                    float a = 0.10f * (1f - y / (h * 0.2f)) * shimmer;
                    p.Add(x, y, A(Color.Lerp(c1, c2, x / (float)w), a));
                }
            if (frost) for (int i = 0; i < 160; i++) p.Add(r.Next(w), r.Next((int)(h * 0.22f)), new Color(0.8f, 0.95f, 1f, 0.6f * R(r)));
        }

        private static void CrystalCave(PixelPainter p, System.Random r)
        {
            int w = p.Width, h = p.Height;
            p.Gradient((0f, C("#07030F")), (0.5f, C("#130A2A")), (1f, C("#090414")));
            var opening = new Vector2(w * 0.5f, h * 0.6f);
            p.Glow(opening, w * 0.75f, A(C("#2FD9FF"), 0.55f), 1.6f);
            p.Glow(opening + new Vector2(0, -h * 0.05f), w * 0.4f, A(C("#9B4DFF"), 0.35f));
            // hanging crystals inside the opening (glowing icicles)
            for (int i = 0; i < 26; i++)
            {
                float x = w * (0.2f + R(r) * 0.6f);
                float len = h * (0.06f + R(r) * 0.2f);
                float top = h * (0.8f + R(r) * 0.06f);
                float hw = 6 + R(r) * 12;
                var col = R(r) < 0.6f ? C("#5CE8FF") : C("#C77DFF");
                Poly(p, new[] { new Vector2(x - hw, top), new Vector2(x + hw, top), new Vector2(x + R(r) * 4 - 2, top - len) }, A(Color.Lerp(col, Color.white, 0.25f), 0.85f));
                p.Glow(new Vector2(x, top - len * 0.5f), len * 0.6f, A(col, 0.18f));
            }
            // rock arch framing (everything outside an irregular ellipse)
            var rock = C("#0D0719");
            var rockLight = C("#1E1236");
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float dx = (x - w * 0.5f) / (w * 0.46f), dy = (y - h * 0.56f) / (h * 0.32f);
                    float edge = Mathf.Sqrt(dx * dx + dy * dy) + (Fbm2(x * 0.02f, y * 0.02f, 5) - 0.5f) * 0.35f;
                    if (edge > 1f) p.Blend(x, y, Color.Lerp(rockLight, rock, Mathf.Clamp01((edge - 1f) * 3f)));
                }
            // purple crystals on the arch
            for (int i = 0; i < 22; i++)
            {
                float a = R(r) * Mathf.PI;
                var basePt = new Vector2(w * 0.5f + Mathf.Cos(a) * w * 0.47f, h * 0.56f + Mathf.Sin(a) * h * 0.33f);
                float len = 30 + R(r) * 70, hw = 8 + R(r) * 10;
                var dir = new Vector2(-Mathf.Cos(a) * 0.4f, -1f);
                var tip = basePt + dir.normalized * len;
                var col = R(r) < 0.5f ? C("#E070FF") : C("#5CE8FF");
                Poly(p, new[] { basePt + new Vector2(-hw, 0), basePt + new Vector2(hw, 0), tip }, A(col, 0.9f));
                p.Glow(Vector2.Lerp(basePt, tip, 0.5f), len * 0.8f, A(col, 0.22f));
            }
            // floor path
            Poly(p, new[] { new Vector2(w * 0.18f, 0), new Vector2(w * 0.82f, 0), new Vector2(w * 0.56f, h * 0.36f), new Vector2(w * 0.46f, h * 0.36f) }, A(C("#2A1748"), 0.85f));
            for (int i = 0; i < 40; i++) p.Glow(new Vector2(w * (0.25f + R(r) * 0.5f), h * R(r) * 0.3f), 4 + R(r) * 8, A(C("#C77DFF"), 0.35f));
        }

        private static void AlchemyLab(PixelPainter p, System.Random r)
        {
            int w = p.Width, h = p.Height;
            p.Gradient((0f, C("#0A0516")), (0.55f, C("#1A0B30")), (1f, C("#08040F")));
            var vp = new Vector2(w * 0.5f, h * 0.55f);
            // back wall neon bars
            for (int i = 0; i < 6; i++)
            {
                float x = vp.x - 60 + i * 24;
                p.Fill(q => PixelPainter.RoundedRect(q, new Rect(x, vp.y - 40, 6, 110), 3), A(C("#FF7BF0"), 0.95f), null, new RectInt((int)x - 2, (int)vp.y - 45, 12, 120));
            }
            p.Glow(vp + new Vector2(0, 15), 260, A(C("#C040FF"), 0.5f), 1.4f);
            // perspective floor + counters
            Poly(p, new[] { new Vector2(0, 0), new Vector2(w, 0), new Vector2(vp.x + 70, vp.y - 50), new Vector2(vp.x - 70, vp.y - 50) }, C("#150A26"));
            Poly(p, new[] { new Vector2(0, h * 0.06f), new Vector2(vp.x - 75, vp.y - 45), new Vector2(vp.x - 75, vp.y - 25), new Vector2(0, h * 0.42f) }, C("#1D1033"));
            Poly(p, new[] { new Vector2(w, h * 0.06f), new Vector2(vp.x + 75, vp.y - 45), new Vector2(vp.x + 75, vp.y - 25), new Vector2(w, h * 0.42f) }, C("#1D1033"));
            // floor reflection line
            for (int y = 0; y < (int)(vp.y - 50); y++) p.Add((int)vp.x + r.Next(-2, 3), y, A(C("#FF7BF0"), 0.08f));
            // side neon tubes
            foreach (float tx in new[] { w * 0.08f, w * 0.92f, w * 0.26f, w * 0.74f })
            {
                bool nearT = tx < w * 0.15f || tx > w * 0.85f;
                float top = nearT ? h * 0.92f : h * 0.75f, bottom = nearT ? h * 0.48f : h * 0.55f;
                p.Fill(q => PixelPainter.RoundedRect(q, new Rect(tx - 4, bottom, 8, top - bottom), 4), A(C("#FF8DF5"), 0.9f), null, new RectInt((int)tx - 8, (int)bottom - 2, 16, (int)(top - bottom) + 4));
                p.Glow(new Vector2(tx, (top + bottom) * 0.5f), (top - bottom) * 0.6f, A(C("#E040FF"), 0.28f));
            }
            // glowing flasks on counters
            for (int side = -1; side <= 1; side += 2)
                for (int i = 0; i < 9; i++)
                {
                    float t = i / 9f;
                    float x = side < 0 ? Mathf.Lerp(10, vp.x - 90, t) : Mathf.Lerp(w - 10, vp.x + 90, t);
                    float y = Mathf.Lerp(h * 0.40f, vp.y - 28, t);
                    float s = Mathf.Lerp(22, 6, t);
                    var col = R(r) < 0.7f ? C("#FF4FD8") : C("#7B5CFF");
                    var fc = new Vector2(x, y + s);
                    p.Fill(q => Mathf.Min(PixelPainter.Circle(q, fc, s), PixelPainter.RoundedRect(q, new Rect(x - s * 0.25f, y + s, s * 0.5f, s * 1.1f), 2)), A(C("#3A2A55"), 0.9f), null, new RectInt((int)(x - s - 2), (int)(y - 2), (int)(s * 2 + 4), (int)(s * 3.4f)));
                    p.Fill(q => PixelPainter.Circle(q, fc + new Vector2(0, -s * 0.25f), s * 0.75f), A(col, 0.9f), null, new RectInt((int)(x - s), (int)y, (int)(s * 2), (int)(s * 2)));
                    p.Glow(fc, s * 2.4f, A(col, 0.3f));
                }
            // ceiling pipes
            for (int i = 0; i < 3; i++)
            {
                float y0 = h * (0.9f + i * 0.03f);
                p.Fill(q => PixelPainter.Segment(q, new Vector2(-10, y0), new Vector2(vp.x - 40 + i * 40, vp.y + 90), 8 - i * 2), C("#24163A"), null, new RectInt(0, (int)(vp.y + 70), w, (int)(y0 - vp.y)));
            }
            for (int i = 0; i < 50; i++) p.Glow(new Vector2(R(r) * w, h * (0.5f + R(r) * 0.45f)), 2 + R(r) * 3, A(C("#8FE9FF"), 0.5f));
        }

        private static void GlowForest(PixelPainter p, System.Random r)
        {
            int w = p.Width, h = p.Height;
            p.Gradient((0f, C("#030B0B")), (0.5f, C("#0B2626")), (1f, C("#071817")));
            p.Glow(new Vector2(w * 0.55f, h * 0.7f), w * 0.7f, A(C("#3FE0C8"), 0.25f), 1.5f);
            // trunks, far to near
            for (int layer = 0; layer < 3; layer++)
            {
                var col = Color.Lerp(C("#0E2E2E"), C("#020707"), layer / 2f);
                int n = 7 - layer * 2;
                for (int i = 0; i < n; i++)
                {
                    float x = R(r) * w;
                    float bw = 8 + layer * 10 + R(r) * 8;
                    float lean = (R(r) - 0.5f) * 40;
                    Poly(p, new[] { new Vector2(x - bw, 0), new Vector2(x + bw, 0), new Vector2(x + lean + bw * 0.4f, h), new Vector2(x + lean - bw * 0.4f, h) }, col);
                }
            }
            // canopy
            for (int i = 0; i < 18; i++)
            {
                var c = new Vector2(R(r) * w, h * (0.82f + R(r) * 0.2f));
                float rad = 50 + R(r) * 80;
                p.Fill(q => PixelPainter.Circle(q, c, rad), A(C("#061615"), 0.92f), null, new RectInt((int)(c.x - rad), (int)(c.y - rad), (int)rad * 2, (int)rad * 2));
            }
            // glowing path (winding)
            // path ribbon (perspective: wide and bright near the viewer, narrowing into the woods)
            for (int y = 0; y < (int)(h * 0.5f); y++)
            {
                float t = y / (h * 0.5f);
                float cx = w * 0.5f + Mathf.Sin(t * 4.2f + 0.6f) * w * 0.22f * (1 - t * 0.5f);
                float hw = Mathf.Lerp(w * 0.24f, w * 0.02f, Mathf.Pow(t, 0.7f));
                for (int x = (int)(cx - hw); x <= (int)(cx + hw); x++)
                    p.Blend(x, y, A(C("#2A1450"), 0.55f * (1 - Mathf.Abs(x - cx) / hw)));
            }
            for (int i = 0; i < 700; i++)
            {
                float t = Mathf.Pow(R(r), 1.4f);
                float y = t * h * 0.5f;
                float cx = w * 0.5f + Mathf.Sin(t * 4.2f + 0.6f) * w * 0.22f * (1 - t * 0.5f);
                float spread = Mathf.Lerp(w * 0.22f, w * 0.02f, Mathf.Pow(t, 0.7f));
                float x = cx + (R(r) - 0.5f) * 2 * spread;
                var col = R(r) < 0.65f ? C("#C46BFF") : C("#4FA8FF");
                p.Glow(new Vector2(x, y), Mathf.Lerp(11, 2, t) * (0.6f + R(r) * 0.8f), A(col, 0.6f));
            }
            // foliage with glowing leaves
            for (int side = 0; side < 2; side++)
                for (int i = 0; i < 9; i++)
                {
                    var c = new Vector2(side == 0 ? R(r) * w * 0.3f : w - R(r) * w * 0.3f, h * R(r) * 0.32f);
                    float rad = 30 + R(r) * 50;
                    p.Fill(q => PixelPainter.Circle(q, c, rad), A(C("#04100F"), 0.95f), null, new RectInt((int)(c.x - rad), (int)(c.y - rad), (int)rad * 2, (int)rad * 2));
                }
            for (int i = 0; i < 70; i++)
            {
                var col = R(r) < 0.5f ? C("#FFB347") : C("#4DFFE0");
                p.Glow(new Vector2(R(r) * w, h * R(r) * 0.75f), 3 + R(r) * 5, A(col, 0.7f));
            }
        }

        private static void NeonFalls(PixelPainter p, System.Random r)
        {
            int w = p.Width, h = p.Height;
            p.Gradient((0f, C("#03082A")), (0.5f, C("#0A1A5C")), (1f, C("#050C33")));
            Stars(p, r, 120, h * 0.6f);
            var moon = new Vector2(w * 0.28f, h * 0.83f);
            p.Glow(moon, 140, A(C("#7FC8FF"), 0.45f));
            p.Fill(q => PixelPainter.Circle(q, moon, 42), C("#CFE9FF"), q => Color.Lerp(C("#9FD3FF"), C("#F0F8FF"), Mathf.Clamp01((q.y - moon.y + 42) / 84f)), new RectInt((int)moon.x - 44, (int)moon.y - 44, 88, 88));
            Silhouette(p, x => h * 0.5f + Mathf.Pow(1f - Mathf.Abs(Fbm1(x * 0.009f, 3) * 2 - 1), 1.4f) * h * 0.14f, C("#0F2470"));
            Silhouette(p, x => h * 0.42f + Fbm1(x * 0.014f, 8) * h * 0.1f, C("#0A1650"));
            // cliff on the right
            Poly(p, new[] { new Vector2(w * 0.5f, h * 0.38f), new Vector2(w * 0.52f, h * 0.62f), new Vector2(w * 0.68f, h * 0.66f), new Vector2(w, h * 0.72f), new Vector2(w, h * 0.32f) }, C("#060B2C"));
            // magenta waterfall
            float wx0 = w * 0.6f, wx1 = w * 0.72f;
            for (int y = (int)(h * 0.38f); y < (int)(h * 0.645f); y++)
                for (int x = (int)wx0; x < (int)wx1; x++)
                {
                    float streak = 0.75f + 0.25f * Mathf.Sin(x * 0.9f + y * 0.05f);
                    p.Blend(x, y, A(Color.Lerp(C("#FF3FD8"), C("#FF9BF0"), streak - 0.5f), 0.92f));
                }
            p.Glow(new Vector2((wx0 + wx1) * 0.5f, h * 0.5f), 170, A(C("#FF2FD0"), 0.35f));
            p.Glow(new Vector2((wx0 + wx1) * 0.5f, h * 0.38f), 120, A(C("#FF7BEF"), 0.4f));
            // lake + reflection
            p.Fill(q => q.y < h * 0.38f ? -1f : 1f, A(C("#071450"), 0.9f), null, new RectInt(0, 0, w, (int)(h * 0.39f)));
            for (int y = (int)(h * 0.2f); y < (int)(h * 0.38f); y++)
                for (int x = (int)wx0 - 10; x < (int)wx1 + 10; x++)
                    if (Mathf.Sin(y * 1.3f + x * 0.2f) > 0.2f) p.Add(x, y, A(C("#FF3FD8"), 0.35f * (y - h * 0.2f) / (h * 0.18f)));
            p.Glow(new Vector2(moon.x, h * 0.3f), 60, A(C("#7FC8FF"), 0.2f));
            // shores with blue lotus glows
            Silhouette(p, x => h * 0.16f + Fbm1(x * 0.02f, 12) * h * 0.06f, C("#030722"));
            for (int i = 0; i < 26; i++)
            {
                var c = new Vector2(R(r) < 0.5f ? R(r) * w * 0.35f : w - R(r) * w * 0.35f, h * (0.03f + R(r) * 0.18f));
                p.Glow(c, 10 + R(r) * 16, A(C("#5FB8FF"), 0.7f));
                p.Glow(c, 3, A(Color.white, 0.8f));
            }
        }

        private static void Nebula(PixelPainter p, System.Random r)
        {
            int w = p.Width, h = p.Height;
            p.Gradient((0f, C("#04030C")), (1f, C("#0A0620")));
            var px = p.Pixels;
            for (int y = 0; y < h; y += 1)
                for (int x = 0; x < w; x += 1)
                {
                    float n1 = Fbm2(x * 0.006f, y * 0.006f, 11, 5);
                    float n2 = Fbm2(x * 0.009f + 40, y * 0.009f, 23, 4);
                    float band = Mathf.Exp(-Mathf.Pow((y - h * 0.62f - (x - w * 0.5f) * 0.4f) / (h * 0.22f), 2f));
                    float a1 = Mathf.Clamp01((n1 - 0.45f) * 2.4f) * band;
                    float a2 = Mathf.Clamp01((n2 - 0.5f) * 2.4f) * band;
                    var c = px[y * w + x];
                    c.r += a1 * 0.55f + a2 * 0.15f; c.g += a1 * 0.12f + a2 * 0.35f; c.b += a1 * 0.7f + a2 * 0.8f;
                    px[y * w + x] = new Color(Mathf.Min(1, c.r), Mathf.Min(1, c.g), Mathf.Min(1, c.b), 1);
                }
            Stars(p, r, 420, 0f);
            // ringed planet
            var pc = new Vector2(w * 0.78f, h * 0.2f);
            p.Fill(q => PixelPainter.Circle(q, pc, 70), C("#1A1240"), q => Color.Lerp(C("#120A30"), C("#5A3FA8"), Mathf.Clamp01((q.x - pc.x + q.y - pc.y + 70) / 160f)), new RectInt((int)pc.x - 72, (int)pc.y - 72, 144, 144));
            p.Fill(q => Mathf.Abs(PixelPainter.Circle(new Vector2(q.x, pc.y + (q.y - pc.y) * 3.2f), pc, 110)) - 3f, A(C("#B48CFF"), 0.6f), null, new RectInt((int)pc.x - 120, (int)pc.y - 40, 240, 80));
        }

        private static void Abyss(PixelPainter p, System.Random r)
        {
            int w = p.Width, h = p.Height;
            p.Gradient((0f, C("#010510")), (0.6f, C("#04233F")), (1f, C("#0A3E66")));
            // light shafts
            for (int i = 0; i < 5; i++)
            {
                float x = w * (0.15f + i * 0.18f) + R(r) * 30;
                Poly(p, new[] { new Vector2(x - 20, h), new Vector2(x + 30, h), new Vector2(x + 120, h * 0.25f), new Vector2(x + 30, h * 0.25f) }, A(C("#5FD6FF"), 0.06f));
            }
            // kelp silhouettes
            for (int i = 0; i < 12; i++)
            {
                float x0 = R(r) * w, height = h * (0.15f + R(r) * 0.35f), ph = R(r) * 6;
                for (int y = 0; y < (int)height; y += 2)
                {
                    float x = x0 + Mathf.Sin(y * 0.03f + ph) * 14;
                    float hw = Mathf.Lerp(6, 1.5f, y / height);
                    for (int dx = (int)-hw; dx <= (int)hw; dx++) { p.Blend((int)x + dx, y, C("#021018")); p.Blend((int)x + dx, y + 1, C("#021018")); }
                }
            }
            // jellyfish
            for (int i = 0; i < 4; i++)
            {
                var c = new Vector2(w * (0.15f + R(r) * 0.7f), h * (0.5f + R(r) * 0.4f));
                float rad = 16 + R(r) * 18;
                var col = i % 2 == 0 ? C("#FF6BE8") : C("#6BE8FF");
                p.Glow(c, rad * 3, A(col, 0.25f));
                p.Fill(q => q.y < c.y ? 1f : PixelPainter.Circle(q, c, rad), A(Color.Lerp(col, Color.white, 0.3f), 0.75f), null, new RectInt((int)(c.x - rad), (int)c.y, (int)rad * 2, (int)rad + 2));
                for (int t = 0; t < 4; t++)
                {
                    float tx = c.x - rad * 0.6f + t * rad * 0.4f;
                    for (int y = 0; y < (int)(rad * 3); y++) p.Add((int)(tx + Mathf.Sin(y * 0.15f + t) * 3), (int)(c.y - y), A(col, 0.35f * (1 - y / (rad * 3))));
                }
            }
            for (int i = 0; i < 120; i++) p.Glow(new Vector2(R(r) * w, R(r) * h), 1.5f + R(r) * 3, A(R(r) < 0.5f ? C("#7FFFE0") : C("#8FB8FF"), 0.55f));
            Silhouette(p, x => h * 0.06f + Fbm1(x * 0.02f, 33) * h * 0.06f, C("#01070D"));
        }

        private static void EmberPeaks(PixelPainter p, System.Random r)
        {
            int w = p.Width, h = p.Height;
            p.Gradient((0f, C("#080303")), (0.45f, C("#3A0E06")), (0.62f, C("#1C0806")), (1f, C("#070202")));
            p.Glow(new Vector2(w * 0.5f, h * 0.48f), w * 0.8f, A(C("#FF5A1F"), 0.3f), 1.6f);
            Silhouette(p, x => h * 0.42f + Mathf.Pow(1f - Mathf.Abs(Fbm1(x * 0.01f, 5) * 2 - 1), 1.6f) * h * 0.1f, C("#140605"));
            // volcano
            Poly(p, new[] { new Vector2(w * 0.08f, h * 0.3f), new Vector2(w * 0.42f, h * 0.66f), new Vector2(w * 0.58f, h * 0.66f), new Vector2(w * 0.92f, h * 0.3f) }, C("#0B0303"));
            p.Glow(new Vector2(w * 0.5f, h * 0.66f), 120, A(C("#FF7A1F"), 0.75f));
            p.Glow(new Vector2(w * 0.5f, h * 0.72f), 200, A(C("#FF3A0F"), 0.3f));
            // lava river
            float x = w * 0.5f;
            for (int y = (int)(h * 0.66f); y > 0; y--)
            {
                x += Mathf.Sin(y * 0.02f) * 1.2f + (Fbm1(y * 0.03f, 4) - 0.5f) * 1.5f;
                float hw = Mathf.Lerp(14, 4, y / (h * 0.66f));
                for (int dx = (int)-hw; dx <= (int)hw; dx++) p.Blend((int)x + dx, y, Color.Lerp(C("#FFC23F"), C("#FF4A12"), Mathf.Abs(dx) / hw));
                if (y % 3 == 0) p.Glow(new Vector2(x, y), hw * 3, A(C("#FF5A1F"), 0.08f));
            }
            Silhouette(p, xx => h * 0.12f + Fbm1(xx * 0.02f, 18) * h * 0.08f, C("#050101"));
            for (int i = 0; i < 90; i++) p.Glow(new Vector2(R(r) * w, h * (0.2f + R(r) * 0.75f)), 1.5f + R(r) * 3, A(C("#FFA040"), 0.8f));
        }

        private static void MoonCitadel(PixelPainter p, System.Random r)
        {
            int w = p.Width, h = p.Height;
            p.Gradient((0f, C("#05050F")), (0.5f, C("#14103A")), (1f, C("#07061A")));
            Stars(p, r, 180, h * 0.45f);
            var moon = new Vector2(w * 0.5f, h * 0.74f);
            p.Glow(moon, 300, A(C("#B9A8FF"), 0.35f));
            p.Fill(q => PixelPainter.Circle(q, moon, 120), C("#E8E2FF"), q => Color.Lerp(C("#C7BDF5"), C("#FBF8FF"), Mathf.Clamp01((q.y - moon.y + 120) / 240f) * (0.8f + 0.2f * Noise2(q.x * 0.05f, q.y * 0.05f, 3))), new RectInt((int)moon.x - 122, (int)moon.y - 122, 244, 244));
            // hill and castle
            Silhouette(p, x => h * 0.38f + Mathf.Exp(-Mathf.Pow((x - w * 0.5f) / (w * 0.35f), 2)) * h * 0.08f, C("#07061A"));
            var castle = C("#060515");
            float baseY = h * 0.44f;
            void Tower(float cx, float tw, float th)
            {
                Poly(p, new[] { new Vector2(cx - tw, baseY - 10), new Vector2(cx + tw, baseY - 10), new Vector2(cx + tw, baseY + th), new Vector2(cx - tw, baseY + th) }, castle);
                Poly(p, new[] { new Vector2(cx - tw - 6, baseY + th), new Vector2(cx + tw + 6, baseY + th), new Vector2(cx, baseY + th + tw * 2.6f) }, castle);
                for (int k = 0; k < 3; k++) if (R(r) < 0.7f)
                {
                    var wc = new Vector2(cx + (R(r) - 0.5f) * tw, baseY + th * (0.3f + 0.2f * k));
                    p.Fill(q => PixelPainter.RoundedRect(q, new Rect(wc.x - 2.5f, wc.y - 4, 5, 8), 2), C("#FFD27A"), null, new RectInt((int)wc.x - 4, (int)wc.y - 6, 8, 12));
                    p.Glow(wc, 12, A(C("#FFB347"), 0.4f));
                }
            }
            Poly(p, new[] { new Vector2(w * 0.28f, baseY - 10), new Vector2(w * 0.72f, baseY - 10), new Vector2(w * 0.72f, baseY + 60), new Vector2(w * 0.28f, baseY + 60) }, castle);
            Tower(w * 0.3f, 18, 110); Tower(w * 0.42f, 14, 150); Tower(w * 0.5f, 22, 210); Tower(w * 0.6f, 15, 140); Tower(w * 0.7f, 18, 100);
            // mist band
            for (int y = (int)(h * 0.32f); y < (int)(h * 0.5f); y++)
                for (int x = 0; x < w; x++)
                {
                    float m = Fbm2(x * 0.01f, y * 0.03f, 41, 3);
                    float a = Mathf.Clamp01((m - 0.45f) * 2f) * 0.22f * (1 - Mathf.Abs(y - h * 0.41f) / (h * 0.09f));
                    if (a > 0) p.Add(x, y, A(C("#8E86D9"), a));
                }
            Silhouette(p, x => h * 0.18f + Fbm1(x * 0.015f, 52) * h * 0.08f, C("#030310"));
        }

        private static void DarkRoyal(PixelPainter p, System.Random r)
        {
            int w = p.Width, h = p.Height;
            p.Gradient((0f, C("#0A0410")), (0.5f, C("#2A0B2E")), (1f, C("#12051A")));
            // velvet banners
            for (int i = 0; i < 4; i++)
            {
                float x = i < 2 ? w * (0.06f + i * 0.14f) : w * (0.8f + (i - 2) * 0.14f);
                Poly(p, new[] { new Vector2(x - 26, h * 0.42f), new Vector2(x + 26, h * 0.42f), new Vector2(x + 26, h * 0.95f), new Vector2(x - 26, h * 0.95f) }, C("#4A0E2A"));
                Poly(p, new[] { new Vector2(x - 26, h * 0.42f), new Vector2(x + 26, h * 0.42f), new Vector2(x, h * 0.38f) }, C("#4A0E2A"));
                p.Fill(q => PixelPainter.Segment(q, new Vector2(x, h * 0.46f), new Vector2(x, h * 0.93f), 3), A(C("#D9A441"), 0.6f), null, new RectInt((int)x - 5, (int)(h * 0.45f), 10, (int)(h * 0.5f)));
            }
            // golden arches
            var gold = C("#E8B54A");
            for (int k = 0; k < 2; k++)
            {
                float rad = w * (0.34f - k * 0.08f);
                var c = new Vector2(w * 0.5f, h * 0.6f);
                p.Fill(q => q.y < c.y ? 1f : PixelPainter.Arc(q, c, rad, 0f, Mathf.PI, 4f - k), A(gold, 0.85f - k * 0.3f), null, new RectInt((int)(c.x - rad - 6), (int)c.y - 2, (int)(rad * 2) + 12, (int)rad + 8));
                p.Fill(q => PixelPainter.Segment(q, new Vector2(c.x - rad, c.y), new Vector2(c.x - rad, h * 0.3f), 4f - k), A(gold, 0.85f - k * 0.3f), null, new RectInt((int)(c.x - rad) - 6, (int)(h * 0.3f), 12, (int)(c.y - h * 0.3f) + 2));
                p.Fill(q => PixelPainter.Segment(q, new Vector2(c.x + rad, c.y), new Vector2(c.x + rad, h * 0.3f), 4f - k), A(gold, 0.85f - k * 0.3f), null, new RectInt((int)(c.x + rad) - 6, (int)(h * 0.3f), 12, (int)(c.y - h * 0.3f) + 2));
            }
            // chandelier glow + throne silhouette far back
            p.Glow(new Vector2(w * 0.5f, h * 0.86f), 200, A(C("#FFC56B"), 0.35f));
            for (int i = 0; i < 7; i++) p.Glow(new Vector2(w * (0.36f + i * 0.047f), h * 0.84f + (i % 2) * 8), 9, A(C("#FFE2A0"), 0.9f));
            Poly(p, new[] { new Vector2(w * 0.44f, h * 0.36f), new Vector2(w * 0.56f, h * 0.36f), new Vector2(w * 0.56f, h * 0.5f), new Vector2(w * 0.5f, h * 0.55f), new Vector2(w * 0.44f, h * 0.5f) }, C("#1A0618"));
            p.Glow(new Vector2(w * 0.5f, h * 0.45f), 90, A(C("#B0306A"), 0.25f));
            // perspective floor tiles
            Poly(p, new[] { new Vector2(0, 0), new Vector2(w, 0), new Vector2(w * 0.62f, h * 0.34f), new Vector2(w * 0.38f, h * 0.34f) }, C("#1C0820"));
            for (int i = -6; i <= 6; i++)
            {
                var a = new Vector2(w * 0.5f + i * w * 0.02f, h * 0.34f); var b = new Vector2(w * 0.5f + i * w * 0.16f, 0);
                p.Fill(q => PixelPainter.Segment(q, a, b, 0.8f), A(gold, 0.18f), null, new RectInt((int)Mathf.Min(a.x, b.x) - 2, 0, (int)Mathf.Abs(a.x - b.x) + 4, (int)(h * 0.35f)));
            }
            for (int i = 0; i < 70; i++) p.Glow(new Vector2(R(r) * w, h * (0.2f + R(r) * 0.75f)), 1.5f + R(r) * 2.5f, A(C("#FFD27A"), 0.55f));
        }

        private static void MysticGarden(PixelPainter p, System.Random r)
        {
            int w = p.Width, h = p.Height;
            p.Gradient((0f, C("#040A10")), (0.55f, C("#0E1E3A")), (1f, C("#060B1E")));
            Stars(p, r, 140, h * 0.6f);
            var moon = new Vector2(w * 0.74f, h * 0.86f);
            p.Glow(moon, 120, A(C("#B6C8FF"), 0.35f));
            p.Fill(q => PixelPainter.Circle(q, moon, 30), C("#E6ECFF"), null, new RectInt((int)moon.x - 32, (int)moon.y - 32, 64, 64));
            // hedges
            Silhouette(p, x => h * 0.42f + Fbm1(x * 0.02f, 71) * h * 0.06f, C("#071512"));
            // stone archway with glowing vines
            var c = new Vector2(w * 0.5f, h * 0.55f);
            float rad = w * 0.26f;
            p.Fill(q => q.y < c.y ? 1f : PixelPainter.Arc(q, c, rad, 0f, Mathf.PI, 9f), C("#1A2433"), null, new RectInt((int)(c.x - rad - 12), (int)c.y - 2, (int)(rad * 2) + 24, (int)rad + 14));
            Poly(p, new[] { new Vector2(c.x - rad - 9, h * 0.3f), new Vector2(c.x - rad + 9, h * 0.3f), new Vector2(c.x - rad + 9, c.y), new Vector2(c.x - rad - 9, c.y) }, C("#1A2433"));
            Poly(p, new[] { new Vector2(c.x + rad - 9, h * 0.3f), new Vector2(c.x + rad + 9, h * 0.3f), new Vector2(c.x + rad + 9, c.y), new Vector2(c.x + rad - 9, c.y) }, C("#1A2433"));
            for (int i = 0; i < 40; i++)
            {
                float a = R(r) * Mathf.PI;
                var pt = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * rad;
                p.Glow(pt, 6 + R(r) * 8, A(i % 2 == 0 ? C("#FF7BD5") : C("#7BD9FF"), 0.7f));
            }
            // glowing flower beds
            for (int i = 0; i < 90; i++)
            {
                var pt = new Vector2(R(r) * w, h * R(r) * 0.32f);
                var col = i % 3 == 0 ? C("#FF7BD5") : (i % 3 == 1 ? C("#7BD9FF") : C("#C9A2FF"));
                p.Glow(pt, 4 + R(r) * 7, A(col, 0.6f));
            }
            // fountain glow
            p.Glow(new Vector2(w * 0.5f, h * 0.2f), 110, A(C("#5FE0FF"), 0.3f));
            Silhouette(p, x => h * 0.1f + Fbm1(x * 0.03f, 72) * h * 0.04f, C("#030807"));
        }

        private static void CyberNeon(PixelPainter p, System.Random r)
        {
            int w = p.Width, h = p.Height;
            p.Gradient((0f, C("#05010E")), (0.4f, C("#1A0636")), (0.62f, C("#3A0A55")), (1f, C("#08021A")));
            // striped synth sun
            var sun = new Vector2(w * 0.5f, h * 0.55f);
            p.Glow(sun, 260, A(C("#FF3FB5"), 0.35f));
            p.Fill(q => PixelPainter.Circle(q, sun, 110), C("#FF5FA8"), q =>
            {
                float t = Mathf.Clamp01((q.y - sun.y + 110) / 220f);
                bool gap = q.y < sun.y && Mathf.Repeat(q.y, 18f) < 6f * (1 - t * 1.6f);
                return gap ? new Color(0, 0, 0, 0) : Color.Lerp(C("#FF3F7F"), C("#FFC23F"), t);
            }, new RectInt((int)sun.x - 112, (int)sun.y - 112, 224, 224));
            // city skyline with neon windows
            float x = 0;
            while (x < w)
            {
                float bw = 30 + R(r) * 50, bh = h * (0.1f + R(r) * 0.22f);
                float bx = x;
                Poly(p, new[] { new Vector2(bx, h * 0.38f), new Vector2(bx + bw, h * 0.38f), new Vector2(bx + bw, h * 0.38f + bh), new Vector2(bx, h * 0.38f + bh) }, C("#0B0418"));
                for (int k = 0; k < (int)(bw * bh / 900); k++)
                {
                    var wp = new Vector2(bx + 5 + R(r) * (bw - 10), h * 0.38f + 6 + R(r) * (bh - 12));
                    p.Glow(wp, 2.5f, A(R(r) < 0.5f ? C("#3FF0FF") : C("#FF4FD8"), 0.9f));
                }
                x += bw + 4;
            }
            // perspective grid floor
            Poly(p, new[] { new Vector2(0, 0), new Vector2(w, 0), new Vector2(w, h * 0.38f), new Vector2(0, h * 0.38f) }, C("#0A0216"));
            var vp = new Vector2(w * 0.5f, h * 0.38f);
            for (int i = -10; i <= 10; i++)
            {
                var b = new Vector2(w * 0.5f + i * w * 0.18f, 0);
                p.Fill(q => PixelPainter.Segment(q, vp, b, 1f), A(C("#3FF0FF"), 0.35f), null, new RectInt((int)Mathf.Min(vp.x, b.x) - 2, 0, (int)Mathf.Abs(vp.x - b.x) + 4, (int)vp.y + 2));
            }
            for (int k = 1; k < 12; k++)
            {
                float yy = vp.y * Mathf.Pow(k / 12f, 2.2f);
                for (int xx = 0; xx < w; xx++) p.Add(xx, (int)yy, A(C("#FF4FD8"), 0.35f));
            }
            for (int i = 0; i < 40; i++) p.Glow(new Vector2(R(r) * w, h * (0.6f + R(r) * 0.4f)), 1.5f, A(Color.white, 0.6f));
        }

        private static void StarlitVoid(PixelPainter p, System.Random r)
        {
            int w = p.Width, h = p.Height;
            p.Gradient((0f, C("#06031A")), (0.5f, C("#120838")), (1f, C("#05021A")));
            // faint galaxy band
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float d = (y - h * 0.5f - (x - w * 0.5f) * 0.8f) / (h * 0.12f);
                    float band = Mathf.Exp(-d * d);
                    if (band < 0.02f) continue;
                    float n = Fbm2(x * 0.02f, y * 0.02f, 77, 4);
                    p.Add(x, y, A(C("#8C6BFF"), band * n * 0.25f));
                }
            Stars(p, r, 380, 0f);
            // constellations
            for (int k = 0; k < 4; k++)
            {
                var pts = new Vector2[5];
                var start = new Vector2(R(r) * w, h * (0.1f + R(r) * 0.8f));
                for (int i = 0; i < pts.Length; i++) { pts[i] = start; start += new Vector2((R(r) - 0.5f) * 140, (R(r) - 0.3f) * 90); }
                for (int i = 0; i < pts.Length - 1; i++)
                {
                    var a = pts[i]; var b = pts[i + 1];
                    p.Fill(q => PixelPainter.Segment(q, a, b, 0.7f), A(C("#B9A8FF"), 0.35f), null,
                        new RectInt((int)Mathf.Min(a.x, b.x) - 3, (int)Mathf.Min(a.y, b.y) - 3, (int)Mathf.Abs(a.x - b.x) + 7, (int)Mathf.Abs(a.y - b.y) + 7));
                }
                foreach (var q in pts) { p.Glow(q, 9, A(C("#DCD2FF"), 0.8f)); p.Add((int)q.x, (int)q.y, Color.white); }
            }
        }
    }
}
