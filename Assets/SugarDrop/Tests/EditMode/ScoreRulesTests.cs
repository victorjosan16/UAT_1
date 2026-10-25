using NUnit.Framework;

namespace SugarDrop.Tests
{
    public class ScoreRulesTests
    {
        [Test]
        public void Winning_AlwaysGivesAtLeastOneStar()
        {
            Assert.AreEqual(1, ScoreRules.Stars(0, LevelConfig.For(3)));
        }

        [Test]
        public void Thresholds_AreOrdered_ForEveryLevel()
        {
            for (int level = 1; level <= 200; level++)
            {
                LevelConfig config = LevelConfig.For(level);
                int two = ScoreRules.TwoStars(config), three = ScoreRules.ThreeStars(config);
                Assert.Greater(two, 0);
                Assert.Greater(three, two);
                Assert.AreEqual(2, ScoreRules.Stars(two, config));
                Assert.AreEqual(3, ScoreRules.Stars(three, config));
                if (config.Type == LevelType.Score) Assert.Greater(two, config.ScoreTarget);
            }
        }

        [Test]
        public void BigGroups_AreWorthMuchMore()
        {
            Assert.AreEqual(0, ScoreRules.GroupBonus(3));
            Assert.Less(ScoreRules.GroupBonus(5), ScoreRules.GroupBonus(7));
            Assert.Less(ScoreRules.GroupBonus(7) * 2, ScoreRules.GroupBonus(10));
        }
    }
}
