using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace QuarryEmpire
{
    /// <summary>Butonul se „apasă” vizual (scară mai mică) cât e ținut apăsat.</summary>
    public class UiPressFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField, Range(0.7f, 1f)] private float pressedScale = 0.9f;
        [SerializeField] private float speed = 18f;

        private Vector3 _baseScale = Vector3.one;
        private float _target = 1f;
        private float _current = 1f;
        private Selectable _selectable;

        private void Awake()
        {
            _baseScale = transform.localScale;
            _selectable = GetComponent<Selectable>();
        }

        private void OnDisable()
        {
            _target = _current = 1f;
            transform.localScale = _baseScale;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_selectable == null || _selectable.interactable) _target = pressedScale;
        }

        public void OnPointerUp(PointerEventData eventData) => _target = 1f;
        public void OnPointerExit(PointerEventData eventData) => _target = 1f;

        private void Update()
        {
            if (Mathf.Approximately(_current, _target)) return;
            _current = Mathf.MoveTowards(_current, _target, speed * Time.unscaledDeltaTime);
            transform.localScale = _baseScale * _current;
        }
    }
}
