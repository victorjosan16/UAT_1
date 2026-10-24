using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// Un utilaj în care urcă jucătorul. Se conduce cu același joystick, relativ la cameră:
    /// utilajul se întoarce spre direcție și înaintează. Unealta (buldozer/burghiu) lucrează cât e condus.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class Vehicle : MonoBehaviour
    {
        [SerializeField] private VehicleDefinition definition;
        [Tooltip("Unde stă jucătorul cât conduce.")]
        [SerializeField] private Transform seat;
        [Tooltip("Unde apare jucătorul când coboară.")]
        [SerializeField] private Transform exitPoint;
        [SerializeField] private VehicleTool tool;
        [SerializeField] private float gravity = -25f;
        [SerializeField, Range(0f, 0.5f)] private float deadZone = 0.1f;
        [Tooltip("Viteza minimă (procent) cât încă virează: utilajul înaintează în timp ce se întoarce.")]
        [SerializeField, Range(0f, 1f)] private float minTurnSpeed = 0.35f;
        [Tooltip("Dacă joystick-ul e tras aproape opus direcției utilajului, merge înapoi (fără să se întoarcă).")]
        [SerializeField, Range(-1f, 0f)] private float reverseThreshold = -0.7f;
        [SerializeField, Range(0.1f, 1f)] private float reverseSpeed = 0.5f;
        [SerializeField] private float acceleration = 12f;

        public VehicleDefinition Definition => definition;
        public Transform Seat => seat;
        public Transform ExitPoint => exitPoint;
        public bool IsDriven => _input != null;
        public bool IsMoving { get; private set; }

        private CharacterController _controller;
        private PlayerInputReader _input;
        private Transform _camera;
        private float _verticalVelocity;
        private Vector3 _velocity;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            if (Camera.main != null) _camera = Camera.main.transform;
        }

        public void BeginDriving(PlayerInputReader input)
        {
            _input = input;
            if (tool != null) tool.SetActive(true);
        }

        public void EndDriving()
        {
            _input = null;
            _velocity = Vector3.zero;
            IsMoving = false;
            if (tool != null) tool.SetActive(false);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            Vector3 horizontal = Vector3.zero;

            if (IsDriven)
            {
                Vector3 direction = GetCameraRelativeDirection(_input.Move);
                IsMoving = direction.sqrMagnitude > 0f;
                if (IsMoving)
                {
                    float alignment = Vector3.Dot(transform.forward, direction.normalized);
                    if (alignment <= reverseThreshold)
                    {
                        // Mers înapoi: nu se întoarce, doar dă cu spatele.
                        horizontal = -transform.forward * (definition.MoveSpeed * reverseSpeed * direction.magnitude);
                    }
                    else
                    {
                        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
                        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, definition.TurnSpeed * dt);
                        // Virează în timp ce înaintează; încetinește doar la viraje strânse.
                        float speedFactor = Mathf.Lerp(minTurnSpeed, 1f, Mathf.Clamp01(alignment));
                        horizontal = transform.forward * (definition.MoveSpeed * direction.magnitude * speedFactor);
                    }
                }
                if (tool != null) tool.Tick(dt);
            }

            _velocity = Vector3.MoveTowards(_velocity, horizontal, acceleration * dt);
            horizontal = _velocity;

            if (_controller.isGrounded && _verticalVelocity < 0f) _verticalVelocity = -2f;
            _verticalVelocity += gravity * dt;
            _controller.Move((horizontal + Vector3.up * _verticalVelocity) * dt);
        }

        private Vector3 GetCameraRelativeDirection(Vector2 rawInput)
        {
            if (rawInput.magnitude < deadZone) return Vector3.zero;
            Vector3 forward = _camera != null ? _camera.forward : Vector3.forward;
            Vector3 right = _camera != null ? _camera.right : Vector3.right;
            forward.y = 0f;
            right.y = 0f;
            return Vector3.ClampMagnitude(forward.normalized * rawInput.y + right.normalized * rawInput.x, 1f);
        }
    }
}
