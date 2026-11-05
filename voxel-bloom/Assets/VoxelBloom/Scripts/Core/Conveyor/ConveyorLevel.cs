using System;
using System.Collections.Generic;

namespace VoxelBloom.Core
{
    public enum StationLock { None, Completed, Booster }

    /// <summary>A BUS as authored: compartment i requires Need[i] and starts with Have[i] (a colour or -1 = empty).</summary>
    public sealed class BusDef
    {
        public int[] Need, Have;
    }

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
        /// <summary>Difficulty tier 0..3 (Ușor, Normal, Greu, Foarte greu) and board size S/M/L.</summary>
        public int Tier = 1;
        public string TierName = "Normal", BoardSize = "M";
        public float BoardW = 16, BoardH = 18, BoardR = 2;
        public int Slots, Speed, Feeder, Visible;
        public string[] Colors;
        public ConveyorStationDef[] Stations;
        /// <summary>BUS already placed at each station at the start (null = empty station).</summary>
        public BusDef[] Start;
        public BusDef[] Queue;
        public int[] Belt, Hopper;
        /// <summary>Space on the belt (the HUD "X/8" counter): releases need occupied &lt; Space.</summary>
        public int Space = 8;
        public int[][] Witness;
        public int WitnessTicks;
        public int RewardCoins;
        public float CasualWinRate;

        /// <summary>Every cube of the level (belt + feeder + all BUS cubes) — equals the required compartments.</summary>
        public int TotalCubes { get { int n = 0; foreach (var b in Start) if (b != null) n += b.Need.Length; foreach (var b in Queue) n += b.Need.Length; return n; } }
        public int TotalBuses { get { int n = Queue.Length; foreach (var b in Start) if (b != null) n++; return n; } }

        public static ConveyorLevel FromJson(Dictionary<string, object> d)
        {
            if (d == null) throw new FormatException("Level is not an object");
            if (MiniJson.GetInt(d, "rules") != 4) throw new FormatException("Not a conveyor BUS level (rules 4)");
            var l = new ConveyorLevel
            {
                Id = MiniJson.GetInt(d, "id"),
                Name = MiniJson.GetString(d, "name", "Nivel"),
                Tutorial = MiniJson.GetBool(d, "tutorial"),
                Slots = MiniJson.GetInt(d, "slots"),
                Speed = MiniJson.GetInt(d, "speed", 6),
                Feeder = MiniJson.GetInt(d, "feeder"),
                Visible = MiniJson.GetInt(d, "visible", 7),
                Space = MiniJson.GetInt(d, "space", 8),
                WitnessTicks = MiniJson.GetInt(d, "witnessTicks"),
                RewardCoins = MiniJson.GetInt(d, "rewardCoins", 40),
                CasualWinRate = (float)MiniJson.GetDouble(d, "casualWinRate", 1),
                Tier = Math.Max(0, Math.Min(3, MiniJson.GetInt(d, "tier", 1))),
                TierName = MiniJson.GetString(d, "tierName", "Normal"),
                BoardSize = MiniJson.GetString(d, "boardSize", "M"),
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
            var startArr = MiniJson.GetArr(d, "start");
            l.Start = new BusDef[l.Stations.Length];
            if (startArr != null) for (int i = 0; i < l.Start.Length && i < startArr.Count; i++) l.Start[i] = Bus(startArr[i]);
            var q = MiniJson.GetArr(d, "queue") ?? new List<object>();
            l.Queue = new BusDef[q.Count];
            for (int i = 0; i < q.Count; i++) l.Queue[i] = Bus(q[i]) ?? throw new FormatException("queue BUS");
            l.Belt = Ints(MiniJson.GetArr(d, "belt"));
            l.Hopper = Ints(MiniJson.GetArr(d, "hopper"));
            l.Witness = Ints2(MiniJson.GetArr(d, "witness"));
            l.Validate();
            return l;
        }

        static BusDef Bus(object o)
        {
            var b = MiniJson.Obj(o);
            if (b == null) return null;
            var need = Ints(MiniJson.GetArr(b, "need"));
            var have = MiniJson.GetArr(b, "have") != null ? Ints(MiniJson.GetArr(b, "have")) : new int[need.Length];
            if (MiniJson.GetArr(b, "have") == null) for (int i = 0; i < have.Length; i++) have[i] = -1;
            return new BusDef { Need = need, Have = have };
        }

        /// <summary>Structural validation (mirrors validate() in build-levels.mjs).</summary>
        public void Validate()
        {
            if (Slots <= 0 || Belt.Length != Slots) throw new FormatException("belt size");
            if (Speed <= 0 || Speed >= ConveyorSession.P) throw new FormatException("speed");
            if (Space <= 0) throw new FormatException("space");
            var supply = new int[Colors.Length]; var demand = new int[Colors.Length];
            int onBelt = 0;
            foreach (int c in Belt) if (c >= 0) { if (c >= Colors.Length) throw new FormatException("colour index"); supply[c]++; onBelt++; }
            if (onBelt >= Space) throw new FormatException("belt starts full");
            foreach (int c in Hopper) { if (c < 0 || c >= Colors.Length) throw new FormatException("colour index"); supply[c]++; }
            void Check(BusDef b, int maxCap)
            {
                if (b.Need.Length == 0 || b.Have.Length != b.Need.Length) throw new FormatException("BUS size");
                if (b.Need.Length > maxCap) throw new FormatException("BUS longer than its station");
                bool complete = true;
                for (int i = 0; i < b.Need.Length; i++)
                {
                    if (b.Need[i] < 0 || b.Need[i] >= Colors.Length || b.Have[i] >= Colors.Length) throw new FormatException("colour index");
                    demand[b.Need[i]]++;
                    if (b.Have[i] >= 0) supply[b.Have[i]]++;
                    if (b.Have[i] != b.Need[i]) complete = false;
                }
                if (complete) throw new FormatException("BUS starts complete");
            }
            int cap = 0; foreach (var s in Stations) cap = Math.Max(cap, s.Capacity);
            for (int k = 0; k < Start.Length; k++)
                if (Start[k] != null) { if (Stations[k].Lock != StationLock.None) throw new FormatException("BUS on a locked station"); Check(Start[k], Stations[k].Capacity); }
            foreach (var b in Queue) Check(b, cap);
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
