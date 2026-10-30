using System;
using System.Collections.Generic;
using TollRoadTycoon.Core;
using TollRoadTycoon.TollSystem;

namespace TollRoadTycoon.Traffic
{
    /// <summary>
    /// Moves every vehicle along its route with a simple car-following model. A vehicle never moves
    /// past the rear of the vehicle in front (minus a gap), so cars cannot overlap or teleport.
    /// Plaza connectors that cross or merge are entered one at a time through reservations.
    /// </summary>
    public sealed class TrafficManager
    {
        private const double Lookahead = 12;
        private const double GateGrantDistance = 1.6;

        private readonly LaneManager _lanes;
        private readonly QueueManager _queues;
        private readonly TrafficConfig _config;
        private readonly RoadLayoutConfig _layout;
        private readonly VehiclePool _pool;
        private readonly List<Vehicle> _active = new List<Vehicle>();
        private readonly List<Vehicle> _completed = new List<Vehicle>();
        private long _nextTripId = 1;

        public IReadOnlyList<Vehicle> ActiveVehicles => _active;
        public VehiclePool Pool => _pool;

        public event Action<Vehicle> VehicleSpawned;
        /// <summary>Raised just before a vehicle is returned to the pool.</summary>
        public event Action<Vehicle> VehicleDespawned;

        public TrafficManager(LaneManager lanes, QueueManager queues, TrafficConfig config)
        {
            _lanes = lanes;
            _queues = queues;
            _config = config;
            _layout = lanes.Layout;
            _pool = new VehiclePool();
        }

        public double MinGap => _config.MinGap;

        /// <summary>Free distance at the start of a segment (from its start to the rear of its last vehicle).</summary>
        public double FreeSpaceAtStart(PathSegment segment)
        {
            var last = segment.Last;
            return last == null ? double.MaxValue : last.Rear;
        }

        public Vehicle Spawn(VehicleTypeConfig type, PathSegment segment, bool tolled, int entryLane, double now)
        {
            double free = FreeSpaceAtStart(segment);
            if (free < type.Length + _config.MinGap) return null;

            var v = _pool.Get();
            v.Reset();
            v.TripId = _nextTripId++;
            v.Type = type;
            v.IsTolled = tolled;
            v.EntryLane = entryLane;
            v.Route.Add(segment);
            v.RouteIndex = 0;
            v.Distance = type.Length;
            double gap = free - v.Distance - _config.MinGap;
            v.Speed = Math.Min(type.MaxSpeed * 0.85, Math.Sqrt(2 * type.Deceleration * Math.Max(0, gap)));
            v.SpawnTime = now;
            v.State = VehicleState.Spawning;
            segment.Vehicles.Add(v);
            UpdatePose(v, true);
            _active.Add(v);
            VehicleSpawned?.Invoke(v);
            return v;
        }

        public void Tick(double now, double dt)
        {
            for (int i = 0; i < _active.Count; i++)
            {
                var v = _active[i];
                v.PreviousPosition = v.Position;
                v.PreviousHeading = v.Heading;
                if (v.IsTolled && v.AssignedBooth == null) TryAssignBooth(v, now);
                TryGrantGate(v);
                Move(v, dt);
                UpdateState(v);
                UpdatePose(v, false);
                if (v.State == VehicleState.Completed) _completed.Add(v);
            }

            if (_completed.Count == 0) return;
            foreach (var v in _completed)
            {
                _active.Remove(v);
                v.Segment?.Vehicles.Remove(v);
                VehicleDespawned?.Invoke(v);
                _pool.Release(v);
            }
            _completed.Clear();
        }

        private void TryAssignBooth(Vehicle v, double now)
        {
            var seg = v.Segment;
            if (seg.Kind != SegmentKind.Approach) return;
            if (seg.Length - v.Distance > _layout.DecisionLookahead) return;

            var booth = _queues.ChooseBooth(v, v.EntryLane);
            if (booth == null) return;

            int exitLane = _lanes.NearestLane(booth.SlotIndex);
            v.AssignedBooth = booth;
            v.ExitLane = exitLane;
            v.QueueJoinTime = now;
            booth.AssignedVehicles.Add(v);
            v.Route.Add(_lanes.EntryConnector(v.EntryLane, booth.SlotIndex));
            v.Route.Add(booth.Lane);
            v.Route.Add(_lanes.ExitConnector(booth.SlotIndex, exitLane));
            v.Route.Add(_lanes.Exit(exitLane));
            v.State = VehicleState.ApproachingQueue;
        }

        /// <summary>Reserves the next plaza connector when nothing conflicting is on it.</summary>
        private void TryGrantGate(Vehicle v)
        {
            var next = v.NextSegment;
            if (next == null || !next.IsConnector || next.Reservations.Contains(v)) return;
            // Only the front-most vehicle of a segment may reserve, so a reservation is never held by a car stuck behind another.
            if (v.Segment.Vehicles.Count == 0 || v.Segment.Vehicles[0] != v) return;
            if (v.Segment.Length - v.Distance > GateGrantDistance + v.Speed * 0.25) return;
            if (v.Segment.Kind == SegmentKind.BoothLane && !v.BarrierCleared) return;

            bool entry = next.Kind == SegmentKind.PlazaEntry;
            foreach (var other in entry ? _lanes.EntryConnectors() : _lanes.ExitConnectors())
            {
                if (other.Reservations.Count == 0) continue;
                bool conflict = entry ? _lanes.EntryConnectorsConflict(next, other) : _lanes.ExitConnectorsConflict(next, other);
                if (conflict) return;
            }
            next.Reservations.Add(v);
        }

        private void Move(Vehicle v, double dt)
        {
            double free = FreeDistance(v) - _config.MinGap;
            double stopSpeed = Math.Sqrt(2 * v.Type.Deceleration * Math.Max(0, free));
            double target = Math.Min(v.Type.MaxSpeed, stopSpeed);
            if (v.State == VehicleState.Paying || v.State == VehicleState.WaitingForBarrier) target = 0;

            double speed = v.Speed < target ? Math.Min(target, v.Speed + v.Type.Acceleration * dt) : target;
            double step = Math.Min(speed * dt, Math.Max(0, free));
            v.Speed = dt > 0 ? step / dt : 0;
            v.Distance += step;

            // Hand over to the following segment(s) when allowed; otherwise hold at the segment end.
            while (v.Distance >= v.Segment.Length)
            {
                var current = v.Segment;
                var next = v.NextSegment;
                if (next == null)
                {
                    if (current.Kind == SegmentKind.Exit || current.Kind == SegmentKind.Oncoming)
                    {
                        v.State = VehicleState.Completed;
                    }
                    else
                    {
                        v.Distance = current.Length;
                        v.Speed = 0;
                    }
                    return;
                }
                if (next.IsConnector && !next.Reservations.Contains(v))
                {
                    v.Distance = current.Length;
                    v.Speed = 0;
                    return;
                }
                v.Distance -= current.Length;
                current.Vehicles.Remove(v);
                current.Reservations.Remove(v);
                v.RouteIndex++;
                next.Vehicles.Add(v);
            }
        }

        /// <summary>Distance from the front bumper to the nearest thing the vehicle must not pass.</summary>
        public double FreeDistance(Vehicle v)
        {
            var seg = v.Segment;
            double best = double.MaxValue;

            // Leader on the same segment.
            int index = seg.Vehicles.IndexOf(v);
            if (index > 0) best = seg.Vehicles[index - 1].Rear - v.Distance;

            // Vehicles on diverging connectors that start where this one starts.
            if (seg.IsConnector)
            {
                foreach (var sibling in seg.Siblings)
                {
                    foreach (var u in sibling.Vehicles)
                    {
                        if (u.Distance > v.Distance) best = Math.Min(best, u.Rear - v.Distance);
                    }
                }
            }

            // Tails of vehicles that just left this segment onto a diverging connector.
            if (index == 0)
            {
                foreach (var succ in seg.Successors)
                {
                    var last = succ.Last;
                    if (last == null) continue;
                    double rel = seg.Length + last.Rear;
                    if (succ == v.NextSegment || rel < seg.Length) best = Math.Min(best, rel - v.Distance);
                }
            }

            // Stop points on the current segment.
            if (seg.Kind == SegmentKind.BoothLane && !v.BarrierCleared)
            {
                best = Math.Min(best, _lanes.StopDistance - v.Distance + _config.MinGap);
            }

            var nextSeg = v.NextSegment;
            bool mayEnterNext = nextSeg != null && (!nextSeg.IsConnector || nextSeg.Reservations.Contains(v));
            if (seg.Kind != SegmentKind.Exit && seg.Kind != SegmentKind.Oncoming && !mayEnterNext)
            {
                // Waiting for a booth assignment or for a plaza gate: stop at the end of the segment.
                return Math.Min(best, seg.Length - v.Distance + _config.MinGap);
            }

            // Look further along the route for the next vehicle.
            double offset = seg.Length - v.Distance;
            for (int k = v.RouteIndex + 1; k < v.Route.Count && offset < Lookahead && best == double.MaxValue; k++)
            {
                var s = v.Route[k];
                var last = s.Last;
                if (last != null)
                {
                    best = Math.Min(best, offset + last.Rear);
                    break;
                }
                if (s.Kind == SegmentKind.BoothLane && !v.BarrierCleared)
                {
                    best = Math.Min(best, offset + _lanes.StopDistance + _config.MinGap);
                    break;
                }
                if (s.IsConnector && !s.Reservations.Contains(v))
                {
                    best = Math.Min(best, offset + _config.MinGap);
                    break;
                }
                offset += s.Length;
            }
            return best;
        }

        private void UpdateState(Vehicle v)
        {
            if (v.State == VehicleState.Completed || v.State == VehicleState.Paying || v.State == VehicleState.WaitingForBarrier) return;
            var seg = v.Segment;
            switch (seg.Kind)
            {
                case SegmentKind.Approach:
                    v.State = v.AssignedBooth == null ? VehicleState.Driving
                        : v.Speed < 0.1 ? VehicleState.WaitingInQueue : VehicleState.ApproachingQueue;
                    break;
                case SegmentKind.PlazaEntry:
                    v.State = v.Speed < 0.1 ? VehicleState.WaitingInQueue : VehicleState.ApproachingQueue;
                    break;
                case SegmentKind.BoothLane:
                    if (v.BarrierCleared)
                    {
                        v.State = v.Rear > _lanes.BarrierDistance ? VehicleState.Exiting : VehicleState.PassingBarrier;
                    }
                    else
                    {
                        bool isFront = seg.Vehicles.Count > 0 && FirstUncleared(seg) == v;
                        v.State = isFront ? VehicleState.ApproachingBooth
                            : v.Speed < 0.1 ? VehicleState.WaitingInQueue : VehicleState.ApproachingQueue;
                    }
                    break;
                case SegmentKind.PlazaExit:
                case SegmentKind.Exit:
                    v.State = VehicleState.Exiting;
                    break;
                case SegmentKind.Oncoming:
                    v.State = VehicleState.Driving;
                    break;
            }
        }

        private static Vehicle FirstUncleared(PathSegment seg)
        {
            foreach (var v in seg.Vehicles)
            {
                if (!v.BarrierCleared) return v;
            }
            return null;
        }

        private static void UpdatePose(Vehicle v, bool snap)
        {
            var seg = v.Segment;
            if (seg == null) return;
            // Pose is the centre of the vehicle body.
            v.Position = seg.Evaluate(v.Distance - v.Length * 0.5, out var dir);
            v.Heading = dir;
            if (snap)
            {
                v.PreviousPosition = v.Position;
                v.PreviousHeading = v.Heading;
            }
        }

        /// <summary>Removes every vehicle (used when loading a save).</summary>
        public void Clear(IReadOnlyList<TollBooth> booths)
        {
            foreach (var v in _active)
            {
                v.Segment?.Vehicles.Remove(v);
                VehicleDespawned?.Invoke(v);
                _pool.Release(v);
            }
            _active.Clear();
            foreach (var seg in _lanes.AllSegments)
            {
                seg.Vehicles.Clear();
                seg.Reservations.Clear();
            }
            foreach (var booth in booths)
            {
                booth.AssignedVehicles.Clear();
                booth.CurrentVehicle = null;
                booth.BarrierOpen = 0;
                booth.State = BoothState.Closed;
            }
        }
    }
}
