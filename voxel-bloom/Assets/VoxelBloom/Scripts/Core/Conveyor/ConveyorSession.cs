using System;
using System.Collections.Generic;

namespace VoxelBloom.Core
{
    public enum ConveyorEventType { Place, Take, Land, Complete, Feed, Leave }

    public struct ConveyorEvent
    {
        public ConveyorEventType Type;
        public int Slot, Station, Color, Tray, Compartment, Tick;
    }

    public sealed class ConveyorTray
    {
        public int Id;
        public int[] Colors;
        /// <summary>Compartments reserved by cubes (filled + in flight); the next required colour is Colors[Reserved].</summary>
        public int Reserved;
        public int Filled;
        public ConveyorTray Clone() => new ConveyorTray { Id = Id, Colors = Colors, Reserved = Reserved, Filled = Filled };
    }

    public sealed class ConveyorStation
    {
        public int S, Capacity;
        public StationLock Lock;
        public int LockN;
        public ConveyorTray Tray;
        /// <summary>Ticks until a completed tray has left (0 = not leaving).</summary>
        public int Leave;
        public bool IsFree => Lock == StationLock.None && Tray == null && Leave == 0;
    }

    public struct ConveyorTransfer { public int Station, Color, Compartment, T; }

    /// <summary>
    /// Conveyor sort rules v3 (engine-free, deterministic, integer and tick based at 60 ticks/s).
    /// Mirrors tools/conveyor/rules.mjs line by line; witness replays prove both agree. See Docs/ConveyorRules.md.
    /// </summary>
    public sealed class ConveyorSession
    {
        public const int P = 100, Transfer = 18, LeaveTicks = 24, TicksPerSecond = 60, MaxTicks = 60 * 60 * 6;

        public readonly ConveyorLevel Level;
        public readonly int C, L, V;
        public int Offset { get; private set; }
        public int Tick { get; private set; }
        public readonly int[] Slot;
        public int HopperPos { get; private set; }
        /// <summary>Cubes waiting to enter the belt (own copy: the colour-shuffle booster may reorder it).</summary>
        readonly int[] hopper;
        public readonly List<ConveyorTray> Queue = new List<ConveyorTray>();
        public readonly ConveyorStation[] Stations;
        public readonly List<ConveyorTransfer> Transfers = new List<ConveyorTransfer>();
        public int Completed { get; private set; }
        public int Delivered { get; private set; }
        public int Placed { get; private set; }
        public int BoostersUsed { get; private set; }
        public int Total => Level.Queue.Length;
        /// <summary>Events produced by Place/Step since the last ClearEvents (for the view); null when not recording.</summary>
        public readonly List<ConveyorEvent> Events;
        readonly Stack<State> undo;

        public ConveyorSession(ConveyorLevel level, bool recordEvents = false, bool enableUndo = false)
        {
            Level = level ?? throw new ArgumentNullException(nameof(level));
            C = level.Slots; L = C * P; V = level.Speed;
            Slot = (int[])level.Belt.Clone();
            hopper = (int[])level.Hopper.Clone();
            for (int i = 0; i < level.Queue.Length; i++) Queue.Add(new ConveyorTray { Id = i, Colors = level.Queue[i] });
            Stations = new ConveyorStation[level.Stations.Length];
            for (int i = 0; i < Stations.Length; i++)
            {
                var d = level.Stations[i];
                Stations[i] = new ConveyorStation { S = d.S, Capacity = d.Capacity, Lock = d.Lock, LockN = d.LockN };
            }
            if (recordEvents) Events = new List<ConveyorEvent>();
            if (enableUndo) undo = new Stack<State>();
            UpdateLocks();
        }

        // ------------------------------------------------------------------ queries
        public int SlotPos(int k) => (k * P + Offset) % L;
        public bool IsWon => Completed == Total;
        public int HopperLeft => hopper.Length - HopperPos;
        public int VisibleCount => Math.Min(Level.Visible, Queue.Count);
        public int UndoCount => undo?.Count ?? 0;

        public int BeltCount { get { int n = 0; foreach (int c in Slot) if (c >= 0) n++; return n; } }

        public int NextColor(ConveyorStation st)
        {
            var t = st.Tray;
            return t != null && t.Reserved < t.Colors.Length ? t.Colors[t.Reserved] : -1;
        }

        public int FreeStation(int len)
        {
            for (int k = 0; k < Stations.Length; k++) if (Stations[k].IsFree && Stations[k].Capacity >= len) return k;
            return -1;
        }

        public bool CanPlace(int qi) => qi >= 0 && qi < VisibleCount && !IsWon && FreeStation(Queue[qi].Colors.Length) >= 0;

        public bool AnyPlaceable()
        {
            for (int i = 0; i < VisibleCount; i++) if (CanPlace(i)) return true;
            return false;
        }

        /// <summary>Deadlock: nothing in flight or leaving, no tray can be placed, no cube can enter, no belt cube is needed.</summary>
        public bool IsStuck()
        {
            if (IsWon || Transfers.Count > 0) return false;
            foreach (var st in Stations) if (st.Leave > 0) return false;
            if (AnyPlaceable()) return false;
            if (HopperLeft > 0 && BeltCount < C) return false;
            foreach (int c in Slot)
            {
                if (c < 0) continue;
                foreach (var st in Stations) if (NextColor(st) == c) return false;
            }
            return true;
        }

        // ------------------------------------------------------------------ actions
        /// <summary>Sends visible queued tray <paramref name="qi"/> to the first free station; returns the station or -1 (nothing consumed).</summary>
        public int Place(int qi)
        {
            if (!CanPlace(qi)) return -1;
            undo?.Push(Capture());
            var tray = Queue[qi];
            Queue.RemoveAt(qi);
            int k = FreeStation(tray.Colors.Length);
            Stations[k].Tray = new ConveyorTray { Id = tray.Id, Colors = tray.Colors };
            Placed++;
            Events?.Add(new ConveyorEvent { Type = ConveyorEventType.Place, Tray = tray.Id, Station = k, Slot = qi, Tick = Tick });
            return k;
        }

        public void Step()
        {
            Tick++;
            for (int i = 0; i < Transfers.Count; i++)
            {
                var tr = Transfers[i];
                tr.T--;
                if (tr.T > 0) { Transfers[i] = tr; continue; }
                var st = Stations[tr.Station];
                st.Tray.Filled++;
                Delivered++;
                Events?.Add(new ConveyorEvent { Type = ConveyorEventType.Land, Station = tr.Station, Color = tr.Color, Compartment = tr.Compartment, Tick = Tick });
                if (st.Tray.Filled == st.Tray.Colors.Length)
                {
                    Completed++;
                    st.Leave = LeaveTicks;
                    Events?.Add(new ConveyorEvent { Type = ConveyorEventType.Complete, Station = tr.Station, Tray = st.Tray.Id, Tick = Tick });
                    UpdateLocks();
                }
                Transfers.RemoveAt(i); i--;
            }
            for (int si = 0; si < Stations.Length; si++)
            {
                var st = Stations[si];
                if (st.Leave > 0 && --st.Leave == 0)
                {
                    Events?.Add(new ConveyorEvent { Type = ConveyorEventType.Leave, Station = si, Tray = st.Tray.Id, Tick = Tick });
                    st.Tray = null;
                }
            }
            int prev = Offset;
            Offset = (Offset + V) % L;
            for (int k = 0; k < C; k++)
            {
                int p0 = (k * P + prev) % L;
                if (Slot[k] >= 0)
                {
                    for (int si = 0; si < Stations.Length; si++)
                    {
                        var st = Stations[si];
                        if (st.Tray == null || st.Leave > 0) continue;
                        int d = (st.S - p0 + L) % L;
                        if (d == 0 || d > V) continue;
                        if (NextColor(st) != Slot[k]) continue;
                        Transfers.Add(new ConveyorTransfer { Station = si, Color = Slot[k], Compartment = st.Tray.Reserved, T = Transfer });
                        Events?.Add(new ConveyorEvent { Type = ConveyorEventType.Take, Slot = k, Station = si, Color = Slot[k], Compartment = st.Tray.Reserved, Tick = Tick });
                        st.Tray.Reserved++;
                        Slot[k] = -1;
                        break;
                    }
                }
                if (Slot[k] < 0 && HopperLeft > 0)
                {
                    int d = (Level.Feeder - p0 + L) % L;
                    if (d != 0 && d <= V)
                    {
                        Slot[k] = hopper[HopperPos++];
                        Events?.Add(new ConveyorEvent { Type = ConveyorEventType.Feed, Slot = k, Color = Slot[k], Tick = Tick });
                    }
                }
            }
        }

        void UpdateLocks()
        {
            foreach (var st in Stations)
                if (st.Lock == StationLock.Completed && Completed >= st.LockN) st.Lock = StationLock.None;
        }

        public void ClearEvents() => Events?.Clear();

        // ------------------------------------------------------------------ boosters
        /// <summary>Undo: restores the state from just before the last tray placement.</summary>
        public bool UndoLast()
        {
            if (undo == null || undo.Count == 0) return false;
            int used = BoostersUsed + 1;
            Restore(undo.Pop());
            BoostersUsed = used;
            return true;
        }

        /// <summary>Extra station: opens the first green "plus" slot. False (nothing consumed) if there is none.</summary>
        public bool ExtraStation()
        {
            foreach (var st in Stations)
                if (st.Lock == StationLock.Booster) { st.Lock = StationLock.None; BoostersUsed++; return true; }
            return false;
        }

        public bool HasLockedPlusSlot()
        {
            foreach (var st in Stations) if (st.Lock == StationLock.Booster) return true;
            return false;
        }

        /// <summary>Reorders the queued trays (seeded); keeps a new order only if the greedy player still wins from here.</summary>
        public bool Shuffle(DeterministicRandom rand)
        {
            if (Queue.Count < 2) return false;
            var orig = new List<ConveyorTray>(Queue);
            for (int attempt = 0; attempt < 20; attempt++)
            {
                var q = new List<ConveyorTray>(orig);
                for (int i = q.Count - 1; i > 0; i--) { int j = rand.Next(i + 1); var t = q[i]; q[i] = q[j]; q[j] = t; }
                Queue.Clear(); Queue.AddRange(q);
                if (ConveyorSolver.PlayGreedy(CloneForSim()).Won) { BoostersUsed++; return true; }
            }
            Queue.Clear(); Queue.AddRange(orig);
            return false;
        }

        /// <summary>
        /// "Amestecă culorile": reorders the colours of the cubes on the belt and in the feeder (positions stay, supply is
        /// unchanged). A new arrangement is kept only if the greedy player can still win; otherwise nothing changes.
        /// </summary>
        public bool ShuffleColors(DeterministicRandom rand)
        {
            var idx = new List<int>();          // >= 0: belt slot, < 0: -(hopper index) - 1
            var cols = new List<int>();
            for (int k = 0; k < C; k++) if (Slot[k] >= 0) { idx.Add(k); cols.Add(Slot[k]); }
            for (int h = HopperPos; h < hopper.Length; h++) { idx.Add(-h - 1); cols.Add(hopper[h]); }
            if (cols.Count < 2) return false;
            var orig = cols.ToArray();
            for (int attempt = 0; attempt < 20; attempt++)
            {
                for (int i = cols.Count - 1; i > 0; i--) { int j = rand.Next(i + 1); int t = cols[i]; cols[i] = cols[j]; cols[j] = t; }
                Assign(idx, cols);
                if (ConveyorSolver.PlayGreedy(CloneForSim()).Won) { BoostersUsed++; return true; }
            }
            Assign(idx, new List<int>(orig));
            return false;
        }

        /// <summary>Cubes still to deliver equal the compartments still to fill, per colour (invariant).</summary>
        public bool HasValidSupply()
        {
            var need = new int[Level.Colors.Length];
            foreach (var t in Queue) foreach (int c in t.Colors) need[c]++;
            foreach (var st in Stations) if (st.Tray != null) for (int i = st.Tray.Reserved; i < st.Tray.Colors.Length; i++) need[st.Tray.Colors[i]]++;
            foreach (int c in Slot) if (c >= 0) need[c]--;
            for (int h = HopperPos; h < hopper.Length; h++) need[hopper[h]]--;
            foreach (int n in need) if (n != 0) return false;
            return true;
        }

        void Assign(List<int> idx, List<int> cols)
        {
            for (int i = 0; i < idx.Count; i++) { if (idx[i] >= 0) Slot[idx[i]] = cols[i]; else hopper[-idx[i] - 1] = cols[i]; }
        }

        // ------------------------------------------------------------------ state
        sealed class State
        {
            public int Offset, Tick, HopperPos, Completed, Delivered, Placed, BoostersUsed;
            public int[] Slot, Hopper;
            public List<ConveyorTray> Queue;
            public StationLock[] Locks;
            public ConveyorTray[] Trays;
            public int[] Leave;
            public List<ConveyorTransfer> Transfers;
        }

        State Capture()
        {
            var s = new State
            {
                Offset = Offset, Tick = Tick, HopperPos = HopperPos, Completed = Completed, Delivered = Delivered, Placed = Placed, BoostersUsed = BoostersUsed,
                Slot = (int[])Slot.Clone(), Hopper = (int[])hopper.Clone(), Queue = new List<ConveyorTray>(Queue), Locks = new StationLock[Stations.Length],
                Trays = new ConveyorTray[Stations.Length], Leave = new int[Stations.Length], Transfers = new List<ConveyorTransfer>(Transfers),
            };
            for (int i = 0; i < Stations.Length; i++) { s.Locks[i] = Stations[i].Lock; s.Trays[i] = Stations[i].Tray?.Clone(); s.Leave[i] = Stations[i].Leave; }
            return s;
        }

        void Restore(State s)
        {
            Offset = s.Offset; Tick = s.Tick; HopperPos = s.HopperPos; Completed = s.Completed; Delivered = s.Delivered; Placed = s.Placed; BoostersUsed = s.BoostersUsed;
            Array.Copy(s.Slot, Slot, Slot.Length);
            Array.Copy(s.Hopper, hopper, hopper.Length);
            Queue.Clear(); Queue.AddRange(s.Queue);
            for (int i = 0; i < Stations.Length; i++) { Stations[i].Lock = s.Locks[i]; Stations[i].Tray = s.Trays[i]?.Clone(); Stations[i].Leave = s.Leave[i]; }
            Transfers.Clear(); Transfers.AddRange(s.Transfers);
        }

        public ConveyorSession CloneForSim()
        {
            var c = new ConveyorSession(Level);
            c.Restore(Capture());
            return c;
        }

        // ------------------------------------------------------------------ persistence (resume an unfinished level)
        public Dictionary<string, object> ToSnapshot()
        {
            var q = new List<object>();
            foreach (var t in Queue) q.Add((double)t.Id);
            var st = new List<object>();
            foreach (var s in Stations)
                st.Add(new Dictionary<string, object>
                {
                    { "lock", (double)(int)s.Lock }, { "leave", (double)s.Leave },
                    { "tray", s.Tray == null ? -1.0 : s.Tray.Id }, { "res", (double)(s.Tray?.Reserved ?? 0) }, { "fill", (double)(s.Tray?.Filled ?? 0) },
                });
            var tr = new List<object>();
            foreach (var t in Transfers) tr.Add(new List<object> { (double)t.Station, (double)t.Color, (double)t.Compartment, (double)t.T });
            var slots = new List<object>();
            foreach (int c in Slot) slots.Add((double)c);
            var hop = new List<object>();
            foreach (int c in hopper) hop.Add((double)c);
            return new Dictionary<string, object>
            {
                { "level", (double)Level.Id }, { "rules", 3.0 }, { "sig", Signature(Level) }, { "offset", (double)Offset }, { "tick", (double)Tick }, { "hopperPos", (double)HopperPos },
                { "completed", (double)Completed }, { "delivered", (double)Delivered }, { "placed", (double)Placed }, { "boosters", (double)BoostersUsed },
                { "slots", slots }, { "hopper", hop }, { "queue", q }, { "stations", st }, { "transfers", tr },
            };
        }

        /// <summary>Fingerprint of the level content, so a save is never restored onto regenerated data.</summary>
        public static string Signature(ConveyorLevel l)
        {
            uint h = 2166136261u;
            void Mix(int v) { unchecked { h = (h ^ (uint)v) * 16777619u; } }
            Mix(l.Slots); Mix(l.Speed); Mix(l.Feeder); Mix(l.Visible);
            foreach (int c in l.Belt) Mix(c);
            foreach (int c in l.Hopper) Mix(c);
            foreach (var t in l.Queue) { Mix(-9); foreach (int c in t) Mix(c); }
            foreach (var st in l.Stations) { Mix(st.S); Mix((int)st.Lock); Mix(st.LockN); }
            return h.ToString();
        }

        public static ConveyorSession FromSnapshot(ConveyorLevel level, Dictionary<string, object> d, bool recordEvents = true, bool enableUndo = true)
        {
            if (d == null || MiniJson.GetInt(d, "rules") != 3 || MiniJson.GetInt(d, "level") != level.Id) return null;
            if (MiniJson.GetString(d, "sig", "") != Signature(level)) return null; // saved with another version of the level
            try
            {
                var s = new ConveyorSession(level, recordEvents, enableUndo);
                var slots = MiniJson.GetArr(d, "slots");
                if (slots == null || slots.Count != s.C) return null;
                for (int i = 0; i < s.C; i++) s.Slot[i] = (int)(double)slots[i];
                var hop = MiniJson.GetArr(d, "hopper");
                if (hop != null && hop.Count == s.hopper.Length) for (int i = 0; i < hop.Count; i++) s.hopper[i] = (int)(double)hop[i];
                s.Offset = MiniJson.GetInt(d, "offset"); s.Tick = MiniJson.GetInt(d, "tick"); s.HopperPos = MiniJson.GetInt(d, "hopperPos");
                s.Completed = MiniJson.GetInt(d, "completed"); s.Delivered = MiniJson.GetInt(d, "delivered"); s.Placed = MiniJson.GetInt(d, "placed");
                s.BoostersUsed = MiniJson.GetInt(d, "boosters");
                s.Queue.Clear();
                foreach (var o in MiniJson.GetArr(d, "queue")) { int id = (int)(double)o; s.Queue.Add(new ConveyorTray { Id = id, Colors = level.Queue[id] }); }
                var st = MiniJson.GetArr(d, "stations");
                for (int i = 0; i < s.Stations.Length; i++)
                {
                    var o = MiniJson.Obj(st[i]);
                    s.Stations[i].Lock = (StationLock)MiniJson.GetInt(o, "lock");
                    s.Stations[i].Leave = MiniJson.GetInt(o, "leave");
                    int tray = MiniJson.GetInt(o, "tray", -1);
                    s.Stations[i].Tray = tray < 0 ? null : new ConveyorTray { Id = tray, Colors = level.Queue[tray], Reserved = MiniJson.GetInt(o, "res"), Filled = MiniJson.GetInt(o, "fill") };
                }
                foreach (var o in MiniJson.GetArr(d, "transfers"))
                {
                    var a = MiniJson.Arr(o);
                    s.Transfers.Add(new ConveyorTransfer { Station = (int)(double)a[0], Color = (int)(double)a[1], Compartment = (int)(double)a[2], T = (int)(double)a[3] });
                }
                // conservation check: nothing lost or duplicated
                if (s.BeltCount + s.HopperLeft + s.Transfers.Count + s.Delivered != level.TotalCubes) return null;
                return s;
            }
            catch (Exception) { return null; }
        }
    }
}
