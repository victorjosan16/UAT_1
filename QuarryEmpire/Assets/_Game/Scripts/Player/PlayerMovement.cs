using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// Mută jucătorul relativ la cameră (sus pe joystick = „în sus” pe ecran)
    /// și îl rotește spre direcția de mers sau, când stă pe loc, spre ținta de minat.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        [SerializeField] private PlayerStats stats;
        [SerializeField] private PlayerInputReader input;
        [Tooltip("Dacă e gol, se folosește automat Main Camera.")]
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private float gravity = -25f;
        [Tooltip("Sub această valoare a joystick-ului considerăm că jucătorul stă pe loc.")]
        [SerializeField, Range(0f, 0.5f)] private float deadZone = 0.1f;

        public bool IsMoving { get; private set; }

        private CharacterController _controller;
        private float _verticalVelocity;
        private bool _hasAimTarget;
        private Vector3 _aimTarget;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            // Camera.main e apelat o singură dată aici, nu în Update.
            if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
        }

        /// <summary>Punctul spre care se întoarce jucătorul când nu se mișcă (ex. blocul lovit).</summary>
        public void SetAimTarget(Vector3 worldPoint)
        {
            _hasAimTarget = true;
            _aimTarget = worldPoint;
        }

        public void ClearAimTarget() => _hasAimTarget = false;

        private void Update()
        {
            Vector3 moveDirection = GetCameraRelativeDirection(input.Move);
            IsMoving = moveDirection.sqrMagnitude > 0f;

            // Gravitație simplă, ca jucătorul să stea lipit de sol.
            if (_controller.isGrounded && _verticalVelocity < 0f) _verticalVelocity = -2f;
            _verticalVelocity += gravity * Time.deltaTime;

            Vector3 velocity = moveDirection * stats.MoveSpeed + Vector3.up * _verticalVelocity;
            _controller.Move(velocity * Time.deltaTime);

            UpdateRotation(moveDirection);
        }

        private Vector3 GetCameraRelativeDirection(Vector2 rawInput)
        {
            if (rawInput.magnitude < deadZone) return Vector3.zero;

            Vector3 forward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
            Vector3 right = cameraTransform != null ? cameraTransform.right : Vector3.right;
            // Proiectăm pe sol: camera e înclinată, dar noi vrem mișcare orizontală.
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            return Vector3.ClampMagnitude(forward * rawInput.y + right * rawInput.x, 1f);
        }

        private void UpdateRotation(Vector3 moveDirection)
        {
            Vector3 lookDirection;
            if (IsMoving)
            {
                lookDirection = moveDirection;
            }
            else if (_hasAimTarget)
            {
                lookDirection = _aimTarget - transform.position;
                lookDirection.y = 0f;
            }
            else
            {
                return;
            }

            if (lookDirection.sqrMagnitude < 0.0001f) return;

            Quaternion targetRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, targetRotation, stats.RotationSpeed * Time.deltaTime);
        }
    }
}
