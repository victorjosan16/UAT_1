using System;
using QuarryEmpire;
using UnityEngine;
using UnityEngine.UI;

namespace SugarDrop
{
    /// <summary>
    /// Tranziția dintre ecrane: un strat pastelat acoperă ecranul (cu o bomboană care se rotește), la mijloc se
    /// schimbă ecranul (încărcarea nivelului se ascunde aici), apoi stratul alunecă și dispare.
    /// Cât rulează, blochează atingerile — apăsările rapide repetate nu pornesc nimic de două ori.
    /// </summary>
    public class UITransitionController : MonoBehaviour
    {
        public static UITransitionController Instance { get; private set; }

        [SerializeField] private float coverDuration = 0.22f;
        [SerializeField] private float revealDuration = 0.3f;

        private CanvasGroup _group;
        private RectTransform _cover;
        private RectTransform _swirl;
        private Action _midpoint;
        private float _time;
        private int _phase; // 0 = inactiv, 1 = acoperă, 2 = descoperă

        public bool IsBusy => _phase != 0;

        private void Awake()
        {
            Instance = this;
            var rect = (RectTransform)transform;
            _group = gameObject.AddComponent<CanvasGroup>();
            Image cover = UiFactory.CreateImage("Cover", rect, SugarSprites.BackgroundGradient, Color.white);
            cover.preserveAspect = false;
            cover.raycastTarget = true;
            UiFactory.Stretch(cover.rectTransform, -300f);
            _cover = cover.rectTransform;
            Image swirl = UiFactory.CreateImage("Swirl", rect, SugarSprites.CandySwirl, Palette.WallStripe);
            UiFactory.Place(swirl.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(220f, 220f));
            _swirl = swirl.rectTransform;
            SetVisible(false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Rulează „midpoint” cu ecranul acoperit. Ignorat dacă o tranziție e deja în curs.</summary>
        public bool Play(Action midpoint)
        {
            if (IsBusy) return false;
            _midpoint = midpoint;
            _phase = 1;
            _time = 0f;
            SetVisible(true);
            _group.alpha = 0f;
            return true;
        }

        /// <summary>Rulează cu tranziție dacă există controller, altfel imediat.</summary>
        public static void Run(Action action)
        {
            if (Instance != null && Instance.isActiveAndEnabled)
            {
                Instance.Play(action);
                return;
            }
            action?.Invoke();
        }

        private void SetVisible(bool visible)
        {
            _group.alpha = visible ? 1f : 0f;
            _group.blocksRaycasts = visible;
            _cover.gameObject.SetActive(visible);
            _swirl.gameObject.SetActive(visible);
        }

        private void Update()
        {
            if (_phase == 0) return;
            _time += Time.unscaledDeltaTime;
            _swirl.localRotation = Quaternion.Euler(0f, 0f, -_time * 360f);

            if (_phase == 1)
            {
                float t = Mathf.Clamp01(_time / coverDuration);
                _group.alpha = UiEase.OutQuad(t);
                _cover.anchoredPosition = new Vector2(0f, (1f - UiEase.OutCubic(t)) * 160f);
                _swirl.localScale = Vector3.one * UiEase.OutBack(t);
                if (t < 1f) return;
                Action action = _midpoint;
                _midpoint = null;
                try
                {
                    action?.Invoke();
                }
                finally
                {
                    _phase = 2;
                    _time = 0f;
                }
                return;
            }

            float r = Mathf.Clamp01(_time / revealDuration);
            _group.alpha = 1f - UiEase.OutQuad(r);
            _cover.anchoredPosition = new Vector2(0f, -UiEase.InOutCubic(r) * 160f);
            _swirl.localScale = Vector3.one * (1f - r);
            if (r < 1f) return;
            _phase = 0;
            _cover.anchoredPosition = Vector2.zero;
            SetVisible(false);
        }
    }
}
