using System;
using System.Text;

namespace Veliqora.Core.Puzzle
{
    /// <summary>How a corked bottle gets unlocked.</summary>
    public enum LockKind
    {
        /// <summary>Unlocks when a bottle of color <c>Param</c> is completed (or that color no longer exists).</summary>
        CompleteColor = 0,
        /// <summary>Unlocks after <c>Param</c> valid moves.</summary>
        Moves = 1,
        /// <summary>Unlocks when bottle index <c>Param</c> is unlocked.</summary>
        AfterBottle = 2,
        /// <summary>Unlocks when at least <c>Param</c> bottles are completed (a puzzle-state condition).</summary>
        CompletedBottles = 3,
    }

    /// <summary>Immutable cork on a bottle: no pouring into or out of it until the condition holds.</summary>
    public sealed class BottleLock : IEquatable<BottleLock>
    {
        public readonly LockKind Kind;
        public readonly int Param;

        public BottleLock(LockKind kind, int param)
        {
            if (param < 0) throw new ArgumentOutOfRangeException(nameof(param));
            Kind = kind;
            Param = param;
        }

        public bool Equals(BottleLock other) => other != null && other.Kind == Kind && other.Param == Param;
        public override bool Equals(object obj) => Equals(obj as BottleLock);
        public override int GetHashCode() => (int)Kind * 1009 + Param;
        public override string ToString() => Kind + ":" + Param;
    }

    /// <summary>
    /// Immutable bottle: ordered units from bottom (index 0) to top, each a ColorId (&gt;= 1), plus special state:
    /// - <see cref="Capacity"/> per bottle (2–6 supported);
    /// - <see cref="Hidden"/>: number of MYSTERY units at the bottom (always a bottom prefix; the top unit is always
    ///   visible). Hidden units keep their real colors; a unit is revealed when it becomes the top unit;
    /// - <see cref="Lock"/>: cork with an unlock condition (null = not corked);
    /// - <see cref="RewardLocked"/>: bottle that becomes usable only after a completed rewarded ad.
    /// </summary>
    public sealed class BottleState : IEquatable<BottleState>
    {
        public const int DefaultCapacity = 4;
        public const int MinCapacity = 2;
        public const int MaxCapacity = 6;

        private readonly int[] _units;

        public int Capacity { get; }
        public int Hidden { get; }
        public BottleLock Lock { get; }
        public bool RewardLocked { get; }

        public int Count => _units.Length;
        public bool IsEmpty => _units.Length == 0;
        public bool IsFull => _units.Length >= Capacity;
        public int FreeSpace => Capacity - _units.Length;
        public int TopColor => _units.Length == 0 ? 0 : _units[_units.Length - 1];
        public bool IsCorked => Lock != null;
        /// <summary>Corked or ad-locked: cannot be poured from or into.</summary>
        public bool IsLocked => Lock != null || RewardLocked;

        public BottleState(int capacity, params int[] units) : this(capacity, units, 0, null, false) { }

        public BottleState(int capacity, int[] units, int hidden, BottleLock bottleLock, bool rewardLocked)
        {
            if (capacity < MinCapacity - 1 || capacity > 12) throw new ArgumentOutOfRangeException(nameof(capacity));
            units = units ?? Array.Empty<int>();
            if (units.Length > capacity) throw new ArgumentException("More units than capacity");
            for (int i = 0; i < units.Length; i++)
                if (units[i] <= 0) throw new ArgumentException("ColorId must be >= 1");
            Capacity = capacity;
            _units = (int[])units.Clone();
            Hidden = NormalizeHidden(hidden, _units.Length);
            Lock = bottleLock;
            RewardLocked = rewardLocked;
        }

        private BottleState(int capacity, int[] units, int hidden, BottleLock bottleLock, bool rewardLocked, bool noCopy)
        {
            Capacity = capacity;
            _units = units;
            Hidden = NormalizeHidden(hidden, units.Length);
            Lock = bottleLock;
            RewardLocked = rewardLocked;
        }

        /// <summary>The top unit is always visible, so at most Count-1 units can be hidden.</summary>
        private static int NormalizeHidden(int hidden, int count) => Math.Max(0, Math.Min(hidden, count - 1));

        public static BottleState Empty(int capacity) => new BottleState(capacity, Array.Empty<int>(), 0, null, false, true);

        public int this[int index] => _units[index];

        public int[] ToArray() => (int[])_units.Clone();

        public bool IsHiddenAt(int index) => index < Hidden;

        /// <summary>Length of the contiguous run of TopColor at the top, counting only REVEALED units.</summary>
        public int TopRunLength
        {
            get
            {
                if (_units.Length == 0) return 0;
                int top = _units[_units.Length - 1];
                int n = 0;
                for (int i = _units.Length - 1; i >= Hidden && _units[i] == top; i--) n++;
                return n;
            }
        }

        public bool IsMonochrome
        {
            get
            {
                for (int i = 1; i < _units.Length; i++) if (_units[i] != _units[0]) return false;
                return true;
            }
        }

        /// <summary>Legacy per-bottle check: empty, or full and single-colored (see GameState.IsBottleComplete for the general rule).</summary>
        public bool IsSolved => IsEmpty || (IsFull && IsMonochrome);

        /// <summary>Full and single-coloured. GameState.IsBottleComplete also accepts "holds every unit of its color".</summary>
        public bool IsCompleted => IsFull && IsMonochrome && !IsEmpty;

        public int CountOf(int color)
        {
            int n = 0;
            for (int i = 0; i < _units.Length; i++) if (_units[i] == color) n++;
            return n;
        }

        public BottleState RemoveTop(int amount)
        {
            if (amount < 0 || amount > _units.Length) throw new ArgumentOutOfRangeException(nameof(amount));
            var next = new int[_units.Length - amount];
            Array.Copy(_units, next, next.Length);
            return new BottleState(Capacity, next, Hidden, Lock, RewardLocked, true); // exposing a hidden unit reveals it
        }

        public BottleState Add(int color, int amount)
        {
            if (color <= 0) throw new ArgumentException("ColorId must be >= 1");
            if (amount < 0 || amount > FreeSpace) throw new ArgumentOutOfRangeException(nameof(amount));
            var next = new int[_units.Length + amount];
            Array.Copy(_units, next, _units.Length);
            for (int i = _units.Length; i < next.Length; i++) next[i] = color;
            return new BottleState(Capacity, next, Hidden, Lock, RewardLocked, true);
        }

        /// <summary>Removes every unit of <paramref name="color"/>; hidden units that survive stay hidden.</summary>
        public BottleState RemoveColor(int color)
        {
            int keep = 0, hiddenKept = 0;
            for (int i = 0; i < _units.Length; i++)
                if (_units[i] != color) { keep++; if (i < Hidden) hiddenKept++; }
            var next = new int[keep];
            int k = 0;
            for (int i = 0; i < _units.Length; i++) if (_units[i] != color) next[k++] = _units[i];
            return new BottleState(Capacity, next, hiddenKept, Lock, RewardLocked, true);
        }

        public BottleState WithUnits(int[] units, int hidden) => new BottleState(Capacity, units, hidden, Lock, RewardLocked);
        public BottleState WithLock(BottleLock bottleLock) => new BottleState(Capacity, _units, Hidden, bottleLock, RewardLocked, true);
        public BottleState WithRewardLock(bool rewardLocked) => new BottleState(Capacity, _units, Hidden, Lock, rewardLocked, true);
        public BottleState WithHidden(int hidden) => new BottleState(Capacity, _units, hidden, Lock, RewardLocked, true);

        public bool Equals(BottleState other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (other is null || other.Capacity != Capacity || other._units.Length != _units.Length || other.Hidden != Hidden
                || other.RewardLocked != RewardLocked || !Equals(other.Lock, Lock)) return false;
            for (int i = 0; i < _units.Length; i++) if (_units[i] != other._units[i]) return false;
            return true;
        }

        public override bool Equals(object obj) => Equals(obj as BottleState);

        public override int GetHashCode()
        {
            unchecked
            {
                int h = Capacity * 397 + Hidden * 31 + (RewardLocked ? 7 : 0) + (Lock?.GetHashCode() ?? 0);
                for (int i = 0; i < _units.Length; i++) h = h * 31 + _units[i];
                return h;
            }
        }

        public void AppendKey(StringBuilder sb)
        {
            for (int i = 0; i < _units.Length; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(_units[i]);
            }
            if (Hidden > 0) sb.Append('h').Append(Hidden);
            if (Lock != null) sb.Append('L').Append((int)Lock.Kind).Append('.').Append(Lock.Param);
            if (RewardLocked) sb.Append('R');
        }

        public override string ToString()
        {
            var sb = new StringBuilder("[");
            AppendKey(sb);
            return sb.Append(']').ToString();
        }
    }
}
