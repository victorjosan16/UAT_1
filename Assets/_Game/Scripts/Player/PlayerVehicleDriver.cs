using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// Urcarea și coborârea jucătorului din utilaje. Cât timp conduce, mișcarea și minatul
    /// cu târnăcopul sunt oprite, iar jucătorul stă pe scaunul utilajului.
    /// </summary>
    public class PlayerVehicleDriver : MonoBehaviour
    {
        [SerializeField] private CharacterController characterController;
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private PlayerMiner miner;
        [SerializeField] private PlayerInputReader input;
        [Tooltip("Pauză după coborâre, ca jucătorul să nu urce imediat la loc.")]
        [SerializeField] private float reenterCooldown = 1.5f;

        public Vehicle Current { get; private set; }
        public bool IsDriving => Current != null;
        public bool CanEnter => !IsDriving && Time.time - _lastExitTime > reenterCooldown;

        private float _lastExitTime = -100f;

        public void Enter(Vehicle vehicle)
        {
            if (!CanEnter || vehicle == null || vehicle.IsDriven) return;

            Current = vehicle;
            characterController.enabled = false;
            movement.enabled = false;
            miner.enabled = false;

            transform.SetParent(vehicle.Seat, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;

            vehicle.BeginDriving(input);
            GameEvents.RaiseVehicleEntered(vehicle);
        }

        public void Exit()
        {
            if (!IsDriving) return;

            Vehicle vehicle = Current;
            Current = null;
            vehicle.EndDriving();

            transform.SetParent(null, true);
            Vector3 forward = vehicle.transform.forward;
            forward.y = 0f;
            transform.SetPositionAndRotation(vehicle.ExitPoint.position,
                forward.sqrMagnitude > 0.001f ? Quaternion.LookRotation(forward) : Quaternion.identity);

            characterController.enabled = true;
            movement.enabled = true;
            miner.enabled = true;
            _lastExitTime = Time.time;
            GameEvents.RaiseVehicleExited(vehicle);
        }
    }
}
