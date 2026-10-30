using System;
using System.Collections.Generic;
using TollRoadTycoon.Core;
using TollRoadTycoon.TollSystem;

namespace TollRoadTycoon.Traffic
{
    /// <summary>Chooses a booth for each arriving vehicle by expected wait, capacity and physical space.</summary>
    public sealed class QueueManager
    {
        private readonly LaneManager _lanes;
        private readonly TrafficConfig _traffic;
        private readonly IReadOnlyList<TollBooth> _booths;
        private readonly SimRandom _random;

        public QueueManager(LaneManager lanes, TrafficConfig traffic, IReadOnlyList<TollBooth> booths, SimRandom random)
        {
            _lanes = lanes;
            _traffic = traffic;
            _booths = booths;
            _random = random;
        }

        public bool CanAccept(TollBooth booth, Vehicle vehicle)
        {
            if (!booth.IsAcceptingVehicles) return false;
            if (booth.AssignedVehicles.Count >= booth.QueueCapacity) return false;
            return ReservedLength(booth) + vehicle.Length + _traffic.MinGap <= _lanes.StopDistance;
        }

        /// <summary>Expected seconds until this vehicle would be served at the booth.</summary>
        public double ExpectedWait(TollBooth booth)
        {
            double wait = booth.AssignedVehicles.Count * booth.CycleSeconds;
            if (booth.State == BoothState.ProcessingPayment) wait += Math.Max(0, booth.Timer);
            return wait;
        }

        public TollBooth ChooseBooth(Vehicle vehicle, int entryLane)
        {
            TollBooth best = null;
            double bestScore = double.MaxValue;
            double laneX = _lanes.LaneX(entryLane);
            foreach (var booth in _booths)
            {
                if (_lanes.EntryConnector(entryLane, booth.SlotIndex) == null) continue;
                if (!CanAccept(booth, vehicle)) continue;
                double score = ExpectedWait(booth) + booth.CycleSeconds
                               + Math.Abs(_lanes.BoothX(booth.SlotIndex) - laneX) * 0.35
                               + _random.NextDouble() * 0.05;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = booth;
                }
            }
            return best;
        }

        private double ReservedLength(TollBooth booth)
        {
            double total = 0;
            foreach (var v in booth.AssignedVehicles)
            {
                if (v != booth.CurrentVehicle || !v.BarrierCleared) total += v.Length + _traffic.MinGap;
            }
            return total;
        }
    }
}
