using System.Collections.Generic;
using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// Comenzi de la camioane: „livrează X resurse în Y secunde” pentru un bonus.
    /// Ține starea comenzii curente; livrarea se face în OrderZone.
    /// </summary>
    public class OrderSystem : MonoBehaviour
    {
        public class ActiveOrder
        {
            public ResourceData Resource;
            public int Required;
            public int Delivered;
            public float TimeLeft;
            public float TimeLimit;
            public long Reward;
            public int Remaining => Mathf.Max(0, Required - Delivered);
        }

        public static OrderSystem Instance { get; private set; }

        [SerializeField] private OrderSettings settings;

        public ActiveOrder Current { get; private set; }
        public bool HasOrder => Current != null;

        private float _cooldown;
        private readonly List<OrderSettings.OrderOption> _candidates = new();

        private void Awake()
        {
            Instance = this;
            _cooldown = settings != null ? settings.FirstOrderDelay : 60f;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (settings == null) return;
            float dt = Time.deltaTime;

            if (Current == null)
            {
                _cooldown -= dt;
                if (_cooldown <= 0f) CreateOrder();
                return;
            }

            Current.TimeLeft -= dt;
            if (Current.TimeLeft > 0f) return;

            Current = null;
            _cooldown = settings.TimeBetweenOrders;
            GameEvents.RaiseOrderFailed();
            GameEvents.RaiseToast("Comanda a expirat. Camionul a plecat.");
            GameEvents.RaiseOrderChanged();
        }

        /// <summary>Livrează o bucată. Întoarce true dacă resursa e cea cerută.</summary>
        public bool Deliver(ResourceData resource)
        {
            if (Current == null || resource != Current.Resource) return false;
            Current.Delivered++;
            if (Current.Delivered >= Current.Required) Complete();
            else GameEvents.RaiseOrderChanged();
            return true;
        }

        private void Complete()
        {
            long reward = Current.Reward;
            Current = null;
            _cooldown = settings.TimeBetweenOrders;
            if (MoneyManager.Instance != null)
            {
                Vector3 position = PlayerRoot.Instance != null ? PlayerRoot.Instance.Position : Vector3.zero;
                reward = MoneyManager.Instance.AddIncome(reward, position, true);
            }
            GameEvents.RaiseOrderCompleted(reward);
            GameEvents.RaiseToast($"Comandă livrată! +{NumberFormat.Money(reward)}");
            GameEvents.RaiseOrderChanged();
        }

        private void CreateOrder()
        {
            int layer = LayerGenerator.Instance != null ? LayerGenerator.Instance.CurrentLayerIndex : 0;
            _candidates.Clear();
            foreach (var option in settings.Options)
                if (option.resource != null && option.minLayer <= layer) _candidates.Add(option);

            if (_candidates.Count == 0)
            {
                _cooldown = settings.TimeBetweenOrders;
                return;
            }

            OrderSettings.OrderOption chosen = _candidates[Random.Range(0, _candidates.Count)];
            int amount = Random.Range(chosen.minAmount, chosen.maxAmount + 1);
            float limit = settings.BaseTimeLimit + amount * settings.SecondsPerItem;
            Current = new ActiveOrder
            {
                Resource = chosen.resource,
                Required = amount,
                TimeLimit = limit,
                TimeLeft = limit,
                Reward = (long)Mathf.Round(amount * chosen.resource.SellPrice * settings.RewardMultiplier),
            };
            GameEvents.RaiseToast($"Comandă nouă: {amount} × {chosen.resource.DisplayName}!");
            GameEvents.RaiseOrderChanged();
        }
    }
}
