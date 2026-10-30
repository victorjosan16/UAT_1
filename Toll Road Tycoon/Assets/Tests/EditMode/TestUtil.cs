using System;
using System.Collections.Generic;
using TollRoadTycoon.Core;
using TollRoadTycoon.Traffic;

namespace TollRoadTycoon.Tests
{
    internal static class TestUtil
    {
        public static GameSimulation NewSim(int seed = 7, Action<GameConfig> tweak = null)
        {
            var config = GameConfig.CreateDefault();
            tweak?.Invoke(config);
            return new GameSimulation(config, seed);
        }

        public static void GiveMoney(GameSimulation sim, long cents)
        {
            sim.Economy.AddMoney(cents, Economy.MoneySource.Debug);
        }

        /// <summary>Exact oriented-box overlap test (separating axis theorem) with a small tolerance.</summary>
        public static bool Overlaps(Vehicle a, Vehicle b, double shrink = 0.04)
        {
            var axes = new[]
            {
                a.Heading, new Vec2(-a.Heading.Y, a.Heading.X),
                b.Heading, new Vec2(-b.Heading.Y, b.Heading.X),
            };
            foreach (var axis in axes)
            {
                Project(a, axis, shrink, out double minA, out double maxA);
                Project(b, axis, shrink, out double minB, out double maxB);
                if (maxA <= minB || maxB <= minA) return false;
            }
            return true;
        }

        private static void Project(Vehicle v, Vec2 axis, double shrink, out double min, out double max)
        {
            var f = v.Heading;
            var r = new Vec2(-f.Y, f.X);
            double hl = v.Type.Length * 0.5 - shrink;
            double hw = v.Type.Width * 0.5 - shrink;
            double c = v.Position.X * axis.X + v.Position.Y * axis.Y;
            double e = Math.Abs(f.X * axis.X + f.Y * axis.Y) * hl + Math.Abs(r.X * axis.X + r.Y * axis.Y) * hw;
            min = c - e;
            max = c + e;
        }

        public static string FindOverlap(IReadOnlyList<Vehicle> vehicles)
        {
            for (int i = 0; i < vehicles.Count; i++)
            {
                for (int j = i + 1; j < vehicles.Count; j++)
                {
                    if (Overlaps(vehicles[i], vehicles[j]))
                    {
                        var a = vehicles[i];
                        var b = vehicles[j];
                        return $"#{a.TripId} {a.Type.Id} {a.State} seg {a.Segment.Kind}/{a.Segment.Id} d={a.Distance:0.00} at {a.Position} vs " +
                               $"#{b.TripId} {b.Type.Id} {b.State} seg {b.Segment.Kind}/{b.Segment.Id} d={b.Distance:0.00} at {b.Position}";
                    }
                }
            }
            return null;
        }
    }
}
