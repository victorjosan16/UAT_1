using UnityEngine;

namespace SugarDrop
{
    /// <summary>
    /// Sistemul de culori „Drop Quest”: aventură în natură. Un singur loc pentru tokenii folosiți de temă, paletă,
    /// ținuturi și ecrane: panouri crem, lemn pentru panglici și butoane secundare, verde pentru acțiunea principală,
    /// albastru și auriu ca accente, roșu pentru „închide”, text maro închis. Tabla e ardezie închisă, ca piesele
    /// colorate să iasă în evidență.
    /// </summary>
    public static class QuestStyle
    {
        public static readonly Color Background = Hex("#EAF6FF");
        /// <summary>Crem: ferestre, cardul obiectivelor, plăcuțe.</summary>
        public static readonly Color Surface = Hex("#FFF6E3");
        public static readonly Color SurfaceTint = Hex("#F6E7C8");
        /// <summary>Lemn: panglici, butoane secundare (text alb).</summary>
        public static readonly Color Wood = Hex("#A0612F");
        public static readonly Color WoodDeep = Hex("#7A4520");
        public static readonly Color Green = Hex("#4CB82E");
        public static readonly Color GreenDeep = Hex("#3C9E24");
        public static readonly Color Blue = Hex("#2E8BEA");
        public static readonly Color Gold = Hex("#FFC233");
        public static readonly Color GoldDeep = Hex("#F0A51C");
        public static readonly Color Red = Hex("#E8483C");
        /// <summary>Tabla și celulele ei.</summary>
        public static readonly Color Board = Hex("#3A4658");
        public static readonly Color CellA = Hex("#4C5A6E");
        public static readonly Color CellB = Hex("#556479");
        public static readonly Color Text = Hex("#4A2E17");
        public static readonly Color TextSoft = Hex("#8A6A4E");
        /// <summary>Umbra moale (maro, nu negru) a panourilor și a pieselor.</summary>
        public static readonly Color Shadow = new(0.29f, 0.18f, 0.09f, 0.22f);
        /// <summary>Fundalul de sub ferestre.</summary>
        public static readonly Color Scrim = new(0.12f, 0.16f, 0.22f, 0.5f);

        private static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out Color c) ? c : Color.magenta;
    }
}
