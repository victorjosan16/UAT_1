using UnityEngine;
using UnityEngine.UI;

namespace Veliqora.UI.Widgets
{
    /// <summary>
    /// Non-blocking contextual tutorial: a short speech bubble plus an animated pointing hand over a target.
    /// Never blocks input (no raycasts), so players learn by doing instead of reading a wall of text.
    /// </summary>
    public sealed class CoachMark : MonoBehaviour
    {
        private Text _text;
        private RectTransform _hand;
        private RectTransform _bubble;
        private RectTransform _target;
        private RectTransform _space;
        private float _t;

        public static CoachMark Create(RectTransform parent)
        {
            var rt = UIFactory.Rect("CoachMark", parent);
            var c = rt.gameObject.AddComponent<CoachMark>();
            c._space = rt;
            c.BuildUI();
            c.gameObject.SetActive(false);
            return c;
        }

        private void BuildUI()
        {
            var bg = UIFactory.Panel(transform, "Bubble", UIStyle.Ivory, UIStyle.RadiusMedium);
            _bubble = (RectTransform)bg.transform.parent;
            _text = UIFactory.Text(_bubble, "", UIStyle.FontLabel, UIStyle.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Stretch(_text.rectTransform, 26);
            var handImg = UIFactory.Image(transform, "Hand", SpriteLibrary.Circle, new Color(1f, 1f, 1f, 0.9f));
            _hand = handImg.rectTransform;
            _hand.anchorMin = _hand.anchorMax = new Vector2(0.5f, 0.5f);
            _hand.sizeDelta = new Vector2(78, 78);
            var ring = UIFactory.Image(_hand, "Ring", SpriteLibrary.RadialGlow, new Color(1f, 0.85f, 0.3f, 0.9f));
            UIFactory.Stretch(ring.rectTransform, -30);
            ring.transform.SetAsFirstSibling();
            foreach (var g in GetComponentsInChildren<Graphic>(true)) g.raycastTarget = false;
        }

        /// <summary>Shows a message in a bubble at the given vertical anchor; optional pointer target.</summary>
        public void Show(string message, RectTransform target, float bubbleAnchorY = 0.8f)
        {
            gameObject.SetActive(true);
            _text.text = message;
            _target = target;
            UIFactory.Place(_bubble, new Vector2(0.5f, bubbleAnchorY), new Vector2(900, 170), Vector2.zero);
            _hand.gameObject.SetActive(target != null);
            _t = 0f;
            var group = GetComponent<CanvasGroup>();
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            Tween.Fade(this, group, 0f, 1f, MotionPrefs.Dur(0.25f));
        }

        public void Hide()
        {
            if (gameObject.activeSelf) gameObject.SetActive(false);
        }

        public bool IsShowing => gameObject.activeSelf;

        private void LateUpdate()
        {
            if (_target == null) return;
            _t += Time.unscaledDeltaTime;
            // Convert target's top-center into our local space.
            var corners = new Vector3[4];
            _target.GetWorldCorners(corners);
            var topCenter = (corners[1] + corners[2]) * 0.5f;
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_space, RectTransformUtility.WorldToScreenPoint(null, topCenter), null, out local);
            float bob = MotionPrefs.Reduced ? 0f : Mathf.Abs(Mathf.Sin(_t * 4f)) * 30f;
            _hand.anchoredPosition = local + new Vector2(0, 70 + bob);
        }
    }
}
