using NUnit.Framework;
using TollRoadTycoon.Core;
using TollRoadTycoon.Economy;
using TollRoadTycoon.TollSystem;
using TollRoadTycoon.Upgrades;

namespace TollRoadTycoon.Tests
{
    public class PaymentTests
    {
        [Test]
        public void DuplicatePaymentCallsAwardOnce()
        {
            var sim = TestUtil.NewSim(2, c => c.Traffic.BaseArrivalsPerSecond = 0);
            var car = sim.Traffic.Spawn(sim.Config.FindVehicleType("compact"), sim.Lanes.Approach(0), true, 0, 0);
            long before = sim.Economy.Balance;
            Assert.IsTrue(sim.Payments.TryProcess(car, sim.Booths[0], out long cents));
            Assert.IsFalse(sim.Payments.TryProcess(car, sim.Booths[0], out _));
            car.HasPaid = false; // even if a flag were reset, the trip id is remembered
            Assert.IsFalse(sim.Payments.TryProcess(car, sim.Booths[0], out _));
            Assert.AreEqual(Money.FromDollars(5), cents);
            Assert.AreEqual(before + cents, sim.Economy.Balance);
        }

        [Test]
        public void EarningsMatchProcessedVehiclesExactly()
        {
            var sim = TestUtil.NewSim(4);
            TestUtil.GiveMoney(sim, Money.FromDollars(100_000));
            sim.Expansion.TryBuyLane();
            sim.Expansion.TryBuyBooth();
            sim.Workers.TryHire(out var w);
            sim.Workers.Assign(w, sim.Booths[1]);
            foreach (var t in sim.Config.VehicleTypes) sim.Upgrades.TryUnlockVehicle(t.Id);
            sim.Upgrades.TryUpgrade(sim.Booths[0], BoothUpgradeType.Toll);

            long expected = 0;
            int collected = 0;
            sim.Payments.TollCollected += (booth, vehicle, cents) =>
            {
                collected++;
                expected += PaymentProcessor.ComputeToll(vehicle.Type, booth, 10000);
            };
            long earningsBefore = sim.Economy.TotalEarnings;
            sim.RunFor(400);

            Assert.Greater(collected, 100);
            Assert.AreEqual(expected, sim.Economy.TotalEarnings - earningsBefore);
            Assert.AreEqual(collected, sim.Stats.VehiclesProcessed + sim.Stats.EmergencyVehiclesPassed);
            long served = 0;
            foreach (var b in sim.Booths) served += b.VehiclesServed;
            Assert.LessOrEqual(served, collected);
            Assert.GreaterOrEqual(served, collected - sim.Booths.Count);
        }

        [Test]
        public void EmergencyVehiclesPassFree()
        {
            var sim = TestUtil.NewSim(2, c => c.Traffic.BaseArrivalsPerSecond = 0);
            long before = sim.Economy.Balance;
            sim.Traffic.Spawn(sim.Config.FindVehicleType("emergency"), sim.Lanes.Approach(0), true, 0, 0);
            sim.RunFor(30);
            Assert.AreEqual(before, sim.Economy.Balance);
            Assert.AreEqual(1, sim.Stats.EmergencyVehiclesPassed);
            Assert.AreEqual(1, sim.Booths[0].VehiclesServed);
        }

        [Test]
        public void TollFormulaAppliesBoothAndBonusMultipliers()
        {
            var sim = TestUtil.NewSim();
            var booth = sim.Booths[0];
            TestUtil.GiveMoney(sim, Money.FromDollars(10_000));
            sim.Upgrades.TryUpgrade(booth, BoothUpgradeType.Toll);
            sim.Upgrades.TryUpgrade(booth, BoothUpgradeType.Toll);
            var truck = sim.Config.FindVehicleType("truck");
            // $25 * 1.5 * 2.0
            Assert.AreEqual(Money.FromDollars(75), PaymentProcessor.ComputeToll(truck, booth, 20000));
        }
    }
}
