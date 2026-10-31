using System.Collections.Generic;
using NUnit.Framework;
using Veliqora.Core.Json;
using Veliqora.Core.Levels;
using Veliqora.Core.Objectives;
using Veliqora.Core.Puzzle;
using Veliqora.Core.Session;
using Veliqora.Core.Solver;
using static Veliqora.Tests.EditMode.TestUtil;

namespace Veliqora.Tests.EditMode
{
    /// <summary>Mystery layers, corked bottles, rewarded bottles, mixed capacities, Remove Top Color, objectives.</summary>
    public class SpecialBottlesTests
    {
        private static BottleState Bottle(int cap, int[] units, int hidden = 0, BottleLock lk = null, bool ad = false) => new BottleState(cap, units, hidden, lk, ad);
        private static GameState State(params BottleState[] b) => new GameState(b);

        // ---------------- mystery ----------------

        [Test]
        public void Mystery_HiddenUnitsDoNotMoveWithTheTopRun_AndRevealWhenExposed()
        {
            // bottom two hidden: [2?, 1?, 1, 1] — only the two revealed 1s move together.
            var s = State(Bottle(4, B(2, 1, 1, 1), hidden: 2), BottleState.Empty(4));
            Assert.AreEqual(2, s[0].TopRunLength, "hidden 1 is not part of the visible group");
            var r = PuzzleRules.Evaluate(s, 0, 1);
            Assert.AreEqual(2, r.Move.Amount);
            Assert.AreEqual(1, r.After[0].Hidden, "the exposed unit is revealed; the one below stays hidden");
            Assert.AreEqual(1, r.After[0].TopColor, "real color kept internally");
        }

        [Test]
        public void Mystery_TopUnitIsNeverHidden_AndCompletedBottlesReveal()
        {
            Assert.AreEqual(3, Bottle(4, B(1, 2, 3, 4), hidden: 9).Hidden);
            var s = State(Bottle(4, B(1, 1, 1), hidden: 2), Bottle(4, B(2, 1)));
            var r = PuzzleRules.Evaluate(s, 1, 0); // completes bottle 0
            Assert.IsTrue(r.After.IsBottleComplete(0));
            Assert.AreEqual(0, r.After[0].Hidden);
        }

        [Test]
        public void Mystery_UndoRestoresVisibility_AndSnapshotPreservesIt()
        {
            var level = new LevelDefinition { Number = 90, Capacity = 4, ColorCount = 2, Bottles = new[] { B(2, 1, 2, 1), B(1, 2, 1, 2), B(), B() }, Hidden = new[] { 2, 2, 0, 0 } };
            level.Solution = LevelDefinition.EncodeMoves(PuzzleSolver.Solve(level.CreateInitialState()).Moves);
            var s = new GameSession(level, "m");
            Assert.IsTrue(s.TryMove(0, 2).IsLegal);
            Assert.AreEqual(2, s.Current[0].Hidden, "first pour doesn't reach the hidden layers");
            var before = s.Current;
            Assert.IsTrue(s.TryMove(0, 3).IsLegal);
            Assert.Less(s.Current[0].Hidden, before[0].Hidden, "second pour exposes (reveals) a hidden layer");
            var snap = SessionSnapshot.FromJson(MiniJson.AsObject(MiniJson.Parse(MiniJson.Serialize(s.ToSnapshot().ToJson()))));
            var restored = GameSession.FromSnapshot(level, snap);
            Assert.AreEqual(s.Current, restored.Current, "hidden/revealed state survives save/load");
            restored.Undo();
            Assert.AreEqual(before, restored.Current, "undo re-hides the layer");
        }

        // ---------------- corks ----------------

        [Test]
        public void Cork_BlocksPouringFromAndInto()
        {
            var s = State(Bottle(4, B(1, 2), lk: new BottleLock(LockKind.Moves, 5)), Bottle(4, B(2)), BottleState.Empty(4));
            Assert.AreEqual(MoveRejection.Locked, PuzzleRules.Evaluate(s, 0, 2).Rejection);
            Assert.AreEqual(MoveRejection.Locked, PuzzleRules.Evaluate(s, 1, 0).Rejection);
            Assert.IsFalse(PuzzleRules.IsLegal(s, 1, 0));
        }

        [Test]
        public void Cork_UnlockConditions_AllKinds_AndChains()
        {
            // Moves(2)
            var s = State(Bottle(4, B(3, 3, 3, 4), lk: new BottleLock(LockKind.Moves, 2)), Bottle(4, B(1, 2)), Bottle(4, B(2, 1)), BottleState.Empty(4), BottleState.Empty(4));
            var r1 = PuzzleRules.Evaluate(s, 1, 3);
            Assert.IsTrue(r1.After[0].IsCorked);
            var r2 = PuzzleRules.Evaluate(r1.After, 2, 4);
            Assert.IsFalse(r2.After[0].IsCorked);
            CollectionAssert.Contains(r2.Unlocked, 0);

            // CompleteColor(1) + AfterBottle chain
            var c = State(Bottle(4, B(1, 1, 1)), Bottle(4, B(2, 1)), Bottle(4, B(3, 2, 2, 2), lk: new BottleLock(LockKind.CompleteColor, 1)),
                          Bottle(4, B(2, 3, 3, 3), lk: new BottleLock(LockKind.AfterBottle, 2)));
            var rc = PuzzleRules.Evaluate(c, 1, 0);
            Assert.IsFalse(rc.After[2].IsCorked, "color 1 completed");
            Assert.IsFalse(rc.After[3].IsCorked, "chain opens after bottle 2");

            // CompletedBottles(1)
            var k = State(Bottle(4, B(1, 1, 1)), Bottle(4, B(2, 1)), Bottle(4, B(2, 2, 2), lk: new BottleLock(LockKind.CompletedBottles, 1)));
            Assert.IsFalse(PuzzleRules.Evaluate(k, 1, 0).After[2].IsCorked);
        }

        [Test]
        public void Cork_UndoRelocks_SnapshotKeepsState_AndWinNeedsTheBottleSorted()
        {
            var level = new LevelDefinition
            {
                Number = 105, Capacity = 4, ColorCount = 3,
                Bottles = new[] { B(1, 2, 1, 2), B(2, 1, 2, 3), B(3, 3, 3, 1), B(), B() },
                Locks = new List<LevelDefinition.LockDef> { new LevelDefinition.LockDef { Bottle = 2, Kind = LockKind.Moves, Param = 1 } },
            };
            // 1 unit of color 1 sits inside the cork; the puzzle cannot be won while the cork stays on.
            Assert.IsFalse(PuzzleRules.IsWon(level.CreateInitialState()));
            var solve = PuzzleSolver.Solve(level.CreateInitialState(), SolverOptions.Validation);
            Assert.AreEqual(SolveStatus.Solved, solve.Status);
            Assert.IsTrue(PuzzleSolver.VerifySolution(level.CreateInitialState(), solve.Moves, out var err), err);
            level.Solution = LevelDefinition.EncodeMoves(solve.Moves);

            var s = new GameSession(level, "cork");
            Assert.IsTrue(s.Current[2].IsCorked);
            s.TryMove(solve.Moves[0].From, solve.Moves[0].To);
            Assert.IsFalse(s.Current[2].IsCorked, "unlocked after one move");
            var restored = GameSession.FromSnapshot(level, SessionSnapshot.FromJson(MiniJson.AsObject(MiniJson.Parse(MiniJson.Serialize(s.ToSnapshot().ToJson())))));
            Assert.IsFalse(restored.Current[2].IsCorked, "unlock persists");
            restored.Undo();
            Assert.IsTrue(restored.Current[2].IsCorked, "undo restores the cork");
        }

        [Test]
        public void Solver_ProvesUnsolvable_WhenACorkCanNeverOpen()
        {
            // Cork waits for color 1 to complete, but a unit of color 1 is trapped inside it.
            var s = State(Bottle(4, B(1, 1, 1, 2), lk: new BottleLock(LockKind.CompleteColor, 1)), Bottle(4, B(2, 2, 2)), BottleState.Empty(4));
            Assert.AreEqual(SolveStatus.Unsolvable, PuzzleSolver.Solve(s, SolverOptions.Validation).Status);
        }

        // ---------------- rewarded bottle ----------------

        [Test]
        public void RewardBottle_IsUnusableUntilUnlocked_AndStaysUnlocked()
        {
            var level = MakeLevel(115, 4, B(1, 2, 1, 2), B(2, 1, 2, 1), B(), B(), B());
            level.RewardBottles = new[] { 4 };
            var s = new GameSession(level, "ad");
            Assert.IsTrue(s.Current[4].RewardLocked);
            Assert.AreEqual(MoveRejection.Locked, PuzzleRules.Evaluate(s.Current, 0, 4).Rejection);
            Assert.IsFalse(PuzzleRules.IsWon(s.Current));
            s.TryMove(0, 2);
            Assert.IsTrue(s.UnlockRewardBottle(4));
            Assert.IsFalse(s.UnlockRewardBottle(4), "only once");
            Assert.IsTrue(PuzzleRules.IsLegal(s.Current, 0, 4));
            s.Undo();
            Assert.IsFalse(s.Current[4].RewardLocked, "undo keeps the paid-for unlock");
            s.Restart();
            Assert.IsFalse(s.Current[4].RewardLocked, "restart keeps it too");
        }

        // ---------------- capacities ----------------

        [TestCase(2)]
        [TestCase(3)]
        [TestCase(5)]
        [TestCase(6)]
        public void VariableCapacities_LimitTransfers(int cap)
        {
            var s = State(Bottle(6, B(1, 1, 1, 1, 1, 1)), BottleState.Empty(cap));
            var r = PuzzleRules.Evaluate(s, 0, 1);
            Assert.AreEqual(cap, r.Move.Amount);
            Assert.IsTrue(r.After[1].IsFull);
        }

        [Test]
        public void MixedCapacities_WinWhenEachColorIsTogether()
        {
            // color 1 has 3 units (fits the 3-bottle), color 2 has 5 units (fits the 6-bottle).
            var won = State(Bottle(3, B(1, 1, 1)), Bottle(6, B(2, 2, 2, 2, 2)), BottleState.Empty(4));
            Assert.IsTrue(PuzzleRules.IsWon(won));
            var notYet = State(Bottle(3, B(1, 1)), Bottle(6, B(2, 2, 2, 2, 2, 1)), BottleState.Empty(4));
            Assert.IsFalse(PuzzleRules.IsWon(notYet));
            var r = PuzzleSolver.Solve(notYet);
            Assert.AreEqual(SolveStatus.Solved, r.Status);
        }

        // ---------------- shuffle / remove color ----------------

        [Test]
        public void Shuffle_PreservesCorksCapacitiesAndQuantities_AndIsSolvable()
        {
            var level = TestUtil.Catalog.Levels.Find(l => l.HasLocks && l.Number > 100 && l.Number < 121);
            Assert.IsNotNull(level, "catalog has corked levels in 101–120");
            var start = level.CreateInitialState();
            var g = LevelGenerator.GenerateShuffle(level, 0);
            Assert.IsNotNull(g);
            for (int i = 0; i < start.BottleCount; i++)
            {
                Assert.AreEqual(start[i].Capacity, g.State[i].Capacity);
                Assert.AreEqual(start[i].Count, g.State[i].Count);
                if (start[i].IsLocked) Assert.AreEqual(start[i], g.State[i], "corked bottles are fixed");
            }
            CollectionAssert.AreEquivalent(start.ColorCounts(), g.State.ColorCounts());
            Assert.IsTrue(PuzzleSolver.VerifySolution(g.State, g.Solve.Moves, out var err), err);
        }

        [Test]
        public void RemoveTopColor_RemovesTheColor_ClearsUndo_AndOpensMatchingCorks()
        {
            var level = MakeLevel(60, 4, B(1, 2, 3, 1), B(2, 3, 1, 2), B(3, 1, 2, 3), B(), B());
            var s = new GameSession(level, "rm");
            s.TryMove(0, 3);
            Assert.IsTrue(s.PreviewRemoveColor(1, out int color, out var current, out var initial));
            Assert.AreEqual(2, color);
            Assert.AreEqual(0, current.ColorTotal(2));
            Assert.AreEqual(SolveStatus.Solved, PuzzleSolver.Solve(current).Status, "caller verifies solvability before committing");
            s.CommitRemoveColor(color, current, initial);
            Assert.IsFalse(s.CanUndo);
            Assert.AreEqual(0, s.Initial.ColorTotal(2), "restart keeps the removal");
            var snap = s.ToSnapshot();
            Assert.IsNotNull(GameSession.FromSnapshot(level, SessionSnapshot.FromJson(MiniJson.AsObject(MiniJson.Parse(MiniJson.Serialize(snap.ToJson()))))));

            var lockState = State(Bottle(4, B(3, 3, 3, 1), lk: new BottleLock(LockKind.CompleteColor, 2)), Bottle(4, B(2, 1)));
            var after = PuzzleRules.ResolveLocks(lockState.WithAllBottles(new[] { lockState[0], lockState[1].RemoveColor(2) }));
            Assert.IsFalse(after[0].IsCorked, "a cork waiting for a removed color opens");
        }

        // ---------------- objectives ----------------

        [Test]
        public void Objectives_ProgressIsDerivedFromState_UndoConsistent_LockedAndRewarded()
        {
            var defs = new List<ObjectiveDef>
            {
                new ObjectiveDef { Id = "a", Type = ObjectiveType.CollectColor, Color = 1, Target = 1, RewardCoins = 10 },
                new ObjectiveDef { Id = "b", Type = ObjectiveType.CompleteBottles, Target = 2, RewardCoins = 10, RequiresId = "a" },
                new ObjectiveDef { Id = "c", Type = ObjectiveType.FinishLevel, Target = 1, RewardCoins = 40, Rewarded = true },
            };
            var start = State(Bottle(4, B(1, 1, 1)), Bottle(4, B(2, 2, 2, 1)), BottleState.Empty(4));
            var st0 = ObjectiveTracker.Evaluate(defs, start, start, false);
            Assert.IsFalse(st0[0].Complete);
            Assert.IsFalse(st0[1].Unlocked, "b is locked until a completes");
            var after = PuzzleRules.Evaluate(start, 1, 0).After;
            var st1 = ObjectiveTracker.Evaluate(defs, start, after, PuzzleRules.IsWon(after));
            Assert.IsTrue(st1[0].Complete);
            Assert.IsTrue(st1[1].Unlocked);
            Assert.IsTrue(st1[1].Complete);
            Assert.IsTrue(st1[2].Complete && st1[2].Def.Rewarded);
            // "undo" = evaluating the earlier state again gives the earlier progress
            Assert.IsFalse(ObjectiveTracker.Evaluate(defs, start, start, false)[0].Complete);
        }

        // ---------------- catalog ----------------

        [Test]
        public void Catalog_IntroducesMechanics_AlongTheBriefCurve()
        {
            var levels = TestUtil.Catalog.Levels;
            Assert.IsFalse(levels.Exists(l => l.Number < 71 && (l.HasMystery || l.HasLocks || l.HasMixedCapacities)), "no special bottles before 71");
            Assert.IsTrue(levels.Exists(l => l.Number >= 71 && l.Number <= 120 && l.HasMystery), "mystery in 71–120");
            Assert.IsTrue(levels.Exists(l => l.Number >= 71 && l.Number <= 120 && l.HasMixedCapacities), "sizes in 71–120");
            Assert.IsTrue(levels.Exists(l => l.Number >= 71 && l.Number <= 120 && l.HasLocks), "corks in 71–120");
            Assert.IsTrue(levels.Exists(l => l.RewardBottles.Length > 0), "rewarded bottles exist");
            int combos = levels.FindAll(l => l.Number >= 121 && l.Number <= 200 &&
                (l.HasMystery ? 1 : 0) + (l.HasLocks ? 1 : 0) + (l.HasMixedCapacities ? 1 : 0) >= 2).Count;
            Assert.Greater(combos, 20, "121–200 combine mechanics");
            Assert.IsTrue(levels.TrueForAll(l => l.Difficulty >= 1 && l.Difficulty <= 10));
            Assert.IsTrue(levels.Exists(l => l.Objectives.Count > 0 && l.Number >= 31));
            Assert.IsTrue(levels.Exists(l => l.Objectives.Exists(o => o.Rewarded)), "rewarded objectives on boss levels");
            Assert.IsTrue(levels.Exists(l => l.DisabledBoosters.Count > 0), "booster restrictions are data-driven");
        }

        [Test]
        public void RewardBottles_AreNeverNeeded_ByTheStoredSolution()
        {
            foreach (var l in TestUtil.Catalog.Levels)
                foreach (var m in l.Solution)
                {
                    Assert.AreEqual(-1, System.Array.IndexOf(l.RewardBottles, m[0]), l.Id);
                    Assert.AreEqual(-1, System.Array.IndexOf(l.RewardBottles, m[1]), l.Id);
                }
        }
    }
}
