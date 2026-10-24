using UnityEngine;
using UnityEngine.EventSystems;

namespace SugarDrop
{
    /// <summary>
    /// Animația de apăsare a unui buton: 1.0 → 0.94 când e apăsat, apoi revine la 1.0 cu o mică săritură.
    /// Folosește timpul nescalat (merge și în pauză) și nu face nimic în Update când nu se animă.
    /// Opțional, o vibrație scurtă la apăsare.
    /// </summary>
    [DisallowMultipleComponent]
    public class UIButtonPressAnimator : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private float pressedScale = 0.94f;
        [SerializeField] private float pressDuration = 0.08f;
        [SerializeField] private float releaseDuration = 0.32f;
        [SerializeField] private bool haptic = true;

        private Transform _target;
        private float _from = 1f;
        private float _to = 1f;
        private float _time;
        private float _duration;
        private bool _pressed;
        private bool _animating;
        private Vector3 _baseScale = Vector3.one;

        private void Awake()
        {
            _target = transform;
            _baseScale = _target.localScale;
        }

        private void OnDisable()
        {
            _pressed = false;
            _animating = false;
            if (_target != null) _target.localScale = _baseScale;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _pressed = true;
            StartTween(pressedScale, pressDuration);
            if (haptic) SugarAudio.Buzz(0);
        }

        public void OnPointerUp(PointerEventData eventData) => Release();
        public void OnPointerExit(PointerEventData eventData) => Release();

        private void Release()
        {
            if (!_pressed) return;
            _pressed = false;
            StartTween(1f, releaseDuration);
        }

        private void StartTween(float to, float duration)
        {
            _from = CurrentScale();
            _to = to;
            _time = 0f;
            _duration = Mathf.Max(0.01f, duration);
            _animating = true;
        }

        private float CurrentScale() => _baseScale.x > 0f ? _target.localScale.x / _baseScale.x : 1f;

        private void Update()
        {
            if (!_animating) return;
            _time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(_time / _duration);
            // La apăsare: rapid și moale; la eliberare: revine cu depășire (bounce).
            float eased = _to < 1f ? UiEase.OutQuad(t) : UiEase.OutBack(t, 3f);
            _target.localScale = _baseScale * Mathf.LerpUnclamped(_from, _to, eased);
            if (t >= 1f) _animating = false;
        }
    }
}
