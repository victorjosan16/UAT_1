using System;
using System.Collections.Generic;
using TollRoadTycoon.Core;
using TollRoadTycoon.Economy;
using TollRoadTycoon.TollSystem;

namespace TollRoadTycoon.Workers
{
    public enum AssignResult
    {
        Assigned,
        AlreadyAssigned,
        WorkerBusy,
        BoothOccupied,
        BoothAutomatic,
        Pending,
        Unassigned,
        NotAssigned,
    }

    /// <summary>Hiring, upgrading and assigning workers. A worker staffs at most one booth.</summary>
    public sealed class WorkerManager
    {
        private readonly WorkerConfig _config;
        private readonly EconomyManager _economy;
        private readonly List<Worker> _workers = new List<Worker>();
        private int _nextId = 1;

        public IReadOnlyList<Worker> Workers => _workers;
        public event Action Changed;

        public WorkerManager(WorkerConfig config, EconomyManager economy)
        {
            _config = config;
            _economy = economy;
        }

        public WorkerConfig Config => _config;

        /// <summary>The first worker is free; every hire after that costs more.</summary>
        public long HireCost => _config.HireCost.CostForLevel(Math.Max(0, _workers.Count - 1));

        public long UpgradeCost(Worker worker) => _config.UpgradeCost.CostForLevel(worker.Level - 1);

        public bool CanUpgrade(Worker worker) => worker.Level < _config.MaxLevel;

        public Worker FindFreeWorker()
        {
            foreach (var w in _workers)
            {
                if (w.AssignedBooth == null) return w;
            }
            return null;
        }

        public bool TryHire(out Worker worker)
        {
            worker = null;
            if (!_economy.TrySpendMoney(HireCost, "worker.hire")) return false;
            worker = AddWorker(1);
            return true;
        }

        public bool TryUpgrade(Worker worker)
        {
            if (worker == null || !CanUpgrade(worker)) return false;
            if (!_economy.TrySpendMoney(UpgradeCost(worker), "worker.upgrade")) return false;
            worker.Level++;
            worker.AssignedBooth?.NotifyChanged();
            Changed?.Invoke();
            return true;
        }

        public AssignResult Assign(Worker worker, TollBooth booth)
        {
            if (worker.AssignedBooth == booth) return AssignResult.AlreadyAssigned;
            if (worker.AssignedBooth != null) return AssignResult.WorkerBusy;
            if (booth.Mode == BoothMode.Automatic) return AssignResult.BoothAutomatic;
            if (booth.AssignedWorker != null) return AssignResult.BoothOccupied;
            worker.AssignedBooth = booth;
            booth.AssignedWorker = worker;
            booth.PendingClose = false;
            booth.NotifyChanged();
            Changed?.Invoke();
            return AssignResult.Assigned;
        }

        /// <summary>
        /// Takes the worker off their booth. If cars are still queued the booth first stops accepting
        /// new cars, serves the queue, and only then releases the worker (no stranded vehicles).
        /// </summary>
        public AssignResult Unassign(Worker worker)
        {
            var booth = worker.AssignedBooth;
            if (booth == null) return AssignResult.NotAssigned;
            if (booth.AssignedVehicles.Count > 0 || booth.CurrentVehicle != null)
            {
                booth.PendingClose = true;
                booth.NotifyChanged();
                Changed?.Invoke();
                return AssignResult.Pending;
            }
            Release(booth);
            return AssignResult.Unassigned;
        }

        /// <summary>Called when a booth that was pending close has drained its queue.</summary>
        internal void OnBoothClosed(TollBooth booth)
        {
            if (booth.PendingClose && booth.AssignedWorker != null) Release(booth);
        }

        /// <summary>Frees the worker of a booth that just became automatic.</summary>
        internal void ReleaseForAutomation(TollBooth booth)
        {
            if (booth.AssignedWorker != null) Release(booth);
        }

        internal Worker AddWorker(int level, int id = 0)
        {
            var worker = new Worker(id > 0 ? id : _nextId, level);
            _nextId = Math.Max(_nextId, worker.Id + 1);
            _workers.Add(worker);
            Changed?.Invoke();
            return worker;
        }

        internal int NextId => _nextId;

        private void Release(TollBooth booth)
        {
            var worker = booth.AssignedWorker;
            booth.AssignedWorker = null;
            booth.PendingClose = false;
            if (worker != null) worker.AssignedBooth = null;
            if (booth.State == BoothState.Idle || booth.State == BoothState.WaitingForVehicle) booth.State = BoothState.Closed;
            booth.NotifyChanged();
            Changed?.Invoke();
        }
    }
}
