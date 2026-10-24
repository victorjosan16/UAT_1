using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SugarDrop.Tests
{
    /// <summary>Regulile alinierilor: ce dispare și ce capac special iese.</summary>
    public class BoardRulesTests
    {
        /// <summary>Tablă din text: rândul de sus e primul; cifrele sunt culori, „.” = gol/nealiniabil.</summary>
        private static System.Func<Vector2Int, int> Grid(out int columns, out int rows, params string[] lines)
        {
            int r = lines.Length, c = lines[0].Length;
            columns = c;
            rows = r;
            return cell =>
            {
                if (cell.x < 0 || cell.y < 0 || cell.x >= c || cell.y >= r) return -1;
                char ch = lines[r - 1 - cell.y][cell.x];
                return ch == '.' ? -1 : ch - '0';
            };
        }

        [Test]
        public void NoMatch_ReturnsNoGroups()
        {
            var colorAt = Grid(out int c, out int r, "0101", "1010", "0101");
            Assert.IsEmpty(BoardRules.FindGroups(c, r, colorAt));
        }

        [Test]
        public void ThreeInRow_IsOneNormalGroup()
        {
            var colorAt = Grid(out int c, out int r, "1201", "0002", "2121");
            List<MatchGroup> groups = BoardRules.FindGroups(c, r, colorAt);
            Assert.AreEqual(1, groups.Count);
            Assert.AreEqual(0, groups[0].Color);
            Assert.AreEqual(3, groups[0].Cells.Count);
            Assert.AreEqual(TileKind.Normal, BoardRules.SpecialFor(groups[0]));
        }

        [Test]
        public void FourInRow_MakesHorizontalStriped()
        {
            var colorAt = Grid(out int c, out int r, "33331");
            Assert.AreEqual(TileKind.StripedRow, BoardRules.SpecialFor(BoardRules.FindGroups(c, r, colorAt)[0]));
        }

        [Test]
        public void FourInColumn_MakesVerticalStriped()
        {
            var colorAt = Grid(out int c, out int r, "2", "2", "2", "2", "0");
            Assert.AreEqual(TileKind.StripedColumn, BoardRules.SpecialFor(BoardRules.FindGroups(c, r, colorAt)[0]));
        }

        [Test]
        public void FiveInRow_MakesRainbow()
        {
            var colorAt = Grid(out int c, out int r, "444440");
            Assert.AreEqual(TileKind.Rainbow, BoardRules.SpecialFor(BoardRules.FindGroups(c, r, colorAt)[0]));
        }

        [Test]
        public void LShape_IsOneWrappedGroup()
        {
            var colorAt = Grid(out int c, out int r, "1..", "1..", "111");
            List<MatchGroup> groups = BoardRules.FindGroups(c, r, colorAt);
            Assert.AreEqual(1, groups.Count);
            Assert.AreEqual(5, groups[0].Cells.Count);
            Assert.AreEqual(TileKind.Wrapped, BoardRules.SpecialFor(groups[0]));
        }

        [Test]
        public void HoleOrBiscuit_BreaksTheRun()
        {
            var colorAt = Grid(out int c, out int r, "00.00");
            Assert.IsEmpty(BoardRules.FindGroups(c, r, colorAt));
        }

        [Test]
        public void IsMatchAt_DetectsBothDirections()
        {
            var colorAt = Grid(out _, out _, "1.2", "1.2", "1.2");
            Assert.IsTrue(BoardRules.IsMatchAt(new Vector2Int(0, 1), colorAt));
            Assert.IsTrue(BoardRules.IsMatchAt(new Vector2Int(2, 0), colorAt));
            Assert.IsFalse(BoardRules.IsMatchAt(new Vector2Int(1, 1), colorAt));
        }
    }
}
