using UnityEngine;

namespace SugarDrop
{
    /// <summary>
    /// Fundalul: degrade pastelat pe tot ecranul și „bokeh” — cercuri moi, colorate, care plutesc încet în sus.
    /// </summary>
    public class Backdrop : MonoBehaviour
    {
        public static Backdrop Instance { get; private set; }

        [SerializeField] private int dotCount = 24;
        [SerializeField] private Vector2 area = new(22f, 28f);

        private SpriteRenderer _gradient;
        private SpriteRenderer[] _dotRenderers;
        private Transform[] _dots;
        private float[] _speeds;
        private float[] _phases;

        private void Awake()
        {
            Instance = this;
            var gradient = new GameObject("Gradient");
            gradient.transform.SetParent(transform, false);
            var renderer = gradient.AddComponent<SpriteRenderer>();
            _gradient = renderer;
            renderer.sprite = SugarSprites.BackgroundGradient;
            renderer.sortingOrder = -100;
            Vector2 spriteSize = renderer.sprite.bounds.size;
            gradient.transform.localScale = new Vector3(area.x / spriteSize.x, area.y / spriteSize.y, 1f);

            _dots = new Transform[dotCount];
            _dotRenderers = new SpriteRenderer[dotCount];
            _speeds = new float[dotCount];
            _phases = new float[dotCount];
            for (int i = 0; i < dotCount; i++)
            {
                var dot = new GameObject("Bokeh");
                dot.transform.SetParent(transform, false);
                var dotRenderer = dot.AddComponent<SpriteRenderer>();
                dotRenderer.sprite = SugarSprites.SoftDot;
                Color c = Color.HSVToRGB(Random.Range(0.8f, 1.15f) % 1f, 0.3f, 1f);
                c.a = Random.Range(0.25f, 0.5f);
                dotRenderer.color = c;
                dotRenderer.sortingOrder = -99;
                float size = Random.Range(0.8f, 2.6f) / dotRenderer.sprite.bounds.size.x;
                dot.transform.localScale = Vector3.one * size;
                dot.transform.localPosition = new Vector3(Random.Range(-0.5f, 0.5f) * area.x, Random.Range(-0.5f, 0.5f) * area.y, 0f);
                _dots[i] = dot.transform;
                _dotRenderers[i] = dotRenderer;
                _speeds[i] = Random.Range(0.15f, 0.45f);
                _phases[i] = Random.value * 10f;
            }
        }

        /// <summary>Cerul și luminile în culorile capitolului.</summary>
        public void ApplyTheme(int chapter)
        {
            ChapterTheme theme = ChapterTheme.For(chapter);
            _gradient.sprite = SugarSprites.Sky(chapter);
            var random = new System.Random(chapter * 131 + 7);
            foreach (SpriteRenderer dot in _dotRenderers)
            {
                float hue = Mathf.Repeat(theme.GlowHue + ((float)random.NextDouble() - 0.5f) * 0.18f, 1f);
                Color c = Color.HSVToRGB(hue, 0.3f, 1f);
                c.a = dot.color.a;
                dot.color = c;
            }
        }

        private void OnEnable()
        {
            SugarQuality.Changed += ApplyQuality;
            if (_dots != null) ApplyQuality();
        }

        private void OnDisable() => SugarQuality.Changed -= ApplyQuality;

        private void Start() => ApplyQuality();

        /// <summary>Pe grafică redusă plutesc mai puține lumini.</summary>
        private void ApplyQuality()
        {
            int visible = Mathf.RoundToInt(_dots.Length * SugarQuality.AmbientScale);
            for (int i = 0; i < _dots.Length; i++) _dots[i].gameObject.SetActive(i < visible);
        }

        private void Update()
        {
            float half = area.y * 0.5f;
            for (int i = 0; i < _dots.Length; i++)
            {
                Vector3 p = _dots[i].localPosition;
                p.y += _speeds[i] * Time.deltaTime;
                p.x += Mathf.Sin(Time.time * 0.5f + _phases[i]) * 0.1f * Time.deltaTime;
                if (p.y > half + 1.5f)
                {
                    p.y = -half - 1.5f;
                    p.x = Random.Range(-0.5f, 0.5f) * area.x;
                }
                _dots[i].localPosition = p;
            }
        }
    }
}
