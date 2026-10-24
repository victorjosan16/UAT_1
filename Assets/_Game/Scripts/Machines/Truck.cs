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
        [Tooltip("Puncte intermediare de la parcare spre vânzare (pe drum, ocolind zonele). La întoarcere se parcurg invers.")]
        [SerializeField] private Transform[] route;
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
        private int _waypoint;
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
                    {
                        _state = State.ToSell;
                        _waypoint = 0;
                    }
                    break;

                case State.ToSell:
                    if (DriveRoute(forward: true, sellPoint.position, dt))
                    {
                        _state = State.Selling;
                        _timer = 0f;
                    }
                    break;

                case State.Selling:
                    // Zona SELL golește automat stiva camionului.
                    _timer += dt;
                    if (stack.IsEmpty || _timer >= maxSellTime)
                    {
                        _state = State.Returning;
                        _waypoint = 0;
                    }
                    break;

                case State.Returning:
                    if (DriveRoute(forward: false, parkPoint.position, dt))
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

        /// <summary>Parcurge traseul (înainte sau invers) și apoi destinația; întoarce true la final.</summary>
        private bool DriveRoute(bool forward, Vector3 destination, float dt)
        {
            int count = route != null ? route.Length : 0;
            if (_waypoint < count)
            {
                Transform point = route[forward ? _waypoint : count - 1 - _waypoint];
                if (point == null || DriveTowards(point.position, dt)) _waypoint++;
                return false;
            }
            return DriveTowards(destination, dt);
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
                State.Parked => stack.IsEmpty ? Loc.T("Încarcă-mă!") : Loc.T("Se încarcă"),
                State.ToSell => Loc.T("Spre vânzare"),
                State.Selling => Loc.T("Vinde"),
                _ => Loc.T("Se întoarce"),
            };
            label.SetText(Loc.F("CAMION {0}/{1}\n{2}", stack.Count, stack.Capacity, status));
            label.SetFill(stack.Capacity > 0 ? (float)stack.Count / stack.Capacity : 0f);
        }
    }
}
