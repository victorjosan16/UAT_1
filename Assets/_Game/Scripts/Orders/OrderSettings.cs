using System;
using System.Collections.Generic;
using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>Ce comenzi pot veni de la camioane și cât de generoase sunt.</summary>
    [CreateAssetMenu(fileName = "OrderSettings", menuName = "Quarry Empire/Order Settings")]
    public class OrderSettings : ScriptableObject
    {
        [Serializable]
        public class OrderOption
        {
            public ResourceData resource;
            [Tooltip("Comanda apare doar de la acest strat în jos (0 = primul).")]
            [Min(0)] public int minLayer;
            [Min(1)] public int minAmount = 5;
            [Min(1)] public int maxAmount = 10;
        }

        [field: SerializeField] public List<OrderOption> Options { get; private set; } = new();
        [field: Tooltip("Secunde până la prima comandă.")]
        [field: SerializeField, Min(0f)] public float FirstOrderDelay { get; private set; } = 45f;
        [field: Tooltip("Secunde între comenzi.")]
        [field: SerializeField, Min(0f)] public float TimeBetweenOrders { get; private set; } = 60f;
        [field: SerializeField, Min(5f)] public float BaseTimeLimit { get; private set; } = 30f;
        [field: SerializeField, Min(0f)] public float SecondsPerItem { get; private set; } = 6f;
        [field: Tooltip("Recompensa = valoarea resurselor × acest multiplicator.")]
        [field: SerializeField, Min(1f)] public float RewardMultiplier { get; private set; } = 3f;
    }
}
