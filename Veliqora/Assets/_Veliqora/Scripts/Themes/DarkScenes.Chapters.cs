using System;
using UnityEngine;
using Veliqora.UI;

namespace Veliqora.Presentation
{
    /// <summary>
    /// Original chapter backgrounds (progress map chapters 1–5, reused cyclically after that). Elegant and low in
    /// contrast: scenery lives in the top and bottom thirds, and the middle — where the bottles stand — is kept calm
    /// and darker so liquid colors stay readable.
    /// </summary>
    public static partial class DarkScenes
    {
        public static readonly string[] ChapterScenes =
        {
            "chapter_quiet_forest", "chapter_crystal_lake", "chapter_colorful_canyon", "chapter_magic_cave", "chapter_temple_colors",
        };

        public static string ChapterScene(int chapterIndex) => ChapterScenes[(Mathf.Max(1, chapterIndex) - 1) % ChapterScenes.Length];

        /// <summary>Darkens the board area (center band) and the edges very softly.</summary>
        private static void CalmCenter(PixelPainter p)
        {
            var px = p.Pixels;
            for (int y = 0; y < p.Height; y++)
            {
                float v = y / (float)(p.Height - 1);
                float band = Mathf.Exp(-Mathf.Pow((v - 0.5f) / 0.2f, 2f));       // strongest in the middle
                for (int x = 0; x < p.Width; x++)
                {
                    float u = x / (float)(p.Width - 1);
                    float edge = Mathf.Pow(Mathf.Abs(u - 0.5f) * 2f, 3f) * 0.25f;
                    float k = 1f - 0.38f * band - edge;
                    var c = px[y * p.Width + x];
                    px[y * p.Width + x] = new Color(c.r * k, c.g * k, c.b * k, c.a);
                }
            }
        }

        private static void Pine(PixelPainter p, float x, float baseY, float height, float width, Color col)
        {
            for (int tier = 0; tier < 4; tier++)
            {
                float t0 = baseY + height * (0.12f + tier * 0.2f);
                float w0 = width * (1f - tier * 0.2f);
                Poly(p, new[] { new Vector2(x - w0, t0), new Vector2(x + w0, t0), new Vector2(x, t0 + height * 0.36f) }, col);
            }
        }

        private static void Fog(PixelPainter p, float centerY, float thickness, Color col)
        {
            for (int y = Mathf.Max(0, (int)(centerY - thickness * 2)); y < Mathf.Min(p.Height, (int)(centerY + thickness * 2)); y++)
            {
                float a = col.a * Mathf.Exp(-Mathf.Pow((y - centerY) / thickness, 2f));
                for (int x = 0; x < p.Width; x++) p.Blend(x, y, A(col, a));
            }
        }

        // 1. Quiet Forest — misty pine layers under a soft moon
        private static void QuietForest(PixelPainter p, System.Random r)
        {
            int w = p.Width, h = p.Height;
            p.Gradient((0f, C("#08151A")), (0.45f, C("#0E2430")), (1f, C("#132A45")));
            p.Glow(new Vector2(w * 0.72f, h * 0.86f), w * 0.5f, A(C("#9CC8E8"), 0.16f), 1.6f);
            p.Fill(q => PixelPainter.Circle(q, new Vector2(w * 0.72f, h * 0.86f), w * 0.055f), A(C("#E8F2FA"), 0.85f), null,
                new RectInt((int)(w * 0.65f), (int)(h * 0.86f - w * 0.08f), (int)(w * 0.15f), (int)(w * 0.16f)));
            Stars(p, r, 90, h * 0.62f, 0.6f);
            Color[] layers = { C("#1B3A44"), C("#14303A"), C("#0D232B"), C("#081A20") };
            for (int l = 0; l < layers.Length; l++)
            {
                float baseY = h * (0.26f - l * 0.07f);
                int n = 9 + l * 2;
                for (int i = 0; i < n; i++)
                {
                    float x = (i + (float)r.NextDouble() * 0.8f) * w / (n - 1) - w * 0.05f;
                    float ht = h * (0.16f + l * 0.035f) * (0.7f + (float)r.NextDouble() * 0.5f);
                    Pine(p, x, baseY, ht, ht * 0.28f, layers[l]);
                }
                Silhouette(p, x => baseY + 2, layers[l]);
                Fog(p, baseY + h * 0.05f, h * 0.025f, A(C("#9FC4CF"), 0.08f));
            }
            for (int i = 0; i < 26; i++) p.Glow(new Vector2((float)r.NextDouble() * w, h * (0.05f + (float)r.NextDouble() * 0.22f)), 2 + (float)r.NextDouble() * 3, A(C("#F6E7A2"), 0.55f));
        }

        // 2. Crystal Lake — mountains mirrored in a still lake, crystals on the shore
        private static void CrystalLake(PixelPainter p, System.Random r)
        {
            int w = p.Width, h = p.Height;
            float shore = h * 0.3f;
            p.Gradient((0f, C("#0A1830")), (0.3f, C("#10264A")), (0.31f, C("#152F55")), (1f, C("#0E1E3C")));
            Stars(p, r, 140, h * 0.55f, 0.7f);
            int seed = r.Next();
            Silhouette(p, x => shore + h * (0.1f + 0.12f * Fbm1(x / (float)w * 3f, seed)), C("#1A3260"));
            Silhouette(p, x => shore + h * (0.04f + 0.08f * Fbm1(x / (float)w * 5f + 3, seed + 7)), C("#132648"));
            // mirrored reflection: soft, darker, with shimmer lines
            for (int y = 0; y < (int)shore; y++)
            {
                float t = y / shore;
                for (int x = 0; x < w; x++) p.Blend(x, y, A(C("#0B1A36"), 0.55f + 0.25f * (1 - t)));
            }
            for (int i = 0; i < 40; i++)
            {
                float y = (float)r.NextDouble() * shore * 0.95f;
                float x = (float)r.NextDouble() * w;
                float len = 20 + (float)r.NextDouble() * 70;
                p.Fill(q => PixelPainter.Segment(q, new Vector2(x, y), new Vector2(x + len, y), 0.8f), A(C("#8FC8FF"), 0.12f + 0.15f * (y / shore)),
                    null, new RectInt((int)x - 3, (int)y - 3, (int)len + 6, 7));
            }
            // shore crystals (left and right)
            for (int side = 0; side < 2; side++)
                for (int i = 0; i < 5; i++)
                {
                    float x = side == 0 ? w * (0.04f + i * 0.05f) : w * (0.96f - i * 0.05f);
                    float ch = h * (0.05f + (float)r.NextDouble() * 0.06f), cw = w * 0.018f;
                    var col = A(i % 2 == 0 ? C("#7FD8F0") : C("#B79CFF"), 0.55f);
                    Poly(p, new[] { new Vector2(x - cw, shore - h * 0.01f), new Vector2(x + cw, shore - h * 0.01f), new Vector2(x + cw * 0.6f, shore + ch * 0.8f), new Vector2(x, shore + ch), new Vector2(x - cw * 0.6f, shore + ch * 0.8f) }, col);
                    p.Glow(new Vector2(x, shore + ch * 0.5f), cw * 3f, A(col, 0.12f));
                }
        }

        // 3. Colorful Canyon — layered mesas at dusk in muted terracotta and violet
        private static void ColorfulCanyon(PixelPainter p, System.Random r)
        {
            int w = p.Width, h = p.Height;
            p.Gradient((0f, C("#1A1426")), (0.35f, C("#3A2238")), (0.48f, C("#5A3440")), (0.62f, C("#2A2648")), (1f, C("#111C38")));
            Stars(p, r, 70, h * 0.68f, 0.55f);
            p.Glow(new Vector2(w * 0.3f, h * 0.47f), w * 0.7f, A(C("#FF9B54"), 0.16f), 1.4f);
            p.Glow(new Vector2(w * 0.8f, h * 0.82f), w * 0.25f, A(C("#F4E6C8"), 0.1f), 1.6f);
            Color[] cols = { C("#6A3A48"), C("#55303F"), C("#432636"), C("#2E1B2C") };
            for (int l = 0; l < cols.Length; l++)
            {
                float baseH = h * (0.44f - l * 0.08f);
                int seed = r.Next();
                // flat-topped mesas: quantized noise ridge
                Silhouette(p, x =>
                {
                    float n = Fbm1(x / (float)w * (2.2f + l), seed);
                    float step = (float)System.Math.Round(n * 4f) / 4f;
                    float mesa = Mathf.Lerp(step, n, 0.35f);                 // flat tops with natural edges
                    return baseH + h * 0.09f * mesa;
                }, cols[l]);
                // strata lines
                for (int s = 0; s < 3; s++) Fog(p, baseH - h * 0.03f * (s + 1), 1.2f, A(C("#C98A6A"), 0.06f));
            }
        }

        // 4. Magic Cave — a dark cave arch with softly glowing crystals in the corners
        private static void MagicCave(PixelPainter p, System.Random r)
        {
            int w = p.Width, h = p.Height;
            p.Gradient((0f, C("#0C0A1C")), (0.5f, C("#16143A")), (1f, C("#0B0A1E")));
            p.Glow(new Vector2(w * 0.5f, h * 0.55f), w * 0.8f, A(C("#5A4AB8"), 0.14f), 1.3f);
            int seed = r.Next();
            // ceiling with stalactites and floor with stalagmites
            Silhouette(p, x => h * (0.1f + 0.05f * Fbm1(x / (float)w * 6f, seed)), C("#08071A"));
            for (int x = 0; x < w; x++)
            {
                float edge = h * (0.86f - 0.06f * Fbm1(x / (float)w * 6f, seed + 3));
                for (int y = (int)edge; y < h; y++) p.Blend(x, y, C("#08071A"));
            }
            for (int i = 0; i < 14; i++)
            {
                float x = (float)r.NextDouble() * w, len = h * (0.04f + (float)r.NextDouble() * 0.08f), bw = w * (0.012f + (float)r.NextDouble() * 0.02f);
                Poly(p, new[] { new Vector2(x - bw, h * 0.88f), new Vector2(x + bw, h * 0.88f), new Vector2(x, h * 0.88f - len) }, C("#08071A"));
            }
            // crystal clusters in the four corners
            Vector2[] spots = { new Vector2(w * 0.1f, h * 0.12f), new Vector2(w * 0.9f, h * 0.1f), new Vector2(w * 0.08f, h * 0.84f), new Vector2(w * 0.92f, h * 0.86f) };
            Color[] glow = { C("#7B6CFF"), C("#32C9DA"), C("#F47FC2"), C("#A58BFA") };
            for (int s = 0; s < spots.Length; s++)
            {
                bool top = spots[s].y > h * 0.5f;
                p.Glow(spots[s], w * 0.22f, A(glow[s], 0.16f), 1.6f);
                for (int i = 0; i < 4; i++)
                {
                    float x = spots[s].x + (i - 1.5f) * w * 0.03f, ch = h * (0.04f + (float)r.NextDouble() * 0.05f) * (top ? -1 : 1), cw = w * 0.016f;
                    Poly(p, new[] { new Vector2(x - cw, spots[s].y), new Vector2(x + cw, spots[s].y), new Vector2(x, spots[s].y + ch) }, A(Color.Lerp(glow[s], Color.white, 0.25f), 0.7f));
                }
            }
        }

        // 5. Temple of Colors — a moonlit temple with five soft colored lights above the roof
        private static void TempleOfColors(PixelPainter p, System.Random r)
        {
            int w = p.Width, h = p.Height;
            p.Gradient((0f, C("#0E1428")), (0.4f, C("#172245")), (1f, C("#1B2A55")));
            Stars(p, r, 120, h * 0.58f, 0.65f);
            Silhouette(p, x => h * 0.12f, C("#0C1226"));
            var stone = C("#22305A");
            float baseY = h * 0.12f, cx = w * 0.5f, tw = w * 0.62f;
            for (int s = 0; s < 3; s++) // steps
                Poly(p, new[] { new Vector2(cx - tw * (0.6f - s * 0.04f), baseY + s * h * 0.012f), new Vector2(cx + tw * (0.6f - s * 0.04f), baseY + s * h * 0.012f), new Vector2(cx + tw * (0.6f - s * 0.04f), baseY + (s + 1) * h * 0.012f), new Vector2(cx - tw * (0.6f - s * 0.04f), baseY + (s + 1) * h * 0.012f) }, Color.Lerp(stone, C("#0C1226"), 0.3f - s * 0.1f));
            float colBase = baseY + h * 0.036f, colTop = colBase + h * 0.13f;
            for (int i = 0; i < 6; i++)
            {
                float x = cx - tw * 0.45f + i * tw * 0.18f;
                Poly(p, new[] { new Vector2(x - w * 0.018f, colBase), new Vector2(x + w * 0.018f, colBase), new Vector2(x + w * 0.015f, colTop), new Vector2(x - w * 0.015f, colTop) }, stone);
            }
            Poly(p, new[] { new Vector2(cx - tw * 0.52f, colTop), new Vector2(cx + tw * 0.52f, colTop), new Vector2(cx + tw * 0.52f, colTop + h * 0.02f), new Vector2(cx - tw * 0.52f, colTop + h * 0.02f) }, stone);
            Poly(p, new[] { new Vector2(cx - tw * 0.55f, colTop + h * 0.02f), new Vector2(cx + tw * 0.55f, colTop + h * 0.02f), new Vector2(cx, colTop + h * 0.075f) }, Color.Lerp(stone, Color.white, 0.05f));
            string[] lights = { "#FF6473", "#FFD35A", "#31D6A0", "#3995F6", "#A58BFA" };
            for (int i = 0; i < lights.Length; i++)
            {
                var c = new Vector2(cx + (i - 2) * w * 0.06f, colTop + h * 0.11f + Mathf.Abs(i - 2) * -h * 0.01f);
                p.Glow(c, w * 0.05f, A(C(lights[i]), 0.35f), 1.8f);
                p.Fill(q => PixelPainter.Circle(q, c, w * 0.008f), A(C(lights[i]), 0.9f), null, new RectInt((int)c.x - 12, (int)c.y - 12, 24, 24));
            }
            p.Glow(new Vector2(w * 0.2f, h * 0.88f), w * 0.3f, A(C("#C8D8F0"), 0.12f), 1.6f);
        }
    }
}
