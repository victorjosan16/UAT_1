using NUnit.Framework;

namespace SugarDrop.Tests
{
    /// <summary>Pe unde pot curge bomboanele prin găurile tablei.</summary>
    public class ChannelRulesTests
    {
        /// <summary>Tablă din text, rândul de sus primul: „.” = gol, „#” = capac.</summary>
        private static bool[,] Map(params string[] lines)
        {
            int rows = lines.Length, columns = lines[0].Length;
            var empty = new bool[columns, rows];
            for (int y = 0; y < rows; y++)
                for (int x = 0; x < columns; x++)
                    empty[x, y] = lines[rows - 1 - y][x] == '.';
            return empty;
        }

        [Test]
        public void FullBoard_HasNoChannel()
        {
            bool[,] empty = Map("###", "###", "###");
            int count = ChannelRules.Reach(empty, new bool[3, 3], out bool bottom);
            Assert.AreEqual(0, count);
            Assert.IsFalse(bottom);
        }

        [Test]
        public void HoleInTheMiddle_IsNotReachable()
        {
            // Gaura din mijloc nu e legată de grămada de sus: bomboanele nu ajung acolo.
            bool[,] empty = Map("###", "#.#", "###");
            Assert.AreEqual(0, ChannelRules.Reach(empty, new bool[3, 3], out _));
        }

        [Test]
        public void WindingPath_ReachesBottom()
        {
            bool[,] empty = Map(".##", "..#", "#..", "##.");
            var reached = new bool[3, 4];
            ChannelRules.Reach(empty, reached, out bool bottom);
            Assert.IsTrue(bottom);
            Assert.IsTrue(reached[2, 0]);
        }

        [Test]
        public void DiagonalGap_DoesNotLetCandiesThrough()
        {
            bool[,] empty = Map(".#", "#.");
            ChannelRules.Reach(empty, new bool[2, 2], out bool bottom);
            Assert.IsFalse(bottom);
        }

        [Test]
        public void OpenChannel_ScoresHigherThanDeepPocket()
        {
            int channel = ChannelRules.Score(Map(".###", ".###", ".###"));
            int pocket = ChannelRules.Score(Map("....", "....", "####"));
            int deeper = ChannelRules.Score(Map(".###", ".###", "####"));
            int shallow = ChannelRules.Score(Map(".###", "####", "####"));
            Assert.Greater(channel, pocket);
            Assert.Greater(deeper, shallow);
        }

        [Test]
        public void CandyAboveBoard_SlidesToNearestTopHole()
        {
            // Rândul de sus are o gaură în coloana 3; bomboana de deasupra coloanei 0 alunecă spre ea.
            bool[,] empty = Map("###.###", "#######");
            Assert.AreEqual(3, ChannelRules.FindDropColumn(empty, 0, 2));
            Assert.AreEqual(3, ChannelRules.FindDropColumn(empty, 6, 5)); // mai sus în grămadă: tot rândul de sus
        }

        [Test]
        public void CandyAlreadyOverHole_IsLeftToFall()
        {
            bool[,] empty = Map("###.###", "#######");
            Assert.AreEqual(-1, ChannelRules.FindDropColumn(empty, 3, 2));
        }

        [Test]
        public void InsideTheBoard_TilesInTheWayBlockTheSlide()
        {
            // Bomboana stă în gaura (1, 1) pe capacul (1, 0); spre dreapta, capacul (2, 1) blochează drumul.
            bool[,] empty = Map(".#..", "#.#.", "##.#");
            Assert.AreEqual(-1, ChannelRules.FindDropColumn(empty, 1, 1));
            // Rândul liber: alunecă spre gaura cea mai apropiată.
            bool[,] open = Map("....", "##.#");
            Assert.AreEqual(2, ChannelRules.FindDropColumn(open, 0, 1));
        }

        [Test]
        public void NoHoleOnTheFloor_NoTarget()
        {
            Assert.AreEqual(-1, ChannelRules.FindDropColumn(Map("####"), 1, 1));
        }
    }
}
