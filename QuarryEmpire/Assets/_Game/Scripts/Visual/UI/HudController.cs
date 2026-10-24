using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace QuarryEmpire
{
    /// <summary>
    /// HUD-ul principal (doar afișare): nivel, bani, progres, misiune și butonul de pauză.
    /// Nu știe de unde vin valorile — le primește prin metodele Set... (azi de la HudStage1Binder,
    /// mai târziu de la sistemele reale de economie / comenzi).
    /// </summary>
    public class HudController : MonoBehaviour
    {
        [Header("Sus")]
        [SerializeField] private TMP_Text levelLabel;
        [SerializeField] private UiPunchScale levelPunch;
        [SerializeField] private TMP_Text moneyLabel;
        [SerializeField] private UiPunchScale moneyPunch;
        [SerializeField] private UiProgressBar progressBar;
        [SerializeField] private TMP_Text progressLabel;
        [SerializeField] private Button pauseButton;
        [SerializeField] private PausePanel pausePanel;

        [Header("Misiune")]
        [SerializeField] private TMP_Text missionLabel;
        [SerializeField] private TMP_Text missionCountLabel;
        [SerializeField] private UiProgressBar missionBar;
        [SerializeField] private UiPunchScale missionPunch;

        [Header("Animație bani")]
        [Tooltip("Cât durează numărătoarea până la noua valoare (secunde).")]
        [SerializeField, Min(0f)] private float moneyCountDuration = 0.35f;

        private long _moneyShown;
        private long _moneyFrom;
        private long _moneyTarget;
        private float _moneyTimer = -1f;
        private int _level = -1;

        private void Awake()
        {
            if (pauseButton != null && pausePanel != null) pauseButton.onClick.AddListener(pausePanel.Open);
        }

        public void SetLevel(int level)
        {
            if (level == _level) return;
            bool changed = _level >= 0;
            _level = level;
            if (levelLabel != null) levelLabel.text = level.ToString();
            if (changed && levelPunch != null) levelPunch.Punch();
        }

        /// <summary>Banii se „numără” animat până la noua valoare; animate=false o setează direct.</summary>
        public void SetMoney(long amount, bool animate = true)
        {
            if (amount == _moneyTarget && _moneyTimer < 0f) return;
            bool increased = amount > _moneyTarget;
            _moneyTarget = amount;
            if (!animate || moneyCountDuration <= 0f)
            {
                _moneyShown = amount;
                _moneyTimer = -1f;
                RefreshMoney();
                return;
            }
            _moneyFrom = _moneyShown;
            _moneyTimer = 0f;
            if (increased && moneyPunch != null) moneyPunch.Punch();
        }

        public void SetProgress(float value01, string label)
        {
            if (progressBar != null) progressBar.SetValue(value01);
            if (progressLabel != null) progressLabel.text = label;
        }

        public void SetMission(string description, int current, int target)
        {
            if (missionLabel != null) missionLabel.text = description;
            if (missionCountLabel != null) missionCountLabel.text = $"{Mathf.Min(current, target)}/{target}";
            if (missionBar != null) missionBar.SetValue(target > 0 ? (float)current / target : 0f);
        }

        public void PunchMission()
        {
            if (missionPunch != null) missionPunch.Punch();
        }

        private void Update()
        {
            if (_moneyTimer < 0f) return;
            _moneyTimer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(_moneyTimer / moneyCountDuration);
            float eased = 1f - (1f - t) * (1f - t);
            _moneyShown = (long)Mathf.Lerp(_moneyFrom, _moneyTarget, eased);
            if (t >= 1f)
            {
                _moneyShown = _moneyTarget;
                _moneyTimer = -1f;
            }
            RefreshMoney();
        }

        private void RefreshMoney()
        {
            if (moneyLabel != null) moneyLabel.text = FormatMoney(_moneyShown);
        }

        /// <summary>1234 → „1.2K”, 2500000 → „2.5M” (încape în pastila de bani).</summary>
        public static string FormatMoney(long value)
        {
            if (value < 10_000) return value.ToString();
            if (value < 1_000_000) return (value / 1000f).ToString("0.#") + "K";
            if (value < 1_000_000_000) return (value / 1_000_000f).ToString("0.#") + "M";
            return (value / 1_000_000_000f).ToString("0.#") + "B";
        }
    }
}
