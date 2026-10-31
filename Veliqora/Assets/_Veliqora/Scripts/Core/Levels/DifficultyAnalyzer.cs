using System.Collections.Generic;
using Veliqora.Core.Puzzle;
using Veliqora.Core.Random;

namespace Veliqora.Core.Levels
{
    /// <summary>
    /// Practical difficulty measurement. Besides benchmark solution length and branching, it simulates a
    /// naive player making random useful moves (no immediate back-and-forth) and records how often that player
    /// gets stuck or loops. A high stuck rate means the level has traps that require planning.
    /// </summary>
    public static class DifficultyAnalyzer
    {
        public static double StuckRate(GameState start, ulong seed, int playouts = 64, int maxDepth = 120)
        {
            var rng = new DeterministicRandom(seed);
            int stuck = 0;
            for (int p = 0; p < playouts; p++)
            {
                var s = start;
                var seen = new HashSet<string> { s.CanonicalKey() };
                bool won = false;
                for (int depth = 0; depth < maxDepth; depth++)
                {
                    var moves = PuzzleRules.LegalMoves(s, usefulOnly: true);
                    // Avoid revisiting states (a naive player still notices obvious loops).
                    var fresh = new List<GameState>(moves.Count);
                    foreach (var m in moves)
                    {
                        var next = PuzzleRules.Evaluate(s, m.From, m.To).After;
                        if (!seen.Contains(next.CanonicalKey())) fresh.Add(next);
                    }
                    if (fresh.Count == 0) break;
                    s = fresh[rng.Range(0, fresh.Count)];
                    seen.Add(s.CanonicalKey());
                    if (PuzzleRules.IsWon(s)) { won = true; break; }
                }
                if (!won) stuck++;
            }
            return (double)stuck / playouts;
        }
    }
}
