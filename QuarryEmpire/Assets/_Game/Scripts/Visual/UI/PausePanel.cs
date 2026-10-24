using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace QuarryEmpire
{
    /// <summary>
    /// Ecran de pauză: oprește timpul (Time.timeScale = 0) cât e deschis și oferă comutarea
    /// profilului grafic (60 FPS / 30 FPS economic). Nu modifică nimic din gameplay.
    /// </summary>
    public class PausePanel : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private RectTransform card;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button qualityButton;
        [SerializeField] private TMP_Text qualityLabel;
        [SerializeField, Min(0.01f)] private float fadeDuration = 0.18f;

        public bool IsOpen { get; private set; }

        private float _previousTimeScale = 1f;
        private float _fade;

        private void Awake()
        {
            if (resumeButton != null) resumeButton.onClick.AddListener(Close);
            if (qualityButton != null) qualityButton.onClick.AddListener(CycleQuality);
            SetVisible(0f);
            // Open() activează obiectul (și deci rulează Awake) — atunci nu îl închidem la loc.
            if (!IsOpen) gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            // Siguranță: dacă panoul e dezactivat din alt loc, nu lăsăm jocul înghețat.
            if (IsOpen) Close();
        }

        public void Open()
        {
            if (IsOpen) return;
            IsOpen = true;
            _previousTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
            AudioListener.pause = true;
            gameObject.SetActive(true);
            RefreshQualityLabel();
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            Time.timeScale = _previousTimeScale;
            AudioListener.pause = false;
        }

        private void CycleQuality()
        {
            if (PerformanceManager.Instance != null) PerformanceManager.Instance.CycleProfile();
            RefreshQualityLabel();
        }

        private void RefreshQualityLabel()
        {
            if (qualityLabel == null) return;
            PerformanceManager manager = PerformanceManager.Instance;
            qualityLabel.text = manager != null && manager.CurrentProfile != null
                ? "Grafică: " + manager.CurrentProfile.DisplayName
                : "Grafică: implicită";
        }

        private void Update()
        {
            float target = IsOpen ? 1f : 0f;
            _fade = Mathf.MoveTowards(_fade, target, Time.unscaledDeltaTime / fadeDuration);
            SetVisible(_fade);
            if (!IsOpen && _fade <= 0f) gameObject.SetActive(false);
        }

        private void SetVisible(float value)
        {
            if (group != null)
            {
                group.alpha = value;
                group.interactable = value > 0.99f;
                group.blocksRaycasts = IsOpen;
            }
            if (card != null)
            {
                // Mic „pop”: cardul crește din 0.9 la 1.
                float s = Mathf.Lerp(0.9f, 1f, 1f - (1f - value) * (1f - value));
                card.localScale = Vector3.one * s;
            }
        }
    }
}
