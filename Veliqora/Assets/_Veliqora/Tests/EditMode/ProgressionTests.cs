using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Veliqora.Core.Economy;
using Veliqora.Core.Localization;
using Veliqora.Core.Progression;
using Veliqora.Core.Save;

namespace Veliqora.Tests.EditMode
{
    public class ProgressionTests
    {
        private static EconomyService Eco() => new EconomyService(new EconomyState(),
            EconomyConfig.FromJson(File.ReadAllText(TestUtil.FindProjectFile(TestUtil.EconomyRelativePath))));
        private static ProgressionConfig Cfg() => ProgressionConfig.FromJson(File.ReadAllText(TestUtil.FindProjectFile("Assets/_Veliqora/Resources/Veliqora/Config/progression.json")));

        [Test]
        public void Daily_ClaimOncePerDay_StreakAdvancesAndResets()
        {
            var eco = Eco();
            var daily = new DailyRewardService(new DailyState(), Cfg(), eco);
            Assert.IsTrue(daily.CanClaim(100));
            var r1 = daily.Claim(100);
            Assert.IsNotNull(r1);
            long coins = eco.Coins;
            Assert.IsNull(daily.Claim(100), "second claim on the same day is refused");
            Assert.AreEqual(coins, eco.Coins);
            daily.Claim(101);
            Assert.AreEqual(2, daily.State.Streak);
            daily.Claim(105);
            Assert.AreEqual(1, daily.State.Streak, "missing days resets the streak");
        }

        [Test]
        public void Daily_AdDouble_OncePerDay_OnlyAfterClaim()
        {
            var eco = Eco();
            var daily = new DailyRewardService(new DailyState(), Cfg(), eco);
            Assert.IsFalse(daily.ClaimAdDouble(10), "nothing to double before claiming");
            daily.Claim(10);
            long c = eco.Coins;
            Assert.IsTrue(daily.ClaimAdDouble(10));
            Assert.IsFalse(daily.ClaimAdDouble(10));
            Assert.AreEqual(c + daily.RewardFor(1).Coins, eco.Coins);
        }

        [Test]
        public void Achievements_ProgressFromMetrics_ClaimExactlyOnce()
        {
            var eco = Eco();
            var claimed = new HashSet<string>();
            int levels = 3;
            var svc = new AchievementService(Cfg(), claimed, eco, m => m == "levels" ? levels : 0);
            Assert.IsFalse(svc.Claim("first_steps"), "not complete yet");
            var before = svc.All();
            levels = 5;
            var after = svc.All();
            CollectionAssert.Contains(AchievementService.NewlyCompleted(before, after), "first_steps");
            long c = eco.Coins;
            Assert.IsTrue(svc.Claim("first_steps"));
            Assert.IsFalse(svc.Claim("first_steps"), "no duplicate reward");
            Assert.AreEqual(c + 50, eco.Coins);
            Assert.AreEqual(0, svc.ClaimableCount());
        }

        [Test]
        public void SaveV3_RoundTrips_ProgressionAndLanguage_AndMigratesV2()
        {
            var d = new SaveData();
            d.Stats.BossCompleted = 2;
            d.Stats.CorksOpened = 7;
            d.Daily.LastClaimDay = 9000;
            d.Daily.Streak = 3;
            d.ClaimedAchievements.Add("first_steps");
            d.Settings.Language = "ro";
            var back = SaveSerializer.Deserialize(SaveSerializer.Serialize(d));
            Assert.AreEqual(2, back.Stats.BossCompleted);
            Assert.AreEqual(7, back.Stats.CorksOpened);
            Assert.AreEqual(9000, back.Daily.LastClaimDay);
            Assert.AreEqual(3, back.Daily.Streak);
            Assert.IsTrue(back.ClaimedAchievements.Contains("first_steps"));
            Assert.AreEqual("ro", back.Settings.Language);

            var v2 = new Dictionary<string, object> { ["progress"] = new Dictionary<string, object> { ["highestUnlocked"] = 12L }, ["economy"] = new Dictionary<string, object> { ["coins"] = 99L } };
            var migrated = SaveSerializer.Deserialize(SaveSerializer.WrapPayload(v2, 2));
            Assert.AreEqual(12, migrated.Progress.HighestUnlocked);
            Assert.AreEqual(-1, migrated.Daily.LastClaimDay);
            Assert.AreEqual(0, migrated.ClaimedAchievements.Count);
        }

        [Test]
        public void Localizer_TranslatesFormats_AndFallsBackToEnglish()
        {
            var ro = Localizer.FromJson(File.ReadAllText(TestUtil.FindProjectFile("Assets/_Veliqora/Resources/Veliqora/Config/strings_ro.json")));
            Assert.AreEqual("ro", ro.Language);
            Assert.AreEqual("Joacă", ro.T("Play"));
            Assert.AreEqual("Nivelul 7", ro.F("Level {0}", 7));
            Assert.AreEqual("Some untranslated text", ro.T("Some untranslated text"));
            Assert.Greater(ro.Count, 80, "the main UI is translated");
            Assert.AreEqual("Level 3", Localizer.English.F("Level {0}", 3));
        }

        [Test]
        public void LevelReward_UsesPerLevelBaseCoins()
        {
            var eco = Eco();
            Assert.AreEqual(38 + 30, eco.ComputeLevelReward(77, 3, 0, Core.Levels.LevelKind.Normal, 38).Total);
        }
    }
}
