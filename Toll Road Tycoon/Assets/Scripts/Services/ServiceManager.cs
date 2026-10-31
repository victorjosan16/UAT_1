using System;
using System.Collections.Generic;
using TollRoadTycoon.Core;
using TollRoadTycoon.Economy;

namespace TollRoadTycoon.Services
{
    public sealed class PlacedService
    {
        public ServiceConfig Config;
        public int Slot;
        public int Level = 1;
        /// <summary>Simulation times at which the current customers finish.</summary>
        public readonly List<double> Customers = new List<double>();
        public long Earned;
        public long CustomersServed;
    }

    public enum PlacementResult
    {
        Ok,
        NotAllowed,
        SlotTaken,
        InvalidSlot,
        NotEnoughMoney,
        MaxLevel,
        NotFound,
    }

    /// <summary>
    /// Roadside services of one road section. Income is event driven: a vehicle that has just paid
    /// the toll may stop at a service with free capacity; the service is paid when that customer
    /// finishes. Nothing is earned per frame.
    /// </summary>
    public sealed class ServiceManager
    {
        private readonly ProgressionConfig _progression;
        private readonly RoadSectionConfig _section;
        private readonly EconomyManager _economy;
        private readonly GameStats _stats;
        private readonly SimRandom _random;
        private readonly List<PlacedService> _placed = new List<PlacedService>();

        public IReadOnlyList<PlacedService> Placed => _placed;
        public RoadSectionConfig Section => _section;

        public event Action Changed;
        /// <summary>Service and amount paid by a finished customer.</summary>
        public event Action<PlacedService, long> CustomerPaid;

        public ServiceManager(ProgressionConfig progression, RoadSectionConfig section, EconomyManager economy, GameStats stats, SimRandom random)
        {
            _progression = progression;
            _section = section;
            _economy = economy;
            _stats = stats;
            _random = random;
        }

        public int SlotCount => _progression.ServiceSlots.Count;

        public PlacedService At(int slot) => _placed.Find(p => p.Slot == slot);

        public bool IsSlotFree(int slot) => slot >= 0 && slot < SlotCount && At(slot) == null;

        public long BuildCost(ServiceConfig config) => RoundDollars(config.BaseCostCents * _section.PriceMultiplier);

        public long UpgradeCost(PlacedService s) => RoundDollars(s.Config.UpgradeCost.CostForLevel(s.Level - 1) * _section.PriceMultiplier);

        /// <summary>Money a finished customer pays at the current level (scaled to the section's tolls).</summary>
        public long SpendPerCustomer(PlacedService s)
        {
            double spend = s.Config.SpendCents * _section.TollMultiplier * (1 + s.Config.SpendBonusPerLevel * (s.Level - 1));
            return RoundCents(spend);
        }

        public int CapacityOf(PlacedService s) => s.Config.Capacity + (s.Level - 1) / 2;

        public PlacementResult CanBuild(string serviceId, int slot)
        {
            var config = _progression.FindService(serviceId);
            if (config == null || !_section.AllowsService(serviceId)) return PlacementResult.NotAllowed;
            if (slot < 0 || slot >= SlotCount) return PlacementResult.InvalidSlot;
            if (!IsSlotFree(slot)) return PlacementResult.SlotTaken;
            if (!_economy.CanAfford(BuildCost(config))) return PlacementResult.NotEnoughMoney;
            return PlacementResult.Ok;
        }

        public PlacementResult Build(string serviceId, int slot)
        {
            var result = CanBuild(serviceId, slot);
            if (result != PlacementResult.Ok) return result;
            var config = _progression.FindService(serviceId);
            if (!_economy.TrySpendMoney(BuildCost(config), "service." + serviceId)) return PlacementResult.NotEnoughMoney;
            _placed.Add(new PlacedService { Config = config, Slot = slot });
            _stats.ServicesBuilt++;
            Changed?.Invoke();
            return PlacementResult.Ok;
        }

        public PlacementResult Upgrade(int slot)
        {
            var s = At(slot);
            if (s == null) return PlacementResult.NotFound;
            if (s.Level >= s.Config.MaxLevel) return PlacementResult.MaxLevel;
            if (!_economy.TrySpendMoney(UpgradeCost(s), "service.upgrade")) return PlacementResult.NotEnoughMoney;
            s.Level++;
            Changed?.Invoke();
            return PlacementResult.Ok;
        }

        /// <summary>Moves a building to a free slot. Current customers keep being served.</summary>
        public PlacementResult Move(int fromSlot, int toSlot)
        {
            var s = At(fromSlot);
            if (s == null) return PlacementResult.NotFound;
            if (toSlot < 0 || toSlot >= SlotCount) return PlacementResult.InvalidSlot;
            if (!IsSlotFree(toSlot)) return PlacementResult.SlotTaken;
            s.Slot = toSlot;
            Changed?.Invoke();
            return PlacementResult.Ok;
        }

        /// <summary>Half of the build price is refunded; customers inside are paid first.</summary>
        public long RemoveRefund(PlacedService s) => BuildCost(s.Config) / 2 / Money.CentsPerDollar * Money.CentsPerDollar;

        public PlacementResult Remove(int slot, double now)
        {
            var s = At(slot);
            if (s == null) return PlacementResult.NotFound;
            foreach (var _ in s.Customers.ToArray()) PayCustomer(s);
            s.Customers.Clear();
            _placed.Remove(s);
            _economy.AddMoney(RemoveRefund(s), MoneySource.Reward);
            Changed?.Invoke();
            return PlacementResult.Ok;
        }

        /// <summary>Called for every vehicle that paid a toll on this section.</summary>
        public void OnVehiclePaid(VehicleTypeConfig type, double now)
        {
            if (_placed.Count == 0 || type.IsEmergency) return;
            // Visit at most one service, starting at a random one so all get customers.
            int start = _random.Range(0, _placed.Count);
            for (int k = 0; k < _placed.Count; k++)
            {
                var s = _placed[(start + k) % _placed.Count];
                double chance = type.IsHeavy ? s.Config.HeavyVisitChance : s.Config.VisitChance;
                if (_random.NextDouble() >= chance) continue;
                if (s.Customers.Count >= CapacityOf(s)) continue;
                s.Customers.Add(now + s.Config.ServiceSeconds);
                return;
            }
        }

        public void Tick(double now)
        {
            foreach (var s in _placed)
            {
                for (int i = s.Customers.Count - 1; i >= 0; i--)
                {
                    if (s.Customers[i] > now) continue;
                    s.Customers.RemoveAt(i);
                    PayCustomer(s);
                }
            }
        }

        private void PayCustomer(PlacedService s)
        {
            long cents = SpendPerCustomer(s);
            s.Earned += cents;
            s.CustomersServed++;
            _stats.ServiceCustomers++;
            _stats.ServiceIncomeCents += cents;
            _economy.AddMoney(cents, MoneySource.Service);
            CustomerPaid?.Invoke(s, cents);
        }

        /// <summary>Expected service income per second for a given rate of paying vehicles.</summary>
        public double EstimateIncomePerSecond(double vehiclesPerSecond, double heavyFraction)
        {
            double total = 0;
            double remaining = vehiclesPerSecond;
            foreach (var s in _placed)
            {
                double chance = heavyFraction * s.Config.HeavyVisitChance + (1 - heavyFraction) * s.Config.VisitChance;
                double visits = Math.Min(remaining * chance, CapacityOf(s) / s.Config.ServiceSeconds);
                remaining = Math.Max(0, remaining - visits);
                total += visits * SpendPerCustomer(s);
            }
            return total;
        }

        /// <summary>Restores a building from a save without charging (values clamped).</summary>
        public bool Restore(string serviceId, int slot, int level)
        {
            var config = _progression.FindService(serviceId);
            if (config == null || !_section.AllowsService(serviceId) || !IsSlotFree(slot)) return false;
            _placed.Add(new PlacedService { Config = config, Slot = slot, Level = Math.Max(1, Math.Min(level, config.MaxLevel)) });
            return true;
        }

        private static long RoundDollars(double cents) => Math.Max(Money.CentsPerDollar, (long)Math.Round(cents / Money.CentsPerDollar) * Money.CentsPerDollar);
        private static long RoundCents(double cents) => (long)Math.Round(cents);
    }
}
