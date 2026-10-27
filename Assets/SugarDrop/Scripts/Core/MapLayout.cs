using UnityEngine;

namespace SugarDrop
{
    /// <summary>
    /// Geometria hărții de niveluri (pixeli de referință, 1080 lățime): pozițiile nodurilor pe un drum șerpuit,
    /// înălțimea conținutului, curba dintre noduri și poziția de derulare care centrează un nivel.
    /// Pură (fără obiecte Unity), ca să poată fi testată.
    /// </summary>
    public static class MapLayout
    {
        public const float BottomPadding = 560f; // deasupra butonului „Joacă” și a barei de jos
        public const float TopPadding = 460f;
        public const float Spacing = 235f;
        public const float Amplitude = 250f;

        /// <summary>Centrul nodului pentru nivelul dat (1 = primul), relativ la mijlocul de jos al conținutului.</summary>
        public static Vector2 NodePosition(int level)
        {
            int i = Mathf.Max(0, level - 1);
            float x = Mathf.Sin(i * 0.85f) * Amplitude + Mathf.Sin(i * 0.37f + 1f) * 55f;
            return new Vector2(x, BottomPadding + i * Spacing);
        }

        public static float ContentHeight(int levels) => BottomPadding + Mathf.Max(0, levels - 1) * Spacing + TopPadding;

        /// <summary>
        /// Poziția normalizată de derulare (0 = jos, 1 = sus) care pune nivelul la „viewportFraction” din înălțimea ecranului.
        /// </summary>
        public static float FocusNormalized(int level, int levels, float viewportHeight, float viewportFraction = 0.42f)
        {
            float scrollable = ContentHeight(levels) - viewportHeight;
            if (scrollable <= 0f) return 0f;
            float offset = NodePosition(level).y - viewportHeight * viewportFraction;
            return Mathf.Clamp01(offset / scrollable);
        }

        /// <summary>Punct pe curba netedă (Catmull-Rom) dintre nivelurile „level” și „level + 1”, t în 0..1.</summary>
        public static Vector2 PathPoint(int level, float t)
        {
            Vector2 p0 = NodePosition(level - 1 < 1 ? 1 : level - 1);
            Vector2 p1 = NodePosition(level);
            Vector2 p2 = NodePosition(level + 1);
            Vector2 p3 = NodePosition(level + 2);
            if (level <= 1) p0 = p1 - (p2 - p1);
            float t2 = t * t, t3 = t2 * t;
            return 0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }

        /// <summary>Câte niveluri arată harta: cel puțin 30 și mereu 15 peste nivelul curent, rotunjit la 10 (ca să apară Boss-ul).</summary>
        public static int VisibleLevels(int currentLevel)
        {
            int needed = Mathf.Max(30, currentLevel + 15);
            return (needed + 9) / 10 * 10;
        }
    }
}
