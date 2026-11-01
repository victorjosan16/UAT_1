using System.Collections.Generic;
using UnityEngine;

namespace VoxelBloom
{
    public enum Icon
    {
        Coin, Heart, Star, StarEmpty, Gear, Plus, Pause, Play, Close, Restart, ArrowLeft, ArrowRight, Lock, Check,
        Hammer, Shuffle, ColorBomb, Eliminate, Paw, Gift, Shop, Calendar, Avatar, Music, Sound, Vibrate, Trophy, Cube,
        Lightning, Video, Home, Sparkle, Eye, Leaf, Flower, CubePink, CubeBlue, Gem, Crown, PawGold, CalendarStar,
    }

    /// <summary>Generates and caches all UI sprites procedurally (see IconPainter).</summary>
    public static class SpriteFactory
    {
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
        static readonly Color Ink = new Color(0.23f, 0.18f, 0.36f, 1f);

        static Sprite Cached(string key, System.Func<Sprite> make)
        {
            if (cache.TryGetValue(key, out var s) && s != null) return s;
            s = make();
            cache[key] = s;
            return s;
        }

        static Sprite ToSprite(IconPainter p, string name, Vector4 border = default)
        {
            var t = p.ToTexture(name);
            return Sprite.Create(t, new Rect(0, 0, p.W, p.H), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        }

        /// <summary>White rounded rectangle, 9-sliced (tint with Image.color).</summary>
        public static Sprite Rounded(int radius = 32) => Cached("rr" + radius, () =>
        {
            int s = radius * 2 + 8;
            var p = new IconPainter(s, s);
            p.RoundRect(0, 0, s, s, radius, Color.white);
            return ToSprite(p, "Rounded" + radius, new Vector4(radius + 1, radius + 1, radius + 1, radius + 1));
        });

        /// <summary>Rounded rect with a subtle top-to-bottom sheen for glossy buttons/blocks.</summary>
        public static Sprite Glossy(int radius = 28) => Cached("gl" + radius, () =>
        {
            int s = radius * 2 + 40;
            var p = new IconPainter(s, s);
            p.RoundRect(0, 0, s, s, radius, Color.white, IconPainter.Vertical(Color.white, new Color(0.94f, 0.94f, 0.96f, 1f), 0, s));
            p.RoundRect(6, s * 0.52f, s - 6, s - 5, radius - 6, new Color(1, 1, 1, 0.35f));
            return ToSprite(p, "Glossy" + radius, new Vector4(radius + 2, radius + 2, radius + 2, radius + 2));
        });

        public static Sprite Shadow(int radius = 32) => Cached("sh" + radius, () =>
        {
            int blur = 14, s = radius * 2 + blur * 4 + 8;
            var p = new IconPainter(s, s);
            p.SoftRect(blur * 2, blur * 2, s - blur * 2, s - blur * 2, radius, blur, Color.white);
            int b = radius + blur * 2 + 2;
            return ToSprite(p, "Shadow" + radius, new Vector4(b, b, b, b));
        });

        public static Sprite Circle() => Cached("circle", () =>
        {
            var p = new IconPainter(128, 128);
            p.Circle(64, 64, 62, Color.white);
            return ToSprite(p, "Circle");
        });

        public static Sprite VerticalGradient(Color top, Color bottom) => Cached("vg" + ColorUtility.ToHtmlStringRGBA(top) + ColorUtility.ToHtmlStringRGBA(bottom), () =>
        {
            var p = new IconPainter(4, 256);
            p.RoundRect(-10, -10, 14, 266, 0, Color.white, IconPainter.Vertical(top, bottom, 0, 256));
            return ToSprite(p, "Gradient");
        });

        /// <summary>Purple title ribbon with folded tails, 9-sliced horizontally.</summary>
        public static Sprite Ribbon() => Cached("ribbon", () =>
        {
            const int W = 256, H = 112;
            var p = new IconPainter(W, H);
            var dark = Theme.PurpleDark; var mid = Theme.Purple;
            p.Polygon(Theme.Darker(dark, 0.8f), new float[] { 0, 20, 40, 20, 40, 76, 0, 76, 14, 48 });
            p.Polygon(Theme.Darker(dark, 0.8f), new float[] { W, 20, W - 40, 20, W - 40, 76, W, 76, W - 14, 48 });
            p.Polygon(Theme.Darker(dark, 0.6f), new float[] { 28, 20, 40, 8, 40, 20 });
            p.Polygon(Theme.Darker(dark, 0.6f), new float[] { W - 28, 20, W - 40, 8, W - 40, 20 });
            p.RoundRect(26, 8, W - 26, H - 6, 14, mid, IconPainter.Vertical(Theme.Lighter(mid, 0.2f), dark, 8, H - 6));
            p.RoundRect(34, H * 0.55f, W - 34, H - 14, 10, new Color(1, 1, 1, 0.18f));
            return ToSprite(p, "Ribbon", new Vector4(60, 30, 60, 30));
        });

        /// <summary>Glossy white sphere (tint with Image.color) - launcher "cannon balls".</summary>
        public static Sprite Orb() => Cached("orb", () =>
        {
            const int S = 128;
            var p = new IconPainter(S, S);
            p.Circle(64, 60, 58, new Color(0, 0, 0, 0.25f));
            p.Circle(64, 64, 56, Color.white, IconPainter.Vertical(Color.white, new Color(0.62f, 0.62f, 0.7f, 1f), 8, 120));
            p.Circle(48, 84, 18, new Color(1, 1, 1, 0.85f));
            p.Circle(44, 90, 7, Color.white);
            return ToSprite(p, "Orb");
        });

        /// <summary>Soft round blob (glow, clouds).</summary>
        public static Sprite Glow() => Cached("glow", () =>
        {
            const int S = 128;
            var p = new IconPainter(S, S);
            p.SoftRect(28, 28, 100, 100, 36, 24, Color.white);
            return ToSprite(p, "Glow");
        });

        // ------------------------------------------------------------------ icons
        public static Sprite Get(Icon icon) => Cached("icon" + icon, () =>
        {
            var p = new IconPainter(128, 128);
            Draw(p, icon);
            return ToSprite(p, "Icon_" + icon);
        });

        static void OutlinedCircle(IconPainter p, float x, float y, float r, Color c, float o = 6) { p.Circle(x, y, r + o, Ink); p.Circle(x, y, r, c, IconPainter.Vertical(Theme.Lighter(c, 0.3f), c, y - r, y + r)); }
        static void OutlinedRect(IconPainter p, float x0, float y0, float x1, float y1, float rad, Color c, float o = 6) { p.RoundRect(x0 - o, y0 - o, x1 + o, y1 + o, rad + o, Ink); p.RoundRect(x0, y0, x1, y1, rad, c, IconPainter.Vertical(Theme.Lighter(c, 0.3f), c, y0, y1)); }
        static void OutlinedLine(IconPainter p, float ax, float ay, float bx, float by, float t, Color c, float o = 6) { p.Line(ax, ay, bx, by, t + o * 2, Ink); p.Line(ax, ay, bx, by, t, c); }
        static void OutlinedPoly(IconPainter p, Color c, float[] pts, float o = 6) { p.Polygon(Ink, pts, null, o); p.Polygon(c, pts, IconPainter.Vertical(Theme.Lighter(c, 0.35f), c, 10, 118)); }

        static void Draw(IconPainter p, Icon icon)
        {
            Color W = Color.white;
            switch (icon)
            {
                case Icon.Coin:
                    OutlinedCircle(p, 64, 64, 50, Theme.Gold);
                    p.Ring(64, 64, 36, 6, Theme.GoldDark);
                    p.RoundRect(56, 40, 72, 88, 6, Theme.Lighter(Theme.Gold, 0.5f));
                    break;
                case Icon.Heart:
                {
                    var c = Theme.Red;
                    float[] h = Heart(64, 60, 52);
                    OutlinedPoly(p, c, h);
                    p.Circle(46, 82, 9, new Color(1, 1, 1, 0.6f));
                    break;
                }
                case Icon.Star: OutlinedPoly(p, Theme.Gold, IconPainter.Star(64, 62, 58)); p.Polygon(new Color(1, 1, 1, 0.35f), IconPainter.Star(56, 72, 18)); break;
                case Icon.StarEmpty: OutlinedPoly(p, new Color(0.78f, 0.8f, 0.88f), IconPainter.Star(64, 62, 58)); break;
                case Icon.Gear:
                    for (int i = 0; i < 8; i++) { float a = i * Mathf.PI / 4; p.Line(64, 64, 64 + Mathf.Cos(a) * 50, 64 + Mathf.Sin(a) * 50, 26, Ink); }
                    p.Circle(64, 64, 42, Ink);
                    for (int i = 0; i < 8; i++) { float a = i * Mathf.PI / 4; p.Line(64, 64, 64 + Mathf.Cos(a) * 45, 64 + Mathf.Sin(a) * 45, 16, new Color(0.62f, 0.66f, 0.76f)); }
                    p.Circle(64, 64, 36, new Color(0.72f, 0.76f, 0.86f), IconPainter.Vertical(new Color(0.9f, 0.92f, 0.97f), new Color(0.6f, 0.64f, 0.75f), 28, 100));
                    p.Circle(64, 64, 15, Ink);
                    break;
                case Icon.Plus: OutlinedCircle(p, 64, 64, 50, Theme.Green); p.Line(64, 38, 64, 90, 16, W); p.Line(38, 64, 90, 64, 16, W); break;
                case Icon.Pause: OutlinedRect(p, 34, 26, 56, 102, 8, W); OutlinedRect(p, 72, 26, 94, 102, 8, W); break;
                case Icon.Play: OutlinedPoly(p, W, new float[] { 38, 20, 38, 108, 106, 64 }); break;
                case Icon.Close: OutlinedLine(p, 34, 34, 94, 94, 18, W); OutlinedLine(p, 94, 34, 34, 94, 18, W); p.Line(34, 34, 94, 94, 18, W); break;
                case Icon.Restart:
                    for (int i = 0; i <= 18; i++)
                    {
                        float a0 = Mathf.Deg2Rad * (60 + i * 15), a1 = Mathf.Deg2Rad * (60 + (i + 1) * 15);
                        if (i < 18) p.Line(64 + Mathf.Cos(a0) * 38, 64 + Mathf.Sin(a0) * 38, 64 + Mathf.Cos(a1) * 38, 64 + Mathf.Sin(a1) * 38, 30, Ink);
                    }
                    for (int i = 0; i < 18; i++)
                    {
                        float a0 = Mathf.Deg2Rad * (60 + i * 15), a1 = Mathf.Deg2Rad * (60 + (i + 1) * 15);
                        p.Line(64 + Mathf.Cos(a0) * 38, 64 + Mathf.Sin(a0) * 38, 64 + Mathf.Cos(a1) * 38, 64 + Mathf.Sin(a1) * 38, 16, W);
                    }
                    OutlinedPoly(p, W, new float[] { 66, 118, 66, 78, 100, 98 });
                    break;
                case Icon.ArrowLeft: OutlinedPoly(p, W, new float[] { 20, 64, 70, 110, 70, 82, 108, 82, 108, 46, 70, 46, 70, 18 }); break;
                case Icon.ArrowRight: OutlinedPoly(p, W, new float[] { 108, 64, 58, 110, 58, 82, 20, 82, 20, 46, 58, 46, 58, 18 }); break;
                case Icon.Lock:
                    p.Ring(64, 78, 24, 26, Ink); p.Ring(64, 78, 24, 12, new Color(0.7f, 0.74f, 0.84f));
                    OutlinedRect(p, 30, 14, 98, 72, 12, Theme.Gold);
                    p.Circle(64, 48, 8, Ink); p.Line(64, 48, 64, 30, 8, Ink);
                    break;
                case Icon.Check: OutlinedCircle(p, 64, 64, 52, Theme.Green); p.Polyline(W, 16, 36, 66, 56, 44, 94, 86); break;
                case Icon.Hammer:
                    OutlinedLine(p, 40, 22, 82, 76, 16, Theme.Orange);
                    p.Line(40, 22, 82, 76, 6, Theme.Lighter(Theme.Orange, 0.4f));
                    OutlinedPoly(p, new Color(0.62f, 0.67f, 0.78f), new float[] { 54, 92, 84, 122, 120, 86, 90, 56 });
                    p.Polygon(new Color(1, 1, 1, 0.35f), new float[] { 62, 92, 84, 114, 92, 106, 70, 84 });
                    OutlinedRect(p, 94, 96, 112, 114, 3, Theme.Red, 4);
                    break;
                case Icon.Shuffle:
                    OutlinedLine(p, 18, 34, 46, 34, 14, Theme.Blue); OutlinedLine(p, 46, 34, 82, 92, 14, Theme.Blue); OutlinedLine(p, 82, 92, 98, 92, 14, Theme.Blue);
                    OutlinedLine(p, 18, 92, 46, 92, 14, Theme.Purple); OutlinedLine(p, 46, 92, 82, 34, 14, Theme.Purple); OutlinedLine(p, 82, 34, 98, 34, 14, Theme.Purple);
                    p.Line(46, 34, 82, 92, 14, Theme.Blue);
                    OutlinedPoly(p, Theme.Blue, new float[] { 96, 108, 96, 76, 120, 92 }, 4);
                    OutlinedPoly(p, Theme.Purple, new float[] { 96, 50, 96, 18, 120, 34 }, 4);
                    break;
                case Icon.ColorBomb:
                {
                    p.Circle(60, 56, 50, Ink);
                    Color[] seg = { Theme.Red, Theme.Orange, Theme.Gold, Theme.Green, Theme.Blue, Theme.Purple };
                    for (int i = 0; i < 6; i++)
                    {
                        float a0 = i * Mathf.PI / 3, a1 = (i + 1) * Mathf.PI / 3;
                        p.Polygon(seg[i], new float[] { 60, 56, 60 + Mathf.Cos(a0) * 44, 56 + Mathf.Sin(a0) * 44, 60 + Mathf.Cos((a0 + a1) / 2) * 46, 56 + Mathf.Sin((a0 + a1) / 2) * 46, 60 + Mathf.Cos(a1) * 44, 56 + Mathf.Sin(a1) * 44 });
                    }
                    p.Circle(44, 74, 12, new Color(1, 1, 1, 0.55f));
                    OutlinedLine(p, 90, 92, 104, 108, 8, new Color(0.55f, 0.4f, 0.25f), 4);
                    p.Polygon(Theme.Gold, IconPainter.Star(110, 114, 14));
                    break;
                }
                case Icon.Eliminate:
                    OutlinedRect(p, 22, 22, 106, 106, 18, Theme.Red);
                    p.Line(46, 46, 82, 82, 16, W); p.Line(82, 46, 46, 82, 16, W);
                    p.RoundRect(30, 84, 98, 98, 7, new Color(1, 1, 1, 0.25f));
                    break;
                case Icon.Paw:
                    OutlinedCircle(p, 64, 46, 26, Theme.Purple);
                    OutlinedCircle(p, 30, 76, 13, Theme.Purple, 5); OutlinedCircle(p, 52, 96, 13, Theme.Purple, 5); OutlinedCircle(p, 78, 96, 13, Theme.Purple, 5); OutlinedCircle(p, 99, 76, 13, Theme.Purple, 5);
                    break;
                case Icon.Gift:
                    OutlinedRect(p, 22, 14, 106, 80, 8, Theme.Red);
                    OutlinedRect(p, 16, 72, 112, 96, 8, Theme.Lighter(Theme.Red, 0.15f));
                    p.RoundRect(56, 14, 72, 96, 2, Theme.Gold);
                    OutlinedCircle(p, 50, 106, 13, Theme.Gold, 5); OutlinedCircle(p, 78, 106, 13, Theme.Gold, 5);
                    break;
                case Icon.Shop:
                    OutlinedRect(p, 22, 14, 106, 72, 8, W);
                    OutlinedRect(p, 50, 14, 78, 50, 4, Theme.Blue, 4);
                    p.RoundRect(12, 66, 116, 108, 10, Ink);
                    for (int i = 0; i < 4; i++) p.RoundRect(18 + i * 25, 70, 18 + i * 25 + 24, 104, 6, i % 2 == 0 ? Theme.Red : W);
                    break;
                case Icon.Calendar:
                    OutlinedRect(p, 18, 14, 110, 100, 12, W);
                    p.RoundRect(18, 76, 110, 100, 12, Theme.Red); p.RoundRect(18, 76, 110, 86, 0, Theme.Red);
                    for (int r = 0; r < 2; r++) for (int c = 0; c < 4; c++) p.RoundRect(28 + c * 20, 26 + r * 22, 42 + c * 20, 40 + r * 22, 3, r == 0 && c == 1 ? Theme.Red : Theme.Lavender);
                    OutlinedRect(p, 38, 92, 46, 116, 4, new Color(0.7f, 0.74f, 0.84f), 4); OutlinedRect(p, 82, 92, 90, 116, 4, new Color(0.7f, 0.74f, 0.84f), 4);
                    break;
                case Icon.Avatar:
                    OutlinedCircle(p, 64, 64, 54, Theme.Hex("#BFE6FF"));
                    p.Circle(64, 58, 32, Theme.Hex("#FFD7B5"));
                    p.Polygon(Theme.Hex("#7A4A2A"), new float[] { 30, 66, 36, 92, 64, 100, 94, 92, 98, 66, 84, 80, 60, 82, 44, 76 });
                    p.Circle(52, 56, 4, Ink); p.Circle(76, 56, 4, Ink);
                    p.Polyline(Ink, 4, 54, 42, 64, 38, 74, 42);
                    p.Circle(44, 46, 5, new Color(1, 0.55f, 0.6f, 0.6f)); p.Circle(84, 46, 5, new Color(1, 0.55f, 0.6f, 0.6f));
                    break;
                case Icon.Music:
                    OutlinedLine(p, 50, 34, 50, 104, 12, Theme.Purple); OutlinedLine(p, 96, 44, 96, 112, 12, Theme.Purple);
                    OutlinedLine(p, 50, 104, 96, 112, 16, Theme.Purple);
                    OutlinedCircle(p, 38, 32, 16, Theme.Purple, 5); OutlinedCircle(p, 84, 42, 16, Theme.Purple, 5);
                    break;
                case Icon.Sound:
                    OutlinedPoly(p, Theme.Blue, new float[] { 14, 48, 40, 48, 70, 20, 70, 108, 40, 80, 14, 80 });
                    p.Ring(70, 64, 22, 8, Ink); p.Ring(70, 64, 40, 8, Ink);
                    break;
                case Icon.Vibrate:
                    OutlinedRect(p, 40, 14, 88, 114, 12, Theme.Blue);
                    p.RoundRect(47, 26, 81, 96, 6, Theme.Hex("#BFE6FF"));
                    p.Polyline(Ink, 6, 22, 40, 14, 52, 22, 64, 14, 76, 22, 88); p.Polyline(Ink, 6, 106, 40, 114, 52, 106, 64, 114, 76, 106, 88);
                    break;
                case Icon.Trophy:
                    OutlinedRect(p, 36, 12, 92, 26, 4, Theme.GoldDark);
                    OutlinedRect(p, 56, 24, 72, 50, 2, Theme.Gold);
                    OutlinedPoly(p, Theme.Gold, new float[] { 26, 112, 102, 112, 92, 64, 64, 48, 36, 64 });
                    p.Ring(24, 92, 14, 8, Ink); p.Ring(104, 92, 14, 8, Ink);
                    p.Polygon(new Color(1, 1, 1, 0.5f), IconPainter.Star(64, 88, 14));
                    break;
                case Icon.Cube:
                    OutlinedPoly(p, Theme.Gold, new float[] { 64, 118, 112, 92, 112, 36, 64, 10, 16, 36, 16, 92 });
                    p.Polygon(Theme.Lighter(Theme.Gold, 0.45f), new float[] { 64, 118, 112, 92, 64, 66, 16, 92 });
                    p.Polygon(Theme.GoldDark, new float[] { 64, 66, 112, 92, 112, 36, 64, 10 });
                    break;
                case Icon.Lightning: OutlinedPoly(p, Theme.Orange, new float[] { 74, 122, 26, 58, 58, 58, 46, 6, 102, 74, 70, 74 }); break;
                case Icon.Video:
                    OutlinedRect(p, 12, 26, 92, 102, 14, Theme.Purple);
                    OutlinedPoly(p, Theme.Purple, new float[] { 92, 64, 118, 92, 118, 36 }, 5);
                    p.Polygon(W, new float[] { 42, 44, 42, 84, 72, 64 });
                    break;
                case Icon.Home:
                    OutlinedPoly(p, Theme.Red, new float[] { 64, 116, 118, 66, 10, 66 });
                    OutlinedRect(p, 24, 14, 104, 70, 6, W);
                    OutlinedRect(p, 54, 14, 74, 46, 4, Theme.Blue, 4);
                    break;
                case Icon.Sparkle:
                    p.Polygon(W, new float[] { 64, 124, 74, 74, 124, 64, 74, 54, 64, 4, 54, 54, 4, 64, 54, 74 });
                    break;
                case Icon.Leaf:
                {
                    var leaf = new System.Collections.Generic.List<float>();
                    for (int i = 0; i <= 20; i++) { float t = i / 20f; leaf.Add(14 + t * 100); leaf.Add(64 + Mathf.Sin(t * Mathf.PI) * 34); }
                    for (int i = 20; i >= 0; i--) { float t = i / 20f; leaf.Add(14 + t * 100); leaf.Add(64 - Mathf.Sin(t * Mathf.PI) * 26); }
                    OutlinedPoly(p, Theme.Hex("#5CCB52"), leaf.ToArray(), 5);
                    p.Line(18, 64, 104, 66, 5, Theme.Hex("#3E9E3E"));
                    break;
                }
                case Icon.Flower:
                    for (int i = 0; i < 5; i++) { float a = i * Mathf.PI * 2 / 5 + 0.3f; OutlinedCircle(p, 64 + Mathf.Cos(a) * 30, 64 + Mathf.Sin(a) * 30, 24, Theme.Hex("#FF7EC2"), 4); }
                    OutlinedCircle(p, 64, 64, 18, Theme.Gold, 4);
                    break;
                case Icon.CubePink:
                    OutlinedPoly(p, Theme.Hex("#FF6FB5"), new float[] { 64, 118, 112, 92, 112, 36, 64, 10, 16, 36, 16, 92 });
                    p.Polygon(Theme.Hex("#FFB0D8"), new float[] { 64, 118, 112, 92, 64, 66, 16, 92 });
                    p.Polygon(Theme.Hex("#E0489A"), new float[] { 64, 66, 112, 92, 112, 36, 64, 10 });
                    break;
                case Icon.CubeBlue:
                    OutlinedPoly(p, Theme.Hex("#59B8FF"), new float[] { 64, 118, 112, 92, 112, 36, 64, 10, 16, 36, 16, 92 });
                    p.Polygon(Theme.Hex("#A9DCFF"), new float[] { 64, 118, 112, 92, 64, 66, 16, 92 });
                    p.Polygon(Theme.Hex("#2F8FE0"), new float[] { 64, 66, 112, 92, 112, 36, 64, 10 });
                    break;
                case Icon.PawGold:
                    OutlinedCircle(p, 64, 46, 26, Theme.Gold);
                    OutlinedCircle(p, 30, 76, 13, Theme.Gold, 5); OutlinedCircle(p, 52, 96, 13, Theme.Gold, 5); OutlinedCircle(p, 78, 96, 13, Theme.Gold, 5); OutlinedCircle(p, 99, 76, 13, Theme.Gold, 5);
                    break;
                case Icon.CalendarStar:
                    Draw(p, Icon.Calendar);
                    p.Polygon(Ink, IconPainter.Star(96, 34, 30), null, 5);
                    p.Polygon(Theme.Gold, IconPainter.Star(96, 34, 30));
                    break;
                case Icon.Gem:
                    OutlinedPoly(p, Theme.Hex("#B347FF"), new float[] { 64, 10, 118, 74, 92, 104, 36, 104, 10, 74 });
                    p.Polygon(Theme.Hex("#E2A8FF"), new float[] { 36, 104, 64, 74, 92, 104 });
                    p.Polygon(Theme.Hex("#8A2BE2"), new float[] { 64, 10, 118, 74, 64, 74 });
                    p.Polygon(new Color(1, 1, 1, 0.55f), new float[] { 40, 84, 54, 84, 64, 40 });
                    break;
                case Icon.Crown:
                    OutlinedPoly(p, Theme.Gold, new float[] { 14, 30, 40, 70, 64, 22, 88, 70, 114, 30, 104, 100, 24, 100 });
                    p.RoundRect(22, 22, 106, 38, 6, Theme.GoldDark);
                    OutlinedCircle(p, 14, 104, 9, Theme.Gold, 4); OutlinedCircle(p, 64, 110, 10, Theme.Gold, 4); OutlinedCircle(p, 114, 104, 9, Theme.Gold, 4);
                    p.Circle(64, 62, 9, Theme.Red); p.Circle(38, 58, 7, Theme.Blue); p.Circle(90, 58, 7, Theme.Green);
                    break;
                case Icon.Eye:
                    OutlinedPoly(p, W, new float[] { 8, 64, 36, 92, 64, 100, 92, 92, 120, 64, 92, 36, 64, 28, 36, 36 });
                    OutlinedCircle(p, 64, 64, 22, Theme.Blue, 4); p.Circle(64, 64, 10, Ink); p.Circle(58, 72, 5, W);
                    break;
            }
        }

        static float[] Heart(float cx, float cy, float s)
        {
            var pts = new List<float>();
            for (int i = 0; i < 48; i++)
            {
                float t = i / 48f * Mathf.PI * 2;
                float x = 16 * Mathf.Pow(Mathf.Sin(t), 3);
                float y = 13 * Mathf.Cos(t) - 5 * Mathf.Cos(2 * t) - 2 * Mathf.Cos(3 * t) - Mathf.Cos(4 * t);
                pts.Add(cx + x * s / 17f);
                pts.Add(cy + y * s / 17f);
            }
            return pts.ToArray();
        }
    }
}
