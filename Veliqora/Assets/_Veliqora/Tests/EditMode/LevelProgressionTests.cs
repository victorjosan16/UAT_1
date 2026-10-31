using System.Collections.Generic;
using NUnit.Framework;
using Veliqora.Core.Levels;
using Veliqora.Core.Puzzle;
using Veliqora.Core.Save;
using Veliqora.Core.Session;
using Veliqora.Core.Solver;

namespace Veliqora.Tests.EditMode
{
    /// <summary>
    /// Requirements for the level set: level 1 is a complete puzzle (3 colors × 4 layers, 3 mixed bottles + 2 empty,
    /// at least 6 pours proven by exhaustive search), no near-solved or duplicate starts, and the first 100 levels
    /// are complete, distinct and solvable.
    /// </summary>
    public class LevelProgressionTests
    {
        private static LevelCatalog Catalog => TestUtil.Catalog;

        [Test]
        public void Level1_IsACompletePuzzle_ReadyToPlay()
        {
            var l = Catalog.Levels[0];
            var s = l.CreateInitialState();
            Assert.AreEqual(3, l.ColorCount, "3 colors");
            Assert.AreEqual(5, s.BottleCount, "5 bottles");
            int full = 0, empty = 0;
            for (int i = 0; i < s.BottleCount; i++)
            {
                Assert.AreEqual(4, s[i].Capacity, "4 layers per bottle");
                if (s[i].IsEmpty) empty++;
                else
                {
                    Assert.AreEqual(4, s[i].Count, "filled bottles start full");
                    Assert.IsFalse(s[i].IsMonochrome, "every filled bottle is mixed");
                    full++;
                }
            }
            Assert.AreEqual(3, full);
            Assert.AreEqual(2, empty);
            for (int c = 1; c <= 3; c++) Assert.AreEqual(4, s.ColorTotal(c), "4 layers of color " + c);
            Assert.IsFalse(PuzzleRules.IsWon(s));
            Assert.IsTrue(string.IsNullOrEmpty(l.TutorialKey));
            Assert.IsEmpty(l.DisabledBoosters, "Undo / Restart / Hint all available");
            int min = MinimumMoves.Compute(s, 1_000_000);
            Assert.GreaterOrEqual(min, 6, "level 1 needs at least 6 pours (exhaustive search)");
            Assert.LessOrEqual(min, l.ParMoves);
            Assert.AreEqual(min, l.Validation.MinimumMoves, "stored proof matches");
        }

        [Test]
        public void Levels1To10_NeedAtLeast7Pours_ProvenByExhaustiveSearch()
        {
            for (int n = 2; n <= 10; n++)
            {
                var l = Catalog.Levels[n - 1];
                int min = MinimumMoves.Compute(l.CreateInitialState(), 1_000_000);
                Assert.GreaterOrEqual(min, 7, l.Id);
            }
        }

        [Test]
        public void First100Levels_AreComplete_Distinct_AndSolvable()
        {
            var keys = new HashSet<string>();
            var failures = new List<string>();
            for (int n = 1; n <= 100; n++)
            {
                var l = Catalog.Levels[n - 1];
                var s = l.CreateInitialState();
                if (PuzzleRules.IsWon(s)) failures.Add(l.Id + " starts solved");
                if (!LevelCatalogBuilder.IsWellMixed(s)) failures.Add(l.Id + " has a monochrome bottle or an unmixed color");
                for (int i = 0; i < s.BottleCount; i++) if (s.IsBottleComplete(i)) failures.Add(l.Id + " starts with a completed bottle");
                if (!keys.Add(s.CanonicalKey())) failures.Add(l.Id + " duplicates an earlier layout");
                if (!PuzzleSolver.VerifySolution(s, l.SolutionMoves(), out string err)) failures.Add(l.Id + ": " + err);
                if (l.ParMoves < 6) failures.Add(l.Id + " too short (" + l.ParMoves + ")");
            }
            Assert.IsEmpty(failures, string.Join("\n", failures));
        }

        [Test]
        public void NoDuplicateLayouts_InTheWholeCatalog()
        {
            var keys = new HashSet<string>();
            foreach (var l in Catalog.Levels) Assert.IsTrue(keys.Add(l.CreateInitialState().CanonicalKey()), l.Id);
        }

        [Test]
        public void Level1_Undo_Restart_Hint_Work_AndWinUnlocksLevel2()
        {
            var l = Catalog.Levels[0];
            var s = new GameSession(l, "t1");
            var h = s.GetHint();
            Assert.IsTrue(h.HasMove, h.Message);
            var before = s.Current;
            Assert.IsTrue(s.TryMove(h.Move.From, h.Move.To).IsLegal);
            Assert.AreNotEqual(before, s.Current);
            Assert.IsNotNull(s.Undo());
            Assert.AreEqual(before, s.Current, "undo restores the board");
            s.TryMove(h.Move.From, h.Move.To);
            s.Restart();
            Assert.AreEqual(l.CreateInitialState(), s.Current, "restart restores the start");
            Assert.AreEqual(0, s.MoveCount);

            for (int guard = 0; guard < 100 && !s.IsComplete; guard++)
            {
                var hint = s.GetHint();
                Assert.IsTrue(hint.HasMove, hint.Message);
                Assert.IsTrue(s.TryMove(hint.Move.From, hint.Move.To).IsLegal);
            }
            Assert.IsTrue(s.IsComplete, "following hints wins level 1");

            var p = new ProgressData();
            ProgressRules.RecordCompletion(p, 1, s.CurrentStars, Catalog.Levels.Count);
            Assert.AreEqual(2, p.HighestUnlocked, "next level opens after victory");
            Assert.AreEqual(2, p.CurrentLevel);

            // progress survives a save round trip
            var data = new SaveData { Progress = p };
            var again = SaveSerializer.Deserialize(SaveSerializer.Serialize(data));
            Assert.AreEqual(2, again.Progress.HighestUnlocked);
            Assert.AreEqual(p.Stars(1), again.Progress.Stars(1));
        }
    }
}
