using UnityEngine;

namespace SugarDrop
{
    /// <summary>
    /// Sistemul de culori „Sugar Drop — Soft Pop Edition”: un singur loc pentru tokenii folosiți de temă, paletă,
    /// ținuturi și ecrane. Fundal deschis, lavandă ca primară, coral și mentă ca secundare, galben pentru accente,
    /// text prună închis. Variantele „Deep” sunt pentru butoane cu text alb (contrast mai bun).
    /// </summary>
    public static class SoftPop
    {
        public static readonly Color Background = Hex("#F8F7FF");
        public static readonly Color Surface = Hex("#FFFFFF");
        /// <summary>Suprafață ușor colorată (tăvi, plăcuțe inactive).</summary>
        public static readonly Color SurfaceTint = Hex("#F1EDFC");
        public static readonly Color Lavender = Hex("#A58AE0");
        public static readonly Color LavenderDeep = Hex("#8B6FD3");
        public static readonly Color Coral = Hex("#FF8D9E");
        public static readonly Color CoralDeep = Hex("#F06A80");
        public static readonly Color Mint = Hex("#66D7B5");
        public static readonly Color MintDeep = Hex("#35BE94");
        public static readonly Color Yellow = Hex("#FFD36B");
        public static readonly Color YellowDeep = Hex("#F5B93A");
        public static readonly Color Sky = Hex("#5FB0F5");
        public static readonly Color Text = Hex("#403653");
        /// <summary>Text secundar (indicii, versiunea).</summary>
        public static readonly Color TextSoft = Hex("#7C7291");
        /// <summary>Umbra moale a panourilor și a tablei.</summary>
        public static readonly Color Shadow = new(0.25f, 0.21f, 0.33f, 0.16f);
        /// <summary>Fundalul întunecat de sub ferestre (mai blând decât negrul).</summary>
        public static readonly Color Scrim = new(0.25f, 0.21f, 0.33f, 0.45f);

        private static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out Color c) ? c : Color.magenta;
    }
}
