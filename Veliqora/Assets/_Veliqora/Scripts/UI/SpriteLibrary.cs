using System;
using System.Collections.Generic;
using UnityEngine;

namespace Veliqora.UI
{
    public enum Icon
    {
        Coin, Star, StarEmpty, Lock, Check, Plus, Gear, Pause, Back, Undo, Shuffle, Bulb, BottlePlus,
        Play, Close, Home, Restart, Chest, ChestOpen, Droplet, Map, Bag, Palette, Ad,
    }

    /// <summary>
    /// Lazily generated, cached procedural sprites (PLACEHOLDER ART). An authored sprite placed at
    /// Resources/Veliqora/Art/Icons/&lt;IconName&gt; (or Panels/…) overrides the generated one automatically.
    /// </summary>
    public static class SpriteLibrary
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static void ClearCache() => Cache.Clear();

        private static Sprite Get(string key, Func<Sprite> build)
        {
            Sprite s;
            if (Cache.TryGetValue(key, out s) && s != null) return s;
            s = build();
            if (s == null) return null;
            s.name = "proc_" + key;
            Cache[key] = s;
            return s;
        }

        private static Sprite Override(string path) => Art(path);

        /// <summary>
        /// Authored art under Resources/Veliqora/Art/&lt;path&gt; (PNG imported as a texture by VeliqoraArtImporter),
        /// turned into a sprite with an optional 9-slice border. Returns null when the file doesn't exist.
        /// </summary>
        public static Sprite Art(string path, Vector4 border = default(Vector4))
        {
            string key = "art_" + path + "_" + border;
            Sprite s;
            if (Cache.TryGetValue(key, out s)) return s;
            s = Resources.Load<Sprite>("Veliqora/Art/" + path);
            if (s == null)
            {
                var tex = Resources.Load<Texture2D>("Veliqora/Art/" + path);
                if (tex != null)
                {
                    s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
                    s.name = "art_" + path;
                }
            }
            Cache[key] = s; // caches misses too (null), so absent art costs one lookup
            return s;
        }

        /// <summary>Full round button art (e.g. Buttons/Undo), used instead of circle + glyph when present.</summary>
        public static Sprite ButtonArt(Icon icon) => Art("Buttons/" + icon);

        /// <summary>Authored 9-slice pill for a button style (Primary = green, Accent = purple), or null.</summary>
        public static Sprite Pill(UIStyle.ButtonKind kind)
        {
            string name = kind == UIStyle.ButtonKind.Primary ? "pill_green" : (kind == UIStyle.ButtonKind.Accent ? "pill_purple" : null);
            if (name == null) return null;
            var probe = Art("Buttons/" + name);
            if (probe == null) return null;
            float h = probe.rect.height, w = probe.rect.width;
            return Art("Buttons/" + name, new Vector4(h * 0.5f, h * 0.45f, h * 0.5f, h * 0.45f));
        }

        /// <summary>Wooden title sign, horizontally 9-sliced.</summary>
        public static Sprite TitleSign()
        {
            var probe = Art("Buttons/sign_wood");
            if (probe == null) return null;
            float w = probe.rect.width;
            return Art("Buttons/sign_wood", new Vector4(w * 0.16f, 0, w * 0.16f, 0));
        }

        // ---------------- panels ----------------

        /// <summary>White 9-sliced rounded rectangle; tint with Image.color.</summary>
        public static Sprite Rounded(float radius)
        {
            int r = Mathf.RoundToInt(radius);
            return Get("rounded" + r, () =>
            {
                int size = r * 2 + 8;
                var p = new PixelPainter(size, size);
                var rect = new Rect(1, 1, size - 2, size - 2);
                p.Fill(q => PixelPainter.RoundedRect(q, rect, r), Color.white);
                return p.ToSprite(new Vector4(r + 2, r + 2, r + 2, r + 2));
            });
        }

        /// <summary>Soft blurred rounded rect used for drop shadows and glows.</summary>
        public static Sprite SoftRounded(float radius, float blur)
        {
            int r = Mathf.RoundToInt(radius), b = Mathf.RoundToInt(blur);
            return Get("soft" + r + "_" + b, () =>
            {
                int size = (r + b) * 2 + 8;
                var p = new PixelPainter(size, size);
                var rect = new Rect(b + 2, b + 2, size - 2 * b - 4, size - 2 * b - 4);
                p.Fill(q => PixelPainter.RoundedRect(q, rect, r) + b * 0.5f, Color.white, null, null, b);
                int border = r + b + 3;
                return p.ToSprite(new Vector4(border, border, border, border));
            });
        }

        public static Sprite Circle => Get("circle", () =>
        {
            var p = new PixelPainter(128, 128);
            p.Fill(q => PixelPainter.Circle(q, new Vector2(64, 64), 62), Color.white);
            return p.ToSprite();
        });

        public static Sprite RadialGlow => Get("glow", () =>
        {
            var p = new PixelPainter(128, 128);
            var c = new Vector2(64, 64);
            p.Fill(q => PixelPainter.Circle(q, c, 63), Color.white, q =>
            {
                float t = Mathf.Clamp01((q - c).magnitude / 63f);
                return new Color(1, 1, 1, Mathf.Pow(1f - t, 2.2f));
            });
            return p.ToSprite();
        });

        public static Sprite Pixel => Get("pixel", () =>
        {
            var p = new PixelPainter(4, 4, Color.white);
            return p.ToSprite();
        });

        // ---------------- icons ----------------

        public static Sprite Get(Icon icon)
        {
            return Get("icon_" + icon, () =>
            {
                var authored = Override("Icons/" + icon);
                return authored != null ? authored : BuildIcon(icon);
            });
        }

        private static Sprite BuildIcon(Icon icon) => PaintIcon(icon).ToSprite();

        /// <summary>Rasterizes an icon (pure managed; also used by the art preview tool).</summary>
        public static PixelPainter PaintIcon(Icon icon)
        {
            const int S = 128;
            var p = new PixelPainter(S, S);
            var c = new Vector2(64, 64);
            Color w = Color.white;
            switch (icon)
            {
                case Icon.Coin:
                {
                    var gold = UIStyle.Hex("#FFC93C");
                    var dark = UIStyle.Hex("#E08A12");
                    p.Fill(q => PixelPainter.Circle(q, c, 60), dark);
                    p.Fill(q => PixelPainter.Circle(q, c + new Vector2(0, 3), 54), gold, q =>
                        Color.Lerp(UIStyle.Hex("#FFB21E"), UIStyle.Hex("#FFE27A"), Mathf.Clamp01((q.y - 10) / 110f)));
                    p.Fill(q => PixelPainter.Circle(q, c + new Vector2(0, 3), 38), UIStyle.WithAlpha(dark, 0.35f));
                    p.Fill(q => PixelPainter.Polygon(q, PixelPainter.Star(c + new Vector2(0, 3), 26, 11)), UIStyle.Hex("#FFF2B8"));
                    break;
                }
                case Icon.Star:
                case Icon.StarEmpty:
                {
                    bool full = icon == Icon.Star;
                    var outer = PixelPainter.Star(c, 60, 27);
                    var inner = PixelPainter.Star(c + new Vector2(0, 2), 50, 22);
                    p.Fill(q => PixelPainter.Polygon(q, outer) - 3, full ? UIStyle.Hex("#E08A12") : UIStyle.Hex("#9C93A8"));
                    p.Fill(q => PixelPainter.Polygon(q, inner) - 2, w, q => full
                        ? Color.Lerp(UIStyle.Hex("#FFB21E"), UIStyle.Hex("#FFE98A"), q.y / 128f)
                        : Color.Lerp(UIStyle.Hex("#C9C2D6"), UIStyle.Hex("#E6E1EE"), q.y / 128f));
                    break;
                }
                case Icon.Lock:
                    p.Fill(q => PixelPainter.Arc(q, new Vector2(64, 74), 24, 0, Mathf.PI, 7), w);
                    p.Fill(q => PixelPainter.RoundedRect(q, new Rect(28, 14, 72, 60), 12), w);
                    p.Fill(q => PixelPainter.Circle(q, new Vector2(64, 46), 8), new Color(0, 0, 0, 0.35f));
                    break;
                case Icon.Check:
                    p.Fill(q => Mathf.Min(PixelPainter.Segment(q, new Vector2(28, 66), new Vector2(54, 40), 10),
                                          PixelPainter.Segment(q, new Vector2(54, 40), new Vector2(102, 90), 10)), w);
                    break;
                case Icon.Plus:
                    p.Fill(q => Mathf.Min(PixelPainter.Segment(q, new Vector2(64, 24), new Vector2(64, 104), 12),
                                          PixelPainter.Segment(q, new Vector2(24, 64), new Vector2(104, 64), 12)), w);
                    break;
                case Icon.Gear:
                    p.Fill(q =>
                    {
                        var d = q - c;
                        float a = Mathf.Atan2(d.y, d.x);
                        float teeth = 6f * Mathf.Clamp(Mathf.Cos(a * 8f) * 3f, -1f, 1f);
                        return PixelPainter.Subtract(d.magnitude - (42 + teeth), d.magnitude - 17);
                    }, w);
                    break;
                case Icon.Pause:
                    p.Fill(q => Mathf.Min(PixelPainter.RoundedRect(q, new Rect(34, 24, 22, 80), 7), PixelPainter.RoundedRect(q, new Rect(72, 24, 22, 80), 7)), w);
                    break;
                case Icon.Back:
                    p.Fill(q => Mathf.Min(PixelPainter.Segment(q, new Vector2(80, 104), new Vector2(40, 64), 11),
                                          PixelPainter.Segment(q, new Vector2(40, 64), new Vector2(80, 24), 11)), w);
                    break;
                case Icon.Undo:
                    p.Fill(q => Mathf.Min(PixelPainter.Arc(q, new Vector2(66, 58), 34, -1.6f, 2.6f, 9),
                                Mathf.Min(PixelPainter.Segment(q, new Vector2(22, 92), new Vector2(38, 70), 9),
                                          PixelPainter.Segment(q, new Vector2(22, 92), new Vector2(52, 98), 9))), w);
                    break;
                case Icon.Restart:
                    p.Fill(q => Mathf.Min(PixelPainter.Arc(q, c, 38, 0.6f, 5.6f, 9),
                                Mathf.Min(PixelPainter.Segment(q, new Vector2(98, 92), new Vector2(98, 62), 9),
                                          PixelPainter.Segment(q, new Vector2(98, 92), new Vector2(70, 94), 9))), w);
                    break;
                case Icon.Shuffle:
                {
                    Func<Vector2, float> a1 = q => PixelPainter.Segment(q, new Vector2(20, 36), new Vector2(104, 92), 8);
                    Func<Vector2, float> a2 = q => PixelPainter.Segment(q, new Vector2(20, 92), new Vector2(104, 36), 8);
                    p.Fill(q => Mathf.Min(a1(q), a2(q)), w);
                    p.Fill(q => PixelPainter.Polygon(q, new[] { new Vector2(112, 98), new Vector2(84, 100), new Vector2(104, 76) }), w);
                    p.Fill(q => PixelPainter.Polygon(q, new[] { new Vector2(112, 30), new Vector2(104, 52), new Vector2(84, 28) }), w);
                    break;
                }
                case Icon.Bulb:
                    p.Fill(q => PixelPainter.Circle(q, new Vector2(64, 76), 32), w);
                    p.Fill(q => PixelPainter.RoundedRect(q, new Rect(46, 26, 36, 30), 6), w);
                    p.Fill(q => PixelPainter.RoundedRect(q, new Rect(50, 12, 28, 12), 5), w);
                    p.Fill(q => PixelPainter.Segment(q, new Vector2(54, 80), new Vector2(64, 64), 3) , new Color(0, 0, 0, 0.25f));
                    break;
                case Icon.BottlePlus:
                    p.Fill(q => PixelPainter.Subtract(PixelPainter.RoundedRect(q, new Rect(30, 8, 50, 86), 20), PixelPainter.RoundedRect(q, new Rect(40, 18, 30, 66), 12)), w);
                    p.Fill(q => PixelPainter.RoundedRect(q, new Rect(42, 92, 26, 26), 6), w);
                    p.Fill(q => PixelPainter.Circle(q, new Vector2(94, 34), 28), UIStyle.Hex("#3CCB6B"));
                    p.Fill(q => Mathf.Min(PixelPainter.Segment(q, new Vector2(94, 20), new Vector2(94, 48), 6), PixelPainter.Segment(q, new Vector2(80, 34), new Vector2(108, 34), 6)), w);
                    break;
                case Icon.Play:
                    p.Fill(q => PixelPainter.Polygon(q, new[] { new Vector2(42, 20), new Vector2(42, 108), new Vector2(110, 64) }) - 6, w);
                    break;
                case Icon.Close:
                    p.Fill(q => Mathf.Min(PixelPainter.Segment(q, new Vector2(32, 32), new Vector2(96, 96), 10), PixelPainter.Segment(q, new Vector2(32, 96), new Vector2(96, 32), 10)), w);
                    break;
                case Icon.Home:
                    p.Fill(q => PixelPainter.Polygon(q, new[] { new Vector2(16, 64), new Vector2(64, 108), new Vector2(112, 64) }) - 2, w);
                    p.Fill(q => PixelPainter.Subtract(PixelPainter.RoundedRect(q, new Rect(30, 16, 68, 56), 6), PixelPainter.RoundedRect(q, new Rect(54, 16, 20, 32), 4)), w);
                    break;
                case Icon.Chest:
                case Icon.ChestOpen:
                {
                    var wood = UIStyle.Hex("#B5652B");
                    var woodDark = UIStyle.Hex("#7A3E16");
                    var gold = UIStyle.Hex("#FFC93C");
                    p.Fill(q => PixelPainter.RoundedRect(q, new Rect(12, 14, 104, 56), 10), woodDark);
                    p.Fill(q => PixelPainter.RoundedRect(q, new Rect(18, 20, 92, 44), 6), wood);
                    if (icon == Icon.Chest)
                        p.Fill(q => PixelPainter.RoundedRect(q, new Rect(12, 66, 104, 44), 20), woodDark);
                    else
                    {
                        p.Fill(q => PixelPainter.Circle(q, new Vector2(64, 70), 26), UIStyle.WithAlpha(gold, 0.9f));
                        p.Fill(q => PixelPainter.RoundedRect(q, new Rect(12, 92, 104, 26), 12), woodDark);
                    }
                    p.Fill(q => PixelPainter.RoundedRect(q, new Rect(54, 50, 20, 26), 5), gold);
                    break;
                }
                case Icon.Droplet:
                    p.Fill(q => PixelPainter.Droplet(q, new Vector2(64, 46), 38), w, q =>
                    {
                        float hue = Mathf.Repeat(0.55f + (q.x + q.y) / 300f, 1f);
                        var col = Color.HSVToRGB(hue, 0.65f, 1f);
                        return Color.Lerp(col, Color.white, Mathf.Clamp01((q.y - 70) / 60f) * 0.4f);
                    });
                    p.Fill(q => PixelPainter.Circle(q, new Vector2(50, 54), 9), new Color(1, 1, 1, 0.75f));
                    break;
                case Icon.Map:
                    p.Fill(q => PixelPainter.Polygon(q, new[] { new Vector2(16, 24), new Vector2(46, 34), new Vector2(46, 110), new Vector2(16, 100) }), w);
                    p.Fill(q => PixelPainter.Polygon(q, new[] { new Vector2(50, 34), new Vector2(78, 24), new Vector2(78, 100), new Vector2(50, 110) }), UIStyle.WithAlpha(w, 0.75f));
                    p.Fill(q => PixelPainter.Polygon(q, new[] { new Vector2(82, 24), new Vector2(112, 34), new Vector2(112, 110), new Vector2(82, 100) }), w);
                    break;
                case Icon.Bag:
                    p.Fill(q => PixelPainter.Arc(q, new Vector2(64, 82), 22, 0, Mathf.PI, 6), w);
                    p.Fill(q => PixelPainter.RoundedRect(q, new Rect(22, 12, 84, 74), 16), w);
                    break;
                case Icon.Palette:
                    p.Fill(q => PixelPainter.Subtract(PixelPainter.Circle(q, c, 54), PixelPainter.Circle(q, new Vector2(84, 44), 13)), w);
                    p.Fill(q => PixelPainter.Circle(q, new Vector2(42, 80), 10), UIStyle.Coral);
                    p.Fill(q => PixelPainter.Circle(q, new Vector2(68, 92), 10), UIStyle.Sunshine);
                    p.Fill(q => PixelPainter.Circle(q, new Vector2(92, 74), 10), UIStyle.Mint);
                    p.Fill(q => PixelPainter.Circle(q, new Vector2(38, 52), 10), UIStyle.Violet);
                    break;
                case Icon.Ad:
                    p.Fill(q => PixelPainter.RoundedRect(q, new Rect(10, 26, 108, 76), 16), w);
                    p.Fill(q => PixelPainter.Polygon(q, new[] { new Vector2(52, 44), new Vector2(52, 84), new Vector2(84, 64) }), UIStyle.Violet);
                    break;
            }
            return p;
        }
    }
}
