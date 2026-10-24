using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// Zona de lângă utilaj: dacă jucătorul stă în ea puțin timp, urcă automat.
    /// Zona se mișcă odată cu utilajul (e copilul lui).
    /// </summary>
    public class VehicleSeat : MonoBehaviour
    {
        [SerializeField] private Vehicle vehicle;
        [SerializeField] private ZoneArea area;
        [SerializeField] private float enterDelay = 0.5f;

        private float _insideTime;

        private void Update()
        {
            PlayerRoot player = PlayerRoot.Instance;
            if (player == null || vehicle.IsDriven || !player.Driver.CanEnter || !area.Contains(player.Position))
            {
                _insideTime = 0f;
                return;
            }

            _insideTime += Time.deltaTime;
            if (_insideTime < enterDelay) return;
            _insideTime = 0f;
            player.Driver.Enter(vehicle);
        }
    }
}
