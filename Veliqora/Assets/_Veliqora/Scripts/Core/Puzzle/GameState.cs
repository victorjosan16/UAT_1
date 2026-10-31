using System;
using System.Collections.Generic;
using System.Text;

namespace Veliqora.Core.Puzzle
{
    /// <summary>
    /// Immutable snapshot of the board: bottles (with capacities, mystery layers, corks, ad locks) and the number
    /// of valid moves made so far (needed by "unlock after N moves" corks). Undo restores whole snapshots, so
    /// contents, locks, mystery visibility and objective progress always roll back together.
    /// </summary>
    public sealed class GameState : IEquatable<GameState>
    {
        private readonly BottleState[] _bottles;
        private Dictionary<int, int> _totals; // lazily computed; shared between states with identical liquid sets

        public int BottleCount => _bottles.Length;
        public BottleState this[int index] => _bottles[index];
        public IReadOnlyList<BottleState> Bottles => _bottles;
        /// <summary>Valid moves made since the level (or shuffle) started.</summary>
        public int Moves { get; }

        public GameState(IEnumerable<BottleState> bottles, int moves = 0)
        {
            if (bottles == null) throw new ArgumentNullException(nameof(bottles));
            _bottles = new List<BottleState>(bottles).ToArray();
            for (int i = 0; i < _bottles.Length; i++)
                if (_bottles[i] == null) throw new ArgumentException("Null bottle");
            Moves = Math.Max(0, moves);
        }

        private GameState(BottleState[] bottles, int moves, Dictionary<int, int> totals)
        {
            _bottles = bottles;
            Moves = moves;
            _totals = totals;
        }

        public static GameState FromArrays(int capacity, int[][] bottles)
        {
            var list = new BottleState[bottles.Length];
            for (int i = 0; i < bottles.Length; i++) list[i] = new BottleState(capacity, bottles[i]);
            return new GameState(list, 0, null);
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
            return new GameState(next, Moves, _totals);
        }

        public GameState WithBottles(int a, BottleState ba, int b, BottleState bb)
        {
            var next = (BottleState[])_bottles.Clone();
            next[a] = ba;
            next[b] = bb;
            return new GameState(next, Moves, _totals);
        }

        public GameState WithMoves(int moves) => new GameState(_bottles, Math.Max(0, moves), _totals);

        /// <summary>Replaces all bottles (liquid totals may change, e.g. Remove Top Color).</summary>
        public GameState WithAllBottles(BottleState[] bottles) => new GameState((BottleState[])bottles.Clone(), Moves, null);

        public GameState AddBottle(BottleState bottle)
        {
            var next = new BottleState[_bottles.Length + 1];
            Array.Copy(_bottles, next, _bottles.Length);
            next[_bottles.Length] = bottle;
            return new GameState(next, Moves, bottle.IsEmpty ? _totals : null);
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

        public int ColorTotal(int color)
        {
            if (_totals == null) _totals = ColorCounts();
            return _totals.TryGetValue(color, out int n) ? n : 0;
        }

        /// <summary>
        /// A bottle is complete when it is non-empty, single-colored and either full or holding every unit of its
        /// color on the board. With uniform capacities (one bottle per color) this equals "full and monochrome";
        /// it also makes mixed-capacity levels (2/3/5/6) well defined.
        /// </summary>
        public bool IsBottleComplete(int index)
        {
            var b = _bottles[index];
            if (b.IsEmpty || !b.IsMonochrome) return false;
            return b.IsFull || b.Count == ColorTotal(b.TopColor);
        }

        public int CompletedBottleCount()
        {
            int n = 0;
            for (int i = 0; i < _bottles.Length; i++) if (IsBottleComplete(i)) n++;
            return n;
        }

        public int HiddenTotal()
        {
            int n = 0;
            foreach (var b in _bottles) n += b.Hidden;
            return n;
        }

        public bool HasLocks
        {
            get
            {
                foreach (var b in _bottles) if (b.IsLocked) return true;
                return false;
            }
        }

        private bool NeedsExactKey
        {
            get
            {
                foreach (var b in _bottles) if (b.Lock != null && b.Lock.Kind == LockKind.AfterBottle) return true;
                return false;
            }
        }

        /// <summary>Largest move-count threshold among pending "unlock after N moves" corks (0 if none).</summary>
        private int MoveThreshold()
        {
            int t = 0;
            foreach (var b in _bottles) if (b.Lock != null && b.Lock.Kind == LockKind.Moves) t = Math.Max(t, b.Lock.Param);
            return t;
        }

        private void AppendMoves(StringBuilder sb)
        {
            int t = MoveThreshold();
            if (t > 0) sb.Append("#m").Append(Math.Min(Moves, t));
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
            AppendMoves(sb);
            return sb.ToString();
        }

        /// <summary>
        /// Canonical key: bottle order does not matter (bottles are interchangeable), so permutations collapse into
        /// one search node. Falls back to the exact key when a cork refers to another bottle by index.
        /// </summary>
        public string CanonicalKey()
        {
            if (NeedsExactKey) return ExactKey();
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
            sb.Clear();
            sb.Append(string.Join("|", parts));
            AppendMoves(sb);
            return sb.ToString();
        }

        public bool Equals(GameState other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (other is null || other._bottles.Length != _bottles.Length || other.Moves != Moves) return false;
            for (int i = 0; i < _bottles.Length; i++) if (!_bottles[i].Equals(other._bottles[i])) return false;
            return true;
        }

        /// <summary>Same bottles regardless of the move counter (used where only the layout matters).</summary>
        public bool SameLayout(GameState other)
        {
            if (other is null || other._bottles.Length != _bottles.Length) return false;
            for (int i = 0; i < _bottles.Length; i++) if (!_bottles[i].Equals(other._bottles[i])) return false;
            return true;
        }

        public override bool Equals(object obj) => Equals(obj as GameState);

        public override int GetHashCode()
        {
            unchecked
            {
                int h = 17 + Moves;
                for (int i = 0; i < _bottles.Length; i++) h = h * 486187739 + _bottles[i].GetHashCode();
                return h;
            }
        }

        public override string ToString() => ExactKey();
    }
}
