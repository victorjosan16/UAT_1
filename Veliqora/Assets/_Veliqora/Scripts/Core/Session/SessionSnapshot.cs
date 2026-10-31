using System.Collections.Generic;
using Veliqora.Core.Json;
using Veliqora.Core.Levels;
using Veliqora.Core.Puzzle;

namespace Veliqora.Core.Session
{
    /// <summary>
    /// Serializable in-progress puzzle: full boards (capacities, mystery visibility, corks, ad-unlocked bottles,
    /// move counter), the initial layout of this attempt and a bounded undo window.
    /// </summary>
    public sealed class SessionSnapshot
    {
        public int LevelNumber;
        public string AttemptId;
        public int Capacity;
        public GameState Initial;
        public GameState Current;
        public List<GameState> History = new List<GameState>();
        public List<int[]> Moves = new List<int[]>();
        public int MovesBeforeHistory;
        public int ExtraBottlesAdded;
        public int ShuffleCount;
        public int ParMoves;
        public int TotalPours;
        public int UndosUsed;
        public int[][] Solution;
        /// <summary>Colors taken off the board by the Remove Top Color booster during this attempt.</summary>
        public List<int> RemovedColors = new List<int>();

        /// <summary>
        /// Checks the snapshot belongs to this level: same bottle structure (plus allowed extra bottles), only the
        /// level's colors, never more units of a color than the level has, and identical liquid across all boards.
        /// </summary>
        public bool IsConsistentWith(LevelDefinition level)
        {
            if (Initial == null || Current == null || History.Count != Moves.Count) return false;
            if (ExtraBottlesAdded < 0 || ExtraBottlesAdded > GameSession.MaxExtraBottles) return false;
            int expectedBottles = level.Bottles.Length + ExtraBottlesAdded;
            var levelCounts = level.CreateInitialState().ColorCounts();
            var reference = Current.ColorCounts();
            if (!Fits(level, Initial, expectedBottles, levelCounts) || !Fits(level, Current, expectedBottles, levelCounts)) return false;
            if (!SameCounts(reference, Initial.ColorCounts())) return false;
            foreach (var h in History)
                if (!Fits(level, h, expectedBottles, levelCounts) || !SameCounts(reference, h.ColorCounts())) return false;
            foreach (var kv in levelCounts)
            {
                int have;
                reference.TryGetValue(kv.Key, out have);
                if (have != kv.Value && !RemovedColors.Contains(kv.Key)) return false;
            }
            return true;
        }

        private static bool Fits(LevelDefinition level, GameState s, int expectedBottles, Dictionary<int, int> levelCounts)
        {
            if (s.BottleCount != expectedBottles) return false;
            for (int i = 0; i < level.Bottles.Length; i++) if (s[i].Capacity != level.CapacityOf(i)) return false;
            foreach (var kv in s.ColorCounts())
            {
                int max;
                if (!levelCounts.TryGetValue(kv.Key, out max) || kv.Value > max) return false;
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
            foreach (var h in History) history.Add(GameStateJson.ToJson(h));
            var moves = new List<object>();
            foreach (var m in Moves) moves.Add(MiniJson.FromIntArray(m));
            var removed = new List<object>();
            foreach (var c in RemovedColors) removed.Add((long)c);
            return new Dictionary<string, object>
            {
                ["level"] = (long)LevelNumber,
                ["attemptId"] = AttemptId ?? "",
                ["capacity"] = (long)Capacity,
                ["initial"] = GameStateJson.ToJson(Initial),
                ["current"] = GameStateJson.ToJson(Current),
                ["history"] = history,
                ["moves"] = moves,
                ["movesBeforeHistory"] = (long)MovesBeforeHistory,
                ["extraBottles"] = (long)ExtraBottlesAdded,
                ["shuffles"] = (long)ShuffleCount,
                ["par"] = (long)ParMoves,
                ["totalPours"] = (long)TotalPours,
                ["undosUsed"] = (long)UndosUsed,
                ["removedColors"] = removed,
                ["solution"] = Solution != null ? Board(Solution) : null,
            };
        }

        public static SessionSnapshot FromJson(Dictionary<string, object> d)
        {
            int cap = MiniJson.GetInt(d, "capacity", BottleState.DefaultCapacity);
            object ini, cur;
            if (!MiniJson.TryGet(d, "initial", out ini) || !MiniJson.TryGet(d, "current", out cur)) throw new System.FormatException("Snapshot boards missing");
            var s = new SessionSnapshot
            {
                LevelNumber = MiniJson.GetInt(d, "level"),
                AttemptId = MiniJson.GetString(d, "attemptId", ""),
                Capacity = cap,
                Initial = GameStateJson.FromJson(ini, cap),
                Current = GameStateJson.FromJson(cur, cap),
                MovesBeforeHistory = MiniJson.GetInt(d, "movesBeforeHistory"),
                ExtraBottlesAdded = MiniJson.GetInt(d, "extraBottles"),
                ShuffleCount = MiniJson.GetInt(d, "shuffles"),
                ParMoves = MiniJson.GetInt(d, "par"),
                TotalPours = MiniJson.GetInt(d, "totalPours"),
                UndosUsed = MiniJson.GetInt(d, "undosUsed"),
                Solution = MiniJson.TryGet(d, "solution", out _) ? ReadBoard(d, "solution") : null,
            };
            foreach (var h in MiniJson.GetList(d, "history")) s.History.Add(GameStateJson.FromJson(h, cap));
            foreach (var m in MiniJson.GetList(d, "moves")) s.Moves.Add(MiniJson.ToIntArray(m));
            foreach (var c in MiniJson.GetList(d, "removedColors")) s.RemovedColors.Add((int)MiniJson.ToLong(c));
            return s;
        }

        private static List<object> Board(int[][] b)
        {
            var list = new List<object>(b.Length);
            foreach (var x in b) list.Add(MiniJson.FromIntArray(x));
            return list;
        }

        private static int[][] ReadBoard(Dictionary<string, object> d, string key)
        {
            var list = MiniJson.GetList(d, key);
            var b = new int[list.Count][];
            for (int i = 0; i < list.Count; i++) b[i] = MiniJson.ToIntArray(list[i]);
            return b;
        }
    }
}
