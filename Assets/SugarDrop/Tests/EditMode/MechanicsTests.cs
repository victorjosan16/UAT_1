using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SugarDrop.Tests
{
    /// <summary>Mecanicile ținuturilor: ghivece, bombe cu ceas, portaluri, nivelul bonus.</summary>
    public class MechanicsTests
    {
        private static int FindLevel(System.Func<LevelConfig, bool> match)
        {
            for (int level = 1; level <= 300; level++)
                if (match(LevelConfig.For(level))) return level;
            Assert.Fail("niciun nivel potrivit");
            return 0;
        }

        [Test]
        public void LandMechanics_StartInTheirLands()
        {
            Assert.AreEqual(LevelType.Garden, LevelConfig.For(12).Type, "Pădurea de Mentă începe cu grădini");
            Assert.AreEqual(LevelType.Defuse, LevelConfig.For(32).Type, "Deșertul de Lămâie are bombe cu ceas");
            Assert.AreEqual(LevelType.Bonus, LevelConfig.For(15).Type);
            Assert.AreNotEqual(LevelType.Bonus, LevelConfig.For(5).Type, "primul capitol nu are bonus");
            for (int level = 1; level < LevelConfig.PortalsFrom; level++) Assert.AreEqual(0, LevelConfig.For(level).PortalIn.Length, $"nivel {level}");
            int lakePortals = 0;
            for (int level = 21; level <= 30; level++) if (LevelConfig.For(level).PortalIn.Length > 0) lakePortals++;
            Assert.GreaterOrEqual(lakePortals, 5, "în Lacul de Afine portalurile sunt peste tot");
        }

        [Test]
        public void Pots_BloomAfterThreeWaterings_AndCountForTheGoal()
        {
            int level = FindLevel(c => c.Type == LevelType.Garden);
            var engine = new BlastEngine(LevelConfig.For(level), 3);
            ObstacleCounts counts = engine.CountObstacles();
            Assert.Greater(counts.Pots, 0);
            var goals = new LevelGoals(LevelConfig.For(level), 0, 0, counts);
            Assert.IsNotNull(goals.Items.Find(i => i.Kind == GoalKind.Flowers));
            var step = new BoardStep(StepKind.Clear);
            step.Cleared.Add(new PieceInfo { Kind = TileKind.Pot, Color = -1 });
            goals.Apply(step);
            Assert.AreEqual(counts.Pots - 1, goals.Items.Find(i => i.Kind == GoalKind.Flowers).Remaining);
        }

        [Test]
        public void TimeBombs_TickEveryMove_AndExpireAtZero()
        {
            int level = FindLevel(c => c.Type == LevelType.Defuse);
            LevelConfig config = LevelConfig.For(level);
            var engine = new BlastEngine(config, 11);
            int bombs = 0, minTimer = 99;
            foreach (PieceInfo p in engine.AllPieces())
                if (p.Timer > 0) { bombs++; minTimer = Mathf.Min(minTimer, p.Timer); }
            Assert.Greater(bombs, 0, "bombele cu ceas sunt pe tablă de la start");
            Assert.AreEqual(config.TimeBombStart, minTimer);

            // Mutări care nu ating bombele: timerele scad până când una explodează.
            for (int move = 0; move < 40 && !engine.TimeBombExpired && !engine.Stuck; move++)
            {
                if (!SafeSwap(engine, out Vector2Int a, out Vector2Int b)) break;
                engine.Swap(a, b);
            }
            Assert.IsTrue(engine.TimeBombExpired, "o bombă neatinsă ajunge la 0");
        }

        [Test]
        public void ClearingATimeBomb_CountsForTheGoal()
        {
            int level = FindLevel(c => c.Type == LevelType.Defuse);
            var goals = new LevelGoals(LevelConfig.For(level), 0);
            GoalItem item = goals.Items.Find(i => i.Kind == GoalKind.TimeBombs);
            Assert.IsNotNull(item);
            var step = new BoardStep(StepKind.Clear);
            step.Cleared.Add(new PieceInfo { Kind = TileKind.Normal, Color = 1, Timer = 3 });
            goals.Apply(step);
            Assert.AreEqual(item.Target - 1, item.Remaining);
        }

        [Test]
        public void Portals_MovePiecesFromTheEntryToTheExit()
        {
            int level = FindLevel(c => c.PortalIn.Length > 0 && c.Type != LevelType.Ingredients);
            LevelConfig config = LevelConfig.For(level);
            bool sawPortal = false;
            for (int seed = 0; seed < 20 && !sawPortal; seed++)
            {
                var engine = new BlastEngine(config, seed);
                for (int move = 0; move < 15 && !engine.Stuck && !sawPortal; move++)
                {
                    if (!engine.TryFindBestSwap(null, out Vector2Int a, out Vector2Int b)) break;
                    List<BoardStep> steps = engine.Swap(a, b);
                    foreach (BoardStep step in steps)
                        foreach (PieceMove m in step.Moves)
                        {
                            if (!m.Portal) continue;
                            sawPortal = true;
                            Assert.AreEqual(config.ColumnBottom[m.From.x], m.From.y, "intră pe jos");
                            Assert.Contains(m.From.x, config.PortalIn);
                            Assert.Contains(m.To.x, config.PortalOut);
                        }
                    Assert.IsTrue(engine.CheckIntegrity(out string problem), problem);
                    Assert.AreEqual(0, engine.EmptyCells(), "tabla rămâne plină");
                }
            }
            Assert.IsTrue(sawPortal, "piesele trec prin portal");
        }

        [Test]
        public void BonusLevel_HasNoGoals_AndNeverEndsByItself()
        {
            LevelConfig config = LevelConfig.For(15);
            Assert.Greater(config.BonusSeconds, 0);
            var goals = new LevelGoals(config, 0);
            Assert.AreEqual(0, goals.Items.Count);
            Assert.IsFalse(goals.IsComplete);
        }

        [Test]
        public void Bosses_BringTheirLandAttack()
        {
            Assert.IsNull(BossDefinition.LandAttack(10), "primul ținut nu are atac propriu");
            Assert.AreEqual(BossAttack.Lock, BossDefinition.LandAttack(20));
            Assert.AreEqual(BossAttack.TimeBomb, BossDefinition.LandAttack(40));
            BossDefinition desert = BossDefinition.For(40, LevelConfig.For(40).Moves);
            Assert.Contains(BossAttack.TimeBomb, desert.Phases[0].Attacks);
        }

        [Test]
        public void BossTimeBombAttack_PutsTimersOnCandies()
        {
            var engine = new BlastEngine(LevelConfig.For(40), 5);
            engine.ApplyBossAttack(BossAttack.TimeBomb, 3);
            int bombs = 0;
            foreach (PieceInfo p in engine.AllPieces()) if (p.Timer > 0) bombs++;
            Assert.That(bombs, Is.InRange(1, BlastEngine.MaxBossTimeBombs));
        }

        /// <summary>Un schimb valid care nu atinge nicio bombă cu ceas (ca să le lăsăm să ajungă la 0).</summary>
        private static bool SafeSwap(BlastEngine engine, out Vector2Int from, out Vector2Int to)
        {
            for (int x = 0; x < engine.Columns; x++)
            for (int y = 0; y < engine.Rows; y++)
            {
                from = new Vector2Int(x, y);
                foreach (Vector2Int d in new[] { Vector2Int.right, Vector2Int.up })
                {
                    to = from + d;
                    if (!engine.IsValidSwap(from, to)) continue;
                    if (engine.TryGetPiece(from, out PieceInfo pa) && pa.Timer > 0) continue;
                    if (engine.TryGetPiece(to, out PieceInfo pb) && pb.Timer > 0) continue;
                    return true;
                }
            }
            from = to = default;
            return false;
        }
    }
}
