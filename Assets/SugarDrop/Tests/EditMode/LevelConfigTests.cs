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
                Assert.That(c.Moves, Is.InRange(12, 70), $"nivel {level}");
                Assert.That(c.Colors, Is.InRange(4, Palette.TileColorCount), $"nivel {level}");
                Assert.That(c.ValidCells, Is.GreaterThanOrEqualTo(60), $"nivel {level}");
                for (int x = 0; x < c.Columns; x++)
                    Assert.That(c.ColumnTop[x] - c.ColumnBottom[x], Is.GreaterThanOrEqualTo(4), $"nivel {level}, coloana {x}"); // cel puțin 5 celule
                Assert.That(c.FrostingCells, Is.LessThanOrEqualTo(c.ValidCells), $"nivel {level}");
                foreach (int color in c.CollectColors) Assert.That(color, Is.InRange(0, c.Colors - 1), $"nivel {level}");
                Assert.That(c.Crates + c.Chocolate + c.Ice + c.Chains, Is.LessThan(c.ValidCells / 3), $"nivel {level}");
                bool hasGoal = c.Type == LevelType.Boss || c.Ingredients > 0 || c.CollectColors.Length > 0 || c.FrostingCells > 0 || c.ScoreTarget > 0;
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
            Assert.AreEqual(5, seen.Count, "în primele 12 niveluri apar toate tipurile (inclusiv primul Boss)");
            Assert.AreEqual(LevelType.Score, LevelConfig.For(1).Type);
        }

        [Test]
        public void Obstacles_StartAtTheirLevels()
        {
            Assert.AreEqual(0, LevelConfig.For(3).Crates);
            Assert.AreEqual(0, LevelConfig.For(6).Ice);
            Assert.AreEqual(0, LevelConfig.For(11).Chains);
            Assert.AreEqual(0, LevelConfig.For(24).Chocolate);
            int crates = 0, ice = 0, chains = 0, chocolate = 0;
            for (int level = 25; level <= 60; level++)
            {
                LevelConfig c = LevelConfig.For(level);
                crates += c.Crates;
                ice += c.Ice;
                chains += c.Chains;
                chocolate += c.Chocolate;
            }
            Assert.Greater(crates, 0);
            Assert.Greater(ice, 0);
            Assert.Greater(chains, 0);
            Assert.Greater(chocolate, 0);
        }

        [Test]
        public void Difficulty_ComesInWaves_WithBossesAndMegaBosses()
        {
            for (int chapter = 0; chapter < 20; chapter++)
            {
                int first = chapter * 10 + 1;
                Assert.AreEqual(Difficulty.Easy, LevelConfig.DifficultyFor(first), $"capitolul {chapter} începe ușor");
                int hard = 0, easy = 0;
                for (int i = 0; i < 9; i++)
                {
                    Difficulty d = LevelConfig.DifficultyFor(first + i);
                    if (d == Difficulty.Hard || d == Difficulty.VeryHard) hard++;
                    if (d == Difficulty.Easy) easy++;
                }
                Assert.That(hard, Is.InRange(1, 2), $"capitolul {chapter}: 1–2 niveluri grele");
                Assert.GreaterOrEqual(easy, 2, $"capitolul {chapter}: câteva niveluri ușoare");
                int last = first + 9;
                Assert.AreEqual(last % 50 == 0 ? Difficulty.MegaBoss : Difficulty.Boss, LevelConfig.DifficultyFor(last));
            }
            foreach (int mega in new[] { 50, 100, 150, 200 }) Assert.IsTrue(LevelConfig.For(mega).IsMegaBoss);
        }

        [Test]
        public void Waves_AreNotIdenticalInEveryChapter()
        {
            var patterns = new System.Collections.Generic.HashSet<string>();
            for (int chapter = 0; chapter < 10; chapter++)
            {
                var text = new System.Text.StringBuilder();
                for (int i = 1; i <= 10; i++) text.Append((int)LevelConfig.DifficultyFor(chapter * 10 + i));
                patterns.Add(text.ToString());
            }
            Assert.Greater(patterns.Count, 2);
        }

        [Test]
        public void BossLevels_AreEveryTenth_AndAreBattles()
        {
            for (int level = 1; level <= 200; level++)
            {
                LevelConfig c = LevelConfig.For(level);
                Assert.AreEqual(level % 10 == 0, c.IsBoss, $"nivel {level}");
                Assert.AreEqual(level % 50 == 0, c.IsMegaBoss, $"nivel {level}");
                Assert.AreEqual(level % 10 == 0, c.Type == LevelType.Boss, $"nivel {level}");
            }
            LevelConfig boss = LevelConfig.For(10);
            Assert.AreEqual(0, boss.CollectColors.Length);
            Assert.AreEqual(0, boss.Ingredients);
        }

        [Test]
        public void InvalidLevel_IsTreatedAsLevelOne()
        {
            Assert.AreEqual(1, LevelConfig.For(0).Level);
            Assert.AreEqual(1, LevelConfig.For(-5).Level);
        }
    }
}
