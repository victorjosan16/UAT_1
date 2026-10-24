using System;
using UnityEngine;
using UnityEngine.UI;

namespace QuarryEmpire
{
    /// <summary>
    /// Funcții ajutătoare pentru a construi interfața din cod (panouri, texte, butoane).
    /// Folosește fontul inclus în Unity, deci nu are nevoie de asset-uri externe.
    /// </summary>
    public static class UiFactory
    {
        private const int UiLayer = 5;
        private static Font _font;
        private static Sprite _whiteSprite;

        public static Font Font => _font != null ? _font : (_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

        /// <summary>Sprite alb simplu (necesar pentru imaginile de tip „Filled”, ex. bare de progres).</summary>
        public static Sprite WhiteSprite => _whiteSprite != null
            ? _whiteSprite
            : (_whiteSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f)));
        private static UiTheme Theme => UiRoot.Theme;

        public static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = UiLayer };
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        /// <summary>Ancorează un element (anchor/pivot în 0..1, poziție și mărime în pixeli de referință).</summary>
        public static RectTransform Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        public static RectTransform Stretch(RectTransform rect, float padding = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
            return rect;
        }

        public static Image CreatePanel(string name, Transform parent, Color color)
        {
            RectTransform rect = CreateRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            if (Theme != null && Theme.PanelSprite != null)
            {
                image.sprite = Theme.PanelSprite;
                image.type = Image.Type.Sliced;
            }
            image.color = color;
            return image;
        }

        public static Text CreateText(string name, Transform parent, string text, int fontSize,
            TextAnchor alignment = TextAnchor.MiddleCenter, Color? color = null, FontStyle style = FontStyle.Bold)
        {
            RectTransform rect = CreateRect(name, parent);
            var label = rect.gameObject.AddComponent<Text>();
            label.font = Font;
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.alignment = alignment;
            label.color = color ?? (Theme != null ? Theme.TextColor : Color.white);
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            label.supportRichText = true;
            var shadow = rect.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
            shadow.effectDistance = new Vector2(2f, -2f);
            return label;
        }

        public static Button CreateButton(string name, Transform parent, string text, Color color, Action onClick,
            int fontSize = 40)
        {
            Image background = CreatePanel(name, parent, color);
            var button = background.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            if (onClick != null) button.onClick.AddListener(() => onClick());
            Text label = CreateText("Label", background.transform, text, fontSize);
            Stretch((RectTransform)label.transform, 8f);
            return button;
        }

        /// <summary>O bară de progres orizontală care se umple (fillAmount 0..1).</summary>
        public static Image CreateFill(string name, Transform parent, Color color)
        {
            RectTransform rect = CreateRect(name, parent);
            var fill = rect.gameObject.AddComponent<Image>();
            fill.sprite = WhiteSprite;
            fill.color = color;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillAmount = 0f;
            fill.raycastTarget = false;
            return fill;
        }

        /// <summary>Textul de pe un buton creat cu CreateButton.</summary>
        public static Text GetButtonLabel(Button button) => button.GetComponentInChildren<Text>();
    }
}
