using System.Collections.Generic;
using UndergroundMetroTycoon.Simulation;
using UnityEngine;

namespace UndergroundMetroTycoon.Game
{
    /// <summary>
    /// Pooled particle effects: chunky soil debris and dust on hits, a bigger burst on breaks and confetti on
    /// contract completion. Each effect is a single always-alive ParticleSystem fed through Emit(), so there is
    /// no per-hit allocation or instantiation.
    /// </summary>
    public sealed class ExcavationVfx : MonoBehaviour
    {
        private readonly Dictionary<TerrainMaterial, ParticleSystem> _debris = new Dictionary<TerrainMaterial, ParticleSystem>();
        private readonly List<ParticleSystem> _confetti = new List<ParticleSystem>();
        private ParticleSystem _dust;
        private MaterialLibrary _materials;

        /// <summary>Scales particle counts for the graphics quality setting.</summary>
        public float Intensity { get; set; } = 1f;

        public void Init(MaterialLibrary materials)
        {
            _materials = materials;
            _dust = CreateSystem("Dust", materials.Get(new Color(0.86f, 0.76f, 0.62f)), PrimitiveType.Sphere, 120, 0.4f, -0.05f);
            foreach (Color c in new[] { Palette.Yellow, Palette.Cyan, Palette.Orange, Palette.Emerald })
                _confetti.Add(CreateSystem("Confetti", materials.Get(c), PrimitiveType.Cube, 80, 0.5f, 0.6f));
        }

        public void EmitHit(Vector3 position, TerrainMaterial material)
        {
            Emit(GetDebris(material), position, Count(5), 1.6f, 2.4f, 0.07f, 0.13f, 0.7f);
            Emit(_dust, position, Count(3), 0.4f, 0.8f, 0.15f, 0.28f, 0.6f);
        }

        public void EmitBreak(Vector3 position, TerrainMaterial material)
        {
            Emit(GetDebris(material), position, Count(12), 2f, 3.4f, 0.09f, 0.18f, 0.9f);
            Emit(_dust, position, Count(7), 0.5f, 1.2f, 0.2f, 0.4f, 0.8f);
        }

        /// <summary>Soil tossed off the shovel blade: a small arc of clumps thrown sideways.</summary>
        public void EmitToss(Vector3 position, Vector3 sideways, TerrainMaterial material)
        {
            ParticleSystem system = GetDebris(material);
            var emit = new ParticleSystem.EmitParams();
            int count = Count(6);
            for (int i = 0; i < count; i++)
            {
                Vector3 dir = (sideways.normalized * 0.8f + Vector3.up * 0.9f + Random.insideUnitSphere * 0.35f).normalized;
                emit.position = position + Random.insideUnitSphere * 0.06f;
                emit.velocity = dir * Random.Range(2.2f, 3.2f);
                emit.startSize = Random.Range(0.06f, 0.12f);
                emit.startLifetime = Random.Range(0.6f, 0.85f);
                emit.rotation3D = new Vector3(Random.Range(0f, 360f), Random.Range(0f, 360f), Random.Range(0f, 360f));
                system.Emit(emit, 1);
            }
        }

        public void EmitCelebration(Vector3 position)
        {
            foreach (ParticleSystem system in _confetti) Emit(system, position + Vector3.up * 0.5f, Count(14), 3f, 5.5f, 0.06f, 0.11f, 1.8f);
        }

        private int Count(int baseCount) => Mathf.Max(1, Mathf.RoundToInt(baseCount * Intensity));

        private ParticleSystem GetDebris(TerrainMaterial material)
        {
            if (_debris.TryGetValue(material, out ParticleSystem system)) return system;
            system = CreateSystem("Debris_" + material, _materials.Get(Palette.Terrain(material)), PrimitiveType.Cube, 150, 1f, 1.4f);
            _debris.Add(material, system);
            return system;
        }

        private static void Emit(ParticleSystem system, Vector3 position, int count, float minSpeed, float maxSpeed, float minSize, float maxSize, float lifetime)
        {
            var emit = new ParticleSystem.EmitParams();
            for (int i = 0; i < count; i++)
            {
                Vector3 dir = Random.insideUnitSphere;
                dir.y = Mathf.Abs(dir.y) + 0.6f;
                emit.position = position + new Vector3(Random.Range(-0.2f, 0.2f), 0.05f, Random.Range(-0.2f, 0.2f));
                emit.velocity = dir.normalized * Random.Range(minSpeed, maxSpeed);
                emit.startSize = Random.Range(minSize, maxSize);
                emit.startLifetime = lifetime * Random.Range(0.75f, 1.15f);
                emit.rotation3D = new Vector3(Random.Range(0f, 360f), Random.Range(0f, 360f), Random.Range(0f, 360f));
                system.Emit(emit, 1);
            }
        }

        private ParticleSystem CreateSystem(string name, Material material, PrimitiveType shape, int maxParticles, float sizeFalloffStart, float gravity)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = system.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = maxParticles;
            main.gravityModifier = gravity;
            main.startSpeed = 0f;
            main.startLifetime = 1f;
            main.startSize3D = false;
            main.startRotation3D = true;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;

            ParticleSystem.ShapeModule shapeModule = system.shape;
            shapeModule.enabled = false;

            ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(sizeFalloffStart, 1f), new Keyframe(1f, 0f)));

            ParticleSystem.RotationOverLifetimeModule rotation = system.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-4f, 4f);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = PrimitiveFactory.GetMesh(shape);
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            system.Play();
            return system;
        }
    }
}
