using System.Collections.Generic;
using Veliqora.Core.Json;
using Veliqora.Core.Levels;
using Veliqora.Core.Puzzle;

namespace Veliqora.Core.Session
{
    /// <summary>Serializable in-progress puzzle (current board, initial layout, bounded undo window).</summary>
    public sealed class SessionSnapshot
    {
        public int LevelNumber;
        public string AttemptId;
        public int Capacity;
        public int[][] Initial;
        public int[][] Current;
        public List<int[][]> History = new List<int[][]>();
        public List<int[]> Moves = new List<int[]>();
        public int MovesBeforeHistory;
        public int ExtraBottlesAdded;
        public int ShuffleCount;
        public int ParMoves;
        public int TotalPours;
        public int UndosUsed;
        public int[][] Solution;

        /// <summary>Checks the snapshot uses exactly the level's colors (plus allowed extra empty bottles).</summary>
        public bool IsConsistentWith(LevelDefinition level)
        {
            if (Initial == null || Current == null || History.Count != Moves.Count) return false;
            int expectedBottles = level.Bottles.Length + ExtraBottlesAdded;
            if (Initial.Length != expectedBottles || Current.Length != expectedBottles) return false;
            if (ExtraBottlesAdded < 0 || ExtraBottlesAdded > GameSession.MaxExtraBottles) return false;
            try
            {
                var levelCounts = level.CreateInitialState().ColorCounts();
                if (!SameCounts(levelCounts, GameState.FromArrays(level.Capacity, Initial).ColorCounts())) return false;
                if (!SameCounts(levelCounts, GameState.FromArrays(level.Capacity, Current).ColorCounts())) return false;
                foreach (var h in History)
                {
                    if (h.Length != expectedBottles) return false;
                    if (!SameCounts(levelCounts, GameState.FromArrays(level.Capacity, h).ColorCounts())) return false;
                }
            }
            catch (System.ArgumentException)
            {
                return false;
            }
            return true;
        }

        private static bool SameCounts(Dictionary<int, int> a, Dictionary<int, int> b)
        {
            if (a.Count != b.Count) return false;
            foreach (var kv in a)
                if (!b.TryGetValue(kv.Key, out int v) || v != kv.Value) return false;
            return true;
        }

        public Dictionary<string, object> ToJson()
        {
            var history = new List<object>();
            foreach (var h in History) history.Add(Board(h));
            var moves = new List<object>();
            foreach (var m in Moves) moves.Add(MiniJson.FromIntArray(m));
            return new Dictionary<string, object>
            {
                ["level"] = (long)LevelNumber,
                ["attemptId"] = AttemptId ?? "",
                ["capacity"] = (long)Capacity,
                ["initial"] = Board(Initial),
                ["current"] = Board(Current),
                ["history"] = history,
                ["moves"] = moves,
                ["movesBeforeHistory"] = (long)MovesBeforeHistory,
                ["extraBottles"] = (long)ExtraBottlesAdded,
                ["shuffles"] = (long)ShuffleCount,
                ["par"] = (long)ParMoves,
                ["totalPours"] = (long)TotalPours,
                ["undosUsed"] = (long)UndosUsed,
                ["solution"] = Solution != null ? Board(Solution) : null,
            };
        }

        public static SessionSnapshot FromJson(Dictionary<string, object> d)
        {
            var s = new SessionSnapshot
            {
                LevelNumber = MiniJson.GetInt(d, "level"),
                AttemptId = MiniJson.GetString(d, "attemptId", ""),
                Capacity = MiniJson.GetInt(d, "capacity", BottleState.DefaultCapacity),
                Initial = ReadBoard(d, "initial"),
                Current = ReadBoard(d, "current"),
                MovesBeforeHistory = MiniJson.GetInt(d, "movesBeforeHistory"),
                ExtraBottlesAdded = MiniJson.GetInt(d, "extraBottles"),
                ShuffleCount = MiniJson.GetInt(d, "shuffles"),
                ParMoves = MiniJson.GetInt(d, "par"),
                TotalPours = MiniJson.GetInt(d, "totalPours"),
                UndosUsed = MiniJson.GetInt(d, "undosUsed"),
                Solution = MiniJson.TryGet(d, "solution", out _) ? ReadBoard(d, "solution") : null,
            };
            foreach (var h in MiniJson.GetList(d, "history")) s.History.Add(ToBoard(h));
            foreach (var m in MiniJson.GetList(d, "moves")) s.Moves.Add(MiniJson.ToIntArray(m));
            return s;
        }

        private static List<object> Board(int[][] b)
        {
            var list = new List<object>(b.Length);
            foreach (var x in b) list.Add(MiniJson.FromIntArray(x));
            return list;
        }

        private static int[][] ReadBoard(Dictionary<string, object> d, string key) => ToBoard(MiniJson.GetList(d, key));

        private static int[][] ToBoard(object o)
        {
            var list = MiniJson.AsList(o);
            var b = new int[list.Count][];
            for (int i = 0; i < list.Count; i++) b[i] = MiniJson.ToIntArray(list[i]);
            return b;
        }
    }
}
