using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace QuarryEmpire
{
    /// <summary>
    /// Rând compact de „cipuri” cu resursele colectate (punct colorat + număr).
    /// Înlocuiește vizual contorul de debug din Etapa 1 (acela e doar dezactivat, nu șters).
    /// Un cip apare abia la prima resursă de acel tip.
    /// </summary>
    public class ResourceChipsBar : MonoBehaviour
    {
        [Tooltip("Cip model (dezactivat în scenă). Trebuie să aibă un Image „Dot” și un TMP_Text „Count”.")]
        [SerializeField] private RectTransform chipTemplate;
        [SerializeField, Min(1)] private int maxChips = 5;

        private class Chip
        {
            public TMP_Text count;
            public UiPunchScale punch;
            public int value;
        }

        private readonly Dictionary<ResourceData, Chip> _chips = new();

        private void Awake()
        {
            if (chipTemplate != null) chipTemplate.gameObject.SetActive(false);
        }

        private void OnEnable() => GameEvents.ResourceCollected += HandleCollected;
        private void OnDisable() => GameEvents.ResourceCollected -= HandleCollected;

        private void HandleCollected(ResourceData resource, Vector3 _)
        {
            if (resource == null || chipTemplate == null) return;
            if (!_chips.TryGetValue(resource, out Chip chip))
            {
                if (_chips.Count >= maxChips) return;
                chip = CreateChip(resource);
                _chips.Add(resource, chip);
            }
            chip.value++;
            if (chip.count != null) chip.count.text = chip.value.ToString();
            if (chip.punch != null) chip.punch.Punch();
        }

        private Chip CreateChip(ResourceData resource)
        {
            RectTransform instance = Instantiate(chipTemplate, chipTemplate.parent);
            instance.name = "Chip_" + resource.Id;
            instance.gameObject.SetActive(true);

            Transform dot = instance.Find("Dot");
            if (dot != null && dot.TryGetComponent(out Image dotImage))
            {
                if (resource.Icon != null)
                {
                    dotImage.sprite = resource.Icon;
                    dotImage.color = Color.white;
                }
                else
                {
                    dotImage.color = resource.UiColor;
                }
            }

            Transform countTransform = instance.Find("Count");
            return new Chip
            {
                count = countTransform != null ? countTransform.GetComponent<TMP_Text>() : null,
                punch = instance.GetComponent<UiPunchScale>(),
            };
        }
    }
}
