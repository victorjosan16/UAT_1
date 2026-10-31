using System.Collections.Generic;

namespace Veliqora.Core.Puzzle
{
    /// <summary>Pure rules of the water-sort puzzle. Stateless and allocation-light.</summary>
    public static class PuzzleRules
    {
        /// <summary>Validates a pour and, if legal, computes the resulting state (contiguous top run, limited by free space).</summary>
        public static MoveResult Evaluate(GameState state, int from, int to)
        {
            if (from < 0 || to < 0 || from >= state.BottleCount || to >= state.BottleCount)
                return MoveResult.Illegal(MoveRejection.OutOfRange, from, to, state);
            if (from == to) return MoveResult.Illegal(MoveRejection.SameBottle, from, to, state);

            var src = state[from];
            var dst = state[to];
            if (src.IsEmpty) return MoveResult.Illegal(MoveRejection.SourceEmpty, from, to, state);
            if (dst.IsFull) return MoveResult.Illegal(MoveRejection.DestinationFull, from, to, state);
            int color = src.TopColor;
            if (!dst.IsEmpty && dst.TopColor != color) return MoveResult.Illegal(MoveRejection.ColorMismatch, from, to, state);

            int amount = src.TopRunLength;
            if (amount > dst.FreeSpace) amount = dst.FreeSpace;

            var after = state.WithBottles(from, src.RemoveTop(amount), to, dst.Add(color, amount));
            return MoveResult.Legal(new Move(from, to, amount, color), state, after);
        }

        public static bool IsLegal(GameState state, int from, int to)
        {
            if (from < 0 || to < 0 || from >= state.BottleCount || to >= state.BottleCount || from == to) return false;
            var src = state[from];
            var dst = state[to];
            return !src.IsEmpty && !dst.IsFull && (dst.IsEmpty || dst.TopColor == src.TopColor);
        }

        /// <summary>Every bottle is empty, or full and single-colored.</summary>
        public static bool IsWon(GameState state)
        {
            for (int i = 0; i < state.BottleCount; i++) if (!state[i].IsSolved) return false;
            return true;
        }

        /// <summary>
        /// A move is "useful" if it can change the arrangement meaningfully. Excludes pouring a whole
        /// single-color bottle into an empty bottle (pure relabel) and pouring out of a completed bottle.
        /// </summary>
        public static bool IsUseful(GameState state, int from, int to)
        {
            if (!IsLegal(state, from, to)) return false;
            var src = state[from];
            var dst = state[to];
            if (src.IsCompleted) return false;
            if (dst.IsEmpty && src.IsMonochrome) return false;
            return true;
        }

        public static List<Move> LegalMoves(GameState state, bool usefulOnly)
        {
            var moves = new List<Move>();
            for (int f = 0; f < state.BottleCount; f++)
            {
                if (state[f].IsEmpty) continue;
                for (int t = 0; t < state.BottleCount; t++)
                {
                    if (usefulOnly ? !IsUseful(state, f, t) : !IsLegal(state, f, t)) continue;
                    var src = state[f];
                    int amount = src.TopRunLength;
                    if (amount > state[t].FreeSpace) amount = state[t].FreeSpace;
                    moves.Add(new Move(f, t, amount, src.TopColor));
                }
            }
            return moves;
        }

        /// <summary>True when the board is unsolved and no useful move exists (player is stuck).</summary>
        public static bool IsStuck(GameState state)
        {
            if (IsWon(state)) return false;
            for (int f = 0; f < state.BottleCount; f++)
                for (int t = 0; t < state.BottleCount; t++)
                    if (IsUseful(state, f, t)) return false;
            return true;
        }

        /// <summary>
        /// Structural sanity: bottle capacities are uniform and every color totals a multiple of capacity
        /// (otherwise the puzzle can never be won).
        /// </summary>
        public static bool HasValidColorDistribution(GameState state, out string error)
        {
            error = null;
            if (state.BottleCount == 0) { error = "No bottles"; return false; }
            int cap = state[0].Capacity;
            for (int i = 1; i < state.BottleCount; i++)
                if (state[i].Capacity != cap) { error = "Mixed capacities"; return false; }
            foreach (var kv in state.ColorCounts())
                if (kv.Value % cap != 0) { error = "Color " + kv.Key + " has " + kv.Value + " units (not a multiple of " + cap + ")"; return false; }
            return true;
        }
    }
}
