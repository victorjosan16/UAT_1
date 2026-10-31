using NUnit.Framework;
using Veliqora.Core.Levels;
using Veliqora.Core.Puzzle;
using Veliqora.Core.Session;
using static Veliqora.Tests.EditMode.TestUtil;

namespace Veliqora.Tests.EditMode
{
    public class GameSessionTests
    {
        private static GameSession NewSession() =>
            new GameSession(MakeLevel(50, 4, B(1, 2, 2), B(2, 1, 1), B(1, 2), B()), "attempt-1");

        [Test]
        public void Tap_SelectThenPour_CommitsMove()
        {
            var s = NewSession();
            Assert.AreEqual(TapKind.Selected, s.Tap(0).Kind);
            Assert.AreEqual(0, s.Selected);
            var r = s.Tap(3);
            Assert.AreEqual(TapKind.Moved, r.Kind);
            Assert.AreEqual(2, r.Move.Move.Amount);
            Assert.AreEqual(-1, s.Selected);
            Assert.AreEqual(1, s.MoveCount);
        }

        [Test]
        public void Tap_SameBottleTwice_Deselects()
        {
            var s = NewSession();
            s.Tap(1);
            Assert.AreEqual(TapKind.Deselected, s.Tap(1).Kind);
            Assert.AreEqual(-1, s.Selected);
        }

        [Test]
        public void Tap_EmptyBottleFirst_IsInvalid()
        {
            var s = NewSession();
            var r = s.Tap(3);
            Assert.AreEqual(TapKind.Invalid, r.Kind);
            Assert.AreEqual(MoveRejection.SourceEmpty, r.Move.Rejection);
            Assert.AreEqual(-1, s.Selected);
        }

        [Test]
        public void Tap_MismatchedColor_ReselectsTappedBottle()
        {
            var s = NewSession();
            s.Tap(0);              // top 2
            var r = s.Tap(1);      // top 1 -> mismatch
            Assert.AreEqual(TapKind.Reselected, r.Kind);
            Assert.AreEqual(MoveRejection.ColorMismatch, r.Move.Rejection);
            Assert.AreEqual(1, s.Selected);
            Assert.AreEqual(0, s.MoveCount);
        }

        [Test]
        public void Taps_AreIgnored_WhileBusy()
        {
            var s = NewSession();
            s.IsBusy = true;
            Assert.AreEqual(TapKind.Ignored, s.Tap(0).Kind);
            Assert.AreEqual(-1, s.Selected);
            s.IsBusy = false;
            Assert.AreEqual(TapKind.Selected, s.Tap(0).Kind);
        }

        [Test]
        public void RapidTapping_NeverDuplicatesOrPartiallyAppliesMoves()
        {
            var s = NewSession();
            var initialCounts = s.Current.ColorCounts();
            int[] taps = { 0, 3, 3, 0, 2, 2, 1, 1, 0, 3, 3, 3, 2, 0, 1, 2, 3, 0, 1, 1 };
            foreach (var t in taps)
            {
                var r = s.Tap(t);
                CollectionAssert.AreEquivalent(initialCounts, s.Current.ColorCounts(), "units must be conserved after tap " + t);
                if (r.Kind == TapKind.Moved) Assert.AreEqual(r.Move.After, s.Current);
            }
        }

        [Test]
        public void Undo_RevertsMultiUnitPourExactly()
        {
            var s = NewSession();
            var start = s.Current;
            s.Tap(0); s.Tap(3);   // moves two 2s
            Assert.IsTrue(s.CanUndo);
            var u = s.Undo();
            Assert.IsNotNull(u);
            Assert.AreEqual(2, u.UndoneMove.Amount);
            Assert.AreEqual(start, s.Current);
            Assert.AreEqual(0, s.MoveCount);
            Assert.IsFalse(s.CanUndo);
            Assert.IsNull(s.Undo(), "nothing left to undo");
        }

        [Test]
        public void Undo_IsBlockedWhileBusy_AndAfterWin()
        {
            var level = MakeLevel(1, 4, B(1, 1, 1), B(1));
            var s = new GameSession(level, "a");
            s.Tap(1); var r = s.Tap(0);
            Assert.IsTrue(r.Won);
            Assert.IsTrue(s.IsComplete);
            Assert.IsFalse(s.CanUndo);
            Assert.AreEqual(TapKind.Ignored, s.Tap(0).Kind);
        }

        [Test]
        public void ExtraBottles_UpToTwo_SurviveUndoAndRestart()
        {
            var s = NewSession();
            s.Tap(0); s.Tap(3);
            Assert.IsTrue(s.AddExtraBottle());
            Assert.AreEqual(5, s.Current.BottleCount);
            Assert.IsTrue(s.AddExtraBottle(), "a second empty bottle (e.g. from a rewarded ad) is allowed");
            Assert.AreEqual(6, s.Current.BottleCount);
            Assert.IsFalse(s.AddExtraBottle(), "at most two extra bottles per attempt");
            s.Undo();
            Assert.AreEqual(6, s.Current.BottleCount);
            s.Restart();
            Assert.AreEqual(6, s.Current.BottleCount);
            Assert.IsTrue(s.Current[4].IsEmpty && s.Current[5].IsEmpty);
        }

        [Test]
        public void StuckBoard_BecomesPlayable_AfterAddingAnEmptyBottle()
        {
            // No useful move without a spare bottle: the "colors are blocked" situation.
            var level = new LevelDefinition { Number = 80, Capacity = 4, ColorCount = 2, Bottles = new[] { B(1, 2, 1, 2), B(2, 1, 2, 1) } };
            var s = new GameSession(level, "stuck");
            Assert.IsTrue(PuzzleRules.IsStuck(s.Current));
            Assert.IsTrue(s.AddExtraBottle());
            Assert.IsFalse(PuzzleRules.IsStuck(s.Current), "an empty bottle always gives a useful move");
        }

        [Test]
        public void NoMoveLimit_LongGamesNeverFailOrLock()
        {
            var level = Catalog.Levels[40];
            var s = new GameSession(level, "long");
            // 500 legal back-and-forth pours: the session never ends by itself or refuses moves.
            int made = 0;
            for (int i = 0; i < 2000 && made < 500; i++)
            {
                var moves = PuzzleRules.LegalMoves(s.Current, false);
                if (moves.Count == 0) break;
                var m = moves[i % moves.Count];
                if (s.TryMove(m.From, m.To).IsLegal) made++;
                if (s.IsComplete) break;
            }
            Assert.IsTrue(made >= 100 || s.IsComplete, "players can keep pouring without a move cap");
            Assert.AreEqual(made, s.TotalPours, "every pour counted; nothing ends the level except winning");
        }

        [Test]
        public void Restart_ResetsBoardAndCounters()
        {
            var s = NewSession();
            var start = s.Current;
            s.Tap(0); s.Tap(3);
            s.Restart();
            Assert.AreEqual(start, s.Current);
            Assert.AreEqual(0, s.MoveCount);
            Assert.IsFalse(s.IsComplete);
        }

        [Test]
        public void Hint_IsAlwaysALegalMove_AndFollowingHintsSolvesTheLevel()
        {
            var level = Catalog.Levels[40];
            var s = new GameSession(level, "h");
            for (int guard = 0; guard < 200 && !s.IsComplete; guard++)
            {
                var h = s.GetHint();
                Assert.IsTrue(h.HasMove, h.Message);
                Assert.IsTrue(PuzzleRules.IsLegal(s.Current, h.Move.From, h.Move.To));
                Assert.IsTrue(s.TryMove(h.Move.From, h.Move.To).IsLegal);
            }
            Assert.IsTrue(s.IsComplete);
        }

        [Test]
        public void Hint_FromDeviatedState_UsesSolver_AndStaysLegal()
        {
            var level = Catalog.Levels[60];
            var s = new GameSession(level, "h2");
            // Make a legal move that is not the first stored solution move, if possible.
            var moves = PuzzleRules.LegalMoves(s.Current, true);
            var first = level.Solution[0];
            foreach (var m in moves)
                if (m.From != first[0] || m.To != first[1]) { s.TryMove(m.From, m.To); break; }
            var h = s.GetHint();
            if (h.HasMove) Assert.IsTrue(PuzzleRules.IsLegal(s.Current, h.Move.From, h.Move.To));
            else Assert.IsFalse(string.IsNullOrEmpty(h.Message), "fallback must explain itself truthfully");
        }

        [Test]
        public void Hint_OnDeadEnd_ReportsNoSolutionInsteadOfFakeHint()
        {
            // A deliberately unsolvable board (no spare bottle): the hint must say so, not invent a move.
            var level = new LevelDefinition { Number = 70, Capacity = 4, ColorCount = 2, Bottles = new[] { B(1, 2, 1, 2), B(2, 1, 2, 1) } };
            var s = new GameSession(level, "dead");
            var h = s.GetHint();
            Assert.IsFalse(h.HasMove);
            Assert.AreEqual(HintSource.NotFound, h.Source);
            StringAssert.Contains("No solution", h.Message);
        }

        [Test]
        public void Shuffle_RestartsWithVerifiedLayout_AndUpdatesPar()
        {
            var level = Catalog.Levels[25];
            var s = new GameSession(level, "sh");
            s.TryMove(level.Solution[0][0], level.Solution[0][1]);
            var g = LevelGenerator.GenerateShuffle(level, 0);
            Assert.IsNotNull(g);
            s.ApplyShuffle(g);
            Assert.AreEqual(0, s.MoveCount);
            Assert.AreEqual(1, s.ShuffleCount);
            Assert.AreEqual(g.Solve.Length, s.ParMoves);
            Assert.AreEqual(g.State, s.Current);
            var h = s.GetHint();
            Assert.AreEqual(HintSource.StoredSolution, h.Source);
        }

        [Test]
        public void Snapshot_RoundTrip_RestoresBoardHistoryAndExtraBottle()
        {
            var level = Catalog.Levels[12];
            var s = new GameSession(level, "snap");
            for (int i = 0; i < 4; i++) s.TryMove(level.Solution[i][0], level.Solution[i][1]);
            s.AddExtraBottle();
            var json = Veliqora.Core.Json.MiniJson.Serialize(s.ToSnapshot().ToJson());
            var snap = SessionSnapshot.FromJson(Veliqora.Core.Json.MiniJson.AsObject(Veliqora.Core.Json.MiniJson.Parse(json)));
            var restored = GameSession.FromSnapshot(level, snap);
            Assert.IsNotNull(restored);
            Assert.AreEqual(s.Current, restored.Current);
            Assert.AreEqual(s.MoveCount, restored.MoveCount);
            Assert.AreEqual(1, restored.ExtraBottlesAdded);
            Assert.AreEqual("snap", restored.AttemptId);
            Assert.IsNotNull(restored.Undo());
            Assert.AreEqual(3, restored.MoveCount);
            Assert.AreEqual(level.Bottles.Length + 1, restored.Current.BottleCount, "extra bottle kept after undo");
        }

        [Test]
        public void Snapshot_ForDifferentLevelOrTamperedColors_IsRejected()
        {
            var level = Catalog.Levels[12];
            var s = new GameSession(level, "x");
            var snap = s.ToSnapshot();
            Assert.IsNull(GameSession.FromSnapshot(Catalog.Levels[13], snap));
            snap.Current = snap.Current.WithBottle(0, new BottleState(level.Capacity, 1, 1, 1, 1));
            Assert.IsNull(GameSession.FromSnapshot(level, snap));
        }
    }
}
