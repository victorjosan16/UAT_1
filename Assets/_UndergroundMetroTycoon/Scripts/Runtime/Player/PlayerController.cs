using UndergroundMetroTycoon.Simulation;
using UnityEngine;

namespace UndergroundMetroTycoon.Game
{
    /// <summary>
    /// Camera-relative movement from the virtual joystick (or WASD/arrows in the Editor) with smooth
    /// acceleration. Collision and ground alignment come from the deterministic <see cref="KinematicMover"/>.
    /// </summary>
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 3.6f;
        [SerializeField] private float acceleration = 18f;
        [SerializeField] private float turnSpeed = 720f;

        private KinematicMover _mover;
        private CharacterRig _rig;
        private VirtualJoystick _joystick;
        private Transform _cameraTransform;
        private Vector2 _velocity;
        private Vector2 _input;

        public KinematicMover Mover => _mover;
        public CharacterRig Rig => _rig;
        public bool HasMoveInput => _input.sqrMagnitude > 0.01f;
        public bool IsMoving => HasMoveInput || _velocity.sqrMagnitude > 0.04f;
        public float Yaw => transform.eulerAngles.y;

        public void Init(KinematicMover mover, CharacterRig rig, Transform cameraTransform)
        {
            _mover = mover;
            _rig = rig;
            _cameraTransform = cameraTransform;
            transform.position = mover.Position.ToUnity();
        }

        public void SetJoystick(VirtualJoystick joystick) => _joystick = joystick;

        public void Teleport(Vector3 position, float yaw)
        {
            _mover.Teleport(position.ToSim());
            _velocity = Vector2.zero;
            transform.SetPositionAndRotation(_mover.Position.ToUnity(), Quaternion.Euler(0f, yaw, 0f));
        }

        /// <summary>Turns toward a world point; returns true once roughly facing it.</summary>
        public bool FaceTowards(Vector3 worldPoint, float deltaTime)
        {
            Vector3 dir = worldPoint - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return true;
            Quaternion target = Quaternion.LookRotation(dir.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeed * deltaTime);
            return Quaternion.Angle(transform.rotation, target) < 25f;
        }

        private void Update()
        {
            if (_mover == null) return;
            float dt = Time.deltaTime;
            _input = ReadInput();

            Vector3 forward = _cameraTransform != null ? _cameraTransform.forward : Vector3.forward;
            forward.y = 0f;
            forward = forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
            Vector3 right = new Vector3(forward.z, 0f, -forward.x);
            Vector3 wish = right * _input.x + forward * _input.y;
            Vector2 desired = new Vector2(wish.x, wish.z) * moveSpeed;

            _velocity = Vector2.MoveTowards(_velocity, desired, acceleration * dt);
            Vector3 before = transform.position;
            transform.position = _mover.Step(_velocity.x * dt, _velocity.y * dt, dt).ToUnity();

            Vector3 moved = transform.position - before;
            moved.y = 0f;
            if (moved.sqrMagnitude < (_velocity * dt).sqrMagnitude * 0.25f) _velocity *= 0.5f;

            if (_velocity.sqrMagnitude > 0.01f && HasMoveInput)
            {
                Quaternion target = Quaternion.LookRotation(new Vector3(_velocity.x, 0f, _velocity.y), Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeed * dt);
            }

            if (_rig != null) _rig.MoveSpeed01 = moved.magnitude / Mathf.Max(0.0001f, moveSpeed * dt);
        }

        private Vector2 ReadInput()
        {
            Vector2 value = _joystick != null ? _joystick.Value : Vector2.zero;
#if ENABLE_INPUT_SYSTEM && UMT_INPUT_SYSTEM
            UnityEngine.InputSystem.Keyboard kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null)
            {
                var keys = new Vector2(
                    (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed || kb.leftArrowKey.isPressed ? 1f : 0f),
                    (kb.wKey.isPressed || kb.upArrowKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed || kb.downArrowKey.isPressed ? 1f : 0f));
                if (keys.sqrMagnitude > value.sqrMagnitude) value = keys;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            var keys = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            if (keys.sqrMagnitude > value.sqrMagnitude) value = keys;
#endif
            return Vector2.ClampMagnitude(value, 1f);
        }
    }
}
