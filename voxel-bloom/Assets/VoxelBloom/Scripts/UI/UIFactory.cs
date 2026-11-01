using UnityEngine;
using UnityEngine.UI;

namespace VoxelBloom
{
    public enum ButtonStyle { Green, Blue, Purple, White, Gold, Red, Lavender }

    /// <summary>Builders for the Voxel Bloom design system (reference resolution 1080x1920).</summary>
    public static class UI
    {
        // ------------------------------------------------------------------ rect helpers
        public static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5; // UI
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static RectTransform Stretch(this RectTransform rt, float left = 0, float right = 0, float top = 0, float bottom = 0)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom); rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        /// <summary>Anchors at a normalized point; pos is the offset from that anchor.</summary>
        public static RectTransform Place(this RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size, Vector2? pivot = null)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot ?? new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            return rt;
        }

        /// <summary>Horizontal band anchored to the top (y from top) spanning the width.</summary>
        public static RectTransform TopBand(this RectTransform rt, float yFromTop, float height, float margin = 0)
        {
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1); rt.pivot = new Vector2(0.5f, 1);
            rt.offsetMin = new Vector2(margin, -yFromTop - height); rt.offsetMax = new Vector2(-margin, -yFromTop);
            return rt;
        }

        public static RectTransform BottomBand(this RectTransform rt, float yFromBottom, float height, float margin = 0)
        {
            rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(1, 0); rt.pivot = new Vector2(0.5f, 0);
            rt.offsetMin = new Vector2(margin, yFromBottom); rt.offsetMax = new Vector2(-margin, yFromBottom + height);
            return rt;
        }

        // ------------------------------------------------------------------ visuals
        public static Image Image(Transform parent, string name, Sprite sprite, Color color, bool sliced = true, bool raycast = false)
        {
            var rt = Rect(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.type = sliced && sprite != null && sprite.border != Vector4.zero ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
            img.raycastTarget = raycast;
            return img;
        }

        public static Image Panel(Transform parent, string name, Color color, int radius = 40, bool shadow = true, float shadowOffset = 10f)
        {
            var root = Rect(parent, name);
            if (shadow)
            {
                var sh = Image(root, "Shadow", SpriteFactory.Shadow(radius), Theme.Shadow);
                sh.rectTransform.Stretch(-radius * 0.9f, -radius * 0.9f, -radius * 0.9f + shadowOffset, -radius * 0.9f - shadowOffset);
            }
            var face = Image(root, "Face", SpriteFactory.Rounded(radius), color, true, true);
            face.rectTransform.Stretch();
            return face;
        }

        public static Image Icon(Transform parent, Icon icon, float size)
        {
            var img = Image(parent, "Icon_" + icon, SpriteFactory.Get(icon), Color.white, false);
            img.preserveAspect = true;
            img.rectTransform.sizeDelta = new Vector2(size, size);
            return img;
        }

        public static VText Text(Transform parent, string text, float size, Color color, TextAnchor align = TextAnchor.MiddleCenter, float outline = 0f, Color? outlineColor = null)
        {
            var rt = Rect(parent, "Text");
            var t = rt.gameObject.AddComponent<VText>();
            t.font = Fonts.Main;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.raycastTarget = false;
            t.supportRichText = false;
            t.Wrap = false;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            if (outline > 0f)
            {
                t.outlineColor = outlineColor ?? Theme.Ink;
                t.outlineWidth = outline;
            }
            return t;
        }

        public static void StyleColors(ButtonStyle s, out Color face, out Color lip, out Color text)
        {
            text = Color.white;
            switch (s)
            {
                case ButtonStyle.Green: face = Theme.Green; lip = Theme.GreenDark; break;
                case ButtonStyle.Blue: face = Theme.Blue; lip = Theme.BlueDark; break;
                case ButtonStyle.Purple: face = Theme.Purple; lip = Theme.PurpleDark; break;
                case ButtonStyle.Gold: face = Theme.Gold; lip = Theme.GoldDark; break;
                case ButtonStyle.Red: face = Theme.Red; lip = Theme.Darker(Theme.Red, 0.7f); break;
                case ButtonStyle.Lavender: face = Theme.Lavender; lip = Theme.Purple; break;
                default: face = Color.white; lip = Theme.PanelLine; text = Theme.Ink; break;
            }
        }

        /// <summary>Glossy 3D-lip button with optional leading icon.</summary>
        public static UIButton Button(Transform parent, string label, ButtonStyle style, Vector2 size, Icon? icon = null, float fontSize = 0f)
        {
            StyleColors(style, out var face, out var lip, out var textColor);
            var root = Rect(parent, "Button_" + label);
            root.sizeDelta = size;
            var hit = root.gameObject.AddComponent<Image>();
            hit.color = new Color(1, 1, 1, 0);
            int radius = Mathf.RoundToInt(Mathf.Min(size.y * 0.42f, 56f));
            float lipH = Mathf.Max(8f, size.y * 0.1f);
            var sh = Image(root, "Shadow", SpriteFactory.Shadow(radius), Theme.Shadow);
            sh.rectTransform.Stretch(-radius * 0.8f, -radius * 0.8f, -radius * 0.8f + lipH * 2, -radius * 0.8f - lipH * 1.5f);
            var lipImg = Image(root, "Lip", SpriteFactory.Rounded(radius), lip);
            lipImg.rectTransform.Stretch(0, 0, lipH, -lipH * 0.2f);
            var faceImg = Image(root, "Face", SpriteFactory.Glossy(radius), face);
            faceImg.rectTransform.Stretch(0, 0, 0, lipH);
            var btn = root.gameObject.AddComponent<UIButton>();
            btn.Face = faceImg.rectTransform;
            float fs = fontSize > 0 ? fontSize : size.y * 0.42f;
            float iconSize = size.y * 0.62f;
            if (icon.HasValue)
            {
                var ic = Icon(faceImg.transform, icon.Value, iconSize);
                if (string.IsNullOrEmpty(label)) ic.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(iconSize, iconSize));
                else ic.rectTransform.Place(new Vector2(0f, 0.5f), new Vector2(iconSize * 0.5f + size.y * 0.22f, 0), new Vector2(iconSize, iconSize));
            }
            if (!string.IsNullOrEmpty(label))
            {
                var t = Text(faceImg.transform, label, fs, textColor, TextAnchor.MiddleCenter, style == ButtonStyle.White ? 0f : 0.22f, Theme.Darker(lip, 0.8f));
                t.rectTransform.Stretch(icon.HasValue ? iconSize + size.y * 0.25f : 16, 16, 0, 0);
                t.name = "Label";
            }
            return btn;
        }

        public static UIButton IconButton(Transform parent, Icon icon, float size, ButtonStyle style = ButtonStyle.White)
        {
            var b = Button(parent, null, style, new Vector2(size, size), icon);
            return b;
        }

        /// <summary>Purple title ribbon with white outlined text.</summary>
        public static RectTransform Ribbon(Transform parent, string text, float width, float height = 132f)
        {
            var img = Image(parent, "Ribbon", SpriteFactory.Ribbon(), Color.white);
            img.rectTransform.sizeDelta = new Vector2(width, height);
            var t = Text(img.transform, text, height * 0.42f, Color.white, TextAnchor.MiddleCenter, 0.25f, Theme.PurpleDark);
            t.rectTransform.Stretch(60, 60, 4, height * 0.12f);
            t.name = "Title";
            return img.rectTransform;
        }

        /// <summary>White rounded pill with an icon on the left and a value label (top bars).</summary>
        public static VText Pill(Transform parent, Icon icon, string value, out RectTransform root, float width = 260f, float height = 84f)
        {
            var bg = Panel(parent, "Pill_" + icon, new Color(1, 1, 1, 0.92f), 40, true, 4f);
            root = (RectTransform)bg.transform.parent;
            root.sizeDelta = new Vector2(width, height);
            var ic = Icon(root, icon, height * 1.02f);
            ic.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(height * 0.35f, 0), new Vector2(height * 1.02f, height * 1.02f));
            var t = Text(root, value, height * 0.46f, Theme.Ink);
            t.rectTransform.Stretch(height * 0.9f, height * 0.25f, 0, 0);
            return t;
        }

        public static ScrollRect VerticalScroll(Transform parent, out RectTransform content, float spacing = 24f, RectOffset padding = null)
        {
            var view = Rect(parent, "Scroll");
            var sr = view.gameObject.AddComponent<ScrollRect>();
            var mask = view.gameObject.AddComponent<RectMask2D>();
            mask.padding = Vector4.zero;
            var bg = view.gameObject.AddComponent<Image>();
            bg.color = new Color(1, 1, 1, 0);
            content = Rect(view, "Content");
            content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = content.offsetMax = Vector2.zero;
            var vl = content.gameObject.AddComponent<VerticalLayoutGroup>();
            vl.spacing = spacing;
            vl.padding = padding ?? new RectOffset(40, 40, 20, 40);
            vl.childControlHeight = false; vl.childControlWidth = true;
            vl.childForceExpandHeight = false; vl.childForceExpandWidth = true;
            vl.childAlignment = TextAnchor.UpperCenter;
            var fit = content.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.content = content;
            sr.horizontal = false;
            sr.movementType = ScrollRect.MovementType.Elastic;
            sr.scrollSensitivity = 40f;
            sr.viewport = view;
            return sr;
        }

        public static RawImage Raw(Transform parent, string name, Texture tex)
        {
            var rt = Rect(parent, name);
            var r = rt.gameObject.AddComponent<RawImage>();
            r.texture = tex;
            r.raycastTarget = false;
            return r;
        }

        /// <summary>Full-screen sky gradient background.</summary>
        public static Image Sky(Transform parent, Color top, Color bottom)
        {
            var img = Image(parent, "Sky", SpriteFactory.VerticalGradient(top, bottom), Color.white, false);
            img.rectTransform.Stretch();
            return img;
        }

        public static LayoutElement Layout(Component c, float height, float width = -1)
        {
            var le = c.gameObject.GetComponent<LayoutElement>();
            if (le == null) le = c.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight = height;
            if (width > 0) le.preferredWidth = width;
            return le;
        }

        /// <summary>Progress bar (rounded track + green fill). Returns the fill image (use fillAmount-like width via SetProgress).</summary>
        public static ProgressBar Progress(Transform parent, Color fill, float height = 40f)
        {
            var track = Panel(parent, "Progress", new Color(0.86f, 0.89f, 0.96f), Mathf.RoundToInt(height / 2), false);
            var root = (RectTransform)track.transform.parent;
            var f = Image(track.transform, "Fill", SpriteFactory.Glossy(Mathf.RoundToInt(height / 2)), fill);
            var pb = root.gameObject.AddComponent<ProgressBar>();
            pb.Fill = f.rectTransform;
            pb.Fill.anchorMin = new Vector2(0, 0); pb.Fill.anchorMax = new Vector2(0, 1);
            pb.Fill.pivot = new Vector2(0, 0.5f);
            pb.Fill.offsetMin = new Vector2(4, 4); pb.Fill.offsetMax = new Vector2(0, -4);
            return pb;
        }
    }

    public sealed class ProgressBar : MonoBehaviour
    {
        public RectTransform Fill;
        float shown, target;

        public void Set(float value, bool instant = false)
        {
            target = Mathf.Clamp01(value);
            if (instant) { shown = target; Apply(); }
        }

        void Update()
        {
            if (Mathf.Abs(shown - target) < 0.0005f) return;
            shown = Mathf.Lerp(shown, target, 1f - Mathf.Exp(-8f * Time.unscaledDeltaTime));
            Apply();
        }

        void Apply()
        {
            var parent = (RectTransform)Fill.parent;
            float w = Mathf.Max(0, parent.rect.width - 8f);
            Fill.anchorMax = new Vector2(0, 1);
            Fill.sizeDelta = new Vector2(Mathf.Max(shown > 0 ? 24f : 0f, w * shown), Fill.sizeDelta.y);
        }
    }
}
