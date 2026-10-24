using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>Sclipire mică, în culoarea resursei, când o bucată ajunge în stiva jucătorului.</summary>
    public class CollectVfx : MonoBehaviour
    {
        [SerializeField] private Material particleMaterial;
        [SerializeField] private float maxDistanceFromPlayer = 4f;

        private GameObject _sparkle;

        private void Awake()
        {
            _sparkle = ParticleFactory.CreateBurst("CollectFx", transform, ParticleFactory.CreateDiscMaterial(particleMaterial),
                new ParticleFactory.BurstSettings
                {
                    Count = 5,
                    Speed = new Vector2(0.6f, 1.6f),
                    Size = new Vector2(0.08f, 0.15f),
                    Lifetime = new Vector2(0.25f, 0.45f),
                    Gravity = -0.4f,
                    Radius = 0.15f,
                    Shape = ParticleSystemShapeType.Sphere,
                    ShrinkOverLifetime = true,
                });
        }

        private void OnEnable() => GameEvents.ResourceCollected += OnCollected;
        private void OnDisable() => GameEvents.ResourceCollected -= OnCollected;

        private void OnCollected(ResourceData resource, Vector3 position)
        {
            if (resource == null || !ParticleFactory.IsNearPlayer(position, maxDistanceFromPlayer)) return;
            ParticleFactory.Play(_sparkle, position, Color.Lerp(resource.UiColor, Color.white, 0.35f));
        }
    }
}
