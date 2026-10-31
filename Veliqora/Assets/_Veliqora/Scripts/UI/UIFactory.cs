using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Veliqora.UI
{
    /// <summary>Handle for a styled button so screens can update label/state later.</summary>
    public sealed class ButtonHandle
    {
        public Button Button;
        public RectTransform Rect;
        public Image Background;
        public Image Edge;
        public Text Label;
        public Image Icon;
        public UIStyle.ButtonKind Kind;

        public Image Gloss;
        public float Height;
        /// <summary>True for round icon buttons drawn entirely from authored art.</summary>
        public bool ArtFace;

        public void SetInteractable(bool on)
        {
            Button.interactable = on;
            if (ArtFace)
            {
                Background.color = on ? Color.white : new Color(0.62f, 0.62f, 0.62f, 0.85f);
                return;
            }
            var pill = SpriteLibrary.Pill(Kind);
            if (pill != null)
            {
                // Authored pill: the art carries fill, edge and gloss; disabled = desaturated tint.
                Background.sprite = pill;
                Background.type = Image.Type.Sliced;
                Background.pixelsPerUnitMultiplier = pill.rect.height / Mathf.Max(1f, Height);
                Background.rectTransform.offsetMin = Vector2.zero;
                Background.color = on ? Color.white : new Color(0.62f, 0.62f, 0.62f, 0.9f);
                if (Edge != null) Edge.enabled = false;
                if (Gloss != null) Gloss.enabled = false;
            }
            else
            {
                Background.sprite = SpriteLibrary.Rounded(Height * 0.45f);
                Background.type = Image.Type.Sliced;
                Background.pixelsPerUnitMultiplier = 1f;
                Background.rectTransform.offsetMin = new Vector2(0, 8);
                Background.color = on ? UIStyle.ButtonFill(Kind) : UIStyle.Disabled;
                if (Edge != null) { Edge.enabled = true; Edge.color = on ? UIStyle.ButtonEdge(Kind) : UIStyle.Darken(UIStyle.Disabled, 0.15f); }
                if (Gloss != null) Gloss.enabled = Kind != UIStyle.ButtonKind.Ghost;
            }
            if (Label != null) Label.color = on ? UIStyle.ButtonText(Kind) : UIStyle.WithAlpha(UIStyle.White, 0.9f);
        }

        public void SetLabel(string text) { if (Label != null) Label.text = text; }
    }

    /// <summary>Builds uGUI hierarchies in code from the design tokens in UIStyle.</summary>
    public static class UIFactory
    {
        private static Font _font;

        /// <summary>
        /// Primary font. Drop a TTF/OTF at Resources/Veliqora/Fonts/Primary to replace the built-in fallback.
        /// (TextMeshPro is not required; see DESIGN_SPEC.md for switching to TMP.)
        /// </summary>
        public static Font Font
        {
            get
            {
                if (_font != null) return _font;
                _font = Resources.Load<Font>("Veliqora/Fonts/Primary");
                if (_font == null)
                {
                    try { _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch (Exception) { _font = null; }
                }
                if (_font == null)
                {
                    try { _font = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch (Exception) { _font = null; }
                }
                if (_font == null) _font = Font.CreateDynamicFontFromOSFont(new[] { "Roboto", "Arial", "Helvetica" }, 42);
                return _font;
            }
        }

        // ---------------- canvas ----------------

        public static Canvas CreateCanvas(string name, int sortingOrder, Transform parent = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            if (parent != null) go.transform.SetParent(parent, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            canvas.pixelPerfect = false;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            go.AddComponent<AdaptiveScaler>();
            return canvas;
        }

        public static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
            UnityEngine.Object.DontDestroyOnLoad(go);
        }

        /// <summary>Full-screen container inset to the device safe area (notches, cutouts, gesture bars).</summary>
        public static RectTransform SafeArea(Transform canvas)
        {
            var rt = Rect("SafeArea", canvas);
            rt.gameObject.AddComponent<SafeAreaFitter>();
            return rt;
        }

        // ---------------- primitives ----------------

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            Stretch(rt);
            return rt;
        }

        public static void Stretch(RectTransform rt, float inset = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        /// <summary>Anchors to a single point (0..1 coords) with fixed size and pixel offset.</summary>
        public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 size, Vector2 offset, Vector2? pivot = null)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot ?? anchor;
            rt.sizeDelta = size;
            rt.anchoredPosition = offset;
            return rt;
        }

        /// <summary>Stretches horizontally between anchors with fixed height.</summary>
        public static RectTransform Band(RectTransform rt, float anchorY, float height, float offsetY, float sideInset = 0f, float pivotY = 0.5f)
        {
            rt.anchorMin = new Vector2(0, anchorY);
            rt.anchorMax = new Vector2(1, anchorY);
            rt.pivot = new Vector2(0.5f, pivotY);
            rt.sizeDelta = new Vector2(-2f * sideInset, height);
            rt.anchoredPosition = new Vector2(0, offsetY);
            return rt;
        }

        public static Image Image(Transform parent, string name, Sprite sprite, Color color, bool raycast = false)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = raycast;
            if (sprite != null && sprite.border != Vector4.zero) img.type = UnityEngine.UI.Image.Type.Sliced;
            return img;
        }

        public static RawImage Raw(Transform parent, string name, Texture tex, Color color)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<RawImage>();
            img.texture = tex;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        public static Text Text(Transform parent, string text, int size, Color color, TextAnchor anchor = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Bold)
        {
            var rt = Rect("Text", parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = anchor;
            t.fontStyle = style;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.resizeTextForBestFit = false;
            return t;
        }

        public static void AddShadow(Graphic g, Color color, Vector2 distance)
        {
            var s = g.gameObject.AddComponent<Shadow>();
            s.effectColor = color;
            s.effectDistance = distance;
        }

        public static void AddOutline(Graphic g, Color color, float size)
        {
            var o = g.gameObject.AddComponent<Outline>();
            o.effectColor = color;
            o.effectDistance = new Vector2(size, -size);
        }

        /// <summary>Rounded panel with optional soft drop shadow (shadow is a sibling placed behind).</summary>
        public static Image Panel(Transform parent, string name, Color color, float radius = UIStyle.RadiusMedium, bool shadow = true)
        {
            var holder = Rect(name, parent);
            if (shadow)
            {
                var sh = Image(holder, "Shadow", SpriteLibrary.SoftRounded(radius, 18), UIStyle.Shadow);
                Stretch(sh.rectTransform, -14);
                sh.rectTransform.anchoredPosition = new Vector2(0, -10);
            }
            var bg = Image(holder, "Fill", SpriteLibrary.Rounded(radius), color, true);
            return bg;
        }

        // ---------------- buttons ----------------

        public static ButtonHandle Button(Transform parent, string label, UIStyle.ButtonKind kind, Action onClick, Vector2 size, Icon? icon = null, int fontSize = UIStyle.FontBody)
        {
            var h = new ButtonHandle { Kind = kind };
            var root = Rect("Button_" + label, parent);
            root.sizeDelta = size;
            h.Rect = root;

            if (kind != UIStyle.ButtonKind.Ghost)
            {
                var sh = Image(root, "Shadow", SpriteLibrary.SoftRounded(size.y * 0.4f, 12), UIStyle.Shadow);
                Stretch(sh.rectTransform, -8);
                sh.rectTransform.anchoredPosition = new Vector2(0, -8);
            }
            var face = Rect("Face", root);
            h.Edge = Image(face, "Edge", SpriteLibrary.Rounded(size.y * 0.45f), UIStyle.ButtonEdge(kind));
            h.Background = Image(face, "Fill", SpriteLibrary.Rounded(size.y * 0.45f), UIStyle.ButtonFill(kind), true);
            h.Background.rectTransform.offsetMin = new Vector2(0, 8);
            var gloss = Image(face, "Gloss", SpriteLibrary.Rounded(size.y * 0.4f), new Color(1, 1, 1, kind == UIStyle.ButtonKind.Ghost ? 0f : 0.22f));
            gloss.rectTransform.anchorMin = new Vector2(0, 0.55f);
            gloss.rectTransform.offsetMin = new Vector2(14, 0);
            gloss.rectTransform.offsetMax = new Vector2(-14, -8);
            h.Gloss = gloss;
            h.Height = size.y;

            float iconSize = size.y * 0.5f;
            if (icon.HasValue)
            {
                h.Icon = Image(face, "Icon", SpriteLibrary.Get(icon.Value), Color.white);
                bool iconOnly = string.IsNullOrEmpty(label);
                Place(h.Icon.rectTransform, iconOnly ? new Vector2(0.5f, 0.5f) : new Vector2(0, 0.5f), new Vector2(iconSize, iconSize),
                    iconOnly ? new Vector2(0, 4) : new Vector2(size.y * 0.32f + iconSize * 0.5f, 4), new Vector2(0.5f, 0.5f));
                if (kind == UIStyle.ButtonKind.Secondary) h.Icon.color = UIStyle.Ink;
            }
            if (!string.IsNullOrEmpty(label))
            {
                h.Label = Text(face, label, fontSize, UIStyle.ButtonText(kind));
                h.Label.rectTransform.offsetMin = new Vector2(icon.HasValue ? size.y * 0.32f + iconSize : 16, 8);
                h.Label.rectTransform.offsetMax = new Vector2(-16, 0);
                h.Label.horizontalOverflow = HorizontalWrapMode.Overflow;
                if (kind != UIStyle.ButtonKind.Secondary) AddShadow(h.Label, new Color(0, 0, 0, 0.18f), new Vector2(0, -3));
            }

            h.Button = h.Background.gameObject.AddComponent<Button>();
            h.Button.transition = Selectable.Transition.None;
            h.Button.targetGraphic = h.Background;
            var fx = h.Background.gameObject.AddComponent<ButtonFx>();
            fx.Target = face;
            if (onClick != null) h.Button.onClick.AddListener(() => onClick());
            h.SetInteractable(true); // applies authored pill art when available
            return h;
        }

        /// <summary>Circular icon button (min touch target respected).</summary>
        public static ButtonHandle IconButton(Transform parent, Icon icon, Color fill, float size, Action onClick, Color? iconColor = null)
        {
            var h = new ButtonHandle { Kind = UIStyle.ButtonKind.Ghost };
            var root = Rect("IconButton_" + icon, parent);
            root.sizeDelta = new Vector2(size, size);
            h.Rect = root;
            var sh = Image(root, "Shadow", SpriteLibrary.RadialGlow, new Color(0, 0, 0, 0.25f));
            Stretch(sh.rectTransform, -10);
            sh.rectTransform.anchoredPosition = new Vector2(0, -8);
            var face = Rect("Face", root);
            h.Edge = Image(face, "Edge", SpriteLibrary.Circle, UIStyle.Darken(fill, 0.2f));
            h.Background = Image(face, "Fill", SpriteLibrary.Circle, fill, true);
            h.Background.rectTransform.offsetMin = new Vector2(0, 6);
            var gloss = Image(face, "Gloss", SpriteLibrary.Circle, new Color(1, 1, 1, 0.2f));
            gloss.rectTransform.anchorMin = new Vector2(0.18f, 0.52f);
            gloss.rectTransform.anchorMax = new Vector2(0.82f, 0.92f);
            h.Icon = Image(face, "Icon", SpriteLibrary.Get(icon), iconColor ?? Color.white);
            Stretch(h.Icon.rectTransform, size * 0.24f);
            h.Icon.rectTransform.anchoredPosition = new Vector2(0, 3);
            var art = SpriteLibrary.ButtonArt(icon);
            if (art != null)
            {
                // Authored round button: one image replaces circle, edge, gloss and glyph.
                h.Edge.enabled = false;
                gloss.enabled = false;
                h.Icon.enabled = false;
                h.Background.sprite = art;
                h.Background.type = UnityEngine.UI.Image.Type.Simple;
                h.Background.preserveAspect = true;
                h.Background.color = Color.white;
                h.Background.rectTransform.offsetMin = Vector2.zero;
                h.Icon = h.Background;
                h.ArtFace = true;
            }
            h.Button = h.Background.gameObject.AddComponent<Button>();
            h.Button.transition = Selectable.Transition.None;
            var fx = h.Background.gameObject.AddComponent<ButtonFx>();
            fx.Target = face;
            if (onClick != null) h.Button.onClick.AddListener(() => onClick());
            // Expand the hit area to the minimum touch target if the visual is smaller.
            if (size < UIStyle.MinTouch)
            {
                var hit = Image(root, "Hit", null, new Color(0, 0, 0, 0), true);
                Stretch(hit.rectTransform, -(UIStyle.MinTouch - size) * 0.5f);
                hit.transform.SetAsFirstSibling();
                var hb = hit.gameObject.AddComponent<Button>();
                hb.transition = Selectable.Transition.None;
                hb.onClick.AddListener(() => h.Button.onClick.Invoke());
            }
            return h;
        }

        /// <summary>
        /// Title banner: the authored wooden sign when present (horizontally 9-sliced), otherwise a colored ribbon.
        /// Returns the label.
        /// </summary>
        public static Text TitleBanner(Transform parent, string text, Vector2 size, Color fallback)
        {
            var sign = SpriteLibrary.TitleSign();
            Image bg;
            if (sign != null)
            {
                bg = Image(parent, "TitleSign", sign, Color.white);
                bg.type = UnityEngine.UI.Image.Type.Sliced;
                bg.pixelsPerUnitMultiplier = sign.rect.height / Mathf.Max(1f, size.y);
            }
            else
            {
                bg = Image(parent, "Ribbon", SpriteLibrary.Rounded(UIStyle.RadiusMedium), fallback);
            }
            bg.rectTransform.sizeDelta = size;
            AddShadow(bg, UIStyle.Shadow, new Vector2(0, -8));
            var t = Text(bg.transform, text, UIStyle.FontHeading, Color.white);
            if (sign != null) AddOutline(t, UIStyle.Hex("#5A2E10"), 4f);
            AddShadow(t, new Color(0, 0, 0, 0.25f), new Vector2(0, -4));
            return t;
        }

        /// <summary>Simple horizontal layout of children with fixed spacing, centered.</summary>
        public static void LayoutRow(IList<RectTransform> items, float spacing, float y = 0f)
        {
            float total = 0f;
            foreach (var i in items) total += i.sizeDelta.x;
            total += spacing * Mathf.Max(0, items.Count - 1);
            float x = -total * 0.5f;
            foreach (var i in items)
            {
                i.anchorMin = i.anchorMax = new Vector2(0.5f, 0.5f);
                i.pivot = new Vector2(0.5f, 0.5f);
                i.anchoredPosition = new Vector2(x + i.sizeDelta.x * 0.5f, y);
                x += i.sizeDelta.x + spacing;
            }
        }
    }
}
