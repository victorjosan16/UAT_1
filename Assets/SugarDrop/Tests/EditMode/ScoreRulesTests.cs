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
                Assert.Greater(three, two);
                Assert.AreEqual(2, ScoreRules.Stars(two, config));
                Assert.AreEqual(3, ScoreRules.Stars(three, config));
            }
        }

        [Test]
        public void LongChains_AreWorthMore_ButCapped()
        {
            Assert.Greater(ScoreRules.TileValue(10), ScoreRules.TileValue(0));
            Assert.AreEqual(ScoreRules.TileValue(1000), ScoreRules.TilePoints + ScoreRules.MaxChainBonus);
        }
    }
}
