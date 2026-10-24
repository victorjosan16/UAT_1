using TMPro;
using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// Stilul interfeței într-un singur loc: fonturi, culori și (opțional) sprite-uri desenate de tine.
    /// Câmpurile de sprite goale sunt înlocuite automat cu cele generate de ProceduralSprites.
    /// Se editează în Assets/_Game/Data/UiTheme.asset.
    /// </summary>
    [CreateAssetMenu(fileName = "UiTheme", menuName = "Quarry Empire/UI Theme")]
    public class UiTheme : ScriptableObject
    {
        [field: Header("Fonturi")]
        [field: Tooltip("Font TextMeshPro (generat din fontul de mai jos). Gol = se folosește textul clasic.")]
        [field: SerializeField] public TMP_FontAsset TmpFont { get; private set; }
        [field: Tooltip("Fontul TTF de rezervă pentru textul clasic.")]
        [field: SerializeField] public Font Font { get; private set; }

        [field: Header("Sprite-uri opționale (gol = generate din cod)")]
        [field: SerializeField] public Sprite PanelSprite { get; private set; }
        [field: SerializeField] public Sprite CircleSprite { get; private set; }

        [field: Header("Culori")]
        [field: SerializeField] public Color PanelColor { get; private set; } = new(0.12f, 0.15f, 0.25f, 0.92f);
        [field: SerializeField] public Color PanelLightColor { get; private set; } = new(1f, 0.98f, 0.94f, 1f);
        [field: SerializeField] public Color AccentColor { get; private set; } = new(1f, 0.73f, 0.16f);
        [field: SerializeField] public Color PositiveColor { get; private set; } = new(0.25f, 0.78f, 0.42f);
        [field: SerializeField] public Color NegativeColor { get; private set; } = new(0.95f, 0.36f, 0.33f);
        [field: SerializeField] public Color TextColor { get; private set; } = Color.white;
        [field: SerializeField] public Color TextDarkColor { get; private set; } = new(0.16f, 0.18f, 0.27f);
        [field: SerializeField] public Color AdColor { get; private set; } = new(0.33f, 0.52f, 1f);

        public Sprite Panel => PanelSprite != null ? PanelSprite : ProceduralSprites.RoundedRect;
        public Sprite Circle => CircleSprite != null ? CircleSprite : ProceduralSprites.Circle;
    }
}
