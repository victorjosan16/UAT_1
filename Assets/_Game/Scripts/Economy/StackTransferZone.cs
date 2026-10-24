using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// Mută resursele din stiva jucătorului într-o altă stivă (ex. încărcarea camionului).
    /// </summary>
    public class StackTransferZone : MonoBehaviour
    {
        [SerializeField] private ZoneArea area;
        [SerializeField] private ResourceStack target;
        [Tooltip("Opțional: transferul merge doar cât timp camionul e parcat.")]
        [SerializeField] private Truck truck;
        [SerializeField] private float interval = 0.06f;

        private float _timer;

        private void Update()
        {
            PlayerRoot player = PlayerRoot.Instance;
            if (player == null || target == null || !area.Contains(player.Position)) return;
            if (truck != null && !truck.IsParked) return;

            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            _timer = interval;

            if (!target.TryReserve()) return;
            if (!player.Stack.TryTakeLast(null, out ResourceData data, out Vector3 from))
            {
                target.CancelReservation();
                return;
            }
            ResourceStack destination = target;
            EffectsLibrary.FlyItem(data.PickupMaterial, from, destination.Anchor, Vector3.zero,
                () => destination.AddReserved(data));
        }
    }
}
