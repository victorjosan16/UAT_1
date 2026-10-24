using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>Valorile unui muncitor NPC.</summary>
    [CreateAssetMenu(fileName = "Worker_", menuName = "Quarry Empire/Worker Definition")]
    public class WorkerDefinition : ScriptableObject
    {
        [field: SerializeField] public string DisplayName { get; private set; } = "Muncitor";
        [field: SerializeField, Min(0.5f)] public float MoveSpeed { get; private set; } = 3f;
        [field: SerializeField, Min(0f)] public float HitDamage { get; private set; } = 1f;
        [field: SerializeField, Min(0.1f)] public float HitInterval { get; private set; } = 0.6f;
        [field: SerializeField, Range(1, 5)] public int MiningTier { get; private set; } = 2;
        [field: SerializeField, Min(1)] public int Capacity { get; private set; } = 8;
        [field: SerializeField, Min(0.5f)] public float CollectRadius { get; private set; } = 2f;
        [field: Tooltip("Bani câștigați pe minut cât jocul e închis (progres idle).")]
        [field: SerializeField, Min(0f)] public float OfflineIncomePerMinute { get; private set; } = 20f;
    }
}
