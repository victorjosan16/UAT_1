using System;

namespace TollRoadTycoon.Core
{
    /// <summary>Engine-independent 2D vector so the simulation can run and be tested outside Unity.</summary>
    [Serializable]
    public struct Vec2 : IEquatable<Vec2>
    {
        public double X;
        public double Y;

        public Vec2(double x, double y)
        {
            X = x;
            Y = y;
        }

        public static Vec2 Zero => new Vec2(0, 0);

        public double Length => Math.Sqrt(X * X + Y * Y);

        public Vec2 Normalized
        {
            get
            {
                double len = Length;
                return len > 1e-9 ? new Vec2(X / len, Y / len) : Zero;
            }
        }

        public static Vec2 operator +(Vec2 a, Vec2 b) => new Vec2(a.X + b.X, a.Y + b.Y);
        public static Vec2 operator -(Vec2 a, Vec2 b) => new Vec2(a.X - b.X, a.Y - b.Y);
        public static Vec2 operator *(Vec2 a, double s) => new Vec2(a.X * s, a.Y * s);
        public static Vec2 operator *(double s, Vec2 a) => new Vec2(a.X * s, a.Y * s);

        public static double Distance(Vec2 a, Vec2 b) => (a - b).Length;

        public static Vec2 Lerp(Vec2 a, Vec2 b, double t) => new Vec2(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);

        public bool Equals(Vec2 other) => X.Equals(other.X) && Y.Equals(other.Y);
        public override bool Equals(object obj) => obj is Vec2 other && Equals(other);
        public override int GetHashCode() => X.GetHashCode() * 397 ^ Y.GetHashCode();
        public override string ToString() => $"({X:0.##}, {Y:0.##})";
    }
}
