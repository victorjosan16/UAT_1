using System.Collections.Generic;
using UnityEngine;
using Veliqora.UI;

namespace Veliqora.Presentation
{
    /// <summary>
    /// Liquid colors (chosen for hue separation on both dark and light gameplay backgrounds) plus
    /// colorblind-assist symbols, one distinct shape per ColorId.
    /// </summary>
    public static class LiquidPalette
    {
        // Vibrant neon set tuned for dark backgrounds; ordered so early levels (few colors) get the most distinct hues.
        private static readonly Color[] Colors =
        {
            UIStyle.Hex("#2F7BFF"), // 1 electric blue
            UIStyle.Hex("#FF3FA4"), // 2 hot pink
            UIStyle.Hex("#2EF07A"), // 3 neon green
            UIStyle.Hex("#FFE135"), // 4 neon yellow
            UIStyle.Hex("#A24DFF"), // 5 violet
            UIStyle.Hex("#FF8A1F"), // 6 neon orange
            UIStyle.Hex("#1FE8FF"), // 7 cyan
            UIStyle.Hex("#FF2E4D"), // 8 red
            UIStyle.Hex("#C6FF3A"), // 9 lime
            UIStyle.Hex("#B07A45"), // 10 amber brown
            UIStyle.Hex("#F4F1FF"), // 11 pearl white
            UIStyle.Hex("#8C93B8"), // 12 moonstone grey
        };

        public static readonly string[] Names = { "Blue", "Pink", "Green", "Yellow", "Violet", "Orange", "Cyan", "Red", "Lime", "Amber", "Pearl", "Moonstone" };

        public static Color Get(int colorId)
        {
            if (colorId <= 0) return Color.clear;
            return Colors[(colorId - 1) % Colors.Length];
        }

        private static readonly Dictionary<int, Sprite> SymbolCache = new Dictionary<int, Sprite>();

        /// <summary>White symbol with dark rim, drawn on top of each liquid unit when colorblind assist is on.</summary>
        public static Sprite Symbol(int colorId)
        {
            Sprite s;
            if (SymbolCache.TryGetValue(colorId, out s) && s != null) return s;
            var p = new PixelPainter(64, 64);
            var c = new Vector2(32, 32);
            System.Func<Vector2, float> sdf;
            switch ((colorId - 1) % 12)
            {
                case 0: sdf = q => PixelPainter.Circle(q, c, 16); break;
                case 1: sdf = q => PixelPainter.Polygon(q, new[] { new Vector2(32, 52), new Vector2(12, 16), new Vector2(52, 16) }); break;
                case 2: sdf = q => PixelPainter.RoundedRect(q, new Rect(15, 15, 34, 34), 4); break;
                case 3: sdf = q => PixelPainter.Polygon(q, new[] { new Vector2(32, 54), new Vector2(54, 32), new Vector2(32, 10), new Vector2(10, 32) }); break;
                case 4: sdf = q => PixelPainter.Polygon(q, PixelPainter.Star(c, 22, 9)); break;
                case 5: sdf = q => Mathf.Min(PixelPainter.Segment(q, new Vector2(32, 12), new Vector2(32, 52), 6), PixelPainter.Segment(q, new Vector2(12, 32), new Vector2(52, 32), 6)); break;
                case 6: sdf = q => Mathf.Abs(PixelPainter.Circle(q, c, 15)) - 5; break;
                case 7: sdf = q => Mathf.Min(PixelPainter.Segment(q, new Vector2(15, 15), new Vector2(49, 49), 6), PixelPainter.Segment(q, new Vector2(15, 49), new Vector2(49, 15), 6)); break;
                case 8: sdf = q => PixelPainter.Polygon(q, Hex(c, 19)); break;
                case 9: sdf = q => PixelPainter.Polygon(q, new[] { new Vector2(12, 46), new Vector2(52, 46), new Vector2(32, 12) }) ; break;
                case 10: sdf = q => Mathf.Min(PixelPainter.Circle(q, new Vector2(20, 32), 8), Mathf.Min(PixelPainter.Circle(q, new Vector2(32, 32), 8), PixelPainter.Circle(q, new Vector2(44, 32), 8))); break;
                default: sdf = q => PixelPainter.RoundedRect(q, new Rect(12, 24, 40, 16), 6); break;
            }
            p.Fill(q => sdf(q) - 3.5f, new Color(0.1f, 0.08f, 0.2f, 0.65f));
            p.Fill(sdf, Color.white);
            s = p.ToSprite();
            SymbolCache[colorId] = s;
            return s;
        }

        private static Vector2[] Hex(Vector2 c, float r)
        {
            var v = new Vector2[6];
            for (int i = 0; i < 6; i++)
            {
                float a = (30 + i * 60) * Mathf.Deg2Rad;
                v[i] = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
            }
            return v;
        }
    }
}
