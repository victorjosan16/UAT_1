using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using VoxelBloom.Core;

namespace VoxelBloom.Tests
{
    /// <summary>Conveyor BUS rules v4 (mirror of tools/conveyor/test.mjs) and validation of every shipped level.</summary>
    public class ConveyorRulesTests
    {
        const int E = ConveyorSession.Empty;

        static BusDef B(int[] need, int[] have) => new BusDef { Need = need, Have = have };

        /// <summary>Tiny level: 10 slots (L = 1000), speed 10; stations (s, start BUS or null, lock, lockN).</summary>
        static ConveyorLevel Mk(int[] belt, (int s, BusDef bus, StationLock lk, int n)[] stations, BusDef[] queue = null, int space = 8)
        {
            var st = new ConveyorStationDef[stations.Length];
            var start = new BusDef[stations.Length];
            for (int i = 0; i < st.Length; i++) { st[i] = new ConveyorStationDef { S = stations[i].s, Capacity = 4, Lock = stations[i].lk, LockN = stations[i].n }; start[i] = stations[i].bus; }
            var l = new ConveyorLevel
            {
                Id = 999, Slots = belt.Length, Speed = 10, Feeder = 50, Visible = 7, Space = space, Colors = new[] { "#FF0000", "#00FF00", "#0000FF" },
                Stations = st, Start = start, Queue = queue ?? new BusDef[0], Belt = belt, Hopper = new int[0], Witness = new int[0][],
            };
            l.Validate();
            return l;
        }

        static int[] Belt(params int[] first) { var b = new int[10]; for (int i = 0; i < 10; i++) b[i] = i < first.Length ? first[i] : -1; return b; }
        static void Run(ConveyorSession s, int n) { for (int i = 0; i < n; i++) s.Step(); }
        static (int, BusDef, StationLock, int) At(int s, BusDef b) => (s, b, StationLock.None, 0);

        [Test]
        public void Belt_MovesClockwise_ConstantSpacing_ThroughAllCorners()
        {
            var s = new ConveyorSession(Mk(Belt(0), new[] { At(450, B(new[] { 1 }, new[] { 2 })) }));
            int last = s.SlotPos(0), travelled = 0;
            for (int i = 0; i < 250; i++)
            {
                s.Step();
                for (int k = 0; k < s.C; k++) Assert.AreEqual(ConveyorSession.P, (s.SlotPos((k + 1) % s.C) - s.SlotPos(k) + s.L) % s.L);
                travelled += (s.SlotPos(0) - last + s.L) % s.L; last = s.SlotPos(0);
            }
            Assert.AreEqual(2500, travelled, "constant speed, 2.5 laps");
            Assert.AreEqual(0, s.Slot[0], "no overtaking");
        }

        [Test]
        public void Release_Then_Collect_ThroughTheBelt()
        {
            var s = new ConveyorSession(Mk(Belt(), new[] { At(450, B(new[] { 0, 1 }, new[] { 0, 2 })), At(800, B(new[] { 2, 1 }, new[] { E, 1 })) }));
            Assert.IsTrue(s.Release(0));
            Assert.AreEqual(E, s.Stations[0].Tray.Have[1]);
            Assert.AreEqual(1, s.Occupied);
            int guard = 0; while (s.BeltCount == 0 && guard++ < 20) s.Step();
            Assert.AreEqual(1, s.BeltCount, "entered the belt");
            guard = 0; while (s.Transfers.Count == 0 && guard++ < 200) s.Step();
            Assert.AreEqual(ConveyorSession.Incoming, s.Stations[1].Tray.Have[0]);
            Run(s, ConveyorSession.Transfer);
            Assert.AreEqual(1, s.Completed);
        }

        [Test]
        public void IncompatibleColour_KeepsCirculating()
        {
            var s = new ConveyorSession(Mk(Belt(1), new[] { At(450, B(new[] { 0, 0 }, new[] { 0, E })), At(800, B(new[] { 1 }, new[] { 2 })) }));
            Run(s, 300);
            Assert.AreEqual(1, s.Slot[0]);
            Assert.AreEqual(0, s.Transfers.Count);
        }

        [Test]
        public void SwapThroughBelt_NoDirectBusToBus()
        {
            var s = new ConveyorSession(Mk(Belt(), new[] { At(450, B(new[] { 0 }, new[] { 1 })), At(660, B(new[] { 1 }, new[] { 0 })) }), true);
            s.Release(0); s.Release(1);
            int drop = -1, take = -1;
            for (int i = 0; i < 400; i++)
            {
                s.Step();
                foreach (var e in s.Events) { if (e.Type == ConveyorEventType.Drop && drop < 0) drop = s.Tick; if (e.Type == ConveyorEventType.Take && take < 0) take = s.Tick; }
                s.ClearEvents();
            }
            Assert.IsTrue(drop > 0 && take > drop, "drop before take");
            Assert.IsTrue(s.IsWon);
        }

        [Test]
        public void OneCube_CollectedOnce()
        {
            var s = new ConveyorSession(Mk(Belt(0), new[] { At(300, B(new[] { 0, 1 }, new[] { E, 1 })), At(600, B(new[] { 0, 2 }, new[] { E, 2 })) }));
            Run(s, 60);
            Assert.AreEqual(E, s.Stations[1].Tray.Have[0], "second BUS still empty");
        }

        [Test]
        public void Space_SevenOfEight_Releases_EightOfEight_Blocks()
        {
            var s = new ConveyorSession(Mk(new[] { 0, 0, 0, 0, 0, 0, 0, -1, -1, -1 },
                new[] { At(450, B(new[] { 1, 1 }, new[] { 2, 2 })), At(800, B(new[] { 2, 2, 0 }, new[] { E, 1, 1 })) }, null, 8));
            Assert.AreEqual(7, s.Occupied);
            Assert.IsTrue(s.Release(0));
            Assert.AreEqual(8, s.Occupied);
            Assert.IsFalse(s.Release(0), "8/8 blocks further releases");
            Assert.IsFalse(s.IsWon, "8/8 is not a win");
        }

        [Test]
        public void CompleteBus_FreesStation_DoubleTapPlacesOnce()
        {
            var s = new ConveyorSession(Mk(Belt(1), new[] { At(450, B(new[] { 1 }, new[] { E })) },
                new[] { B(new[] { 0 }, new[] { 2 }), B(new[] { 2 }, new[] { 0 }) }));
            Assert.AreEqual(-1, s.Place(0));
            int guard = 0; while (s.Completed < 1 && guard++ < 400) s.Step();
            Run(s, ConveyorSession.LeaveTicks);
            Assert.GreaterOrEqual(s.Place(0), 0);
            Assert.AreEqual(-1, s.Place(0));
            Assert.AreEqual(1, s.Queue.Count);
        }

        [Test]
        public void Undo_RestoresStateBeforeLastAction()
        {
            var s = new ConveyorSession(Mk(Belt(), new[] { At(450, B(new[] { 0, 1 }, new[] { 0, 2 })), At(800, B(new[] { 2, 1 }, new[] { E, 1 })) }), false, true);
            var before = (int[])s.Slot.Clone();
            s.Release(0); Run(s, 30);
            Assert.IsTrue(s.UndoLast());
            CollectionAssert.AreEqual(before, s.Slot);
            Assert.AreEqual(2, s.Stations[0].Tray.Have[1]);
        }

        static IEnumerable<ConveyorLevel> AllLevels()
        {
            for (int id = 1; ; id++)
            {
                var ta = Resources.Load<TextAsset>("VoxelBloom/Conveyor/level_" + id.ToString("000"));
                if (ta == null) yield break;
                yield return ConveyorLevel.FromJson(MiniJson.Obj(MiniJson.Parse(ta.text)));
            }
        }

        [Test]
        public void AllLevels_WitnessWins_SameTicksAsJs_NoCubeLostOrDuplicated()
        {
            int n = 0;
            foreach (var l in AllLevels())
            {
                n++;
                var s = new ConveyorSession(l);
                int w = 0;
                while (!s.IsWon && s.Tick < ConveyorSession.MaxTicks)
                {
                    while (w < l.Witness.Length && l.Witness[w][0] == s.Tick) Assert.IsTrue(s.Act(l.Witness[w++]), "level " + l.Id + " action");
                    s.Step();
                    if (s.Tick % 30 == 0) Assert.AreEqual(l.TotalCubes, s.CubeCount(), "level " + l.Id + " cubes at tick " + s.Tick);
                }
                Assert.IsTrue(s.IsWon, "level " + l.Id + " witness wins");
                Assert.AreEqual(l.WitnessTicks, s.Tick, "level " + l.Id + ": C# and JS rules agree tick for tick");
            }
            Assert.GreaterOrEqual(n, 50, "50 shipped levels");
        }

        [Test]
        public void Snapshot_RoundTrip_ContinuesIdentically()
        {
            ConveyorLevel l = null;
            foreach (var x in AllLevels()) if (x.Id == 6) { l = x; break; }
            var a = new ConveyorSession(l);
            int w = 0;
            while (a.Tick < 600) { while (w < l.Witness.Length && l.Witness[w][0] == a.Tick) a.Act(l.Witness[w++]); a.Step(); }
            var b = ConveyorSession.FromSnapshot(l, MiniJson.Obj(MiniJson.Parse(MiniJson.Serialize(a.ToSnapshot()))), false, false);
            Assert.IsNotNull(b);
            while (!a.IsWon && a.Tick < ConveyorSession.MaxTicks)
            {
                while (w < l.Witness.Length && l.Witness[w][0] == a.Tick) { a.Act(l.Witness[w]); b.Act(l.Witness[w]); w++; }
                a.Step(); b.Step();
            }
            Assert.IsTrue(a.IsWon && b.IsWon);
            Assert.AreEqual(a.Tick, b.Tick);
        }

        [Test]
        public void Snapshot_FromAnotherLevelVersion_IsRejected()
        {
            ConveyorLevel l = null;
            foreach (var x in AllLevels()) { l = x; break; }
            var snap = MiniJson.Obj(MiniJson.Parse(MiniJson.Serialize(new ConveyorSession(l).ToSnapshot())));
            snap["sig"] = "123";
            Assert.IsNull(ConveyorSession.FromSnapshot(l, snap));
        }

        [Test]
        public void ShuffleColors_KeepsEveryCube()
        {
            ConveyorLevel l = null;
            foreach (var x in AllLevels()) if (x.Id == 11) { l = x; break; }
            var s = new ConveyorSession(l);
            int w = 0;
            while (s.Tick < 600) { while (w < l.Witness.Length && l.Witness[w][0] == s.Tick) s.Act(l.Witness[w++]); s.Step(); }
            int belt = s.BeltCount;
            s.ShuffleColors(new DeterministicRandom(5));
            Assert.AreEqual(belt, s.BeltCount);
            Assert.IsTrue(s.HasValidSupply());
        }
    }
}
