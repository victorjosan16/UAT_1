using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// Bară de progres cu capete rotunjite: în loc să „taie” imaginea, îi micșorează lățimea,
    /// deci colțurile rămân rotunde la orice valoare. Valoarea se animă lin.
    /// </summary>
    public class UiProgressBar : MonoBehaviour
    {
        [SerializeField] private RectTransform fill;
        [SerializeField] private float smoothing = 8f;
        [Tooltip("Lățimea minimă a umplerii (în fracțiuni), ca rotunjirea să nu se deformeze.")]
        [SerializeField] private float minVisible = 0.06f;

        private float _target;
        private float _current;

        public void Initialize(RectTransform fillRect)
        {
            fill = fillRect;
            Apply(0f);
        }

        /// <param name="instant">True = fără animație (ex. la pornire).</param>
        public void SetValue(float value, bool instant = false)
        {
            _target = Mathf.Clamp01(value);
            if (instant) Apply(_target);
        }

        private void Update()
        {
            if (Mathf.Approximately(_current, _target)) return;
            Apply(Mathf.MoveTowards(_current, _target, Time.unscaledDeltaTime * smoothing * Mathf.Max(0.1f, Mathf.Abs(_target - _current))));
        }

        private void Apply(float value)
        {
            _current = value;
            if (fill == null) return;
            fill.gameObject.SetActive(value > 0.001f);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(Mathf.Max(minVisible, value), 1f);
            fill.offsetMin = new Vector2(4f, 4f);
            fill.offsetMax = new Vector2(-4f, -4f);
        }
    }
}
