using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;

namespace QuarryEmpire
{
    /// <summary>
    /// Joystick flotant: apare exact unde pui degetul (oriunde în jumătatea de jos a ecranului)
    /// și trimite direcția în New Input System ca stick-ul stâng al unui gamepad, deci
    /// PlayerInputReader îl citește la fel ca înainte. Se pune pe o zonă transparentă (Image) mare.
    /// </summary>
    public class FloatingJoystick : OnScreenControl, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [InputControl(layout = "Vector2")]
        [SerializeField] private string m_ControlPath = "<Gamepad>/leftStick";

        [Tooltip("Inelul exterior (se mută la punctul atins).")]
        [SerializeField] private RectTransform background;
        [Tooltip("Butonul interior (se mișcă în interiorul inelului).")]
        [SerializeField] private RectTransform handle;
        [Tooltip("Cât de departe (pixeli de referință) se poate mișca butonul: distanța pentru viteză maximă.")]
        [SerializeField] private float radius = 110f;
        [Tooltip("Transparența joystick-ului cât nu e atins.")]
        [SerializeField, Range(0f, 1f)] private float idleAlpha = 0.45f;

        protected override string controlPathInternal
        {
            get => m_ControlPath;
            set => m_ControlPath = value;
        }

        private RectTransform _area;
        private CanvasGroup _visuals;
        private Vector3 _homePosition;
        private int _pointerId = int.MinValue;

        private void Awake()
        {
            _area = (RectTransform)transform;
            _visuals = background.GetComponent<CanvasGroup>();
            if (_visuals == null) _visuals = background.gameObject.AddComponent<CanvasGroup>();
            _visuals.blocksRaycasts = false; // atingerile le primește zona mare, nu inelul
            _visuals.alpha = idleAlpha;
        }

        private void Start() => _homePosition = background.position;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_pointerId != int.MinValue) return; // un singur deget controlează joystick-ul
            _pointerId = eventData.pointerId;
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(_area, eventData.position, eventData.pressEventCamera, out Vector3 world))
                background.position = world;
            _visuals.alpha = 1f;
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != _pointerId) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(background, eventData.position, eventData.pressEventCamera, out Vector2 local))
                return;
            Vector2 clamped = Vector2.ClampMagnitude(local, radius);
            handle.anchoredPosition = clamped;
            SendValueToControl(clamped / radius);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != _pointerId) return;
            Release();
        }

        private void Release()
        {
            _pointerId = int.MinValue;
            handle.anchoredPosition = Vector2.zero;
            SendValueToControl(Vector2.zero);
            background.position = _homePosition;
            _visuals.alpha = idleAlpha;
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && _pointerId != int.MinValue) Release();
        }
    }
}
