using UnityEngine;

namespace SugarDrop
{
    /// <summary>Culorile jocului: capacele colorate de pe tablă și bomboanele din grămadă.</summary>
    public static class Palette
    {
        private static readonly Color[] TileColors =
        {
            Hex("#FF6B80"), // inimă-gemă coral
            Hex("#409EFF"), // picătură de apă
            Hex("#38CC94"), // cristal mentă
            Hex("#FFC440"), // gemă aurie
            Hex("#9975EB"), // prismă violet
        };

        private static readonly Color[] CandyColors =
        {
            Hex("#FF7BA9"), Hex("#FFD166"), Hex("#7BDFF2"), Hex("#B8F2A0"), Hex("#C9A7FF"), Hex("#FF9F6E"),
        };

        public static int TileColorCount => TileColors.Length;
        public static Color Tile(int index) => TileColors[Mathf.Clamp(index, 0, TileColors.Length - 1)];
        public static Color TileLight(int index) => Color.Lerp(Tile(index), Color.white, 0.55f);
        public static Color Candy(int index) => CandyColors[((index % CandyColors.Length) + CandyColors.Length) % CandyColors.Length];
        public static Color RandomCandy() => CandyColors[Random.Range(0, CandyColors.Length)];

        public static readonly Color Background = SoftPop.Background;
        public static readonly Color BoardBack = Hex("#F6C5D8");
        public static readonly Color Wall = Hex("#FFFFFF");
        public static readonly Color WallStripe = SoftPop.Lavender;
        public static readonly Color Glass = new(1f, 1f, 1f, 0.35f);

        private static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out Color c) ? c : Color.magenta;
    }
}
