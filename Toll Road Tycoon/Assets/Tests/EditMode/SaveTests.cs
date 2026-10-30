using System;
using System.IO;
using NUnit.Framework;
using TollRoadTycoon.Core;
using TollRoadTycoon.Economy;
using TollRoadTycoon.Save;
using TollRoadTycoon.TollSystem;
using TollRoadTycoon.Upgrades;

namespace TollRoadTycoon.Tests
{
    public class SaveTests
    {
        private string _dir;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "trt-save-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        private SaveManager NewManager() => new SaveManager(Path.Combine(_dir, "save.json"), TestSerializer.Instance);

        private static GameSimulation ProgressedSim()
        {
            var sim = TestUtil.NewSim(21);
            TestUtil.GiveMoney(sim, Money.FromDollars(100_000));
            sim.Expansion.TryBuyLane();
            var b1 = sim.Expansion.TryBuyBooth();
            sim.Workers.TryHire(out var w1);
            sim.Workers.Assign(w1, b1);
            sim.Workers.TryUpgrade(w1);
            sim.Expansion.TryBuyBooth();
            for (int i = 0; i < 3; i++) sim.Upgrades.TryUpgrade(sim.Booths[2], BoothUpgradeType.Speed);
            sim.Upgrades.TryUpgrade(sim.Booths[2], BoothUpgradeType.Automation);
            sim.Upgrades.TryUpgrade(sim.Booths[0], BoothUpgradeType.Toll);
            sim.Upgrades.TryUpgrade(sim.Booths[0], BoothUpgradeType.Capacity);
            sim.Upgrades.TryUnlockVehicle("suv");
            sim.RunFor(120);
            return sim;
        }

        [Test]
        public void RoundTripRestoresProgress()
        {
            var sim = ProgressedSim();
            var manager = NewManager();
            var now = new DateTime(2026, 10, 30, 12, 0, 0, DateTimeKind.Utc);
            manager.Save(sim.CaptureSave(now, "0.1.0", new SettingsSave { Language = "en", MusicEnabled = false }));

            var loaded = manager.Load();
            Assert.AreEqual(LoadSource.Primary, manager.LastLoadSource);
            var restored = GameSimulation.FromSave(GameConfig.CreateDefault(), loaded);

            Assert.AreEqual(sim.Economy.Balance, restored.Economy.Balance);
            Assert.AreEqual(sim.Economy.TotalEarnings, restored.Economy.TotalEarnings);
            Assert.AreEqual(sim.Economy.TotalExpenses, restored.Economy.TotalExpenses);
            Assert.AreEqual(2, restored.Lanes.ActiveLaneCount);
            Assert.AreEqual(3, restored.Booths.Count);
            Assert.AreEqual(1, restored.Booths[0].TollLevel);
            Assert.AreEqual(1, restored.Booths[0].CapacityLevel);
            Assert.AreEqual(BoothMode.Automatic, restored.Booths[2].Mode);
            Assert.AreEqual(3, restored.Booths[2].SpeedLevel);
            Assert.AreEqual(2, restored.Booths[1].AssignedWorker.Level);
            Assert.AreEqual(sim.Workers.Workers.Count, restored.Workers.Workers.Count);
            Assert.IsTrue(restored.Upgrades.IsVehicleUnlocked("suv"));
            Assert.AreEqual(sim.Stats.VehiclesProcessed, restored.Stats.VehiclesProcessed);
            Assert.AreEqual("en", loaded.Settings.Language);
            Assert.IsFalse(loaded.Settings.MusicEnabled);
            Assert.AreEqual(now, DateTime.Parse(loaded.LastSeenUtc).ToUniversalTime());

            restored.RunFor(120);
            Assert.Greater(restored.Stats.VehiclesProcessed, sim.Stats.VehiclesProcessed);
        }

        [Test]
        public void ReloadingDoesNotDoubleAward()
        {
            var sim = ProgressedSim();
            var manager = NewManager();
            manager.Save(sim.CaptureSave(DateTime.UtcNow, "t", null));
            var a = GameSimulation.FromSave(GameConfig.CreateDefault(), manager.Load());
            var b = GameSimulation.FromSave(GameConfig.CreateDefault(), manager.Load());
            Assert.AreEqual(sim.Economy.Balance, a.Economy.Balance);
            Assert.AreEqual(sim.Economy.Balance, b.Economy.Balance);
            Assert.AreEqual(0, a.Traffic.ActiveVehicles.Count, "vehicles in transit are not restored");
        }

        [Test]
        public void SaveKeepsBackupAndRecoversFromCorruption()
        {
            var manager = NewManager();
            var sim = TestUtil.NewSim();
            var first = sim.CaptureSave(DateTime.UtcNow, "t", null);
            first.BalanceCents = 111;
            manager.Save(first);
            var second = sim.CaptureSave(DateTime.UtcNow, "t", null);
            second.BalanceCents = 222;
            manager.Save(second);
            Assert.IsTrue(File.Exists(manager.BackupPath));
            Assert.IsFalse(File.Exists(manager.TempPath));

            File.WriteAllText(manager.PrimaryPath, "{ this is not json");
            var loaded = manager.Load();
            Assert.AreEqual(LoadSource.Backup, manager.LastLoadSource);
            Assert.AreEqual(111, loaded.BalanceCents);
        }

        [Test]
        public void MissingSaveReturnsNull()
        {
            var manager = NewManager();
            Assert.IsNull(manager.Load());
            Assert.IsFalse(manager.HasSave);
        }

        [Test]
        public void OldVersionIsMigrated()
        {
            var manager = NewManager();
            File.WriteAllText(manager.PrimaryPath,
                "{\"Version\":0,\"BalanceCents\":5000,\"LaneCount\":0,\"Booths\":[{\"Slot\":0,\"WorkerId\":1}],\"Workers\":[{\"Id\":1,\"Level\":3}],\"Settings\":null,\"Stats\":null}");
            var data = manager.Load();
            Assert.IsNotNull(data, manager.LastError);
            Assert.AreEqual(SaveData.CurrentVersion, data.Version);
            Assert.AreEqual(1, data.LaneCount);
            Assert.IsNotNull(data.Settings);
            Assert.IsNotNull(data.Stats);
            var sim = GameSimulation.FromSave(GameConfig.CreateDefault(), data);
            Assert.AreEqual(5000, sim.Economy.Balance);
            Assert.AreEqual(3, sim.Booths[0].AssignedWorker.Level);
        }

        [Test]
        public void NewerVersionIsNotLoadedOrOverwritten()
        {
            var manager = NewManager();
            File.WriteAllText(manager.PrimaryPath, "{\"Version\":99,\"BalanceCents\":5}");
            Assert.IsNull(manager.Load());
            Assert.IsTrue(File.Exists(manager.PrimaryPath + ".v99"));
        }

        [Test]
        public void TamperedValuesAreClamped()
        {
            var data = new SaveData
            {
                BalanceCents = -500,
                LaneCount = 42,
            };
            data.Booths.Add(new BoothSave { Slot = 0, SpeedLevel = 999, WorkerId = 7 });
            data.Booths.Add(new BoothSave { Slot = 0, SpeedLevel = 1 });
            data.Workers.Add(new WorkerSave { Id = 7, Level = 500 });
            var sim = GameSimulation.FromSave(GameConfig.CreateDefault(), data);
            Assert.AreEqual(0, sim.Economy.Balance);
            Assert.AreEqual(3, sim.Lanes.ActiveLaneCount);
            Assert.AreEqual(1, sim.Booths.Count);
            Assert.AreEqual(sim.Config.BoothUpgrades.MaxSpeedLevel, sim.Booths[0].SpeedLevel);
            Assert.AreEqual(sim.Config.Workers.MaxLevel, sim.Booths[0].AssignedWorker.Level);
        }
    }
}
