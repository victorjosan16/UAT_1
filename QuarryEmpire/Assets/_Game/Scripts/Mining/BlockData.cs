using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// Definiția unui tip de bloc: cât rezistă, cât de dur e și ce resurse lasă.
    /// </summary>
    [CreateAssetMenu(fileName = "Block_", menuName = "Quarry Empire/Block Data")]
    public class BlockData : ScriptableObject
    {
        [field: Header("Identitate")]
        [field: SerializeField] public string DisplayName { get; private set; } = "Bloc de piatră";
        [field: SerializeField] public Material BlockMaterial { get; private set; }

        [field: Header("Rezistență")]
        [field: Tooltip("Câte puncte de viață are blocul (o lovitură scade HitDamage).")]
        [field: SerializeField, Min(1f)] public float MaxHealth { get; private set; } = 3f;

        [field: Tooltip("Duritatea. Jucătorul poate mina doar blocuri cu Hardness <= MiningTier.")]
        [field: SerializeField, Range(1, 5)] public int Hardness { get; private set; } = 1;

        [field: Header("Drop")]
        [field: SerializeField] public ResourceData DroppedResource { get; private set; }
        [field: SerializeField, Min(0)] public int MinDrops { get; private set; } = 1;
        [field: SerializeField, Min(0)] public int MaxDrops { get; private set; } = 2;
    }
}
