using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>Culorile și sprite-urile interfeței, într-un singur loc (ușor de înlocuit cu grafica ta).</summary>
    [CreateAssetMenu(fileName = "UiTheme", menuName = "Quarry Empire/UI Theme")]
    public class UiTheme : ScriptableObject
    {
        [field: SerializeField] public Sprite PanelSprite { get; private set; }
        [field: SerializeField] public Sprite CircleSprite { get; private set; }
        [field: SerializeField] public Color PanelColor { get; private set; } = new(0.12f, 0.14f, 0.19f, 0.88f);
        [field: SerializeField] public Color AccentColor { get; private set; } = new(1f, 0.69f, 0.13f);
        [field: SerializeField] public Color PositiveColor { get; private set; } = new(0.24f, 0.75f, 0.38f);
        [field: SerializeField] public Color NegativeColor { get; private set; } = new(0.9f, 0.3f, 0.25f);
        [field: SerializeField] public Color TextColor { get; private set; } = Color.white;
        [field: SerializeField] public Color AdColor { get; private set; } = new(0.36f, 0.45f, 0.95f);
    }
}
