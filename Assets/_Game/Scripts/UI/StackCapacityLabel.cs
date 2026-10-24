using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>Eticheta „7/10” sau „PLIN” deasupra jucătorului.</summary>
    public class StackCapacityLabel : MonoBehaviour
    {
        [SerializeField] private ResourceStack stack;
        [SerializeField] private WorldLabel label;

        private void OnEnable() => stack.Changed += Refresh;

        // În Start, nu în OnEnable: WorldLabel trebuie să se fi construit deja (în Awake-ul lui).
        private void Start() => Refresh(stack);

        private void OnDisable() => stack.Changed -= Refresh;

        private void Refresh(ResourceStack _)
        {
            if (label == null) return;
            label.SetVisible(stack.Count > 0);
            bool full = stack.Count >= stack.Capacity;
            label.SetText(full ? "PLIN" : $"{stack.Count}/{stack.Capacity}");
            label.SetTextColor(full ? new Color(1f, 0.35f, 0.3f) : Color.white);
        }
    }
}
