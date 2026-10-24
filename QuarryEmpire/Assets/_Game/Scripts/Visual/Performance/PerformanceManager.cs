using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace QuarryEmpire
{
    /// <summary>
    /// Alege și aplică profilul grafic: 60 FPS pe telefoane mid-range, 30 FPS „economic” pe cele slabe.
    /// - La prima pornire alege după memorie / nuclee; apoi păstrează alegerea jucătorului (PlayerPrefs).
    /// - Opțional coboară singur la profilul economic dacă FPS-ul real rămâne sub prag.
    /// Rulează după GameManager (care setează 60 FPS) și suprascrie FPS-ul țintă cu cel al profilului.
    /// </summary>
    [DefaultExecutionOrder(-150)]
    public class PerformanceManager : MonoBehaviour
    {
        private const string PrefsKey = "QE.PerformanceProfile";

        public static PerformanceManager Instance { get; private set; }

        [Tooltip("Ordinea contează: 0 = cel mai bun, ultimul = cel mai economic.")]
        [SerializeField] private PerformanceProfile[] profiles = new PerformanceProfile[0];

        [Header("Alegere automată (prima pornire)")]
        [SerializeField] private int minMemoryMbForHigh = 3500;
        [SerializeField] private int minCoresForHigh = 6;

        [Header("Coborâre automată")]
        [SerializeField] private bool autoDowngrade = true;
        [Tooltip("Sub acest FPS mediu (pe fereastra de mai jos) se trece la profilul următor.")]
        [SerializeField] private float downgradeFps = 45f;
        [SerializeField, Min(1f)] private float sampleWindow = 5f;
        [SerializeField, Min(0f)] private float warmup = 4f;

        public PerformanceProfile CurrentProfile => _index >= 0 && _index < profiles.Length ? profiles[_index] : null;

        private int _index = -1;
        private RenderPipelineAsset _originalQualityPipeline;
        private bool _changedPipeline;
        private float _elapsed;
        private float _sampleTime;
        private int _sampleFrames;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            _originalQualityPipeline = QualitySettings.renderPipeline;

            int saved = PlayerPrefs.GetInt(PrefsKey, -1);
            Apply(saved >= 0 && saved < profiles.Length ? saved : ChooseDefault(), false);
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            Instance = null;
            // În Editor, schimbările din Play Mode la QualitySettings ar rămâne în proiect: le anulăm.
            if (Application.isEditor && _changedPipeline) QualitySettings.renderPipeline = _originalQualityPipeline;
        }

        private int ChooseDefault()
        {
            if (profiles.Length == 0) return -1;
            bool strong = SystemInfo.systemMemorySize >= minMemoryMbForHigh && SystemInfo.processorCount >= minCoresForHigh;
            return strong ? 0 : profiles.Length - 1;
        }

        /// <summary>Trece la următorul profil (folosit de butonul din meniul de pauză) și îl salvează.</summary>
        public void CycleProfile()
        {
            if (profiles.Length == 0) return;
            Apply((_index + 1) % profiles.Length, true);
        }

        public void Apply(int index, bool save)
        {
            if (index < 0 || index >= profiles.Length || profiles[index] == null) return;
            _index = index;
            PerformanceProfile profile = profiles[index];

            QualitySettings.vSyncCount = 0; // pe mobil targetFrameRate controlează ritmul
            Application.targetFrameRate = profile.TargetFrameRate;

            if (profile.PipelineAsset != null && QualitySettings.renderPipeline != profile.PipelineAsset)
            {
                QualitySettings.renderPipeline = profile.PipelineAsset;
                _changedPipeline = true;
            }

            if (VfxManager.Instance != null) VfxManager.Instance.Density = profile.ParticleDensity;

            Camera main = Camera.main;
            if (main != null && main.TryGetComponent(out UniversalAdditionalCameraData cameraData))
                cameraData.renderShadows = profile.CameraShadows;

            if (save)
            {
                PlayerPrefs.SetInt(PrefsKey, index);
                PlayerPrefs.Save();
            }
            _sampleTime = 0f;
            _sampleFrames = 0;
        }

        private void Start()
        {
            // VfxManager poate să se fi trezit după noi: reaplicăm densitatea.
            if (VfxManager.Instance != null && CurrentProfile != null) VfxManager.Instance.Density = CurrentProfile.ParticleDensity;
        }

        private void Update()
        {
            if (!autoDowngrade || CurrentProfile == null || _index >= profiles.Length - 1) return;
            if (Time.timeScale <= 0f) return; // în pauză nu măsurăm

            float dt = Time.unscaledDeltaTime;
            _elapsed += dt;
            if (_elapsed < warmup) return;

            _sampleTime += dt;
            _sampleFrames++;
            if (_sampleTime < sampleWindow) return;

            float fps = _sampleFrames / _sampleTime;
            _sampleTime = 0f;
            _sampleFrames = 0;
            // Comparăm cu ținta profilului: un profil de 30 FPS nu e „lent” la 30.
            float threshold = Mathf.Min(downgradeFps, CurrentProfile.TargetFrameRate * 0.75f);
            if (fps < threshold)
            {
                Debug.Log($"[QuarryEmpire] FPS mediu {fps:0} < {threshold:0}: trec la profilul „{profiles[_index + 1].DisplayName}”.");
                Apply(_index + 1, false);
            }
        }
    }
}
