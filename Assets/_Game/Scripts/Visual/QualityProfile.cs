using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace QuarryEmpire
{
    /// <summary>
    /// Profiluri de calitate pentru mobil. La prima pornire alege automat după telefon (memorie, nuclee);
    /// jucătorul îl poate schimba din Pauză → GRAFICĂ. Setarea se ține în PlayerPrefs (nu atinge salvarea jocului).
    ///   ÎNALTĂ:      60 FPS, umbre până la 30 m, MSAA 2x, rezoluție 100%
    ///   ECHILIBRATĂ: 60 FPS, umbre până la 18 m, fără MSAA, rezoluție 90%
    ///   ECONOMIC:    30 FPS, fără umbre, rezoluție 80% (baterie, telefoane slabe)
    /// </summary>
    [DefaultExecutionOrder(-350)]
    public class QualityProfile : MonoBehaviour
    {
        public enum Level { Economic = 0, Balanced = 1, High = 2 }

        private const string PrefsKey = "qe_quality_level";

        public static QualityProfile Instance { get; private set; }

        public Level Current { get; private set; }

        public int TargetFrameRate => Current == Level.Economic ? 30 : 60;

        public string LevelName => Current switch
        {
            Level.High => "ÎNALTĂ",
            Level.Balanced => "ECHILIBRATĂ",
            _ => "ECONOMIC (30 FPS)",
        };

        private void Awake()
        {
            Instance = this;
            Current = PlayerPrefs.HasKey(PrefsKey) ? (Level)Mathf.Clamp(PlayerPrefs.GetInt(PrefsKey), 0, 2) : DetectDefault();
            Apply();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Trece la nivelul următor: Înaltă → Echilibrată → Economic → Înaltă.</summary>
        public void CycleLevel()
        {
            Current = Current == Level.Economic ? Level.High : Current - 1;
            Save();
            Apply();
        }

        /// <summary>Folosit de PerformanceGovernor când FPS-ul e prea mic. Nu coboară automat sub Echilibrată.</summary>
        public bool TryStepDownAutomatically()
        {
            if (Current != Level.High) return false;
            Current = Level.Balanced;
            Save();
            Apply();
            return true;
        }

        private void Save()
        {
            PlayerPrefs.SetInt(PrefsKey, (int)Current);
            PlayerPrefs.Save();
        }

        private static Level DetectDefault()
        {
            if (!Application.isMobilePlatform) return Level.High;
            int memory = SystemInfo.systemMemorySize; // MB
            int cores = SystemInfo.processorCount;
            if (memory >= 6000 && cores >= 8) return Level.High;
            if (memory >= 3000) return Level.Balanced;
            return Level.Economic;
        }

        private void Apply()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = TargetFrameRate;

            float shadowDistance = Current switch { Level.High => 30f, Level.Balanced => 18f, _ => 0f };
            if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp)
            {
                urp.renderScale = Current switch { Level.High => 1f, Level.Balanced => 0.9f, _ => 0.8f };
                urp.msaaSampleCount = Current == Level.High ? 2 : 1;
                urp.shadowDistance = Mathf.Max(1f, shadowDistance);
            }

            Light sun = RenderSettings.sun;
            if (sun != null) sun.shadows = shadowDistance > 0f ? LightShadows.Soft : LightShadows.None;
        }
    }
}
