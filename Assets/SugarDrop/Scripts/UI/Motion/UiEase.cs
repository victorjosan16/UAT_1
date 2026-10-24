using UnityEngine;

namespace SugarDrop
{
    /// <summary>Curbe de animație folosite de interfață (fără alocări).</summary>
    public static class UiEase
    {
        public static float OutQuad(float t) => 1f - (1f - t) * (1f - t);
        public static float OutCubic(float t) => 1f - (1f - t) * (1f - t) * (1f - t);
        public static float InOutCubic(float t) => t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) * 0.5f;

        /// <summary>Depășește puțin ținta și revine („boing”).</summary>
        public static float OutBack(float t, float overshoot = 1.70158f)
        {
            t -= 1f;
            return t * t * ((overshoot + 1f) * t + overshoot) + 1f;
        }

        /// <summary>Arc elastic amortizat (pentru săritura de la final).</summary>
        public static float OutElastic(float t)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * (2f * Mathf.PI / 3f)) + 1f;
        }
    }
}
