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
#if ENABLE_LEGACY_INPUT_MANAGER
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f) _zoomTarget -= scroll * 0.6f;

            bool joystickBusy = _joystick != null && _joystick.IsHeld;
            if (Input.touchCount == 2 && !joystickBusy)
            {
                Touch a = Input.GetTouch(0);
                Touch b = Input.GetTouch(1);
                float current = Vector2.Distance(a.position, b.position);
                float previous = Vector2.Distance(a.position - a.deltaPosition, b.position - b.deltaPosition);
                float dpiScale = Screen.dpi > 0f ? Screen.dpi / 160f : 1f;
                _zoomTarget -= (current - previous) * 0.01f / dpiScale;
            }
#endif
            _zoomTarget = Mathf.Clamp(_zoomTarget, minZoom, maxZoom);
        }
    }
}
