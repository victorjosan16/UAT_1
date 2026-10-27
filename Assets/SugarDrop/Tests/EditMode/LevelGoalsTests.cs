using NUnit.Framework;

namespace SugarDrop.Tests
{
    public class LevelGoalsTests
    {
        [Test]
        public void CollectGoal_CountsOnlyItsColor()
        {
            LevelConfig config = LevelConfig.For(2); // „strânge culori”
            Assert.AreEqual(LevelType.Collect, config.Type);
            var goals = new LevelGoals(config, 0);
            GoalItem item = goals.Items.Find(i => i.Kind == GoalKind.Collect);
            int start = item.Remaining;
            goals.OnPieceCleared(item.Color, TileKind.Normal);
            goals.OnPieceCleared(item.Color, TileKind.Normal);
            goals.OnPieceCleared((item.Color + 1) % config.Colors, TileKind.Normal);
            goals.OnPieceCleared(-1, TileKind.Crate);
            goals.OnPieceCleared(item.Color, TileKind.RocketH); // piesele speciale nu au culoare de strâns
            Assert.AreEqual(start - 2, item.Remaining);
        }

        [Test]
        public void Goals_CompleteOnlyWhenAllAreDone()
        {
            var goals = new LevelGoals(LevelConfig.For(10), 3, 1000); // Boss + glazură
            Assert.AreEqual(2, goals.Items.Count);
            Assert.AreEqual(GoalKind.Boss, goals.Items[0].Kind);
            goals.OnBossHealth(0);
            Assert.IsFalse(goals.IsComplete);
            for (int i = 0; i < 3; i++) goals.OnFrostingCleared();
            Assert.IsTrue(goals.IsComplete);
        }

        [Test]
        public void BossGoal_FollowsTheHealth()
        {
            var goals = new LevelGoals(LevelConfig.For(20), 0, 900);
            GoalItem item = goals.Items[0];
            Assert.AreEqual(GoalKind.Boss, item.Kind);
            Assert.AreEqual(900, item.Remaining);
            Assert.IsTrue(goals.OnBossHealth(400));
            Assert.IsFalse(goals.OnBossHealth(400));
            Assert.AreEqual(400, item.Remaining);
            goals.OnBossHealth(-50);
            Assert.AreEqual(0, item.Remaining);
            Assert.IsTrue(goals.IsComplete);
        }

        [Test]
        public void BossLevel_HasNoScoreGoal()
        {
            var goals = new LevelGoals(LevelConfig.For(10), 0, 500);
            Assert.AreEqual(1, goals.Items.Count);
            Assert.AreEqual(GoalKind.Boss, goals.Items[0].Kind);
        }

        [Test]
        public void ScoreGoal_TracksTheTarget()
        {
            var goals = new LevelGoals(LevelConfig.For(1), 0);
            GoalItem item = goals.Items[0];
            Assert.AreEqual(GoalKind.Score, item.Kind);
            goals.OnScore(item.Target - 1);
            Assert.IsFalse(goals.IsComplete);
            goals.OnScore(item.Target);
            Assert.IsTrue(goals.IsComplete);
        }

        [Test]
        public void Frosting_And_Ingredients_CountDown()
        {
            var goals = new LevelGoals(LevelConfig.For(4), 3);
            goals.OnFrostingCleared();
            goals.OnFrostingCleared();
            goals.OnFrostingCleared();
            goals.OnFrostingCleared(); // în plus: nu trece sub zero
            Assert.IsTrue(goals.Items.Find(i => i.Kind == GoalKind.Frosting).Done);
        }

        [Test]
        public void CrateGoal_CountsDestroyedCrates()
        {
            LevelConfig config = LevelConfig.For(4);
            Assert.AreEqual(LevelType.Crates, config.Type);
            var goals = new LevelGoals(config, 0, 0, new ObstacleCounts { Crates = 2 });
            GoalItem item = goals.Items.Find(i => i.Kind == GoalKind.Crates);
            Assert.AreEqual(2, item.Target);
            var step = new BoardStep(StepKind.Clear);
            step.Cleared.Add(new PieceInfo { Kind = TileKind.Crate, Color = -1 });
            Assert.IsTrue(goals.Apply(step));
            Assert.AreEqual(1, item.Remaining);
        }

        [Test]
        public void IceGoal_CountsBrokenLayers()
        {
            LevelConfig config = LevelConfig.For(7);
            Assert.AreEqual(LevelType.Ice, config.Type);
            var goals = new LevelGoals(config, 0, 0, new ObstacleCounts { IceLayers = 3 });
            var step = new BoardStep(StepKind.Clear) { IceLayersBroken = 2 };
            goals.Apply(step);
            Assert.AreEqual(1, goals.Items.Find(i => i.Kind == GoalKind.Ice).Remaining);
        }

        [Test]
        public void SpecialsGoal_CountsActivations()
        {
            int level = 5;
            while (LevelConfig.For(level).Type != LevelType.Specials) level++;
            LevelConfig config = LevelConfig.For(level);
            var goals = new LevelGoals(config, 0);
            GoalItem item = goals.Items.Find(i => i.Kind == GoalKind.Specials);
            Assert.AreEqual(config.SpecialsTarget, item.Target);
            var step = new BoardStep(StepKind.Clear);
            step.Activations.Add(new Activation { Kind = TileKind.Bomb, Color = -1 });
            goals.Apply(step);
            Assert.AreEqual(config.SpecialsTarget - 1, item.Remaining);
        }

        [Test]
        public void ObstacleLevel_WithoutObstacles_FallsBackToAScore()
        {
            var goals = new LevelGoals(LevelConfig.For(4), 0, 0, default);
            Assert.AreEqual(1, goals.Items.Count);
            Assert.AreEqual(GoalKind.Score, goals.Items[0].Kind);
            Assert.AreEqual(LevelGoals.FallbackScore, goals.Items[0].Target);
        }
    }
}
