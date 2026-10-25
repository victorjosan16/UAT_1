using NUnit.Framework;

namespace SugarDrop.Tests
{
    /// <summary>Nivelurile: tipuri variate, forme valide, obiective care se pot îndeplini.</summary>
    public class LevelConfigTests
    {
        [Test]
        public void EveryLevel_HasSaneShapeMovesAndGoals()
        {
            for (int level = 1; level <= 300; level++)
            {
                LevelConfig c = LevelConfig.For(level);
                Assert.That(c.Moves, Is.InRange(16, 45), $"nivel {level}");
                Assert.That(c.Colors, Is.InRange(4, Palette.TileColorCount), $"nivel {level}");
                Assert.That(c.ValidCells, Is.GreaterThanOrEqualTo(60), $"nivel {level}");
                for (int x = 0; x < c.Columns; x++)
                    Assert.That(c.ColumnTop[x] - c.ColumnBottom[x], Is.GreaterThanOrEqualTo(4), $"nivel {level}, coloana {x}"); // cel puțin 5 celule
                Assert.That(c.FrostingCells, Is.LessThanOrEqualTo(c.ValidCells), $"nivel {level}");
                foreach (int color in c.CollectColors) Assert.That(color, Is.InRange(0, c.Colors - 1), $"nivel {level}");
                Assert.That(c.Biscuits + c.Chocolate + c.Frozen, Is.LessThan(c.ValidCells / 3), $"nivel {level}");
                bool hasGoal = c.Ingredients > 0 || c.CollectColors.Length > 0 || c.FrostingCells > 0 || c.ScoreTarget > 0;
                Assert.IsTrue(hasGoal, $"nivel {level} fără obiectiv");
            }
        }

        [Test]
        public void SameLevel_IsGeneratedTheSameWayEveryTime()
        {
            LevelConfig a = LevelConfig.For(23), b = LevelConfig.For(23);
            Assert.AreEqual(a.Type, b.Type);
            CollectionAssert.AreEqual(a.ColumnBottom, b.ColumnBottom);
            CollectionAssert.AreEqual(a.ColumnTop, b.ColumnTop);
            CollectionAssert.AreEqual(a.CollectCounts, b.CollectCounts);
        }

        [Test]
        public void LevelTypes_AreMixed()
        {
            var seen = new System.Collections.Generic.HashSet<LevelType>();
            for (int level = 1; level <= 12; level++) seen.Add(LevelConfig.For(level).Type);
            Assert.AreEqual(4, seen.Count, "în primele 12 niveluri apar toate tipurile");
            Assert.AreEqual(LevelType.Score, LevelConfig.For(1).Type);
        }

        [Test]
        public void Obstacles_StartAtTheirLevels()
        {
            // Pe nivelurile cu bomboane și glazură obstacolele sunt mai puține, deci verificăm pe celelalte tipuri.
            Assert.AreEqual(0, LevelConfig.For(4).Biscuits);
            Assert.AreEqual(LevelType.Collect, LevelConfig.For(6).Type);
            Assert.Greater(LevelConfig.For(6).Biscuits, 0);
            Assert.AreEqual(0, LevelConfig.For(6).Frozen);
            Assert.Greater(LevelConfig.For(8).Frozen, 0);
            Assert.AreEqual(2, LevelConfig.For(15).BiscuitHits);
            Assert.AreEqual(0, LevelConfig.For(24).Chocolate);
            Assert.AreEqual(LevelType.Score, LevelConfig.For(26).Type);
            Assert.Greater(LevelConfig.For(26).Chocolate, 0);
        }

        [Test]
        public void BossLevels_AreEveryTenth_WithTwoGoals()
        {
            LevelConfig boss = LevelConfig.For(10);
            Assert.IsTrue(boss.IsBoss);
            Assert.Greater(boss.Ingredients, 0);
            Assert.Greater(boss.CollectColors.Length, 0);
            Assert.IsFalse(LevelConfig.For(11).IsBoss);
        }

        [Test]
        public void InvalidLevel_IsTreatedAsLevelOne()
        {
            Assert.AreEqual(1, LevelConfig.For(0).Level);
            Assert.AreEqual(1, LevelConfig.For(-5).Level);
        }
    }
}
