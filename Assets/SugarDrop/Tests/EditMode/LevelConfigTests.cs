using NUnit.Framework;

namespace SugarDrop.Tests
{
    /// <summary>Echilibrul nivelurilor: rămâne jucabil la orice nivel.</summary>
    public class LevelConfigTests
    {
        [Test]
        public void EveryLevel_IsWinnableOnPaper()
        {
            for (int level = 1; level <= 300; level++)
            {
                LevelConfig c = LevelConfig.For(level);
                Assert.That(c.Moves, Is.GreaterThanOrEqualTo(10), $"nivel {level}");
                Assert.That(c.Goal, Is.GreaterThan(0).And.LessThanOrEqualTo(c.Candies), $"nivel {level}");
                Assert.That(c.Colors, Is.InRange(3, Palette.TileColorCount), $"nivel {level}");
                // Obstacolele lasă destule capace mobile pe tabla de 8×8.
                Assert.That(c.Biscuits + c.Frozen, Is.LessThan(c.Columns * c.Rows / 2), $"nivel {level}");
            }
        }

        [Test]
        public void Obstacles_StartAtTheirLevels()
        {
            Assert.AreEqual(0, LevelConfig.For(3).Biscuits);
            Assert.Greater(LevelConfig.For(4).Biscuits, 0);
            Assert.AreEqual(0, LevelConfig.For(5).Frozen);
            Assert.Greater(LevelConfig.For(6).Frozen, 0);
            Assert.AreEqual(1, LevelConfig.For(14).BiscuitHits);
            Assert.AreEqual(2, LevelConfig.For(15).BiscuitHits);
        }

        [Test]
        public void BossLevels_AreEveryTenth_AndGenerous()
        {
            Assert.IsTrue(LevelConfig.For(10).IsBoss);
            Assert.IsTrue(LevelConfig.For(20).IsBoss);
            Assert.IsFalse(LevelConfig.For(11).IsBoss);
            Assert.Greater(LevelConfig.For(10).Moves, LevelConfig.For(9).Moves);
            Assert.Greater(LevelConfig.For(10).Candies, LevelConfig.For(9).Candies);
        }

        [Test]
        public void InvalidLevel_IsTreatedAsLevelOne()
        {
            Assert.AreEqual(1, LevelConfig.For(0).Level);
            Assert.AreEqual(1, LevelConfig.For(-5).Level);
        }
    }
}
