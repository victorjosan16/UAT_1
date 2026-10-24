using Unity.Cinemachine;
using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// Depărtează ușor camera când jucătorul conduce un utilaj sau cară o stivă mare,
    /// ca să vadă mai mult din jur. Modifică doar distanța (FollowOffset), nu și unghiul.
    /// </summary>
    public class CameraZoom : MonoBehaviour
    {
        [SerializeField] private CinemachineFollow follow;
        [SerializeField] private Vector3 baseOffset = new(-8.2f, 13.8f, -8.2f);
        [Tooltip("Zoom suplimentar (procent) în utilaj.")]
        [SerializeField] private float vehicleZoom = 0.28f;
        [Tooltip("Zoom suplimentar (procent) la stivă plină.")]
        [SerializeField] private float fullStackZoom = 0.12f;
        [SerializeField] private float smoothing = 2.5f;

        private float _zoom = 1f;

        private void LateUpdate()
        {
            PlayerRoot player = PlayerRoot.Instance;
            if (follow == null || player == null) return;

            float target = 1f;
            if (player.Driver != null && player.Driver.IsDriving) target += vehicleZoom;
            ResourceStack stack = player.Stack;
            if (stack != null && stack.Capacity > 0) target += fullStackZoom * Mathf.Clamp01((float)stack.Count / stack.Capacity);

            _zoom = Mathf.Lerp(_zoom, target, 1f - Mathf.Exp(-smoothing * Time.deltaTime));
            follow.FollowOffset = baseOffset * _zoom;
        }
    }
}
