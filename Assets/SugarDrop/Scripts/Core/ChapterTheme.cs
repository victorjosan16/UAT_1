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
            CellA = Hex(cellA);
            CellB = Hex(cellB);
            Stripe = Hex(stripe);
            GlowHue = glowHue;
        }

        private static readonly ChapterTheme[] Themes =
        {
            // Drop Quest: cer de natură (sus albastru, jos culoarea ținutului), tablă ardezie închisă, cadru de lemn
            // sau în culoarea ținutului. Imaginile pictate din Resources/Art (sky0–sky9) înlocuiesc degradeul.
            new("Câmpia de Căpșuni", "#BFE79A", "#7CC8F5", "#3A4658", "#4C5A6E", "#556479", "#A0612F", 0.3f),
            new("Pădurea de Mentă", "#8FD39A", "#6CC0E8", "#34484A", "#465C5E", "#4F6668", "#7A5A2E", 0.38f),
            new("Lacul de Afine", "#9AD8F0", "#5FA8E8", "#33435E", "#44567A", "#4C6086", "#3E7BC0", 0.58f),
            new("Deșertul de Lămâie", "#F5D98C", "#8CCBF0", "#4A4436", "#5E5746", "#675F4D", "#C08A3A", 0.12f),
            new("Castelul de Struguri", "#C9B2EE", "#7FA8EC", "#3E3A5C", "#504A72", "#58527C", "#7A4FB0", 0.75f),
            new("Muntele de Ciocolată", "#D9B48C", "#8CC2E6", "#45382F", "#58483D", "#614F43", "#7A4520", 0.07f),
            new("Regatul de Gheață", "#D4F2FA", "#7EC4EE", "#33475A", "#445C72", "#4C667E", "#5AA8C8", 0.52f),
            new("Canionul de Caramel", "#F2B983", "#86C4EC", "#4A3A30", "#5E4A3E", "#685245", "#B8652A", 0.07f),
            new("Norii de Vată de Zahăr", "#F5D2EC", "#9CC8F4", "#423C58", "#544C6E", "#5C5478", "#C060A8", 0.82f),
            new("Galaxia Curcubeu", "#5A4AB0", "#1C1848", "#2A2648", "#3A355E", "#423C68", "#FFC233", 0.7f),
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
