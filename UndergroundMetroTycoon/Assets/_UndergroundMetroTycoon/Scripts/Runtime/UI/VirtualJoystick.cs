using UnityEngine;
using UnityEngine.EventSystems;

namespace UndergroundMetroTycoon.Game
{
    /// <summary>
    /// Floating joystick: touching anywhere in its zone moves the base under the finger; dragging the knob
    /// produces a -1..1 vector. Works with any EventSystem input module.
    /// </summary>
    public sealed class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private RectTransform _zone;
        private RectTransform _base;
        private RectTransform _knob;
        private CanvasGroup _baseGroup;
        private Vector2 _restPosition;
        private float _radius;
        private int _pointerId = int.MinValue;

        public Vector2 Value { get; private set; }
        public bool IsHeld => _pointerId != int.MinValue;

        public void Init(RectTransform zone, RectTransform baseRect, RectTransform knob, float radius)
        {
            _zone = zone;
            _base = baseRect;
            _knob = knob;
            _radius = radius;
            _restPosition = baseRect.anchoredPosition;
            _baseGroup = baseRect.gameObject.AddComponent<CanvasGroup>();
            _baseGroup.blocksRaycasts = false;
            _baseGroup.alpha = 0.55f;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (IsHeld) return;
            _pointerId = eventData.pointerId;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_zone, eventData.position, eventData.pressEventCamera, out Vector2 local))
            {
                _base.anchoredPosition = local;
            }
            _baseGroup.alpha = 1f;
            UpdateKnob(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId == _pointerId) UpdateKnob(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != _pointerId) return;
            Release();
        }

        private void OnDisable() => Release();

        private void Release()
        {
            _pointerId = int.MinValue;
            Value = Vector2.zero;
            if (_knob != null) _knob.anchoredPosition = Vector2.zero;
            if (_base != null) _base.anchoredPosition = _restPosition;
            if (_baseGroup != null) _baseGroup.alpha = 0.55f;
        }

        private void UpdateKnob(PointerEventData eventData)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_base, eventData.position, eventData.pressEventCamera, out Vector2 local)) return;
            Vector2 clamped = Vector2.ClampMagnitude(local, _radius);
            _knob.anchoredPosition = clamped;
            Vector2 raw = clamped / _radius;
            // Small dead zone, then rescale so the response still reaches 1.
            float magnitude = raw.magnitude;
            Value = magnitude < 0.12f ? Vector2.zero : raw.normalized * Mathf.InverseLerp(0.12f, 1f, magnitude);
        }
    }
}
