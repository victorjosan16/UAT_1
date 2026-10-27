using NUnit.Framework;
using UnityEngine;

namespace SugarDrop.Tests
{
    public class RescueTests
    {
        /// <summary>Tablă mică făcută de mână: o coloană de bomboane roșii de la rezervor până la Bobo.</summary>
        private static RescueLayout Column(int height = 4)
        {
            var layout = new RescueLayout
            {
                Width = 3,
                Height = height,
                InletX = 1,
                OutletX = 1,
                Seed = 5,
                Kind = new RescueCell[3, height],
                Color = new int[3, height],
            };
            for (int x = 0; x < 3; x++)
                for (int y = 0; y < height; y++)
                {
                    layout.Kind[x, y] = RescueCell.Piece;
                    layout.Color[x, y] = x == 1 ? 0 : 1 + (x + y) % 2;
                }
            return layout;
        }

        [Test]
        public void Tap_RemovesWholeGroup_AndSyrupReachesBobo()
        {
            var engine = new RescueEngine(Column());
            RescueStep step = engine.Tap(new Vector2Int(1, 2));
            Assert.AreEqual(4, step.Removed.Count, "toată coloana roșie dispare");
            Assert.IsTrue(step.Rescued);
            Assert.IsTrue(engine.Rescued);
            Assert.AreEqual(4, step.Syrup.Count);
            Assert.AreEqual(3, step.Syrup.Find(s => s.cell == new Vector2Int(1, 0)).distance, "siropul curge de sus în jos");
            Assert.IsNull(engine.Tap(new Vector2Int(0, 0)), "după salvare nu se mai joacă");
        }

        [Test]
        public void Pieces_DoNotFall_AndSingleCandiesCanBeTapped()
        {
            RescueLayout layout = Column();
            layout.Color[1, 1] = 2; // întrerupe coloana: două grupuri roșii
            var engine = new RescueEngine(layout);
            RescueStep step = engine.Tap(new Vector2Int(1, 3));
            Assert.AreEqual(2, step.Removed.Count);
            Assert.AreEqual(RescueCell.Piece, engine.Kind(1, 1), "nimic nu cade în gol");
            Assert.IsFalse(engine.Rescued);
            Assert.IsTrue(engine.Syrup(1, 3) && engine.Syrup(1, 2));
            Assert.IsFalse(engine.Syrup(1, 1));
            Assert.IsNotNull(engine.Tap(new Vector2Int(1, 1)), "o bomboană singură se poate atinge");
            engine.Tap(new Vector2Int(1, 0));
            Assert.IsTrue(engine.Rescued);
        }

        [Test]
        public void Stones_BlockSyrup_CagesNeedTwoHits_SlimeClearsNextToGroups()
        {
            RescueLayout layout = Column(5);
            layout.Kind[0, 4] = RescueCell.Stone;
            layout.Kind[2, 3] = RescueCell.Cage;
            layout.Kind[0, 2] = RescueCell.Slime;
            var engine = new RescueEngine(layout);
            Assert.IsNull(engine.Tap(new Vector2Int(0, 4)), "piatra nu se sparge");
            RescueStep step = engine.Tap(new Vector2Int(1, 0));
            Assert.Contains(new Vector2Int(2, 3), step.CageHits);
            Assert.Contains(new Vector2Int(0, 2), step.SlimeCleared);
            Assert.AreEqual(RescueCell.Cage, engine.Kind(2, 3));
            Assert.AreEqual(RescueCell.Stone, engine.Kind(0, 4));
        }

        [Test]
        public void Obstacles_CanBeTappedDirectly_SoThePathNeverLocks()
        {
            RescueLayout layout = Column(5);
            layout.Kind[1, 2] = RescueCell.Cage;
            layout.Kind[1, 1] = RescueCell.Slime;
            var engine = new RescueEngine(layout);
            Assert.IsNotNull(engine.Tap(new Vector2Int(1, 1)), "slime-ul se curăță cu o atingere");
            Assert.AreEqual(RescueCell.Empty, engine.Kind(1, 1));
            RescueStep step = engine.Tap(new Vector2Int(1, 2));
            Assert.Contains(new Vector2Int(1, 2), step.CageHits);
            step = engine.Tap(new Vector2Int(1, 2));
            Assert.Contains(new Vector2Int(1, 2), step.CagesOpened);
            Assert.AreEqual(RescueCell.Piece, engine.Kind(1, 2));
        }

        [Test]
        public void Cage_OpensAfterTwoHits()
        {
            RescueLayout layout = Column(5);
            layout.Kind[2, 2] = RescueCell.Cage;
            layout.Color[1, 2] = 2;
            var engine = new RescueEngine(layout);
            RescueStep step = engine.Tap(new Vector2Int(1, 2)); // vecina din stânga a cuștii
            Assert.Contains(new Vector2Int(2, 2), step.CageHits);
            step = engine.Tap(new Vector2Int(2, 3)); // vecina de deasupra
            Assert.Contains(new Vector2Int(2, 2), step.CagesOpened);
            Assert.AreEqual(RescueCell.Piece, engine.Kind(2, 2));
        }

        [Test]
        public void BoboPower_ClearsTheNextBlockersOnTheBestPath_WithoutCountingAMove()
        {
            RescueLayout layout = Column(6);
            for (int y = 0; y < 6; y++) layout.Color[1, y] = y % 2 + 1; // coloana din mijloc: culori alternante
            var engine = new RescueEngine(layout);
            int before = engine.PathCost();
            RescueStep step = engine.PowerClear(4);
            Assert.AreEqual(4, step.Removed.Count);
            Assert.AreEqual(4, step.RemovedColors.Count);
            Assert.AreEqual(0, engine.Taps, "puterea nu e o mutare");
            Assert.Less(engine.PathCost(), before);
            engine.PowerClear(4);
            Assert.IsTrue(engine.Rescued);
        }

        [Test]
        public void GeneratedLevels_AreSolvable_Deterministic_AndFair()
        {
            for (int level = 2; level <= 260; level++)
            {
                if (!LevelConfig.IsRescueLevel(level)) continue;
                LevelConfig config = LevelConfig.For(level);
                Assert.AreEqual(LevelType.Rescue, config.Type, $"nivel {level}");
                RescueLayout layout = RescueLayout.For(level, config.Difficulty);
                Assert.Less(layout.BotMoves, 60, $"nivel {level}: botul îl salvează pe Bobo");
                Assert.Greater(config.Moves, layout.BotMoves, $"nivel {level}: jucătorul are rezervă");
                Assert.AreEqual(layout.BotMoves, RescueEngine.BotMoves(layout), $"nivel {level}: determinist");
                Assert.AreEqual(RescueCell.Piece, layout.Kind[layout.OutletX, 0]);
            }
        }

        [Test]
        public void Mix_TwoRescueOneClassic_BossesAndBonusUnchanged()
        {
            Assert.AreEqual(LevelType.Rescue, LevelConfig.For(101).Type);
            Assert.AreEqual(LevelType.Rescue, LevelConfig.For(103).Type);
            Assert.AreNotEqual(LevelType.Rescue, LevelConfig.For(102).Type);
            Assert.AreEqual(LevelType.Bonus, LevelConfig.For(105).Type);
            Assert.AreEqual(LevelType.Boss, LevelConfig.For(110).Type);
            Assert.AreNotEqual(LevelType.Rescue, LevelConfig.For(99).Type);
            Assert.AreEqual(LevelType.Rescue, LevelConfig.For(5).Type, "prima salvare vine devreme");
            Assert.AreEqual(LevelType.Rescue, LevelConfig.For(13).Type);
            Assert.AreNotEqual(LevelType.Rescue, LevelConfig.For(1).Type);
            var goals = new LevelGoals(LevelConfig.For(101), 0, 0, default);
            Assert.IsFalse(goals.IsComplete);
            goals.MarkRescued();
            Assert.IsTrue(goals.IsComplete);
        }
    }
}
