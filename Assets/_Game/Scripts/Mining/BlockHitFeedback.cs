using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// Feedback vizual la lovire: blocul se „turtește” scurt și clipește alb.
    /// Folosește MaterialPropertyBlock ca să nu creeze copii de material (important pe mobil).
    /// </summary>
    public class BlockHitFeedback : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor"); // culoarea în URP Lit

        [SerializeField] private MineBlock block;
        [Tooltip("Copilul „Visual” (se scalează doar el, nu și colliderul).")]
        [SerializeField] private Transform visual;
        [SerializeField] private Renderer targetRenderer;
        [SerializeField, Range(0f, 0.5f)] private float punchAmount = 0.15f;
        [SerializeField, Min(0.01f)] private float punchDuration = 0.15f;
        [SerializeField, Min(0.01f)] private float flashDuration = 0.1f;
        [SerializeField] private Color flashColor = Color.white;

        private MaterialPropertyBlock _propertyBlock;
        private Vector3 _baseScale;
        private Color _originalColor;
        private float _timer = -1f; // -1 = nu animăm

        private void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
            _baseScale = visual.localScale;
        }

        private void OnEnable() => block.Damaged += HandleDamaged;

        private void OnDisable()
        {
            block.Damaged -= HandleDamaged;
            ResetVisual();
        }

        private void HandleDamaged(MineBlock _)
        {
            // Citim culoarea acum, pentru că materialul se poate schimba prin MineBlock.Init.
            Material material = targetRenderer.sharedMaterial;
            _originalColor = material != null && material.HasProperty(BaseColorId)
                ? material.GetColor(BaseColorId)
                : Color.white;
            _timer = 0f;
        }

        private void Update()
        {
            if (_timer < 0f) return;
            _timer += Time.deltaTime;

            // Turtire: scade pe înălțime și revine (o jumătate de sinusoidă).
            float punchT = Mathf.Clamp01(_timer / punchDuration);
            float squash = punchAmount * Mathf.Sin(punchT * Mathf.PI);
            visual.localScale = new Vector3(
                _baseScale.x * (1f + squash * 0.5f),
                _baseScale.y * (1f - squash),
                _baseScale.z * (1f + squash * 0.5f));

            // Clipire: pornește alb și revine la culoarea originală.
            float flashT = 1f - Mathf.Clamp01(_timer / flashDuration);
            _propertyBlock.SetColor(BaseColorId, Color.Lerp(_originalColor, flashColor, flashT));
            targetRenderer.SetPropertyBlock(_propertyBlock);

            if (_timer >= Mathf.Max(punchDuration, flashDuration)) ResetVisual();
        }

        private void ResetVisual()
        {
            _timer = -1f;
            visual.localScale = _baseScale;
            // Fără property block, renderer-ul revine la batching-ul normal (SRP Batcher).
            targetRenderer.SetPropertyBlock(null);
        }
    }
}
