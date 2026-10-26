using System;

namespace UndergroundMetroTycoon.Simulation
{
    /// <summary>Cell coordinate. Layer 0 is the surface layer; layers grow downwards.</summary>
    public readonly struct GridCoord : IEquatable<GridCoord>
    {
        public readonly int X;
        public readonly int Layer;
        public readonly int Z;

        public GridCoord(int x, int layer, int z)
        {
            X = x;
            Layer = layer;
            Z = z;
        }

        public bool Equals(GridCoord other) => X == other.X && Layer == other.Layer && Z == other.Z;
        public override bool Equals(object obj) => obj is GridCoord other && Equals(other);
        public override int GetHashCode() => (X * 73856093) ^ (Layer * 19349663) ^ (Z * 83492791);
        public override string ToString() => $"({X},{Layer},{Z})";
    }
}
