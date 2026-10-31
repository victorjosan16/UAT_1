using System;
using System.Collections.Generic;
using System.Text;

namespace Veliqora.Core.Puzzle
{
    /// <summary>Immutable snapshot of all bottles on the board.</summary>
    public sealed class GameState : IEquatable<GameState>
    {
        private readonly BottleState[] _bottles;

        public int BottleCount => _bottles.Length;
        public BottleState this[int index] => _bottles[index];
        public IReadOnlyList<BottleState> Bottles => _bottles;

        public GameState(IEnumerable<BottleState> bottles)
        {
            if (bottles == null) throw new ArgumentNullException(nameof(bottles));
            _bottles = new List<BottleState>(bottles).ToArray();
            for (int i = 0; i < _bottles.Length; i++)
                if (_bottles[i] == null) throw new ArgumentException("Null bottle");
        }

        private GameState(BottleState[] bottles, bool noCopy) { _bottles = bottles; }

        public static GameState FromArrays(int capacity, int[][] bottles)
        {
            var list = new BottleState[bottles.Length];
            for (int i = 0; i < bottles.Length; i++) list[i] = new BottleState(capacity, bottles[i]);
            return new GameState(list, true);
        }

        public int[][] ToArrays()
        {
            var result = new int[_bottles.Length][];
            for (int i = 0; i < _bottles.Length; i++) result[i] = _bottles[i].ToArray();
            return result;
        }

        public GameState WithBottle(int index, BottleState bottle)
        {
            var next = (BottleState[])_bottles.Clone();
            next[index] = bottle;
            return new GameState(next, true);
        }

        public GameState WithBottles(int a, BottleState ba, int b, BottleState bb)
        {
            var next = (BottleState[])_bottles.Clone();
            next[a] = ba;
            next[b] = bb;
            return new GameState(next, true);
        }

        public GameState AddBottle(BottleState bottle)
        {
            var next = new BottleState[_bottles.Length + 1];
            Array.Copy(_bottles, next, _bottles.Length);
            next[_bottles.Length] = bottle;
            return new GameState(next, true);
        }

        /// <summary>Count of units per color across the board.</summary>
        public Dictionary<int, int> ColorCounts()
        {
            var counts = new Dictionary<int, int>();
            foreach (var b in _bottles)
                for (int i = 0; i < b.Count; i++)
                {
                    counts.TryGetValue(b[i], out int c);
                    counts[b[i]] = c + 1;
                }
            return counts;
        }

        /// <summary>Exact key (bottle order matters).</summary>
        public string ExactKey()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < _bottles.Length; i++)
            {
                if (i > 0) sb.Append('|');
                sb.Append(_bottles[i].Capacity).Append(':');
                _bottles[i].AppendKey(sb);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Canonical key: bottle order does not matter (bottles are interchangeable), so permutations of the
        /// same multiset of bottles collapse into one search node.
        /// </summary>
        public string CanonicalKey()
        {
            var parts = new string[_bottles.Length];
            var sb = new StringBuilder();
            for (int i = 0; i < _bottles.Length; i++)
            {
                sb.Clear();
                sb.Append(_bottles[i].Capacity).Append(':');
                _bottles[i].AppendKey(sb);
                parts[i] = sb.ToString();
            }
            Array.Sort(parts, StringComparer.Ordinal);
            return string.Join("|", parts);
        }

        public bool Equals(GameState other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (other is null || other._bottles.Length != _bottles.Length) return false;
            for (int i = 0; i < _bottles.Length; i++) if (!_bottles[i].Equals(other._bottles[i])) return false;
            return true;
        }

        public override bool Equals(object obj) => Equals(obj as GameState);

        public override int GetHashCode()
        {
            unchecked
            {
                int h = 17;
                for (int i = 0; i < _bottles.Length; i++) h = h * 486187739 + _bottles[i].GetHashCode();
                return h;
            }
        }

        public override string ToString() => ExactKey();
    }
}
