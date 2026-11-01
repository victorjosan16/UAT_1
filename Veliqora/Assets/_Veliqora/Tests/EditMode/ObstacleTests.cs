using NUnit.Framework;
using Veliqora.Core.Levels;
using Veliqora.Core.Puzzle;
using Veliqora.Core.Solver;
using static Veliqora.Tests.EditMode.TestUtil;

namespace Veliqora.Tests.EditMode
{
    /// <summary>Frozen and locked bottles against the pouring rules, plus the generator for big (12–20 bottle) boards.</summary>
    public class ObstacleTests
    {
        private static GameState WithLock(GameState s, int i, LockKind kind, int param) =>
            s.WithBottle(i, s[i].WithLock(new BottleLock(kind, param)));

        [Test]
        public void ObstacleKinds_MapToFrozenAndLocked()
        {
            Assert.IsTrue(new BottleLock(LockKind.Moves, 3).IsFrozen);
            Assert.IsTrue(new BottleLock(LockKind.CompletedBottles, 1).IsFrozen);
            Assert.AreEqual(ObstacleKind.Locked, new BottleLock(LockKind.CompleteColor, 2).Obstacle);
            Assert.AreEqual(ObstacleKind.Locked, new BottleLock(LockKind.AfterBottle, 0).Obstacle);
        }

        [Test]
        public void FrozenBottle_BlocksPours_ThenThawsAfterNPours()
        {
            var s = S(4, B(1, 2, 1, 2), B(2, 1, 2, 1), B(), B(), B());
            s = WithLock(s, 4, LockKind.Moves, 2);
            Assert.AreEqual(MoveRejection.Locked, PuzzleRules.Evaluate(s, 0, 4).Rejection, "cannot pour into ice");
            var r1 = PuzzleRules.Evaluate(s, 0, 2);
            Assert.IsTrue(r1.IsLegal);
            Assert.IsTrue(r1.After[4].IsCorked, "still frozen after 1 pour");
            var r2 = PuzzleRules.Evaluate(r1.After, 1, 3);
            Assert.IsTrue(r2.IsLegal);
            Assert.IsFalse(r2.After[4].IsCorked, "thawed after 2 pours");
            CollectionAssert.Contains(r2.Unlocked, 4);
            Assert.IsTrue(PuzzleRules.Evaluate(r2.After, 0, 4).IsLegal, "usable once thawed");
        }

        [Test]
        public void FrozenBottle_WithContents_CannotBeASource()
        {
            var s = S(4, B(1, 1, 2, 2), B(2, 2, 1, 1), B(), B());
            s = WithLock(s, 0, LockKind.CompletedBottles, 1);
            Assert.AreEqual(MoveRejection.Locked, PuzzleRules.Evaluate(s, 0, 2).Rejection);
        }

        [Test]
        public void LockedBottle_OpensWhenKeyColorIsCompleted()
        {
            // Color 1 is the key: completing a bottle of color 1 opens the padlock on bottle 3.
            var s = S(4, B(1, 1, 1), B(2, 2, 2, 1), B(2), B());
            s = WithLock(s, 3, LockKind.CompleteColor, 1);
            Assert.AreEqual(MoveRejection.Locked, PuzzleRules.Evaluate(s, 1, 3).Rejection);
            var r = PuzzleRules.Evaluate(s, 1, 0); // completes color 1
            Assert.IsTrue(r.IsLegal);
            Assert.IsFalse(r.After[3].IsCorked, "padlock opened by the key color");
        }

        [Test]
        public void LockChain_OpensAfterTheFirstLock()
        {
            var s = S(4, B(1, 2, 1, 2), B(2, 1, 2, 1), B(), B(), B());
            s = WithLock(s, 3, LockKind.Moves, 1);
            s = WithLock(s, 4, LockKind.AfterBottle, 3);
            var r = PuzzleRules.Evaluate(s, 0, 2);
            Assert.IsFalse(r.After[3].IsCorked);
            Assert.IsFalse(r.After[4].IsCorked, "chained lock opens in the same step");
        }

        [Test]
        public void Undo_RestoresFrozenState()
        {
            var level = TestUtil.Catalog.Levels.Find(l => l.HasLocks && l.Locks.TrueForAll(k => k.Kind == LockKind.Moves));
            Assert.IsNotNull(level, "a level with move-frozen bottles exists");
            var session = new Core.Session.GameSession(level, "u");
            var start = session.Current;
            var h = session.GetHint();
            Assert.IsTrue(session.TryMove(h.Move.From, h.Move.To).IsLegal);
            session.Undo();
            Assert.AreEqual(start, session.Current, "undo restores ice and move counter");
        }

        [Test]
        public void Generator_FrozenAndLockedStyles_ProduceOnlyThatObstacle()
        {
            var frozen = LevelGenerator.Generate(new GenerationSpec { Colors = 6, Locks = 2, LockStyle = LockStyle.Frozen, MaxAttempts = 200 }, 7);
            var locked = LevelGenerator.Generate(new GenerationSpec { Colors = 6, Locks = 2, LockStyle = LockStyle.Locked, MaxAttempts = 200 }, 7);
            Assert.IsNotNull(frozen);
            Assert.IsNotNull(locked);
            int f = 0, l = 0;
            for (int i = 0; i < frozen.State.BottleCount; i++) if (frozen.State[i].Lock != null) { Assert.IsTrue(frozen.State[i].Lock.IsFrozen); f++; }
            for (int i = 0; i < locked.State.BottleCount; i++) if (locked.State[i].Lock != null) { Assert.IsFalse(locked.State[i].Lock.IsFrozen); l++; }
            Assert.Greater(f, 0);
            Assert.Greater(l, 0);
            Assert.IsTrue(PuzzleSolver.VerifySolution(frozen.State, frozen.Solve.Moves, out _));
            Assert.IsTrue(PuzzleSolver.VerifySolution(locked.State, locked.Solve.Moves, out _));
        }

        [Test]
        public void DoubledColors_Build20BottleBoards_ThatAreSolvable()
        {
            var spec = new GenerationSpec { Colors = 8, DoubleColors = 8, EmptyBottles = 0, SpreadBottles = 1, SmallEmpties = 2, RewardBottles = 1, MaxAttempts = 200 };
            var g = LevelGenerator.Generate(spec, 99);
            Assert.IsNotNull(g, "20-bottle board generated within budget");
            Assert.AreEqual(20, g.State.BottleCount);
            for (int c = 1; c <= 8; c++) Assert.AreEqual(8, g.State.ColorTotal(c), "doubled colors fill two bottles");
            Assert.IsTrue(PuzzleSolver.VerifySolution(g.State, g.Solve.Moves, out string err), err);
        }

        [Test]
        public void EveryCatalogLock_IsReachable_InTheStoredSolution()
        {
            foreach (var l in TestUtil.Catalog.Levels)
            {
                if (!l.HasLocks) continue;
                var s = l.CreateInitialState();
                foreach (var m in l.SolutionMoves()) s = PuzzleRules.Evaluate(s, m.From, m.To).After;
                for (int i = 0; i < s.BottleCount; i++)
                    Assert.IsFalse(s[i].IsCorked, l.Id + ": bottle " + i + " still frozen/locked at the win");
            }
        }
    }
}
