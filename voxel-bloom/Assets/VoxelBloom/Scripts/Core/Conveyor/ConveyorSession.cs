using System;
using System.Collections.Generic;

namespace VoxelBloom.Core
{
    public enum ConveyorEventType { Place, Lift, Drop, Take, Land, Complete, Feed, Leave }

    public struct ConveyorEvent
    {
        public ConveyorEventType Type;
        public int Slot, Station, Color, Tray, Compartment, Tick;
    }

    /// <summary>A BUS in play: compartment i requires Need[i] and holds Have[i] (a colour, Empty or Incoming).</summary>
    public sealed class ConveyorTray
    {
        public int Id;
        public int[] Need, Have;
        /// <summary>Ticks until a completed BUS has left its station (0 = in play).</summary>
        public int Leave;
        public ConveyorTray Clone() => new ConveyorTray { Id = Id, Need = Need, Have = (int[])Have.Clone(), Leave = Leave };

        public int WrongIndex()
        {
            for (int i = 0; i < Need.Length; i++) if (Have[i] >= 0 && Have[i] != Need[i]) return i;
            return -1;
        }

        public bool IsComplete()
        {
            for (int i = 0; i < Need.Length; i++) if (Have[i] != Need[i]) return false;
            return true;
        }
    }

    public sealed class ConveyorStation
    {
        public int S, Capacity;
        public StationLock Lock;
        public int LockN;
        public ConveyorTray Tray;
        /// <summary>Released cubes waiting at the transfer point for an empty belt slot (in order).</summary>
        public readonly List<int> Out = new List<int>();
        public bool IsFree => Lock == StationLock.None && Tray == null;
        public bool IsOpen => Tray != null && Tray.Leave == 0;
        public int Leave => Tray?.Leave ?? 0;
    }

    public struct ConveyorTransfer { public int Station, Color, Compartment, T; }

    /// <summary>
    /// Conveyor BUS rules v4 (engine-free, deterministic, integer and tick based at 60 ticks/s).
    /// Mirrors tools/conveyor/rules.mjs line by line; witness replays prove both agree. See Docs/ConveyorRules.md.
    /// BUS -> belt (release, only with free space and an empty slot) -> belt circulates clockwise -> BUS (collect into an
    /// empty compartment of the same colour). Space counter = cubes on the belt + cubes waiting to enter it.
    /// </summary>
    public sealed class ConveyorSession
    {
        public const int P = 100, Transfer = 18, LeaveTicks = 24, TicksPerSecond = 60, MaxTicks = 60 * 60 * 8;
        public const int Empty = -1, Incoming = -2;
        public const int ActPlace = 0, ActRelease = 1;

        public readonly ConveyorLevel Level;
        public readonly int C, L, V, Space;
        public int Offset { get; private set; }
        public int Tick { get; private set; }
        public readonly int[] Slot;
        public int HopperPos { get; private set; }
        readonly int[] hopper;
        public readonly List<ConveyorTray> Queue = new List<ConveyorTray>();
        public readonly ConveyorStation[] Stations;
        public readonly List<ConveyorTransfer> Transfers = new List<ConveyorTransfer>();
        public int Completed { get; private set; }
        public int Delivered { get; private set; }
        public int Released { get; private set; }
        /// <summary>Cubes that left the board inside completed BUSes.</summary>
        public int Gone { get; private set; }
        public int Placed { get; private set; }
        public int BoostersUsed { get; private set; }
        public int Total { get; }
        public readonly List<ConveyorEvent> Events;
        readonly Stack<State> undo;

        public ConveyorSession(ConveyorLevel level, bool recordEvents = false, bool enableUndo = false)
        {
            Level = level ?? throw new ArgumentNullException(nameof(level));
            C = level.Slots; L = C * P; V = level.Speed; Space = level.Space > 0 ? level.Space : level.Slots;
            Slot = (int[])level.Belt.Clone();
            hopper = (int[])level.Hopper.Clone();
            Stations = new ConveyorStation[level.Stations.Length];
            for (int i = 0; i < Stations.Length; i++)
            {
                var d = level.Stations[i];
                Stations[i] = new ConveyorStation { S = d.S, Capacity = d.Capacity, Lock = d.Lock, LockN = d.LockN };
            }
            int id = 0;
            for (int k = 0; k < Stations.Length; k++)
                if (level.Start[k] != null) Stations[k].Tray = Bus(id++, level.Start[k]);
            foreach (var b in level.Queue) Queue.Add(Bus(id++, b));
            Total = id;
            if (recordEvents) Events = new List<ConveyorEvent>();
            if (enableUndo) undo = new Stack<State>();
            for (int k = 0; k < Stations.Length; k++) CheckComplete(k);
            UpdateLocks();
        }

        static ConveyorTray Bus(int id, BusDef b) => new ConveyorTray { Id = id, Need = b.Need, Have = (int[])b.Have.Clone() };

        // ------------------------------------------------------------------ queries
        public int SlotPos(int k) => (k * P + Offset) % L;
        public bool IsWon => Completed == Total && Transfers.Count == 0;
        public int HopperLeft => hopper.Length - HopperPos;
        public int VisibleCount => Math.Min(Level.Visible, Queue.Count);
        public int UndoCount => undo?.Count ?? 0;
        public int BeltCount { get { int n = 0; foreach (int c in Slot) if (c >= 0) n++; return n; } }
        public int Waiting { get { int n = 0; foreach (var st in Stations) n += st.Out.Count; return n; } }
        /// <summary>The "X/Space" counter: cubes on the belt plus cubes waiting to enter it.</summary>
        public int Occupied => BeltCount + Waiting;

        public int FreeStation(int len)
        {
            for (int k = 0; k < Stations.Length; k++) if (Stations[k].IsFree && Stations[k].Capacity >= len) return k;
            return -1;
        }

        public bool CanPlace(int qi) => qi >= 0 && qi < VisibleCount && !IsWon && FreeStation(Queue[qi].Need.Length) >= 0;

        public bool AnyPlaceable()
        {
            for (int i = 0; i < VisibleCount; i++) if (CanPlace(i)) return true;
            return false;
        }

        public bool CanRelease(int k) => k >= 0 && k < Stations.Length && Stations[k].IsOpen && Occupied < Space && Stations[k].Tray.WrongIndex() >= 0;

        public bool AnyReleasable()
        {
            for (int k = 0; k < Stations.Length; k++) if (CanRelease(k)) return true;
            return false;
        }

        /// <summary>Compartment of the BUS at <paramref name="st"/> that would collect colour c (-1 if none).</summary>
        public int Wants(ConveyorStation st, int c)
        {
            if (!st.IsOpen) return -1;
            var b = st.Tray;
            for (int i = 0; i < b.Need.Length; i++) if (b.Have[i] == Empty && b.Need[i] == c) return i;
            return -1;
        }

        public bool IsStuck()
        {
            if (IsWon || Transfers.Count > 0 || Waiting > 0) return false;
            foreach (var st in Stations) if (st.Tray != null && st.Tray.Leave > 0) return false;
            if (AnyPlaceable() || AnyReleasable()) return false;
            if (HopperLeft > 0 && Occupied < Space && BeltCount < C) return false;
            foreach (int c in Slot)
            {
                if (c < 0) continue;
                foreach (var st in Stations) if (Wants(st, c) >= 0) return false;
            }
            return true;
        }

        // ------------------------------------------------------------------ actions
        /// <summary>Sends visible queued BUS <paramref name="qi"/> to the first free station; returns the station or -1 (nothing consumed).</summary>
        public int Place(int qi)
        {
            if (!CanPlace(qi)) return -1;
            undo?.Push(Capture());
            var bus = Queue[qi];
            Queue.RemoveAt(qi);
            int k = FreeStation(bus.Need.Length);
            Stations[k].Tray = bus;
            Placed++;
            Events?.Add(new ConveyorEvent { Type = ConveyorEventType.Place, Tray = bus.Id, Station = k, Slot = qi, Tick = Tick });
            CheckComplete(k);
            return k;
        }

        /// <summary>The BUS at station k lets its first wrong cube out; it waits at the transfer point for an empty slot.</summary>
        public bool Release(int k)
        {
            if (!CanRelease(k)) return false;
            undo?.Push(Capture());
            var st = Stations[k];
            int i = st.Tray.WrongIndex(), c = st.Tray.Have[i];
            st.Tray.Have[i] = Empty;
            st.Out.Add(c);
            Released++;
            Events?.Add(new ConveyorEvent { Type = ConveyorEventType.Lift, Station = k, Compartment = i, Color = c, Tick = Tick });
            return true;
        }

        /// <summary>Witness action [tick, kind, index].</summary>
        public bool Act(int[] a) => a[1] == ActPlace ? Place(a[2]) >= 0 : Release(a[2]);

        void CheckComplete(int k)
        {
            var st = Stations[k]; var b = st.Tray;
            if (b == null || b.Leave > 0 || !b.IsComplete() || st.Out.Count > 0) return;
            Completed++;
            b.Leave = LeaveTicks;
            Events?.Add(new ConveyorEvent { Type = ConveyorEventType.Complete, Station = k, Tray = b.Id, Tick = Tick });
            UpdateLocks();
        }

        public void Step()
        {
            Tick++;
            for (int i = 0; i < Transfers.Count; i++)
            {
                var tr = Transfers[i];
                tr.T--;
                if (tr.T > 0) { Transfers[i] = tr; continue; }
                Stations[tr.Station].Tray.Have[tr.Compartment] = tr.Color;
                Delivered++;
                Events?.Add(new ConveyorEvent { Type = ConveyorEventType.Land, Station = tr.Station, Color = tr.Color, Compartment = tr.Compartment, Tick = Tick });
                Transfers.RemoveAt(i); i--;
                CheckComplete(tr.Station);
            }
            for (int k = 0; k < Stations.Length; k++)
            {
                var st = Stations[k];
                if (st.Tray != null && st.Tray.Leave > 0 && --st.Tray.Leave == 0)
                {
                    Events?.Add(new ConveyorEvent { Type = ConveyorEventType.Leave, Station = k, Tray = st.Tray.Id, Tick = Tick });
                    Gone += st.Tray.Need.Length;
                    st.Tray = null;
                }
            }
            int prev = Offset;
            Offset = (Offset + V) % L;
            for (int k = 0; k < C; k++)
            {
                int p0 = (k * P + prev) % L;
                for (int si = 0; si < Stations.Length; si++)
                {
                    var st = Stations[si];
                    int d = (st.S - p0 + L) % L;
                    if (d == 0 || d > V) continue;
                    if (Slot[k] >= 0)
                    {
                        int i = Wants(st, Slot[k]);
                        if (i < 0) continue;
                        st.Tray.Have[i] = Incoming;
                        Transfers.Add(new ConveyorTransfer { Station = si, Color = Slot[k], Compartment = i, T = Transfer });
                        Events?.Add(new ConveyorEvent { Type = ConveyorEventType.Take, Slot = k, Station = si, Color = Slot[k], Compartment = i, Tick = Tick });
                        Slot[k] = -1;
                        break;
                    }
                    if (st.Out.Count > 0)
                    {
                        Slot[k] = st.Out[0];
                        st.Out.RemoveAt(0);
                        Events?.Add(new ConveyorEvent { Type = ConveyorEventType.Drop, Slot = k, Station = si, Color = Slot[k], Tick = Tick });
                        CheckComplete(si);
                        break;
                    }
                }
                if (Slot[k] < 0 && HopperLeft > 0 && Occupied < Space)
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
        public bool UndoLast()
        {
            if (undo == null || undo.Count == 0) return false;
            int used = BoostersUsed + 1;
            Restore(undo.Pop());
            BoostersUsed = used;
            return true;
        }

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

        /// <summary>Reorders the queued BUSes (seeded); keeps a new order only if the greedy player still wins from here.</summary>
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

        /// <summary>"Amestecă culorile": reorders the colours of the cubes on the belt and in the feeder (positions stay).</summary>
        public bool ShuffleColors(DeterministicRandom rand)
        {
            var idx = new List<int>(); var cols = new List<int>();
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

        void Assign(List<int> idx, List<int> cols)
        {
            for (int i = 0; i < idx.Count; i++) { if (idx[i] >= 0) Slot[idx[i]] = cols[i]; else hopper[-idx[i] - 1] = cols[i]; }
        }

        /// <summary>Every cube is somewhere exactly once (belt, waiting, in flight, feeder, in a BUS, or gone with a completed BUS).</summary>
        public int CubeCount()
        {
            int n = BeltCount + Waiting + Transfers.Count + HopperLeft + Gone;
            foreach (var st in Stations) if (st.Tray != null) foreach (int c in st.Tray.Have) if (c >= 0) n++;
            foreach (var b in Queue) foreach (int c in b.Have) if (c >= 0) n++;
            return n;
        }

        public bool HasValidSupply() => CubeCount() == Level.TotalCubes;

        // ------------------------------------------------------------------ state
        sealed class State
        {
            public int Offset, Tick, HopperPos, Completed, Delivered, Released, Gone, Placed, BoostersUsed;
            public int[] Slot, Hopper;
            public List<ConveyorTray> Queue;
            public StationLock[] Locks;
            public ConveyorTray[] Trays;
            public List<int>[] Out;
            public List<ConveyorTransfer> Transfers;
        }

        State Capture()
        {
            var s = new State
            {
                Offset = Offset, Tick = Tick, HopperPos = HopperPos, Completed = Completed, Delivered = Delivered, Released = Released, Gone = Gone,
                Placed = Placed, BoostersUsed = BoostersUsed, Slot = (int[])Slot.Clone(), Hopper = (int[])hopper.Clone(),
                Queue = new List<ConveyorTray>(), Locks = new StationLock[Stations.Length], Trays = new ConveyorTray[Stations.Length],
                Out = new List<int>[Stations.Length], Transfers = new List<ConveyorTransfer>(Transfers),
            };
            foreach (var b in Queue) s.Queue.Add(b.Clone());
            for (int i = 0; i < Stations.Length; i++) { s.Locks[i] = Stations[i].Lock; s.Trays[i] = Stations[i].Tray?.Clone(); s.Out[i] = new List<int>(Stations[i].Out); }
            return s;
        }

        void Restore(State s)
        {
            Offset = s.Offset; Tick = s.Tick; HopperPos = s.HopperPos; Completed = s.Completed; Delivered = s.Delivered; Released = s.Released; Gone = s.Gone;
            Placed = s.Placed; BoostersUsed = s.BoostersUsed;
            Array.Copy(s.Slot, Slot, Slot.Length);
            Array.Copy(s.Hopper, hopper, hopper.Length);
            Queue.Clear(); foreach (var b in s.Queue) Queue.Add(b.Clone());
            for (int i = 0; i < Stations.Length; i++)
            {
                Stations[i].Lock = s.Locks[i]; Stations[i].Tray = s.Trays[i]?.Clone();
                Stations[i].Out.Clear(); Stations[i].Out.AddRange(s.Out[i]);
            }
            Transfers.Clear(); Transfers.AddRange(s.Transfers);
        }

        public ConveyorSession CloneForSim()
        {
            var c = new ConveyorSession(Level);
            c.Restore(Capture());
            return c;
        }

        // ------------------------------------------------------------------ persistence (resume an unfinished level)
        static List<object> L2(IEnumerable<int> a) { var l = new List<object>(); foreach (int v in a) l.Add((double)v); return l; }
        static int[] I(List<object> a) { var r = new int[a.Count]; for (int i = 0; i < r.Length; i++) r[i] = (int)(double)a[i]; return r; }

        static Dictionary<string, object> BusObj(ConveyorTray b) => new Dictionary<string, object>
            { { "id", (double)b.Id }, { "need", L2(b.Need) }, { "have", L2(b.Have) }, { "leave", (double)b.Leave } };

        static ConveyorTray ReadBus(object o)
        {
            var d = MiniJson.Obj(o);
            if (d == null) return null;
            return new ConveyorTray { Id = MiniJson.GetInt(d, "id"), Need = I(MiniJson.GetArr(d, "need")), Have = I(MiniJson.GetArr(d, "have")), Leave = MiniJson.GetInt(d, "leave") };
        }

        public Dictionary<string, object> ToSnapshot()
        {
            var q = new List<object>();
            foreach (var b in Queue) q.Add(BusObj(b));
            var st = new List<object>();
            foreach (var s in Stations)
                st.Add(new Dictionary<string, object> { { "lock", (double)(int)s.Lock }, { "bus", s.Tray == null ? null : BusObj(s.Tray) }, { "out", L2(s.Out) } });
            var tr = new List<object>();
            foreach (var t in Transfers) tr.Add(new List<object> { (double)t.Station, (double)t.Color, (double)t.Compartment, (double)t.T });
            return new Dictionary<string, object>
            {
                { "level", (double)Level.Id }, { "rules", 4.0 }, { "sig", Signature(Level) }, { "offset", (double)Offset }, { "tick", (double)Tick },
                { "hopperPos", (double)HopperPos }, { "completed", (double)Completed }, { "delivered", (double)Delivered }, { "released", (double)Released },
                { "gone", (double)Gone }, { "placed", (double)Placed }, { "boosters", (double)BoostersUsed },
                { "slots", L2(Slot) }, { "hopper", L2(hopper) }, { "queue", q }, { "stations", st }, { "transfers", tr },
            };
        }

        /// <summary>Fingerprint of the level content, so a save is never restored onto regenerated data.</summary>
        public static string Signature(ConveyorLevel l)
        {
            uint h = 2166136261u;
            void Mix(int v) { unchecked { h = (h ^ (uint)v) * 16777619u; } }
            Mix(l.Slots); Mix(l.Speed); Mix(l.Feeder); Mix(l.Visible); Mix(l.Space);
            foreach (int c in l.Belt) Mix(c);
            foreach (int c in l.Hopper) Mix(c);
            foreach (var b in l.Start) { Mix(-7); if (b != null) { foreach (int c in b.Need) Mix(c); foreach (int c in b.Have) Mix(c); } }
            foreach (var b in l.Queue) { Mix(-9); foreach (int c in b.Need) Mix(c); foreach (int c in b.Have) Mix(c); }
            foreach (var st in l.Stations) { Mix(st.S); Mix((int)st.Lock); Mix(st.LockN); Mix(st.Capacity); }
            return h.ToString();
        }

        public static ConveyorSession FromSnapshot(ConveyorLevel level, Dictionary<string, object> d, bool recordEvents = true, bool enableUndo = true)
        {
            if (d == null || MiniJson.GetInt(d, "rules") != 4 || MiniJson.GetInt(d, "level") != level.Id) return null;
            if (MiniJson.GetString(d, "sig", "") != Signature(level)) return null;
            try
            {
                var s = new ConveyorSession(level, recordEvents, enableUndo);
                var slots = I(MiniJson.GetArr(d, "slots"));
                if (slots.Length != s.C) return null;
                Array.Copy(slots, s.Slot, s.C);
                var hop = I(MiniJson.GetArr(d, "hopper"));
                if (hop.Length == s.hopper.Length) Array.Copy(hop, s.hopper, hop.Length);
                s.Offset = MiniJson.GetInt(d, "offset"); s.Tick = MiniJson.GetInt(d, "tick"); s.HopperPos = MiniJson.GetInt(d, "hopperPos");
                s.Completed = MiniJson.GetInt(d, "completed"); s.Delivered = MiniJson.GetInt(d, "delivered"); s.Released = MiniJson.GetInt(d, "released");
                s.Gone = MiniJson.GetInt(d, "gone"); s.Placed = MiniJson.GetInt(d, "placed"); s.BoostersUsed = MiniJson.GetInt(d, "boosters");
                s.Queue.Clear();
                foreach (var o in MiniJson.GetArr(d, "queue")) s.Queue.Add(ReadBus(o));
                var st = MiniJson.GetArr(d, "stations");
                for (int i = 0; i < s.Stations.Length; i++)
                {
                    var o = MiniJson.Obj(st[i]);
                    s.Stations[i].Lock = (StationLock)MiniJson.GetInt(o, "lock");
                    s.Stations[i].Tray = ReadBus(MiniJson.GetObj(o, "bus"));
                    s.Stations[i].Out.Clear(); s.Stations[i].Out.AddRange(I(MiniJson.GetArr(o, "out")));
                }
                s.Transfers.Clear();
                foreach (var o in MiniJson.GetArr(d, "transfers"))
                {
                    var a = MiniJson.Arr(o);
                    s.Transfers.Add(new ConveyorTransfer { Station = (int)(double)a[0], Color = (int)(double)a[1], Compartment = (int)(double)a[2], T = (int)(double)a[3] });
                }
                if (!s.HasValidSupply()) return null; // nothing lost or duplicated
                return s;
            }
            catch (Exception) { return null; }
        }
    }
}
