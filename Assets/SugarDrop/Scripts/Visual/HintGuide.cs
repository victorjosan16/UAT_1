using UnityEngine;

namespace SugarDrop
{
    /// <summary>
    /// Ghidul tutorialului: un inel care pulsează pe piesa de mutat și un „deget” luminos care alunecă ritmic
    /// spre vecina cu care trebuie schimbată, arătând gestul.
    /// </summary>
    public class HintGuide : MonoBehaviour
    {
        private SpriteRenderer _ring, _dot;
        private Vector3 _from, _to;
        private bool _visible;

        private void Awake()
        {
            _ring = Make("Ring", SugarSprites.Ring, 1.2f);
            _dot = Make("Dot", SugarSprites.SoftDot, 0.8f);
            if (!_visible) gameObject.SetActive(false);
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

        /// <summary>Arată gestul: de pe piesa „from” spre vecina „to” (poziții din lume).</summary>
        public void Show(Vector3 from, Vector3 to)
        {
            _from = from;
            _to = to;
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
            // Ritm: degetul apasă pe piesă, alunecă spre vecină, apoi se stinge și o ia de la capăt.
            float t = Mathf.Repeat(Time.time / 1.4f, 1f);
            float slide = Mathf.Clamp01((t - 0.2f) / 0.45f);
            slide = slide * slide * (3f - 2f * slide);
            float press = t < 0.2f ? t / 0.2f : 1f;
            _ring.transform.position = _from;
            _ring.transform.localScale = Vector3.one * (1.2f / _ring.sprite.bounds.size.x * (1.2f - press * 0.15f));
            _ring.color = new Color(1f, 1f, 1f, 0.9f * (t < 0.75f ? 1f : 1f - (t - 0.75f) / 0.25f));
            _dot.transform.position = Vector3.Lerp(_from, _to, slide);
            _dot.color = new Color(1f, 1f, 0.85f, (0.6f + press * 0.4f) * (t < 0.8f ? 1f : 1f - (t - 0.8f) / 0.2f));
        }
    }
}
