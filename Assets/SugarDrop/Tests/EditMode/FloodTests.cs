using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace SugarDrop.Tests
{
    /// <summary>„Apa din cer” (de la nivelul 51): rezervoare sus, focul coboară spre Bobo, apa eliberată stinge focul.</summary>
    public class FloodTests
    {
        private static int FloodLevel(int from = LevelConfig.FloodFrom)
        {
            for (int level = from; level < from + 100; level++)
                if (LevelConfig.For(level).Tanks > 0) return level;
            Assert.Fail("niciun nivel cu rezervoare");
            return 0;
        }

        [Test]
        public void Tanks_OnlyAfterLevel50_AndTwoFireLevelsPerChapter()
        {
            for (int level = 1; level < LevelConfig.FloodFrom; level++)
                Assert.AreEqual(0, LevelConfig.For(level).Tanks, $"nivelul {level}: fără rezervoare înainte de 51");
            for (int chapter = 5; chapter < 15; chapter++)
            {
                int fire = 0, tanks = 0;
                for (int level = chapter * 10 + 1; level <= chapter * 10 + 10; level++)
                {
                    LevelConfig c = LevelConfig.For(level);
                    if (c.Type == LevelType.Blaze) fire++;
                    if (c.Tanks > 0)
                    {
                        tanks++;
                        Assert.AreEqual(LevelType.Blaze, c.Type);
                        Assert.GreaterOrEqual(c.TankHits, 2);
                    }
                }
                Assert.AreEqual(2, fire, $"ținutul {chapter + 1}: două niveluri cu foc");
                Assert.GreaterOrEqual(tanks, 1, $"ținutul {chapter + 1}: cel puțin un nivel cu rezervoare");
                // Cu Bobo pe tablă în continuare 4 niveluri pe ținut.
                int bobo = Enumerable.Range(chapter * 10 + 1, 10).Count(l => LevelConfig.For(l).HasBobo || LevelConfig.For(l).Type == LevelType.Blaze);
                Assert.AreEqual(4, bobo);
            }
        }

        [Test]
        public void FloodLevel_HasTanksAtTheTop_AndTheGoalIsToBreakThem()
        {
            int level = FloodLevel();
            LevelConfig config = LevelConfig.For(level);
            var engine = new BlastEngine(config, 3);
            ObstacleCounts counts = engine.CountObstacles();
            Assert.AreEqual(config.Tanks, counts.Tanks, "toate rezervoarele sunt pe tablă");
            foreach (PieceInfo p in engine.AllPieces().Where(p => p.Kind == TileKind.Tank))
                Assert.AreEqual(config.ColumnTop[p.Cell.x], p.Cell.y, "rezervoarele stau sus");
            var goals = new LevelGoals(config, engine.FrostingLeft, 0, counts);
            Assert.IsTrue(goals.Items.Any(i => i.Kind == GoalKind.Tanks && i.Target == config.Tanks));
            Assert.IsFalse(goals.Items.Any(i => i.Kind == GoalKind.Fire), "obiectivul e apa, nu focul");
        }

        [Test]
        public void BreakingATank_FloodsItsColumn_AndPutsOutTheFireThere()
        {
            int level = FloodLevel();
            LevelConfig config = LevelConfig.For(level);
            var engine = new BlastEngine(config, 5);
            int column = engine.AllPieces().First(p => p.Kind == TileKind.Tank).Cell.x;
            int top = config.ColumnTop[column];
            engine.DebugSet(new Vector2Int(column, top), TileKind.Tank, -1, hits: 1);
            engine.DebugSet(new Vector2Int(column, top - 3), TileKind.Fire, -1, hits: 1);
            engine.DebugSet(new Vector2Int(column, top - 5), TileKind.Fire, -1, hits: 1);
            // Zona din jurul bombei e curată (fără foc inițial), ca schimbul să fie valid.
            for (int dy = 1; dy <= 2; dy++)
                for (int dx = -1; dx <= 1; dx++)
                    if (config.IsValidCell(column + dx, top - dy)) engine.DebugSet(new Vector2Int(column + dx, top - dy), TileKind.Normal, 2 + (dx + dy) % 2);
            // O bombă lângă rezervor: explodează 3×3 și îl lovește.
            engine.DebugSet(new Vector2Int(column, top - 2), TileKind.Bomb, -1);
            Vector2Int from = new(column, top - 2), to = new(column, top - 1); // bomba urcă lângă rezervor și explodează 3×3
            var steps = engine.Swap(from, to);
            Assert.IsNotNull(steps);
            Assert.IsTrue(steps.Any(s => s.Floods.Contains(new Vector2Int(column, top))), "apa a fost eliberată");
            Assert.IsFalse(engine.AllPieces().Any(p => p.Kind == TileKind.Fire && p.Cell.x == column), "focul de pe coloană s-a stins");
            Assert.IsTrue(steps.SelectMany(s => s.Cleared).Any(p => p.Kind == TileKind.Tank));
        }

        [Test]
        public void TankGoal_CountsEachBrokenTank()
        {
            int level = FloodLevel();
            LevelConfig config = LevelConfig.For(level);
            var engine = new BlastEngine(config, 1);
            var goals = new LevelGoals(config, engine.FrostingLeft, 0, engine.CountObstacles());
            var step = new BoardStep(StepKind.Clear);
            for (int i = 0; i < config.Tanks; i++) step.Cleared.Add(new PieceInfo { Kind = TileKind.Tank, Cell = new Vector2Int(i, 8), Color = -1 });
            Assert.IsTrue(goals.Apply(step));
            Assert.IsTrue(goals.IsComplete);
        }

        [Test]
        public void Pressure_BoboDescendsWhileTimeBombsTick_FromLevel61()
        {
            for (int level = 1; level < LevelConfig.PressureFrom; level++)
                Assert.IsFalse(LevelConfig.For(level).HasBobo && LevelConfig.For(level).TimeBombs > 0, $"nivelul {level}: fără presiune înainte de 61");
            int found = 0;
            for (int level = LevelConfig.PressureFrom; level <= 200; level++)
            {
                LevelConfig c = LevelConfig.For(level);
                if (!c.HasBobo || c.TimeBombs <= 0) continue;
                found++;
                Assert.AreEqual(7, (level - 1) % 10, "al 8-lea nivel al ținutului");
                var engine = new BlastEngine(c, 2);
                Assert.Greater(engine.TimeBombsOnBoard, 0, "bombe pe tablă de la start");
            }
            Assert.GreaterOrEqual(found, 10);
        }

        [Test]
        public void PressureLevels_AreWinnableForTheAutoPlayer()
        {
            int wins = 0, total = 0;
            for (int level = LevelConfig.PressureFrom; level <= 150; level++)
            {
                LevelConfig c = LevelConfig.For(level);
                if (!c.HasBobo || c.TimeBombs <= 0) continue;
                int levelWins = 0;
                for (int seed = 0; seed < 6; seed++)
                    if (BalanceSimulator.Play(level, seed * 131 + level * 7, out _)) levelWins++;
                TestContext.Progress.WriteLine($"presiune {level}: {levelWins}/6");
                wins += levelWins;
                total += 6;
            }
            TestContext.Progress.WriteLine($"Presiune total: {wins}/{total}");
            Assert.GreaterOrEqual(wins * 100 / total, 40, "nivelurile cu presiune sunt prea grele pentru jucătorul automat");
        }

        [Test]
        public void FloodLevels_AreWinnableForTheAutoPlayer()
        {
            int wins = 0, total = 0;
            for (int level = LevelConfig.FloodFrom; level <= 150; level++)
            {
                if (LevelConfig.For(level).Tanks <= 0) continue;
                int levelWins = 0;
                for (int seed = 0; seed < 6; seed++)
                    if (BalanceSimulator.Play(level, seed * 131 + level * 7, out _)) levelWins++;
                TestContext.Progress.WriteLine($"nivel {level}: {levelWins}/6");
                wins += levelWins;
                total += 6;
            }
            TestContext.Progress.WriteLine($"Flood total: {wins}/{total}");
            Assert.GreaterOrEqual(wins * 100 / total, 45, "nivelurile cu rezervoare sunt prea grele pentru jucătorul automat");
        }
    }
}
