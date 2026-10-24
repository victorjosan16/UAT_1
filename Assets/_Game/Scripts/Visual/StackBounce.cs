using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>Teancul de pe spate „sare” puțin când primește o resursă nouă (feedback la colectare).</summary>
    public class StackBounce : MonoBehaviour
    {
        [SerializeField] private ResourceStack stack;
        [SerializeField] private float strength = 0.18f;
        [SerializeField] private float duration = 0.18f;

        private Transform _anchor;
        private int _lastCount;
        private float _time = -1f;

        private void OnEnable()
        {
            _anchor = stack.Anchor;
            _lastCount = stack.Count;
            stack.Changed += OnChanged;
        }

        private void OnDisable()
        {
            stack.Changed -= OnChanged;
            if (_anchor != null) _anchor.localScale = Vector3.one;
        }

        private void OnChanged(ResourceStack _)
        {
            if (stack.Count > _lastCount) _time = 0f;
            _lastCount = stack.Count;
        }

        private void Update()
        {
            if (_time < 0f) return;
            _time += Time.deltaTime;
            float t = Mathf.Clamp01(_time / duration);
            float squash = Mathf.Sin(t * Mathf.PI) * strength;
            _anchor.localScale = new Vector3(1f + squash, 1f - squash * 0.5f, 1f + squash);
            if (t >= 1f)
            {
                _time = -1f;
                _anchor.localScale = Vector3.one;
            }
        }
    }
}
