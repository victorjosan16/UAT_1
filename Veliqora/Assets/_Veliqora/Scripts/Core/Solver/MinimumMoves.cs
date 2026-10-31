using System.Collections.Generic;
using Veliqora.Core.Puzzle;

namespace Veliqora.Core.Solver
{
    /// <summary>
    /// Exact minimum number of pours needed to win, by breadth-first search over canonical board states.
    /// Used to certify that early levels really need thinking (e.g. level 1 needs at least 6 pours) and to report
    /// the true optimum in the level verification report. Returns -1 when the state space exceeds the node budget
    /// (the A* benchmark solution then remains an upper bound).
    /// </summary>
    public static class MinimumMoves
    {
        public static int Compute(GameState start, int maxNodes = 250_000)
        {
            if (PuzzleRules.IsWon(start)) return 0;
            var seen = new HashSet<string> { start.CanonicalKey() };
            var frontier = new List<GameState> { start };
            int depth = 0;
            while (frontier.Count > 0)
            {
                depth++;
                var next = new List<GameState>();
                foreach (var s in frontier)
                {
                    foreach (var m in PuzzleRules.LegalMoves(s, false))
                    {
                        var after = PuzzleRules.Evaluate(s, m.From, m.To).After;
                        if (PuzzleRules.IsWon(after)) return depth;
                        if (!seen.Add(after.CanonicalKey())) continue;
                        if (seen.Count > maxNodes) return -1;
                        next.Add(after);
                    }
                }
                frontier = next;
            }
            return -1; // no solution reachable
        }
    }
}
