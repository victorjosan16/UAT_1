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
            // Culori vii: cer saturat (jos la orizont mai cald, sus mai adânc), tablă închisă la culoare pe care
            // bomboanele colorate ies în evidență, dungi intense.
            new("Câmpia de Căpșuni", "#FFC2DA", "#4FB3FF", "#7A1F4F", "#A8326E", "#B83E7C", "#FF3D7F", 0.92f),
            new("Pădurea de Mentă", "#C6F7A0", "#2FC4E8", "#1F5E3A", "#2E8A55", "#36996A", "#22C55E", 0.38f),
            new("Lacul de Afine", "#9AE6FF", "#2F6BFF", "#1B2F78", "#2E4FB0", "#3A5DC2", "#3D8BFF", 0.6f),
            new("Deșertul de Lămâie", "#FFE65C", "#FF9E3D", "#7A4A10", "#A8701E", "#B88028", "#FFB400", 0.13f),
            new("Castelul de Struguri", "#F0A8FF", "#5B2BCF", "#3A1670", "#5A2BA0", "#6835B0", "#A855F7", 0.75f),
            new("Muntele de Ciocolată", "#FFD1A3", "#E8743B", "#4A2414", "#6E3A22", "#7C452A", "#D9772F", 0.07f),
            new("Regatul de Gheață", "#D2F8FF", "#2AA8F0", "#154A66", "#2479A0", "#2C88B0", "#38D1F5", 0.53f),
            new("Canionul de Caramel", "#FFC98A", "#FF6A2B", "#6A2A0E", "#93441A", "#A35022", "#FF7A1A", 0.06f),
            new("Norii de Vată de Zahăr", "#B3D0FF", "#FF7AD9", "#5A2178", "#7E35A0", "#8C3FB0", "#FF5FCB", 0.85f),
            new("Galaxia Curcubeu", "#9A5CFF", "#1A1150", "#1E1550", "#3A2A8A", "#45339A", "#FFD23C", 0.7f),
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
