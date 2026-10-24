using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// Când apare un strat nou, blocurile „răsar” din pământ într-un val pornit din centrul carierei.
    /// Animează doar modelul (nu colliderul).
    /// </summary>
    public class BlockSpawnPop : MonoBehaviour
    {
        [SerializeField] private MineBlock block;
        [SerializeField] private Transform visual;
        [SerializeField] private float duration = 0.35f;
        [Tooltip("Întârzierea pe metru de la centrul carierei (efectul de val).")]
        [SerializeField] private float delayPerMeter = 0.05f;
        [SerializeField] private float riseDepth = 0.9f;

        private float _time = -1f;
        private float _delay;
        private Vector3 _baseScale;

        private void Awake() => _baseScale = visual.localScale;

        private void OnEnable() => block.Initialized += OnInitialized;

        private void OnDisable()
        {
            block.Initialized -= OnInitialized;
            _time = -1f;
            visual.localPosition = Vector3.zero;
            visual.localScale = _baseScale;
        }

        private void OnInitialized(MineBlock _)
        {
            Vector3 center = transform.parent != null ? transform.parent.position : transform.position;
            Vector3 offset = transform.position - center;
            offset.y = 0f;
            _delay = offset.magnitude * delayPerMeter;
            _time = 0f;
            Apply(0f);
        }

        private void Update()
        {
            if (_time < 0f) return;
            _time += Time.deltaTime;
            float t = Mathf.Clamp01((_time - _delay) / duration);
            Apply(t);
            if (t >= 1f) _time = -1f;
        }

        private void Apply(float t)
        {
            // Ușoară depășire la final (back-out) pentru un efect jucăuș.
            float eased = 1f + 2.2f * Mathf.Pow(t - 1f, 3f) + 1.2f * Mathf.Pow(t - 1f, 2f);
            visual.localPosition = Vector3.down * (riseDepth * (1f - Mathf.Clamp01(eased)));
            visual.localScale = t >= 1f ? _baseScale : _baseScale * Mathf.LerpUnclamped(0.3f, 1f, eased);
        }
    }
}
