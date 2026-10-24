using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// Feedback la colectare: scântei în culoarea resursei și text plutitor cu valoarea ei.
    /// Ascultă doar GameEvents.ResourceCollected.
    /// </summary>
    public class CollectFeedbackRelay : MonoBehaviour
    {
        [Tooltip("Afișează „+preț$” (ResourceData.SellPrice). Dezactivează dacă vrei doar particule.")]
        [SerializeField] private bool showValueText = true;
        [SerializeField] private Vector3 textOffset = new(0f, 0.9f, 0f);
        [SerializeField, Min(0.1f)] private float textScale = 1f;

        private void OnEnable() => GameEvents.ResourceCollected += HandleCollected;
        private void OnDisable() => GameEvents.ResourceCollected -= HandleCollected;

        private void HandleCollected(ResourceData resource, Vector3 position)
        {
            VfxManager vfx = VfxManager.Instance;
            if (vfx == null || resource == null) return;

            Color color = resource.UiColor;
            vfx.PlayCollect(position, color);
            if (showValueText && resource.SellPrice > 0)
            {
                // Textul ia culoarea resursei, deschisă puțin ca să se citească pe fundal.
                vfx.ShowFloatingText(position + textOffset, $"+{resource.SellPrice}$", Color.Lerp(color, Color.white, 0.35f), textScale);
            }
        }
    }
}
