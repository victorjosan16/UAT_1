using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using TollRoadTycoon.Core;
using TollRoadTycoon.TollSystem;
using TollRoadTycoon.Traffic;

namespace TollRoadTycoon.Tests
{
    public class TrafficTests
    {
        private static GameSimulation QuietSim()
        {
            return TestUtil.NewSim(1, c =>
            {
                c.Traffic.BaseArrivalsPerSecond = 0;
                c.Traffic.OncomingArrivalsPerSecond = 0;
            });
        }

        private static GameSimulation BuildPlaza(int lanes, int booths, bool unlockAll, int seed = 3)
        {
            var sim = TestUtil.NewSim(seed);
            TestUtil.GiveMoney(sim, Economy.Money.FromDollars(10_000_000));
            while (sim.Expansion.LaneCount < lanes) Assert.IsTrue(sim.Expansion.TryBuyLane());
            while (sim.Booths.Count < booths)
            {
                var booth = sim.Expansion.TryBuyBooth();
                Assert.IsNotNull(booth);
                if (booth.AssignedWorker == null)
                {
                    Assert.IsTrue(sim.Workers.TryHire(out var w));
                    sim.Workers.Assign(w, booth);
                }
            }
            if (unlockAll)
            {
                foreach (var t in sim.Config.VehicleTypes) sim.Upgrades.TryUnlockVehicle(t.Id);
            }
            return sim;
        }

        [Test]
        public void SingleCarGoesThroughEveryStateAndPaysOnce()
        {
            var sim = QuietSim();
            var booth = sim.Booths[0];
            var compact = sim.Config.FindVehicleType("compact");
            long before = sim.Economy.Balance;
            var car = sim.Traffic.Spawn(compact, sim.Lanes.Approach(0), true, 0, sim.Time);
            long trip = car.TripId;

            var vehicleStates = new List<VehicleState> { car.State };
            var boothStates = new List<BoothState> { booth.State };
            for (int i = 0; i < 30 * 60 && car.TripId == trip; i++)
            {
                sim.Step(1.0 / 30);
                if (car.TripId == trip && vehicleStates[vehicleStates.Count - 1] != car.State) vehicleStates.Add(car.State);
                if (boothStates[boothStates.Count - 1] != booth.State) boothStates.Add(booth.State);
            }

            Assert.AreNotEqual(trip, car.TripId, "car should have despawned into the pool");
            CollectionAssert.IsSubsetOf(new[]
            {
                VehicleState.Spawning, VehicleState.Driving, VehicleState.ApproachingQueue, VehicleState.ApproachingBooth,
                VehicleState.Paying, VehicleState.WaitingForBarrier, VehicleState.PassingBarrier, VehicleState.Exiting,
            }, vehicleStates);
            AssertInOrder(vehicleStates, VehicleState.ApproachingBooth, VehicleState.Paying, VehicleState.WaitingForBarrier,
                VehicleState.PassingBarrier, VehicleState.Exiting);
            AssertInOrder(boothStates, BoothState.ProcessingPayment, BoothState.OpeningBarrier, BoothState.VehiclePassing,
                BoothState.ClosingBarrier, BoothState.Idle);
            Assert.AreEqual(Economy.Money.FromDollars(5), sim.Economy.Balance - before);
            Assert.AreEqual(1, sim.Stats.VehiclesProcessed);
            Assert.AreEqual(0, booth.AssignedVehicles.Count);
            Assert.AreEqual(0, booth.BarrierOpen);
        }

        private static void AssertInOrder<T>(List<T> seen, params T[] expected)
        {
            int index = 0;
            foreach (var s in seen)
            {
                if (index < expected.Length && Equals(s, expected[index])) index++;
            }
            Assert.AreEqual(expected.Length, index, "states out of order: " + string.Join(" > ", seen));
        }

        [Test]
        public void MoneyIsEarnedOnPaymentNotOnSpawn()
        {
            var sim = QuietSim();
            long before = sim.Economy.Balance;
            sim.Traffic.Spawn(sim.Config.FindVehicleType("sedan"), sim.Lanes.Approach(0), true, 0, sim.Time);
            sim.RunFor(1);
            Assert.AreEqual(before, sim.Economy.Balance);
        }

        [Test]
        public void SpawnRefusesWhenEntryIsOccupied()
        {
            var sim = QuietSim();
            var type = sim.Config.FindVehicleType("compact");
            Assert.IsNotNull(sim.Traffic.Spawn(type, sim.Lanes.Approach(0), true, 0, 0));
            Assert.IsNull(sim.Traffic.Spawn(type, sim.Lanes.Approach(0), true, 0, 0));
        }

        [TestCase(1, 1)]
        [TestCase(1, 2)]
        [TestCase(2, 4)]
        [TestCase(3, 6)]
        public void VehiclesNeverOverlapAndBoothsAllServe(int lanes, int booths)
        {
            var sim = BuildPlaza(lanes, booths, unlockAll: true, seed: lanes * 10 + booths);
            for (int i = 0; i < 30 * 300; i++)
            {
                sim.Step(1.0 / 30);
                string overlap = TestUtil.FindOverlap(sim.Traffic.ActiveVehicles);
                Assert.IsNull(overlap, $"t={sim.Time:0.00}: {overlap}");
            }
            foreach (var booth in sim.Booths)
            {
                Assert.Greater(booth.VehiclesServed, 20, $"booth {booth.SlotIndex} served too few vehicles");
            }
        }

        [TestCase(1, 3, 6)]
        [TestCase(2, 3, 5)]
        [TestCase(3, 2, 4)]
        public void DenseTrafficWithLongVehiclesNeverOverlaps(int seed, int lanes, int booths)
        {
            // Busiest road (section 5) with every vehicle type, including buses and heavy trucks.
            var config = GameConfig.CreateDefault();
            var sim = new GameSimulation(config.ForSection(config.Progression.Sections[4]), seed);
            TestUtil.GiveMoney(sim, Economy.Money.FromDollars(10_000_000_000));
            while (sim.Lanes.ActiveLaneCount < lanes) Assert.IsTrue(sim.Expansion.TryBuyLane());
            while (sim.Booths.Count < booths)
            {
                var b = sim.Expansion.TryBuyBooth();
                if (b.AssignedWorker == null && sim.Workers.TryHire(out var w)) sim.Workers.Assign(w, b);
            }
            foreach (var t in config.VehicleTypes) sim.Upgrades.TryUnlockVehicle(t.Id);
            for (int i = 0; i < 30 * 300; i++)
            {
                sim.Step(1.0 / 30);
                string overlap = TestUtil.FindOverlap(sim.Traffic.ActiveVehicles);
                Assert.IsNull(overlap, $"t={sim.Time:0.00}: {overlap}");
            }
            Assert.Greater(sim.Stats.HeavyVehiclesProcessed, 10);
        }

        [Test]
        public void ThroughputGrowsWithBooths()
        {
            long Processed(int lanes, int booths)
            {
                var sim = BuildPlaza(lanes, booths, unlockAll: false);
                sim.RunFor(300);
                return sim.Stats.VehiclesProcessed;
            }

            long one = Processed(1, 1);
            long two = Processed(1, 2);
            long four = Processed(2, 4);
            Assert.Greater(two, one * 1.7);
            Assert.Greater(four, two * 1.7);
        }

        [Test]
        public void ArrivalsUseEveryLane()
        {
            var sim = BuildPlaza(3, 6, unlockAll: false);
            var perLane = new int[3];
            sim.Traffic.VehicleSpawned += v =>
            {
                if (v.IsTolled) perLane[v.EntryLane]++;
            };
            sim.RunFor(120);
            foreach (int count in perLane) Assert.Greater(count, 10);
        }

        [Test]
        public void QueueNeverExceedsCapacity()
        {
            var sim = BuildPlaza(1, 1, unlockAll: true);
            for (int i = 0; i < 30 * 200; i++)
            {
                sim.Step(1.0 / 30);
                Assert.LessOrEqual(sim.Booths[0].AssignedVehicles.Count, sim.Booths[0].QueueCapacity);
            }
        }

        [TestCase(20)]
        [TestCase(50)]
        [TestCase(100)]
        [TestCase(200)]
        public void StressManyVehicles(int target)
        {
            // Long approach lanes so the road can physically hold the requested number of vehicles.
            var sim = TestUtil.NewSim(5, c =>
            {
                c.Layout.SpawnY = -14 - 18 - target * 0.6;
                c.Layout.DespawnY = 40;
                c.Traffic.BaseArrivalsPerSecond = 6;
                c.Traffic.MaxPendingArrivals = 6;
            });
            TestUtil.GiveMoney(sim, Economy.Money.FromDollars(1_000_000));
            sim.Expansion.TryBuyLane();
            sim.Expansion.TryBuyLane();

            int peak = 0;
            var watch = Stopwatch.StartNew();
            int steps = 0;
            for (; steps < 30 * 240 && peak < target; steps++)
            {
                sim.Step(1.0 / 30);
                peak = System.Math.Max(peak, sim.Traffic.ActiveVehicles.Count);
            }
            Assert.GreaterOrEqual(peak, target, "could not reach target vehicle count");

            // Time 300 steps at the peak load.
            watch.Restart();
            for (int i = 0; i < 300; i++)
            {
                sim.Step(1.0 / 30);
                Assert.IsNull(TestUtil.FindOverlap(sim.Traffic.ActiveVehicles));
            }
            watch.Stop();
            double collisionCheckIncluded = watch.Elapsed.TotalMilliseconds / 300;

            watch.Restart();
            for (int i = 0; i < 300; i++) sim.Step(1.0 / 30);
            watch.Stop();
            double msPerStep = watch.Elapsed.TotalMilliseconds / 300;
            TestContext.WriteLine($"{sim.Traffic.ActiveVehicles.Count} vehicles: {msPerStep:0.000} ms per simulation step " +
                                  $"({collisionCheckIncluded:0.000} ms incl. O(n^2) overlap check), pool size {sim.Traffic.Pool.CreatedCount}");
            Assert.Less(msPerStep, 4.0, "simulation step too slow");
        }

        [Test]
        public void VehiclesAreRecycledThroughThePool()
        {
            var sim = BuildPlaza(1, 2, unlockAll: false);
            sim.RunFor(600);
            Assert.Greater(sim.Stats.VehiclesSpawned, sim.Traffic.Pool.CreatedCount * 2);
        }
    }
}
