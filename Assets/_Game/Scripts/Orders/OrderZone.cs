using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// Locul unde parchează camionul cu comanda. Jucătorul care intră aici descarcă automat
    /// resursele cerute. Camionul se vede doar cât există o comandă activă.
    /// </summary>
    public class OrderZone : MonoBehaviour
    {
        [SerializeField] private ZoneArea area;
        [Tooltip("Modelul camionului de comandă (activ doar cât e comandă).")]
        [SerializeField] private GameObject truckModel;
        [Tooltip("Unde zboară resursele livrate.")]
        [SerializeField] private Transform dropPoint;
        [SerializeField] private WorldLabel label;
        [SerializeField] private float interval = 0.08f;

        private float _timer;
        private float _labelTimer;
        private int _inFlight;

        private void OnEnable() => GameEvents.OrderChanged += Refresh;

        private void Start() => Refresh();

        private void OnDisable() => GameEvents.OrderChanged -= Refresh;

        private void Update()
        {
            OrderSystem orders = OrderSystem.Instance;
            if (orders == null || !orders.HasOrder) return;

            _labelTimer -= Time.deltaTime;
            if (_labelTimer <= 0f)
            {
                _labelTimer = 0.25f;
                Refresh();
            }

            PlayerRoot player = PlayerRoot.Instance;
            if (player == null || !area.Contains(player.Position)) return;

            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            _timer = interval;

            OrderSystem.ActiveOrder order = orders.Current;
            ResourceData wanted = order.Resource;
            // Nu scoatem mai multe decât mai trebuie (inclusiv cele aflate deja în zbor).
            if (order.Remaining - _inFlight <= 0) return;
            if (!player.Stack.TryTakeLast(d => d == wanted, out ResourceData data, out Vector3 from)) return;

            _inFlight++;
            EffectsLibrary.FlyItemTo(data.PickupMaterial, from, dropPoint.position, () =>
            {
                _inFlight--;
                if (OrderSystem.Instance == null || !OrderSystem.Instance.Deliver(data))
                {
                    // Comanda a expirat între timp: vindem bucata la preț normal, ca să nu se piardă.
                    if (MoneyManager.Instance != null) MoneyManager.Instance.AddIncome(data.SellPrice, dropPoint.position, true);
                }
            });
        }

        private void Refresh()
        {
            OrderSystem orders = OrderSystem.Instance;
            bool active = orders != null && orders.HasOrder;
            if (truckModel != null && truckModel.activeSelf != active) truckModel.SetActive(active);
            if (label == null) return;

            label.SetVisible(true);
            if (!active)
            {
                label.SetText(Loc.T("COMENZI\nAșteaptă un camion…"));
                label.SetFill(0f);
                return;
            }
            OrderSystem.ActiveOrder order = orders.Current;
            label.SetText(Loc.F("COMANDĂ: {0} × {1}\n{2}/{3} · {4}s · {5}", order.Required, Loc.T(order.Resource.DisplayName), order.Delivered, order.Required, Mathf.CeilToInt(order.TimeLeft), NumberFormat.Money(order.Reward)));
            label.SetFill(order.TimeLimit > 0f ? order.TimeLeft / order.TimeLimit : 0f);
        }
    }
}
