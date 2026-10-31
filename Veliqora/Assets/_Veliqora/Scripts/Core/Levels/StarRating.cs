using System;

namespace Veliqora.Core.Levels
{
    /// <summary>
    /// Transparent, forgiving star rules based on the solver benchmark ("par"):
    ///   3 stars: moves &lt;= par + max(2, ceil(25% of par))
    ///   2 stars: moves &lt;= par + max(5, ceil(60% of par))
    ///   1 star : any completion.
    /// Undone moves are not counted (the counter tracks the moves currently on the board's history).
    /// </summary>
    public static class StarRating
    {
        public static int ThreeStarLimit(int par) => par + Math.Max(2, (int)Math.Ceiling(par * 0.25));
        public static int TwoStarLimit(int par) => par + Math.Max(5, (int)Math.Ceiling(par * 0.60));

        public static int Evaluate(int moves, int par)
        {
            if (par <= 0) return 3;
            if (moves <= ThreeStarLimit(par)) return 3;
            if (moves <= TwoStarLimit(par)) return 2;
            return 1;
        }
    }
}
