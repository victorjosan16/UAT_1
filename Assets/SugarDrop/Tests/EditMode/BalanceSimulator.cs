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
            score = 0;
            movesUsed = 0;
            // Nivelul bonus nu se poate pierde (se termină când expiră timpul).
            if (config.IsBonus) return true;
            if (config.Type == LevelType.Rescue) return PlayRescue(level, config, seed, out movesUsed);
            BossBattle boss = config.Type == LevelType.Boss ? new BossBattle(BossDefinition.For(level, config.Moves)) : null;
            var goals = new LevelGoals(config, engine.FrostingLeft, boss?.Health ?? 0, engine.CountObstacles());
            Func<int, int> weight = color =>
            {
                foreach (GoalItem item in goals.Items)
                    if (item.Kind == GoalKind.Collect && item.Color == color && !item.Done) return 3;
                return 0;
            };
            for (int move = 0; move < config.Moves && !goals.IsComplete && !engine.Stuck && !engine.TimeBombExpired; move++)
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
            return goals.IsComplete && !engine.TimeBombExpired;
        }
    
        /// <summary>
        /// Salvarea lui Bobo: botul face cea mai bună atingere, dar 30% din mutări le face la întâmplare
        /// (ca un jucător care nu vede din prima drumul cel mai bun).
        /// </summary>
        private static bool PlayRescue(int level, LevelConfig config, int seed, out int movesUsed)
        {
            var engine = new RescueEngine(RescueLayout.For(level, config.Difficulty));
            var random = new System.Random(seed);
            movesUsed = 0;
            while (movesUsed < config.Moves && !engine.Rescued)
            {
                var groups = engine.Groups();
                if (groups.Count == 0) break;
                Vector2Int tap = random.NextDouble() < 0.3 ? groups[random.Next(groups.Count)][0] : engine.BestTap();
                engine.Tap(tap);
                movesUsed++;
            }
            return engine.Rescued;
        }
}
}
