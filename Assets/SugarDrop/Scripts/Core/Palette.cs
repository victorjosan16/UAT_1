using UnityEngine;

namespace SugarDrop
{
    /// <summary>Culorile jocului: capacele colorate de pe tablă și bomboanele din grămadă.</summary>
    public static class Palette
    {
        private static readonly Color[] TileColors =
        {
            Hex("#FF3352"), // căpșună
            Hex("#1F8FFF"), // afine
            Hex("#2ECC4F"), // mentă
            Hex("#FFC20E"), // lămâie
            Hex("#9D4DFF"), // strugure
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

        public static readonly Color Background = Hex("#FFE6EE");
        public static readonly Color BoardBack = Hex("#F6C5D8");
        public static readonly Color Wall = Hex("#FFFFFF");
        public static readonly Color WallStripe = Hex("#FF8FB1");
        public static readonly Color Glass = new(1f, 1f, 1f, 0.35f);

        private static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out Color c) ? c : Color.magenta;
    }
}
