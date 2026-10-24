using UnityEngine;
using UnityEngine.UI;

namespace QuarryEmpire
{
    /// <summary>
    /// Tranziția dintre straturi: ecranul se întunecă scurt cu „Coborâm…”, apoi apare titlul stratului nou
    /// („STRATUL 3 · 35 m · Rocă”) și se luminează la loc.
    /// </summary>
    public class LayerTransitionUI : MonoBehaviour
    {
        [SerializeField] private float fadeInDelay = 1f;
        [SerializeField] private float fadeSpeed = 2.5f;
        [SerializeField] private float titleHold = 1.4f;

        private CanvasGroup _group;
        private UiLabel _title;
        private UiLabel _subtitle;
        private float _target;
        private float _darkenAt = -1f;
        private float _holdUntil;
        private bool _firstLayerShown;

        private void Awake()
        {
            Image overlay = UiFactory.CreateImage("LayerOverlay", transform, UiFactory.WhiteSprite, new Color(0.05f, 0.04f, 0.08f, 0.82f));
            overlay.preserveAspect = false;
            UiFactory.Stretch(overlay.rectTransform, -200f);
            _group = overlay.gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _group.interactable = false;

            _title = UiFactory.CreateText("Title", overlay.transform, "", 96, TextAnchor.MiddleCenter, UiRoot.Theme.AccentColor);
            UiFactory.Place(_title.RectTransform, new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 140f));
            _subtitle = UiFactory.CreateText("Subtitle", overlay.transform, "", 46);
            UiFactory.Place(_subtitle.RectTransform, new Vector2(0.5f, 0.47f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 80f));
        }

        private void OnEnable()
        {
            GameEvents.LayerCleared += OnLayerCleared;
            GameEvents.LayerStarted += OnLayerStarted;
        }

        private void OnDisable()
        {
            GameEvents.LayerCleared -= OnLayerCleared;
            GameEvents.LayerStarted -= OnLayerStarted;
        }

        private void OnLayerCleared(int _)
        {
            _darkenAt = Time.unscaledTime + fadeInDelay;
            _title.Text = "";
            _subtitle.Text = Loc.T("Coborâm…");
        }

        private void OnLayerStarted(int index, LayerDefinition layer)
        {
            // La pornirea jocului nu arătăm tranziția (stratul salvat apare direct).
            if (!_firstLayerShown)
            {
                _firstLayerShown = true;
                return;
            }
            int depth = LayerGenerator.Instance != null ? LayerGenerator.Instance.DepthMeters : 0;
            _title.Text = Loc.F("STRATUL {0}", index + 1);
            _subtitle.Text = $"{depth} m · {Loc.T(layer.DisplayName)}";
            _darkenAt = -1f;
            _target = 1f;
            _group.alpha = Mathf.Max(_group.alpha, 0.6f);
            _holdUntil = Time.unscaledTime + titleHold;
        }

        private void Update()
        {
            if (_darkenAt > 0f && Time.unscaledTime >= _darkenAt)
            {
                _darkenAt = -1f;
                _target = 1f;
                _holdUntil = float.MaxValue; // rămâne întunecat până apare stratul nou
            }
            if (_target > 0f && Time.unscaledTime > _holdUntil) _target = 0f;
            _group.alpha = Mathf.MoveTowards(_group.alpha, _target, fadeSpeed * Time.unscaledDeltaTime);
        }
    }
}
