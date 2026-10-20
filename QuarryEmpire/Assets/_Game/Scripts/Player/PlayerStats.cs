using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// Valorile de bază ale jucătorului. În Etapa 3 upgrade-urile vor porni de la aceste valori.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerStats", menuName = "Quarry Empire/Player Stats")]
    public class PlayerStats : ScriptableObject
    {
        [field: Header("Mișcare")]
        [field: SerializeField, Min(0f)] public float MoveSpeed { get; private set; } = 5f;
        [field: Tooltip("Grade pe secundă.")]
        [field: SerializeField, Min(0f)] public float RotationSpeed { get; private set; } = 720f;

        [field: Header("Minat")]
        [field: SerializeField, Min(0f)] public float HitDamage { get; private set; } = 1f;
        [field: Tooltip("Secunde între două lovituri.")]
        [field: SerializeField, Min(0.05f)] public float HitInterval { get; private set; } = 0.4f;
        [field: Tooltip("Raza (m) în care jucătorul găsește blocuri de lovit.")]
        [field: SerializeField, Min(0.1f)] public float HitRange { get; private set; } = 1f;
        [field: Tooltip("Nivelul târnăcopului. Blocurile mai dure de atât sunt ignorate.")]
        [field: SerializeField, Range(1, 5)] public int MiningTier { get; private set; } = 2;

        [field: Header("Colectare")]
        [field: Tooltip("Raza (m) în care resursele de pe jos zboară spre jucător.")]
        [field: SerializeField, Min(0.1f)] public float CollectRadius { get; private set; } = 2.5f;
    }
}
