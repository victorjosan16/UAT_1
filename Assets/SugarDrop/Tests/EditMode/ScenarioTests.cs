using System.Linq;
using NUnit.Framework;

namespace SugarDrop.Tests
{
    /// <summary>După nivelul 50 fiecare nivel dintr-un ținut are altă sarcină, iar ținuturile nu se repetă pe aceleași poziții.</summary>
    public class ScenarioTests
    {
        [Test]
        public void EveryChapter_HasEightDifferentTasks()
        {
            for (int chapter = 5; chapter < 40; chapter++)
            {
                var scenarios = Enumerable.Range(chapter * 10 + 1, 10).Select(LevelConfig.ScenarioOf)
                    .Where(s => s != LevelScenario.None).ToList();
                Assert.AreEqual(8, scenarios.Count, $"ținutul {chapter + 1}: 8 niveluri obișnuite (fără Boss și Sugar Rush)");
                Assert.AreEqual(8, scenarios.Distinct().Count(), $"ținutul {chapter + 1}: nicio sarcină nu se repetă");
                // Fiecare ținut are 4 niveluri cu Bobo.
                int bobo = Enumerable.Range(chapter * 10 + 1, 10).Count(l => LevelConfig.BoboTypeFor(l).HasValue);
                Assert.AreEqual(4, bobo, $"ținutul {chapter + 1}");
            }
        }

        [Test]
        public void SamePosition_NeverRepeatsTheTaskOfThePreviousChapter()
        {
            int clashes = 0;
            for (int chapter = 6; chapter < 40; chapter++)
                for (int slot = 0; slot < 10; slot++)
                {
                    LevelScenario now = LevelConfig.ScenarioOf(chapter * 10 + slot + 1), before = LevelConfig.ScenarioOf((chapter - 1) * 10 + slot + 1);
                    if (now != LevelScenario.None && now == before) clashes++;
                }
            Assert.AreEqual(0, clashes, "aceeași poziție are altă sarcină decât în ținutul dinainte");
        }

        [Test]
        public void NewTasks_AreIntroducedInOrder()
        {
            var first = new System.Collections.Generic.Dictionary<LevelScenario, int>();
            for (int level = 1; level <= 200; level++)
            {
                LevelScenario s = LevelConfig.ScenarioOf(level);
                if (s != LevelScenario.None && !first.ContainsKey(s)) first[s] = level;
            }
            Assert.That(first[LevelScenario.Flood], Is.InRange(51, 60), "apa din cer în ținutul 6");
            Assert.That(first[LevelScenario.Craft], Is.InRange(51, 60));
            Assert.That(first[LevelScenario.Combo], Is.InRange(51, 60));
            Assert.That(first[LevelScenario.Pressure], Is.InRange(61, 70));
            Assert.That(first[LevelScenario.Twins], Is.InRange(71, 80));
        }

        [Test]
        public void ScenarioLevels_HaveTheirOwnGoals()
        {
            for (int level = 51; level <= 300; level++)
            {
                LevelConfig c = LevelConfig.For(level);
                var engine = new BlastEngine(c, 1);
                var goals = new LevelGoals(c, engine.FrostingLeft, 0, engine.CountObstacles());
                if (c.IsBoss || c.IsBonus) continue;
                Assert.IsNotEmpty(goals.Items, $"nivelul {level}");
                switch (c.Scenario)
                {
                    case LevelScenario.Craft: Assert.IsTrue(goals.Items.Any(i => i.Kind == GoalKind.Make && i.Target == c.CraftTarget)); break;
                    case LevelScenario.Combo: Assert.IsTrue(goals.Items.Any(i => i.Kind == GoalKind.Combos && i.Target == c.ComboTarget)); break;
                    case LevelScenario.Twins: Assert.IsTrue(goals.Items.Any(i => i.Kind == GoalKind.Bobo && i.Target == 2)); Assert.AreEqual(2, c.Bobos); break;
                    case LevelScenario.Flood: Assert.IsTrue(goals.Items.Any(i => i.Kind == GoalKind.Tanks)); break;
                }
            }
        }

        [Test]
        public void TwoBobos_StartOnTheBoard()
        {
            int level = Enumerable.Range(71, 100).First(l => LevelConfig.ScenarioOf(l) == LevelScenario.Twins);
            var engine = new BlastEngine(LevelConfig.For(level), 4);
            int bobos = engine.AllPieces().Count(p => p.Kind == TileKind.Ingredient && p.Color == BlastRules.BoboColor);
            Assert.AreEqual(2, bobos);
        }

        [Test]
        public void CraftAndComboGoals_CountWhatThePlayerMakes()
        {
            var ball = new BoardStep(StepKind.Clear);
            ball.Changed.Add(new PieceInfo { Kind = TileKind.Rainbow, Color = 0 });
            var combo = new BoardStep(StepKind.Clear) { Combo = true };
            combo.Changed.Add(new PieceInfo { Kind = TileKind.RocketH, Color = 0 });

            LevelConfig craft = Enumerable.Range(51, 200).Select(LevelConfig.For).First(c => c.Type == LevelType.Craft && c.CraftKind == 2);
            var goals = new LevelGoals(craft, 0, 0, new ObstacleCounts());
            Assert.IsTrue(goals.Apply(ball));
            Assert.IsFalse(goals.Apply(combo), "piesele din combinații nu se numără ca „create”");

            LevelConfig comboLevel = Enumerable.Range(51, 200).Select(LevelConfig.For).First(c => c.Type == LevelType.Combo);
            var comboGoals = new LevelGoals(comboLevel, 0, 0, new ObstacleCounts());
            Assert.IsTrue(comboGoals.Apply(combo));
        }

        [Test]
        public void NewTasks_AreWinnableForTheAutoPlayer()
        {
            foreach (LevelScenario scenario in new[] { LevelScenario.Craft, LevelScenario.Combo, LevelScenario.Twins, LevelScenario.Flood, LevelScenario.Pressure })
            {
                int wins = 0, total = 0;
                for (int level = 51; level <= 170 && total < 48; level++)
                {
                    if (LevelConfig.For(level).Scenario != scenario) continue;
                    for (int seed = 0; seed < 4; seed++)
                    {
                        if (BalanceSimulator.Play(level, seed * 131 + level * 7, out _)) wins++;
                        total++;
                    }
                }
                TestContext.Progress.WriteLine($"{scenario}: {wins}/{total}");
                Assert.GreaterOrEqual(wins * 100 / total, 35, $"{scenario} e prea greu pentru jucătorul automat ({wins}/{total})");
            }
        }
    }
}
