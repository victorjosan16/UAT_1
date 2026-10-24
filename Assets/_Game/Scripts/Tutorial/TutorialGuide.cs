using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// Tutorialul de început, în 3 pași: sparge blocuri → vinde → cumpără un upgrade.
    /// O săgeată lângă jucător arată direcția, iar un marcaj sare deasupra țintei.
    /// Textul pasului apare în cardul de misiune (OrderHud). Progresul se salvează; după pasul 3 dispare.
    /// </summary>
    public class TutorialGuide : MonoBehaviour
    {
        public const int FinishedStep = 3;

        public static TutorialGuide Instance { get; private set; }

        [Header("Ținte")]
        [SerializeField] private Transform quarryTarget;
        [SerializeField] private Transform sellTarget;
        [SerializeField] private Transform upgradeTarget;

        [Header("Vizual")]
        [Tooltip("Săgeata de pe sol, lângă jucător (arată spre țintă).")]
        [SerializeField] private Transform groundArrow;
        [Tooltip("Marcajul care sare deasupra țintei.")]
        [SerializeField] private Transform targetMarker;
        [SerializeField] private float arrowDistance = 1.4f;
        [SerializeField] private float hideNearDistance = 2.2f;
        [Tooltip("Câte resurse trebuie strânse la pasul 1.")]
        [SerializeField] private int resourcesToCollect = 3;

        public bool IsActive => _step < FinishedStep;

        public string Title => _step switch
        {
            0 => Loc.T("Sparge blocuri în carieră"),
            1 => Loc.T("Vinde resursele"),
            _ => Loc.T("Cumpără un upgrade"),
        };

        public string Detail => _step switch
        {
            0 => Loc.T("Mergi lângă blocuri: minerul lovește singur"),
            1 => Loc.T("Du stiva la placa verde VINDE"),
            _ => Loc.T("Stai pe o placă mov ca să plătești"),
        };

        private int _step;
        private int _collected;

        private void Awake()
        {
            Instance = this;
            SaveData data = SaveSystem.Instance != null ? SaveSystem.Instance.Data : null;
            _step = data != null ? data.tutorialStep : 0;

            // Jucătorii care aveau deja progres înainte de tutorial nu-l mai văd.
            if (data != null && _step == 0 && (data.upgradeLevels.Count > 0 || data.unlocked.Count > 0 || data.layerIndex > 0))
                SetStep(FinishedStep);
            UpdateVisibility();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnEnable()
        {
            GameEvents.ResourceCollected += OnCollected;
            GameEvents.MoneyEarned += OnMoneyEarned;
            GameEvents.Upgraded += OnUpgraded;
        }

        private void OnDisable()
        {
            GameEvents.ResourceCollected -= OnCollected;
            GameEvents.MoneyEarned -= OnMoneyEarned;
            GameEvents.Upgraded -= OnUpgraded;
        }

        private void OnCollected(ResourceData _, Vector3 __)
        {
            if (_step != 0) return;
            _collected++;
            if (_collected >= resourcesToCollect) SetStep(1);
        }

        private void OnMoneyEarned(long _, Vector3 __, bool animate)
        {
            // „animate” e true doar pentru banii jucătorului (nu muncitori / camion).
            if (_step == 1 && animate) SetStep(2);
        }

        private void OnUpgraded(UpgradeDefinition _, int __)
        {
            if (_step <= 2) SetStep(FinishedStep);
        }

        private void SetStep(int step)
        {
            _step = step;
            if (SaveSystem.Instance != null)
            {
                SaveSystem.Instance.Data.tutorialStep = step;
                SaveSystem.Instance.MarkDirty();
            }
            UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            if (groundArrow != null) groundArrow.gameObject.SetActive(IsActive);
            if (targetMarker != null) targetMarker.gameObject.SetActive(IsActive);
        }

        private Transform CurrentTarget => _step switch
        {
            0 => quarryTarget,
            1 => sellTarget,
            _ => upgradeTarget,
        };

        private void LateUpdate()
        {
            if (!IsActive) return;
            PlayerRoot player = PlayerRoot.Instance;
            Transform target = CurrentTarget;
            if (player == null || target == null) return;

            Vector3 toTarget = target.position - player.Position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;
            Vector3 direction = distance > 0.01f ? toTarget / distance : Vector3.forward;

            if (groundArrow != null)
            {
                bool show = distance > hideNearDistance;
                if (groundArrow.gameObject.activeSelf != show) groundArrow.gameObject.SetActive(show);
                // Săgeata pulsează ușor înainte și înapoi pe direcția țintei.
                float pulse = Mathf.Sin(Time.time * 6f) * 0.12f;
                groundArrow.SetPositionAndRotation(player.Position + direction * (arrowDistance + pulse) + Vector3.up * 0.08f,
                    Quaternion.LookRotation(direction, Vector3.up));
            }

            if (targetMarker != null)
                targetMarker.position = target.position + Vector3.up * (2.6f + Mathf.Abs(Mathf.Sin(Time.time * 3f)) * 0.4f);
        }
    }
}
