using System.Collections.Generic;
using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// Ține nivelurile upgrade-urilor (din salvare) și calculează bonusurile totale pe tip.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public class UpgradeManager : MonoBehaviour
    {
        public static UpgradeManager Instance { get; private set; }

        [SerializeField] private List<UpgradeDefinition> upgrades = new();

        public IReadOnlyList<UpgradeDefinition> Upgrades => upgrades;

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public int GetLevel(UpgradeDefinition upgrade)
        {
            if (upgrade == null || SaveSystem.Instance == null) return 0;
            return SaveData.GetInt(SaveSystem.Instance.Data.upgradeLevels, upgrade.Id);
        }

        public bool IsMaxed(UpgradeDefinition upgrade) => GetLevel(upgrade) >= upgrade.MaxLevel;

        /// <summary>Crește nivelul (plata s-a făcut deja în zona de upgrade).</summary>
        public void LevelUp(UpgradeDefinition upgrade)
        {
            if (upgrade == null || SaveSystem.Instance == null || IsMaxed(upgrade)) return;
            int level = GetLevel(upgrade) + 1;
            SaveData.SetInt(SaveSystem.Instance.Data.upgradeLevels, upgrade.Id, level);
            SaveSystem.Instance.MarkDirty();
            GameEvents.RaiseUpgraded(upgrade, level);
            GameEvents.RaiseToast($"{upgrade.DisplayName} → nivel {level}");
        }

        /// <summary>Suma bonusurilor pentru un tip (toate upgrade-urile de acel tip).</summary>
        public float GetTotalBonus(UpgradeType type)
        {
            float total = 0f;
            foreach (var upgrade in upgrades)
                if (upgrade != null && upgrade.Type == type) total += GetLevel(upgrade) * upgrade.ValuePerLevel;
            return total;
        }
    }
}
