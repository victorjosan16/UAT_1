using System;
using UnityEngine;
using UnityEngine.UI;

namespace QuarryEmpire
{
    /// <summary>
    /// Punctul unic prin care jocul cere reclame „rewarded” (cu recompensă).
    /// Acum folosește o reclamă de TEST (un ecran cu numărătoare inversă), ca jocul să poată fi
    /// testat fără cont de reclame. Pentru reclame reale (Unity LevelPlay), se înlocuiește doar
    /// corpul metodei ShowRewarded — restul jocului nu se schimbă. Vezi docs/MONETIZARE.md.
    /// </summary>
    public class AdService : MonoBehaviour
    {
        public static AdService Instance { get; private set; }

        [SerializeField] private float testAdSeconds = 3f;

        private GameObject _overlay;
        private Text _countdown;
        private Action<bool> _onComplete;
        private float _timeLeft;
        private float _previousTimeScale = 1f;

        public bool IsShowing => _onComplete != null;

        private void Awake()
        {
            Instance = this;
            BuildOverlay();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Arată o reclamă cu recompensă. onComplete(true) = jucătorul a văzut-o până la capăt.</summary>
        public void ShowRewarded(string placement, Action<bool> onComplete)
        {
            if (IsShowing)
            {
                onComplete?.Invoke(false);
                return;
            }
            _onComplete = onComplete;
            _timeLeft = testAdSeconds;
            _previousTimeScale = Time.timeScale;
            Time.timeScale = 0f; // jocul stă pe pauză în timpul reclamei
            _overlay.SetActive(true);
            Debug.Log($"[Ads] Reclamă de test pentru „{placement}”.");
        }

        private void Update()
        {
            if (!IsShowing) return;
            _timeLeft -= Time.unscaledDeltaTime;
            _countdown.text = $"RECLAMĂ DE TEST\n\nRecompensa vine în {Mathf.CeilToInt(Mathf.Max(0f, _timeLeft))}…";
            if (_timeLeft <= 0f) Finish(true);
        }

        private void Finish(bool rewarded)
        {
            Action<bool> callback = _onComplete;
            _onComplete = null;
            _overlay.SetActive(false);
            Time.timeScale = _previousTimeScale;
            callback?.Invoke(rewarded);
        }

        private void BuildOverlay()
        {
            RectTransform canvas = UiRoot.CanvasRect;
            Image overlay = UiFactory.CreatePanel("AdOverlay", canvas != null ? canvas : transform, new Color(0f, 0f, 0f, 0.92f));
            UiFactory.Stretch((RectTransform)overlay.transform);
            overlay.transform.SetAsLastSibling();
            _overlay = overlay.gameObject;

            _countdown = UiFactory.CreateText("Countdown", overlay.transform, "", 56);
            UiFactory.Stretch((RectTransform)_countdown.transform, 80f);

            var close = UiFactory.CreateButton("Close", overlay.transform, "ÎNCHIDE (fără recompensă)",
                UiRoot.Theme.NegativeColor, () => Finish(false), 34);
            UiFactory.Place((RectTransform)close.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 120f), new Vector2(620f, 110f));
            _overlay.SetActive(false);
        }
    }
}
