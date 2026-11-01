using System.Diagnostics;
using NUnit.Framework;
using Veliqora.Core.Layout;
using Veliqora.Core.Puzzle;
using Veliqora.Core.Session;

namespace Veliqora.Tests.EditMode
{
    /// <summary>Adaptive layout for 5–20 bottles: fits the board, no overlaps (bottles + obstacle tags), balanced rows.</summary>
    public class BoardLayoutTests
    {
        // Board area of the gameplay screen at 1080×1920 reference (between HUD/objectives and the booster bar).
        private const float AreaW = 1020f, AreaH = 1240f;

        [Test]
        public void Layout_FitsAndNeverOverlaps_From5To20Bottles([Values(3.0f, 3.6f)] float aspect)
        {
            for (int n = 5; n <= 20; n++)
            {
                var l = BoardLayout.Choose(n, AreaW, AreaH, aspect);
                Assert.That(l.Rows, Is.InRange(1, BoardLayout.MaxRows), "n=" + n);
                Assert.GreaterOrEqual(l.Rows * l.PerRow, n);
                Assert.GreaterOrEqual(l.BottleWidth, 56f, "bottles stay tappable (n=" + n + ")");
                var x = new float[n]; var y = new float[n];
                BoardLayout.Positions(n, l, x, y);
                for (int i = 0; i < n; i++)
                {
                    // inside the board
                    Assert.LessOrEqual(System.Math.Abs(x[i]) + l.BottleWidth * 0.5f, AreaW * 0.5f + 0.5f, "n=" + n + " i=" + i);
                    Assert.GreaterOrEqual(y[i] - BoardLayout.TagSpace, -AreaH * 0.5f - 0.5f, "tag below the board n=" + n);
                    Assert.LessOrEqual(y[i] + l.BottleHeight, AreaH * 0.5f + 0.5f, "top above the board n=" + n);
                    for (int j = i + 1; j < n; j++)
                    {
                        bool sameRow = System.Math.Abs(y[i] - y[j]) < 0.01f;
                        if (sameRow) Assert.GreaterOrEqual(System.Math.Abs(x[i] - x[j]), l.BottleWidth * 1.2f, "horizontal overlap n=" + n);
                        else Assert.GreaterOrEqual(System.Math.Abs(y[i] - y[j]), l.BottleHeight + BoardLayout.TagSpace, "vertical overlap (incl. tag) n=" + n);
                    }
                }
                // balanced rows: no row differs from another by more than one bottle
                var counts = new System.Collections.Generic.Dictionary<float, int>();
                foreach (var yy in y) counts[yy] = counts.TryGetValue(yy, out int c) ? c + 1 : 1;
                int min = int.MaxValue, max = 0;
                foreach (var c in counts.Values) { min = System.Math.Min(min, c); max = System.Math.Max(max, c); }
                Assert.LessOrEqual(max - min, 1, "rows balanced n=" + n);
            }
        }

        [Test]
        public void Layout_UsesMoreRows_AsTheBoardGrows()
        {
            Assert.AreEqual(1, BoardLayout.Choose(5, AreaW, AreaH, 3.2f).Rows);
            Assert.GreaterOrEqual(BoardLayout.Choose(20, AreaW, AreaH, 3.2f).Rows, 3);
        }

        [Test]
        public void PerformanceProxy_HintOnTheBiggestBoard_IsFast()
        {
            // Not an Android measurement: a desktop .NET timing of the heaviest gameplay computation (a hint from a
            // deviated position on a 20-bottle level), used to catch algorithmic regressions.
            var level = TestUtil.Catalog.Levels.Find(l => l.Bottles.Length == 20);
            Assert.IsNotNull(level);
            var s = new GameSession(level, "perf");
            var first = PuzzleRules.LegalMoves(s.Current, true);
            Assert.IsNotEmpty(first);
            // deviate from the stored solution
            var m = first[first.Count - 1];
            s.TryMove(m.From, m.To);
            var sw = Stopwatch.StartNew();
            var h = s.GetHint();
            sw.Stop();
            TestContext.WriteLine("hint on " + level.Id + " (20 bottles): " + sw.ElapsedMilliseconds + " ms, has move: " + h.HasMove);
            // Either a legal next move, or (from a dead end) the exact number of undos back to the verified path.
            Assert.IsTrue(h.HasMove || h.Source == HintSource.UndoSuggested, "hint is useful on the biggest board: " + h.Message);
            if (h.HasMove) Assert.IsTrue(PuzzleRules.IsLegal(s.Current, h.Move.From, h.Move.To));
            else Assert.AreEqual(1, h.UndoSteps);
            Assert.Less(sw.ElapsedMilliseconds, 1500, "hint stays within its time budget");
        }
    }
}
