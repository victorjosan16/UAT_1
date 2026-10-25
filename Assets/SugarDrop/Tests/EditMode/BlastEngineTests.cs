using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SugarDrop.Tests
{
    /// <summary>Motorul „atinge și sparge”: grupuri, piese speciale, combinații, obstacole, cădere.</summary>
    public class BlastEngineTests
    {
        /// <summary>Tablă de nivel 1 (pătrată, fără obstacole) umplută după un desen: rândul de sus primul, cifre = culori.</summary>
        private static BlastEngine Board(params string[] rows)
        {
            var engine = new BlastEngine(LevelConfig.For(1), 1);
            engine.DebugFill(c =>
            {
                int fromTop = LevelConfig.BoardRows - 1 - c.y;
                if (fromTop < rows.Length && c.x < rows[fromTop].Length) return rows[fromTop][c.x] - '0';
                return (c.x * 7 + c.y * 3) % 2 == 0 ? 2 : 3; // restul tablei: model fără grupuri mari de 0/1
            });
            return engine;
        }

        private static Vector2Int Top(int x, int fromTop) => new(x, LevelConfig.BoardRows - 1 - fromTop);

        private static List<BoardStep> Tap(BlastEngine engine, Vector2Int cell) => engine.Tap(cell);

        [TestCase(1, false)]
        [TestCase(2, false)]
        [TestCase(3, true)]
        public void GroupSize_DecidesIfTapIsValid(int size, bool valid)
        {
            string row = new string('0', size).PadRight(9, '1');
            var engine = Board(row, "121212121");
            Assert.AreEqual(size, engine.GroupAt(Top(0, 0)).Count);
            Assert.AreEqual(valid, engine.Tap(Top(0, 0)) != null, "o atingere invalidă nu face nimic și nu consumă mutarea");
        }

        [Test]
        public void IrregularGroup_IsFoundWithFloodFill()
        {
            var engine = Board("0010", "1000", "0111");
            // 0-urile lipite pe orizontală/verticală (nu pe diagonală): 3 pe rândul 2 + 3 deasupra lor = 6.
            // 0-ul din stânga-jos atinge grupul doar pe diagonală, deci nu face parte din el.
            Assert.AreEqual(6, engine.GroupAt(Top(1, 1)).Count);
        }

        [TestCase(5, TileKind.RocketH)]
        [TestCase(7, TileKind.Bomb)]
        [TestCase(10, TileKind.Rainbow)]
        public void BigGroups_CreateOneSpecial_AtTheTappedCell(int size, TileKind expected)
        {
            string first = new string('0', System.Math.Min(size, 9)).PadRight(9, '1');
            string second = new string('0', System.Math.Max(0, size - 9)).PadRight(9, '1');
            var engine = Board(first, second.Replace('1', '2'));
            Vector2Int tapped = Top(0, 0);
            List<BoardStep> steps = engine.Tap(tapped);
            Assert.AreEqual(size, steps[0].GroupSize);
            Assert.AreEqual(1, steps[0].Changed.Count, "exact o piesă specială");
            Assert.AreEqual(expected, steps[0].Changed[0].Kind);
            Assert.AreEqual(tapped, steps[0].Changed[0].Cell);
        }

        [Test]
        public void VerticalGroup_MakesVerticalRocket()
        {
            var engine = Board("0", "0", "0", "0", "0");
            List<BoardStep> steps = engine.Tap(Top(0, 0));
            Assert.AreEqual(TileKind.RocketV, steps[0].Changed[0].Kind);
        }

        [Test]
        public void Rockets_ClearTheirRowOrColumn()
        {
            var engine = Board();
            engine.DebugSet(new Vector2Int(4, 4), TileKind.RocketH, -1);
            BoardStep step = engine.Tap(new Vector2Int(4, 4))[0];
            Assert.AreEqual(9, step.Cleared.Count);
            Assert.IsTrue(step.Cleared.TrueForAll(p => p.Cell.y == 4));

            engine = Board();
            engine.DebugSet(new Vector2Int(2, 6), TileKind.RocketV, -1);
            step = engine.Tap(new Vector2Int(2, 6))[0];
            Assert.AreEqual(9, step.Cleared.Count);
            Assert.IsTrue(step.Cleared.TrueForAll(p => p.Cell.x == 2));
        }

        [Test]
        public void Bomb_IsClippedAtTheBoardEdge()
        {
            var engine = Board();
            engine.DebugSet(new Vector2Int(0, 0), TileKind.Bomb, -1);
            BoardStep step = engine.Tap(new Vector2Int(0, 0))[0];
            Assert.AreEqual(9, step.Cleared.Count, "5×5 din colț = 3×3");
            engine = Board();
            engine.DebugSet(new Vector2Int(4, 4), TileKind.Bomb, -1);
            Assert.AreEqual(25, engine.Tap(new Vector2Int(4, 4))[0].Cleared.Count);
        }

        [Test]
        public void Rainbow_ClearsItsColor()
        {
            var engine = Board("400000000", "111111111");
            engine.DebugSet(Top(0, 0), TileKind.Rainbow, 0);
            BoardStep step = engine.Tap(Top(0, 0))[0];
            Assert.IsTrue(step.Cleared.TrueForAll(p => p.Color == 0 || p.Kind == TileKind.Rainbow));
            Assert.AreEqual(9, step.Cleared.Count); // curcubeul + cele 8 bomboane 0
        }

        [Test]
        public void ChainReaction_EachSpecialFiresOnce()
        {
            var engine = Board();
            engine.DebugSet(new Vector2Int(4, 4), TileKind.RocketH, -1);
            engine.DebugSet(new Vector2Int(7, 4), TileKind.RocketV, -1);
            BoardStep step = engine.Tap(new Vector2Int(4, 4))[0];
            Assert.AreEqual(2, step.Activations.Count);
            var ids = new HashSet<int>();
            foreach (PieceInfo piece in step.Cleared) Assert.IsTrue(ids.Add(piece.Id), "nicio piesă eliminată de două ori");
            Assert.AreEqual(9 + 8, step.Cleared.Count, "rândul + restul coloanei");
        }

        [Test]
        public void Combos_HaveTheirAreas()
        {
            Assert.AreEqual(9 + 9 - 1, ComboCleared(TileKind.RocketH, TileKind.RocketV), "rachetă + rachetă: rând + coloană");
            Assert.AreEqual(3 * 9 + 3 * 9 - 9, ComboCleared(TileKind.RocketH, TileKind.Bomb), "rachetă + bombă: 3 rânduri + 3 coloane");
            Assert.AreEqual(49, ComboCleared(TileKind.Bomb, TileKind.Bomb), "bombă + bombă: 7×7");
            Assert.AreEqual(81, ComboCleared(TileKind.Rainbow, TileKind.Rainbow), "curcubeu + curcubeu: toată tabla");
        }

        private static int ComboCleared(TileKind a, TileKind b)
        {
            var engine = Board();
            engine.DebugSet(new Vector2Int(4, 4), a, a == TileKind.Rainbow ? 2 : -1);
            engine.DebugSet(new Vector2Int(4, 5), b, b == TileKind.Rainbow ? 2 : -1);
            List<BoardStep> steps = engine.Combine(new Vector2Int(4, 5), new Vector2Int(4, 4));
            Assert.IsNotNull(steps);
            Assert.IsTrue(steps[0].Combo);
            return steps[0].Cleared.Count;
        }

        [Test]
        public void RainbowWithRocketOrBomb_TransformsAColor()
        {
            foreach (TileKind other in new[] { TileKind.RocketH, TileKind.Bomb })
            {
                var engine = Board();
                engine.DebugSet(new Vector2Int(4, 4), TileKind.Rainbow, 2);
                engine.DebugSet(new Vector2Int(5, 4), other, -1);
                BoardStep step = engine.Combine(new Vector2Int(4, 4), new Vector2Int(5, 4))[0];
                Assert.Greater(step.Changed.Count, 5, "bomboanele culorii devin speciale");
                Assert.IsTrue(step.Changed.TrueForAll(p => BlastRules.IsSpecial(p.Kind)));
                Assert.Greater(step.Activations.Count, step.Changed.Count, "toate se activează");
            }
        }

        [Test]
        public void Combine_RejectsNonAdjacentOrNormalPieces()
        {
            var engine = Board();
            engine.DebugSet(new Vector2Int(1, 1), TileKind.Bomb, -1);
            engine.DebugSet(new Vector2Int(3, 1), TileKind.Bomb, -1);
            Assert.IsNull(engine.Combine(new Vector2Int(1, 1), new Vector2Int(3, 1)));
            Assert.IsNull(engine.Combine(new Vector2Int(1, 1), new Vector2Int(1, 2)));
        }

        [Test]
        public void Obstacles_TakeOneHitPerAction()
        {
            var engine = Board("000111222");
            engine.DebugSet(Top(1, 1), TileKind.Normal, 3, ice: 3);
            engine.DebugSet(Top(0, 1), TileKind.Crate, -1, hits: 2);
            engine.DebugSet(Top(2, 1), TileKind.Normal, 3, chained: true);
            BoardStep step = engine.Tap(Top(0, 0))[0];
            Assert.AreEqual(3, step.ObstacleHits.Count, "gheața, lada și lanțul: câte o lovitură");
            Assert.IsTrue(step.ObstacleHits.Exists(p => p.Ice == 2));
            Assert.IsTrue(step.ObstacleHits.Exists(p => p.Kind == TileKind.Crate && p.Hits == 1));
            Assert.IsTrue(step.ObstacleHits.Exists(p => !p.Chained && p.Kind == TileKind.Normal && p.Ice == 0));
        }

        [Test]
        public void LockedPieces_CannotBeTapped_OrGrouped()
        {
            var engine = Board("000");
            engine.DebugSet(Top(1, 0), TileKind.Normal, 0, ice: 1);
            Assert.IsNull(engine.Tap(Top(1, 0)), "o bomboană înghețată nu se poate atinge");
            Assert.AreEqual(1, engine.GroupAt(Top(0, 0)).Count, "gheața rupe grupul");
        }

        [Test]
        public void AfterEveryAction_TheBoardIsFull_AndPlayable()
        {
            var random = new System.Random(3);
            for (int level = 1; level <= 60; level++)
            {
                LevelConfig config = LevelConfig.For(level);
                var engine = new BlastEngine(config, level * 13);
                Assert.AreEqual(0, engine.EmptyCells(), $"nivel {level}: goluri la start");
                Assert.IsTrue(engine.HasValidMove(), $"nivel {level}: nicio mutare la start");
                for (int move = 0; move < 40 && !engine.Stuck; move++)
                {
                    var taps = new List<Vector2Int>();
                    for (int x = 0; x < engine.Columns; x++)
                    for (int y = 0; y < engine.Rows; y++)
                        if (engine.IsValidTap(new Vector2Int(x, y))) taps.Add(new Vector2Int(x, y));
                    Assert.IsNotEmpty(taps, $"nivel {level}: fără mutări, dar nemarcat ca blocat");
                    List<BoardStep> steps = engine.Tap(taps[random.Next(taps.Count)]);
                    Assert.IsNotNull(steps);
                    Assert.IsTrue(engine.CheckIntegrity(out string problem), $"nivel {level}, mutarea {move}: {problem}");
                    Assert.IsTrue(engine.HasValidMove() || engine.Stuck, $"nivel {level}: tablă blocată nemarcată");
                    Assert.AreEqual(config.Ingredients, engine.IngredientsExited + engine.IngredientsOnBoard + engine.IngredientsToSpawn,
                        $"nivel {level}: bomboane pierdute");
                    foreach (BoardStep step in steps)
                        foreach (PieceMove m in step.Moves)
                            Assert.LessOrEqual(m.To.y, m.From.y, "piesele doar coboară");
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
