using System.Collections.Generic;

namespace Veliqora.Core.Puzzle
{
    /// <summary>
    /// Pure rules of the water-sort puzzle, including special bottles:
    /// mixed capacities, mystery layers (only revealed units move), corked bottles with unlock conditions and
    /// ad-locked bottles. Stateless; every committed move returns a brand-new immutable state.
    /// </summary>
    public static class PuzzleRules
    {
        /// <summary>Validates a pour and, if legal, computes the resulting state: the contiguous revealed top run moves
        /// (limited by free space), the move counter advances, newly exposed mystery units are revealed and corks
        /// whose conditions now hold are removed.</summary>
        public static MoveResult Evaluate(GameState state, int from, int to)
        {
            if (from < 0 || to < 0 || from >= state.BottleCount || to >= state.BottleCount)
                return MoveResult.Illegal(MoveRejection.OutOfRange, from, to, state);
            if (from == to) return MoveResult.Illegal(MoveRejection.SameBottle, from, to, state);

            var src = state[from];
            var dst = state[to];
            if (src.IsLocked || dst.IsLocked) return MoveResult.Illegal(MoveRejection.Locked, from, to, state);
            if (src.IsEmpty) return MoveResult.Illegal(MoveRejection.SourceEmpty, from, to, state);
            if (dst.IsFull) return MoveResult.Illegal(MoveRejection.DestinationFull, from, to, state);
            int color = src.TopColor;
            if (!dst.IsEmpty && dst.TopColor != color) return MoveResult.Illegal(MoveRejection.ColorMismatch, from, to, state);

            int amount = src.TopRunLength;
            if (amount > dst.FreeSpace) amount = dst.FreeSpace;

            var moved = RevealCompleted(state.WithBottles(from, src.RemoveTop(amount), to, dst.Add(color, amount)).WithMoves(state.Moves + 1));
            List<int> unlocked;
            var after = ResolveLocks(moved, out unlocked);
            return MoveResult.Legal(new Move(from, to, amount, color), state, after, unlocked);
        }

        public static bool IsLegal(GameState state, int from, int to)
        {
            if (from < 0 || to < 0 || from >= state.BottleCount || to >= state.BottleCount || from == to) return false;
            var src = state[from];
            var dst = state[to];
            if (src.IsLocked || dst.IsLocked) return false;
            return !src.IsEmpty && !dst.IsFull && (dst.IsEmpty || dst.TopColor == src.TopColor);
        }

        /// <summary>A completed bottle is single-colored, so its remaining mystery units carry no secret: reveal them.</summary>
        public static GameState RevealCompleted(GameState state)
        {
            for (int i = 0; i < state.BottleCount; i++)
                if (state[i].Hidden > 0 && state.IsBottleComplete(i)) state = state.WithBottle(i, state[i].WithHidden(0));
            return state;
        }

        /// <summary>True when a cork's condition holds in <paramref name="state"/>.</summary>
        public static bool IsUnlockConditionMet(GameState state, BottleLock bottleLock)
        {
            switch (bottleLock.Kind)
            {
                case LockKind.Moves: return state.Moves >= bottleLock.Param;
                case LockKind.CompletedBottles: return state.CompletedBottleCount() >= bottleLock.Param;
                case LockKind.AfterBottle:
                    return bottleLock.Param >= 0 && bottleLock.Param < state.BottleCount && !state[bottleLock.Param].IsCorked;
                case LockKind.CompleteColor:
                    if (state.ColorTotal(bottleLock.Param) == 0) return true; // color removed from the board
                    for (int i = 0; i < state.BottleCount; i++)
                        if (state[i].TopColor == bottleLock.Param && state.IsBottleComplete(i)) return true;
                    return false;
                default: return false;
            }
        }

        /// <summary>Removes every cork whose condition holds, repeating until stable (handles unlock chains).</summary>
        public static GameState ResolveLocks(GameState state, out List<int> unlocked)
        {
            unlocked = null;
            if (!state.HasLocks) return state;
            bool changed = true;
            while (changed)
            {
                changed = false;
                for (int i = 0; i < state.BottleCount; i++)
                {
                    var b = state[i];
                    if (b.Lock == null || !IsUnlockConditionMet(state, b.Lock)) continue;
                    state = state.WithBottle(i, b.WithLock(null));
                    (unlocked ?? (unlocked = new List<int>())).Add(i);
                    changed = true;
                }
            }
            return state;
        }

        public static GameState ResolveLocks(GameState state) => ResolveLocks(state, out _);

        /// <summary>
        /// Victory: every bottle is empty or complete (single color and full, or holding all units of its color).
        /// Ad-locked bottles are empty by construction and never required.
        /// </summary>
        public static bool IsWon(GameState state)
        {
            for (int i = 0; i < state.BottleCount; i++)
                if (!state[i].IsEmpty && !state.IsBottleComplete(i)) return false;
            return true;
        }

        /// <summary>
        /// A move is "useful" if it can change the arrangement meaningfully. Excludes moving out of a completed
        /// bottle and pure relabels (a whole single-color bottle into an empty bottle of the same capacity).
        /// </summary>
        public static bool IsUseful(GameState state, int from, int to)
        {
            if (!IsLegal(state, from, to)) return false;
            var src = state[from];
            var dst = state[to];
            if (state.IsBottleComplete(from)) return false;
            if (dst.IsEmpty && src.IsMonochrome && src.Hidden == 0 && src.Capacity == dst.Capacity) return false;
            return true;
        }

        public static List<Move> LegalMoves(GameState state, bool usefulOnly)
        {
            var moves = new List<Move>();
            for (int f = 0; f < state.BottleCount; f++)
            {
                if (state[f].IsEmpty || state[f].IsLocked) continue;
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

        /// <summary>True when the board is unsolved and no useful move exists (player is stuck / colors are blocked).</summary>
        public static bool IsStuck(GameState state)
        {
            if (IsWon(state)) return false;
            for (int f = 0; f < state.BottleCount; f++)
                for (int t = 0; t < state.BottleCount; t++)
                    if (IsUseful(state, f, t)) return false;
            return true;
        }

        /// <summary>
        /// Structural sanity: capacities in the supported range, and every color fits in at least one bottle
        /// (or totals a multiple of a bottle capacity, the classic layout). Otherwise the level can't be won.
        /// </summary>
        public static bool HasValidColorDistribution(GameState state, out string error)
        {
            error = null;
            if (state.BottleCount == 0) { error = "No bottles"; return false; }
            int maxCap = 0;
            for (int i = 0; i < state.BottleCount; i++)
            {
                int c = state[i].Capacity;
                if (c < BottleState.MinCapacity || c > BottleState.MaxCapacity) { error = "Capacity " + c + " out of range"; return false; }
                if (c > maxCap) maxCap = c;
            }
            foreach (var kv in state.ColorCounts())
            {
                if (kv.Value <= maxCap) continue;
                bool multiple = false;
                for (int i = 0; i < state.BottleCount; i++) if (kv.Value % state[i].Capacity == 0) multiple = true;
                if (!multiple) { error = "Color " + kv.Key + " has " + kv.Value + " units and fits no bottle"; return false; }
            }
            return true;
        }
    }
}
