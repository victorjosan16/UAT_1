using UnityEngine;

namespace SugarDrop
{
    /// <summary>
    /// Tema unui capitol (10 niveluri): numele ținutului și culorile cerului, ale tablei și ale bastoanelor de bomboane.
    /// Temele se schimbă la fiecare capitol și se reiau după ultima, ca jocul să nu arate mereu la fel.
    /// </summary>
    public readonly struct ChapterTheme
    {
        /// <summary>Numele ținutului (în română; cheie pentru SugarLoc).</summary>
        public readonly string Name;
        public readonly Color SkyTop;
        public readonly Color SkyBottom;
        public readonly Color BoardBack;
        public readonly Color CellA;
        public readonly Color CellB;
        /// <summary>Dungile bastoanelor, gulerul borcanului și panglicile.</summary>
        public readonly Color Stripe;
        /// <summary>Nuanța (0–1) în jurul căreia plutesc luminile din fundal.</summary>
        public readonly float GlowHue;

        /// <param name="skyBottom">Culoarea de jos a cerului (mai intensă); skyTop: cea de sus (mai deschisă).</param>
        private ChapterTheme(string name, string skyBottom, string skyTop, string boardBack, string cellA, string cellB, string stripe, float glowHue)
        {
            Name = name;
            SkyBottom = Hex(skyBottom);
            SkyTop = Hex(skyTop);
            BoardBack = Hex(boardBack);
            CellA = Hex(cellA, 0.5f);
            CellB = Hex(cellB, 0.5f);
            Stripe = Hex(stripe);
            GlowHue = glowHue;
        }

        private static readonly ChapterTheme[] Themes =
        {
            new("Câmpia de Căpșuni", "#D6C2FF", "#FFDBE6", "#F6C5D8", "#66307A", "#783D8C", "#FF8FB1", 0.92f),
            new("Pădurea de Mentă", "#B9F0D5", "#EAFBE0", "#C5EBD2", "#2E6B4F", "#38805E", "#4CCB8A", 0.38f),
            new("Lacul de Afine", "#AFC8FF", "#DDF1FF", "#C3D6F6", "#2F4C8A", "#3A5CA0", "#5A9BFF", 0.6f),
            new("Deșertul de Lămâie", "#FFE29A", "#FFF6D6", "#F6E3B0", "#8A6420", "#9C7428", "#FFC23C", 0.13f),
            new("Castelul de Struguri", "#C9A7FF", "#F0DEFF", "#DCC8F2", "#4E2D85", "#5E3999", "#B57BFF", 0.75f),
            new("Muntele de Ciocolată", "#E2C1A8", "#FBEADC", "#E8CDB8", "#5C3420", "#6E4028", "#B97A50", 0.07f),
            new("Regatul de Gheață", "#B7ECF7", "#F1FCFF", "#CDEFF7", "#2D6E82", "#358096", "#5CD2F0", 0.53f),
            new("Canionul de Caramel", "#FFC79A", "#FFEBD9", "#F5D2B5", "#874322", "#9A5129", "#FF9550", 0.06f),
            new("Norii de Vată de Zahăr", "#F5C6F0", "#E4E9FF", "#EED2F2", "#6A3A88", "#7A4799", "#F78FD8", 0.85f),
            new("Galaxia Curcubeu", "#8F7BE8", "#F7C6E8", "#CFC4F2", "#36296E", "#433382", "#FFD45A", 0.7f),
        };

        public static int Count => Themes.Length;

        /// <summary>Capitolul unui nivel (0 = nivelurile 1–10).</summary>
        public static int ChapterOf(int level) => (Mathf.Max(1, level) - 1) / LevelConfig.ChapterSize;

        public static ChapterTheme For(int chapter) => Themes[((chapter % Themes.Length) + Themes.Length) % Themes.Length];

        public static ChapterTheme ForLevel(int level) => For(ChapterOf(level));

        /// <summary>Indexul temei (pentru sprite-urile generate o singură dată pe temă).</summary>
        public static int IndexOf(int chapter) => ((chapter % Themes.Length) + Themes.Length) % Themes.Length;

        private static Color Hex(string hex, float alpha = 1f)
        {
            Color c = ColorUtility.TryParseHtmlString(hex, out Color parsed) ? parsed : Color.magenta;
            c.a = alpha;
            return c;
        }
    }
}
