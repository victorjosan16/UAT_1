using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// Zona UPGRADE pentru un singur upgrade: stai pe ea și plătești nivelul următor.
    /// </summary>
    public class UpgradePad : PayZone
    {
        [SerializeField] private UpgradeDefinition upgrade;

        private int Level => UpgradeManager.Instance != null ? UpgradeManager.Instance.GetLevel(upgrade) : 0;

        protected override string SaveKey => "upg_" + upgrade.Id;
        protected override string Title => Loc.F("{0} · Nv.{1}", Loc.T(upgrade.DisplayName), Level);
        protected override long Cost => upgrade.CostForLevel(Level);
        protected override bool IsAvailable => upgrade != null && Level < upgrade.MaxLevel;

        private void OnEnable() => GameEvents.Upgraded += OnUpgraded;
        private void OnDisable() => GameEvents.Upgraded -= OnUpgraded;

        private void OnUpgraded(UpgradeDefinition changed, int _)
        {
            if (changed == upgrade) RefreshLabel();
        }

        protected override void OnFullyPaid()
        {
            if (UpgradeManager.Instance != null) UpgradeManager.Instance.LevelUp(upgrade);
        }
    }
}
