using System.Collections.Generic;
using UnityEngine;
using Veliqora.UI;

namespace Veliqora.Presentation
{
    /// <summary>
    /// PROCEDURAL PLACEHOLDER illustrations: an original whimsical valley (sky, clouds, mountains, waterfall,
    /// winding river, hills, trees, flowers) colored by the active theme, plus a vertically seamless map tile.
    /// Generated once per theme/size and cached. Replace with authored art via Resources/Veliqora/Art/Backgrounds.
    /// </summary>
    public static class BackgroundArt
    {
        private static readonly Dictionary<string, Texture2D> Cache = new Dictionary<string, Texture2D>();

        public static void ClearCache()
        {
            foreach (var t in Cache.Values) if (t != null) Object.Destroy(t);
            Cache.Clear();
        }

        private static float Hash(int x, int seed)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + seed * 668265263);
                h = (h ^ (h >> 13)) * 1274126177u;
                return ((h ^ (h >> 16)) & 0xFFFFFF) / (float)0xFFFFFF;
            }
        }

        private static float Noise1(float x, int seed)
        {
            int i = Mathf.FloorToInt(x);
            float f = x - i;
            float u = f * f * (3f - 2f * f);
            return Mathf.Lerp(Hash(i, seed), Hash(i + 1, seed), u);
        }

        private static float Fbm(float x, int seed, int octaves = 4)
        {
            float v = 0, a = 0.5f, f = 1f;
            for (int o = 0; o < octaves; o++) { v += a * Noise1(x * f, seed + o * 17); f *= 2f; a *= 0.5f; }
            return v;
        }

        // ---------------- home / valley ----------------

        public static Texture2D Valley(ThemeVisuals t, int w = 540, int h = 960)
        {
            if (t.IsDark) { w = 540; h = 960; } // dark scenes are authored at full size; thumbnails reuse the texture
            string key = "valley_" + t.Id + "_" + w + "x" + h;
            Texture2D cached;
            if (Cache.TryGetValue(key, out cached) && cached != null) return cached;
            var authored = Resources.Load<Texture2D>("Veliqora/Art/Backgrounds/" + t.Id);
            if (authored != null) return Cache[key] = authored;
            var tex = (t.IsDark ? DarkScenes.Paint(t.Scene, w, h) : PaintValley(t, w, h)).ToTexture();
            tex.name = key;
            Cache[key] = tex;
            return tex;
        }

        /// <summary>Rasterizes the valley illustration (pure managed; also used by the art preview tool).</summary>
        public static PixelPainter PaintValley(ThemeVisuals t, int w, int h)
        {
            var p = new PixelPainter(w, h);
            p.VerticalGradient(t.SkyBottom, t.SkyTop);
            float s = w / 540f;

            // sun / moon glow
            var sunC = new Vector2(w * 0.78f, h * 0.84f);
            var glowCol = t.IsDarkSky ? new Color(1f, 0.97f, 0.8f, 0.9f) : new Color(1f, 0.98f, 0.85f, 0.85f);
            var sunBounds = new RectInt((int)(sunC.x - 122 * s), (int)(sunC.y - 122 * s), (int)(244 * s) + 2, (int)(244 * s) + 2);
            p.Fill(q => PixelPainter.Circle(q, sunC, 120 * s), glowCol, q =>
            {
                float d = (q - sunC).magnitude / (120 * s);
                return UIStyle.WithAlpha(glowCol, Mathf.Pow(Mathf.Clamp01(1 - d), 2.2f) * 0.8f);
            }, sunBounds);
            p.Fill(q => PixelPainter.Circle(q, sunC, 34 * s), UIStyle.WithAlpha(Color.white, t.IsDarkSky ? 0.95f : 0.7f), null, sunBounds);
            if (t.IsDarkSky) Stars(p, 90, 77, h * 0.55f);

            // clouds
            Cloud(p, new Vector2(w * 0.2f, h * 0.86f), 46 * s, t);
            Cloud(p, new Vector2(w * 0.62f, h * 0.76f), 34 * s, t);
            Cloud(p, new Vector2(w * 0.9f, h * 0.66f), 26 * s, t);

            // distant mountains (two layers)
            Ridge(p, h * 0.56f, 170 * s, 0.009f / s, 11, Color.Lerp(t.Mountain, t.SkyBottom, 0.45f), true);
            Ridge(p, h * 0.50f, 140 * s, 0.013f / s, 23, t.Mountain, true);

            // waterfall from the mountain into the river
            float wfX = w * 0.42f;
            var wfBounds = new RectInt((int)(wfX - 30 * s), (int)(h * 0.37f), (int)(60 * s), (int)(h * 0.22f));
            p.Fill(q => PixelPainter.RoundedRect(q, new Rect(wfX - 12 * s, h * 0.40f, 24 * s, h * 0.18f), 10 * s), UIStyle.WithAlpha(Color.Lerp(t.River, Color.white, 0.55f), 0.95f), null, wfBounds);
            p.Fill(q => PixelPainter.Circle(q, new Vector2(wfX, h * 0.405f), 26 * s), UIStyle.WithAlpha(Color.white, 0.55f), null, wfBounds);

            // mid hills
            Ridge(p, h * 0.42f, 60 * s, 0.008f / s, 31, Color.Lerp(t.Hill, t.Mountain, 0.25f), false);

            // river: perspective band from waterfall down to the bottom, wide at the bottom
            for (int y = 0; y < (int)(h * 0.41f); y++)
            {
                float k = y / (h * 0.41f);                // 0 at bottom, 1 at horizon
                float cx = wfX + Mathf.Sin(k * 5.2f + 0.6f) * w * 0.18f * (1 - k) + (1 - k) * w * 0.08f;
                float half = Mathf.Lerp(w * 0.24f, 10 * s, Mathf.Pow(k, 0.8f));
                for (int x = Mathf.Max(0, (int)(cx - half)); x < Mathf.Min(w, (int)(cx + half)); x++)
                {
                    float e = Mathf.Abs(x - cx) / half;
                    var col = Color.Lerp(Color.Lerp(t.River, Color.white, 0.25f), t.River, e);
                    if (Mathf.Repeat(y * 0.11f + Mathf.Sin(x * 0.05f) * 1.5f, 6f) < 0.35f) col = Color.Lerp(col, Color.white, 0.35f);
                    p.Blend(x, y, UIStyle.WithAlpha(col, Mathf.Clamp01((1 - e) * 6f)));
                }
            }

            // near hills left & right framing the river
            Hill(p, new Vector2(-w * 0.1f, -h * 0.06f), w * 0.55f, h * 0.36f, t.Hill, t.HillDark);
            Hill(p, new Vector2(w * 1.08f, -h * 0.08f), w * 0.5f, h * 0.33f, t.Hill, t.HillDark);

            // trees
            var rng = new System.Random(7);
            for (int i = 0; i < 14; i++)
            {
                bool left = i % 2 == 0;
                float x = left ? (float)rng.NextDouble() * w * 0.28f : w - (float)rng.NextDouble() * w * 0.28f;
                float y = h * (0.12f + (float)rng.NextDouble() * 0.2f);
                Tree(p, new Vector2(x, y), (18 + (float)rng.NextDouble() * 14) * s, t);
            }
            // flowers
            for (int i = 0; i < 40; i++)
            {
                float x = (float)rng.NextDouble() * w;
                float y = (float)rng.NextDouble() * h * 0.22f;
                if (Mathf.Abs(x - w * 0.5f) < w * 0.2f && y > h * 0.05f) continue;
                Flower(p, new Vector2(x, y), (4 + (float)rng.NextDouble() * 4) * s, i % 3 == 0 ? Color.white : t.Accent);
            }
            return p;
        }

        // ---------------- level map tile (vertically seamless) ----------------

        public const int MapTileWidth = 540;
        public const int MapTileHeight = 1080;

        /// <summary>River center X (0..1) at a normalized height inside the map tile (periodic).</summary>
        public static float MapRiverX(float yNorm) => 0.5f + 0.2f * Mathf.Sin(yNorm * Mathf.PI * 2f) + 0.04f * Mathf.Sin(yNorm * Mathf.PI * 6f);

        public static Texture2D MapTile(ThemeVisuals t)
        {
            string key = "map_" + t.Id;
            Texture2D cached;
            if (Cache.TryGetValue(key, out cached) && cached != null) return cached;
            var tex = PaintMapTile(t).ToTexture(repeat: true);
            tex.name = key;
            Cache[key] = tex;
            return tex;
        }

        public static PixelPainter PaintMapTile(ThemeVisuals t)
        {
            int w = MapTileWidth, h = MapTileHeight;
            var p = new PixelPainter(w, h);
            var grassA = Color.Lerp(t.Hill, Color.white, 0.18f);
            var grassB = t.Hill;
            for (int y = 0; y < h; y++)
            {
                float band = 0.5f + 0.5f * Mathf.Sin(y / (float)h * Mathf.PI * 8f);
                var c = Color.Lerp(grassA, grassB, band * 0.35f);
                for (int x = 0; x < w; x++) p.Blend(x, y, c);
            }
            // soft hill shadows (periodic)
            for (int i = 0; i < 6; i++)
            {
                float cy = h * (i + 0.5f) / 6f;
                float cx = i % 2 == 0 ? w * 0.08f : w * 0.92f;
                for (int dy = -1; dy <= 1; dy++)
                {
                    var c = new Vector2(cx, cy + dy * h);
                    var b = new RectInt((int)(c.x - 132), (int)(c.y - 132), 264, 264);
                    p.Fill(q => PixelPainter.Circle(q, c, 130), UIStyle.WithAlpha(t.HillDark, 0.25f), null, b);
                }
            }
            // river
            for (int y = 0; y < h; y++)
            {
                float cx = MapRiverX(y / (float)h) * w;
                float half = 46f;
                for (int x = Mathf.Max(0, (int)(cx - half - 10)); x < Mathf.Min(w, (int)(cx + half + 10)); x++)
                {
                    float d = Mathf.Abs(x - cx);
                    if (d > half + 8) continue;
                    if (d > half) { p.Blend(x, y, UIStyle.WithAlpha(Color.Lerp(t.HillDark, Color.white, 0.6f), 0.7f)); continue; }
                    var col = Color.Lerp(Color.Lerp(t.River, Color.white, 0.3f), t.River, d / half);
                    if (Mathf.Repeat(y * 0.07f + x * 0.02f, 5f) < 0.3f) col = Color.Lerp(col, Color.white, 0.3f);
                    p.Blend(x, y, col);
                }
            }
            // trees & flowers, wrapped vertically for seamless tiling
            var rng = new System.Random(11);
            for (int i = 0; i < 22; i++)
            {
                float y = (float)rng.NextDouble() * h;
                float rx = MapRiverX(y / h) * w;
                float x = (float)rng.NextDouble() * w;
                if (Mathf.Abs(x - rx) < 150) x = rx + (x < rx ? -170 : 170);
                float size = 20 + (float)rng.NextDouble() * 16;
                for (int dy = -1; dy <= 1; dy++) Tree(p, new Vector2(x, y + dy * h), size, t);
            }
            for (int i = 0; i < 70; i++)
            {
                float y = (float)rng.NextDouble() * h;
                float x = (float)rng.NextDouble() * w;
                if (Mathf.Abs(x - MapRiverX(y / h) * w) < 70) continue;
                var col = i % 3 == 0 ? Color.white : t.Accent;
                for (int dy = -1; dy <= 1; dy++) Flower(p, new Vector2(x, y + dy * h), 4 + (float)rng.NextDouble() * 3, col);
            }
            return p;
        }

        /// <summary>White texture whose alpha fades from <paramref name="top"/> (top edge) and <paramref name="bottom"/> (bottom edge) to 0 mid-screen; tint it with RawImage.color.</summary>
        public static Texture2D ScrimGradient(float top, float bottom)
        {
            string key = "scrim_" + top.ToString("0.00") + "_" + bottom.ToString("0.00");
            Texture2D cached;
            if (Cache.TryGetValue(key, out cached) && cached != null) return cached;
            const int h = 128;
            var p = new PixelPainter(2, h);
            for (int y = 0; y < h; y++)
            {
                float v = y / (float)(h - 1);                       // 0 bottom .. 1 top
                float a = Mathf.Max(bottom * Mathf.Pow(Mathf.Clamp01(1f - v / 0.32f), 1.6f),
                                    top * Mathf.Pow(Mathf.Clamp01((v - 0.72f) / 0.28f), 1.6f));
                for (int x = 0; x < 2; x++) p.Pixels[y * 2 + x] = new Color(1, 1, 1, a);
            }
            var tex = p.ToTexture();
            tex.name = key;
            Cache[key] = tex;
            return tex;
        }

        /// <summary>Authored screen background (Resources/Veliqora/Art/Backgrounds/screen_&lt;name&gt;), or null.</summary>
        public static Texture2D ScreenArt(string name) => Resources.Load<Texture2D>("Veliqora/Art/Backgrounds/screen_" + name);

        /// <summary>Authored theme illustration (Resources/Veliqora/Art/Backgrounds/&lt;themeId&gt;), or null.</summary>
        public static Texture2D ThemeArt(string themeId) => Resources.Load<Texture2D>("Veliqora/Art/Backgrounds/" + themeId);

        // ---------------- gameplay ----------------

        public static Texture2D GameplayGradient(Color top, Color bottom, bool light)
        {
            string key = "game_" + ColorUtility.ToHtmlStringRGB(top) + ColorUtility.ToHtmlStringRGB(bottom) + light;
            Texture2D cached;
            if (Cache.TryGetValue(key, out cached) && cached != null) return cached;
            int w = 108, h = 192;
            var p = new PixelPainter(w, h);
            if (light) p.VerticalGradient(UIStyle.Hex("#F3EBDD"), UIStyle.Hex("#FFFBF3"));
            else p.VerticalGradient(bottom, top);
            var c = new Vector2(w * 0.5f, h * 0.55f);
            p.Fill(q => PixelPainter.Circle(q, c, w * 0.9f), Color.white, q =>
                new Color(1, 1, 1, Mathf.Pow(Mathf.Clamp01(1 - (q - c).magnitude / (w * 0.9f)), 2f) * (light ? 0.5f : 0.16f)));
            var tex = p.ToTexture();
            tex.name = key;
            Cache[key] = tex;
            return tex;
        }

        // ---------------- elements ----------------

        private static void Stars(PixelPainter p, int count, int seed, float minY)
        {
            var rng = new System.Random(seed);
            for (int i = 0; i < count; i++)
            {
                var c = new Vector2((float)rng.NextDouble() * p.Width, minY + (float)rng.NextDouble() * (p.Height - minY));
                float r = 0.8f + (float)rng.NextDouble() * 1.6f;
                p.Fill(q => PixelPainter.Circle(q, c, r), new Color(1, 1, 1, 0.5f + (float)rng.NextDouble() * 0.5f),
                    null, new RectInt((int)c.x - 4, (int)c.y - 4, 9, 9));
            }
        }

        private static void Cloud(PixelPainter p, Vector2 c, float r, ThemeVisuals t)
        {
            var col = t.IsDarkSky ? new Color(0.8f, 0.82f, 1f, 0.25f) : new Color(1, 1, 1, 0.92f);
            var b = new RectInt((int)(c.x - r * 3), (int)(c.y - r * 1.4f), (int)(r * 6), (int)(r * 3));
            p.Fill(q => Mathf.Min(PixelPainter.Circle(q, c, r),
                        Mathf.Min(PixelPainter.Circle(q, c + new Vector2(-r * 1.1f, -r * 0.35f), r * 0.75f),
                        Mathf.Min(PixelPainter.Circle(q, c + new Vector2(r * 1.15f, -r * 0.3f), r * 0.8f),
                                  PixelPainter.RoundedRect(q, new Rect(c.x - r * 1.9f, c.y - r * 1.05f, r * 3.8f, r * 0.9f), r * 0.45f)))), col, null, b);
        }

        private static void Ridge(PixelPainter p, float baseY, float amp, float freq, int seed, Color col, bool peaks)
        {
            var snow = Color.Lerp(col, Color.white, 0.55f);
            for (int x = 0; x < p.Width; x++)
            {
                float n = Fbm(x * freq, seed, 3);
                if (peaks) n = Mathf.Pow(1f - Mathf.Abs(n * 2f - 1f), 1.6f); // folded noise -> soft peaks
                float top = baseY + n * amp;
                // gentle left-lit shading from the local slope
                float slope = (Fbm((x + 3) * freq, seed, 3) - Fbm((x - 3) * freq, seed, 3));
                var body = Color.Lerp(col, Color.white, Mathf.Clamp01(-slope * 2.5f) * 0.18f);
                for (int y = 0; y < Mathf.Min(p.Height, (int)top); y++)
                {
                    var c = body;
                    if (peaks && y > baseY + amp * 0.62f) c = Color.Lerp(body, snow, 0.8f); // snow caps above the snow line
                    p.Blend(x, y, c);
                }
                int ty = (int)top;
                if (ty >= 0 && ty < p.Height) p.Blend(x, ty, UIStyle.WithAlpha(peaks && ty > baseY + amp * 0.62f ? snow : body, top - ty));
            }
        }

        private static void Hill(PixelPainter p, Vector2 c, float rx, float ry, Color col, Color dark)
        {
            var b = new RectInt((int)(c.x - rx), 0, (int)(rx * 2), (int)(c.y + ry) + 2);
            p.Fill(q =>
            {
                var d = new Vector2((q.x - c.x) / rx, (q.y - c.y) / ry);
                return (d.magnitude - 1f) * Mathf.Min(rx, ry);
            }, col, q => Color.Lerp(dark, col, Mathf.Clamp01((q.y - c.y) / ry + 0.4f)), b);
        }

        private static void Tree(PixelPainter p, Vector2 c, float r, ThemeVisuals t)
        {
            var b = new RectInt((int)(c.x - r * 1.4f), (int)(c.y - r * 1.6f), (int)(r * 2.8f), (int)(r * 3.4f));
            p.Fill(q => PixelPainter.RoundedRect(q, new Rect(c.x - r * 0.16f, c.y - r * 1.4f, r * 0.32f, r * 1.2f), r * 0.1f), UIStyle.Hex("#8A5A3B"), null, b);
            var leaf = Color.Lerp(t.HillDark, UIStyle.Hex("#2F7D4A"), 0.4f);
            p.Fill(q => Mathf.Min(PixelPainter.Circle(q, c, r), Mathf.Min(PixelPainter.Circle(q, c + new Vector2(-r * 0.6f, -r * 0.35f), r * 0.7f), PixelPainter.Circle(q, c + new Vector2(r * 0.6f, -r * 0.35f), r * 0.7f))),
                leaf, q => Color.Lerp(leaf, Color.Lerp(leaf, Color.white, 0.3f), Mathf.Clamp01((q.y - c.y) / r * 0.8f + 0.3f)), b);
        }

        private static void Flower(PixelPainter p, Vector2 c, float r, Color col)
        {
            var b = new RectInt((int)(c.x - r * 2.2f), (int)(c.y - r * 2.2f), (int)(r * 4.4f), (int)(r * 4.4f));
            for (int i = 0; i < 5; i++)
            {
                float a = i * Mathf.PI * 2f / 5f;
                var pc = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                p.Fill(q => PixelPainter.Circle(q, pc, r * 0.75f), col, null, b);
            }
            p.Fill(q => PixelPainter.Circle(q, c, r * 0.55f), UIStyle.Hex("#FFD23F"), null, b);
        }
    }
}
