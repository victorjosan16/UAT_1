using System;
using System.Collections.Generic;
using TollRoadTycoon.Core;

namespace TollRoadTycoon.Traffic
{
    /// <summary>
    /// Owns the road geometry: approach/exit lanes, booth lanes and the connectors between them.
    /// Expansion only ever adds segments, so vehicles already driving keep valid references.
    /// </summary>
    public sealed class LaneManager
    {
        private readonly RoadLayoutConfig _layout;
        private readonly PathSegment[] _approach;
        private readonly PathSegment[] _exit;
        private readonly PathSegment[] _boothLanes;
        private readonly PathSegment[,] _entry;
        private readonly PathSegment[,] _exitConnectors;
        private readonly List<PathSegment> _all = new List<PathSegment>();
        private int _nextId;

        public int ActiveLaneCount { get; private set; }
        public PathSegment Oncoming { get; }
        public IReadOnlyList<PathSegment> AllSegments => _all;

        public event Action GeometryChanged;

        public LaneManager(RoadLayoutConfig layout, int maxLanes, int maxBooths)
        {
            _layout = layout;
            _approach = new PathSegment[maxLanes];
            _exit = new PathSegment[maxLanes];
            _boothLanes = new PathSegment[maxBooths];
            _entry = new PathSegment[maxLanes, maxBooths];
            _exitConnectors = new PathSegment[maxBooths, maxLanes];
            Oncoming = Add(PathSegment.Line(_nextId++, SegmentKind.Oncoming,
                new Vec2(layout.OncomingLaneX, layout.DespawnY), new Vec2(layout.OncomingLaneX, layout.SpawnY)));
        }

        public RoadLayoutConfig Layout => _layout;
        public double LaneX(int lane) => _layout.LaneX[lane];
        public double BoothX(int slot) => _layout.BoothSlotX[slot];

        /// <summary>Distance along a booth lane at which the front bumper stops to pay.</summary>
        public double StopDistance => _layout.StopLineY - _layout.BoothLaneStartY;
        public double BarrierDistance => _layout.BarrierY - _layout.BoothLaneStartY;

        public PathSegment Approach(int lane) => _approach[lane];
        public PathSegment Exit(int lane) => _exit[lane];
        public PathSegment BoothLane(int slot) => _boothLanes[slot];
        public PathSegment EntryConnector(int lane, int slot) => _entry[lane, slot];
        public PathSegment ExitConnector(int slot, int lane) => _exitConnectors[slot, lane];

        public void ActivateLane(int lane)
        {
            if (_approach[lane] != null) return;
            double x = LaneX(lane);
            _approach[lane] = Add(PathSegment.Line(_nextId++, SegmentKind.Approach,
                new Vec2(x, _layout.SpawnY), new Vec2(x, _layout.DecisionY), lane));
            _exit[lane] = Add(PathSegment.Line(_nextId++, SegmentKind.Exit,
                new Vec2(x, _layout.ExitStartY), new Vec2(x, _layout.DespawnY), lane));
            ActiveLaneCount = Math.Max(ActiveLaneCount, lane + 1);
            for (int slot = 0; slot < _boothLanes.Length; slot++)
            {
                if (_boothLanes[slot] != null) Connect(lane, slot);
            }
            GeometryChanged?.Invoke();
        }

        public void ActivateBoothSlot(int slot)
        {
            if (_boothLanes[slot] != null) return;
            double x = BoothX(slot);
            _boothLanes[slot] = Add(PathSegment.Line(_nextId++, SegmentKind.BoothLane,
                new Vec2(x, _layout.BoothLaneStartY), new Vec2(x, _layout.BoothLaneEndY), -1, slot));
            for (int lane = 0; lane < _approach.Length; lane++)
            {
                if (_approach[lane] != null) Connect(lane, slot);
            }
            GeometryChanged?.Invoke();
        }

        private void Connect(int lane, int slot)
        {
            var entry = PathSegment.LaneShift(_nextId++, SegmentKind.PlazaEntry,
                _approach[lane].End, _boothLanes[slot].Start, lane, slot);
            var exit = PathSegment.LaneShift(_nextId++, SegmentKind.PlazaExit,
                _boothLanes[slot].End, _exit[lane].Start, lane, slot);
            _entry[lane, slot] = Add(entry);
            _exitConnectors[slot, lane] = Add(exit);

            Link(_approach[lane], entry);
            entry.Successors.Add(_boothLanes[slot]);
            Link(_boothLanes[slot], exit);
            exit.Successors.Add(_exit[lane]);
        }

        private static void Link(PathSegment from, PathSegment connector)
        {
            foreach (var sibling in from.Successors)
            {
                sibling.Siblings.Add(connector);
                connector.Siblings.Add(sibling);
            }
            from.Successors.Add(connector);
        }

        /// <summary>True when two plaza-entry connectors would cross or merge into the same booth lane.</summary>
        public bool EntryConnectorsConflict(PathSegment a, PathSegment b)
        {
            if (a == b || a.LaneIndex == b.LaneIndex) return false;
            if (a.BoothSlot == b.BoothSlot) return true;
            return Math.Sign(LaneX(a.LaneIndex) - LaneX(b.LaneIndex)) != Math.Sign(BoothX(a.BoothSlot) - BoothX(b.BoothSlot));
        }

        /// <summary>True when two plaza-exit connectors would cross or merge into the same exit lane.</summary>
        public bool ExitConnectorsConflict(PathSegment a, PathSegment b)
        {
            if (a == b || a.BoothSlot == b.BoothSlot) return false;
            if (a.LaneIndex == b.LaneIndex) return true;
            return Math.Sign(LaneX(a.LaneIndex) - LaneX(b.LaneIndex)) != Math.Sign(BoothX(a.BoothSlot) - BoothX(b.BoothSlot));
        }

        public IEnumerable<PathSegment> EntryConnectors()
        {
            foreach (var s in _all)
            {
                if (s.Kind == SegmentKind.PlazaEntry) yield return s;
            }
        }

        public IEnumerable<PathSegment> ExitConnectors()
        {
            foreach (var s in _all)
            {
                if (s.Kind == SegmentKind.PlazaExit) yield return s;
            }
        }

        /// <summary>Active exit lane closest to a booth slot.</summary>
        public int NearestLane(int slot)
        {
            int best = 0;
            double bestDist = double.MaxValue;
            for (int lane = 0; lane < ActiveLaneCount; lane++)
            {
                if (_exit[lane] == null) continue;
                double d = Math.Abs(LaneX(lane) - BoothX(slot));
                if (d < bestDist - 1e-6)
                {
                    bestDist = d;
                    best = lane;
                }
            }
            return best;
        }

        private PathSegment Add(PathSegment segment)
        {
            _all.Add(segment);
            return segment;
        }
    }
}
