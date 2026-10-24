using System;
using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// Valorile curente ale jucătorului = valorile de bază (PlayerStats) + bonusurile din upgrade-uri.
    /// Toate scripturile jucătorului citesc de aici, nu direct din ScriptableObject.
    /// </summary>
    [DefaultExecutionOrder(-150)]
    public class PlayerStatsRuntime : MonoBehaviour
    {
        [SerializeField] private PlayerStats baseStats;
        [SerializeField] private ResourceStack stack;
        [SerializeField] private ResourceCollector collector;

        public float MoveSpeed { get; private set; }
        public float RotationSpeed => baseStats.RotationSpeed;
        public float HitDamage { get; private set; }
        public float HitInterval { get; private set; }
        public float HitRange => baseStats.HitRange;
        public int MiningTier { get; private set; }
        public float CollectRadius => baseStats.CollectRadius;
        public int StackCapacity { get; private set; }

        public event Action Changed;

        private void Awake() => Recalculate();

        private void OnEnable() => GameEvents.Upgraded += OnUpgraded;
        private void OnDisable() => GameEvents.Upgraded -= OnUpgraded;

        private void Start() => Recalculate(); // UpgradeManager a încărcat nivelurile în Awake

        private void OnUpgraded(UpgradeDefinition _, int __) => Recalculate();

        private void Recalculate()
        {
            UpgradeManager upgrades = UpgradeManager.Instance;

            MoveSpeed = baseStats.MoveSpeed + Bonus(upgrades, UpgradeType.MoveSpeed);
            HitDamage = baseStats.HitDamage + Bonus(upgrades, UpgradeType.MiningPower);
            // Viteza de lovire: fiecare nivel scade intervalul cu un procent.
            float speedPercent = Bonus(upgrades, UpgradeType.MiningSpeed);
            HitInterval = Mathf.Max(0.12f, baseStats.HitInterval * (1f - Mathf.Clamp01(speedPercent)));
            MiningTier = baseStats.MiningTier + Mathf.RoundToInt(Bonus(upgrades, UpgradeType.PickaxeTier));
            StackCapacity = baseStats.StackCapacity + Mathf.RoundToInt(Bonus(upgrades, UpgradeType.Capacity));

            if (stack != null) stack.Capacity = StackCapacity;
            if (collector != null) collector.Radius = CollectRadius;
            Changed?.Invoke();
        }

        private static float Bonus(UpgradeManager upgrades, UpgradeType type) =>
            upgrades != null ? upgrades.GetTotalBonus(type) : 0f;
    }
}
