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
            // Soft Pop: cer pastel, tablă albă cu celule lavandă discrete, câte o culoare de accent pe ținut.
            new("Câmpia de Căpșuni", "#FFE3E6", "#CFE8FF", "#FFFFFF", "#F3EEFC", "#ECE5FA", "#FF8D9E", 0.95f),
            new("Pădurea de Mentă", "#DDF7EC", "#CDEFFF", "#FFFFFF", "#EEF8F3", "#E4F3EC", "#66D7B5", 0.42f),
            new("Lacul de Afine", "#D6EEFF", "#E9E3FF", "#FFFFFF", "#EDF3FD", "#E3ECFA", "#5FB0F5", 0.58f),
            new("Deșertul de Lămâie", "#FFF1CC", "#FFE0D2", "#FFFFFF", "#FCF6E8", "#F8EFDB", "#FFD36B", 0.12f),
            new("Castelul de Struguri", "#E9E0FF", "#F9E4F4", "#FFFFFF", "#F1ECFC", "#E8E0F8", "#A58AE0", 0.75f),
            new("Muntele de Ciocolată", "#F4E6DA", "#FFE9DE", "#FFFFFF", "#F7F0EA", "#F0E6DD", "#C99A7A", 0.07f),
            new("Regatul de Gheață", "#E0F8FC", "#E6EEFF", "#FFFFFF", "#ECF7FA", "#E1F1F6", "#7FD6EA", 0.52f),
            new("Canionul de Caramel", "#FFE6D2", "#FFF3D6", "#FFFFFF", "#FBF1E8", "#F6E7DA", "#F7A86A", 0.07f),
            new("Norii de Vată de Zahăr", "#F2E4FF", "#DDEBFF", "#FFFFFF", "#F2ECFC", "#EAE3FA", "#E89BE0", 0.82f),
            new("Galaxia Curcubeu", "#7E6FD8", "#2E2A6B", "#FFFFFF", "#ECE8FB", "#E2DCF7", "#FFD36B", 0.7f),
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
