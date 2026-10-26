using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UndergroundMetroTycoon.Game
{
    public enum UIButtonStyle
    {
        Primary,   // green: confirm / accept / claim
        Secondary, // blue: navigation
        Gold,      // rewards
        Danger,    // destructive
        Navy,      // HUD tiles and round icon buttons
        Light      // white card buttons
    }

    /// <summary>
    /// Reusable "tactile" button: a darker edge plate under a coloured face with a soft highlight. The face sinks
    /// on press and springs back, so every button gives clear feedback. Wraps a UGUI Button for clicks.
    /// </summary>
    public sealed class UIButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private RectTransform _face;
        private Image _faceImage;
        private Image _edgeImage;
        private Image _shine;
        private Text _label;
        private Image _icon;
        private Button _button;
        private UIButtonStyle _style;
        private float _pressed;
        private bool _held;

        public Text Label => _label;
        public Image Icon => _icon;
        public RectTransform Rect => (RectTransform)transform;
        public Button Button => _button;
        public bool Interactable => _button.interactable;

        public static UIButton Create(Transform parent, string label, UIButtonStyle style, Action onClick, Sprite icon = null,
            int fontSize = UITheme.SizeLabel, bool round = false)
        {
            RectTransform root = UIFactory.Rect("Button_" + (string.IsNullOrEmpty(label) ? "Icon" : label), parent);
            UIButton button = root.gameObject.AddComponent<UIButton>();
            button.Build(label, style, onClick, icon, fontSize, round);
            return button;
        }

        private void Build(string label, UIButtonStyle style, Action onClick, Sprite icon, int fontSize, bool round)
        {
            Sprite shape = round ? UISprites.Circle : UISprites.Rounded;
            float corner = UITheme.CornerButton;

            _edgeImage = UIFactory.Image("Edge", transform, Color.black, shape, corner);
            UIFactory.Stretch(_edgeImage.rectTransform);
            _edgeImage.raycastTarget = true;

            _faceImage = UIFactory.Image("Face", transform, Color.white, shape, corner);
            _face = _faceImage.rectTransform;
            UIFactory.Stretch(_face, 0f, 0f, 0f, UITheme.ButtonEdge);
            _faceImage.raycastTarget = true;

            _shine = UIFactory.Image("Shine", _face, new Color(1f, 1f, 1f, 0.18f), shape, corner);
            _shine.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            _shine.rectTransform.anchorMax = Vector2.one;
            _shine.rectTransform.offsetMin = new Vector2(6f, 0f);
            _shine.rectTransform.offsetMax = new Vector2(-6f, -5f);
            _shine.gameObject.SetActive(!round);

            float textLeft = 16f;
            if (icon != null)
            {
                _icon = UIFactory.Image("Icon", _face, Color.white, icon);
                _icon.preserveAspect = true;
                if (string.IsNullOrEmpty(label))
                {
                    UIFactory.Stretch(_icon.rectTransform, 14f, 14f, 14f, 14f);
                }
                else
                {
                    _icon.rectTransform.anchorMin = new Vector2(0f, 0.5f);
                    _icon.rectTransform.anchorMax = new Vector2(0f, 0.5f);
                    _icon.rectTransform.pivot = new Vector2(0f, 0.5f);
                    _icon.rectTransform.anchoredPosition = new Vector2(18f, 0f);
                    _icon.rectTransform.sizeDelta = new Vector2(fontSize * 1.4f, fontSize * 1.4f);
                    textLeft = 18f + fontSize * 1.4f + 6f;
                }
            }

            _label = UIFactory.Text("Label", _face, label ?? string.Empty, fontSize, Color.white, TextAnchor.MiddleCenter, true);
            UIFactory.Stretch(_label.rectTransform, textLeft, 2f, 16f, 2f);
            _label.gameObject.SetActive(!string.IsNullOrEmpty(label));

            _button = gameObject.AddComponent<Button>();
            _button.transition = Selectable.Transition.None;
            _button.targetGraphic = _faceImage;
            if (onClick != null) _button.onClick.AddListener(() => onClick());
            SetStyle(style);
        }

        public void SetLabel(string text)
        {
            _label.text = text;
            _label.gameObject.SetActive(!string.IsNullOrEmpty(text));
        }

        public void SetInteractable(bool interactable)
        {
            _button.interactable = interactable;
            ApplyColors();
        }

        public void SetStyle(UIButtonStyle style)
        {
            _style = style;
            ApplyColors();
        }

        private void ApplyColors()
        {
            Color face, edge, text;
            switch (_style)
            {
                case UIButtonStyle.Primary: face = UITheme.Green; edge = UITheme.GreenDark; text = Color.white; break;
                case UIButtonStyle.Secondary: face = UITheme.Blue; edge = UITheme.BlueDark; text = Color.white; break;
                case UIButtonStyle.Gold: face = UITheme.Gold; edge = UITheme.GoldDark; text = UITheme.TextDark; break;
                case UIButtonStyle.Danger: face = UITheme.Red; edge = UITheme.RedDark; text = Color.white; break;
                case UIButtonStyle.Light: face = UITheme.CardWhite; edge = UITheme.CardLocked; text = UITheme.TextDark; break;
                default: face = UITheme.NavyRaised; edge = UITheme.NavyDeep; text = Color.white; break;
            }
            if (_button != null && !_button.interactable)
            {
                face = Palette.Hex(0xB7C3D3);
                edge = Palette.Hex(0x8C99AB);
                text = Palette.Hex(0x5D6B80);
            }
            _faceImage.color = face;
            _edgeImage.color = edge;
            _label.color = text;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_button.interactable) _held = true;
        }

        public void OnPointerUp(PointerEventData eventData) => _held = false;
        public void OnPointerExit(PointerEventData eventData) => _held = false;

        private void OnDisable()
        {
            _held = false;
            _pressed = 0f;
            if (_face != null) _face.anchoredPosition = Vector2.zero;
            transform.localScale = Vector3.one;
        }

        private void Update()
        {
            float target = _held ? 1f : 0f;
            if (Mathf.Approximately(_pressed, target)) return;
            _pressed = Mathf.MoveTowards(_pressed, target, Time.unscaledDeltaTime / UITheme.MotionFast * 1.5f);
            float sink = _pressed * (UITheme.ButtonEdge - 2f);
            _face.offsetMin = new Vector2(0f, UITheme.ButtonEdge - sink);
            _face.offsetMax = new Vector2(0f, -sink);
            float s = 1f - 0.03f * _pressed;
            transform.localScale = new Vector3(s, s, 1f);
        }
    }
}
