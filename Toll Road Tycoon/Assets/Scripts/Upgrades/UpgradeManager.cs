using System;
using System.Collections.Generic;
using TollRoadTycoon.Core;
using TollRoadTycoon.Economy;
using TollRoadTycoon.TollSystem;
using TollRoadTycoon.Workers;

namespace TollRoadTycoon.Upgrades
{
    public enum BoothUpgradeType
    {
        Speed,
        Toll,
        Capacity,
        Barrier,
        Automation,
    }

    /// <summary>Current and next value of an upgrade, for "before -> after" UI.</summary>
    public struct UpgradePreview
    {
        public BoothUpgradeType Type;
        public int Level;
        public int MaxLevel;
        public double Current;
        public double Next;
        public long CostCents;
        public bool IsMaxed;
        public bool IsLocked;
        public string LockReasonKey;
    }

    /// <summary>Booth upgrades and vehicle type unlocks. Purchases are transactional.</summary>
    public sealed class UpgradeManager
    {
        private readonly GameConfig _config;
        private readonly EconomyManager _economy;
        private readonly WorkerManager _workers;
        private readonly HashSet<string> _unlockedVehicles;

        public event Action Changed;

        public UpgradeManager(GameConfig config, EconomyManager economy, WorkerManager workers, HashSet<string> unlockedVehicles)
        {
            _config = config;
            _economy = economy;
            _workers = workers;
            _unlockedVehicles = unlockedVehicles;
        }

        private BoothUpgradeConfig Cfg => _config.BoothUpgrades;

        public UpgradePreview GetPreview(TollBooth booth, BoothUpgradeType type)
        {
            var p = new UpgradePreview { Type = type };
            switch (type)
            {
                case BoothUpgradeType.Speed:
                    p.Level = booth.SpeedLevel;
                    p.MaxLevel = Cfg.MaxSpeedLevel;
                    p.Current = booth.ProcessingSeconds;
                    p.Next = booth.ProcessingSeconds / (1 + Cfg.SpeedBonusPerLevel * (booth.SpeedLevel + 1)) * (1 + Cfg.SpeedBonusPerLevel * booth.SpeedLevel);
                    p.CostCents = Cfg.SpeedCost.CostForLevel(booth.SpeedLevel);
                    break;
                case BoothUpgradeType.Toll:
                    p.Level = booth.TollLevel;
                    p.MaxLevel = Cfg.MaxTollLevel;
                    p.Current = booth.TollMultiplierBasisPoints / 10000.0;
                    p.Next = (booth.TollMultiplierBasisPoints + Cfg.TollBasisPointsPerLevel) / 10000.0;
                    p.CostCents = Cfg.TollCost.CostForLevel(booth.TollLevel);
                    break;
                case BoothUpgradeType.Capacity:
                    p.Level = booth.CapacityLevel;
                    p.MaxLevel = Cfg.MaxCapacityLevel;
                    p.Current = booth.QueueCapacity;
                    p.Next = booth.QueueCapacity + 1;
                    p.CostCents = Cfg.CapacityCost.CostForLevel(booth.CapacityLevel);
                    break;
                case BoothUpgradeType.Barrier:
                    p.Level = booth.BarrierLevel;
                    p.MaxLevel = Cfg.MaxBarrierLevel;
                    p.Current = booth.BarrierSeconds;
                    p.Next = Cfg.BaseBarrierSeconds / (1 + Cfg.BarrierBonusPerLevel * (booth.BarrierLevel + 1));
                    p.CostCents = Cfg.BarrierCost.CostForLevel(booth.BarrierLevel);
                    break;
                case BoothUpgradeType.Automation:
                    bool auto = booth.Mode == BoothMode.Automatic;
                    p.Level = auto ? 1 : 0;
                    p.MaxLevel = 1;
                    p.Current = booth.ProcessingSeconds;
                    p.Next = Cfg.AutomaticProcessingSeconds * booth.SpeedMultiplier;
                    p.CostCents = Cfg.AutomationCostCents;
                    if (!auto && booth.SpeedLevel < Cfg.AutomationRequiredSpeedLevel)
                    {
                        p.IsLocked = true;
                        p.LockReasonKey = "upgrade.automation.locked";
                    }
                    break;
            }
            p.IsMaxed = p.Level >= p.MaxLevel;
            return p;
        }

        public bool CanBuy(TollBooth booth, BoothUpgradeType type)
        {
            var p = GetPreview(booth, type);
            return !p.IsMaxed && !p.IsLocked && _economy.CanAfford(p.CostCents);
        }

        public bool TryUpgrade(TollBooth booth, BoothUpgradeType type)
        {
            var p = GetPreview(booth, type);
            if (p.IsMaxed || p.IsLocked) return false;
            if (!_economy.TrySpendMoney(p.CostCents, "booth." + type)) return false;
            switch (type)
            {
                case BoothUpgradeType.Speed: booth.SpeedLevel++; break;
                case BoothUpgradeType.Toll: booth.TollLevel++; break;
                case BoothUpgradeType.Capacity: booth.CapacityLevel++; break;
                case BoothUpgradeType.Barrier: booth.BarrierLevel++; break;
                case BoothUpgradeType.Automation:
                    booth.Mode = BoothMode.Automatic;
                    _workers.ReleaseForAutomation(booth);
                    break;
            }
            booth.NotifyChanged();
            Changed?.Invoke();
            return true;
        }

        public bool IsVehicleUnlocked(string id) => _unlockedVehicles.Contains(id);

        public bool TryUnlockVehicle(string id)
        {
            var type = _config.FindVehicleType(id);
            if (type == null || _unlockedVehicles.Contains(id)) return false;
            if (!_economy.TrySpendMoney(type.UnlockPriceCents, "vehicle." + id)) return false;
            _unlockedVehicles.Add(id);
            Changed?.Invoke();
            return true;
        }
    }
}
