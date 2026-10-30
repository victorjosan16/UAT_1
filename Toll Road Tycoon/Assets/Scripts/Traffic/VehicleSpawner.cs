using System;
using System.Collections.Generic;
using TollRoadTycoon.Core;

namespace TollRoadTycoon.Traffic
{
    /// <summary>
    /// Generates tolled arrivals on the active approach lanes and free-flowing oncoming traffic.
    /// Demand scales with the number of lanes and unlocked vehicle types; a blocked spawn point
    /// simply holds the arrival until there is room.
    /// </summary>
    public sealed class VehicleSpawner
    {
        private readonly GameConfig _config;
        private readonly LaneManager _lanes;
        private readonly TrafficManager _traffic;
        private readonly SimRandom _random;
        private readonly HashSet<string> _unlocked;
        private double _pendingTolled;
        private double _pendingOncoming;

        /// <summary>Optional override used by stress tests and the debug menu.</summary>
        public double DemandMultiplier { get; set; } = 1.0;

        public long BlockedSpawns { get; private set; }

        public VehicleSpawner(GameConfig config, LaneManager lanes, TrafficManager traffic, SimRandom random, HashSet<string> unlockedVehicleIds)
        {
            _config = config;
            _lanes = lanes;
            _traffic = traffic;
            _random = random;
            _unlocked = unlockedVehicleIds;
        }

        public double ArrivalsPerSecond
        {
            get
            {
                var t = _config.Traffic;
                int extraTypes = 0;
                foreach (var type in _config.VehicleTypes)
                {
                    if (!type.UnlockedByDefault && _unlocked.Contains(type.Id)) extraTypes++;
                }
                double lanes = Math.Max(1, _lanes.ActiveLaneCount);
                return (t.BaseArrivalsPerSecond + t.ArrivalsPerExtraLane * (lanes - 1))
                       * (1 + t.ArrivalBonusPerUnlockedType * extraTypes) * DemandMultiplier;
            }
        }

        public void Tick(double now, double dt)
        {
            var t = _config.Traffic;
            _pendingTolled = Math.Min(t.MaxPendingArrivals, _pendingTolled + ArrivalsPerSecond * dt);
            while (_pendingTolled >= 1)
            {
                if (!TrySpawnTolled(now))
                {
                    BlockedSpawns++;
                    break;
                }
                _pendingTolled -= 1;
            }

            _pendingOncoming = Math.Min(t.MaxPendingArrivals, _pendingOncoming + t.OncomingArrivalsPerSecond * dt);
            if (_pendingOncoming >= 1)
            {
                var type = PickType(allowEmergency: false);
                if (type != null && _traffic.FreeSpaceAtStart(_lanes.Oncoming) >= type.Length + t.SpawnClearance)
                {
                    _traffic.Spawn(type, _lanes.Oncoming, false, -1, now);
                    _pendingOncoming -= 1;
                }
            }
        }

        private bool TrySpawnTolled(double now)
        {
            var type = PickType(allowEmergency: true);
            if (type == null) return false;

            int bestLane = -1;
            double bestSpace = 0;
            for (int lane = 0; lane < _lanes.ActiveLaneCount; lane++)
            {
                var approach = _lanes.Approach(lane);
                if (approach == null) continue;
                double space = _traffic.FreeSpaceAtStart(approach);
                if (space < type.Length + _config.Traffic.SpawnClearance) continue;
                // Prefer the emptiest lane; add a little noise so lanes alternate.
                double score = Math.Min(space, 1000) + _random.NextDouble() * 0.5 - approach.Vehicles.Count * 2;
                if (bestLane < 0 || score > bestSpace)
                {
                    bestLane = lane;
                    bestSpace = score;
                }
            }
            if (bestLane < 0) return false;
            return _traffic.Spawn(type, _lanes.Approach(bestLane), true, bestLane, now) != null;
        }

        public VehicleTypeConfig PickType(bool allowEmergency)
        {
            double total = 0;
            foreach (var type in _config.VehicleTypes)
            {
                if (IsSpawnable(type, allowEmergency)) total += type.SpawnWeight;
            }
            if (total <= 0) return null;
            double roll = _random.NextDouble() * total;
            VehicleTypeConfig last = null;
            foreach (var type in _config.VehicleTypes)
            {
                if (!IsSpawnable(type, allowEmergency)) continue;
                last = type;
                roll -= type.SpawnWeight;
                if (roll < 0) return type;
            }
            return last;
        }

        private bool IsSpawnable(VehicleTypeConfig type, bool allowEmergency)
        {
            if (type.SpawnWeight <= 0) return false;
            if (type.IsEmergency && !allowEmergency) return false;
            return _unlocked.Contains(type.Id);
        }
    }
}
