using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>Inel de lumină în jurul jucătorului și un mic „salt” al modelului la fiecare upgrade.</summary>
    public class UpgradeVfx : MonoBehaviour
    {
        [SerializeField] private Material particleMaterial;
        [SerializeField] private Color ringColor = new(0.35f, 0.95f, 0.55f);
        [SerializeField] private Color sparkColor = new(1f, 0.85f, 0.3f);
        [Tooltip("Modelul jucătorului (primește un puls de scară).")]
        [SerializeField] private Transform playerModel;

        private GameObject _ring;
        private GameObject _sparks;
        private float _punchTime = -1f;

        private void Awake()
        {
            Material disc = ParticleFactory.CreateDiscMaterial(particleMaterial);
            _ring = ParticleFactory.CreateBurst("UpgradeRingFx", transform, disc, new ParticleFactory.BurstSettings
            {
                Count = 28,
                Speed = new Vector2(3f, 3.5f),
                Size = new Vector2(0.18f, 0.26f),
                Lifetime = new Vector2(0.45f, 0.55f),
                Gravity = 0f,
                Radius = 0.4f,
                Shape = ParticleSystemShapeType.Circle,
                ShapeRotation = new Vector3(-90f, 0f, 0f),
                ShrinkOverLifetime = true,
            });
            _sparks = ParticleFactory.CreateBurst("UpgradeSparkFx", transform, disc, new ParticleFactory.BurstSettings
            {
                Count = 14,
                Speed = new Vector2(1.5f, 3f),
                Size = new Vector2(0.1f, 0.18f),
                Lifetime = new Vector2(0.6f, 0.9f),
                Gravity = -0.6f,
                Radius = 0.5f,
                Shape = ParticleSystemShapeType.Sphere,
                ShrinkOverLifetime = true,
            });
        }

        private void OnEnable() => GameEvents.Upgraded += OnUpgraded;
        private void OnDisable() => GameEvents.Upgraded -= OnUpgraded;

        private void OnUpgraded(UpgradeDefinition _, int __)
        {
            PlayerRoot player = PlayerRoot.Instance;
            if (player == null) return;
            ParticleFactory.Play(_ring, player.Position + Vector3.up * 0.2f, ringColor);
            ParticleFactory.Play(_sparks, player.Position + Vector3.up * 1.2f, sparkColor);
            _punchTime = 0f;
        }

        private void Update()
        {
            if (_punchTime < 0f || playerModel == null) return;
            _punchTime += Time.deltaTime;
            float t = Mathf.Clamp01(_punchTime / 0.35f);
            playerModel.localScale = Vector3.one * (1f + Mathf.Sin(t * Mathf.PI) * 0.18f);
            if (t >= 1f)
            {
                _punchTime = -1f;
                playerModel.localScale = Vector3.one;
            }
        }
    }
}
