using UnityEngine;

namespace VoxelBloom
{
    /// <summary>Design tokens matching the reference mockup (pastel sky, white panels, glossy buttons).</summary>
    public static class Theme
    {
        public static readonly Color SkyTop = Hex("#7CC8FF");
        public static readonly Color SkyBottom = Hex("#EAF7FF");
        public static readonly Color GameSkyTop = Hex("#9AD8FF");
        public static readonly Color GameSkyBottom = Hex("#DDF2FF");
        public static readonly Color Panel = Color.white;
        public static readonly Color PanelSoft = Hex("#F4F1FF");
        public static readonly Color PanelLine = Hex("#E3DCFA");
        public static readonly Color Ink = Hex("#3B2F5C");
        public static readonly Color InkSoft = Hex("#7A6F99");
        public static readonly Color Purple = Hex("#8E5BEA");
        public static readonly Color PurpleDark = Hex("#6436C4");
        public static readonly Color Lavender = Hex("#C9B6FF");
        public static readonly Color Green = Hex("#3FCB4C");
        public static readonly Color GreenDark = Hex("#22963A");
        public static readonly Color Blue = Hex("#3F8EFF");
        public static readonly Color BlueDark = Hex("#2763C9");
        public static readonly Color Gold = Hex("#FFC531");
        public static readonly Color GoldDark = Hex("#E59A0E");
        public static readonly Color Red = Hex("#FF4D5E");
        public static readonly Color Orange = Hex("#FF9A2E");
        public static readonly Color SlotEmpty = Hex("#DCE9FA");
        public static readonly Color GameBackground = Hex("#EAF6FF");
        public static readonly Color GamePanel = Hex("#F8FAFF");
        public static readonly Color Navy = Hex("#1F2A5C");
        public static readonly Color Shadow = new Color(0.24f, 0.16f, 0.45f, 0.22f);
        public static readonly Color Backdrop = new Color(0.12f, 0.08f, 0.22f, 0.55f);

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }

        public static Color Darker(Color c, float k = 0.75f) => new Color(c.r * k, c.g * k, c.b * k, c.a);
        public static Color Lighter(Color c, float k = 0.35f) => Color.Lerp(c, Color.white, k);
    }
}
