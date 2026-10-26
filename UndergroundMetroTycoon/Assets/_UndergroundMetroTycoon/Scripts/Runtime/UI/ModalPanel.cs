using UnityEngine;
using UnityEngine.UI;

namespace UndergroundMetroTycoon.Game
{
    /// <summary>Shared modal frame: dimmed backdrop, panel with title and close button, pop-in animation.</summary>
    public abstract class ModalPanel : MonoBehaviour
    {
        private CanvasGroup _group;
        private RectTransform _panel;
        private float _anim;

        protected RectTransform Body { get; private set; }
        protected SfxPlayer Sfx { get; private set; }
        public bool IsOpen => gameObject.activeSelf;

        protected void BuildFrame(RectTransform root, string title, float height, SfxPlayer sfx)
        {
            Sfx = sfx;
            var rt = (RectTransform)transform;
            UIFactory.Stretch(rt);
            _group = gameObject.AddComponent<CanvasGroup>();

            Image dim = UIFactory.Image("Dim", rt, new Color(0.02f, 0.04f, 0.08f, 0.72f));
            UIFactory.Stretch(dim.rectTransform);
            dim.raycastTarget = true;
            Button dimButton = dim.gameObject.AddComponent<Button>();
            dimButton.transition = Selectable.Transition.None;
            dimButton.onClick.AddListener(Close);

            Image panel = UIFactory.Panel("Panel", rt, Palette.Navy, 1f);
            panel.raycastTarget = true;
            _panel = panel.rectTransform;
            UIFactory.Place(_panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(980f, height));
            UIFactory.AddShadow(panel, 10f, 0.5f);

            Image header = UIFactory.Panel("Header", _panel, Palette.NavyLight, 1f);
            header.rectTransform.anchorMin = new Vector2(0f, 1f);
            header.rectTransform.anchorMax = new Vector2(1f, 1f);
            header.rectTransform.pivot = new Vector2(0.5f, 1f);
            header.rectTransform.offsetMin = new Vector2(0f, -130f);
            header.rectTransform.offsetMax = Vector2.zero;

            Text titleText = UIFactory.Text("Title", header.transform, title, 52, Palette.White, TextAnchor.MiddleLeft, true);
            UIFactory.Stretch(titleText.rectTransform, 48f, 0f, 160f, 0f);
            UIFactory.AddShadow(titleText, 3f);

            Button close = UIFactory.Button("Close", header.transform, "X", Palette.Red, Palette.White, 44, Close, out _);
            UIFactory.Place((RectTransform)close.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-28f, 0f), new Vector2(96f, 96f));

            Body = UIFactory.Rect("Body", _panel);
            UIFactory.Stretch(Body, 36f, 156f, 36f, 36f);
            gameObject.SetActive(false);
        }

        public void Open()
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            _anim = 0f;
            OnOpened();
        }

        public void Close()
        {
            if (!gameObject.activeSelf) return;
            Sfx?.PlayClick();
            gameObject.SetActive(false);
        }

        protected virtual void OnOpened() { }

        protected virtual void Update()
        {
            if (_anim >= 1f) return;
            _anim = Mathf.Min(1f, _anim + Time.unscaledDeltaTime * 6f);
            float eased = 1f - Mathf.Pow(1f - _anim, 3f);
            _group.alpha = eased;
            float s = Mathf.Lerp(0.88f, 1f, eased);
            _panel.localScale = new Vector3(s, s, 1f);
        }
    }
}
