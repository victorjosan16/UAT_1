using NUnit.Framework;
using TollRoadTycoon.Economy;
using TollRoadTycoon.TollSystem;
using TollRoadTycoon.Workers;

namespace TollRoadTycoon.Tests
{
    public class WorkerTests
    {
        [Test]
        public void StarterBoothIsStaffed()
        {
            var sim = TestUtil.NewSim();
            Assert.AreEqual(1, sim.Workers.Workers.Count);
            Assert.AreSame(sim.Workers.Workers[0], sim.Booths[0].AssignedWorker);
            Assert.AreEqual(3.0, sim.Booths[0].ProcessingSeconds, 1e-9);
        }

        [Test]
        public void WorkerCannotStaffTwoBooths()
        {
            var sim = TestUtil.NewSim();
            TestUtil.GiveMoney(sim, Money.FromDollars(10_000));
            var second = sim.Expansion.TryBuyBooth();
            var worker = sim.Workers.Workers[0];
            Assert.AreEqual(AssignResult.WorkerBusy, sim.Workers.Assign(worker, second));
            Assert.IsNull(second.AssignedWorker);
            Assert.AreEqual(BoothState.Closed, second.State);

            Assert.IsTrue(sim.Workers.TryHire(out var hired));
            Assert.AreEqual(AssignResult.Assigned, sim.Workers.Assign(hired, second));
            Assert.IsTrue(sim.Workers.TryHire(out var third));
            Assert.AreEqual(AssignResult.BoothOccupied, sim.Workers.Assign(third, second));
        }

        [Test]
        public void HiringCostsMoneyAndIsRejectedWhenBroke()
        {
            var sim = TestUtil.NewSim();
            sim.Economy.TrySpendMoney(sim.Economy.Balance, "drain");
            Assert.IsFalse(sim.Workers.TryHire(out _));
            Assert.AreEqual(1, sim.Workers.Workers.Count);
            TestUtil.GiveMoney(sim, sim.Workers.HireCost);
            Assert.IsTrue(sim.Workers.TryHire(out _));
            Assert.AreEqual(0, sim.Economy.Balance);
        }

        [Test]
        public void UpgradingWorkerSpeedsUpBoothAndThroughput()
        {
            var slow = TestUtil.NewSim(9);
            var fast = TestUtil.NewSim(9);
            TestUtil.GiveMoney(fast, Money.FromDollars(100_000));
            var worker = fast.Workers.Workers[0];
            for (int i = 0; i < 4; i++) Assert.IsTrue(fast.Workers.TryUpgrade(worker));
            Assert.AreEqual(5, worker.Level);
            Assert.AreEqual(1.8, fast.Booths[0].ProcessingSeconds, 1e-9);

            slow.RunFor(300);
            fast.RunFor(300);
            Assert.Greater(fast.Stats.VehiclesProcessed, slow.Stats.VehiclesProcessed * 1.25);
        }

        [Test]
        public void UnassigningWithQueueDrainsBeforeClosing()
        {
            var sim = TestUtil.NewSim(3);
            sim.RunFor(60);
            var booth = sim.Booths[0];
            var worker = booth.AssignedWorker;
            Assert.Greater(booth.AssignedVehicles.Count, 0, "test needs a queue");

            Assert.AreEqual(AssignResult.Pending, sim.Workers.Unassign(worker));
            Assert.IsTrue(booth.PendingClose);
            Assert.AreSame(worker, booth.AssignedWorker);

            long servedBefore = booth.VehiclesServed;
            int queued = booth.AssignedVehicles.Count;
            sim.RunFor(90);
            Assert.AreEqual(0, booth.AssignedVehicles.Count);
            Assert.AreEqual(servedBefore + queued, booth.VehiclesServed, "every queued car was served");
            Assert.AreEqual(BoothState.Closed, booth.State);
            Assert.IsNull(worker.AssignedBooth);
            Assert.IsNull(booth.AssignedWorker);

            // Reassigning reopens the booth.
            Assert.AreEqual(AssignResult.Assigned, sim.Workers.Assign(worker, booth));
            sim.RunFor(60);
            Assert.Greater(booth.VehiclesServed, servedBefore + queued);
        }

        [Test]
        public void ClosedBoothTrafficReroutesToOpenBooth()
        {
            var sim = TestUtil.NewSim(3);
            TestUtil.GiveMoney(sim, Money.FromDollars(10_000));
            var second = sim.Expansion.TryBuyBooth();
            sim.Workers.TryHire(out var w);
            sim.Workers.Assign(w, second);
            sim.RunFor(60);
            sim.Workers.Unassign(sim.Booths[0].AssignedWorker);
            sim.RunFor(60);
            long servedSecond = second.VehiclesServed;
            long servedFirst = sim.Booths[0].VehiclesServed;
            sim.RunFor(120);
            Assert.AreEqual(servedFirst, sim.Booths[0].VehiclesServed);
            Assert.Greater(second.VehiclesServed, servedSecond + 10);
        }
    }
}
