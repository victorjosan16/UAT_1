using UnityEngine;

namespace SugarDrop
{
    /// <summary>Cum se mișcă atmosfera unui ținut în fundal.</summary>
    public enum AmbientMotion { Rise, Fall, Flutter, Drift, Twinkle }

    /// <summary>
    /// Stilul fiecărui ținut în timpul nivelului: ce plutește în fundal (petale, frunze, bule, nisip, gemuri, picături
    /// de ciocolată, fulgi, caramel, nori, stele) și cum se mișcă, plus ornamentele tablei și culoarea exploziilor.
    /// </summary>
    public static class LandStyle
    {
        public static AmbientMotion Motion(int theme) => theme switch
        {
            0 => AmbientMotion.Flutter,
            1 => AmbientMotion.Flutter,
            2 => AmbientMotion.Rise,
            3 => AmbientMotion.Drift,
            4 => AmbientMotion.Twinkle,
            5 => AmbientMotion.Fall,
            6 => AmbientMotion.Fall,
            7 => AmbientMotion.Rise,
            8 => AmbientMotion.Drift,
            _ => AmbientMotion.Twinkle,
        };

        /// <summary>Particula de atmosferă (alternează două forme pe ținut).</summary>
        public static Sprite Ambient(int theme, int index)
        {
            bool alt = index % 3 == 2;
            return theme switch
            {
                0 => alt ? SugarSprites.SoftDot : SugarSprites.Heart,
                1 => SugarSprites.Leaf,
                2 => alt ? SugarSprites.Circle : SugarSprites.Ring,
                3 => alt ? SugarSprites.Crumb : SugarSprites.Sparkle,
                4 => alt ? SugarSprites.Sparkle : SugarSprites.Gem,
                5 => alt ? SugarSprites.Crumb : SugarSprites.Drop,
                6 => SugarSprites.Sparkle,
                7 => alt ? SugarSprites.Circle : SugarSprites.Drop,
                8 => SugarSprites.Frosting,
                _ => alt ? SugarSprites.Sparkle : SugarSprites.Star,
            };
        }

        public static Color AmbientColor(int theme, int index)
        {
            float a = 0.35f + (index % 4) * 0.08f;
            Color c = theme switch
            {
                0 => index % 2 == 0 ? new Color(1f, 0.45f, 0.6f) : new Color(1f, 0.75f, 0.82f),
                1 => index % 2 == 0 ? new Color(0.35f, 0.75f, 0.45f) : new Color(0.6f, 0.9f, 0.6f),
                2 => index % 2 == 0 ? new Color(0.85f, 0.95f, 1f) : new Color(0.55f, 0.75f, 1f),
                3 => index % 2 == 0 ? new Color(1f, 0.9f, 0.45f) : new Color(1f, 0.8f, 0.55f),
                4 => index % 2 == 0 ? new Color(0.7f, 0.5f, 1f) : new Color(0.9f, 0.75f, 1f),
                5 => index % 2 == 0 ? new Color(0.45f, 0.27f, 0.17f) : new Color(0.6f, 0.38f, 0.25f),
                6 => Color.white,
                7 => index % 2 == 0 ? new Color(1f, 0.6f, 0.3f) : new Color(1f, 0.78f, 0.45f),
                8 => index % 2 == 0 ? new Color(1f, 0.85f, 0.95f) : new Color(0.88f, 0.88f, 1f),
                _ => index % 2 == 0 ? new Color(1f, 0.95f, 0.6f) : Color.white,
            };
            c.a = theme == 8 ? a + 0.25f : a + 0.15f;
            return c;
        }

        /// <summary>Mărimea particulelor (norii de vată sunt mari, fulgii și nisipul mici).</summary>
        public static Vector2 AmbientSize(int theme) => theme switch
        {
            8 => new Vector2(2.2f, 3.6f),
            3 or 6 => new Vector2(0.25f, 0.6f),
            _ => new Vector2(0.4f, 1.1f),
        };

        /// <summary>Ornamentul de la colțurile tablei.</summary>
        public static Sprite Ornament(int theme) => theme switch
        {
            0 => SugarSprites.Heart,
            1 => SugarSprites.Leaf,
            2 => SugarSprites.Drop,
            3 => SugarSprites.Star,
            4 => SugarSprites.Gem,
            5 => SugarSprites.Chocolate,
            6 => SugarSprites.Ice,
            7 => SugarSprites.CandySwirl,
            8 => SugarSprites.Frosting,
            _ => SugarSprites.Star,
        };

        public static Color OrnamentColor(int theme) => theme switch
        {
            0 => new Color(0.95f, 0.3f, 0.42f),
            1 => new Color(0.35f, 0.78f, 0.45f),
            2 => new Color(0.4f, 0.6f, 1f),
            3 => new Color(1f, 0.82f, 0.25f),
            4 => Color.white,
            5 => Color.white,
            6 => Color.white,
            7 => new Color(1f, 0.6f, 0.3f),
            8 => new Color(1f, 0.8f, 0.93f),
            _ => new Color(1f, 0.92f, 0.5f),
        };
    }
}
