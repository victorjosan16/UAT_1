using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM && LASTBLOCK_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace LastBlock.Gameplay
{
    /// <summary>
    /// Single-pointer input (touch or mouse): tap a block to select it, drag to slide it out along its long
    /// axis, release to let go. Dragging on empty space orbits the camera.
    ///
    /// The block is moved physically: every FixedUpdate its velocity is set toward the target offset,
    /// capped at <see cref="maxPullSpeed"/>. It is never teleported, and its rotation/other axes are frozen
    /// while dragged so it cannot sweep through neighbouring blocks.
    /// </summary>
    public class BlockDragController : MonoBehaviour
    {
        enum PointerMode { None, DraggingBlock, Orbiting }

        [SerializeField] Camera worldCamera;
        [SerializeField] CameraController cameraController;
        [SerializeField] TowerGenerator towerGenerator;

        [Header("Drag")]
        [SerializeField] float maxPullSpeed = 2f;
        [Tooltip("Fraction of the remaining distance covered per physics step (before the speed cap).")]
        [SerializeField, Range(0.05f, 1f)] float responsiveness = 0.3f;
        [SerializeField] float dragSensitivity = 1.5f;
        [Tooltip("Extra distance beyond the tower footprint before a block counts as extracted.")]
        [SerializeField] float extractionMargin = 0.03f;
        [SerializeField] float raycastDistance = 100f;

        /// <summary>When false, blocks cannot be selected (camera orbit still works).</summary>
        public bool SelectionEnabled { get; set; }
        public bool OrbitEnabled { get; set; } = true;
        public BlockController DraggedBlock => draggedBlock;
        public bool IsDragging => draggedBlock != null;

        public event Action<BlockController> DragStarted;
        public event Action<BlockController> DragEnded;
        public event Action<BlockController> BlockExtracted;
        public event Action<BlockController> LockedBlockTapped;

        static readonly List<RaycastResult> UiHits = new List<RaycastResult>();

        PointerMode mode;
        bool wasPressed;
        Vector2 pointerStart;
        Vector2 lastPointer;

        BlockController draggedBlock;
        Vector3 pullAxis;
        float dragStartOffset;
        float targetOffset;
        Vector2 screenAxis;
        float pixelsPerUnit;

        float ExtractDistance => (towerGenerator != null ? towerGenerator.BlockLength : 1.54f) + extractionMargin;
        Vector3 TowerCenter => towerGenerator != null ? towerGenerator.TowerCenter : Vector3.zero;

        public void Configure(Camera cameraToUse, CameraController orbitCamera, TowerGenerator generator)
        {
            worldCamera = cameraToUse;
            cameraController = orbitCamera;
            towerGenerator = generator;
        }

        void Awake()
        {
            if (worldCamera == null) worldCamera = Camera.main;
            if (cameraController == null && worldCamera != null) cameraController = worldCamera.GetComponent<CameraController>();
            if (towerGenerator == null) towerGenerator = FindFirstObjectByType<TowerGenerator>();
        }

        void Update()
        {
            bool pressed = ReadPointer(out Vector2 position);
            if (pressed && !wasPressed) OnPointerDown(position);
            else if (pressed) OnPointerHeld(position);
            else if (wasPressed) OnPointerUp();
            wasPressed = pressed;
        }

        // ---------------------------------------------------------------- input

        static bool ReadPointer(out Vector2 position)
        {
#if ENABLE_INPUT_SYSTEM && LASTBLOCK_INPUT_SYSTEM
            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen != null && touchscreen.primaryTouch.press.isPressed)
            {
                position = touchscreen.primaryTouch.position.ReadValue();
                return true;
            }
            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                position = mouse.position.ReadValue();
                return mouse.leftButton.isPressed;
            }
            position = Vector2.zero;
            return false;
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                position = touch.position;
                return touch.phase != TouchPhase.Ended && touch.phase != TouchPhase.Canceled;
            }
            position = Input.mousePosition;
            return Input.GetMouseButton(0);
#else
            position = Vector2.zero;
            return false;
#endif
        }

        static bool IsPointerOverUI(Vector2 position)
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null) return false;
            PointerEventData data = new PointerEventData(eventSystem) { position = position };
            UiHits.Clear();
            eventSystem.RaycastAll(data, UiHits);
            return UiHits.Count > 0;
        }

        void OnPointerDown(Vector2 position)
        {
            mode = PointerMode.None;
            pointerStart = position;
            lastPointer = position;
            if (worldCamera == null || IsPointerOverUI(position)) return;

            if (SelectionEnabled && TryPickBlock(position, out BlockController block))
            {
                if (block.IsLocked)
                {
                    block.FlashDenied();
                    LockedBlockTapped?.Invoke(block);
                }
                else if (BeginDrag(block))
                {
                    mode = PointerMode.DraggingBlock;
                    return;
                }
            }

            if (OrbitEnabled) mode = PointerMode.Orbiting;
        }

        void OnPointerHeld(Vector2 position)
        {
            switch (mode)
            {
                case PointerMode.DraggingBlock:
                    if (draggedBlock == null) { mode = PointerMode.None; break; }
                    float pixels = Vector2.Dot(position - pointerStart, screenAxis);
                    SetTargetOffset(dragStartOffset + pixels / pixelsPerUnit * dragSensitivity);
                    break;
                case PointerMode.Orbiting:
                    if (cameraController != null && OrbitEnabled) cameraController.Orbit(position.x - lastPointer.x);
                    break;
            }
            lastPointer = position;
        }

        void OnPointerUp()
        {
            if (mode == PointerMode.DraggingBlock) EndDrag();
            mode = PointerMode.None;
        }

        bool TryPickBlock(Vector2 screenPosition, out BlockController block)
        {
            block = null;
            Ray ray = worldCamera.ScreenPointToRay(screenPosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, raycastDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                return false;
            // Only objects with a BlockController are interactive; the platform and background are ignored.
            block = hit.collider.GetComponentInParent<BlockController>();
            return block != null && block.State == BlockState.Idle;
        }

        // ---------------------------------------------------------------- drag API (also used by tests)

        /// <summary>Starts dragging a block. Only one block can be dragged at a time.</summary>
        public bool BeginDrag(BlockController block)
        {
            if (draggedBlock != null || block == null || !block.CanBeSelected) return false;

            draggedBlock = block;
            pullAxis = block.PullAxis;
            dragStartOffset = Vector3.Dot(block.Body.position - TowerCenter, pullAxis);
            targetOffset = dragStartOffset;
            ComputeScreenAxis(block.transform.position);
            block.BeginDrag(pullAxis);
            DragStarted?.Invoke(block);
            return true;
        }

        /// <summary>Sets where (along its pull axis, relative to the tower centre) the block should move to.</summary>
        public void SetTargetOffset(float offset)
        {
            float limit = ExtractDistance + 0.5f;
            targetOffset = Mathf.Clamp(offset, -limit, limit);
        }

        public void EndDrag()
        {
            if (draggedBlock == null) return;
            BlockController block = draggedBlock;
            draggedBlock = null;
            mode = PointerMode.None;
            if (block != null && block.State == BlockState.Dragging)
            {
                block.EndDrag();
                DragEnded?.Invoke(block);
            }
        }

        /// <summary>Stops any drag immediately (e.g. on game over or restart).</summary>
        public void CancelDrag()
        {
            EndDrag();
            mode = PointerMode.None;
        }

        void ComputeScreenAxis(Vector3 blockPosition)
        {
            Vector2 a = worldCamera != null ? (Vector2)worldCamera.WorldToScreenPoint(blockPosition) : Vector2.zero;
            Vector2 b = worldCamera != null ? (Vector2)worldCamera.WorldToScreenPoint(blockPosition + pullAxis) : Vector2.zero;
            Vector2 delta = b - a;
            if (delta.magnitude >= 20f)
            {
                pixelsPerUnit = delta.magnitude;
                screenAxis = delta / pixelsPerUnit;
            }
            else
            {
                // Axis points almost straight at the camera: use vertical swipes (up = away from camera).
                pixelsPerUnit = Mathf.Max(100f, Screen.height * 0.15f);
                bool away = worldCamera == null || Vector3.Dot(pullAxis, worldCamera.transform.forward) > 0f;
                screenAxis = away ? Vector2.up : Vector2.down;
            }
        }

        void FixedUpdate()
        {
            if (draggedBlock == null) return;
            if (draggedBlock.State != BlockState.Dragging)
            {
                draggedBlock = null;
                return;
            }

            Rigidbody body = draggedBlock.Body;
            float current = Vector3.Dot(body.position - TowerCenter, pullAxis);
            float speed = (targetOffset - current) * responsiveness / Time.fixedDeltaTime;
            speed = Mathf.Clamp(speed, -maxPullSpeed, maxPullSpeed);
            body.linearVelocity = pullAxis * speed;
            body.angularVelocity = Vector3.zero;

            if (Mathf.Abs(current) >= ExtractDistance) CompleteExtraction(Mathf.Sign(current));
        }

        void CompleteExtraction(float side)
        {
            BlockController block = draggedBlock;
            draggedBlock = null;
            mode = PointerMode.None;
            block.MarkExtracted(pullAxis * side);
            BlockExtracted?.Invoke(block);
        }
    }
}
