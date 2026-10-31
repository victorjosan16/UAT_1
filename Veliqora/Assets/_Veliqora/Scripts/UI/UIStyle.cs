using UnityEngine;

namespace Veliqora.UI
{
    /// <summary>
    /// Design tokens for the whole app (reference resolution 1080x1920 portrait). Every screen pulls colors,
    /// sizes, spacing and timings from here so the look stays consistent and is easy to re-skin.
    /// </summary>
    public static class UIStyle
    {
        // ---------- palette ----------
        public static readonly Color SkyBlue = Hex("#7EC8F2");
        // Clean premium surfaces on the #101827 background (names kept for API stability: "Ivory" = panel surface).
        public static readonly Color Background = Hex("#101827");
        public static readonly Color Ivory = Hex("#182235");
        public static readonly Color IvoryDark = Hex("#2B3852");
        /// <summary>Card surface inside panels.</summary>
        public static readonly Color Card = Hex("#1F2B42");
        public static readonly Color Mint = Hex("#4FD1A5");
        public static readonly Color MintDark = Hex("#2FA67F");
        public static readonly Color Coral = Hex("#FF7A6B");
        public static readonly Color Violet = Hex("#8C6CF0");
        public static readonly Color Sunshine = Hex("#FFC93C");
        public static readonly Color SunshineDark = Hex("#E59E1A");
        public static readonly Color Navy = Hex("#1D2A55");
        public static readonly Color Ink = Hex("#F2F5FA");
        public static readonly Color InkSoft = Hex("#A3AEC4");
        /// <summary>Text color for the optional light gameplay background (accessibility setting).</summary>
        public static readonly Color InkOnLight = Hex("#2E2440");
        public static readonly Color Brown = Hex("#8A4B22");
        public static readonly Color White = Color.white;
        public static readonly Color Shadow = new Color(0.08f, 0.06f, 0.2f, 0.28f);
        public static readonly Color Scrim = new Color(0.05f, 0.06f, 0.16f, 0.62f);
        public static readonly Color Disabled = Hex("#4A4566");
        public static readonly Color Danger = Hex("#E5484D");
        /// <summary>Accent of the active visual theme (set by ThemeService); used for highlights and glows.</summary>
        public static Color ThemeAccent = Hex("#3995F6");

        // ---------- button variants ----------
        public enum ButtonKind { Primary, Secondary, Accent, Danger, Ghost, Gold }

        public static Color ButtonFill(ButtonKind k)
        {
            switch (k)
            {
                case ButtonKind.Primary: return Hex("#3CCB6B");
                case ButtonKind.Secondary: return Ivory;
                case ButtonKind.Accent: return Violet;
                case ButtonKind.Danger: return Danger;
                case ButtonKind.Gold: return Sunshine;
                default: return new Color(1, 1, 1, 0.18f);
            }
        }

        public static Color ButtonText(ButtonKind k) =>
            k == ButtonKind.Secondary ? Ink : (k == ButtonKind.Gold ? Brown : White);

        public static Color ButtonEdge(ButtonKind k)
        {
            switch (k)
            {
                case ButtonKind.Primary: return Hex("#24A050");
                case ButtonKind.Secondary: return IvoryDark;
                case ButtonKind.Accent: return Hex("#6A4CD0");
                case ButtonKind.Danger: return Hex("#B8323A");
                case ButtonKind.Gold: return SunshineDark;
                default: return new Color(1, 1, 1, 0.1f);
            }
        }

        // ---------- typography (px at reference resolution) ----------
        public const int FontDisplay = 132;
        public const int FontTitle = 72;
        public const int FontHeading = 56;
        public const int FontBody = 42;
        public const int FontLabel = 36;
        public const int FontCaption = 30;

        // ---------- spacing / shape ----------
        public const float Space1 = 12f;
        public const float Space2 = 24f;
        public const float Space3 = 36f;
        public const float Space4 = 56f;
        public const float RadiusSmall = 24f;
        public const float RadiusMedium = 40f;
        public const float RadiusLarge = 64f;
        /// <summary>Minimum touch target (~48dp on a 420dpi phone at reference scale).</summary>
        public const float MinTouch = 132f;
        public const float ButtonHeight = 150f;
        public const float TopBarHeight = 170f;

        // ---------- motion (seconds; scaled by settings, collapsed in reduced-motion) ----------
        public const float DurFast = 0.15f;
        public const float DurNormal = 0.25f;
        public const float DurSlow = 0.4f;

        public static Color Hex(string hex)
        {
            Color c;
            return TryParseHex(hex, out c) ? c : Color.magenta;
        }

        /// <summary>Pure-managed "#RRGGBB" / "#RRGGBBAA" parser (safe during static init, no engine calls).</summary>
        public static bool TryParseHex(string hex, out Color color)
        {
            color = Color.magenta;
            if (string.IsNullOrEmpty(hex)) return false;
            string h = hex[0] == '#' ? hex.Substring(1) : hex;
            if (h.Length != 6 && h.Length != 8) return false;
            uint v;
            if (!uint.TryParse(h, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out v)) return false;
            if (h.Length == 6) v = (v << 8) | 0xFF;
            color = new Color(((v >> 24) & 0xFF) / 255f, ((v >> 16) & 0xFF) / 255f, ((v >> 8) & 0xFF) / 255f, (v & 0xFF) / 255f);
            return true;
        }

        public static Color WithAlpha(Color c, float a) { c.a = a; return c; }
        public static Color Lighten(Color c, float t) => Color.Lerp(c, Color.white, t);
        public static Color Darken(Color c, float t) => Color.Lerp(c, Color.black, t);
    }
}
