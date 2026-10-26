using UnityEngine;
using UnityEngine.UI;

namespace SugarDrop
{
    /// <summary>
    /// Particule pentru interfață (imagini mici refolosite dintr-un pool fix): sclipiri care plutesc continuu
    /// în fundal și explozii scurte (succes, deblocare de nivel). Fără alocări în timpul animației;
    /// când nu e nimic de animat, Update iese imediat. Totul în timp nescalat.
    /// </summary>
    public class UIParticleController : MonoBehaviour
    {
        private struct Particle
        {
            public Vector2 Position, Velocity;
            public float Age, Life, Size, Spin, Rotation, Gravity, Phase;
            public Color Color;
            public bool Ambient;
        }

        [SerializeField] private int capacity = 48;
        [Tooltip("Particule ambientale pe secundă (0 = doar explozii).")]
        [SerializeField] private float ambientRate;

        private RectTransform _rect;
        private Image[] _images;
        private RectTransform[] _rects;
        private Particle[] _particles;
        private bool[] _alive;
        private int _aliveCount;
        private float _ambientAccumulator;
        private Sprite _ambientSprite;
        private Color[] _ambientColors;
        private Vector2 _ambientSize = new(14f, 34f);

        public float AmbientRate
        {
            get => ambientRate;
            set => ambientRate = Mathf.Max(0f, value);
        }

        /// <summary>Creează pool-ul (apelat o dată, după AddComponent).</summary>
        public void Initialize(int poolSize, Sprite ambientSprite, Color[] ambientColors, float rate)
        {
            capacity = Mathf.Max(1, poolSize);
            _ambientSprite = ambientSprite;
            _ambientColors = ambientColors;
            ambientRate = rate;
            _rect = (RectTransform)transform;
            _images = new Image[capacity];
            _rects = new RectTransform[capacity];
            _particles = new Particle[capacity];
            _alive = new bool[capacity];
            for (int i = 0; i < capacity; i++)
            {
                var go = new GameObject("P", typeof(RectTransform)) { layer = 5 };
                var rect = (RectTransform)go.transform;
                rect.SetParent(transform, false);
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                var image = go.AddComponent<Image>();
                image.raycastTarget = false;
                go.SetActive(false);
                _images[i] = image;
                _rects[i] = rect;
            }
        }

        public void SetAmbientSize(float min, float max) => _ambientSize = new Vector2(min, max);

        /// <summary>Explozie de „count” particule în punctul dat (coordonate locale ale acestui obiect).</summary>
        public void Burst(Vector2 localPosition, int count, Sprite sprite, Color[] colors, float speed = 520f, float size = 34f)
        {
            if (_particles == null) return;
            for (int i = 0; i < count; i++)
            {
                float angle = (i + Random.value * 0.6f) / count * Mathf.PI * 2f;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Spawn(new Particle
                {
                    Position = localPosition,
                    Velocity = direction * speed * Random.Range(0.55f, 1f),
                    Life = Random.Range(0.55f, 0.9f),
                    Size = size * Random.Range(0.7f, 1.2f),
                    Spin = Random.Range(-360f, 360f),
                    Rotation = Random.Range(0f, 360f),
                    Gravity = 650f,
                    Color = colors[i % colors.Length],
                }, sprite);
            }
        }

        /// <summary>Burst la poziția unui alt element de interfață.</summary>
        public void BurstAt(RectTransform target, int count, Sprite sprite, Color[] colors, float speed = 520f, float size = 34f)
        {
            if (_rect == null || target == null) return;
            Vector3 local = _rect.InverseTransformPoint(target.position);
            Burst(local, count, sprite, colors, speed, size);
        }

        private void Spawn(Particle particle, Sprite sprite)
        {
            for (int i = 0; i < capacity; i++)
            {
                if (_alive[i]) continue;
                _particles[i] = particle;
                _alive[i] = true;
                _aliveCount++;
                _images[i].sprite = sprite;
                _images[i].gameObject.SetActive(true);
                Apply(i);
                return;
            }
        }

        private void OnDisable()
        {
            // Ecranul s-a închis: oprim tot (nu animăm ce nu se vede).
            if (_alive == null) return;
            for (int i = 0; i < capacity; i++)
            {
                if (!_alive[i]) continue;
                _alive[i] = false;
                _images[i].gameObject.SetActive(false);
            }
            _aliveCount = 0;
        }

        private void Update()
        {
            if (_particles == null) return;
            float dt = Time.unscaledDeltaTime;

            if (ambientRate > 0f && _ambientSprite != null)
            {
                _ambientAccumulator += dt * ambientRate * SugarQuality.AmbientScale;
                while (_ambientAccumulator >= 1f)
                {
                    _ambientAccumulator -= 1f;
                    SpawnAmbient();
                }
            }
            if (_aliveCount == 0) return;

            for (int i = 0; i < capacity; i++)
            {
                if (!_alive[i]) continue;
                ref Particle p = ref _particles[i];
                p.Age += dt;
                if (p.Age >= p.Life)
                {
                    _alive[i] = false;
                    _aliveCount--;
                    _images[i].gameObject.SetActive(false);
                    continue;
                }
                if (p.Ambient) p.Position.x += Mathf.Sin(p.Age * 1.7f + p.Phase) * 18f * dt;
                p.Velocity.y -= p.Gravity * dt;
                p.Velocity *= 1f / (1f + 1.8f * dt);
                p.Position += p.Velocity * dt;
                p.Rotation += p.Spin * dt;
                Apply(i);
            }
        }

        private void SpawnAmbient()
        {
            Rect area = _rect.rect;
            Spawn(new Particle
            {
                Position = new Vector2(Random.Range(area.xMin, area.xMax), Random.Range(area.yMin, area.yMax)),
                Velocity = new Vector2(0f, Random.Range(18f, 45f)),
                Life = Random.Range(2.5f, 4.5f),
                Size = Random.Range(_ambientSize.x, _ambientSize.y),
                Spin = Random.Range(-40f, 40f),
                Rotation = Random.Range(0f, 360f),
                Color = _ambientColors != null && _ambientColors.Length > 0 ? _ambientColors[Random.Range(0, _ambientColors.Length)] : Color.white,
                Phase = Random.value * 6f,
                Ambient = true,
            }, _ambientSprite);
        }

        private void Apply(int i)
        {
            Particle p = _particles[i];
            float t = p.Age / p.Life;
            // Ambientale: apar și dispar lin; exploziile: pornesc mari și se micșorează.
            float alpha = p.Ambient ? Mathf.Sin(t * Mathf.PI) : 1f - t * t;
            float scale = p.Ambient ? 1f : 1f - t * 0.6f;
            RectTransform rect = _rects[i];
            rect.anchoredPosition = p.Position;
            rect.sizeDelta = new Vector2(p.Size, p.Size) * scale;
            rect.localRotation = Quaternion.Euler(0f, 0f, p.Rotation);
            Color c = p.Color;
            c.a *= alpha;
            _images[i].color = c;
        }
    }
}
