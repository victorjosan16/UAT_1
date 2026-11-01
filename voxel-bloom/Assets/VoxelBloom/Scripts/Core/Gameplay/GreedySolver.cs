using System;
using System.Collections.Generic;

namespace VoxelBloom.Core
{
    /// <summary>
    /// Heuristic witness solver mirroring tools/rules.mjs solveGreedy(). It is NOT exhaustive:
    /// a win proves the level solvable (the tap list is a witness); a loss only means the
    /// heuristic found no solution. Used for tests and the in-game hint.
    /// </summary>
    public static class GreedySolver
    {
        public sealed class Result
        {
            public bool Won;
            public int Moves, Score, MaxCombo;
            public FailReason Fail;
            public readonly List<int> Taps = new List<int>();
        }

        public static int BestSlot(GameSession s)
        {
            int best = -1;
            double bestScore = double.NegativeInfinity;
            for (int slot = 0; slot < GameSession.BoardSize; slot++)
            {
                var ob = s.BoardAt(slot);
                if (!ob.HasValue || s.Queue.Count >= s.QueueSize) continue;
                var b = ob.Value;
                int exp = s.ExposedCount(b.Color);
                int pending = 0;
                foreach (var q in s.Queue) if (q.Color == b.Color) pending += q.Power;
                int useful = Math.Min(b.Power, Math.Max(0, s.Remaining(b.Color) - pending));
                double sc = (exp > 0 ? 1000 : 0) + useful * 4 - Math.Max(0, b.Power - s.Remaining(b.Color)) * 2 + (exp > 0 ? Math.Min(exp, b.Power) : -50);
                if (sc > bestScore) { bestScore = sc; best = slot; }
            }
            return best;
        }

        public static Result Solve(LevelDefinition level, int maxMoves = 1000)
        {
            var s = new GameSession(level);
            var r = new Result();
            while (!s.IsWon && r.Taps.Count < maxMoves)
            {
                int best = BestSlot(s);
                if (best < 0) break;
                s.Tap(best);
                r.Taps.Add(best);
                s.ResolveAll();
                if (s.GetFailReason() != FailReason.None) break;
            }
            r.Won = s.IsWon; r.Moves = s.Moves; r.Score = s.Score; r.MaxCombo = s.MaxCombo; r.Fail = s.GetFailReason();
            return r;
        }
    }
}
