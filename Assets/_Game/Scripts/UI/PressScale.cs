using UnityEngine;
using UnityEngine.EventSystems;

namespace QuarryEmpire
{
    /// <summary>Butonul se „apasă” vizual (se micșorează) când e atins.</summary>
    public class PressScale : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private float pressedScale = 0.92f;

        private float _target = 1f;

        public void OnPointerDown(PointerEventData eventData) => _target = pressedScale;
        public void OnPointerUp(PointerEventData eventData) => _target = 1f;
        public void OnPointerExit(PointerEventData eventData) => _target = 1f;

        private void OnDisable()
        {
            _target = 1f;
            transform.localScale = Vector3.one;
        }

        private void Update()
        {
            float current = transform.localScale.x;
            if (Mathf.Approximately(current, _target)) return;
            float next = Mathf.MoveTowards(current, _target, Time.unscaledDeltaTime * 2.5f);
            transform.localScale = new Vector3(next, next, 1f);
        }
    }
}
