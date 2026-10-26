using UnityEngine;

namespace UndergroundMetroTycoon.Game
{
    /// <summary>
    /// Orthographic isometric camera for portrait screens: smooth follow, pinch / mouse-wheel zoom and
    /// focus bounds. The camera sits far above the scene so it never clips through terrain.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class IsometricCameraController : MonoBehaviour
    {
        [SerializeField] private float pitch = 38f;
        [SerializeField] private float yaw = 45f;
        [SerializeField] private float distance = 40f;
        [SerializeField] private float minZoom = 4.5f;
        [SerializeField] private float maxZoom = 12f;
        [SerializeField] private float zoom = 7.5f;
        [SerializeField] private float followSmoothTime = 0.2f;
        [SerializeField] private float zoomSmoothTime = 0.12f;

        private Camera _camera;
        private Transform _target;
        private Vector3 _focus;
        private Vector3 _focusVelocity;
        private float _zoomTarget;
        private float _zoomVelocity;
        private Rect _bounds = new Rect(-8f, -8f, 16f, 16f);
        private VirtualJoystick _joystick;

        public Camera Camera => _camera;

        public void Init(Transform target, Rect focusBounds, VirtualJoystick joystick)
        {
            _camera = GetComponent<Camera>();
            _camera.orthographic = true;
            _camera.nearClipPlane = 1f;
            _camera.farClipPlane = 120f;
            _target = target;
            _bounds = focusBounds;
            _joystick = joystick;
            _zoomTarget = zoom;
            _camera.orthographicSize = zoom;
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            _focus = ClampFocus(target != null ? target.position : Vector3.zero);
            Apply();
        }

        public void SetJoystick(VirtualJoystick joystick) => _joystick = joystick;

        private void LateUpdate()
        {
            if (_camera == null) return;
            HandleZoomInput();
            float dt = Time.deltaTime;
            _camera.orthographicSize = Mathf.SmoothDamp(_camera.orthographicSize, _zoomTarget, ref _zoomVelocity, zoomSmoothTime, Mathf.Infinity, dt);
            if (_target != null)
            {
                Vector3 desired = ClampFocus(_target.position + Vector3.up * 0.6f);
                _focus = Vector3.SmoothDamp(_focus, desired, ref _focusVelocity, followSmoothTime, Mathf.Infinity, dt);
            }
            Apply();
        }

        private void Apply()
        {
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            transform.position = _focus - transform.forward * distance;
        }

        private Vector3 ClampFocus(Vector3 p) =>
            new Vector3(Mathf.Clamp(p.x, _bounds.xMin, _bounds.xMax), p.y, Mathf.Clamp(p.z, _bounds.yMin, _bounds.yMax));

        private void HandleZoomInput()
        {
            bool joystickHeld = _joystick != null && _joystick.IsHeld;
            float dpi = Screen.dpi > 0f ? Screen.dpi / 160f : 1f;
#if ENABLE_INPUT_SYSTEM && UMT_INPUT_SYSTEM
            UnityEngine.InputSystem.Mouse mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse != null)
            {
                float wheel = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(wheel) > 0.01f) _zoomTarget -= Mathf.Sign(wheel) * 0.6f;
            }
            UnityEngine.InputSystem.Touchscreen screen = UnityEngine.InputSystem.Touchscreen.current;
            if (screen != null && !joystickHeld)
            {
                UnityEngine.InputSystem.Controls.TouchControl a = null, b = null;
                foreach (UnityEngine.InputSystem.Controls.TouchControl t in screen.touches)
                {
                    if (!t.press.isPressed) continue;
                    if (a == null) a = t;
                    else if (b == null) b = t;
                }
                if (a != null && b != null)
                {
                    Vector2 pa = a.position.ReadValue(), pb = b.position.ReadValue();
                    float current = Vector2.Distance(pa, pb);
                    float previous = Vector2.Distance(pa - a.delta.ReadValue(), pb - b.delta.ReadValue());
                    _zoomTarget -= (current - previous) * 0.01f / dpi;
                }
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f) _zoomTarget -= scroll * 0.6f;

            if (Input.touchCount == 2 && !joystickHeld)
            {
                Touch a = Input.GetTouch(0);
                Touch b = Input.GetTouch(1);
                float current = Vector2.Distance(a.position, b.position);
                float previous = Vector2.Distance(a.position - a.deltaPosition, b.position - b.deltaPosition);
                _zoomTarget -= (current - previous) * 0.01f / dpi;
            }
#endif
            _zoomTarget = Mathf.Clamp(_zoomTarget, minZoom, maxZoom);
        }
    }
}
