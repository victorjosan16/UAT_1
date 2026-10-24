using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SugarDrop
{
    /// <summary>
    /// Derularea hărții: se poate trage cu degetul (ScrollRect) și se poate centra lin pe un nivel.
    /// Mișcă straturile din fundal mai încet decât drumul (paralaxă, efect de adâncime).
    /// Centrarea automată se oprește imediat ce jucătorul trage de hartă.
    /// </summary>
    [RequireComponent(typeof(ScrollRect))]
    public class LevelMapScrollController : MonoBehaviour, IBeginDragHandler
    {
        [SerializeField] private float focusDuration = 0.6f;

        private ScrollRect _scroll;
        private RectTransform[] _layers;
        private float[] _factors;
        private float _from, _to, _time = -1f;

        public ScrollRect Scroll => _scroll != null ? _scroll : _scroll = GetComponent<ScrollRect>();
        public bool IsFocusing => _time >= 0f;

        private void Awake()
        {
            _scroll = GetComponent<ScrollRect>();
            _scroll.onValueChanged.AddListener(_ => UpdateParallax());
        }

        /// <summary>Straturi de fundal: factor 0 = fix, 1 = se mișcă odată cu drumul.</summary>
        public void SetParallaxLayers(RectTransform[] layers, float[] factors)
        {
            _layers = layers;
            _factors = factors;
            UpdateParallax();
        }

        /// <summary>Derulează la poziția normalizată (0 = jos, 1 = sus).</summary>
        public void FocusNormalized(float target, bool animate)
        {
            target = Mathf.Clamp01(target);
            Scroll.StopMovement();
            if (!animate)
            {
                _time = -1f;
                Scroll.verticalNormalizedPosition = target;
                UpdateParallax();
                return;
            }
            _from = Scroll.verticalNormalizedPosition;
            _to = target;
            _time = 0f;
        }

        public void OnBeginDrag(PointerEventData eventData) => _time = -1f;

        private void Update()
        {
            if (_time < 0f) return;
            _time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(_time / focusDuration);
            Scroll.verticalNormalizedPosition = Mathf.LerpUnclamped(_from, _to, UiEase.InOutCubic(t));
            if (t >= 1f) _time = -1f;
        }

        private void UpdateParallax()
        {
            if (_layers == null || Scroll.content == null) return;
            float offset = Scroll.content.anchoredPosition.y;
            for (int i = 0; i < _layers.Length; i++)
                if (_layers[i] != null) _layers[i].anchoredPosition = new Vector2(0f, offset * _factors[i]);
        }
    }
}
