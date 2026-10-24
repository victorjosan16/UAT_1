using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// Definiția unei resurse (piatră, cupru, fier, aur, diamant).
    /// E un ScriptableObject: un fișier de date în proiect pe care îl editezi în Inspector,
    /// fără să atingi codul.
    /// </summary>
    [CreateAssetMenu(fileName = "Resource_", menuName = "Quarry Empire/Resource Data")]
    public class ResourceData : ScriptableObject
    {
        [field: Header("Identitate")]
        [field: SerializeField] public string Id { get; private set; } = "stone";
        [field: SerializeField] public string DisplayName { get; private set; } = "Piatră";
        [field: SerializeField] public Sprite Icon { get; private set; }
        [field: SerializeField] public Color UiColor { get; private set; } = Color.gray;

        [field: Header("Vizual")]
        [field: Tooltip("Materialul bucății care cade din bloc.")]
        [field: SerializeField] public Material PickupMaterial { get; private set; }

        [field: Header("Economie")]
        [field: Tooltip("Prețul de vânzare al unei bucăți brute (folosit din Etapa 2).")]
        [field: SerializeField, Min(0)] public int SellPrice { get; private set; } = 1;
    }
}
