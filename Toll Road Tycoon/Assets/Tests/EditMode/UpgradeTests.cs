using NUnit.Framework;
using TollRoadTycoon.Economy;
using TollRoadTycoon.TollSystem;
using TollRoadTycoon.Upgrades;

namespace TollRoadTycoon.Tests
{
    public class UpgradeTests
    {
        [Test]
        public void SpeedUpgradeShowsRealBeforeAfterAndApplies()
        {
            var sim = TestUtil.NewSim();
            var booth = sim.Booths[0];
            var preview = sim.Upgrades.GetPreview(booth, BoothUpgradeType.Speed);
            Assert.AreEqual(3.0, preview.Current, 1e-9);
            Assert.AreEqual(3.0 / 1.15, preview.Next, 1e-9);
            Assert.AreEqual(Money.FromDollars(100), preview.CostCents);

            long before = sim.Economy.Balance;
            Assert.IsTrue(sim.Upgrades.TryUpgrade(booth, BoothUpgradeType.Speed));
            Assert.AreEqual(preview.Next, booth.ProcessingSeconds, 1e-9);
            Assert.AreEqual(before - preview.CostCents, sim.Economy.Balance);
            Assert.Greater(sim.Upgrades.GetPreview(booth, BoothUpgradeType.Speed).CostCents, preview.CostCents);
        }

        [Test]
        public void FailedPurchaseChangesNothing()
        {
            var sim = TestUtil.NewSim();
            sim.Economy.TrySpendMoney(sim.Economy.Balance, "drain");
            var booth = sim.Booths[0];
            Assert.IsFalse(sim.Upgrades.TryUpgrade(booth, BoothUpgradeType.Toll));
            Assert.AreEqual(0, booth.TollLevel);
            Assert.AreEqual(0, sim.Economy.Balance);
        }

        [Test]
        public void UpgradesStopAtMaxLevel()
        {
            var sim = TestUtil.NewSim();
            TestUtil.GiveMoney(sim, Money.FromDollars(100_000_000));
            var booth = sim.Booths[0];
            int bought = 0;
            while (sim.Upgrades.TryUpgrade(booth, BoothUpgradeType.Capacity)) bought++;
            Assert.AreEqual(sim.Config.BoothUpgrades.MaxCapacityLevel, bought);
            Assert.AreEqual(8, booth.QueueCapacity);
            Assert.IsTrue(sim.Upgrades.GetPreview(booth, BoothUpgradeType.Capacity).IsMaxed);
        }

        [Test]
        public void TollUpgradeIncreasesIncome()
        {
            var basic = TestUtil.NewSim(11);
            var upgraded = TestUtil.NewSim(11);
            TestUtil.GiveMoney(upgraded, Money.FromDollars(10_000));
            for (int i = 0; i < 4; i++) upgraded.Upgrades.TryUpgrade(upgraded.Booths[0], BoothUpgradeType.Toll);
            long e0 = basic.Economy.TotalEarnings;
            long e1 = upgraded.Economy.TotalEarnings;
            basic.RunFor(300);
            upgraded.RunFor(300);
            // Same seed, same traffic: x2.0 toll multiplier doubles income.
            Assert.AreEqual((basic.Economy.TotalEarnings - e0) * 2, upgraded.Economy.TotalEarnings - e1);
        }

        [Test]
        public void AutomationRequiresSpeedLevelAndFreesTheWorker()
        {
            var sim = TestUtil.NewSim();
            TestUtil.GiveMoney(sim, Money.FromDollars(1_000_000));
            var booth = sim.Booths[0];
            var worker = booth.AssignedWorker;
            Assert.IsTrue(sim.Upgrades.GetPreview(booth, BoothUpgradeType.Automation).IsLocked);
            Assert.IsFalse(sim.Upgrades.TryUpgrade(booth, BoothUpgradeType.Automation));
            for (int i = 0; i < 3; i++) sim.Upgrades.TryUpgrade(booth, BoothUpgradeType.Speed);
            Assert.IsTrue(sim.Upgrades.TryUpgrade(booth, BoothUpgradeType.Automation));
            Assert.AreEqual(BoothMode.Automatic, booth.Mode);
            Assert.IsNull(booth.AssignedWorker);
            Assert.IsNull(worker.AssignedBooth);
            sim.RunFor(120);
            Assert.Greater(booth.VehiclesServed, 10);
        }

        [Test]
        public void UnlockingVehicleTypeAddsItToTraffic()
        {
            var sim = TestUtil.NewSim(8);
            TestUtil.GiveMoney(sim, Money.FromDollars(10_000));
            int trucks = 0;
            sim.Traffic.VehicleSpawned += v =>
            {
                if (v.Type.Id == "truck") trucks++;
            };
            sim.RunFor(120);
            Assert.AreEqual(0, trucks);
            Assert.IsTrue(sim.Upgrades.TryUnlockVehicle("truck"));
            Assert.IsFalse(sim.Upgrades.TryUnlockVehicle("truck"));
            sim.RunFor(300);
            Assert.Greater(trucks, 0);
        }
    }
}
