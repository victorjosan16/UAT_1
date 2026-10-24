using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// Efectul de spargere a blocurilor: bucăți de rocă 3D (colorate ca blocul) + un nor de praf.
    /// Șabloanele sunt construite din cod o singură dată și reciclate prin ObjectPool.
    /// </summary>
    public class BlockBreakParticles : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [Tooltip("Material de particule pentru praf (unlit, cu alpha clip).")]
        [SerializeField] private Material particleMaterial;
        [Tooltip("Material opac pentru bucățile de rocă. Gol = se folosește cel de praf.")]
        [SerializeField] private Material chunkMaterial;
        [Tooltip("Mesh-ul bucăților (ex. cubul rotunjit). Gol = particule plate.")]
        [SerializeField] private Mesh chunkMesh;
        [SerializeField, Range(1, 40)] private int particleCount = 10;
        [SerializeField] private float maxDistanceFromPlayer = 14f;

        private GameObject _chunks;
        private GameObject _dust;

        private void Awake()
        {
            _chunks = ParticleFactory.CreateBurst("BlockChunks", transform, chunkMaterial != null ? chunkMaterial : particleMaterial,
                new ParticleFactory.BurstSettings
                {
                    Count = particleCount,
                    Speed = new Vector2(3f, 6f),
                    Size = new Vector2(0.12f, 0.26f),
                    Lifetime = new Vector2(0.5f, 0.85f),
                    Gravity = 2f,
                    Radius = 0.35f,
                    Shape = ParticleSystemShapeType.Hemisphere,
                    Mesh = chunkMesh,
                    ShrinkOverLifetime = true,
                });
            _dust = ParticleFactory.CreateBurst("BlockDust", transform, ParticleFactory.CreateDiscMaterial(particleMaterial),
                new ParticleFactory.BurstSettings
                {
                    Count = 8,
                    Speed = new Vector2(0.6f, 1.6f),
                    Size = new Vector2(0.35f, 0.7f),
                    Lifetime = new Vector2(0.4f, 0.7f),
                    Gravity = -0.15f,
                    Radius = 0.45f,
                    Shape = ParticleSystemShapeType.Sphere,
                    ShrinkOverLifetime = true,
                });
        }

        private void OnEnable() => GameEvents.BlockDestroyed += OnBlockDestroyed;
        private void OnDisable() => GameEvents.BlockDestroyed -= OnBlockDestroyed;

        private void OnBlockDestroyed(MineBlock block)
        {
            Vector3 position = block.transform.position;
            if (!ParticleFactory.IsNearPlayer(position, maxDistanceFromPlayer)) return;
            Color color = GetBlockColor(block);
            ParticleFactory.Play(_chunks, position, color);
            ParticleFactory.Play(_dust, position, Color.Lerp(color, Color.white, 0.55f));
        }

        public static Color GetBlockColor(MineBlock block)
        {
            Material material = block.Data != null ? block.Data.BlockMaterial : null;
            return material != null && material.HasProperty(BaseColorId) ? material.GetColor(BaseColorId) : Color.gray;
        }
    }
}
