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
        // Paleta „Premium Cartoon Adventure” (vezi docs/redesign/DESIGN_TOKENS.md).
        /// <summary>Turcoaz: ape, cer, panouri de hartă.</summary>
        public static readonly Color Turquoise = Hex("#78C7D5");
        /// <summary>Verde mentă: vegetație, insule, fundaluri blânde.</summary>
        public static readonly Color Mint = Hex("#8CD5B0");
        /// <summary>Petrol închis: tabla de joc, bara de progres, textul de pe fundal deschis.</summary>
        public static readonly Color Petrol = Hex("#34545A");
        public static readonly Color Background = Hex("#EAF6FF");
        /// <summary>Crem: ferestre, cardul obiectivelor, plăcuțe.</summary>
        public static readonly Color Surface = Hex("#FFF5DF");
        public static readonly Color SurfaceTint = Hex("#F6E7C8");
        /// <summary>Lemn: panglici, butoane secundare (text alb).</summary>
        public static readonly Color Wood = Hex("#9D633B");
        public static readonly Color WoodDeep = Hex("#774A2B");
        public static readonly Color Green = Hex("#50BC3A");
        public static readonly Color GreenDeep = Hex("#3FA02B");
        public static readonly Color Blue = Hex("#2E8BEA");
        public static readonly Color Gold = Hex("#F6B62D");
        public static readonly Color GoldDeep = Hex("#E39B14");
        public static readonly Color Red = Hex("#E8483C");
        /// <summary>Tabla și celulele ei.</summary>
        public static readonly Color Board = Hex("#34545A");
        public static readonly Color CellA = Hex("#416268");
        public static readonly Color CellB = Hex("#4A6D74");
        public static readonly Color Text = Hex("#4A2E17");
        public static readonly Color TextSoft = Hex("#8A6A4E");
        /// <summary>Umbra moale (maro, nu negru) a panourilor și a pieselor.</summary>
        public static readonly Color Shadow = new(0.29f, 0.18f, 0.09f, 0.22f);
        /// <summary>Fundalul de sub ferestre.</summary>
        public static readonly Color Scrim = new(0.12f, 0.16f, 0.22f, 0.5f);
        /// <summary>Conturul textelor albe (titluri, cifre): maro-închis, ca lemnul, nu mov.</summary>
        public static readonly Color Outline = new(0.24f, 0.15f, 0.08f, 0.85f);
        /// <summary>Fundalul din jurul ferestrelor de poveste (noapte albastră).</summary>
        public static readonly Color Night = new(0.06f, 0.1f, 0.2f, 0.92f);

        /// <summary>Viteza cu care apare textul din baloane (litere pe secundă).</summary>
        public const float LettersPerSecond = 38f;

        private static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out Color c) ? c : Color.magenta;
    }
}
