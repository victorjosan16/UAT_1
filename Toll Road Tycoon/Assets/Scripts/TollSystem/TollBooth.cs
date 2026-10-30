using System;
using System.Collections.Generic;
using TollRoadTycoon.Core;
using TollRoadTycoon.Traffic;
using TollRoadTycoon.Workers;

namespace TollRoadTycoon.TollSystem
{
    public enum BoothState
    {
        Closed,
        Idle,
        WaitingForVehicle,
        ProcessingPayment,
        OpeningBarrier,
        VehiclePassing,
        ClosingBarrier,
    }

    public enum BoothMode
    {
        Manual,
        Automatic,
    }

    /// <summary>Data and derived stats of one toll booth. Behaviour lives in TollBoothController.</summary>
    public sealed class TollBooth
    {
        private readonly BoothUpgradeConfig _upgrades;
        private readonly WorkerConfig _workers;

        public int Id => SlotIndex;
        public int SlotIndex { get; }
        public PathSegment Lane { get; }
        public BoothMode Mode { get; internal set; }
        public Worker AssignedWorker { get; internal set; }
        public BoothState State { get; internal set; }

        public int SpeedLevel { get; internal set; }
        public int TollLevel { get; internal set; }
        public int CapacityLevel { get; internal set; }
        public int BarrierLevel { get; internal set; }

        /// <summary>When set the booth accepts no new vehicles, serves the ones assigned, then closes.</summary>
        public bool PendingClose { get; internal set; }

        /// <summary>0 = barrier down, 1 = fully open.</summary>
        public double BarrierOpen { get; internal set; }

        public Vehicle CurrentVehicle { get; internal set; }
        internal double Timer;

        /// <summary>Vehicles that chose this booth and have not yet passed its barrier.</summary>
        public readonly List<Vehicle> AssignedVehicles = new List<Vehicle>();

        public double BusySeconds { get; internal set; }
        public long VehiclesServed { get; internal set; }

        public event Action<TollBooth> Changed;

        public TollBooth(int slotIndex, PathSegment lane, BoothUpgradeConfig upgrades, WorkerConfig workers)
        {
            SlotIndex = slotIndex;
            Lane = lane;
            _upgrades = upgrades;
            _workers = workers;
            State = BoothState.Closed;
        }

        public int Level => 1 + SpeedLevel + TollLevel + CapacityLevel + BarrierLevel + (Mode == BoothMode.Automatic ? 1 : 0);

        public bool IsStaffed => Mode == BoothMode.Automatic || AssignedWorker != null;

        public bool IsAcceptingVehicles => IsStaffed && !PendingClose;

        public int QueueCapacity => _upgrades.BaseQueueCapacity + CapacityLevel;

        public long TollMultiplierBasisPoints => 10000 + _upgrades.TollBasisPointsPerLevel * TollLevel;

        public double SpeedMultiplier => 1.0 / (1.0 + _upgrades.SpeedBonusPerLevel * SpeedLevel);

        public double BarrierSeconds => _upgrades.BaseBarrierSeconds / (1.0 + _upgrades.BarrierBonusPerLevel * BarrierLevel);

        /// <summary>Seconds needed to take payment from a standard vehicle, or +inf when unstaffed.</summary>
        public double ProcessingSeconds
        {
            get
            {
                double baseSeconds;
                if (Mode == BoothMode.Automatic) baseSeconds = _upgrades.AutomaticProcessingSeconds;
                else if (AssignedWorker != null) baseSeconds = AssignedWorker.GetProcessingSeconds(_workers);
                else return double.PositiveInfinity;
                return baseSeconds * SpeedMultiplier;
            }
        }

        /// <summary>Approximate seconds per standard vehicle including both barrier movements.</summary>
        public double CycleSeconds => ProcessingSeconds + BarrierSeconds * 2 + 0.6;

        internal void NotifyChanged() => Changed?.Invoke(this);
    }
}
