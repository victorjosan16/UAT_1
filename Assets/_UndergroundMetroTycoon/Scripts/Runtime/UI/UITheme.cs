using UnityEngine;

namespace UndergroundMetroTycoon.Game
{
    /// <summary>
    /// Design tokens for the whole UI: colours, typography, sizes, spacing and motion. Every UI element takes
    /// its values from here so the look stays consistent and can be tuned in one place.
    /// </summary>
    public static class UITheme
    {
        // Surfaces
        public static readonly Color NavyDeep = Palette.Hex(0x0B1730);
        public static readonly Color Navy = Palette.Hex(0x13254A);
        public static readonly Color NavyRaised = Palette.Hex(0x1D3466);
        public static readonly Color PanelGlass = new Color(0.07f, 0.13f, 0.27f, 0.90f);
        public static readonly Color CardWhite = Palette.Hex(0xFFFFFF);
        public static readonly Color CardPale = Palette.Hex(0xEAF3FF);
        public static readonly Color CardLocked = Palette.Hex(0xD5DEEA);
        public static readonly Color Scrim = new Color(0.03f, 0.06f, 0.14f, 0.70f);

        // Interactive + semantic
        public static readonly Color Blue = Palette.Hex(0x2F7BFF);
        public static readonly Color BlueDark = Palette.Hex(0x1C55C4);
        public static readonly Color BlueLight = Palette.Hex(0x7FC0FF);
        public static readonly Color Green = Palette.Hex(0x2DBE60);
        public static readonly Color GreenDark = Palette.Hex(0x1C8E45);
        public static readonly Color Gold = Palette.Hex(0xFFC52E);
        public static readonly Color GoldDark = Palette.Hex(0xD68E00);
        public static readonly Color Orange = Palette.Hex(0xFF9A3C);
        public static readonly Color OrangeDark = Palette.Hex(0xD9671A);
        public static readonly Color Red = Palette.Hex(0xF0544F);
        public static readonly Color RedDark = Palette.Hex(0xB8312D);
        public static readonly Color Turquoise = Palette.Hex(0x34D1C6);

        // Text
        public static readonly Color TextOnDark = Palette.Hex(0xFFFFFF);
        public static readonly Color TextOnDarkMuted = Palette.Hex(0xA9C2E6);
        public static readonly Color TextDark = Palette.Hex(0x1B2A44);
        public static readonly Color TextMuted = Palette.Hex(0x6B7F99);

        // Typography (reference canvas is 1080 px wide)
        public const int SizeCaption = 26;
        public const int SizeBody = 32;
        public const int SizeLabel = 36;
        public const int SizeTitle = 44;
        public const int SizeHeadline = 54;
        public const int SizeDisplay = 72;

        // Spacing and shape
        public const float SpaceS = 12f;
        public const float SpaceM = 24f;
        public const float SpaceL = 36f;
        public const float ButtonEdge = 8f;
        public const float CornerCard = 0.8f;
        public const float CornerButton = 0.7f;
        public const float CornerPill = 1.4f;

        // Motion (seconds)
        public const float MotionFast = 0.15f;
        public const float MotionNormal = 0.25f;
        public const float MotionSlow = 0.35f;

        private static Font _heavy;
        private static Font _body;

        /// <summary>Baloo 2 ExtraBold (SIL OFL) for titles, numbers and buttons. Falls back to Unity's built-in font.</summary>
        public static Font Heavy => _heavy != null ? _heavy : _heavy = LoadFont("Fonts/Baloo2-ExtraBold");

        /// <summary>Baloo 2 SemiBold (SIL OFL) for body text.</summary>
        public static Font Body => _body != null ? _body : _body = LoadFont("Fonts/Baloo2-SemiBold");

        private static Font LoadFont(string path)
        {
            var font = Resources.Load<Font>(path);
            return font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        /// <summary>Formats a number the Romanian way: 1.250, 12.000.</summary>
        public static string Number(long value) =>
            value.ToString("N0", System.Globalization.CultureInfo.InvariantCulture).Replace(',', '.');
    }
}
