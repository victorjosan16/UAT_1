using System.Collections.Generic;
using UnityEngine;
using Veliqora.UI;

namespace Veliqora.Presentation
{
    /// <summary>
    /// Liquid colors (clean premium palette, separated hues on the #101827 background) plus
    /// colorblind-assist symbols, one distinct shape per ColorId.
    /// </summary>
    public static class LiquidPalette
    {
        // Clean premium palette (exact brand values). Levels use at most 8 colors (StagePlanner.MaxColors), ordered
        // so that early levels with few colors get the most distinct hues. Entries 9–12 are calm extras kept only
        // as a safety net for custom content; generated levels never use them.
        private static readonly Color[] Colors =
        {
            UIStyle.Hex("#FF6473"), // 1 coral
            UIStyle.Hex("#3995F6"), // 2 blue
            UIStyle.Hex("#31D6A0"), // 3 mint
            UIStyle.Hex("#FFD35A"), // 4 yellow
            UIStyle.Hex("#A58BFA"), // 5 violet
            UIStyle.Hex("#FF9B54"), // 6 orange
            UIStyle.Hex("#32C9DA"), // 7 turquoise
            UIStyle.Hex("#F47FC2"), // 8 pink
            UIStyle.Hex("#9BD45B"), // 9 lime (extra)
            UIStyle.Hex("#C9A27A"), // 10 sand (extra)
            UIStyle.Hex("#E8ECF4"), // 11 pearl (extra)
            UIStyle.Hex("#8C96AE"), // 12 slate (extra)
        };

        public static readonly string[] Names = { "Coral", "Blue", "Mint", "Yellow", "Violet", "Orange", "Turquoise", "Pink", "Lime", "Sand", "Pearl", "Slate" };

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
