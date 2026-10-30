using NUnit.Framework;
using TollRoadTycoon.Economy;
using TollRoadTycoon.RoadExpansion;
using TollRoadTycoon.Traffic;

namespace TollRoadTycoon.Tests
{
    public class ExpansionTests
    {
        [Test]
        public void BuyingBoothAddsRealGeometryAndRouting()
        {
            var sim = TestUtil.NewSim(6);
            TestUtil.GiveMoney(sim, Money.FromDollars(10_000));
            int segmentsBefore = sim.Lanes.AllSegments.Count;
            var booth = sim.Expansion.TryBuyBooth();
            Assert.IsNotNull(booth);
            Assert.IsNotNull(sim.Lanes.BoothLane(1));
            Assert.IsNotNull(sim.Lanes.EntryConnector(0, 1));
            Assert.IsNotNull(sim.Lanes.ExitConnector(1, 0));
            Assert.Greater(sim.Lanes.AllSegments.Count, segmentsBefore);

            sim.Workers.TryHire(out var w);
            sim.Workers.Assign(w, booth);
            sim.RunFor(180);
            Assert.Greater(sim.Booths[0].VehiclesServed, 10);
            Assert.Greater(booth.VehiclesServed, 10);
        }

        [Test]
        public void BoothSlotsRequireLanes()
        {
            var sim = TestUtil.NewSim();
            TestUtil.GiveMoney(sim, Money.FromDollars(1_000_000));
            Assert.IsNotNull(sim.Expansion.TryBuyBooth()); // booth 2 on one lane
            Assert.AreEqual(ExpansionBlock.NeedsMoreLanes, sim.Expansion.CanBuyBooth());
            Assert.IsNull(sim.Expansion.TryBuyBooth());
            Assert.IsTrue(sim.Expansion.TryBuyLane());
            Assert.AreEqual(2, sim.Lanes.ActiveLaneCount);
            Assert.IsNotNull(sim.Lanes.Approach(1));
            Assert.IsNotNull(sim.Lanes.EntryConnector(1, 0));
            Assert.IsNotNull(sim.Expansion.TryBuyBooth());
            Assert.IsNotNull(sim.Expansion.TryBuyBooth());
            Assert.AreEqual(ExpansionBlock.NeedsMoreLanes, sim.Expansion.CanBuyBooth());
            Assert.IsTrue(sim.Expansion.TryBuyLane());
            Assert.IsNotNull(sim.Expansion.TryBuyBooth());
            Assert.IsNotNull(sim.Expansion.TryBuyBooth());
            Assert.AreEqual(6, sim.Booths.Count);
            Assert.AreEqual(ExpansionBlock.MaxReached, sim.Expansion.CanBuyBooth());
            Assert.AreEqual(ExpansionBlock.MaxReached, sim.Expansion.CanBuyLane());
        }

        [Test]
        public void ExpansionIsChargedOnce()
        {
            var sim = TestUtil.NewSim();
            TestUtil.GiveMoney(sim, Money.FromDollars(150));
            long balance = sim.Economy.Balance;
            long price = sim.Expansion.NextBoothCost;
            Assert.IsNotNull(sim.Expansion.TryBuyBooth());
            Assert.AreEqual(balance - price, sim.Economy.Balance);
            Assert.AreEqual(ExpansionBlock.NeedsMoreLanes, sim.Expansion.CanBuyBooth());
        }

        [Test]
        public void NewLaneCarriesTraffic()
        {
            var sim = TestUtil.NewSim(12);
            TestUtil.GiveMoney(sim, Money.FromDollars(100_000));
            sim.Expansion.TryBuyLane();
            int onLane1 = 0;
            sim.Traffic.VehicleSpawned += v =>
            {
                if (v.EntryLane == 1) onLane1++;
            };
            int exitsOnLane1 = 0;
            sim.Traffic.VehicleDespawned += v =>
            {
                if (v.Segment != null && v.Segment.Kind == SegmentKind.Exit && v.Segment.LaneIndex == 1) exitsOnLane1++;
            };
            sim.Expansion.TryBuyBooth();
            sim.Workers.TryHire(out var w);
            sim.Workers.Assign(w, sim.Booths[1]);
            sim.Expansion.TryBuyBooth();
            sim.Workers.TryHire(out var w2);
            sim.Workers.Assign(w2, sim.Booths[2]);
            sim.RunFor(200);
            Assert.Greater(onLane1, 10);
            Assert.Greater(exitsOnLane1, 5);
        }
    }
}
