using System;
using System.Text;

namespace Veliqora.Core.Puzzle
{
    /// <summary>
    /// Immutable bottle: ordered units from bottom (index 0) to top. Each unit is a ColorId (&gt;= 1).
    /// </summary>
    public sealed class BottleState : IEquatable<BottleState>
    {
        public const int DefaultCapacity = 4;

        private readonly int[] _units;

        public int Capacity { get; }
        public int Count => _units.Length;
        public bool IsEmpty => _units.Length == 0;
        public bool IsFull => _units.Length >= Capacity;
        public int FreeSpace => Capacity - _units.Length;
        public int TopColor => _units.Length == 0 ? 0 : _units[_units.Length - 1];

        public BottleState(int capacity, params int[] units)
        {
            if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity));
            units = units ?? Array.Empty<int>();
            if (units.Length > capacity) throw new ArgumentException("More units than capacity");
            for (int i = 0; i < units.Length; i++)
                if (units[i] <= 0) throw new ArgumentException("ColorId must be >= 1");
            Capacity = capacity;
            _units = (int[])units.Clone();
        }

        private BottleState(int capacity, int[] units, bool noCopy)
        {
            Capacity = capacity;
            _units = units;
        }

        public static BottleState Empty(int capacity) => new BottleState(capacity, Array.Empty<int>(), true);

        public int this[int index] => _units[index];

        public int[] ToArray() => (int[])_units.Clone();

        /// <summary>Length of the contiguous run of TopColor at the top.</summary>
        public int TopRunLength
        {
            get
            {
                if (_units.Length == 0) return 0;
                int top = _units[_units.Length - 1];
                int n = 0;
                for (int i = _units.Length - 1; i >= 0 && _units[i] == top; i--) n++;
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

        /// <summary>Empty, or full and single-colored.</summary>
        public bool IsSolved => IsEmpty || (IsFull && IsMonochrome);

        /// <summary>Full and single-coloured (a "completed" bottle; empty doesn't count).</summary>
        public bool IsCompleted => IsFull && IsMonochrome && !IsEmpty;

        public BottleState RemoveTop(int amount)
        {
            if (amount < 0 || amount > _units.Length) throw new ArgumentOutOfRangeException(nameof(amount));
            var next = new int[_units.Length - amount];
            Array.Copy(_units, next, next.Length);
            return new BottleState(Capacity, next, true);
        }

        public BottleState Add(int color, int amount)
        {
            if (color <= 0) throw new ArgumentException("ColorId must be >= 1");
            if (amount < 0 || amount > FreeSpace) throw new ArgumentOutOfRangeException(nameof(amount));
            var next = new int[_units.Length + amount];
            Array.Copy(_units, next, _units.Length);
            for (int i = _units.Length; i < next.Length; i++) next[i] = color;
            return new BottleState(Capacity, next, true);
        }

        public bool Equals(BottleState other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (other is null || other.Capacity != Capacity || other._units.Length != _units.Length) return false;
            for (int i = 0; i < _units.Length; i++) if (_units[i] != other._units[i]) return false;
            return true;
        }

        public override bool Equals(object obj) => Equals(obj as BottleState);

        public override int GetHashCode()
        {
            unchecked
            {
                int h = Capacity * 397;
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
        }

        public override string ToString()
        {
            var sb = new StringBuilder("[");
            AppendKey(sb);
            return sb.Append(']').ToString();
        }
    }
}
