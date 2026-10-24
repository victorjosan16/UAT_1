using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// Construiește din cod șabloane de particule de tip „explozie scurtă” (burst) și le redă prin ObjectPool.
    /// Toate efectele jocului folosesc aceeași rețetă, cu alte valori — ușor de reglat și ieftin pe mobil.
    /// </summary>
    public static class ParticleFactory
    {
        public struct BurstSettings
        {
            public int Count;
            public Vector2 Speed;
            public Vector2 Size;
            public Vector2 Lifetime;
            public float Gravity;
            public float Radius;
            public ParticleSystemShapeType Shape;
            /// <summary>Dacă e setat, particulele sunt mici mesh-uri 3D (ex. bucăți de rocă) în loc de discuri.</summary>
            public Mesh Mesh;
            /// <summary>Rotește forma emisiei (ex. inel orizontal: -90 pe X).</summary>
            public Vector3 ShapeRotation;
            public bool ShrinkOverLifetime;
        }

        /// <summary>Creează un șablon inactiv; ObjectPool face copii din el.</summary>
        public static GameObject CreateBurst(string name, Transform parent, Material material, BurstSettings settings)
        {
            var go = new GameObject(name);
            go.SetActive(false);
            go.transform.SetParent(parent, false);

            var system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = system.main;
            main.duration = 0.5f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(settings.Lifetime.x, settings.Lifetime.y);
            main.startSpeed = new ParticleSystem.MinMaxCurve(settings.Speed.x, settings.Speed.y);
            main.startSize = new ParticleSystem.MinMaxCurve(settings.Size.x, settings.Size.y);
            main.gravityModifier = settings.Gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = Mathf.Max(8, settings.Count * 2);
            if (settings.Mesh != null)
            {
                main.startRotation3D = true;
                main.startRotationX = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
                main.startRotationY = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
                main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            }

            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)settings.Count) });

            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = settings.Shape;
            shape.radius = Mathf.Max(0.01f, settings.Radius);
            shape.rotation = settings.ShapeRotation;

            if (settings.ShrinkOverLifetime)
            {
                ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = system.sizeOverLifetime;
                sizeOverLifetime.enabled = true;
                sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
            }

            var particleRenderer = go.GetComponent<ParticleSystemRenderer>();
            particleRenderer.sharedMaterial = material;
            particleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            particleRenderer.receiveShadows = false;
            if (settings.Mesh != null)
            {
                particleRenderer.renderMode = ParticleSystemRenderMode.Mesh;
                particleRenderer.mesh = settings.Mesh;
            }
            else
            {
                particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            }

            float maxLifetime = settings.Lifetime.y + 0.2f;
            go.AddComponent<AutoDespawn>().Lifetime = maxLifetime;
            return go;
        }

        /// <summary>Redă o copie a șablonului în poziția dată, cu o culoare.</summary>
        public static void Play(GameObject template, Vector3 position, Color color)
        {
            if (template == null || ObjectPool.Instance == null) return;
            GameObject instance = ObjectPool.Instance.Spawn(template, position, Quaternion.identity);
            var system = instance.GetComponent<ParticleSystem>();
            ParticleSystem.MainModule main = system.main;
            main.startColor = color;
            system.Clear();
            system.Play();
        }

        /// <summary>
        /// Copie a materialului de particule cu un disc moale ca textură (particulele devin rotunde, nu pătrate).
        /// </summary>
        public static Material CreateDiscMaterial(Material baseMaterial)
        {
            if (baseMaterial == null) return null;
            var material = new Material(baseMaterial) { name = baseMaterial.name + " (disc)" };
            material.mainTexture = ProceduralSprites.Circle.texture;
            return material;
        }

        /// <summary>Distanța față de jucător (efectele departe de el sunt ignorate, ca să economisim).</summary>
        public static bool IsNearPlayer(Vector3 position, float maxDistance)
        {
            PlayerRoot player = PlayerRoot.Instance;
            return player == null || (player.Position - position).sqrMagnitude <= maxDistance * maxDistance;
        }
    }
}
