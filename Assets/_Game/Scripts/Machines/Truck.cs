using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// Camionul de transport automat: jucătorul îl încarcă în parcare (StackTransferZone);
    /// când are destulă marfă, pleacă singur la zona SELL, vinde tot și se întoarce.
    /// </summary>
    public class Truck : MonoBehaviour
    {
        private enum State { Parked, ToSell, Selling, Returning }

        [SerializeField] private ResourceStack stack;
        [SerializeField] private Transform parkPoint;
        [Tooltip("Punctul din interiorul zonei SELL unde oprește camionul.")]
        [SerializeField] private Transform sellPoint;
        [SerializeField] private WorldLabel label;
        [SerializeField] private float speed = 6f;
        [SerializeField] private float turnSpeed = 240f;
        [Tooltip("Pleacă atunci când are cel puțin atâtea resurse.")]
        [SerializeField, Min(1)] private int departThreshold = 12;
        [Tooltip("Sau după atâtea secunde de așteptare, dacă are măcar ceva.")]
        [SerializeField] private float maxWaitWithCargo = 15f;
        [SerializeField] private float maxSellTime = 20f;

        public bool IsParked => _state == State.Parked;

        private State _state;
        private float _timer;
        private float _labelTimer;

        private void OnEnable()
        {
            if (parkPoint != null) transform.SetPositionAndRotation(parkPoint.position, parkPoint.rotation);
            _state = State.Parked;
            _timer = 0f;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            switch (_state)
            {
                case State.Parked:
                    _timer = stack.IsEmpty ? 0f : _timer + dt;
                    if (stack.Count >= departThreshold || (!stack.IsEmpty && _timer >= maxWaitWithCargo) || (stack.IsFull && !stack.IsEmpty))
                        _state = State.ToSell;
                    break;

                case State.ToSell:
                    if (DriveTowards(sellPoint.position, dt))
                    {
                        _state = State.Selling;
                        _timer = 0f;
                    }
                    break;

                case State.Selling:
                    // Zona SELL golește automat stiva camionului.
                    _timer += dt;
                    if (stack.IsEmpty || _timer >= maxSellTime) _state = State.Returning;
                    break;

                case State.Returning:
                    if (DriveTowards(parkPoint.position, dt))
                    {
                        transform.rotation = parkPoint.rotation;
                        _state = State.Parked;
                        _timer = 0f;
                    }
                    break;
            }

            _labelTimer -= dt;
            if (_labelTimer <= 0f)
            {
                _labelTimer = 0.25f;
                RefreshLabel();
            }
        }

        /// <summary>Merge spre punct; întoarce true când a ajuns.</summary>
        private bool DriveTowards(Vector3 target, float dt)
        {
            Vector3 offset = target - transform.position;
            offset.y = 0f;
            if (offset.magnitude < 0.2f) return true;

            Quaternion look = Quaternion.LookRotation(offset.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, look, turnSpeed * dt);
            transform.position = Vector3.MoveTowards(transform.position, new Vector3(target.x, transform.position.y, target.z), speed * dt);
            return false;
        }

        private void RefreshLabel()
        {
            if (label == null) return;
            string status = _state switch
            {
                State.Parked => stack.IsEmpty ? "Încarcă-mă!" : "Se încarcă",
                State.ToSell => "Spre vânzare",
                State.Selling => "Vinde",
                _ => "Se întoarce",
            };
            label.SetText($"CAMION {stack.Count}/{stack.Capacity}\n{status}");
            label.SetFill(stack.Capacity > 0 ? (float)stack.Count / stack.Capacity : 0f);
        }
    }
}
