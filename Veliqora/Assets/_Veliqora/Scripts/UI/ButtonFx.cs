using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Veliqora.UI
{
    /// <summary>Press-scale animation + feedback for any button.</summary>
    public sealed class ButtonFx : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IPointerClickHandler
    {
        public RectTransform Target;
        private bool _down;

        private Selectable _sel;
        private void Awake() { _sel = GetComponent<Selectable>(); }

        public void OnPointerDown(PointerEventData e)
        {
            if (_sel != null && !_sel.interactable) return;
            _down = true;
            Animate(0.94f);
        }

        public void OnPointerUp(PointerEventData e) { if (_down) { _down = false; Animate(1f); } }
        public void OnPointerExit(PointerEventData e) { if (_down) { _down = false; Animate(1f); } }

        public void OnPointerClick(PointerEventData e)
        {
            if (_sel != null && !_sel.interactable) return;
            UIFeedback.RaiseButtonPressed();
        }

        private void Animate(float s)
        {
            var t = Target != null ? Target : (RectTransform)transform;
            var from = t.localScale;
            Tween.Run(this, MotionPrefs.Dur(0.1f), k => { if (t != null) t.localScale = Vector3.Lerp(from, new Vector3(s, s, 1), k); });
        }
    }
}
