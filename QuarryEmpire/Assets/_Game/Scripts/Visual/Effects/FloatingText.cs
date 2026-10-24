using TMPro;
using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// Text 3D care sare, urcă și se stinge (ex. „+3$”). Vine din ObjectPool și se întoarce singur.
    /// Se orientează mereu spre cameră. Folosește un material TMP partajat (cu contur), fără copii.
    /// </summary>
    [RequireComponent(typeof(TextMeshPro))]
    public class FloatingText : MonoBehaviour, IPoolable
    {
        [SerializeField] private TextMeshPro label;
        [SerializeField, Min(0.1f)] private float lifetime = 0.9f;
        [SerializeField] private float riseDistance = 1.1f;
        [Tooltip("Mărimea în timp (0..1 din viață). Implicit: pop rapid, apoi stă.")]
        [SerializeField] private AnimationCurve scaleCurve = new(
            new Keyframe(0f, 0.2f), new Keyframe(0.15f, 1.25f), new Keyframe(0.3f, 1f), new Keyframe(1f, 0.9f));

        private VfxManager _owner;
        private Transform _camera;
        private Vector3 _start;
        private Color _color;
        private float _scale = 1f;
        private float _timer = -1f;

        private void Awake()
        {
            if (label == null) label = GetComponent<TextMeshPro>();
        }

        public void Play(VfxManager owner, string text, Color color, float scale)
        {
            if (_owner == null && owner != null) owner.ActiveFloatingTexts++;
            _owner = owner;
            _start = transform.position;
            _color = color;
            _scale = scale;
            _timer = 0f;
            label.text = text;
            label.color = color;
            if (_camera == null && Camera.main != null) _camera = Camera.main.transform;
            UpdateVisual(0f);
        }

        private void LateUpdate()
        {
            if (_timer < 0f) return;
            _timer += Time.deltaTime;
            float t = Mathf.Clamp01(_timer / lifetime);
            UpdateVisual(t);
            if (t >= 1f) Finish();
        }

        private void UpdateVisual(float t)
        {
            // Ease-out pe urcare: pornește repede și încetinește.
            float rise = 1f - (1f - t) * (1f - t);
            transform.position = _start + Vector3.up * (rise * riseDistance);
            transform.localScale = Vector3.one * (scaleCurve.Evaluate(t) * _scale);
            if (_camera != null) transform.rotation = _camera.rotation; // billboard

            Color c = _color;
            c.a = t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f;
            label.color = c;
        }

        private void Finish()
        {
            _timer = -1f;
            if (ObjectPool.Instance != null) ObjectPool.Instance.Despawn(gameObject);
            else gameObject.SetActive(false);
        }

        public void OnSpawned() => _timer = -1f;

        public void OnDespawned()
        {
            if (_owner != null) _owner.ActiveFloatingTexts = Mathf.Max(0, _owner.ActiveFloatingTexts - 1);
            _owner = null;
            _timer = -1f;
        }
    }
}
