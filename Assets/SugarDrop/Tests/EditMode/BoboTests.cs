using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace SugarDrop.Tests
{
    public class BoboTests
    {
        private static int FirstLevel(LevelType type)
        {
            for (int level = 1; level <= 100; level++)
                if (LevelConfig.For(level).Type == type) return level;
            Assert.Fail($"niciun nivel {type} până la 100");
            return 0;
        }

        private static PieceInfo Bobo(BlastEngine engine) => engine.AllPieces().First(p => p.Kind == TileKind.Ingredient && p.Color == BlastRules.BoboColor);

        [Test]
        public void EveryLand_HasFourBoboLevels_AndTheyAreIntroducedInOrder()
        {
            Assert.AreEqual(LevelType.Descent, LevelConfig.For(3).Type, "prima aventură: coborârea");
            Assert.AreEqual(13, FirstLevel(LevelType.Blaze), "focul vine în Pădurea de Mentă");
            Assert.AreEqual(23, FirstLevel(LevelType.Frozen), "gheața vine în Lacul de Afine");
            for (int chapter = 0; chapter < 10; chapter++)
            {
                int bobo = 0;
                for (int level = chapter * 10 + 1; level <= chapter * 10 + 10; level++)
                {
                    LevelConfig c = LevelConfig.For(level);
                    if (c.HasBobo || c.Type == LevelType.Blaze) bobo++;
                }
                Assert.AreEqual(4, bobo, $"ținutul {chapter + 1}");
            }
            Assert.AreEqual(LevelType.Score, LevelConfig.For(1).Type, "nivelul 1 rămâne antrenamentul");
        }

        [Test]
        public void Descent_StartsAtTheTop_AndEscapingCompletesTheGoal()
        {
            LevelConfig config = LevelConfig.For(3);
            var engine = new BlastEngine(config, 7);
            PieceInfo bobo = Bobo(engine);
            Assert.AreEqual(config.ColumnTop[bobo.Cell.x], bobo.Cell.y, "Bobo pornește din rândul de sus");
            var goals = new LevelGoals(config, engine.FrostingLeft, 0, engine.CountObstacles());
            Assert.AreEqual(GoalKind.Bobo, goals.Items.Single().Kind);
            var exit = new BoardStep(StepKind.Exit);
            exit.Exits.Add(bobo);
            goals.Apply(exit);
            Assert.IsTrue(goals.IsComplete);
        }

        [Test]
        public void Frozen_BoboWaitsInIce_UntilFreed()
        {
            int level = FirstLevel(LevelType.Frozen);
            LevelConfig config = LevelConfig.For(level);
            var engine = new BlastEngine(config, 3);
            PieceInfo bobo = Bobo(engine);
            Assert.AreEqual(config.BoboIce, bobo.Ice);
            Assert.Greater(bobo.Cell.y, config.ColumnBottom[bobo.Cell.x], "înghețat la mijloc, nu jos");
            // Cât are gheață, nu se mișcă.
            for (int move = 0; move < 15 && Bobo(engine).Ice > 0; move++)
            {
                Vector2Int cell = Bobo(engine).Cell;
                Assert.IsTrue(engine.TryFindBestSwap(null, out Vector2Int a, out Vector2Int b));
                engine.Swap(a, b);
                if (engine.AllPieces().Any(p => p.Color == BlastRules.BoboColor && p.Ice > 0))
                    Assert.AreEqual(cell, Bobo(engine).Cell);
            }
        }

        [Test]
        public void Blaze_FireStartsHigh_GrowsTowardBobo_AndCanBurnHim()
        {
            int level = FirstLevel(LevelType.Blaze);
            LevelConfig config = LevelConfig.For(level);
            var engine = new BlastEngine(config, 5);
            List<PieceInfo> fires = engine.AllPieces().Where(p => p.Kind == TileKind.Fire).ToList();
            Assert.AreEqual(config.Fires, fires.Count);
            Assert.IsTrue(fires.All(f => f.Cell.y >= config.ColumnTop[f.Cell.x] - 2), "focul pornește sus");
            Assert.AreEqual(1, engine.AllPieces().Count(p => p.Kind == TileKind.Hero), "Bobo stă jos, pe loc");
            Assert.AreEqual(config.Fires, engine.CountObstacles().Fires);

            // Mutări la întâmplare: focul crește și, lăsat în pace, îl ajunge pe Bobo.
            var random = new System.Random(2);
            int grown = 0;
            for (int move = 0; move < 300 && !engine.BoboBurned && !engine.Stuck && engine.CountObstacles().Fires > 0; move++)
            {
                var swaps = new List<(Vector2Int, Vector2Int)>();
                for (int x = 0; x < engine.Columns; x++)
                    for (int y = 0; y < engine.Rows; y++)
                    {
                        var c = new Vector2Int(x, y);
                        if (engine.IsValidSwap(c, c + Vector2Int.right)) swaps.Add((c, c + Vector2Int.right));
                        if (engine.IsValidSwap(c, c + Vector2Int.up)) swaps.Add((c, c + Vector2Int.up));
                    }
                if (swaps.Count == 0) break;
                (Vector2Int a, Vector2Int b) = swaps[random.Next(swaps.Count)];
                List<BoardStep> steps = engine.Swap(a, b);
                grown += steps.Count(s => s.Kind == StepKind.Grow && s.Changed.Any(p => p.Kind == TileKind.Fire));
            }
            Assert.Greater(grown, 0, "focul se întinde");
            // Ori focul l-a ajuns pe Bobo, ori l-au stins (din întâmplare) liniile albastre și exploziile.
            Assert.IsTrue(engine.BoboBurned || engine.CountObstacles().Fires == 0, "focul nu rămâne la nesfârșit");
        }

        [Test]
        public void OnlyWater_PutsOutFire()
        {
            int level = FirstLevel(LevelType.Blaze);
            var goals = new LevelGoals(LevelConfig.For(level), 0, 0, new ObstacleCounts { Fires = 3 });
            GoalItem fire = goals.Items.Single(i => i.Kind == GoalKind.Fire);
            var grow = new BoardStep(StepKind.Grow);
            grow.Changed.Add(new PieceInfo { Kind = TileKind.Fire });
            goals.Apply(grow);
            Assert.AreEqual(4, fire.Remaining, "focul care crește mărește obiectivul");
            var clear = new BoardStep(StepKind.Clear);
            clear.Cleared.Add(new PieceInfo { Kind = TileKind.Fire });
            goals.Apply(clear);
            Assert.AreEqual(3, fire.Remaining);
        }
    }
}
