using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace QuarryEmpire
{
    /// <summary>
    /// TEMPORAR (Etapa 1): afișează pe ecran câte resurse de fiecare tip au fost colectate.
    /// Ascultă doar GameEvents, deci nu depinde de jucător. În Etapa 2 vine UI-ul real.
    /// Folosește Text-ul simplu din uGUI, care nu cere importul TMP Essentials (build în cloud).
    /// </summary>
    public class ResourceCounterDebug : MonoBehaviour
    {
        [SerializeField] private Text label;

        private readonly Dictionary<ResourceData, int> _counts = new();
        private readonly StringBuilder _builder = new();

        private void OnEnable()
        {
            GameEvents.ResourceCollected += HandleCollected;
            Refresh();
        }

        private void OnDisable() => GameEvents.ResourceCollected -= HandleCollected;

        private void HandleCollected(ResourceData resource, Vector3 _)
        {
            if (resource == null) return;
            _counts.TryGetValue(resource, out int current);
            _counts[resource] = current + 1;
            Refresh();
        }

        private void Refresh()
        {
            _builder.Clear();
            _builder.AppendLine("Resurse colectate:");
            foreach (var pair in _counts)
                _builder.Append(pair.Key.DisplayName).Append(": ").Append(pair.Value).AppendLine();
            label.text = _builder.ToString();
        }
    }
}
