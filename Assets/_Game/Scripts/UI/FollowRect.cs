using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>Un element de UI care urmează alt element cu un decalaj (ex. umbra butonului joystick-ului).</summary>
    public class FollowRect : MonoBehaviour
    {
        private RectTransform _target;
        private Vector2 _offset;
        private RectTransform _rect;

        public void Initialize(RectTransform target, Vector2 offset)
        {
            _target = target;
            _offset = offset;
            _rect = (RectTransform)transform;
        }

        private void LateUpdate()
        {
            if (_target == null) return;
            _rect.anchoredPosition = _target.anchoredPosition + _offset;
        }
    }
}
