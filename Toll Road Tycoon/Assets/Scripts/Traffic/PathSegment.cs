using System;
using System.Collections.Generic;
using TollRoadTycoon.Core;

namespace TollRoadTycoon.Traffic
{
    public enum SegmentKind
    {
        Approach,
        PlazaEntry,
        BoothLane,
        PlazaExit,
        Exit,
        Oncoming,
    }

    /// <summary>
    /// A one-way polyline that vehicles drive along. Vehicles on a segment are kept in driving order
    /// (index 0 is the front-most) because overtaking is impossible inside a segment.
    /// </summary>
    public sealed class PathSegment
    {
        private readonly Vec2[] _points;
        private readonly double[] _cumulative;

        public int Id { get; }
        public SegmentKind Kind { get; }
        public int LaneIndex { get; }
        public int BoothSlot { get; }
        public double Length { get; }
        public Vec2 Start => _points[0];
        public Vec2 End => _points[_points.Length - 1];
        public IReadOnlyList<Vec2> Points => _points;

        /// <summary>Vehicles whose front bumper is on this segment, front-most first.</summary>
        public readonly List<Vehicle> Vehicles = new List<Vehicle>();

        /// <summary>Vehicles allowed to enter this connector (includes those already on it).</summary>
        public readonly List<Vehicle> Reservations = new List<Vehicle>();

        public readonly List<PathSegment> Successors = new List<PathSegment>();

        /// <summary>Connectors that start at the same point (diverging paths), used for following.</summary>
        public readonly List<PathSegment> Siblings = new List<PathSegment>();

        public PathSegment(int id, SegmentKind kind, Vec2[] points, int laneIndex = -1, int boothSlot = -1)
        {
            if (points == null || points.Length < 2) throw new ArgumentException("A segment needs at least two points.");
            Id = id;
            Kind = kind;
            LaneIndex = laneIndex;
            BoothSlot = boothSlot;
            _points = points;
            _cumulative = new double[points.Length];
            for (int i = 1; i < points.Length; i++)
            {
                _cumulative[i] = _cumulative[i - 1] + Vec2.Distance(points[i - 1], points[i]);
            }
            Length = _cumulative[points.Length - 1];
        }

        public bool IsConnector => Kind == SegmentKind.PlazaEntry || Kind == SegmentKind.PlazaExit;

        public Vehicle Last => Vehicles.Count > 0 ? Vehicles[Vehicles.Count - 1] : null;

        /// <summary>Position at a distance along the segment; distances outside the segment are extrapolated.</summary>
        public Vec2 Evaluate(double distance, out Vec2 direction)
        {
            if (distance <= 0)
            {
                direction = (_points[1] - _points[0]).Normalized;
                return _points[0] + direction * distance;
            }
            if (distance >= Length)
            {
                int n = _points.Length;
                direction = (_points[n - 1] - _points[n - 2]).Normalized;
                return _points[n - 1] + direction * (distance - Length);
            }
            int lo = 0;
            int hi = _cumulative.Length - 1;
            while (hi - lo > 1)
            {
                int mid = (lo + hi) / 2;
                if (_cumulative[mid] <= distance) lo = mid;
                else hi = mid;
            }
            double span = _cumulative[hi] - _cumulative[lo];
            double t = span > 1e-9 ? (distance - _cumulative[lo]) / span : 0;
            direction = (_points[hi] - _points[lo]).Normalized;
            return Vec2.Lerp(_points[lo], _points[hi], t);
        }

        public static PathSegment Line(int id, SegmentKind kind, Vec2 from, Vec2 to, int lane = -1, int booth = -1)
        {
            return new PathSegment(id, kind, new[] { from, to }, lane, booth);
        }

        /// <summary>S-shaped lane change between two vertical lanes, sampled as a polyline.</summary>
        public static PathSegment LaneShift(int id, SegmentKind kind, Vec2 from, Vec2 to, int lane, int booth, int samples = 16)
        {
            double dy = to.Y - from.Y;
            Vec2 c1 = new Vec2(from.X, from.Y + dy * 0.5);
            Vec2 c2 = new Vec2(to.X, to.Y - dy * 0.5);
            var points = new Vec2[samples + 1];
            for (int i = 0; i <= samples; i++)
            {
                double t = (double)i / samples;
                double u = 1 - t;
                points[i] = from * (u * u * u) + c1 * (3 * u * u * t) + c2 * (3 * u * t * t) + to * (t * t * t);
            }
            return new PathSegment(id, kind, points, lane, booth);
        }
    }
}
