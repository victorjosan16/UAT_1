using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>Ploaie scurtă de monede aurii acolo unde se câștigă bani (vânzare, comenzi).</summary>
    public class SellVfx : MonoBehaviour
    {
        [SerializeField] private Material particleMaterial;
        [SerializeField] private Color coinColor = new(1f, 0.8f, 0.2f);
        [Tooltip("Nu mai des de atât (secunde), ca să nu se suprapună zeci de efecte la vânzări rapide.")]
        [SerializeField] private float minInterval = 0.12f;

        private GameObject _coins;
        private float _lastPlay = -10f;

        private void Awake()
        {
            _coins = ParticleFactory.CreateBurst("SellFx", transform, ParticleFactory.CreateDiscMaterial(particleMaterial),
                new ParticleFactory.BurstSettings
                {
                    Count = 8,
                    Speed = new Vector2(2.5f, 4.5f),
                    Size = new Vector2(0.14f, 0.22f),
                    Lifetime = new Vector2(0.45f, 0.7f),
                    Gravity = 1.6f,
                    Radius = 0.2f,
                    Shape = ParticleSystemShapeType.Hemisphere,
                    ShrinkOverLifetime = false,
                });
        }

        private void OnEnable() => GameEvents.MoneyEarned += OnMoneyEarned;
        private void OnDisable() => GameEvents.MoneyEarned -= OnMoneyEarned;

        private void OnMoneyEarned(long _, Vector3 position, bool animate)
        {
            if (!animate || Time.time - _lastPlay < minInterval) return;
            _lastPlay = Time.time;
            ParticleFactory.Play(_coins, position + Vector3.up * 0.3f, coinColor);
        }
    }
}
