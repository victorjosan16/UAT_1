using System.Collections.Generic;
using TollRoadTycoon.Core;
using TollRoadTycoon.TollSystem;

namespace TollRoadTycoon.Traffic
{
    public enum VehicleState
    {
        Spawning,
        Driving,
        ApproachingQueue,
        WaitingInQueue,
        ApproachingBooth,
        Paying,
        WaitingForBarrier,
        PassingBarrier,
        Exiting,
        Completed,
    }

    /// <summary>Pooled simulation vehicle. Positions are derived from the segment and distance.</summary>
    public sealed class Vehicle
    {
        /// <summary>Unique per trip; pooled objects get a new id every time they spawn.</summary>
        public long TripId;
        public VehicleTypeConfig Type;
        public VehicleState State;
        public bool IsTolled;

        public readonly List<PathSegment> Route = new List<PathSegment>(5);
        public int RouteIndex;
        /// <summary>Distance of the front bumper along the current segment.</summary>
        public double Distance;
        public double Speed;

        public TollBooth AssignedBooth;
        public int EntryLane = -1;
        public int ExitLane = -1;
        public bool HasPaid;
        /// <summary>Set by the booth once the barrier is open for this vehicle.</summary>
        public bool BarrierCleared;

        public double SpawnTime;
        public double QueueJoinTime;

        public Vec2 Position;
        public Vec2 Heading = new Vec2(0, 1);
        public Vec2 PreviousPosition;
        public Vec2 PreviousHeading = new Vec2(0, 1);

        public PathSegment Segment => RouteIndex < Route.Count ? Route[RouteIndex] : null;
        public PathSegment NextSegment => RouteIndex + 1 < Route.Count ? Route[RouteIndex + 1] : null;
        public double Length => Type.Length;
        public double Rear => Distance - Type.Length;

        internal void Reset()
        {
            TripId = 0;
            Type = null;
            State = VehicleState.Spawning;
            IsTolled = false;
            Route.Clear();
            RouteIndex = 0;
            Distance = 0;
            Speed = 0;
            AssignedBooth = null;
            EntryLane = -1;
            ExitLane = -1;
            HasPaid = false;
            BarrierCleared = false;
            SpawnTime = 0;
            QueueJoinTime = 0;
        }
    }

    public sealed class VehiclePool
    {
        private readonly Stack<Vehicle> _free = new Stack<Vehicle>();

        public int CreatedCount { get; private set; }

        public Vehicle Get()
        {
            if (_free.Count > 0) return _free.Pop();
            CreatedCount++;
            return new Vehicle();
        }

        public void Release(Vehicle vehicle)
        {
            vehicle.Reset();
            vehicle.State = VehicleState.Completed;
            _free.Push(vehicle);
        }
    }
}
