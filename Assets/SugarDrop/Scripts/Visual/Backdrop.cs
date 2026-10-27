using UnityEngine;

namespace SugarDrop
{
    /// <summary>
    /// Fundalul nivelului: cerul ținutului (degrade) și atmosfera lui animată — petale care plutesc în Câmpia de
    /// Căpșuni, frunze în Pădurea de Mentă, bule în Lacul de Afine, nisip purtat de vânt în deșert, gemuri care
    /// sclipesc la castel, picături de ciocolată, fulgi de zăpadă, caramel, nori de vată de zahăr, stele în galaxie.
    /// </summary>
    public class Backdrop : MonoBehaviour
    {
        public static Backdrop Instance { get; private set; }

        [SerializeField] private int dotCount = 26;
        [SerializeField] private Vector2 area = new(22f, 28f);

        private SpriteRenderer _gradient;
        private SpriteRenderer[] _dotRenderers;
        private Transform[] _dots;
        private float[] _speeds;
        private float[] _phases;
        private float[] _sizes;
        private AmbientMotion _motion = AmbientMotion.Rise;

        private void Awake()
        {
            Instance = this;
            var gradient = new GameObject("Gradient");
            gradient.transform.SetParent(transform, false);
            _gradient = gradient.AddComponent<SpriteRenderer>();
            _gradient.sprite = SugarSprites.BackgroundGradient;
            _gradient.sortingOrder = -100;
            Vector2 spriteSize = _gradient.sprite.bounds.size;
            gradient.transform.localScale = new Vector3(area.x / spriteSize.x, area.y / spriteSize.y, 1f);

            _dots = new Transform[dotCount];
            _dotRenderers = new SpriteRenderer[dotCount];
            _speeds = new float[dotCount];
            _phases = new float[dotCount];
            _sizes = new float[dotCount];
            for (int i = 0; i < dotCount; i++)
            {
                var dot = new GameObject("Ambient");
                dot.transform.SetParent(transform, false);
                var dotRenderer = dot.AddComponent<SpriteRenderer>();
                dotRenderer.sprite = SugarSprites.SoftDot;
                dotRenderer.sortingOrder = -99;
                dot.transform.localPosition = new Vector3(Random.Range(-0.5f, 0.5f) * area.x, Random.Range(-0.5f, 0.5f) * area.y, 0f);
                _dots[i] = dot.transform;
                _dotRenderers[i] = dotRenderer;
                _speeds[i] = Random.Range(0.15f, 0.45f);
                _phases[i] = Random.value * 10f;
            }
            ApplyTheme(0);
        }

        private void OnEnable()
        {
            SugarQuality.Changed += ApplyQuality;
            if (_dots != null) ApplyQuality();
        }

        private void OnDisable() => SugarQuality.Changed -= ApplyQuality;

        private void Start() => ApplyQuality();

        /// <summary>Pe grafică redusă plutesc mai puține particule.</summary>
        private void ApplyQuality()
        {
            int visible = Mathf.RoundToInt(_dots.Length * SugarQuality.AmbientScale);
            for (int i = 0; i < _dots.Length; i++) _dots[i].gameObject.SetActive(i < visible);
        }

        /// <summary>Cerul și atmosfera ținutului.</summary>
        public void ApplyTheme(int chapter)
        {
            int theme = ChapterTheme.IndexOf(chapter);
            _gradient.sprite = SugarSprites.Sky(chapter);
            // Degradeul se întinde pe tot ecranul; o imagine pictată (Resources/Art) acoperă ecranul păstrându-și proporțiile.
            Vector2 spriteSize = _gradient.sprite.bounds.size;
            bool painted = _gradient.sprite.texture.width > 8;
            float cover = Mathf.Max(area.x / spriteSize.x, area.y / spriteSize.y);
            _gradient.transform.localScale = painted ? new Vector3(cover, cover, 1f) : new Vector3(area.x / spriteSize.x, area.y / spriteSize.y, 1f);
            _motion = LandStyle.Motion(theme);
            Vector2 sizeRange = LandStyle.AmbientSize(theme);
            for (int i = 0; i < _dots.Length; i++)
            {
                SpriteRenderer renderer = _dotRenderers[i];
                renderer.sprite = LandStyle.Ambient(theme, i);
                renderer.color = LandStyle.AmbientColor(theme, i);
                _sizes[i] = Random.Range(sizeRange.x, sizeRange.y);
                _dots[i].localScale = Vector3.one * (_sizes[i] / renderer.sprite.bounds.size.x);
            }
        }

        private void Update()
        {
            float half = area.y * 0.5f, halfWidth = area.x * 0.5f;
            float time = Time.time, dt = Time.deltaTime;
            for (int i = 0; i < _dots.Length; i++)
            {
                Transform dot = _dots[i];
                Vector3 p = dot.localPosition;
                float wave = Mathf.Sin(time * 0.9f + _phases[i]);
                switch (_motion)
                {
                    case AmbientMotion.Rise:
                        p.y += _speeds[i] * dt;
                        p.x += wave * 0.25f * dt;
                        break;
                    case AmbientMotion.Fall:
                        p.y -= _speeds[i] * 1.3f * dt;
                        p.x += wave * 0.35f * dt;
                        break;
                    case AmbientMotion.Flutter:
                        // Petale și frunze: cad legănat, rotindu-se.
                        p.y -= _speeds[i] * 0.8f * dt;
                        p.x += wave * 0.9f * dt;
                        dot.localRotation = Quaternion.Euler(0f, 0f, wave * 50f + _phases[i] * 30f);
                        break;
                    case AmbientMotion.Drift:
                        // Vânt: spre dreapta, cu o ondulație ușoară.
                        p.x += _speeds[i] * 1.6f * dt;
                        p.y += wave * 0.2f * dt;
                        break;
                    default:
                        // Sclipire: urcă foarte încet, crește și scade.
                        p.y += _speeds[i] * 0.2f * dt;
                        float pulse = 0.6f + 0.4f * Mathf.Abs(Mathf.Sin(time * 1.8f + _phases[i]));
                        dot.localScale = Vector3.one * (_sizes[i] * pulse / _dotRenderers[i].sprite.bounds.size.x);
                        break;
                }
                // Ieșite din ecran: reapar pe partea opusă.
                if (p.y > half + 1.5f) p = new Vector3(Random.Range(-halfWidth, halfWidth), -half - 1.5f, 0f);
                else if (p.y < -half - 1.5f) p = new Vector3(Random.Range(-halfWidth, halfWidth), half + 1.5f, 0f);
                if (p.x > halfWidth + 2f) p.x = -halfWidth - 2f;
                dot.localPosition = p;
            }
        }
    }
}
