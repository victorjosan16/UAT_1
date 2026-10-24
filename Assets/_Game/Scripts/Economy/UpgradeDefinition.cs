using UnityEngine;

namespace QuarryEmpire
{
    public enum UpgradeType
    {
        MiningPower,  // + daune pe lovitură
        MiningSpeed,  // - procent din intervalul dintre lovituri
        MoveSpeed,    // + metri pe secundă
        Capacity,     // + locuri în stivă
        PickaxeTier,  // + nivel târnăcop (poate sparge blocuri mai dure)
    }

    /// <summary>Un upgrade cumpărabil: cost pe nivel și bonus pe nivel.</summary>
    [CreateAssetMenu(fileName = "Upgrade_", menuName = "Quarry Empire/Upgrade Definition")]
    public class UpgradeDefinition : ScriptableObject
    {
        [field: SerializeField] public string Id { get; private set; } = "power";
        [field: SerializeField] public string DisplayName { get; private set; } = "Forță";
        [field: SerializeField] public UpgradeType Type { get; private set; }
        [field: SerializeField, Min(1)] public int MaxLevel { get; private set; } = 20;
        [field: SerializeField, Min(1)] public long BaseCost { get; private set; } = 50;
        [field: Tooltip("Costul se înmulțește cu atât la fiecare nivel.")]
        [field: SerializeField, Min(1f)] public float CostMultiplier { get; private set; } = 1.5f;
        [field: Tooltip("Bonusul adăugat la fiecare nivel (unitatea depinde de tip).")]
        [field: SerializeField] public float ValuePerLevel { get; private set; } = 0.5f;

        /// <summary>Costul pentru a trece de la nivelul „currentLevel” la următorul.</summary>
        public long CostForLevel(int currentLevel) =>
            (long)Mathf.Round(BaseCost * Mathf.Pow(CostMultiplier, currentLevel));
    }
}
