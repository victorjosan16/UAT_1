using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// Măsoară FPS-ul mediu în timpul jocului; dacă telefonul nu ține pasul pe calitatea Înaltă,
    /// coboară automat la Echilibrată (o singură dată pe sesiune).
    /// </summary>
    public class PerformanceGovernor : MonoBehaviour
    {
        [SerializeField] private float warmupSeconds = 8f;
        [SerializeField] private float sampleSeconds = 6f;
        [Tooltip("Coboară calitatea dacă FPS-ul mediu e sub acest procent din țintă.")]
        [SerializeField, Range(0.5f, 1f)] private float threshold = 0.82f;

        private float _elapsed;
        private float _sampleTime;
        private int _frames;
        private bool _done;

        private void Update()
        {
            if (_done || Time.timeScale <= 0f || QualityProfile.Instance == null) return;

            float dt = Time.unscaledDeltaTime;
            _elapsed += dt;
            if (_elapsed < warmupSeconds) return;

            _sampleTime += dt;
            _frames++;
            if (_sampleTime < sampleSeconds) return;

            float fps = _frames / _sampleTime;
            _sampleTime = 0f;
            _frames = 0;
            if (fps >= QualityProfile.Instance.TargetFrameRate * threshold) return;

            _done = true;
            if (QualityProfile.Instance.TryStepDownAutomatically())
                Debug.Log($"[QuarryEmpire] FPS mediu {fps:0}: calitatea a fost redusă automat la Echilibrată.");
        }
    }
}
