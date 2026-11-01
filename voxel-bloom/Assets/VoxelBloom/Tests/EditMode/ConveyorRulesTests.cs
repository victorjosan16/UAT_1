using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using VoxelBloom.Core;

namespace VoxelBloom.Tests
{
    /// <summary>Conveyor rules v3 (mirror of tools/conveyor/test.mjs) and validation of every shipped level.</summary>
    public class ConveyorRulesTests
    {
        static ConveyorLevel Mk(int[] belt, int[][] queue, (int s, StationLock lk, int n)[] stations, int[] hopper = null, int visible = 7, int speed = 10)
        {
            var st = new ConveyorStationDef[stations.Length];
            for (int i = 0; i < st.Length; i++) st[i] = new ConveyorStationDef { S = stations[i].s, Capacity = 4, Lock = stations[i].lk, LockN = stations[i].n };
            var l = new ConveyorLevel
            {
                Id = 999, Slots = belt.Length, Speed = speed, Feeder = 50, Visible = visible, Colors = new[] { "#FF0000", "#00FF00", "#0000FF" },
                Stations = st, Queue = queue, Belt = belt, Hopper = hopper ?? new int[0], Witness = new int[0][],
            };
            l.Validate();
            return l;
        }

        static int[] Belt(params int[] firstSlots) { var b = new int[10]; for (int i = 0; i < 10; i++) b[i] = i < firstSlots.Length ? firstSlots[i] : -1; return b; }
        static void Run(ConveyorSession s, int n) { for (int i = 0; i < n; i++) s.Step(); }
        static (int, StationLock, int) Open(int s) => (s, StationLock.None, 0);

        [Test]
        public void Slots_KeepConstantSpacing_AroundTheLoop()
        {
            var s = new ConveyorSession(Mk(Belt(0), new[] { new[] { 0 } }, new[] { Open(450) }));
            for (int i = 0; i < 250; i++)
            {
                s.Step();
                for (int k = 0; k < s.C; k++) Assert.AreEqual(ConveyorSession.P, (s.SlotPos((k + 1) % s.C) - s.SlotPos(k) + s.L) % s.L);
            }
        }

        [Test]
        public void MatchingCube_Transfers_ThenLandsAfterTransferTicks()
        {
            var s = new ConveyorSession(Mk(Belt(0), new[] { new[] { 0 } }, new[] { Open(450) }));
            s.Place(0);
            int guard = 0;
            while (s.Transfers.Count == 0 && guard++ < 100) s.Step();
            Assert.AreEqual(-1, s.Slot[0], "cube left the belt");
            Run(s, ConveyorSession.Transfer - 1);
            Assert.AreEqual(0, s.Completed);
            s.Step();
            Assert.IsTrue(s.IsWon);
        }

        [Test]
        public void NonMatchingCube_KeepsCirculating()
        {
            var s = new ConveyorSession(Mk(Belt(1, 0), new[] { new[] { 0 }, new[] { 1 } }, new[] { Open(450) }));
            s.Place(0);
            Run(s, 300);
            Assert.AreEqual(1, s.Slot[0], "green stays on the belt");
            Assert.AreEqual(1, s.Completed);
        }

        [Test]
        public void OneCube_IsReservedByOneStationOnly_AndDeliveredOnce()
        {
            var s = new ConveyorSession(Mk(new[] { 0, -1, -1, -1, -1, -1, -1, -1, -1, 0 }, new[] { new[] { 0 }, new[] { 0 } }, new[] { Open(300), Open(600) }));
            s.Place(0); s.Place(0);
            Run(s, 300);
            Assert.IsTrue(s.IsWon);
            Assert.AreEqual(2, s.Delivered);
        }

        [Test]
        public void DoubleTap_WithOneFreeStation_PlacesOneTray()
        {
            var s = new ConveyorSession(Mk(Belt(0, 0), new[] { new[] { 0 }, new[] { 0 } }, new[] { Open(450) }));
            Assert.GreaterOrEqual(s.Place(0), 0);
            Assert.AreEqual(-1, s.Place(0));
            Assert.AreEqual(1, s.Queue.Count, "nothing consumed");
        }

        [Test]
        public void Locks_NumberedOpensAfterCompletions_PlusOpensWithBooster()
        {
            var s = new ConveyorSession(Mk(Belt(0, 0), new[] { new[] { 0 }, new[] { 0 } },
                new[] { Open(300), (600, StationLock.Completed, 1), (800, StationLock.Booster, 0) }));
            s.Place(0);
            Assert.AreEqual(-1, s.FreeStation(1));
            int guard = 0; while (s.Completed < 1 && guard++ < 400) s.Step();
            Assert.AreEqual(StationLock.None, s.Stations[1].Lock);
            Assert.IsTrue(s.ExtraStation());
            Assert.IsFalse(s.ExtraStation(), "nothing left: booster not consumed");
        }

        [Test]
        public void Deadlock_BeltFullOfUnneededCubes()
        {
            var s = new ConveyorSession(Mk(new[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }, new[] { new[] { 0 }, new[] { 1, 1, 1, 1 }, new[] { 1, 1, 1, 1 }, new[] { 1, 1 } },
                new[] { Open(450) }, new[] { 0 }, visible: 1));
            s.Place(0);
            Run(s, 50);
            Assert.IsTrue(s.IsStuck());
        }

        [Test]
        public void Undo_RestoresStateBeforeLastPlacement()
        {
            var s = new ConveyorSession(Mk(Belt(0, 1), new[] { new[] { 0 }, new[] { 1 } }, new[] { Open(450) }), false, true);
            var before = (int[])s.Slot.Clone();
            s.Place(0); Run(s, 50);
            Assert.IsTrue(s.UndoLast());
            CollectionAssert.AreEqual(before, s.Slot);
            Assert.AreEqual(2, s.Queue.Count);
            Assert.AreEqual(1, s.BoostersUsed);
            Assert.IsFalse(s.UndoLast());
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
        public void AllLevels_Validate_AndWitnessReplaysWin_WithIdenticalTickCount()
        {
            int n = 0;
            foreach (var l in AllLevels())
            {
                n++;
                var s = ConveyorSolver.Replay(l, l.Witness);
                Assert.IsTrue(s.IsWon, "level " + l.Id + " witness wins");
                Assert.AreEqual(l.WitnessTicks, s.Tick, "level " + l.Id + ": C# and JS rules agree tick for tick");
                Assert.AreEqual(l.TotalCubes, s.Delivered, "level " + l.Id + ": no cube lost or duplicated");
            }
            Assert.GreaterOrEqual(n, 20, "20 shipped levels");
        }

        [Test]
        public void Snapshot_RoundTrip_ContinuesIdentically()
        {
            ConveyorLevel l = null;
            foreach (var x in AllLevels()) if (x.Id == 6) { l = x; break; }
            Assert.IsNotNull(l);
            var a = new ConveyorSession(l);
            int w = 0;
            while (a.Tick < 900) { while (w < l.Witness.Length && l.Witness[w][0] == a.Tick) a.Place(l.Witness[w++][1]); a.Step(); }
            var b = ConveyorSession.FromSnapshot(l, MiniJson.Obj(MiniJson.Parse(MiniJson.Serialize(a.ToSnapshot()))), false, false);
            Assert.IsNotNull(b);
            while (!a.IsWon && a.Tick < ConveyorSession.MaxTicks)
            {
                while (w < l.Witness.Length && l.Witness[w][0] == a.Tick) { a.Place(l.Witness[w][1]); b.Place(l.Witness[w][1]); w++; }
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
    }
}
