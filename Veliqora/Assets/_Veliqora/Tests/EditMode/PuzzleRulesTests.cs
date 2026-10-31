using NUnit.Framework;
using Veliqora.Core.Puzzle;
using static Veliqora.Tests.EditMode.TestUtil;

namespace Veliqora.Tests.EditMode
{
    public class PuzzleRulesTests
    {
        [Test]
        public void PourIntoEmptyBottle_IsLegal_AndMovesWholeTopRun()
        {
            var s = S(4, B(1, 2, 2), B());
            var r = PuzzleRules.Evaluate(s, 0, 1);
            Assert.IsTrue(r.IsLegal);
            Assert.AreEqual(2, r.Move.Amount);
            Assert.AreEqual(2, r.Move.Color);
            CollectionAssert.AreEqual(new[] { 1 }, r.After[0].ToArray());
            CollectionAssert.AreEqual(new[] { 2, 2 }, r.After[1].ToArray());
        }

        [Test]
        public void PourOntoMatchingColor_IsLegal()
        {
            var r = PuzzleRules.Evaluate(S(4, B(1, 3), B(2, 3)), 0, 1);
            Assert.IsTrue(r.IsLegal);
            CollectionAssert.AreEqual(new[] { 2, 3, 3 }, r.After[1].ToArray());
        }

        [Test]
        public void PourOntoDifferentColor_IsRejected()
        {
            var r = PuzzleRules.Evaluate(S(4, B(1, 3), B(2, 1)), 0, 1);
            Assert.IsFalse(r.IsLegal);
            Assert.AreEqual(MoveRejection.ColorMismatch, r.Rejection);
            Assert.AreSame(r.Before, r.After);
        }

        [Test]
        public void PourFromEmpty_IsRejected()
        {
            Assert.AreEqual(MoveRejection.SourceEmpty, PuzzleRules.Evaluate(S(4, B(), B(1)), 0, 1).Rejection);
        }

        [Test]
        public void PourIntoFull_IsRejected()
        {
            Assert.AreEqual(MoveRejection.DestinationFull, PuzzleRules.Evaluate(S(4, B(1), B(1, 1, 1, 1)), 0, 1).Rejection);
        }

        [Test]
        public void PourIntoSelf_IsRejected()
        {
            Assert.AreEqual(MoveRejection.SameBottle, PuzzleRules.Evaluate(S(4, B(1), B()), 0, 0).Rejection);
        }

        [TestCase(-1, 0)]
        [TestCase(0, 5)]
        [TestCase(2, 0)]
        public void OutOfRangeIndices_AreRejected(int from, int to)
        {
            Assert.AreEqual(MoveRejection.OutOfRange, PuzzleRules.Evaluate(S(4, B(1), B()), from, to).Rejection);
        }

        [Test]
        public void ContiguousRun_IsLimitedByDestinationFreeSpace()
        {
            // Source has three 2s on top; destination has room for one.
            var r = PuzzleRules.Evaluate(S(4, B(1, 2, 2, 2), B(3, 3, 2)), 0, 1);
            Assert.IsTrue(r.IsLegal);
            Assert.AreEqual(1, r.Move.Amount);
            CollectionAssert.AreEqual(new[] { 1, 2, 2 }, r.After[0].ToArray());
            CollectionAssert.AreEqual(new[] { 3, 3, 2, 2 }, r.After[1].ToArray());
        }

        [Test]
        public void OnlyTopRunMoves_NotUnitsBelowADifferentColor()
        {
            var r = PuzzleRules.Evaluate(S(4, B(2, 1, 2), B()), 0, 1);
            Assert.AreEqual(1, r.Move.Amount);
            CollectionAssert.AreEqual(new[] { 2, 1 }, r.After[0].ToArray());
        }

        [Test]
        public void Move_ConservesUnits_AndDoesNotMutateOriginal()
        {
            var s = S(4, B(1, 2, 2), B(2));
            var before = s.ExactKey();
            var r = PuzzleRules.Evaluate(s, 0, 1);
            Assert.AreEqual(before, s.ExactKey(), "original state must be immutable");
            Assert.AreEqual(s.ColorCounts()[2], r.After.ColorCounts()[2]);
            Assert.AreEqual(s.ColorCounts()[1], r.After.ColorCounts()[1]);
        }

        [Test]
        public void CustomCapacity_IsRespected()
        {
            var s = S(5, B(1, 1, 1, 1), B(1));
            var r = PuzzleRules.Evaluate(s, 1, 0);
            Assert.IsTrue(r.IsLegal);
            Assert.IsTrue(r.After[0].IsCompleted);
            Assert.Throws<System.ArgumentException>(() => S(3, B(1, 1, 1, 1)));
        }

        [Test]
        public void WinCondition_AllEmptyOrFullMonochrome()
        {
            Assert.IsTrue(PuzzleRules.IsWon(S(4, B(1, 1, 1, 1), B(2, 2, 2, 2), B())));
            Assert.IsFalse(PuzzleRules.IsWon(S(4, B(1, 1, 1), B(1))), "partial bottles are not solved");
            Assert.IsFalse(PuzzleRules.IsWon(S(4, B(1, 1, 2, 2), B(2, 2, 1, 1), B())));
            Assert.IsTrue(PuzzleRules.IsWon(S(4, B(), B())), "all-empty board counts as solved");
        }

        [Test]
        public void OneColorPuzzle_IsSolvedByConsolidating()
        {
            var s = S(4, B(1, 1, 1), B(1));
            Assert.IsFalse(PuzzleRules.IsWon(s));
            var r = PuzzleRules.Evaluate(s, 1, 0);
            Assert.IsTrue(PuzzleRules.IsWon(r.After));
        }

        [Test]
        public void Stuck_WhenNoUsefulMoveRemains()
        {
            Assert.IsTrue(PuzzleRules.IsStuck(S(4, B(1, 2, 1, 2), B(2, 1, 2, 1))));
            Assert.IsFalse(PuzzleRules.IsStuck(S(4, B(1, 2, 1, 2), B(2, 1, 2, 1), B())));
            Assert.IsFalse(PuzzleRules.IsStuck(S(4, B(1, 1, 1, 1))), "won is not stuck");
        }

        [Test]
        public void UsefulMoves_ExcludeRelabelingAndPouringFromCompleted()
        {
            var s = S(4, B(1, 1), B(), B(2, 2, 2, 2), B(2));
            Assert.IsFalse(PuzzleRules.IsUseful(s, 0, 1), "monochrome into empty is a relabel");
            Assert.IsFalse(PuzzleRules.IsUseful(s, 2, 1), "completed bottle never needs to move");
            Assert.IsTrue(PuzzleRules.IsLegal(s, 0, 1), "but it is still legal for the player");
        }

        [Test]
        public void CanonicalKey_IgnoresBottleOrder()
        {
            Assert.AreEqual(S(4, B(1, 2), B(3)).CanonicalKey(), S(4, B(3), B(1, 2)).CanonicalKey());
            Assert.AreNotEqual(S(4, B(1, 2), B(3)).ExactKey(), S(4, B(3), B(1, 2)).ExactKey());
        }

        [Test]
        public void ColorDistribution_Validation()
        {
            Assert.IsTrue(PuzzleRules.HasValidColorDistribution(S(4, B(1, 1, 2, 2), B(2, 2, 1, 1)), out _));
            Assert.IsTrue(PuzzleRules.HasValidColorDistribution(S(4, B(1, 1, 2), B(2, 2, 1, 1)), out _), "3 units of a color fit in one bottle");
            Assert.IsFalse(PuzzleRules.HasValidColorDistribution(S(2, B(1, 1), B(1, 2), B(2)), out _), "3 units can never fit a 2-unit bottle");
        }
    }
}
