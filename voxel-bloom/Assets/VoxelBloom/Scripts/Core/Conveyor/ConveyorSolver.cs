using System.Collections.Generic;

namespace VoxelBloom.Core
{
    /// <summary>Players and witness replay for conveyor levels (mirror of tools/conveyor/rules.mjs).</summary>
    public static class ConveyorSolver
    {
        public struct Result { public bool Won; public int Ticks; }

        /// <summary>The visible tray whose first required colours are most available on the belt (ties: lowest index).</summary>
        public static int BestTray(ConveyorSession s)
        {
            var count = new Dictionary<int, int>();
            foreach (int c in s.Slot) if (c >= 0) count[c] = count.TryGetValue(c, out var n) ? n + 1 : 1;
            int best = -1; double bestScore = -1e9;
            for (int i = 0; i < s.VisibleCount; i++)
            {
                if (!s.CanPlace(i)) continue;
                var cols = s.Queue[i].Colors;
                var avail = new Dictionary<int, int>(count);
                double score = 0, w = 1;
                foreach (int c in cols)
                {
                    int a = avail.TryGetValue(c, out var n) ? n : 0;
                    if (a > 0) { score += w * 10; avail[c] = a - 1; } else break;
                    w *= 0.7;
                }
                score -= cols.Length * 0.5;
                if (score > bestScore) { bestScore = score; best = i; }
            }
            return best;
        }

        public static Result PlayGreedy(ConveyorSession s)
        {
            while (!s.IsWon && !s.IsStuck() && s.Tick < ConveyorSession.MaxTicks)
            {
                int qi;
                while ((qi = BestTray(s)) >= 0) s.Place(qi);
                s.Step();
            }
            return new Result { Won = s.IsWon, Ticks = s.Tick };
        }

        /// <summary>Replays a witness [[tick, queueIndex], ...]; returns the final session.</summary>
        public static ConveyorSession Replay(ConveyorLevel level, int[][] witness)
        {
            var s = new ConveyorSession(level);
            int w = 0;
            while (!s.IsWon && s.Tick < ConveyorSession.MaxTicks)
            {
                while (w < witness.Length && witness[w][0] == s.Tick) { if (s.Place(witness[w][1]) < 0) return s; w++; }
                if (s.IsStuck()) break;
                s.Step();
            }
            return s;
        }

        /// <summary>Hint: the tray the greedy player would place now (-1 if none).</summary>
        public static int Hint(ConveyorSession s) => BestTray(s);
    }
}
