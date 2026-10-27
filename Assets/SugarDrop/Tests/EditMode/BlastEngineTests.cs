using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SugarDrop.Tests
{
    /// <summary>Motorul cu schimburi: linii de 3+, piese speciale după formă, combinații, obstacole, cascade.</summary>
    public class BlastEngineTests
    {
        /// <summary>
        /// Tablă de nivel 1 (9×9, fără obstacole) în tablă de șah din culorile 2 și 3 (nicio linie). Testele pun apoi
        /// culorile 0 și 1 unde au nevoie, ca rezultatul să fie previzibil.
        /// </summary>
        private static BlastEngine Board()
        {
            var engine = new BlastEngine(LevelConfig.For(1), 1);
            engine.DebugFill(c => (c.x + c.y) % 2 == 0 ? 2 : 3);
            return engine;
        }

        private static void Paint(BlastEngine engine, int color, params (int x, int y)[] cells)
        {
            foreach ((int x, int y) in cells) engine.DebugSet(new Vector2Int(x, y), TileKind.Normal, color);
        }

        private static readonly Vector2Int A = new(2, 5), B = new(2, 4);

        /// <summary>Pregătește o mutare: bomboana 0 de deasupra coboară în (2,4) (unde e o bomboană 1, care urcă).</summary>
        private static BlastEngine WithMove()
        {
            BlastEngine engine = Board();
            Paint(engine, 0, (2, 5));
            Paint(engine, 1, (2, 4));
            return engine;
        }

        [Test]
        public void InvalidSwaps_DoNothing_AndCostNothing()
        {
            BlastEngine engine = Board();
            Assert.IsNull(engine.Swap(new Vector2Int(0, 0), new Vector2Int(1, 0)), "fără linie: schimbul nu se face");
            Assert.IsNull(engine.Swap(new Vector2Int(0, 0), new Vector2Int(2, 0)), "doar vecine");
            Assert.IsNull(engine.Swap(new Vector2Int(0, 0), new Vector2Int(1, 1)), "nu pe diagonală");
            engine.DebugSet(new Vector2Int(5, 5), TileKind.Crate, -1, hits: 1);
            engine.DebugSet(new Vector2Int(6, 6), TileKind.Normal, 0, ice: 1);
            Paint(engine, 0, (6, 4), (7, 5), (8, 5));
            Assert.IsFalse(engine.IsValidSwap(new Vector2Int(5, 5), new Vector2Int(5, 4)), "lada nu se mută");
            Assert.IsFalse(engine.IsValidSwap(new Vector2Int(6, 6), new Vector2Int(6, 5)), "bomboana înghețată nu se mută");
        }

        [Test]
        public void Three_InARow_Disappear()
        {
            BlastEngine engine = WithMove();
            Paint(engine, 0, (0, 4), (1, 4));
            List<BoardStep> steps = engine.Swap(A, B);
            Assert.IsNotNull(steps);
            Assert.AreEqual(StepKind.Swap, steps[0].Kind);
            Assert.AreEqual(2, steps[0].Moves.Count);
            BoardStep clear = steps[1];
            Assert.AreEqual(StepKind.Clear, clear.Kind);
            Assert.AreEqual(0, clear.Cascade);
            Assert.AreEqual(3, clear.Cleared.Count);
            Assert.IsTrue(clear.Cleared.TrueForAll(p => p.Color == 0 && p.Cell.y == 4));
            Assert.AreEqual(0, clear.Changed.Count, "3 în linie nu lasă piesă specială");
        }

        [Test]
        public void Four_InARow_MakesARocket_AtTheMovedCell()
        {
            BlastEngine engine = WithMove();
            Paint(engine, 0, (0, 4), (1, 4), (3, 4));
            BoardStep clear = engine.Swap(A, B)[1];
            Assert.AreEqual(4, clear.Cleared.Count);
            Assert.AreEqual(1, clear.Changed.Count);
            Assert.AreEqual(TileKind.RocketV, clear.Changed[0].Kind, "4 pe orizontală → rachetă verticală");
            Assert.AreEqual(B, clear.Changed[0].Cell);

            engine = Board();
            Paint(engine, 0, (4, 1), (4, 2), (4, 4), (3, 3));
            Paint(engine, 1, (4, 3));
            clear = engine.Swap(new Vector2Int(3, 3), new Vector2Int(4, 3))[1];
            Assert.AreEqual(TileKind.RocketH, clear.Changed[0].Kind, "4 pe verticală → rachetă orizontală");
        }

        [Test]
        public void LShape_MakesABomb()
        {
            BlastEngine engine = WithMove();
            Paint(engine, 0, (0, 4), (1, 4), (2, 3), (2, 2));
            BoardStep clear = engine.Swap(A, B)[1];
            Assert.AreEqual(5, clear.Cleared.Count);
            Assert.AreEqual(TileKind.Bomb, clear.Changed[0].Kind);
            Assert.AreEqual(B, clear.Changed[0].Cell, "în colțul liniilor, unde a venit bomboana");
        }

        [Test]
        public void Five_InARow_MakesARainbow()
        {
            BlastEngine engine = WithMove();
            Paint(engine, 0, (0, 4), (1, 4), (3, 4), (4, 4));
            BoardStep clear = engine.Swap(A, B)[1];
            Assert.AreEqual(5, clear.Cleared.Count);
            Assert.AreEqual(TileKind.Rainbow, clear.Changed[0].Kind);
        }

        [Test]
        public void MovingASpecial_ActivatesIt_WhereItLands()
        {
            BlastEngine engine = Board();
            engine.DebugSet(new Vector2Int(4, 4), TileKind.RocketH, -1);
            BoardStep step = engine.Swap(new Vector2Int(4, 4), new Vector2Int(4, 5))[1];
            Assert.AreEqual(9, step.Cleared.Count);
            Assert.IsTrue(step.Cleared.TrueForAll(p => p.Cell.y == 5), "racheta a ajuns pe rândul 5 și l-a curățat");

            engine = Board();
            engine.DebugSet(new Vector2Int(0, 1), TileKind.Bomb, -1);
            Assert.AreEqual(4, engine.Swap(new Vector2Int(0, 1), new Vector2Int(0, 0))[1].Cleared.Count, "3×3 din colț = 2×2");
            engine = Board();
            engine.DebugSet(new Vector2Int(4, 5), TileKind.Bomb, -1);
            Assert.AreEqual(9, engine.Swap(new Vector2Int(4, 5), new Vector2Int(4, 4))[1].Cleared.Count, "bomba: 3×3");
        }

        [Test]
        public void Rainbow_SwappedWithACandy_ClearsThatColor()
        {
            BlastEngine engine = Board();
            engine.DebugSet(new Vector2Int(4, 4), TileKind.Rainbow, 3);
            Paint(engine, 0, (4, 5), (0, 0), (8, 8), (0, 8));
            BoardStep step = engine.Swap(new Vector2Int(4, 4), new Vector2Int(4, 5))[1];
            Assert.IsTrue(step.Cleared.TrueForAll(p => p.Color == 0 || p.Kind == TileKind.Rainbow));
            Assert.AreEqual(5, step.Cleared.Count, "curcubeul + cele 4 bomboane 0");
        }

        [Test]
        public void ChainReaction_EachSpecialFiresOnce()
        {
            BlastEngine engine = Board();
            engine.DebugSet(new Vector2Int(4, 5), TileKind.RocketH, -1);
            Paint(engine, 1, (4, 4));
            engine.DebugSet(new Vector2Int(7, 4), TileKind.RocketV, -1);
            BoardStep step = engine.Swap(new Vector2Int(4, 5), new Vector2Int(4, 4))[1];
            Assert.AreEqual(2, step.Activations.Count);
            var ids = new HashSet<int>();
            foreach (PieceInfo piece in step.Cleared) Assert.IsTrue(ids.Add(piece.Id), "nicio piesă eliminată de două ori");
            Assert.AreEqual(9 + 8, step.Cleared.Count, "rândul + restul coloanei");
        }

        [Test]
        public void TwoSpecials_Swapped_Combine()
        {
            Assert.AreEqual(9 + 9 - 1, ComboCleared(TileKind.RocketH, TileKind.RocketV), "rachetă + rachetă: rând + coloană");
            Assert.AreEqual(3 * 9 + 3 * 9 - 9, ComboCleared(TileKind.RocketH, TileKind.Bomb), "rachetă + bombă: 3 rânduri + 3 coloane");
            Assert.AreEqual(25, ComboCleared(TileKind.Bomb, TileKind.Bomb), "bombă + bombă: 5×5");
            Assert.AreEqual(81, ComboCleared(TileKind.Rainbow, TileKind.Rainbow), "curcubeu + curcubeu: toată tabla");
        }

        private static int ComboCleared(TileKind a, TileKind b)
        {
            BlastEngine engine = Board();
            engine.DebugSet(new Vector2Int(4, 4), a, a == TileKind.Rainbow ? 2 : -1);
            engine.DebugSet(new Vector2Int(4, 5), b, b == TileKind.Rainbow ? 2 : -1);
            List<BoardStep> steps = engine.Swap(new Vector2Int(4, 5), new Vector2Int(4, 4));
            Assert.IsNotNull(steps);
            Assert.IsTrue(steps[1].Combo);
            return steps[1].Cleared.Count;
        }

        [Test]
        public void RainbowWithRocketOrBomb_TransformsAColor()
        {
            foreach (TileKind other in new[] { TileKind.RocketH, TileKind.Bomb })
            {
                BlastEngine engine = Board();
                engine.DebugSet(new Vector2Int(4, 4), TileKind.Rainbow, 2);
                engine.DebugSet(new Vector2Int(5, 4), other, -1);
                BoardStep step = engine.Swap(new Vector2Int(4, 4), new Vector2Int(5, 4))[1];
                Assert.Greater(step.Changed.Count, 5, "bomboanele culorii devin speciale");
                Assert.IsTrue(step.Changed.TrueForAll(p => BlastRules.IsSpecial(p.Kind)));
                Assert.Greater(step.Activations.Count, step.Changed.Count, "toate se activează");
            }
        }

        [Test]
        public void Obstacles_NextToTheLine_TakeOneHit_AndFrozenCandiesInTheLine_LoseALayer()
        {
            BlastEngine engine = WithMove();
            Paint(engine, 0, (1, 4));
            engine.DebugSet(new Vector2Int(0, 4), TileKind.Normal, 0, ice: 2); // înghețată, dar face parte din linie
            engine.DebugSet(new Vector2Int(0, 3), TileKind.Crate, -1, hits: 2);
            engine.DebugSet(new Vector2Int(1, 3), TileKind.Normal, 3, chained: true);
            engine.DebugSet(new Vector2Int(3, 4), TileKind.Normal, 3, ice: 3);
            BoardStep clear = engine.Swap(A, B)[1];
            Assert.AreEqual(2, clear.Cleared.Count, "bomboana înghețată din linie rămâne");
            Assert.IsTrue(clear.ObstacleHits.Exists(p => p.Cell == new Vector2Int(0, 4) && p.Ice == 1), "dar pierde un strat");
            Assert.IsTrue(clear.ObstacleHits.Exists(p => p.Kind == TileKind.Crate && p.Hits == 1));
            Assert.IsTrue(clear.ObstacleHits.Exists(p => p.Cell == new Vector2Int(1, 3) && !p.Chained));
            Assert.IsTrue(clear.ObstacleHits.Exists(p => p.Cell == new Vector2Int(3, 4) && p.Ice == 2));
            Assert.AreEqual(4, clear.ObstacleHits.Count, "câte o lovitură fiecare");
        }

        [Test]
        public void FallingCandies_CanMakeNewLines_InCascade()
        {
            BlastEngine engine = WithMove();
            Paint(engine, 0, (0, 4), (1, 4));
            // După ce rândul 4 (x 0–2) dispare, (1,5) și bomboana 1 urcată în (2,5) cad lângă (3,4): 1,1,1.
            Paint(engine, 1, (1, 5), (3, 4));
            List<BoardStep> steps = engine.Swap(A, B);
            BoardStep cascade = steps.Find(s => s.Kind == StepKind.Clear && s.Cascade == 1);
            Assert.IsNotNull(cascade, "a doua linie dispare singură");
            Assert.IsTrue(cascade.Cleared.FindAll(p => p.Color == 1).Count >= 3);
        }

        [Test]
        public void TheBoard_StartsWithoutLines_AndWithAMove()
        {
            for (int level = 1; level <= 60; level++)
            for (int seed = 0; seed < 3; seed++)
            {
                var engine = new BlastEngine(LevelConfig.For(level), seed * 7919 + level);
                Assert.AreEqual(0, engine.DebugMatchCount(), $"nivel {level}: linii gata făcute la start");
                Assert.IsTrue(engine.HasValidMove(), $"nivel {level}: nicio mutare la start");
                Assert.IsTrue(engine.TryFindBestSwap(null, out Vector2Int from, out Vector2Int to));
                Assert.IsTrue(engine.IsValidSwap(from, to), "indiciul e un schimb valid");
            }
        }

        [Test]
        public void AfterEveryMove_TheBoardIsStable_AndPlayable()
        {
            var random = new System.Random(3);
            for (int level = 1; level <= 60; level++)
            {
                LevelConfig config = LevelConfig.For(level);
                var engine = new BlastEngine(config, level * 13);
                Assert.AreEqual(0, engine.EmptyCells(), $"nivel {level}: goluri la start");
                for (int move = 0; move < 30 && !engine.Stuck; move++)
                {
                    var swaps = new List<(Vector2Int, Vector2Int)>();
                    for (int x = 0; x < engine.Columns; x++)
                    for (int y = 0; y < engine.Rows; y++)
                    {
                        var c = new Vector2Int(x, y);
                        if (engine.IsValidSwap(c, c + Vector2Int.right)) swaps.Add((c, c + Vector2Int.right));
                        if (engine.IsValidSwap(c, c + Vector2Int.up)) swaps.Add((c, c + Vector2Int.up));
                    }
                    Assert.IsNotEmpty(swaps, $"nivel {level}: fără mutări, dar nemarcat ca blocat");
                    (Vector2Int a, Vector2Int b) = swaps[random.Next(swaps.Count)];
                    List<BoardStep> steps = engine.Swap(a, b);
                    Assert.IsNotNull(steps);
                    Assert.IsTrue(engine.CheckIntegrity(out string problem), $"nivel {level}, mutarea {move}: {problem}");
                    Assert.AreEqual(0, engine.DebugMatchCount(), $"nivel {level}: linii rămase nerezolvate");
                    Assert.IsTrue(engine.HasValidMove() || engine.Stuck, $"nivel {level}: tablă blocată nemarcată");
                    Assert.AreEqual(config.Ingredients, engine.IngredientsExited + engine.IngredientsOnBoard + engine.IngredientsToSpawn,
                        $"nivel {level}: bomboane pierdute");
                    foreach (BoardStep step in steps)
                    {
                        if (step.Kind == StepKind.Swap) continue;
                        foreach (PieceMove m in step.Moves)
                        {
                            // Prin portal, piesa iese sus în altă coloană.
                            if (m.Portal) Assert.AreNotEqual(m.From.x, m.To.x, "portalul duce în altă coloană");
                            else Assert.LessOrEqual(m.To.y, m.From.y, "piesele doar coboară");
                        }
                    }
                }
            }
        }

        [Test]
        public void Finale_SpecialsDetonate_UntilNoneLeft()
        {
            var engine = new BlastEngine(LevelConfig.For(2), 4);
            for (int i = 0; i < 5; i++) Assert.IsNotNull(engine.MakeRandomSpecial());
            int detonations = 0;
            while (engine.DetonateNextSpecial() != null && detonations < 100) detonations++;
            Assert.That(detonations, Is.InRange(1, 99));
            Assert.IsTrue(engine.CheckIntegrity(out string problem), problem);
        }
    }
}
