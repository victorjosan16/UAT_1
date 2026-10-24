using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>„Puls” de scară: elementul crește scurt și revine (ex. bani câștigați).</summary>
    public class PunchScale : MonoBehaviour
    {
        [SerializeField] private float strength = 0.15f;
        [SerializeField] private float duration = 0.25f;

        private float _time = -1f;
        private float _currentStrength;

        public void Punch(float multiplier = 1f)
        {
            _time = 0f;
            _currentStrength = strength * multiplier;
        }

        private void Update()
        {
            if (_time < 0f) return;
            _time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(_time / duration);
            transform.localScale = Vector3.one * (1f + _currentStrength * Mathf.Sin(t * Mathf.PI));
            if (t >= 1f)
            {
                _time = -1f;
                transform.localScale = Vector3.one;
            }
        }
    }
}
