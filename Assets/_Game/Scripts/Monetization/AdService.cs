using System;
using UnityEngine;
using UnityEngine.UI;

namespace QuarryEmpire
{
    /// <summary>
    /// Punctul unic prin care jocul cere reclame „rewarded” (cu recompensă).
    /// Acum folosește o reclamă de TEST (un ecran cu numărătoare inversă), ca jocul să poată fi
    /// testat fără cont de reclame. Pentru reclame reale (Unity LevelPlay), se înlocuiește doar
    /// corpul metodei ShowRewarded — restul jocului nu se schimbă. Vezi Assets/SugarDrop/RECLAME.md.
    /// </summary>
    public class AdService : MonoBehaviour
    {
        public static AdService Instance { get; private set; }

        [SerializeField] private float testAdSeconds = 3f;

        private GameObject _overlay;
        private UiLabel _countdown;
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
            _countdown.Text = Loc.F("RECLAMĂ DE TEST\n\nRecompensa vine în {0}…", Mathf.CeilToInt(Mathf.Max(0f, _timeLeft)));
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
            Image overlay = UiFactory.CreateImage("AdOverlay", canvas != null ? canvas : transform, UiFactory.WhiteSprite, new Color(0.03f, 0.04f, 0.08f, 0.96f));
            overlay.preserveAspect = false;
            overlay.raycastTarget = true;
            UiFactory.Stretch(overlay.rectTransform);
            overlay.transform.SetAsLastSibling();
            _overlay = overlay.gameObject;

            Image badge = UiFactory.CreateImage("PlayIcon", overlay.transform, ProceduralSprites.Play, UiRoot.Theme.AdColor);
            UiFactory.Place(badge.rectTransform, new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(180f, 180f));

            _countdown = UiFactory.CreateText("Countdown", overlay.transform, "", 56);
            UiFactory.Place(_countdown.RectTransform, new Vector2(0.5f, 0.45f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 300f));

            var close = UiFactory.CreateButton("Close", overlay.transform, Loc.T("ÎNCHIDE (fără recompensă)"),
                UiRoot.Theme.NegativeColor, () => Finish(false), 34);
            UiFactory.Place((RectTransform)close.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 160f), new Vector2(660f, 120f));
            _overlay.SetActive(false);
        }
    }
}
