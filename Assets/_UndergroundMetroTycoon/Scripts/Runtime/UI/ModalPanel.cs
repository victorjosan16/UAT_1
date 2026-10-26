using UnityEngine;
using UnityEngine.UI;

namespace UndergroundMetroTycoon.Game
{
    /// <summary>
    /// Shared modal window: dimmed scrim, light card body, navy header with title and a round close button,
    /// and a short slide-up/fade entrance. Subclasses fill <see cref="Body"/>.
    /// </summary>
    public abstract class ModalPanel : MonoBehaviour
    {
        private CanvasGroup _group;
        private RectTransform _panel;
        private float _anim = 1f;

        protected RectTransform Body { get; private set; }
        protected SfxPlayer Sfx { get; private set; }
        public bool IsOpen => gameObject.activeSelf;

        protected void BuildFrame(RectTransform root, string title, float height, SfxPlayer sfx, float width = 1000f)
        {
            Sfx = sfx;
            var rt = (RectTransform)transform;
            UIFactory.Stretch(rt);
            _group = gameObject.AddComponent<CanvasGroup>();

            Image scrim = UIFactory.Image("Scrim", rt, UITheme.Scrim);
            UIFactory.Stretch(scrim.rectTransform, -400f, -400f, -400f, -400f);
            scrim.raycastTarget = true;
            Button scrimButton = scrim.gameObject.AddComponent<Button>();
            scrimButton.transition = Selectable.Transition.None;
            scrimButton.onClick.AddListener(Close);

            Image panel = UIFactory.Panel("Window", rt, UITheme.CardPale, 1.1f);
            panel.raycastTarget = true;
            _panel = panel.rectTransform;
            UIFactory.Place(_panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(width, height));
            UIFactory.AddShadow(panel, 14f, 0.45f);

            Image header = UIFactory.Panel("Header", _panel, UITheme.Navy, 1.1f);
            header.rectTransform.anchorMin = new Vector2(0f, 1f);
            header.rectTransform.anchorMax = new Vector2(1f, 1f);
            header.rectTransform.pivot = new Vector2(0.5f, 1f);
            header.rectTransform.offsetMin = new Vector2(0f, -128f);
            header.rectTransform.offsetMax = Vector2.zero;
            // Square off the header's lower corners where it meets the card.
            Image headerFoot = UIFactory.Image("HeaderFoot", header.transform, UITheme.Navy);
            headerFoot.rectTransform.anchorMin = Vector2.zero;
            headerFoot.rectTransform.anchorMax = new Vector2(1f, 0f);
            headerFoot.rectTransform.offsetMin = Vector2.zero;
            headerFoot.rectTransform.offsetMax = new Vector2(0f, 40f);

            Text titleText = UIFactory.Text("Title", header.transform, title, UITheme.SizeHeadline, UITheme.TextOnDark, TextAnchor.MiddleLeft, true);
            UIFactory.Stretch(titleText.rectTransform, 44f, 0f, 150f, 0f);
            UIFactory.AddShadow(titleText, 3f, 0.5f);

            UIButton close = UIButton.Create(header.transform, null, UIButtonStyle.Danger, Close, UIIcons.Close, round: true);
            UIFactory.Place(close.Rect, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-24f, 2f), new Vector2(92f, 96f));

            Body = UIFactory.Rect("Body", _panel);
            UIFactory.Stretch(Body, UITheme.SpaceL, 128f + UITheme.SpaceL, UITheme.SpaceL, UITheme.SpaceL);
            gameObject.SetActive(false);
        }

        public void Open()
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            _anim = 0f;
            Apply(0f);
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
            _anim = Mathf.Min(1f, _anim + Time.unscaledDeltaTime / UITheme.MotionNormal);
            Apply(_anim);
        }

        private void Apply(float t)
        {
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            _group.alpha = eased;
            float s = Mathf.Lerp(0.94f, 1f, eased);
            _panel.localScale = new Vector3(s, s, 1f);
            _panel.anchoredPosition = new Vector2(0f, Mathf.Lerp(-60f, 0f, eased));
        }
    }
}
