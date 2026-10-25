using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SugarDrop.Tests
{
    /// <summary>Motorul tablei: cădere, piese noi, cascade, bomboane care ies, glazură, obstacole.</summary>
    public class Match3EngineTests
    {
        private static bool HasBlockers(LevelConfig c) => c.Biscuits > 0 || c.Chocolate > 0 || c.Frozen > 0;

        [Test]
        public void NewBoard_IsFull_WithoutMatches_AndPlayable()
        {
            for (int level = 1; level <= 80; level++)
            {
                var engine = new Match3Engine(LevelConfig.For(level), level);
                Assert.IsFalse(engine.HasMatches(), $"nivel {level}: alinieri la start");
                Assert.IsTrue(engine.HasValidMove(), $"nivel {level}: nicio mutare la start");
                Assert.AreEqual(0, engine.EmptyCells(), $"nivel {level}: goluri la start");
                Assert.IsTrue(engine.CheckIntegrity(out string problem), $"nivel {level}: {problem}");
            }
        }

        [Test]
        public void InvalidSwap_ReturnsNull_AndChangesNothing()
        {
            var engine = new Match3Engine(LevelConfig.For(1), 7);
            Assert.IsNull(engine.Swap(new Vector2Int(0, 0), new Vector2Int(2, 0)), "celule care nu sunt vecine");
            Assert.IsNull(engine.Swap(new Vector2Int(0, 0), new Vector2Int(-1, 0)), "în afara tablei");
        }

        [Test]
        public void ThousandsOfRandomMoves_KeepTheBoardConsistent()
        {
            var random = new System.Random(5);
            for (int level = 1; level <= 60; level++)
            for (int attempt = 0; attempt < 2; attempt++)
            {
                LevelConfig config = LevelConfig.For(level);
                var engine = new Match3Engine(config, level * 31 + attempt);
                int frostingStart = engine.FrostingLeft;
                for (int move = 0; move < 40 && !engine.Stuck; move++)
                {
                    List<(Vector2Int, Vector2Int)> moves = ValidMoves(engine);
                    Assert.IsNotEmpty(moves, $"nivel {level}: fără mutări, dar nu e marcat blocat");
                    var (a, b) = moves[random.Next(moves.Count)];
                    List<BoardStep> steps = engine.Swap(a, b);
                    Assert.IsNotNull(steps, $"nivel {level}: mutare validă refuzată");
                    Assert.AreEqual(StepKind.Swap, steps[0].Kind);
                    Assert.IsFalse(engine.HasMatches(), $"nivel {level}, mutarea {move}: alinieri rămase");
                    Assert.IsTrue(engine.CheckIntegrity(out string problem), $"nivel {level}, mutarea {move}: {problem}");
                    if (!HasBlockers(config)) Assert.AreEqual(0, engine.EmptyCells(), $"nivel {level}: goluri fără obstacole");
                    Assert.AreEqual(config.Ingredients, engine.IngredientsExited + engine.IngredientsOnBoard + engine.IngredientsToSpawn,
                        $"nivel {level}: bomboane pierdute");
                    Assert.LessOrEqual(engine.FrostingLeft, frostingStart);
                }
            }
        }

        [Test]
        public void IngredientLevel_CandiesReachTheJar_WithGoodMoves()
        {
            for (int level = 1; level <= 30; level++)
            {
                LevelConfig config = LevelConfig.For(level);
                if (config.Type != LevelType.Ingredients) continue;
                int best = 0;
                for (int seed = 0; seed < 3; seed++)
                {
                    var engine = new Match3Engine(config, level * 11 + seed);
                    for (int move = 0; move < config.Moves && engine.IngredientsExited < config.Ingredients && !engine.Stuck; move++)
                    {
                        Assert.IsTrue(engine.TryFindBestMove(null, out Vector2Int a, out Vector2Int b));
                        engine.Swap(a, b);
                    }
                    best = System.Math.Max(best, engine.IngredientsExited);
                }
                Assert.Greater(best, 0, $"nivel {level}: nicio bomboană n-a ajuns jos");
            }
        }

        [Test]
        public void FrostingLevel_FrostingMelts_WithGoodMoves()
        {
            for (int level = 1; level <= 40; level++)
            {
                LevelConfig config = LevelConfig.For(level);
                if (config.Type != LevelType.Frosting) continue;
                var engine = new Match3Engine(config, level);
                int start = engine.FrostingLeft;
                Assert.Greater(start, 0);
                for (int move = 0; move < 10 && !engine.Stuck; move++)
                {
                    engine.TryFindBestMove(null, out Vector2Int a, out Vector2Int b);
                    engine.Swap(a, b);
                }
                Assert.Less(engine.FrostingLeft, start, $"nivel {level}");
            }
        }

        [Test]
        public void Blast_FallsAndRefills_TheWholeBoard()
        {
            var engine = new Match3Engine(LevelConfig.For(1), 3);
            List<BoardStep> steps = engine.Blast(new Vector2Int(4, 4));
            Assert.IsNotNull(steps);
            Assert.AreEqual(StepKind.Clear, steps[0].Kind);
            Assert.GreaterOrEqual(steps[0].Cleared.Count, 9);
            BoardStep fall = steps.Find(s => s.Kind == StepKind.Fall);
            Assert.IsNotNull(fall, "după explozie piesele trebuie să cadă");
            Assert.IsTrue(fall.Moves.Exists(m => m.Spawned), "trebuie să intre piese noi de sus");
            Assert.AreEqual(0, engine.EmptyCells());
            Assert.IsFalse(engine.HasMatches());
        }

        [Test]
        public void SpawnedPieces_StartAboveTheBoard_AndFallDown()
        {
            var engine = new Match3Engine(LevelConfig.For(1), 9);
            foreach (BoardStep step in engine.Blast(new Vector2Int(4, 2)))
                foreach (PieceMove move in step.Moves)
                {
                    Assert.Less(move.To.y, move.From.y + (move.From.x == move.To.x ? 0 : 1), "piesele doar coboară");
                    if (move.Spawned) Assert.GreaterOrEqual(move.From.y, LevelConfig.BoardRows, "piesele noi vin de deasupra");
                }
        }

        [Test]
        public void Finale_SpecialsDetonate_UntilNoneLeft()
        {
            var engine = new Match3Engine(LevelConfig.For(2), 4);
            for (int i = 0; i < 5; i++) Assert.IsNotNull(engine.MakeRandomSpecial());
            int detonations = 0;
            while (engine.DetonateNextSpecial() != null && detonations < 100) detonations++;
            Assert.GreaterOrEqual(detonations, 1);
            Assert.Less(detonations, 100);
            Assert.IsTrue(engine.CheckIntegrity(out string problem), problem);
        }

        private static List<(Vector2Int, Vector2Int)> ValidMoves(Match3Engine engine)
        {
            var moves = new List<(Vector2Int, Vector2Int)>();
            for (int x = 0; x < engine.Columns; x++)
            for (int y = 0; y < engine.Rows; y++)
            {
                var a = new Vector2Int(x, y);
                if (engine.IsValidMove(a, a + Vector2Int.right)) moves.Add((a, a + Vector2Int.right));
                if (engine.IsValidMove(a, a + Vector2Int.up)) moves.Add((a, a + Vector2Int.up));
            }
            return moves;
        }
    }
}
