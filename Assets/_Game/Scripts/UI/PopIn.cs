using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>Panou care apare cu un mic „pop” (scară + transparență) când e activat.</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class PopIn : MonoBehaviour
    {
        [SerializeField] private float duration = 0.22f;

        private CanvasGroup _group;
        private float _time;

        private void Awake() => _group = GetComponent<CanvasGroup>();

        private void OnEnable()
        {
            _time = 0f;
            if (_group != null) _group.alpha = 0f;
            transform.localScale = Vector3.one * 0.9f;
        }

        private void Update()
        {
            if (_time >= duration) return;
            _time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(_time / duration);
            float eased = 1f - (1f - t) * (1f - t);
            _group.alpha = eased;
            // Ușoară depășire (overshoot) pentru un efect „jucăuș”.
            float scale = Mathf.LerpUnclamped(0.9f, 1f, eased) + Mathf.Sin(t * Mathf.PI) * 0.04f;
            transform.localScale = Vector3.one * scale;
        }
    }
}
