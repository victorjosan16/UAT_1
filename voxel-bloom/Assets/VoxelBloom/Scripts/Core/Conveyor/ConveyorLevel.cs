using System;
using System.Collections.Generic;

namespace VoxelBloom.Core
{
    public enum StationLock { None, Completed, Booster }

    public sealed class ConveyorStationDef
    {
        /// <summary>Side of the loop the tray is attached to ("top", "right", "bottom", "left") and its position on it.</summary>
        public string Side = "left";
        public float At;
        /// <summary>Pickup point on the loop, in path units (integer, shared with the JS reference).</summary>
        public int S;
        public int Capacity = 4;
        public StationLock Lock;
        /// <summary>For StationLock.Completed: number of completed trays that unlocks the station.</summary>
        public int LockN;
    }

    /// <summary>
    /// One conveyor level (rules v3), loaded from Resources/VoxelBloom/Conveyor/level_NNN.json
    /// (generated and validated by tools/conveyor/build-levels.mjs).
    /// </summary>
    public sealed class ConveyorLevel
    {
        public int Id;
        public string Name;
        public bool Tutorial;
        public float BoardW = 16, BoardH = 18, BoardR = 2;
        public int Slots, Speed, Feeder, Visible;
        public string[] Colors;
        public ConveyorStationDef[] Stations;
        public int[][] Queue;
        public int[] Belt, Hopper;
        public int[][] Witness;
        public int WitnessTicks;
        public int RewardCoins;
        public float CasualWinRate;

        public int TotalCubes { get { int n = 0; foreach (var t in Queue) n += t.Length; return n; } }

        public static ConveyorLevel FromJson(Dictionary<string, object> d)
        {
            if (d == null) throw new FormatException("Level is not an object");
            if (MiniJson.GetInt(d, "rules") != 3) throw new FormatException("Not a conveyor level (rules 3)");
            var l = new ConveyorLevel
            {
                Id = MiniJson.GetInt(d, "id"),
                Name = MiniJson.GetString(d, "name", "Nivel"),
                Tutorial = MiniJson.GetBool(d, "tutorial"),
                Slots = MiniJson.GetInt(d, "slots"),
                Speed = MiniJson.GetInt(d, "speed", 6),
                Feeder = MiniJson.GetInt(d, "feeder"),
                Visible = MiniJson.GetInt(d, "visible", 7),
                WitnessTicks = MiniJson.GetInt(d, "witnessTicks"),
                RewardCoins = MiniJson.GetInt(d, "rewardCoins", 40),
                CasualWinRate = (float)MiniJson.GetDouble(d, "casualWinRate", 1),
            };
            var board = MiniJson.GetObj(d, "board");
            if (board != null)
            {
                l.BoardW = (float)MiniJson.GetDouble(board, "w", 16);
                l.BoardH = (float)MiniJson.GetDouble(board, "h", 18);
                l.BoardR = (float)MiniJson.GetDouble(board, "r", 2);
            }
            var cols = MiniJson.GetArr(d, "colors") ?? throw new FormatException("colors missing");
            l.Colors = new string[cols.Count];
            for (int i = 0; i < cols.Count; i++) l.Colors[i] = (string)cols[i];
            var st = MiniJson.GetArr(d, "stations") ?? throw new FormatException("stations missing");
            l.Stations = new ConveyorStationDef[st.Count];
            for (int i = 0; i < st.Count; i++)
            {
                var o = MiniJson.Obj(st[i]);
                var s = new ConveyorStationDef
                {
                    Side = MiniJson.GetString(o, "side", "left"),
                    At = (float)MiniJson.GetDouble(o, "at"),
                    S = MiniJson.GetInt(o, "s"),
                    Capacity = MiniJson.GetInt(o, "capacity", 4),
                };
                var lk = MiniJson.GetObj(o, "lock");
                if (lk != null)
                {
                    string type = MiniJson.GetString(lk, "type", "");
                    s.Lock = type == "booster" ? StationLock.Booster : StationLock.Completed;
                    s.LockN = MiniJson.GetInt(lk, "n");
                }
                l.Stations[i] = s;
            }
            l.Queue = Ints2(MiniJson.GetArr(d, "queue"));
            l.Belt = Ints(MiniJson.GetArr(d, "belt"));
            l.Hopper = Ints(MiniJson.GetArr(d, "hopper"));
            l.Witness = Ints2(MiniJson.GetArr(d, "witness"));
            l.Validate();
            return l;
        }

        /// <summary>Structural validation (mirrors validate() in build-levels.mjs).</summary>
        public void Validate()
        {
            if (Slots <= 0 || Belt.Length != Slots) throw new FormatException("belt size");
            if (Speed <= 0 || Speed >= ConveyorSession.P) throw new FormatException("speed");
            var supply = new int[Colors.Length]; var demand = new int[Colors.Length];
            foreach (int c in Belt) if (c >= 0) { if (c >= Colors.Length) throw new FormatException("colour index"); supply[c]++; }
            foreach (int c in Hopper) { if (c < 0 || c >= Colors.Length) throw new FormatException("colour index"); supply[c]++; }
            foreach (var t in Queue)
            {
                if (t.Length == 0) throw new FormatException("empty tray");
                if (t.Length > 4) throw new FormatException("tray too long");
                foreach (int c in t) { if (c < 0 || c >= Colors.Length) throw new FormatException("colour index"); demand[c]++; }
            }
            for (int c = 0; c < supply.Length; c++)
                if (supply[c] != demand[c]) throw new FormatException("colour " + c + ": supply " + supply[c] + " != demand " + demand[c]);
            bool free = false;
            foreach (var s in Stations) if (s.Lock == StationLock.None) free = true;
            if (!free) throw new FormatException("no free station");
        }

        static int[] Ints(List<object> a)
        {
            if (a == null) return Array.Empty<int>();
            var r = new int[a.Count];
            for (int i = 0; i < r.Length; i++) r[i] = (int)(double)a[i];
            return r;
        }

        static int[][] Ints2(List<object> a)
        {
            if (a == null) return Array.Empty<int[]>();
            var r = new int[a.Count][];
            for (int i = 0; i < r.Length; i++) r[i] = Ints(MiniJson.Arr(a[i]));
            return r;
        }
    }
}
