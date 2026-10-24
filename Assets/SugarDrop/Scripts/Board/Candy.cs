using UnityEngine;

namespace SugarDrop
{
    /// <summary>
    /// O bomboană din grămadă. Borcanul o numără o singură dată; după ce a intrat, se „așază” micșorându-se
    /// ușor, ca borcanul să încapă toate bomboanele nivelului.
    /// </summary>
    public class Candy : MonoBehaviour
    {
        private const float SettleScale = 0.6f;
        private const float SettleDuration = 0.5f;

        public bool Counted { get; private set; }
        public Color Color { get; set; } = Color.white;

        private float _settleT = -1f;
        private Vector3 _baseScale;

        public void MarkCounted()
        {
            if (Counted) return;
            Counted = true;
            _baseScale = transform.localScale;
            _settleT = 0f;
        }

        private void Update()
        {
            if (_settleT < 0f) return;
            _settleT += Time.deltaTime / SettleDuration;
            float t = Mathf.Clamp01(_settleT);
            transform.localScale = _baseScale * Mathf.Lerp(1f, SettleScale, t * t * (3f - 2f * t));
            if (t >= 1f) _settleT = -1f;
        }
    }
}
