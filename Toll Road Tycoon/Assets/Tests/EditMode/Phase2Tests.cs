using System;
using System.IO;
using NUnit.Framework;
using TollRoadTycoon.Core;
using TollRoadTycoon.Economy;
using TollRoadTycoon.Missions;
using TollRoadTycoon.Monetization;
using TollRoadTycoon.Save;
using TollRoadTycoon.Services;
using TollRoadTycoon.Upgrades;

namespace TollRoadTycoon.Tests
{
    public class SectionTests
    {
        private static TycoonGame NewGame(IAdService ads = null) => new TycoonGame(GameConfig.CreateDefault(), ads, 5);

        private static void Give(TycoonGame g, long dollars) => g.Economy.AddMoney(Money.FromDollars(dollars), MoneySource.Debug);

        private static void BuildBooths(TycoonGame g, int count)
        {
            var sim = g.Active;
            while (sim.Booths.Count < count)
            {
                if (sim.Expansion.CanBuyBooth() == RoadExpansion.ExpansionBlock.NeedsMoreLanes) Assert.IsTrue(sim.Expansion.TryBuyLane());
                var b = sim.Expansion.TryBuyBooth();
                Assert.IsNotNull(b);
                if (b.AssignedWorker == null)
                {
                    Assert.IsTrue(sim.Workers.TryHire(out var w));
                    sim.Workers.Assign(w, b);
                }
            }
        }

        [Test]
        public void FiveSectionsWithDistinctProgression()
        {
            var g = NewGame();
            Assert.AreEqual(5, g.Sections.Count);
            for (int i = 1; i < 5; i++)
            {
                var a = g.Sections[i - 1];
                var b = g.Sections[i];
                Assert.Greater(b.Config.UnlockPriceCents, a.Config.UnlockPriceCents);
                Assert.Greater(b.RoadConfig.Expansion.BoothSlotPrices[1], a.RoadConfig.Expansion.BoothSlotPrices[1]);
                Assert.Greater(b.RoadConfig.FindVehicleType("compact").TollCents, a.RoadConfig.FindVehicleType("compact").TollCents);
                Assert.Greater(b.Config.KmFrom, a.Config.KmFrom);
            }
            Assert.AreEqual(4, g.Sections[0].RoadConfig.MaxBooths);
            Assert.AreEqual(6, g.Sections[4].RoadConfig.MaxBooths);
            Assert.AreEqual(0, g.Sections[0].RoadConfig.FindVehicleType("heavy_truck").SpawnWeight);
            Assert.Greater(g.Sections[2].RoadConfig.FindVehicleType("heavy_truck").SpawnWeight, 0);
        }

        [Test]
        public void UnlockRequiresBoothsAndMoney()
        {
            var g = NewGame();
            Assert.AreEqual(SectionBlock.NeedsBooths, g.CanUnlock(1));
            Assert.AreEqual(SectionBlock.PreviousLocked, g.CanUnlock(2));
            Give(g, 100_000);
            BuildBooths(g, 3);
            Assert.AreEqual(SectionBlock.None, g.CanUnlock(1));
            long before = g.Economy.Balance;
            Assert.IsTrue(g.Unlock(1));
            Assert.AreEqual(before - g.Sections[1].Config.UnlockPriceCents, g.Economy.Balance);
            Assert.IsFalse(g.Unlock(1));
            Assert.AreEqual(1, g.Stats.SectionsUnlocked);
        }

        [Test]
        public void SwitchingKeepsEachRoadsProgress()
        {
            var g = NewGame();
            Give(g, 100_000);
            BuildBooths(g, 3);
            g.Active.Upgrades.TryUpgrade(g.Active.Booths[0], BoothUpgradeType.Toll);
            Assert.IsTrue(g.Unlock(1));
            Assert.IsTrue(g.SwitchSection(1));
            Assert.AreEqual(1, g.Active.Booths.Count);
            Assert.AreEqual(g.Sections[1].RoadConfig, g.Active.Config);
            Assert.IsTrue(g.SwitchSection(0));
            Assert.AreEqual(3, g.Active.Booths.Count);
            Assert.AreEqual(1, g.Active.Booths[0].TollLevel);
            Assert.IsFalse(g.SwitchSection(3));
        }

        [Test]
        public void InactiveRoadsEarnEstimateAndActiveRoadOnlyRealTolls()
        {
            var g = NewGame();
            Give(g, 100_000);
            BuildBooths(g, 3);
            Assert.IsTrue(g.Unlock(1));
            g.RunFor(60);
            // Active road 0: every toll is a real payment; road 1 is inactive and earns its estimate.
            long tollsBefore = g.Stats.TollCentsCollected;
            long passiveBefore = g.Stats.PassiveIncomeCents;
            long earningsBefore = g.Economy.TotalEarnings;
            g.RunFor(120);
            long tolls = g.Stats.TollCentsCollected - tollsBefore;
            long passive = g.Stats.PassiveIncomeCents - passiveBefore;
            Assert.Greater(passive, 0);
            Assert.AreEqual(tolls + passive, g.Economy.TotalEarnings - earningsBefore, "no other income source, nothing counted twice");
            double expected = g.EstimateIncomePerSecond(g.Sections[1]) * g.Progression.EstimateSafetyFactor * 120;
            Assert.AreEqual(expected, passive, expected * 0.02 + 2);
        }

        [TestCase(1, false)]
        [TestCase(2, false)]
        [TestCase(4, true)]
        public void EstimateMatchesSimulation(int booths, bool upgraded)
        {
            var g = NewGame();
            Give(g, 1_000_000);
            BuildBooths(g, booths);
            if (upgraded)
            {
                foreach (var b in g.Active.Booths)
                {
                    g.Active.Upgrades.TryUpgrade(b, BoothUpgradeType.Speed);
                    g.Active.Upgrades.TryUpgrade(b, BoothUpgradeType.Toll);
                }
                g.Active.Upgrades.TryUnlockVehicle("suv");
                g.Active.Upgrades.TryUnlockVehicle("van");
            }
            g.RunFor(60);
            long before = g.Stats.TollCentsCollected;
            g.RunFor(600);
            double measured = (g.Stats.TollCentsCollected - before) / 600.0;
            double estimate = SectionEstimator.Estimate(g.Active).TollCentsPerSecond;
            TestContext.WriteLine($"booths={booths} measured={measured:0} c/s estimate={estimate:0} c/s");
            Assert.AreEqual(measured, estimate, measured * 0.2);
            // With the safety factor the estimate does not exceed what playing earns.
            Assert.LessOrEqual(estimate * g.Progression.EstimateSafetyFactor, measured * 1.05);
        }
    }

    public class OfflineAndAdsTests
    {
        private static readonly DateTime Now = new DateTime(2026, 10, 31, 12, 0, 0, DateTimeKind.Utc);

        private static SaveData SaveAt(TycoonGame g, DateTime time) => g.CaptureSave(time, "t", null);

        [Test]
        public void ElapsedIsCappedAndGuarded()
        {
            Assert.AreEqual(3600, OfflineEarnings.ElapsedSeconds(Now.AddHours(-1).ToString("o"), Now, 8), 1e-6);
            Assert.AreEqual(8 * 3600, OfflineEarnings.ElapsedSeconds(Now.AddDays(-3).ToString("o"), Now, 8), 1e-6);
            Assert.AreEqual(0, OfflineEarnings.ElapsedSeconds(Now.AddHours(2).ToString("o"), Now, 8));
            Assert.AreEqual(0, OfflineEarnings.ElapsedSeconds("garbage", Now, 8));
            Assert.AreEqual(0, OfflineEarnings.ElapsedSeconds("", Now, 8));
        }

        [Test]
        public void OfflineIncomeIsEstimatedAndCollectedOnce()
        {
            var g = new TycoonGame(GameConfig.CreateDefault(), null, 3);
            g.RunFor(30);
            var data = SaveAt(g, Now.AddHours(-2));
            var loaded = TycoonGame.FromSave(GameConfig.CreateDefault(), data, Now);
            double rate = loaded.EstimateOfflineIncomePerSecond();
            Assert.Greater(loaded.PendingOfflineCents, 0);
            Assert.AreEqual(rate * 7200, loaded.PendingOfflineCents, rate * 2 + 1);

            long balance = loaded.Economy.Balance;
            long got = loaded.CollectOffline();
            Assert.AreEqual(got, loaded.Economy.Balance - balance);
            Assert.AreEqual(0, loaded.CollectOffline());
            Assert.AreEqual(0, loaded.PendingOfflineCents);

            // Saving right after collecting and reloading at the same moment gives nothing new.
            var again = TycoonGame.FromSave(GameConfig.CreateDefault(), SaveAt(loaded, Now), Now);
            Assert.AreEqual(0, again.PendingOfflineCents);
        }

        [Test]
        public void UncollectedOfflineIncomeSurvivesASave()
        {
            var g = new TycoonGame(GameConfig.CreateDefault(), null, 3);
            var loaded = TycoonGame.FromSave(GameConfig.CreateDefault(), SaveAt(g, Now.AddHours(-1)), Now);
            long pending = loaded.PendingOfflineCents;
            var reloaded = TycoonGame.FromSave(GameConfig.CreateDefault(), SaveAt(loaded, Now), Now);
            Assert.AreEqual(pending, reloaded.PendingOfflineCents);
        }

        [Test]
        public void DoublingNeedsACompletedAd()
        {
            var ads = new TestAdService();
            var g = new TycoonGame(GameConfig.CreateDefault(), null, 3);
            var loaded = TycoonGame.FromSave(GameConfig.CreateDefault(), SaveAt(g, Now.AddHours(-1)), Now, ads);
            long pending = loaded.PendingOfflineCents;
            long balance = loaded.Economy.Balance;

            Assert.IsTrue(loaded.RequestOfflineDouble());
            ads.Finish(AdResult.Skipped);
            Assert.AreEqual(balance, loaded.Economy.Balance, "skipped ad gives nothing");
            Assert.AreEqual(pending, loaded.PendingOfflineCents);

            Assert.IsTrue(loaded.RequestOfflineDouble());
            ads.Finish(AdResult.Completed);
            ads.Finish(AdResult.Completed); // duplicate callback is ignored
            Assert.AreEqual(balance + pending * 2, loaded.Economy.Balance);
            Assert.AreEqual(0, loaded.PendingOfflineCents);
        }

        [Test]
        public void DisabledAdsNeverReward()
        {
            var g = new TycoonGame(GameConfig.CreateDefault(), new DisabledAdService(), 3);
            long balance = g.Economy.Balance;
            Assert.IsFalse(g.RequestAdBoost());
            Assert.IsFalse(g.RequestAdInstantCash());
            Assert.IsFalse(g.Boost.IsActive);
            Assert.AreEqual(balance, g.Economy.Balance);
        }

        [Test]
        public void BoostDoublesTollIncome()
        {
            var plain = new TycoonGame(GameConfig.CreateDefault(), null, 9);
            var ads = new TestAdService();
            var boosted = new TycoonGame(GameConfig.CreateDefault(), ads, 9);
            Assert.IsTrue(boosted.RequestAdBoost());
            ads.Finish(AdResult.Completed);
            Assert.IsTrue(boosted.Boost.IsActive);
            plain.RunFor(100);
            boosted.RunFor(100);
            Assert.AreEqual(plain.Stats.TollCentsCollected * 2, boosted.Stats.TollCentsCollected);
            boosted.RunFor(30);
            Assert.IsFalse(boosted.Boost.IsActive, "boost lasts 120 s");
        }

        [Test]
        public void InstantCashAndCashBoost()
        {
            var ads = new TestAdService();
            var g = new TycoonGame(GameConfig.CreateDefault(), ads, 9);
            long reward = g.InstantCashReward;
            long balance = g.Economy.Balance;
            Assert.IsTrue(g.RequestAdInstantCash());
            Assert.IsFalse(g.RequestAdInstantCash(), "one ad at a time");
            ads.Finish(AdResult.Completed);
            Assert.AreEqual(balance + reward, g.Economy.Balance);

            Assert.IsFalse(g.BuyBoostWithCash());
            g.Economy.AddMoney(g.Progression.BoostCashPriceCents, MoneySource.Debug);
            Assert.IsTrue(g.BuyBoostWithCash());
            Assert.IsTrue(g.Boost.IsActive);
        }
    }

    public class ServiceAndShopTests
    {
        private static TycoonGame Rich()
        {
            var g = new TycoonGame(GameConfig.CreateDefault(), null, 4);
            g.Economy.AddMoney(Money.FromDollars(1_000_000), MoneySource.Debug);
            return g;
        }

        [Test]
        public void ServicesFollowSectionRulesAndSlots()
        {
            var g = Rich();
            var services = g.ActiveSection.Services;
            Assert.AreEqual(PlacementResult.NotAllowed, services.Build("gas", 0), "gas stations start on road 2");
            Assert.AreEqual(PlacementResult.Ok, services.Build("cafe", 0));
            Assert.AreEqual(PlacementResult.SlotTaken, services.Build("parking", 0));
            Assert.AreEqual(PlacementResult.InvalidSlot, services.Build("parking", 99));
            Assert.AreEqual(PlacementResult.Ok, services.Build("parking", 1));
            Assert.AreEqual(PlacementResult.SlotTaken, services.Move(0, 1));
            Assert.AreEqual(PlacementResult.Ok, services.Move(0, 5));
            Assert.IsNull(services.At(0));
            Assert.AreEqual("cafe", services.At(5).Config.Id);
            long balance = g.Economy.Balance;
            long refund = services.RemoveRefund(services.At(1));
            Assert.AreEqual(PlacementResult.Ok, services.Remove(1, 0));
            Assert.AreEqual(balance + refund, g.Economy.Balance);
            Assert.AreEqual(2, g.Stats.ServicesBuilt);
        }

        [Test]
        public void ServiceIncomeComesOnlyFromCustomers()
        {
            var g = Rich();
            var services = g.ActiveSection.Services;
            Assert.AreEqual(PlacementResult.Ok, services.Build("cafe", 0));
            var cafe = services.At(0);
            g.RunFor(400);
            Assert.Greater(cafe.CustomersServed, 0);
            Assert.AreEqual(cafe.CustomersServed * services.SpendPerCustomer(cafe), cafe.Earned);
            Assert.AreEqual(cafe.Earned, g.Stats.ServiceIncomeCents);
            Assert.LessOrEqual(cafe.Customers.Count, services.CapacityOf(cafe));
            // Customers cannot exceed paying vehicles.
            Assert.LessOrEqual(cafe.CustomersServed, g.Stats.VehiclesProcessed);
        }

        [Test]
        public void ServiceWithoutTrafficEarnsNothing()
        {
            var config = GameConfig.CreateDefault();
            config.Traffic.BaseArrivalsPerSecond = 0;
            var g = new TycoonGame(config, null, 4);
            g.Economy.AddMoney(Money.FromDollars(10_000), MoneySource.Debug);
            g.ActiveSection.Services.Build("cafe", 0);
            g.RunFor(300);
            Assert.AreEqual(0, g.Stats.ServiceIncomeCents);
        }

        [Test]
        public void ServiceUpgradeRaisesSpending()
        {
            var g = Rich();
            var services = g.ActiveSection.Services;
            services.Build("parking", 2);
            var p = services.At(2);
            long before = services.SpendPerCustomer(p);
            Assert.AreEqual(PlacementResult.Ok, services.Upgrade(2));
            Assert.Greater(services.SpendPerCustomer(p), before);
            while (services.Upgrade(2) == PlacementResult.Ok) { }
            Assert.AreEqual(p.Config.MaxLevel, p.Level);
            Assert.AreEqual(PlacementResult.MaxLevel, services.Upgrade(2));
        }

        [Test]
        public void DecorationsAreCosmeticAndPersist()
        {
            var g = Rich();
            var decor = g.ActiveSection.Decorations;
            Assert.AreEqual(PlacementResult.Ok, decor.Buy("bench", 0));
            Assert.AreEqual(PlacementResult.SlotTaken, decor.Buy("lamp", 0));
            Assert.AreEqual(PlacementResult.Ok, decor.Buy("lamp", decor.FirstFreeSlot()));
            Assert.AreEqual(PlacementResult.Ok, decor.Move(0, 6));
            var data = g.CaptureSave(DateTime.UtcNow, "t", null);
            var loaded = TycoonGame.FromSave(GameConfig.CreateDefault(), data, DateTime.UtcNow);
            Assert.AreEqual("bench", loaded.ActiveSection.Decorations.At(6).Config.Id);
            Assert.AreEqual(2, loaded.ActiveSection.Decorations.Placed.Count);
            Assert.AreEqual(0, loaded.EstimateIncomePerSecond(loaded.ActiveSection) - g.EstimateIncomePerSecond(g.ActiveSection), 1e-6);
        }

        [Test]
        public void ShopVehicleUnlockIsShared()
        {
            var g = Rich();
            Assert.IsTrue(g.Active.Upgrades.TryUnlockVehicle("truck"));
            g.Unlock(1); // not allowed yet, needs booths
            Assert.IsTrue(g.IsVehicleUnlocked("truck"));
        }
    }

    public class MissionTests
    {
        [Test]
        public void MilestonesAreClaimedInOrderAndOnce()
        {
            var g = new TycoonGame(GameConfig.CreateDefault(), null, 2);
            var first = g.Missions.CurrentMilestone;
            Assert.AreEqual(MissionType.ProcessCars, first.Type);
            Assert.IsFalse(g.Missions.Claim(first));
            g.RunFor(120);
            Assert.IsTrue(g.Missions.IsComplete(first));
            long balance = g.Economy.Balance;
            Assert.IsTrue(g.Missions.Claim(first));
            Assert.AreEqual(balance + first.RewardCents, g.Economy.Balance);
            Assert.IsFalse(g.Missions.Claim(first));
            Assert.AreNotSame(first, g.Missions.CurrentMilestone);
            // A later milestone cannot be claimed before the current one.
            var later = g.Missions.Milestones[5];
            Assert.IsFalse(g.Missions.CanClaim(later));
        }

        [Test]
        public void DailyMissionsRefreshPerDayAndTrackFromStart()
        {
            var g = new TycoonGame(GameConfig.CreateDefault(), null, 2);
            g.RunFor(60);
            var day = new DateTime(2026, 10, 31, 8, 0, 0, DateTimeKind.Utc);
            g.Missions.RefreshDaily(day, g.Economy.GetIncomePerMinute());
            Assert.AreEqual(4, g.Missions.Daily.Count);
            var set = g.Missions.Daily[0];
            g.Missions.RefreshDaily(day.AddHours(5), 0);
            Assert.AreSame(set, g.Missions.Daily[0], "same day keeps missions");
            foreach (var m in g.Missions.Daily)
            {
                if (m.Type != MissionType.ReachIncome) Assert.AreEqual(0, g.Missions.Progress(m));
            }
            g.Missions.RefreshDaily(day.AddDays(1), 0);
            Assert.AreEqual("2026-11-01", g.Missions.DailyDate);
        }

        [Test]
        public void ClaimedDailyMissionStaysClaimedAfterReload()
        {
            var g = new TycoonGame(GameConfig.CreateDefault(), null, 2);
            g.Missions.RefreshDaily(DateTime.UtcNow, 0);
            var m = g.Missions.Daily[0];
            m.Baseline = g.GetCounter(m.Type) - m.Target; // force completion
            if (m.Type == MissionType.ReachIncome) m.Target = 0;
            Assert.IsTrue(g.Missions.Claim(m) || m.Type == MissionType.ReachIncome);
            var loaded = TycoonGame.FromSave(GameConfig.CreateDefault(), g.CaptureSave(DateTime.UtcNow, "t", null), DateTime.UtcNow);
            loaded.Missions.RefreshDaily(DateTime.UtcNow, 0);
            Assert.AreEqual(m.Claimed, loaded.Missions.Daily[0].Claimed);
            Assert.IsFalse(loaded.Missions.Claim(loaded.Missions.Daily[0]) && m.Claimed);
        }
    }

    public class TycoonSaveTests
    {
        [Test]
        public void FullGameRoundTrip()
        {
            var g = new TycoonGame(GameConfig.CreateDefault(), null, 6);
            g.Economy.AddMoney(Money.FromDollars(500_000), MoneySource.Debug);
            var sim = g.Active;
            for (int i = 0; i < 2; i++)
            {
                var b = sim.Expansion.TryBuyBooth() ?? (sim.Expansion.TryBuyLane() ? sim.Expansion.TryBuyBooth() : null);
                sim.Workers.TryHire(out var w);
                sim.Workers.Assign(w, b);
            }
            g.ActiveSection.Services.Build("cafe", 3);
            g.Unlock(1);
            g.SwitchSection(1);
            g.ActiveSection.Services.Build("gas", 1);
            g.RunFor(30);
            var data = g.CaptureSave(DateTime.UtcNow, "t", null);

            var path = Path.Combine(Path.GetTempPath(), "trt-full-" + Guid.NewGuid().ToString("N") + ".json");
            var manager = new SaveManager(path, TestSerializer.Instance);
            manager.Save(data);
            var loaded = TycoonGame.FromSave(GameConfig.CreateDefault(), manager.Load(), DateTime.UtcNow);
            manager.Delete();

            Assert.AreEqual(1, loaded.ActiveIndex);
            Assert.IsTrue(loaded.Sections[1].Unlocked);
            Assert.AreEqual("gas", loaded.ActiveSection.Services.At(1).Config.Id);
            Assert.AreEqual("cafe", loaded.Sections[0].Services.At(3).Config.Id);
            Assert.AreEqual(3, loaded.Sections[0].Road.Booths.Count);
            Assert.AreEqual(g.Economy.Balance, loaded.Economy.Balance);
            Assert.AreEqual(g.Stats.SectionsUnlocked, loaded.Stats.SectionsUnlocked);
            loaded.SwitchSection(0);
            Assert.AreEqual(3, loaded.Active.Booths.Count);
        }

        [Test]
        public void Version2SaveBecomesSectionOne()
        {
            var legacy = new GameSimulation(GameConfig.CreateDefault(), 3);
            legacy.Economy.AddMoney(Money.FromDollars(10_000), MoneySource.Debug);
            legacy.Expansion.TryBuyBooth();
            var data = legacy.CaptureSave(DateTime.UtcNow, "old", null);
            data.Version = 2;
            data.Sections = null;
            var g = TycoonGame.FromSave(GameConfig.CreateDefault(), data, DateTime.UtcNow);
            Assert.AreEqual(2, g.Active.Booths.Count);
            Assert.AreEqual(legacy.Economy.Balance, g.Economy.Balance);
            Assert.IsFalse(g.Sections[1].Unlocked);
        }
    }
}
