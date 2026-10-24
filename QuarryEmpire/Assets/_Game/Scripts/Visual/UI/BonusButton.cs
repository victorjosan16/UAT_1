using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace QuarryEmpire
{
    /// <summary>
    /// Buton rotund de bonus (ex. x2 bani, magnet) cu cooldown radial.
    /// Logica bonusului NU e aici: leagă-o în Inspector la „On Activated” (sau din cod).
    /// Fără nicio acțiune legată, butonul apare blocat cu eticheta „În curând”.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class BonusButton : MonoBehaviour
    {
        [SerializeField] private Image cooldownOverlay;
        [SerializeField] private GameObject comingSoonBadge;
        [SerializeField] private TMP_Text timerLabel;
        [SerializeField] private CanvasGroup contentGroup;
        [SerializeField, Min(0f)] private float cooldownSeconds = 30f;
        [Tooltip("Pornit = butonul e activ chiar dacă acțiunea se adaugă din cod (AddListener).")]
        [SerializeField] private bool forceAvailable;
        [SerializeField] private UnityEvent onActivated = new();

        public UnityEvent OnActivated => onActivated;

        private Button _button;
        private float _cooldownLeft;
        private bool _available;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(HandleClick);
            SetAvailable(forceAvailable || onActivated.GetPersistentEventCount() > 0);
        }

        /// <summary>Activează/blochează butonul (ex. când sistemul de bonusuri sau reclamele sunt gata).</summary>
        public void SetAvailable(bool available)
        {
            _available = available;
            if (comingSoonBadge != null) comingSoonBadge.SetActive(!available);
            if (contentGroup != null) contentGroup.alpha = available ? 1f : 0.55f;
            RefreshInteractable();
        }

        private void HandleClick()
        {
            if (!_available || _cooldownLeft > 0f) return;
            onActivated.Invoke();
            _cooldownLeft = cooldownSeconds;
            RefreshInteractable();
        }

        private void Update()
        {
            if (_cooldownLeft <= 0f) return;
            _cooldownLeft = Mathf.Max(0f, _cooldownLeft - Time.unscaledDeltaTime);
            if (cooldownOverlay != null) cooldownOverlay.fillAmount = cooldownSeconds > 0f ? _cooldownLeft / cooldownSeconds : 0f;
            if (timerLabel != null) timerLabel.text = _cooldownLeft > 0f ? Mathf.CeilToInt(_cooldownLeft).ToString() : string.Empty;
            if (_cooldownLeft <= 0f) RefreshInteractable();
        }

        private void RefreshInteractable()
        {
            bool cooling = _cooldownLeft > 0f;
            _button.interactable = _available && !cooling;
            if (cooldownOverlay != null)
            {
                cooldownOverlay.gameObject.SetActive(cooling);
                if (!cooling) cooldownOverlay.fillAmount = 0f;
            }
            if (timerLabel != null && !cooling) timerLabel.text = string.Empty;
        }
    }
}
