using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>Valorile de echilibru ale unui utilaj condus de jucător.</summary>
    [CreateAssetMenu(fileName = "Vehicle_", menuName = "Quarry Empire/Vehicle Definition")]
    public class VehicleDefinition : ScriptableObject
    {
        [field: SerializeField] public string DisplayName { get; private set; } = "Buldozer";

        [field: Header("Mișcare")]
        [field: SerializeField, Min(0f)] public float MoveSpeed { get; private set; } = 4.5f;
        [field: Tooltip("Grade pe secundă.")]
        [field: SerializeField, Min(0f)] public float TurnSpeed { get; private set; } = 180f;

        [field: Header("Unealtă")]
        [field: SerializeField, Min(0f)] public float HitDamage { get; private set; } = 3f;
        [field: SerializeField, Min(0.05f)] public float HitInterval { get; private set; } = 0.5f;
        [field: Tooltip("Cel mai dur bloc pe care îl poate sparge.")]
        [field: SerializeField, Range(1, 5)] public int MiningTier { get; private set; } = 2;
        [field: Tooltip("Cât de în fața utilajului lovește unealta (m).")]
        [field: SerializeField, Min(0f)] public float ToolReach { get; private set; } = 1.6f;
        [field: Tooltip("Mărimea zonei lovite (m). La burghiu se folosește doar X, ca rază.")]
        [field: SerializeField] public Vector3 ToolSize { get; private set; } = new(2.4f, 1f, 1.2f);
    }
}
