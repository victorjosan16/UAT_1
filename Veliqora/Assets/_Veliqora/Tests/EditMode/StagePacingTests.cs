using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Veliqora.Core.Economy;
using Veliqora.Core.Levels;

namespace Veliqora.Tests.EditMode
{
    /// <summary>Stage pacing: random hard levels from early on, boss level at every stage finale, non-linear difficulty.</summary>
    public class StagePacingTests
    {
        private static List<LevelDefinition> Levels => TestUtil.Catalog.Levels;

        [Test]
        public void Planner_IsDeterministic()
        {
            for (int n = 1; n <= 300; n++)
            {
                var a = StagePlanner.Plan(LevelCatalogBuilder.DefaultBaseSeed, n);
                var b = StagePlanner.Plan(LevelCatalogBuilder.DefaultBaseSeed, n);
                Assert.AreEqual(a.Kind, b.Kind);
                Assert.AreEqual(a.ColorDelta, b.ColorDelta);
            }
        }

        [Test]
        public void EveryStageAfterTheTutorial_EndsWithABossLevel()
        {
            foreach (var l in Levels)
            {
                Assert.AreEqual((l.Number - 1) / 10 + 1, l.Stage);
                bool finale = l.Number > 10 && l.Number % 10 == 0;
                Assert.AreEqual(finale, l.Kind == LevelKind.Boss, l.Id);
            }
        }

        [Test]
        public void HardLevels_AppearEarly_AreRandomlySpread_AndNeverBackToBack()
        {
            var hard = Levels.FindAll(l => l.Kind == LevelKind.Hard);
            Assert.IsTrue(hard.Exists(l => l.Number <= 20), "a random hard level must appear in the first stages");
            Assert.IsFalse(Levels.Exists(l => l.Number <= 10 && l.IsHard), "tutorial stage stays gentle");
            int candidates = Levels.FindAll(l => l.Number >= StagePlanner.FirstRandomHardLevel && l.Kind != LevelKind.Boss).Count;
            double rate = hard.Count / (double)candidates;
            Assert.That(rate, Is.InRange(0.08, 0.30), "about one in six levels is randomly hard");
            for (int i = 1; i < Levels.Count; i++)
                Assert.IsFalse(Levels[i].IsHard && Levels[i - 1].IsHard && Levels[i].Kind == LevelKind.Hard, "two hard levels in a row at " + Levels[i].Id);
        }

        [Test]
        public void HardAndBossLevels_AreHarderThanNormalLevels_InTheSameTier()
        {
            foreach (DifficultyTier tier in new[] { DifficultyTier.Beginner, DifficultyTier.Intermediate, DifficultyTier.Advanced, DifficultyTier.Expert, DifficultyTier.Master })
            {
                var normal = Levels.FindAll(l => l.Tier == tier && !l.Handcrafted && l.Kind == LevelKind.Normal);
                var hard = Levels.FindAll(l => l.Tier == tier && l.IsHard);
                Assert.IsNotEmpty(hard, tier + " has hard/boss levels");
                Assert.Greater(Avg(hard, l => l.ParMoves), Avg(normal, l => l.ParMoves), tier + " par");
                Assert.Greater(Avg(hard, l => l.Validation.StuckRate), Avg(normal, l => l.Validation.StuckRate), tier + " stuck rate");
            }
        }

        [Test]
        public void Difficulty_IsNotAStraightLine()
        {
            int drops = 0;
            for (int i = 10; i < Levels.Count - 1; i++) if (Levels[i + 1].ParMoves < Levels[i].ParMoves - 3) drops++;
            Assert.Greater(drops, 20, "regular breathers after hard levels / random variation");
        }

        [Test]
        public void HardAndBoss_FirstClear_PayBonusCoins()
        {
            var cfg = EconomyConfig.FromJson(File.ReadAllText(TestUtil.FindProjectFile(TestUtil.EconomyRelativePath)));
            var e = new EconomyService(new EconomyState(), cfg);
            var normal = e.ComputeLevelReward(13, 3, 0, LevelKind.Normal);
            var hard = e.ComputeLevelReward(13, 3, 0, LevelKind.Hard);
            var boss = e.ComputeLevelReward(13, 3, 0, LevelKind.Boss);
            Assert.AreEqual(normal.Total + cfg.HardLevelBonus, hard.Total);
            Assert.AreEqual(normal.Total + cfg.BossLevelBonus, boss.Total);
            Assert.AreEqual(0, e.ComputeLevelReward(13, 3, 3, LevelKind.Boss).DifficultyBonus, "no bonus farming on replays");
        }

        private static double Avg(List<LevelDefinition> list, System.Func<LevelDefinition, double> f)
        {
            double t = 0;
            foreach (var l in list) t += f(l);
            return t / list.Count;
        }
    }
}
