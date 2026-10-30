using System.Collections.Generic;
using NUnit.Framework;
using TollRoadTycoon.Core;
using TollRoadTycoon.Economy;
using TollRoadTycoon.RoadExpansion;
using TollRoadTycoon.TollSystem;
using TollRoadTycoon.Upgrades;
using TollRoadTycoon.Workers;

namespace TollRoadTycoon.Tests
{
    /// <summary>
    /// Plays the default economy with a simple bot that checks every 10 seconds: it saves for the next
    /// booth/lane when that costs less than ~6 minutes of income, otherwise buys the cheapest upgrade.
    /// Checks that progression milestones are reachable in sane times.
    /// </summary>
    public class BalanceTests
    {
        [Test]
        public void GreedyPlayerProgressesAtAReasonablePace()
        {
            var sim = TestUtil.NewSim(42);
            var milestones = new Dictionary<string, double>();
            void Mark(string key)
            {
                if (!milestones.ContainsKey(key)) milestones[key] = sim.Time / 60.0;
            }

            for (int tick = 0; tick < 12 * 60 * 6 && milestones.Count < 8; tick++)
            {
                sim.RunFor(10);
                if (sim.Stats.VehiclesProcessed >= 10) Mark("process 10 cars");
                if (sim.Economy.TotalEarnings >= Money.FromDollars(100)) Mark("earn $100");
                while (BuyCheapest(sim)) { }
                if (sim.Booths[0].SpeedLevel >= 1) Mark("speed upgrade");
                if (sim.Booths.Count >= 2) Mark("booth 2");
                if (sim.Upgrades.IsVehicleUnlocked("suv")) Mark("unlock SUV");
                if (sim.Booths.Count >= 3) Mark("booth 3");
                if (sim.Upgrades.IsVehicleUnlocked("truck")) Mark("unlock trucks");
                if (sim.Booths.Count >= 4) Mark("booth 4");
            }

            foreach (var kv in milestones) TestContext.WriteLine($"{kv.Key,-16} at {kv.Value,6:0.0} min");
            TestContext.WriteLine($"income/min at end: {Money.Format(sim.Economy.GetIncomePerMinute())}, booths {sim.Booths.Count}, lanes {sim.Lanes.ActiveLaneCount}");

            Assert.Less(milestones["process 10 cars"], 2.0);
            Assert.Less(milestones["speed upgrade"], 3.0);
            Assert.Less(milestones["booth 2"], 15.0);
            Assert.That(milestones.ContainsKey("booth 4"), "4 booths should be reachable within 12 hours of play");
            Assert.Less(milestones["booth 4"], 90.0);
        }

        /// <summary>Buys one thing. Returns false when nothing was bought.</summary>
        private static bool BuyCheapest(GameSimulation sim)
        {
            long income = System.Math.Max(Money.FromDollars(1), sim.Economy.GetIncomePerMinute());
            var block = sim.Expansion.CanBuyBooth();
            if (block != ExpansionBlock.MaxReached)
            {
                bool needsLane = block == ExpansionBlock.NeedsMoreLanes;
                long cost = needsLane ? sim.Expansion.NextLaneCost : sim.Expansion.NextBoothCost + sim.Workers.HireCost;
                if (needsLane && sim.Expansion.CanBuyLane() == ExpansionBlock.MaxReached) cost = long.MaxValue;
                if (cost <= income * 6)
                {
                    if (!sim.Economy.CanAfford(cost)) return false;
                    if (needsLane) return sim.Expansion.TryBuyLane();
                    var booth = sim.Expansion.TryBuyBooth();
                    if (booth != null && booth.AssignedWorker == null && sim.Workers.TryHire(out var hired)) sim.Workers.Assign(hired, booth);
                    return booth != null;
                }
            }

            var options = new List<(long cost, System.Func<bool> buy)>();
            foreach (var booth in sim.Booths)
            {
                foreach (BoothUpgradeType type in System.Enum.GetValues(typeof(BoothUpgradeType)))
                {
                    var p = sim.Upgrades.GetPreview(booth, type);
                    if (!p.IsMaxed && !p.IsLocked) options.Add((p.CostCents, () => sim.Upgrades.TryUpgrade(booth, type)));
                }
                if (booth.Mode == BoothMode.Manual && booth.AssignedWorker == null)
                {
                    options.Add((sim.Workers.HireCost, () =>
                        sim.Workers.TryHire(out var w) && sim.Workers.Assign(w, booth) == AssignResult.Assigned));
                }
                var worker = booth.AssignedWorker;
                if (worker != null && sim.Workers.CanUpgrade(worker))
                {
                    options.Add((sim.Workers.UpgradeCost(worker), () => sim.Workers.TryUpgrade(worker)));
                }
            }
            if (sim.Expansion.CanBuyBooth() != ExpansionBlock.MaxReached && sim.Expansion.CanBuyBooth() != ExpansionBlock.NeedsMoreLanes)
            {
                options.Add((sim.Expansion.NextBoothCost, () => sim.Expansion.TryBuyBooth() != null));
            }
            if (sim.Expansion.CanBuyLane() != ExpansionBlock.MaxReached && sim.Expansion.CanBuyBooth() == ExpansionBlock.NeedsMoreLanes)
            {
                options.Add((sim.Expansion.NextLaneCost, () => sim.Expansion.TryBuyLane()));
            }
            foreach (var type in sim.Config.VehicleTypes)
            {
                if (!sim.Upgrades.IsVehicleUnlocked(type.Id)) options.Add((type.UnlockPriceCents, () => sim.Upgrades.TryUnlockVehicle(type.Id)));
            }

            options.Sort((a, b) => a.cost.CompareTo(b.cost));
            if (options.Count == 0 || !sim.Economy.CanAfford(options[0].cost)) return false;
            return options[0].buy();
        }
    }
}
