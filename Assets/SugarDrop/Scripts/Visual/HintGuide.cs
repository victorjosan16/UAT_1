using UnityEngine;

namespace SugarDrop
{
    /// <summary>
    /// Ghidul tutorialului: un inel care pulsează pe grupul de atins și un „deget” luminos care apasă ritmic,
    /// arătând atingerea.
    /// </summary>
    public class HintGuide : MonoBehaviour
    {
        private SpriteRenderer _ring, _dot;
        private Vector3 _target;
        private bool _visible;

        private void Awake()
        {
            _ring = Make("Ring", SugarSprites.Ring, 1.2f);
            _dot = Make("Dot", SugarSprites.SoftDot, 0.8f);
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

        public void Show(Vector3 target)
        {
            _target = target;
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
            // Ritm de „apăsare”: inelul se strânge când punctul apasă, apoi se lărgește și se stinge.
            float t = Mathf.Repeat(Time.time / 1.1f, 1f);
            float press = t < 0.25f ? t / 0.25f : 1f - Mathf.Clamp01((t - 0.25f) / 0.3f);
            _ring.transform.position = _target;
            _dot.transform.position = _target + new Vector3(0.18f, -0.18f, 0f) * (1f - press);
            _ring.transform.localScale = Vector3.one * (1.2f / _ring.sprite.bounds.size.x * (1.25f - press * 0.25f + Mathf.Max(0f, t - 0.55f)));
            _ring.color = new Color(1f, 1f, 1f, t < 0.55f ? 0.9f : 0.9f * (1f - (t - 0.55f) / 0.45f));
            _dot.color = new Color(1f, 1f, 0.85f, 0.6f + press * 0.4f);
        }
    }
}
