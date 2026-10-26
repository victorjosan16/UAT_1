using System;
using UnityEngine;

namespace SugarDrop.Tests
{
    /// <summary>
    /// Un jucător automat simplu: face mereu schimbul sugerat de indiciu, fără ajutoare.
    /// Folosit ca să verificăm că nivelurile (inclusiv luptele cu Boss) se pot câștiga — un om joacă de obicei
    /// mai bine și are ajutoare. Folosește aceleași reguli de scor, obiective, daune și atacuri ca jocul.
    /// </summary>
    public static class BalanceSimulator
    {
        public static bool Play(int level, int seed, out int score) => Play(level, seed, out score, out _);

        public static bool Play(int level, int seed, out int score, out int movesUsed)
        {
            LevelConfig config = LevelConfig.For(level);
            var engine = new BlastEngine(config, seed);
            BossBattle boss = config.Type == LevelType.Boss ? new BossBattle(BossDefinition.For(level, config.Moves)) : null;
            var goals = new LevelGoals(config, engine.FrostingLeft, boss?.Health ?? 0, engine.CountObstacles());
            score = 0;
            movesUsed = 0;
            Func<int, int> weight = color =>
            {
                foreach (GoalItem item in goals.Items)
                    if (item.Kind == GoalKind.Collect && item.Color == color && !item.Done) return 3;
                return 0;
            };
            for (int move = 0; move < config.Moves && !goals.IsComplete && !engine.Stuck; move++)
            {
                if (!engine.TryFindBestSwap(weight, out Vector2Int from, out Vector2Int to)) break;
                movesUsed++;
                foreach (BoardStep step in engine.Swap(from, to))
                {
                    score += ScoreRules.PointsFor(step);
                    goals.Apply(step);
                    boss?.Damage(BossRules.DamageFor(step));
                }
                goals.OnScore(score);
                if (boss == null) continue;
                goals.OnBossHealth(boss.Health);
                BossAttack? attack = boss.EndOfMove();
                if (attack.HasValue) engine.ApplyBossAttack(attack.Value, boss.Phase.Strength);
            }
            return goals.IsComplete;
        }
    }
}
