using System;
using System.Collections.Generic;
using UnityEngine;

namespace UndergroundMetroTycoon.Game
{
    /// <summary>
    /// Original icons painted procedurally at startup (no external art): shapes are rasterised with 4x4
    /// supersampling for smooth edges. Coordinates are normalised 0..1 with Y pointing up.
    /// </summary>
    public static class UIIcons
    {
        private const int Size = 128;
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite Gear => Get("gear", PaintGear);
        public static Sprite Shovel => Get("shovel", PaintShovel);
        public static Sprite Clipboard => Get("clipboard", PaintClipboard);
        public static Sprite Avatar => Get("avatar", PaintAvatar);
        public static Sprite Star => Get("star", PaintStar);
        public static Sprite Lock => Get("lock", PaintLock);
        public static Sprite Check => Get("check", PaintCheck);
        public static Sprite Close => Get("close", PaintClose);
        public static Sprite SoilBlock => Get("soil", PaintSoilBlock);
        public static Sprite Coin => Get("coin", PaintCoin);

        private static Sprite Get(string key, Action<Painter> paint)
        {
            if (Cache.TryGetValue(key, out Sprite sprite) && sprite != null) return sprite;
            var painter = new Painter(Size);
            paint(painter);
            sprite = painter.ToSprite("UMT_Icon_" + key);
            Cache[key] = sprite;
            return sprite;
        }

        // ---- Icon definitions ------------------------------------------------------------------------

        private static void PaintGear(Painter p)
        {
            Color white = Color.white;
            for (int i = 0; i < 8; i++)
            {
                float a = i * 45f;
                float rad = a * Mathf.Deg2Rad;
                p.RoundRect(0.5f + Mathf.Cos(rad) * 0.33f, 0.5f + Mathf.Sin(rad) * 0.33f, 0.075f, 0.09f, 0.03f, a + 90f, white);
            }
            p.Circle(0.5f, 0.5f, 0.32f, white);
            p.Erase(Painter.CircleShape(0.5f, 0.5f, 0.13f));
        }

        private static void PaintShovel(Painter p)
        {
            Color wood = Palette.Hex(0xC98A4B), woodDark = Palette.Hex(0x9A6230);
            Color steel = Palette.Hex(0xB8C4D2), steelDark = Palette.Hex(0x7E8B9C), grip = Palette.Hex(0xFF8A3D);
            p.Capsule(0.68f, 0.74f, 0.42f, 0.46f, 0.05f, woodDark);
            p.Capsule(0.68f, 0.74f, 0.42f, 0.46f, 0.036f, wood);
            p.Capsule(0.62f, 0.81f, 0.76f, 0.67f, 0.045f, grip);
            p.Polygon(steelDark, new Vector2(0.46f, 0.52f), new Vector2(0.52f, 0.44f), new Vector2(0.33f, 0.10f), new Vector2(0.12f, 0.24f), new Vector2(0.19f, 0.42f));
            p.Polygon(steel, new Vector2(0.45f, 0.49f), new Vector2(0.49f, 0.44f), new Vector2(0.32f, 0.14f), new Vector2(0.16f, 0.25f), new Vector2(0.21f, 0.40f));
            p.Capsule(0.27f, 0.36f, 0.22f, 0.27f, 0.018f, new Color(1f, 1f, 1f, 0.75f));
        }

        private static void PaintClipboard(Painter p)
        {
            p.RoundRect(0.5f, 0.47f, 0.31f, 0.40f, 0.06f, 0f, Palette.Hex(0xE08A3C));
            p.RoundRect(0.5f, 0.44f, 0.25f, 0.32f, 0.03f, 0f, Color.white);
            p.RoundRect(0.5f, 0.86f, 0.13f, 0.06f, 0.03f, 0f, Palette.Hex(0x8C99AB));
            Color line = Palette.Hex(0xA7B6CB);
            for (int i = 0; i < 3; i++) p.RoundRect(0.56f, 0.62f - i * 0.15f, 0.13f, 0.022f, 0.02f, 0f, line);
            Color green = Palette.Hex(0x2DBE60);
            for (int i = 0; i < 3; i++)
            {
                float y = 0.62f - i * 0.15f;
                p.Capsule(0.30f, y, 0.335f, y - 0.035f, 0.02f, green);
                p.Capsule(0.335f, y - 0.035f, 0.40f, y + 0.04f, 0.02f, green);
            }
        }

        private static void PaintAvatar(Painter p)
        {
            Color skin = Palette.Hex(0xF4C38C), skinShade = Palette.Hex(0xE3A86F);
            Color helmet = Palette.Hex(0xFFC21A), helmetDark = Palette.Hex(0xE09A00);
            Color vest = Palette.Hex(0xFF7A1A), dark = Palette.Hex(0x23324D);
            p.RoundRect(0.5f, 0.06f, 0.36f, 0.14f, 0.12f, 0f, vest);
            p.RoundRect(0.5f, 0.06f, 0.36f, 0.025f, 0.02f, 0f, new Color(1f, 1f, 1f, 0.85f));
            p.Circle(0.5f, 0.42f, 0.27f, skin);
            p.Circle(0.25f, 0.42f, 0.06f, skinShade);
            p.Circle(0.75f, 0.42f, 0.06f, skinShade);
            p.Circle(0.5f, 0.42f, 0.25f, skin);
            p.Circle(0.40f, 0.43f, 0.035f, dark);
            p.Circle(0.60f, 0.43f, 0.035f, dark);
            p.Circle(0.41f, 0.445f, 0.012f, Color.white);
            p.Circle(0.61f, 0.445f, 0.012f, Color.white);
            p.Circle(0.33f, 0.35f, 0.035f, new Color(1f, 0.55f, 0.5f, 0.55f));
            p.Circle(0.67f, 0.35f, 0.035f, new Color(1f, 0.55f, 0.5f, 0.55f));
            p.Shape((x, y) => Painter.InRing(x, y, 0.5f, 0.36f, 0.075f, 0.105f) && y < 0.33f, dark);
            p.Shape((x, y) => Painter.InCircle(x, y, 0.5f, 0.56f, 0.30f) && y > 0.55f, helmet);
            p.RoundRect(0.5f, 0.56f, 0.36f, 0.035f, 0.03f, 0f, helmetDark);
            p.RoundRect(0.5f, 0.74f, 0.04f, 0.11f, 0.03f, 0f, helmetDark);
        }

        private static void PaintStar(Painter p)
        {
            var points = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float r = i % 2 == 0 ? 0.46f : 0.2f;
                float a = (90f + i * 36f) * Mathf.Deg2Rad;
                points[i] = new Vector2(0.5f + Mathf.Cos(a) * r, 0.47f + Mathf.Sin(a) * r);
            }
            p.Polygon(Palette.Hex(0xD68E00), points);
            for (int i = 0; i < 10; i++) points[i] = Vector2.Lerp(new Vector2(0.5f, 0.47f), points[i], 0.78f);
            p.Polygon(Palette.Hex(0xFFC52E), points);
        }

        private static void PaintLock(Painter p)
        {
            Color white = Color.white;
            p.Shape((x, y) => Painter.InRing(x, y, 0.5f, 0.55f, 0.17f, 0.24f) && y > 0.5f, white);
            p.RoundRect(0.33f, 0.52f, 0.035f, 0.06f, 0.0f, 0f, white);
            p.RoundRect(0.67f, 0.52f, 0.035f, 0.06f, 0.0f, 0f, white);
            p.RoundRect(0.5f, 0.32f, 0.30f, 0.22f, 0.06f, 0f, white);
            p.Erase(Painter.CircleShape(0.5f, 0.35f, 0.05f));
            p.Erase((x, y) => Painter.InRoundRect(x, y, 0.5f, 0.26f, 0.02f, 0.06f, 0.01f, 0f));
        }

        private static void PaintCheck(Painter p)
        {
            p.Capsule(0.20f, 0.52f, 0.40f, 0.30f, 0.07f, Color.white);
            p.Capsule(0.40f, 0.30f, 0.80f, 0.72f, 0.07f, Color.white);
        }

        private static void PaintClose(Painter p)
        {
            p.Capsule(0.26f, 0.26f, 0.74f, 0.74f, 0.075f, Color.white);
            p.Capsule(0.26f, 0.74f, 0.74f, 0.26f, 0.075f, Color.white);
        }

        private static void PaintSoilBlock(Painter p)
        {
            Vector2 top = new Vector2(0.5f, 0.92f), left = new Vector2(0.08f, 0.70f), right = new Vector2(0.92f, 0.70f);
            Vector2 center = new Vector2(0.5f, 0.48f), bottom = new Vector2(0.5f, 0.06f);
            Vector2 leftBottom = new Vector2(0.08f, 0.28f), rightBottom = new Vector2(0.92f, 0.28f);
            p.Polygon(Palette.Hex(0xD99A5B), top, right, center, left);
            p.Polygon(Palette.Hex(0xB06A37), left, center, bottom, leftBottom);
            p.Polygon(Palette.Hex(0x8A4E26), center, right, rightBottom, bottom);
            p.Circle(0.38f, 0.72f, 0.04f, Palette.Hex(0xEAB57C));
            p.Circle(0.6f, 0.78f, 0.03f, Palette.Hex(0xEAB57C));
            p.Circle(0.25f, 0.45f, 0.03f, Palette.Hex(0x8C5228));
            p.Circle(0.72f, 0.36f, 0.035f, Palette.Hex(0x6B3A1A));
        }

        private static void PaintCoin(Painter p)
        {
            p.Circle(0.5f, 0.46f, 0.46f, Palette.Hex(0xC97A00));
            p.Circle(0.5f, 0.52f, 0.46f, Palette.Hex(0xF2A900));
            p.Circle(0.5f, 0.52f, 0.36f, Palette.Hex(0xFFC52E));
            p.Shape((x, y) => Painter.InRing(x, y, 0.5f, 0.52f, 0.30f, 0.34f), Palette.Hex(0xE9A100));
            var star = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float r = i % 2 == 0 ? 0.2f : 0.085f;
                float a = (90f + i * 36f) * Mathf.Deg2Rad;
                star[i] = new Vector2(0.5f + Mathf.Cos(a) * r, 0.51f + Mathf.Sin(a) * r);
            }
            p.Polygon(Palette.Hex(0xE09000), star);
            p.Circle(0.36f, 0.70f, 0.07f, new Color(1f, 1f, 1f, 0.7f));
        }

        // ---- Rasteriser ------------------------------------------------------------------------------

        private sealed class Painter
        {
            private const int Samples = 4;
            private readonly int _size;
            private readonly Color[] _pixels;

            public Painter(int size)
            {
                _size = size;
                _pixels = new Color[size * size];
            }

            public delegate bool ShapeTest(float x, float y);

            public void Circle(float cx, float cy, float r, Color c) => Shape(CircleShape(cx, cy, r), c);

            public void RoundRect(float cx, float cy, float halfW, float halfH, float radius, float angleDeg, Color c) =>
                Shape((x, y) => InRoundRect(x, y, cx, cy, halfW, halfH, radius, angleDeg), c);

            public void Capsule(float ax, float ay, float bx, float by, float r, Color c) =>
                Shape((x, y) => SegmentDistance(x, y, ax, ay, bx, by) <= r, c);

            public void Polygon(Color c, params Vector2[] points) => Shape((x, y) => InPolygon(x, y, points), c);

            public static ShapeTest CircleShape(float cx, float cy, float r) => (x, y) => InCircle(x, y, cx, cy, r);

            public static bool InCircle(float x, float y, float cx, float cy, float r) => (x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r;

            public static bool InRing(float x, float y, float cx, float cy, float r0, float r1)
            {
                float d = (x - cx) * (x - cx) + (y - cy) * (y - cy);
                return d >= r0 * r0 && d <= r1 * r1;
            }

            public static bool InRoundRect(float x, float y, float cx, float cy, float halfW, float halfH, float radius, float angleDeg)
            {
                float a = -angleDeg * Mathf.Deg2Rad;
                float dx = x - cx, dy = y - cy;
                float lx = dx * Mathf.Cos(a) - dy * Mathf.Sin(a);
                float ly = dx * Mathf.Sin(a) + dy * Mathf.Cos(a);
                float qx = Mathf.Abs(lx) - (halfW - radius);
                float qy = Mathf.Abs(ly) - (halfH - radius);
                float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
                return outside + Mathf.Min(Mathf.Max(qx, qy), 0f) <= radius;
            }

            public void Shape(ShapeTest test, Color color) => Rasterise(test, color, false);

            public void Erase(ShapeTest test) => Rasterise(test, Color.clear, true);

            private void Rasterise(ShapeTest test, Color color, bool erase)
            {
                float step = 1f / (_size * Samples);
                for (int py = 0; py < _size; py++)
                {
                    for (int px = 0; px < _size; px++)
                    {
                        int hits = 0;
                        for (int sy = 0; sy < Samples; sy++)
                        {
                            for (int sx = 0; sx < Samples; sx++)
                            {
                                float x = (px * Samples + sx + 0.5f) * step;
                                float y = (py * Samples + sy + 0.5f) * step;
                                if (test(x, y)) hits++;
                            }
                        }
                        if (hits == 0) continue;
                        float coverage = hits / (float)(Samples * Samples);
                        int i = py * _size + px;
                        if (erase)
                        {
                            Color e = _pixels[i];
                            e.a *= 1f - coverage;
                            _pixels[i] = e;
                            continue;
                        }
                        // Standard "over" compositing in straight alpha.
                        float srcA = color.a * coverage;
                        Color dst = _pixels[i];
                        float outA = srcA + dst.a * (1f - srcA);
                        if (outA <= 0f) continue;
                        float r = (color.r * srcA + dst.r * dst.a * (1f - srcA)) / outA;
                        float g = (color.g * srcA + dst.g * dst.a * (1f - srcA)) / outA;
                        float b = (color.b * srcA + dst.b * dst.a * (1f - srcA)) / outA;
                        _pixels[i] = new Color(r, g, b, outA);
                    }
                }
            }

            public Sprite ToSprite(string name)
            {
                var tex = new Texture2D(_size, _size, TextureFormat.RGBA32, true)
                {
                    name = name,
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Trilinear
                };
                tex.SetPixels(_pixels);
                tex.Apply(true, true);
                return Sprite.Create(tex, new Rect(0, 0, _size, _size), new Vector2(0.5f, 0.5f), 100f);
            }

            private static float SegmentDistance(float x, float y, float ax, float ay, float bx, float by)
            {
                float vx = bx - ax, vy = by - ay;
                float t = Mathf.Clamp01(((x - ax) * vx + (y - ay) * vy) / (vx * vx + vy * vy));
                float dx = x - (ax + vx * t), dy = y - (ay + vy * t);
                return Mathf.Sqrt(dx * dx + dy * dy);
            }

            private static bool InPolygon(float x, float y, Vector2[] pts)
            {
                bool inside = false;
                for (int i = 0, j = pts.Length - 1; i < pts.Length; j = i++)
                {
                    if ((pts[i].y > y) != (pts[j].y > y) &&
                        x < (pts[j].x - pts[i].x) * (y - pts[i].y) / (pts[j].y - pts[i].y) + pts[i].x)
                        inside = !inside;
                }
                return inside;
            }
        }
    }
}
