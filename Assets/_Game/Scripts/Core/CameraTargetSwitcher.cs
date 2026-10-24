using Unity.Cinemachine;
using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>Camera urmărește jucătorul, iar cât conduce un utilaj, urmărește utilajul.</summary>
    public class CameraTargetSwitcher : MonoBehaviour
    {
        [SerializeField] private CinemachineCamera virtualCamera;
        [SerializeField] private Transform player;

        private void OnEnable()
        {
            GameEvents.VehicleEntered += OnVehicleEntered;
            GameEvents.VehicleExited += OnVehicleExited;
        }

        private void OnDisable()
        {
            GameEvents.VehicleEntered -= OnVehicleEntered;
            GameEvents.VehicleExited -= OnVehicleExited;
        }

        private void OnVehicleEntered(Vehicle vehicle) => SetTarget(vehicle.transform);
        private void OnVehicleExited(Vehicle _) => SetTarget(player);

        private void SetTarget(Transform target)
        {
            if (virtualCamera == null || target == null) return;
            CameraTarget cameraTarget = virtualCamera.Target;
            cameraTarget.TrackingTarget = target;
            virtualCamera.Target = cameraTarget;
        }
    }
}
