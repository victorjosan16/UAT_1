using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TollRoadTycoon.Presentation
{
    /// <summary>
    /// Portrait camera: drag to pan (mostly vertical), pinch or mouse wheel to zoom, tap to select.
    /// Works with touch on devices and the mouse in the editor.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class CameraController : MonoBehaviour
    {
        public float MinZoom = 6.5f;
        public float MaxZoom = 15f;
        public float MinY = -30f;
        public float MaxY = 28f;
        public float MinX = -6f;
        public float MaxX = 5f;
        public float TapMaxMove = 18f;

        /// <summary>Raised with the world point of a tap that did not hit the UI.</summary>
        public event Action<Vector2> Tapped;

        /// <summary>Set by UI so the bottom panel does not overlap the plaza focus point.</summary>
        public float BottomPanelFraction { get; set; } = 0.3f;

        private Camera _camera;
        private Vector2 _pressScreen;
        private Vector3 _lastWorld;
        private bool _dragging;
        private bool _pressedOnUi;
        private float _lastPinch;
        private bool _wasPinching;

        public Camera Camera => _camera;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _camera.orthographic = true;
            _camera.orthographicSize = 10.5f;
        }

        public void Focus(Vector2 world)
        {
            var p = transform.position;
            // Shift down so the focus point sits in the visible area above the bottom panel.
            float offset = _camera.orthographicSize * BottomPanelFraction;
            transform.position = new Vector3(world.x, world.y - offset, p.z);
            Clamp();
        }

        public void SetHorizontalBounds(float min, float max)
        {
            MinX = min;
            MaxX = max;
            Clamp();
        }

        private void Update()
        {
            if (Input.touchSupported && Input.touchCount > 0) HandleTouch();
            else HandleMouse();
        }

        private void HandleMouse()
        {
            float wheel = Input.mouseScrollDelta.y;
            if (Mathf.Abs(wheel) > 0.01f && !OverUi(-1)) Zoom(-wheel * 0.8f);

            if (Input.GetMouseButtonDown(0))
            {
                _pressedOnUi = OverUi(-1);
                Begin(Input.mousePosition);
            }
            else if (Input.GetMouseButton(0) && !_pressedOnUi)
            {
                Drag(Input.mousePosition);
            }
            else if (Input.GetMouseButtonUp(0) && !_pressedOnUi)
            {
                End(Input.mousePosition);
            }
        }

        private void HandleTouch()
        {
            if (Input.touchCount >= 2)
            {
                var a = Input.GetTouch(0);
                var b = Input.GetTouch(1);
                float dist = Vector2.Distance(a.position, b.position);
                if (_wasPinching)
                {
                    float delta = _lastPinch - dist;
                    Zoom(delta * _camera.orthographicSize / Mathf.Max(200f, Screen.height * 0.5f));
                }
                _lastPinch = dist;
                _wasPinching = true;
                _dragging = true; // a pinch never counts as a tap
                _lastWorld = _camera.ScreenToWorldPoint((a.position + b.position) * 0.5f);
                return;
            }

            var touch = Input.GetTouch(0);
            if (_wasPinching)
            {
                _wasPinching = false;
                _lastWorld = _camera.ScreenToWorldPoint(touch.position);
                if (touch.phase == TouchPhase.Ended) return;
            }
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    _pressedOnUi = OverUi(touch.fingerId);
                    Begin(touch.position);
                    break;
                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    if (!_pressedOnUi) Drag(touch.position);
                    break;
                case TouchPhase.Ended:
                    if (!_pressedOnUi) End(touch.position);
                    break;
            }
        }

        private void Begin(Vector2 screen)
        {
            _pressScreen = screen;
            _lastWorld = _camera.ScreenToWorldPoint(screen);
            _dragging = false;
        }

        private void Drag(Vector2 screen)
        {
            if (!_dragging && Vector2.Distance(screen, _pressScreen) > TapMaxMove * Screen.dpi / 160f + 1f) _dragging = true;
            if (!_dragging) return;
            var world = _camera.ScreenToWorldPoint(screen);
            var delta = _lastWorld - world;
            transform.position += new Vector3(delta.x * 0.6f, delta.y, 0);
            Clamp();
            _lastWorld = _camera.ScreenToWorldPoint(screen);
        }

        private void End(Vector2 screen)
        {
            if (!_dragging) Tapped?.Invoke(_camera.ScreenToWorldPoint(screen));
            _dragging = false;
        }

        private void Zoom(float amount)
        {
            _camera.orthographicSize = Mathf.Clamp(_camera.orthographicSize + amount, MinZoom, MaxZoom);
            Clamp();
        }

        private void Clamp()
        {
            var p = transform.position;
            p.x = Mathf.Clamp(p.x, MinX, MaxX);
            p.y = Mathf.Clamp(p.y, MinY, MaxY);
            transform.position = p;
        }

        private static bool OverUi(int pointerId)
        {
            var es = EventSystem.current;
            if (es == null) return false;
            return pointerId < 0 ? es.IsPointerOverGameObject() : es.IsPointerOverGameObject(pointerId);
        }
    }
}
