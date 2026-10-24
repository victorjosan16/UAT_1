using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>„Puls” de scară (ex. bani crescuți, misiune completă). Folosește timp nescalat (merge și în pauză).</summary>
    public class UiPunchScale : MonoBehaviour
    {
        [SerializeField] private float amount = 0.18f;
        [SerializeField, Min(0.01f)] private float duration = 0.25f;

        private Vector3 _baseScale = Vector3.one;
        private float _timer = -1f;
        private bool _initialized;

        private void Awake() => Init();

        private void Init()
        {
            if (_initialized) return;
            _baseScale = transform.localScale;
            _initialized = true;
        }

        public void Punch()
        {
            Init();
            _timer = 0f;
        }

        private void OnDisable()
        {
            if (!_initialized) return;
            _timer = -1f;
            transform.localScale = _baseScale;
        }

        private void Update()
        {
            if (_timer < 0f) return;
            _timer += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(_timer / duration);
            // Sinusoidă amortizată: crește, depășește puțin, revine.
            float s = Mathf.Sin(k * Mathf.PI) * (1f - k * 0.5f) * amount;
            transform.localScale = _baseScale * (1f + s);
            if (k >= 1f)
            {
                _timer = -1f;
                transform.localScale = _baseScale;
            }
        }
    }
}
