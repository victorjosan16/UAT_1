using System.Collections.Generic;
using UnityEngine;

namespace SugarDrop
{
    /// <summary>
    /// Efectele vizuale ale jocului, desenate din cod cu sprite-uri mici (fără ParticleSystem):
    /// fiecare aromă are explozia ei — inimioare (căpșună), bule (afină), frunze (mentă), stele (lămâie),
    /// gemuri pe orbită (strugure) — iar capacele speciale au raze, unde de șoc și curcubeie.
    /// Particulele se refolosesc (pool intern), deci nu se creează obiecte noi în timpul jocului.
    /// </summary>
    public class SugarVfx : MonoBehaviour
    {
        public static SugarVfx Instance { get; private set; }

        private const int SortingOrder = 30;

        private enum Motion { Ballistic, Orbit, Bubble, Flutter, Static }

        private class Particle
        {
            public Transform Transform;
            public SpriteRenderer Renderer;
            public bool Active;
            public Motion Motion;
            public Vector3 Position, Velocity, Center;
            public float Gravity, Drag, Delay, Age, Life;
            public float Rotation, Spin;
            public Vector2 SizeFrom, SizeTo;
            public Color Color;
            public float FadeIn;
            public float OrbitRadius, OrbitAngle, OrbitSpeed;
            public float Phase;
        }

        [SerializeField] private int capacity = 500;
        [SerializeField] private Camera shakeCamera;

        private readonly List<Particle> _particles = new();
        private int _cursor;
        private Vector3 _cameraBase;
        private float _shake;

        private void Awake()
        {
            Instance = this;
            for (int i = 0; i < capacity; i++)
            {
                var go = new GameObject("P");
                go.transform.SetParent(transform, false);
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sortingOrder = SortingOrder;
                go.SetActive(false);
                _particles.Add(new Particle { Transform = go.transform, Renderer = renderer });
            }
            if (shakeCamera == null) shakeCamera = Camera.main;
            if (shakeCamera != null) _cameraBase = shakeCamera.transform.position;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ------------------------------------------------------------ Efecte per aromă

        /// <summary>Un capac a dispărut: efectul depinde de aromă (culoare).</summary>
        public void TilePopped(int color, Vector3 position)
        {
            Color main = Palette.Tile(color);
            Color light = Palette.TileLight(color);
            Flash(position, light, 1.1f, 0.18f);

            switch (color)
            {
                case 0: // Căpșună: inimioare care țâșnesc și plutesc în sus.
                    for (int i = 0; i < 6; i++)
                    {
                        Vector2 dir = Random.insideUnitCircle.normalized;
                        Particle p = Emit(SugarSprites.Heart, position, Random.Range(0.55f, 0.8f), i % 2 == 0 ? main : light);
                        p.Velocity = (Vector3)(dir * Random.Range(1.6f, 2.8f)) + Vector3.up * 1.2f;
                        p.Drag = 3f;
                        p.Gravity = -1.2f; // plutesc
                        Size(p, 0.18f, 0.32f, 0f);
                        p.Spin = Random.Range(-60f, 60f);
                    }
                    break;

                case 1: // Afină: bule care urcă legănându-se și se sparg.
                    for (int i = 0; i < 5; i++)
                    {
                        Particle p = Emit(SugarSprites.Ring, position + (Vector3)(Random.insideUnitCircle * 0.25f), Random.Range(0.7f, 1.1f), light);
                        p.Motion = Motion.Bubble;
                        p.Velocity = new Vector3(0f, Random.Range(1.2f, 2.2f), 0f);
                        p.Phase = Random.value * 6f;
                        float s = Random.Range(0.12f, 0.26f);
                        Size(p, s * 0.5f, s, s * 1.4f);
                    }
                    for (int i = 0; i < 6; i++)
                    {
                        Particle p = Emit(SugarSprites.Circle, position, 0.35f, main);
                        p.Velocity = Random.insideUnitCircle.normalized * Random.Range(1.5f, 2.5f);
                        p.Drag = 5f;
                        Size(p, 0.09f, 0.09f, 0f);
                    }
                    break;

                case 2: // Mentă: frunzulițe care se învârt și cad legănat.
                    for (int i = 0; i < 6; i++)
                    {
                        Particle p = Emit(SugarSprites.Leaf, position, Random.Range(0.8f, 1.2f), i % 2 == 0 ? main : light);
                        p.Motion = Motion.Flutter;
                        p.Velocity = new Vector3(Random.Range(-2.2f, 2.2f), Random.Range(1.5f, 3f), 0f);
                        p.Drag = 2.5f;
                        p.Gravity = 2.5f;
                        p.Spin = Random.Range(180f, 420f) * (Random.value < 0.5f ? -1f : 1f);
                        p.Phase = Random.value * 6f;
                        Size(p, 0.22f, 0.28f, 0.1f);
                    }
                    break;

                case 3: // Lămâie: explozie de steluțe rapide + un inel strălucitor.
                    Ring(position, light, 1.1f, 0.28f);
                    for (int i = 0; i < 9; i++)
                    {
                        float angle = i / 9f * Mathf.PI * 2f + Random.Range(-0.2f, 0.2f);
                        Particle p = Emit(SugarSprites.Star, position, Random.Range(0.4f, 0.6f), i % 3 == 0 ? Color.white : main);
                        p.Velocity = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * Random.Range(3.5f, 5.5f);
                        p.Drag = 6f;
                        p.Spin = Random.Range(-360f, 360f);
                        Size(p, 0.26f, 0.2f, 0f);
                    }
                    break;

                default: // Strugure: gemuri care se rotesc pe orbită, apoi se strâng într-o sclipire.
                    for (int i = 0; i < 5; i++)
                    {
                        Particle p = Emit(SugarSprites.Gem, position, 0.6f, i % 2 == 0 ? main : light);
                        p.Motion = Motion.Orbit;
                        p.Center = position;
                        p.OrbitAngle = i / 5f * 360f;
                        p.OrbitRadius = 0.55f;
                        p.OrbitSpeed = 520f;
                        Size(p, 0.16f, 0.24f, 0.05f);
                    }
                    Particle spark = Emit(SugarSprites.Sparkle, position, 0.35f, Color.white);
                    spark.Delay = 0.45f;
                    Size(spark, 0.2f, 0.9f, 0f);
                    spark.Spin = 180f;
                    break;
            }
        }

        // ------------------------------------------------------------ Capace speciale

        /// <summary>Capac dungat: o rază pe tot rândul/coloana, cu sclipiri de-a lungul ei.</summary>
        public void StripeBlast(Vector3 position, bool horizontal, float length, int color)
        {
            Color c = color >= 0 ? Palette.TileLight(color) : Color.white;
            float angle = horizontal ? 0f : 90f;
            Particle beam = Emit(SugarSprites.Beam, position, 0.4f, c);
            beam.Motion = Motion.Static;
            beam.Rotation = angle;
            beam.SizeFrom = new Vector2(length * 0.2f, 0.9f);
            beam.SizeTo = new Vector2(length * 1.1f, 0.15f);
            Particle core = Emit(SugarSprites.Beam, position, 0.3f, Color.white);
            core.Motion = Motion.Static;
            core.Rotation = angle;
            core.SizeFrom = new Vector2(length * 0.3f, 0.35f);
            core.SizeTo = new Vector2(length * 1.1f, 0.05f);

            Vector3 axis = horizontal ? Vector3.right : Vector3.up;
            for (int i = 0; i < 14; i++)
            {
                float t = Random.Range(-0.5f, 0.5f);
                Particle p = Emit(SugarSprites.Sparkle, position + axis * (t * length), Random.Range(0.3f, 0.5f), i % 2 == 0 ? Color.white : c);
                p.Velocity = Vector3.Cross(axis, Vector3.forward) * Random.Range(-1.5f, 1.5f);
                p.Delay = Mathf.Abs(t) * 0.15f;
                p.Spin = 200f;
                Size(p, 0.3f, 0.12f, 0f);
            }
            Shake(0.12f);
        }

        /// <summary>Capac ambalat (și bomba): undă de șoc, confetti de zahăr și camera tremură.</summary>
        public void WrapBlast(Vector3 position, int color)
        {
            Color c = color >= 0 ? Palette.Tile(color) : new Color(1f, 0.6f, 0.3f);
            Ring(position, Color.white, 3.4f, 0.4f);
            Ring(position, c, 2.4f, 0.5f).Delay = 0.06f;
            Flash(position, Color.white, 2.6f, 0.2f);
            for (int i = 0; i < 18; i++)
            {
                Particle p = Emit(SugarSprites.Crumb, position, Random.Range(0.6f, 1f), Color.HSVToRGB(Random.value, 0.45f, 1f));
                p.Velocity = Random.insideUnitCircle.normalized * Random.Range(3f, 6f);
                p.Drag = 2f;
                p.Gravity = 6f;
                p.Spin = Random.Range(-600f, 600f);
                Size(p, 0.16f, 0.16f, 0.1f);
            }
            Shake(0.3f);
        }

        /// <summary>Curcubeu: câte o rază colorată spre fiecare capac lovit, pe rând, cu sclipiri la capete.</summary>
        public void RainbowBlast(Vector3 origin, List<Vector3> targets)
        {
            Ring(origin, Color.white, 2.2f, 0.45f);
            for (int i = 0; i < targets.Count; i++)
            {
                Vector3 target = targets[i];
                Vector3 delta = target - origin;
                float delay = i * 0.025f;
                Color c = Color.HSVToRGB(Mathf.Repeat(i * 0.13f, 1f), 0.55f, 1f);
                Particle beam = Emit(SugarSprites.Beam, (origin + target) * 0.5f, 0.35f, c);
                beam.Motion = Motion.Static;
                beam.Rotation = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
                beam.SizeFrom = new Vector2(delta.magnitude, 0.3f);
                beam.SizeTo = new Vector2(delta.magnitude, 0.02f);
                beam.Delay = delay;
                Particle spark = Emit(SugarSprites.Sparkle, target, 0.4f, Color.white);
                spark.Delay = delay + 0.05f;
                spark.Spin = 240f;
                Size(spark, 0.5f, 0.6f, 0f);
            }
            Shake(0.18f);
        }

        /// <summary>Un capac special nou: o coroniță de sclipiri care se strâng spre el.</summary>
        public void SpecialCreated(Vector3 position)
        {
            for (int i = 0; i < 8; i++)
            {
                float angle = i / 8f * Mathf.PI * 2f;
                Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * 0.9f;
                Particle p = Emit(SugarSprites.Sparkle, position + offset, 0.35f, Color.white);
                p.Velocity = -offset * 2.4f;
                p.Spin = 300f;
                Size(p, 0.3f, 0.1f, 0f);
            }
            Ring(position, Color.white, 1.4f, 0.3f);
        }

        // ------------------------------------------------------------ Obstacole, borcan

        public void IceShatter(Vector3 position)
        {
            Flash(position, new Color(0.85f, 0.95f, 1f), 1.2f, 0.15f);
            for (int i = 0; i < 9; i++)
            {
                Particle p = Emit(SugarSprites.Shard, position + (Vector3)(Random.insideUnitCircle * 0.3f), Random.Range(0.5f, 0.8f),
                    new Color(0.8f, 0.93f, 1f, 0.95f));
                p.Velocity = new Vector3(Random.Range(-2.5f, 2.5f), Random.Range(1f, 3.5f), 0f);
                p.Gravity = 9f;
                p.Spin = Random.Range(-500f, 500f);
                Size(p, 0.2f, 0.2f, 0.12f);
            }
        }

        public void BiscuitCrumbs(Vector3 position, bool broken)
        {
            int count = broken ? 14 : 6;
            for (int i = 0; i < count; i++)
            {
                Color c = Random.value < 0.3f ? new Color(0.42f, 0.25f, 0.15f) : new Color(0.86f, 0.62f, 0.36f);
                Particle p = Emit(SugarSprites.Crumb, position + (Vector3)(Random.insideUnitCircle * 0.3f), Random.Range(0.6f, 1f), c);
                p.Velocity = new Vector3(Random.Range(-2f, 2f), Random.Range(0.5f, 3f), 0f);
                p.Gravity = 10f;
                p.Spin = Random.Range(-400f, 400f);
                float s = Random.Range(0.1f, broken ? 0.24f : 0.16f);
                Size(p, s, s, s * 0.7f);
            }
            if (broken) Shake(0.08f);
        }

        /// <summary>Sclipire mică la gura borcanului când intră o bomboană.</summary>
        public void JarSparkle(Vector3 position, Color color)
        {
            Particle p = Emit(SugarSprites.Sparkle, position, 0.4f, Color.Lerp(color, Color.white, 0.5f));
            p.Velocity = new Vector3(Random.Range(-0.4f, 0.4f), 0.8f, 0f);
            p.Spin = 220f;
            Size(p, 0.1f, 0.32f, 0f);
        }

        /// <summary>Ploaie de confetti pe tot ecranul (victorie).</summary>
        public void Confetti(Vector3 center, float width, float top)
        {
            for (int i = 0; i < 70; i++)
            {
                Sprite sprite = (i % 4) switch { 0 => SugarSprites.Heart, 1 => SugarSprites.Star, 2 => SugarSprites.Crumb, _ => SugarSprites.Circle };
                Vector3 position = new(center.x + Random.Range(-0.5f, 0.5f) * width, top + Random.Range(0f, 3f), 0f);
                Particle p = Emit(sprite, position, Random.Range(2.2f, 3.4f), Color.HSVToRGB(Random.value, 0.5f, 1f));
                p.Motion = Motion.Flutter;
                p.Velocity = new Vector3(Random.Range(-1f, 1f), Random.Range(-2f, -0.5f), 0f);
                p.Gravity = 2f;
                p.Drag = 0.6f;
                p.Spin = Random.Range(-300f, 300f);
                p.Phase = Random.value * 6f;
                Size(p, 0.22f, 0.22f, 0.18f);
            }
        }

        /// <summary>Tremurul camerei (explozii); se suprapune, dar are o limită.</summary>
        public void Shake(float amount) => _shake = Mathf.Min(0.45f, _shake + amount);

        // ------------------------------------------------------------ Motor

        private Particle Flash(Vector3 position, Color color, float size, float life)
        {
            Particle p = Emit(SugarSprites.SoftDot, position, life, color);
            Size(p, size * 0.6f, size, size * 1.2f);
            return p;
        }

        private Particle Ring(Vector3 position, Color color, float size, float life)
        {
            Particle p = Emit(SugarSprites.Ring, position, life, color);
            Size(p, size * 0.2f, size, size);
            return p;
        }

        /// <summary>Mărimea în timp: start → mijloc → final (unități din lume).</summary>
        private static void Size(Particle p, float start, float middle, float end)
        {
            p.SizeFrom = new Vector2(start, middle);
            p.SizeTo = new Vector2(end, -1f); // y < 0 = curbă în trei puncte (vezi Update)
        }

        private Particle Emit(Sprite sprite, Vector3 position, float life, Color color)
        {
            // Caută o particulă liberă; dacă toate sunt ocupate, o refolosește pe cea mai veche.
            Particle p = null;
            for (int i = 0; i < _particles.Count; i++)
            {
                Particle candidate = _particles[(_cursor + i) % _particles.Count];
                if (candidate.Active) continue;
                p = candidate;
                _cursor = (_cursor + i + 1) % _particles.Count;
                break;
            }
            if (p == null)
            {
                p = _particles[_cursor];
                _cursor = (_cursor + 1) % _particles.Count;
            }

            p.Active = true;
            p.Motion = Motion.Ballistic;
            p.Position = position;
            p.Velocity = Vector3.zero;
            p.Center = position;
            p.Gravity = 0f;
            p.Drag = 0f;
            p.Delay = 0f;
            p.Age = 0f;
            p.Life = Mathf.Max(0.05f, life);
            p.Rotation = Random.Range(0f, 360f);
            p.Spin = 0f;
            p.Color = color;
            p.FadeIn = 0.06f;
            p.Phase = 0f;
            Size(p, 0.2f, 0.2f, 0f);
            p.Renderer.sprite = sprite;
            p.Renderer.color = new Color(color.r, color.g, color.b, 0f);
            p.Transform.position = position;
            p.Transform.gameObject.SetActive(true);
            return p;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < _particles.Count; i++)
            {
                Particle p = _particles[i];
                if (!p.Active) continue;
                if (p.Delay > 0f)
                {
                    p.Delay -= dt;
                    p.Renderer.enabled = false;
                    continue;
                }
                p.Renderer.enabled = true;
                p.Age += dt;
                float t = p.Age / p.Life;
                if (t >= 1f)
                {
                    p.Active = false;
                    p.Transform.gameObject.SetActive(false);
                    continue;
                }

                switch (p.Motion)
                {
                    case Motion.Orbit:
                        p.OrbitAngle += p.OrbitSpeed * dt;
                        float radius = p.OrbitRadius * (1f - t * t);
                        float a = p.OrbitAngle * Mathf.Deg2Rad;
                        p.Position = p.Center + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * radius;
                        p.Rotation = p.OrbitAngle;
                        break;
                    case Motion.Bubble:
                        p.Position += p.Velocity * dt;
                        p.Position.x = p.Center.x + Mathf.Sin(p.Age * 7f + p.Phase) * 0.15f;
                        break;
                    case Motion.Static:
                        break;
                    default:
                        p.Velocity.y -= p.Gravity * dt;
                        p.Velocity *= 1f / (1f + p.Drag * dt);
                        p.Position += p.Velocity * dt;
                        if (p.Motion == Motion.Flutter) p.Position.x += Mathf.Sin(p.Age * 6f + p.Phase) * 0.6f * dt;
                        break;
                }
                p.Rotation += p.Spin * dt;

                Vector2 size;
                if (p.SizeTo.y < 0f)
                {
                    // Curbă în trei puncte: crește repede până la „middle”, apoi se îndreaptă spre „end”.
                    float start = p.SizeFrom.x, middle = p.SizeFrom.y, end = p.SizeTo.x;
                    float s = t < 0.25f ? Mathf.Lerp(start, middle, EaseOut(t / 0.25f)) : Mathf.Lerp(middle, end, (t - 0.25f) / 0.75f);
                    size = new Vector2(s, s);
                }
                else size = Vector2.Lerp(p.SizeFrom, p.SizeTo, EaseOut(t));

                float alpha = Mathf.Clamp01(p.Age / p.FadeIn) * (1f - t * t);
                Vector2 spriteSize = p.Renderer.sprite != null ? (Vector2)p.Renderer.sprite.bounds.size : Vector2.one;
                p.Transform.position = p.Position;
                p.Transform.rotation = Quaternion.Euler(0f, 0f, p.Rotation);
                float flutter = p.Motion == Motion.Flutter ? Mathf.Abs(Mathf.Cos(p.Age * 8f + p.Phase)) * 0.8f + 0.2f : 1f;
                p.Transform.localScale = new Vector3(size.x / spriteSize.x * flutter, size.y / spriteSize.y, 1f);
                Color c = p.Color;
                c.a *= alpha;
                p.Renderer.color = c;
            }

            if (shakeCamera != null)
            {
                if (_shake > 0.001f)
                {
                    _shake = Mathf.MoveTowards(_shake, 0f, dt * 1.2f);
                    Vector2 offset = Random.insideUnitCircle * _shake * 0.35f;
                    shakeCamera.transform.position = _cameraBase + (Vector3)offset;
                }
                else shakeCamera.transform.position = _cameraBase;
            }
        }

        private static float EaseOut(float t) => 1f - (1f - t) * (1f - t);
    }
}
