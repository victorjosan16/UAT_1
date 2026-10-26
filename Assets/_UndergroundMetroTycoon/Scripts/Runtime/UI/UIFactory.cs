using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UndergroundMetroTycoon.Game
{
    /// <summary>Small helpers for building the code-generated UGUI interface with one consistent style.</summary>
    public static class UIFactory
    {
        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5; // UI
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static RectTransform Stretch(RectTransform rt, float left = 0f, float top = 0f, float right = 0f, float bottom = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        /// <summary>Anchors to a single point with a fixed size.</summary>
        public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return rt;
        }

        public static Image Image(string name, Transform parent, Color color, Sprite sprite = null, float cornerScale = 1f)
        {
            RectTransform rt = Rect(name, parent);
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            if (sprite == UISprites.Rounded)
            {
                image.type = UnityEngine.UI.Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = 1f / Mathf.Max(0.05f, cornerScale);
            }
            return image;
        }

        public static Image Panel(string name, Transform parent, Color color, float cornerScale = 1f) =>
            Image(name, parent, color, UISprites.Rounded, cornerScale);

        public static Text Text(string name, Transform parent, string value, int size, Color color, TextAnchor anchor = TextAnchor.MiddleLeft, bool bold = false)
        {
            RectTransform rt = Rect(name, parent);
            var text = rt.gameObject.AddComponent<Text>();
            text.font = bold ? UITheme.Heavy : UITheme.Body;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = anchor;
            text.fontStyle = FontStyle.Normal;
            text.lineSpacing = 0.9f;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        public static void AddShadow(Graphic graphic, float distance = 3f, float alpha = 0.45f)
        {
            var shadow = graphic.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, alpha);
            shadow.effectDistance = new Vector2(0f, -distance);
        }

        public static Image CoinIcon(Transform parent, float size)
        {
            Image icon = Image("Coin", parent, Color.white, UIIcons.Coin);
            icon.rectTransform.sizeDelta = new Vector2(size, size);
            icon.preserveAspect = true;
            return icon;
        }
    }

    /// <summary>Keeps its RectTransform inside the device safe area (notches, rounded corners, gesture bars).</summary>
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private Rect _applied;
        private Vector2Int _screen;

        private void OnEnable() => Apply();
        private void Update() => Apply();

        private void Apply()
        {
            Rect safe = Screen.safeArea;
            var screen = new Vector2Int(Screen.width, Screen.height);
            if (safe == _applied && screen == _screen) return;
            if (screen.x <= 0 || screen.y <= 0) return;
            _applied = safe;
            _screen = screen;
            var rt = (RectTransform)transform;
            rt.anchorMin = new Vector2(safe.xMin / screen.x, safe.yMin / screen.y);
            rt.anchorMax = new Vector2(safe.xMax / screen.x, safe.yMax / screen.y);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
