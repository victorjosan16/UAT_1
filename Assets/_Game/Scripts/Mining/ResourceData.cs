using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// Definiția unei resurse (piatră, cupru, fier, aur, diamant și lingourile lor).
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
        [field: Tooltip("Materialul bucății care cade din bloc și al cubului din stivă.")]
        [field: SerializeField] public Material PickupMaterial { get; private set; }

        [field: Header("Economie")]
        [field: Tooltip("Prețul de vânzare al unei bucăți.")]
        [field: SerializeField, Min(0)] public int SellPrice { get; private set; } = 1;

        [field: Header("Rafinare")]
        [field: Tooltip("În ce se transformă la rafinărie. Gol = nu se poate rafina.")]
        [field: SerializeField] public ResourceData RefinedInto { get; private set; }
        [field: Tooltip("Secunde pentru rafinarea unei bucăți.")]
        [field: SerializeField, Min(0.1f)] public float RefineSeconds { get; private set; } = 2f;

        public bool CanBeRefined => RefinedInto != null;
    }
}
