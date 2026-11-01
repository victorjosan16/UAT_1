using NUnit.Framework;
using VoxelBloom.Core;
using static VoxelBloom.Tests.TestUtil;

namespace VoxelBloom.Tests
{
    public class EconomyTests
    {
        [Test]
        public void Wallet_NeverNegative()
        {
            var w = new Wallet(100);
            Assert.IsFalse(w.TrySpend(150));
            Assert.AreEqual(100, w.Coins);
            Assert.IsTrue(w.TrySpend(100));
            Assert.AreEqual(0, w.Coins);
            w.Add(-50);
            Assert.AreEqual(0, w.Coins);
            Assert.AreEqual(0, new Wallet(-10).Coins);
        }

        [Test]
        public void BoosterPurchase_DeductsOnlyWhenGranted()
        {
            var cfg = Config();
            var w = new Wallet(450);
            var inv = new BoosterInventory();
            Assert.AreEqual(PurchaseResult.Success, inv.Buy(BoosterType.Hammer, cfg, w));
            Assert.AreEqual(150, w.Coins);
            Assert.AreEqual(1, inv.Count(BoosterType.Hammer));
            Assert.AreEqual(PurchaseResult.NotEnoughCoins, inv.Buy(BoosterType.ColorBomb, cfg, w));
            Assert.AreEqual(150, w.Coins);
            Assert.AreEqual(0, inv.Count(BoosterType.ColorBomb));
            Assert.AreEqual(PurchaseResult.Success, inv.Buy(BoosterType.Eliminate, cfg, w), "Eliminare is affordable with HARD-level coins");
            Assert.IsTrue(inv.TryConsume(BoosterType.Eliminate));
            Assert.IsFalse(inv.TryConsume(BoosterType.Eliminate));
        }

        [Test]
        public void DailyReward_OncePerUtcDay_StreakAndReset()
        {
            var w = new Wallet(0);
            var d = new DailyRewards(new[] { 10, 20, 30 }, -1, 0);
            long day0 = 86400L * 20000 + 3600;
            Assert.AreEqual(10, d.Claim(day0, w));
            Assert.AreEqual(0, d.Claim(day0 + 3600, w), "no double claim the same day");
            Assert.AreEqual(20, d.Claim(day0 + 86400, w));
            Assert.AreEqual(10, d.Claim(day0 + 86400 * 3, w), "missed a day: streak resets");
            Assert.AreEqual(40, w.Coins);
        }

        [Test]
        public void Lives_RegenerateOverTime()
        {
            var l = new Lives(5, 5, 30, 0);
            long t = 1000000;
            Assert.IsTrue(l.TryConsume(t));
            Assert.AreEqual(4, l.Count);
            Assert.AreEqual(1800, l.SecondsToNext(t));
            l.Update(t + 1799);
            Assert.AreEqual(4, l.Count);
            l.Update(t + 1800);
            Assert.AreEqual(5, l.Count);
            for (int i = 0; i < 5; i++) l.TryConsume(t + 2000);
            Assert.IsFalse(l.TryConsume(t + 2000));
            l.Update(t + 2000 + 1800 * 2);
            Assert.AreEqual(2, l.Count);
        }
    }

    public class SaveTests
    {
        [Test]
        public void Save_RoundTrip()
        {
            var cfg = Config();
            var store = new MemorySaveStorage();
            var m = new SaveManager(store, cfg);
            var s = m.Load();
            Assert.AreEqual(LoadSource.New, m.LoadedFrom);
            s.Coins = 1234; s.Stars[1] = 3; s.Stars[2] = 2; s.HighestUnlockedLevel = 3;
            s.Boosters[BoosterType.Eliminate] = 4; s.CollectionUnlocked.Add("fox"); s.Settings.Music = false; s.Settings.Quality = 1; s.Settings.AutoRotate = false; s.Settings.FireSpeed = 2;
            s.ActiveLevel = new GameSession(Level(1)).ToSnapshot();
            m.Save();
            var m2 = new SaveManager(store, cfg);
            var s2 = m2.Load();
            Assert.AreEqual(LoadSource.Main, m2.LoadedFrom);
            Assert.AreEqual(1234, s2.Coins);
            Assert.AreEqual(5, s2.TotalStars);
            Assert.AreEqual(4, s2.Boosters[BoosterType.Eliminate]);
            Assert.IsTrue(s2.CollectionUnlocked.Contains("fox"));
            Assert.IsFalse(s2.Settings.Music);
            Assert.AreEqual(1, s2.Settings.Quality);
            Assert.IsFalse(s2.Settings.AutoRotate, "turntable preference persists");
            Assert.AreEqual(2, s2.Settings.FireSpeed, "fire speed persists");
            Assert.NotNull(GameSession.FromSnapshot(Level(1), s2.ActiveLevel));
        }

        [Test]
        public void Save_CorruptMain_RecoversBackup_ThenFresh()
        {
            var cfg = Config();
            var store = new MemorySaveStorage();
            var m = new SaveManager(store, cfg);
            m.Load().Coins = 777; m.Save();
            m.Data.Coins = 888; m.Save(); // main=888, backup=777
            store.Main = "{ corrupted";
            var m2 = new SaveManager(store, cfg);
            Assert.AreEqual(777, m2.Load().Coins);
            Assert.AreEqual(LoadSource.Backup, m2.LoadedFrom);
            store.Backup = "also broken";
            var m3 = new SaveManager(store, cfg);
            Assert.AreEqual(cfg.Economy.StartCoins, m3.Load().Coins);
            Assert.AreEqual(LoadSource.New, m3.LoadedFrom);
        }

        [Test]
        public void Save_MigratesV1_AddsEliminateBooster()
        {
            var cfg = Config();
            var s = SaveSerializer.FromJson("{\"version\":1,\"coins\":50,\"boosters\":{\"Hammer\":2}}", cfg);
            Assert.AreEqual(SaveData.CurrentVersion, s.Version);
            Assert.AreEqual(2, s.Boosters[BoosterType.Hammer]);
            Assert.AreEqual(cfg.Booster(BoosterType.Eliminate).StartCount, s.Boosters[BoosterType.Eliminate]);
            Assert.Throws<System.FormatException>(() => SaveSerializer.FromJson("{\"version\":99}", cfg));
        }

        [Test]
        public void FileStorage_AtomicReplace_KeepsBackup()
        {
            string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "vb_save_test_" + System.Guid.NewGuid().ToString("N"));
            try
            {
                var fs = new FileSaveStorage(dir);
                fs.WriteAtomic("{\"v\":1}");
                fs.WriteAtomic("{\"v\":2}");
                Assert.AreEqual("{\"v\":2}", fs.ReadMain());
                Assert.AreEqual("{\"v\":1}", fs.ReadBackup());
            }
            finally { if (System.IO.Directory.Exists(dir)) System.IO.Directory.Delete(dir, true); }
        }
    }

    public class ProgressionTests
    {
        [Test]
        public void CompleteLevel_UnlocksNext_StarsRewardsCollection()
        {
            var cfg = Config();
            var save = SaveData.CreateNew(cfg);
            var w = new Wallet(0);
            var p = new Progression(cfg, save, w);
            var l1 = Level(1);
            Assert.IsTrue(p.IsLevelUnlocked(1));
            Assert.IsFalse(p.IsLevelUnlocked(2));
            var r = p.CompleteLevel(l1, 10, l1.StarScoreThree, 2);
            Assert.AreEqual(3, r.Stars);
            Assert.IsTrue(r.FirstClear);
            Assert.IsTrue(p.IsLevelUnlocked(2));
            Assert.AreEqual(r.Coins, w.Coins);
            Assert.IsTrue(p.IsModelUnlocked(l1.ModelId));
            var replay = p.CompleteLevel(l1, 10, 0, 0);
            Assert.IsFalse(replay.FirstClear);
            Assert.AreEqual(3, p.StarsFor(1), "best stars kept");
            Assert.Less(replay.Coins, r.Coins);
        }

        [Test]
        public void HardLevels_PayMore()
        {
            var cfg = Config();
            var p = new Progression(cfg, SaveData.CreateNew(cfg), new Wallet(0));
            var l4 = Level(4); var l5 = Level(5);
            Assert.IsTrue(p.IsHard(5));
            Assert.IsFalse(p.IsHard(4));
            Assert.Greater(l5.RewardCoins, 2 * l4.RewardCoins);
        }

        [Test]
        public void DoubleReward_OnlyOnce()
        {
            var cfg = Config();
            var save = SaveData.CreateNew(cfg);
            var w = new Wallet(0);
            var p = new Progression(cfg, save, w);
            var r = p.CompleteLevel(Level(1), 99, 0, 0);
            int after = w.Coins;
            Assert.IsTrue(p.TryDoubleReward(r));
            Assert.AreEqual(after + r.Coins, w.Coins);
            Assert.IsFalse(p.TryDoubleReward(r), "no double claims");
        }

        [Test]
        public void ChapterLock_RequiresStars()
        {
            var cfg = Config();
            var save = SaveData.CreateNew(cfg);
            var p = new Progression(cfg, save, new Wallet(0));
            save.HighestUnlockedLevel = 21;
            for (int i = 1; i <= 20; i++) save.Stars[i] = 1;
            Assert.IsFalse(p.IsChapterUnlocked(cfg.Chapters[1]), "20 stars < 30 required");
            for (int i = 1; i <= 10; i++) save.Stars[i] = 2;
            Assert.IsTrue(p.IsChapterUnlocked(cfg.Chapters[1]));
            Assert.AreEqual(21, p.LatestPlayableLevel());
        }

        [Test]
        public void RewardGate_MockProviderNeverGrants()
        {
            Assert.IsFalse(RewardGate.MayGrant(new MockTestAdService(), AdOutcome.Completed));
            Assert.IsFalse(RewardGate.MayGrant(new NoAdsService(), AdOutcome.Unavailable));
            Assert.IsFalse(RewardGate.MayGrant(null, AdOutcome.Completed));
        }
    }

    public class StateMachineTests
    {
        [Test]
        public void IllegalTransitionsRejected_PauseResumeRestores()
        {
            var sm = new GameStateMachine();
            Assert.IsFalse(sm.TryTransition(GameState.Attacking));
            Assert.IsTrue(sm.TryTransition(GameState.Home));
            Assert.IsTrue(sm.TryTransition(GameState.LevelLoading));
            Assert.IsTrue(sm.TryTransition(GameState.Ready));
            Assert.IsTrue(sm.AcceptsBoardInput);
            Assert.IsTrue(sm.TryTransition(GameState.Attacking));
            Assert.IsTrue(sm.TryTransition(GameState.Paused));
            Assert.IsFalse(sm.AcceptsBoardInput, "no input while paused");
            Assert.IsFalse(sm.TryTransition(GameState.LevelComplete));
            Assert.IsTrue(sm.Resume());
            Assert.AreEqual(GameState.Attacking, sm.Current);
            Assert.IsTrue(sm.TryTransition(GameState.Resolving));
            Assert.IsTrue(sm.TryTransition(GameState.LevelComplete));
            Assert.IsFalse(sm.TryTransition(GameState.Attacking), "no attacks after completion");
        }
    }
}
