using UnityEngine;

namespace SugarDrop
{
    /// <summary>
    /// Ghidul tutorialului: două inele care pulsează pe capacele de schimbat și un punct luminos
    /// care „alunecă” de la unul la altul, arătând glisarea.
    /// </summary>
    public class HintGuide : MonoBehaviour
    {
        private SpriteRenderer _ringA, _ringB, _dot;
        private Vector3 _a, _b;
        private bool _visible;

        private void Awake()
        {
            _ringA = Make("RingA", SugarSprites.Ring, 1.1f);
            _ringB = Make("RingB", SugarSprites.Ring, 1.1f);
            _dot = Make("Dot", SugarSprites.SoftDot, 0.7f);
            Hide();
        }

        private SpriteRenderer Make(string name, Sprite sprite, float size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 25;
            go.transform.localScale = Vector3.one * (size / sprite.bounds.size.x);
            return renderer;
        }

        public void Show(Vector3 a, Vector3 b)
        {
            _a = a;
            _b = b;
            _visible = true;
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            _visible = false;
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!_visible) return;
            float time = Time.time;
            float pulse = 1f + Mathf.Sin(time * 6f) * 0.12f;
            _ringA.transform.position = _a;
            _ringB.transform.position = _b;
            float ringScale = 1.1f / _ringA.sprite.bounds.size.x * pulse;
            _ringA.transform.localScale = _ringB.transform.localScale = Vector3.one * ringScale;
            _ringA.color = _ringB.color = new Color(1f, 1f, 1f, 0.75f + Mathf.Sin(time * 6f) * 0.25f);

            // Punctul pleacă de pe primul capac, ajunge pe al doilea, apoi se stinge și o ia de la capăt.
            float t = Mathf.Repeat(time / 1.2f, 1f);
            float move = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.15f) / 0.55f));
            _dot.transform.position = Vector3.Lerp(_a, _b, move);
            float alpha = t < 0.1f ? t / 0.1f : t > 0.8f ? 1f - (t - 0.8f) / 0.2f : 1f;
            _dot.color = new Color(1f, 1f, 0.85f, alpha);
        }
    }
}
