using UnityEngine;

namespace LastBlock.Effects
{
    /// <summary>
    /// Particle bursts (extraction chips, victory confetti) and sound effects.
    /// Particle systems are built in code; only the material comes from the scene setup
    /// so the shader is guaranteed to be included in builds.
    /// Optional AudioClips can be assigned to replace the procedural sounds.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class SimpleEffects : MonoBehaviour
    {
        [SerializeField] Material particleMaterial;

        [Header("Optional clip overrides (procedural sounds are used when empty)")]
        [SerializeField] AudioClip selectClip;
        [SerializeField] AudioClip extractClip;
        [SerializeField] AudioClip impactClip;
        [SerializeField] AudioClip denyClip;
        [SerializeField] AudioClip winClip;
        [SerializeField] AudioClip loseClip;
        [SerializeField, Range(0f, 1f)] float volume = 0.8f;
        [SerializeField] float minImpactInterval = 0.08f;

        AudioSource audioSource;
        ParticleSystem chipsSystem;
        ParticleSystem confettiSystem;
        float lastImpactTime = -1f;

        void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;

            if (selectClip == null) selectClip = ProceduralAudio.CreateClick();
            if (extractClip == null) extractClip = ProceduralAudio.CreatePop();
            if (impactClip == null) impactClip = ProceduralAudio.CreateThud();
            if (denyClip == null) denyClip = ProceduralAudio.CreateDeny();
            if (winClip == null) winClip = ProceduralAudio.CreateWin();
            if (loseClip == null) loseClip = ProceduralAudio.CreateLose();

            Material material = ResolveParticleMaterial();
            chipsSystem = CreateChipsSystem(material);
            confettiSystem = CreateConfettiSystem(material);
        }

        // ---------------------------------------------------------------- public API

        public void PlaySelect() => Play(selectClip, 0.5f);
        public void PlayDeny() => Play(denyClip, 0.6f);

        public void PlayExtraction(Vector3 position, Color color)
        {
            Play(extractClip, 0.8f);
            Emit(chipsSystem, position, 24, color);
        }

        /// <summary>Impact thud, louder for harder hits; rate limited so a collapse is not deafening.</summary>
        public void PlayImpact(float speed)
        {
            if (Time.time - lastImpactTime < minImpactInterval) return;
            lastImpactTime = Time.time;
            float loudness = Mathf.Clamp01(speed / 5f);
            if (audioSource != null && impactClip != null)
            {
                audioSource.PlayOneShot(impactClip, volume * Mathf.Lerp(0.15f, 1f, loudness));
            }
        }

        public void PlayVictory(Vector3 position)
        {
            Play(winClip, 1f);
            Emit(confettiSystem, position, 140, null);
        }

        public void PlayLose() => Play(loseClip, 0.9f);

        // ---------------------------------------------------------------- helpers

        void Play(AudioClip clip, float scale)
        {
            if (audioSource != null && clip != null) audioSource.PlayOneShot(clip, volume * scale);
        }

        static void Emit(ParticleSystem system, Vector3 position, int count, Color? color)
        {
            if (system == null) return;
            ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams
            {
                position = position,
                applyShapeToPosition = true
            };
            if (color.HasValue) emitParams.startColor = color.Value;
            system.Emit(emitParams, count);
        }

        Material ResolveParticleMaterial()
        {
            if (particleMaterial != null) return particleMaterial;
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            return shader != null ? new Material(shader) : null;
        }

        ParticleSystem CreateBaseSystem(string systemName, Material material, int maxParticles)
        {
            GameObject go = new GameObject(systemName);
            go.transform.SetParent(transform, false);
            ParticleSystem system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = system.main;
            main.playOnAwake = false;
            main.loop = true; // Always "playing" with zero emission so Emit() particles simulate normally.
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = maxParticles;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 0f;

            ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            if (material != null) renderer.sharedMaterial = material;
            return system;
        }

        ParticleSystem CreateChipsSystem(Material material)
        {
            ParticleSystem system = CreateBaseSystem("ExtractionChips", material, 200);
            ParticleSystem.MainModule main = system.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 2.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.09f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = 1f;

            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.12f;

            ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

            system.Play();
            return system;
        }

        ParticleSystem CreateConfettiSystem(Material material)
        {
            ParticleSystem system = CreateBaseSystem("VictoryConfetti", material, 400);
            ParticleSystem.MainModule main = system.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 3.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 7f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.12f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = 0.6f;

            Gradient rainbow = new Gradient();
            rainbow.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.35f, 0.4f), 0f),
                    new GradientColorKey(new Color(1f, 0.8f, 0.3f), 0.25f),
                    new GradientColorKey(new Color(0.4f, 0.9f, 0.5f), 0.5f),
                    new GradientColorKey(new Color(0.35f, 0.65f, 1f), 0.75f),
                    new GradientColorKey(new Color(0.8f, 0.45f, 1f), 1f)
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            main.startColor = new ParticleSystem.MinMaxGradient(rainbow) { mode = ParticleSystemGradientMode.RandomColor };

            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 30f;
            shape.radius = 0.3f;
            shape.rotation = new Vector3(-90f, 0f, 0f); // Cone points up.

            ParticleSystem.RotationOverLifetimeModule rotation = system.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-6f, 6f);

            ParticleSystem.LimitVelocityOverLifetimeModule limit = system.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.limit = 6f;
            limit.drag = 1.2f;

            system.Play();
            return system;
        }
    }
}
