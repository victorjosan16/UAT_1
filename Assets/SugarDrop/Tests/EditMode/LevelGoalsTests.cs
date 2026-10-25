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
            LevelConfig boss = LevelConfig.For(10);
            var goals = new LevelGoals(boss, 0);
            Assert.GreaterOrEqual(goals.Items.Count, 2);
            for (int i = 0; i < boss.Ingredients; i++) goals.OnIngredientCollected();
            Assert.IsFalse(goals.IsComplete);
            foreach (GoalItem item in goals.Items)
                for (int i = 0; i < item.Target && item.Kind == GoalKind.Collect; i++) goals.OnPieceCleared(item.Color, TileKind.Normal);
            Assert.IsTrue(goals.IsComplete);
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
    }
}
