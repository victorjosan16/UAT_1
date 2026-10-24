using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// Pepite colorate pe fețele blocului, în culoarea minereului, ca blocurile să se deosebească ușor
    /// (blocul de piatră nu are pepite). Rotește aleator modelul ca blocurile să nu arate identic.
    /// </summary>
    public class BlockOreVisual : MonoBehaviour
    {
        [SerializeField] private MineBlock block;
        [Tooltip("Modelul blocului (se rotește aleator în pași de 90°).")]
        [SerializeField] private Transform visual;
        [Tooltip("Grupul de pepite.")]
        [SerializeField] private GameObject oreGroup;
        [SerializeField] private Renderer[] nuggets;
        [Tooltip("Resursa pentru care nu se afișează pepite (piatra simplă).")]
        [SerializeField] private string plainResourceId = "stone";

        private void OnEnable() => block.Initialized += Apply;
        private void OnDisable() => block.Initialized -= Apply;

        private void Apply(MineBlock _)
        {
            ResourceData resource = block.Data != null ? block.Data.DroppedResource : null;
            bool showOre = resource != null && resource.Id != plainResourceId && resource.PickupMaterial != null;
            if (oreGroup != null) oreGroup.SetActive(showOre);
            if (showOre)
                foreach (Renderer nugget in nuggets)
                    if (nugget != null) nugget.sharedMaterial = resource.PickupMaterial;
            if (visual != null) visual.localRotation = Quaternion.Euler(0f, Random.Range(0, 4) * 90f, 0f);
        }
    }
}
