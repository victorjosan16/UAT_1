using System;
using UnityEngine;
using UnityEngine.UI;

namespace QuarryEmpire
{
    /// <summary>
    /// Funcții ajutătoare pentru a construi interfața din cod, în stilul jocului:
    /// panouri rotunjite, butoane „3D” (cu umbră dedesubt), iconițe și bare de progres.
    /// </summary>
    public static class UiFactory
    {
        private const int UiLayer = 5;
        private static Font _builtinFont;
        private static Sprite _whiteSprite;

        public static Font BuiltinFont => _builtinFont != null
            ? _builtinFont
            : (_builtinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

        /// <summary>Sprite alb simplu (pentru imaginile de tip „Filled”).</summary>
        public static Sprite WhiteSprite => _whiteSprite != null
            ? _whiteSprite
            : (_whiteSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f)));

        private static UiTheme Theme => UiRoot.Theme;

        public static Color Darken(Color color, float amount = 0.3f) =>
            new(color.r * (1f - amount), color.g * (1f - amount), color.b * (1f - amount), color.a);

        // ------------------------------------------------------------ Așezare

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

        // ------------------------------------------------------------ Elemente

        /// <summary>Panou cu colțuri rotunjite (9-slice).</summary>
        public static Image CreatePanel(string name, Transform parent, Color color)
        {
            Image image = CreateImage(name, parent, Theme != null ? Theme.Panel : ProceduralSprites.RoundedRect, color);
            image.type = Image.Type.Sliced;
            image.preserveAspect = false;
            return image;
        }

        public static Image CreateImage(string name, Transform parent, Sprite sprite, Color color)
        {
            RectTransform rect = CreateRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        public static UiLabel CreateText(string name, Transform parent, string text, float fontSize,
            TextAnchor alignment = TextAnchor.MiddleCenter, Color? color = null)
        {
            Color resolved = color ?? (Theme != null ? Theme.TextColor : Color.white);
            return UiLabel.Create(name, parent, text, fontSize, alignment, resolved);
        }

        /// <summary>Buton cu aspect „3D”: o umbră mai închisă dedesubt, fața colorată deasupra și text.</summary>
        public static Button CreateButton(string name, Transform parent, string text, Color color, Action onClick,
            float fontSize = 40f)
        {
            RectTransform root = CreateRect(name, parent);
            Image shadow = CreatePanel("Shadow", root, Darken(color, 0.35f));
            Stretch((RectTransform)shadow.transform);
            ((RectTransform)shadow.transform).offsetMax = new Vector2(0f, -10f);

            Image face = CreatePanel("Face", root, color);
            Stretch((RectTransform)face.transform);
            ((RectTransform)face.transform).offsetMin = new Vector2(0f, 10f);
            face.raycastTarget = true;

            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = face;
            if (onClick != null) button.onClick.AddListener(() => onClick());
            root.gameObject.AddComponent<PressScale>();

            UiLabel label = CreateText("Label", face.transform, text, fontSize);
            Stretch(label.RectTransform, 10f);
            return button;
        }

        /// <summary>Buton rotund cu iconiță (pauză, reclame).</summary>
        public static Button CreateIconButton(string name, Transform parent, Sprite icon, Color color, Action onClick,
            float iconScale = 0.5f)
        {
            RectTransform root = CreateRect(name, parent);
            Sprite circle = Theme != null ? Theme.Circle : ProceduralSprites.Circle;
            Image shadow = CreateImage("Shadow", root, circle, Darken(color, 0.35f));
            Stretch((RectTransform)shadow.transform);
            ((RectTransform)shadow.transform).anchoredPosition = new Vector2(0f, -8f);

            Image face = CreateImage("Face", root, circle, color);
            Stretch((RectTransform)face.transform);
            face.raycastTarget = true;

            Image glyph = CreateImage("Icon", face.transform, icon, Color.white);
            var glyphRect = (RectTransform)glyph.transform;
            glyphRect.anchorMin = new Vector2(0.5f - iconScale * 0.5f, 0.5f - iconScale * 0.5f);
            glyphRect.anchorMax = new Vector2(0.5f + iconScale * 0.5f, 0.5f + iconScale * 0.5f);
            glyphRect.offsetMin = Vector2.zero;
            glyphRect.offsetMax = Vector2.zero;

            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = face;
            if (onClick != null) button.onClick.AddListener(() => onClick());
            root.gameObject.AddComponent<PressScale>();
            return button;
        }

        /// <summary>Bară de progres rotunjită (vezi UiProgressBar).</summary>
        public static UiProgressBar CreateProgressBar(string name, Transform parent, Color fillColor)
        {
            RectTransform root = CreateRect(name, parent);
            Image background = CreatePanel("Background", root, new Color(0f, 0f, 0f, 0.35f));
            Stretch((RectTransform)background.transform);
            Image fill = CreatePanel("Fill", root, fillColor);
            var bar = root.gameObject.AddComponent<UiProgressBar>();
            bar.Initialize((RectTransform)fill.transform);
            return bar;
        }

        /// <summary>O bară orizontală simplă de tip „Filled” (pentru etichetele 3D, foarte ieftină).</summary>
        public static Image CreateFill(string name, Transform parent, Color color)
        {
            Image fill = CreateImage(name, parent, WhiteSprite, color);
            fill.preserveAspect = false;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillAmount = 0f;
            return fill;
        }

        /// <summary>Textul de pe un buton creat cu CreateButton.</summary>
        public static UiLabel GetButtonLabel(Button button) => button.GetComponentInChildren<UiLabel>(true);
    }
}
