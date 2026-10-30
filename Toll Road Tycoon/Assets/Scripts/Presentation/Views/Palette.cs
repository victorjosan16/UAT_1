using UnityEngine;

namespace TollRoadTycoon.Presentation
{
    /// <summary>Art direction palette from the mockup.</summary>
    public static class Palette
    {
        public static readonly Color Blue = Hex(0x0879C7);
        public static readonly Color DarkBlue = Hex(0x064D83);
        public static readonly Color Green = Hex(0x31C653);
        public static readonly Color DarkGreen = Hex(0x1F9A3C);
        public static readonly Color Grass = Hex(0xA8D78D);
        public static readonly Color GrassDark = Hex(0x8CC572);
        public static readonly Color Asphalt = Hex(0x697A88);
        public static readonly Color AsphaltDark = Hex(0x56656F);
        public static readonly Color Cream = Hex(0xF5E8D2);
        public static readonly Color Gold = Hex(0xFFCB36);
        public static readonly Color Red = Hex(0xF05B54);
        public static readonly Color White = Color.white;
        public static readonly Color Ink = Hex(0x1C2B3A);
        public static readonly Color Shadow = new Color(0f, 0f, 0f, 0.22f);
        public static readonly Color Disabled = Hex(0x9AA7B2);
        public static readonly Color PanelLight = Hex(0xEAF4FC);
        public static readonly Color TreeDark = Hex(0x3E9B47);
        public static readonly Color TreeLight = Hex(0x5BBE5A);
        public static readonly Color Glass = Hex(0x2D4A63);

        public static readonly Color[] CarColors =
        {
            Hex(0xF05B54), Hex(0xFFCB36), Hex(0x2F8FE0), Hex(0x31C653), Hex(0xFF8A3D), Hex(0xFFFFFF), Hex(0x8E6BE8), Hex(0x30C7C2),
        };

        public static Color Hex(int rgb)
        {
            return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);
        }

        public static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, a);
    }
}
