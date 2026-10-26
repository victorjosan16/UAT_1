using UnityEngine;

namespace SugarDrop
{
    public enum GraphicsQuality { Low, Medium, High }

    /// <summary>
    /// Setarea de grafică: câte particule pot fi pe ecran, câte lumini plutesc în fundal și câte cadre pe secundă.
    /// La prima pornire se alege după memoria telefonului; se schimbă din Setări.
    /// </summary>
    public static class SugarQuality
    {
        public static GraphicsQuality Current
        {
            get
            {
                int saved = SugarSave.Data.graphics;
                if (saved >= 0 && saved <= (int)GraphicsQuality.High) return (GraphicsQuality)saved;
                return Auto(SystemInfo.systemMemorySize);
            }
        }

        /// <summary>Calitatea potrivită pentru memoria telefonului (MB).</summary>
        public static GraphicsQuality Auto(int memoryMb) =>
            memoryMb <= 0 ? GraphicsQuality.High : memoryMb <= 3072 ? GraphicsQuality.Low : memoryMb <= 4096 ? GraphicsQuality.Medium : GraphicsQuality.High;

        public static int FrameRate => Current == GraphicsQuality.Low ? 30 : 60;

        /// <summary>Câte particule de efect pot fi active deodată.</summary>
        public static int MaxParticles => Current switch
        {
            GraphicsQuality.Low => 140,
            GraphicsQuality.Medium => 280,
            _ => 500,
        };

        /// <summary>Ce parte din luminile de fundal și din sclipirile ambientale se văd (0–1).</summary>
        public static float AmbientScale => Current switch
        {
            GraphicsQuality.Low => 0.3f,
            GraphicsQuality.Medium => 0.65f,
            _ => 1f,
        };

        /// <summary>Textul butonului din Setări (cheie SugarLoc).</summary>
        public static string LabelKey => Current switch
        {
            GraphicsQuality.Low => "GRAFICĂ: SCĂZUTĂ",
            GraphicsQuality.Medium => "GRAFICĂ: MEDIE",
            _ => "GRAFICĂ: ÎNALTĂ",
        };

        /// <summary>Trece la următoarea calitate (Înaltă → Scăzută → Medie → Înaltă) și o aplică.</summary>
        public static void Cycle()
        {
            SugarSave.Data.graphics = Current switch
            {
                GraphicsQuality.High => (int)GraphicsQuality.Low,
                GraphicsQuality.Low => (int)GraphicsQuality.Medium,
                _ => (int)GraphicsQuality.High,
            };
            Apply();
        }

        public static event System.Action Changed;

        public static void Apply()
        {
            Application.targetFrameRate = FrameRate;
            Changed?.Invoke();
        }
    }
}
