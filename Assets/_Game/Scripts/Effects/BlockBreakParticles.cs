using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// Particule (așchii) la spargerea blocurilor, colorate ca blocul.
    /// Sistemul de particule e construit din cod o singură dată și apoi reciclat prin ObjectPool.
    /// </summary>
    public class BlockBreakParticles : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private Material particleMaterial;
        [SerializeField, Range(1, 40)] private int particleCount = 14;
        [SerializeField] private float maxDistanceFromPlayer = 14f;

        private GameObject _template;

        private void Awake() => _template = BuildTemplate();

        private void OnEnable() => GameEvents.BlockDestroyed += OnBlockDestroyed;
        private void OnDisable() => GameEvents.BlockDestroyed -= OnBlockDestroyed;

        private void OnBlockDestroyed(MineBlock block)
        {
            if (ObjectPool.Instance == null) return;
            Vector3 position = block.transform.position;
            PlayerRoot player = PlayerRoot.Instance;
            if (player != null && (player.Position - position).sqrMagnitude > maxDistanceFromPlayer * maxDistanceFromPlayer) return;

            GameObject instance = ObjectPool.Instance.Spawn(_template, position, Quaternion.identity);
            var system = instance.GetComponent<ParticleSystem>();
            ParticleSystem.MainModule main = system.main;
            main.startColor = GetBlockColor(block);
            system.Clear();
            system.Play();
        }

        private static Color GetBlockColor(MineBlock block)
        {
            Material material = block.Data != null ? block.Data.BlockMaterial : null;
            return material != null && material.HasProperty(BaseColorId) ? material.GetColor(BaseColorId) : Color.gray;
        }

        private GameObject BuildTemplate()
        {
            var go = new GameObject("BlockBreakFx");
            go.SetActive(false); // șablon inactiv; ObjectPool creează copii
            go.transform.SetParent(transform, false);

            var system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = system.main;
            main.duration = 0.5f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
            main.gravityModifier = 1.4f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 60;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)particleCount) });

            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.4f;

            var particleRenderer = go.GetComponent<ParticleSystemRenderer>();
            particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            if (particleMaterial != null) particleRenderer.sharedMaterial = particleMaterial;

            go.AddComponent<AutoDespawn>().Lifetime = 1.2f;
            return go;
        }
    }
}
