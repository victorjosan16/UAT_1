using System;
using UnityEngine;

namespace SugarDrop.Tests
{
    /// <summary>
    /// Un jucător automat simplu: alege mereu mutarea sugerată de indiciu, fără ajutoare.
    /// Folosit ca să verificăm că nivelurile se pot câștiga (un om joacă de obicei mai bine și are ajutoare).
    /// </summary>
    public static class BalanceSimulator
    {
        public static bool Play(int level, int seed, out int score)
        {
            LevelConfig config = LevelConfig.For(level);
            var engine = new Match3Engine(config, seed);
            var goals = new LevelGoals(config, engine.FrostingLeft);
            score = 0;
            Func<int, int> weight = color =>
            {
                foreach (GoalItem item in goals.Items)
                    if (item.Kind == GoalKind.Collect && item.Color == color && !item.Done) return 3;
                return 0;
            };
            for (int move = 0; move < config.Moves && !goals.IsComplete && !engine.Stuck; move++)
            {
                if (!engine.TryFindBestMove(weight, out Vector2Int a, out Vector2Int b)) break;
                foreach (BoardStep step in engine.Swap(a, b))
                {
                    score += ScoreRules.PointsFor(step);
                    goals.Apply(step);
                }
                goals.OnScore(score);
            }
            return goals.IsComplete;
        }
    }
}
