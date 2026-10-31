using System;
using System.Collections.Generic;

namespace Veliqora.Core.Puzzle
{
    /// <summary>A pour from one bottle to another. Amount = units actually transferred.</summary>
    public readonly struct Move : IEquatable<Move>
    {
        public readonly int From;
        public readonly int To;
        public readonly int Amount;
        public readonly int Color;

        public Move(int from, int to, int amount, int color)
        {
            From = from;
            To = to;
            Amount = amount;
            Color = color;
        }

        public bool Equals(Move other) => From == other.From && To == other.To && Amount == other.Amount && Color == other.Color;
        public override bool Equals(object obj) => obj is Move m && Equals(m);
        public override int GetHashCode() => (From * 397 ^ To) * 31 + Amount * 7 + Color;
        public override string ToString() => From + "->" + To + " x" + Amount + " (c" + Color + ")";
    }

    public enum MoveRejection
    {
        None = 0,
        OutOfRange,
        SameBottle,
        SourceEmpty,
        DestinationFull,
        ColorMismatch,
        /// <summary>Source or destination is corked / ad-locked.</summary>
        Locked,
    }

    /// <summary>Result of validating a requested pour. When legal, NewState holds the committed board.</summary>
    public sealed class MoveResult
    {
        public bool IsLegal => Rejection == MoveRejection.None;
        public MoveRejection Rejection { get; }
        public Move Move { get; }
        public GameState Before { get; }
        public GameState After { get; }
        /// <summary>Bottles whose cork came off because of this move (empty when none).</summary>
        public IReadOnlyList<int> Unlocked { get; }

        private static readonly int[] None = new int[0];

        private MoveResult(MoveRejection rejection, Move move, GameState before, GameState after, IReadOnlyList<int> unlocked)
        {
            Rejection = rejection;
            Move = move;
            Before = before;
            After = after;
            Unlocked = unlocked ?? None;
        }

        public static MoveResult Legal(Move move, GameState before, GameState after, IReadOnlyList<int> unlocked = null) => new MoveResult(MoveRejection.None, move, before, after, unlocked);
        public static MoveResult Illegal(MoveRejection reason, int from, int to, GameState before) => new MoveResult(reason, new Move(from, to, 0, 0), before, before, null);

        public override string ToString() => IsLegal ? "Legal " + Move : "Illegal (" + Rejection + ")";
    }
}
