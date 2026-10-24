using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>Scântei și praf mic în punctul în care târnăcopul (sau utilajul) lovește blocul.</summary>
    public class ImpactVfx : MonoBehaviour
    {
        [SerializeField] private Material particleMaterial;
        [SerializeField] private float maxDistanceFromPlayer = 10f;

        private GameObject _impact;

        private void Awake()
        {
            _impact = ParticleFactory.CreateBurst("ImpactFx", transform, ParticleFactory.CreateDiscMaterial(particleMaterial),
                new ParticleFactory.BurstSettings
                {
                    Count = 7,
                    Speed = new Vector2(1.5f, 3.5f),
                    Size = new Vector2(0.07f, 0.16f),
                    Lifetime = new Vector2(0.2f, 0.4f),
                    Gravity = 1.2f,
                    Radius = 0.1f,
                    Shape = ParticleSystemShapeType.Hemisphere,
                    ShrinkOverLifetime = true,
                });
        }

        private void OnEnable() => GameEvents.BlockHit += OnBlockHit;
        private void OnDisable() => GameEvents.BlockHit -= OnBlockHit;

        private void OnBlockHit(MineBlock block)
        {
            Vector3 position = block.transform.position;
            if (!ParticleFactory.IsNearPlayer(position, maxDistanceFromPlayer)) return;

            // Punctul de impact: fața blocului dinspre jucător.
            PlayerRoot player = PlayerRoot.Instance;
            if (player != null)
            {
                Vector3 toPlayer = player.Position - position;
                toPlayer.y = 0f;
                if (toPlayer.sqrMagnitude > 0.01f) position += toPlayer.normalized * 0.5f;
            }
            position += Vector3.up * 0.25f;
            Color color = Color.Lerp(BlockBreakParticles.GetBlockColor(block), Color.white, 0.4f);
            ParticleFactory.Play(_impact, position, color);
        }
    }
}
