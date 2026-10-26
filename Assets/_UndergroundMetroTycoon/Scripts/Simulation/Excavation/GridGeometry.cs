using System.Numerics;

namespace UndergroundMetroTycoon.Simulation
{
    /// <summary>Maps grid coordinates to world space. Cell (0,0,0) occupies the min-X/min-Z corner of the surface.</summary>
    public readonly struct GridGeometry
    {
        public readonly float OriginX;
        public readonly float OriginZ;
        public readonly float SurfaceY;
        public readonly float CellSize;
        public readonly float LayerHeight;

        public GridGeometry(float originX, float originZ, float surfaceY, float cellSize, float layerHeight)
        {
            OriginX = originX;
            OriginZ = originZ;
            SurfaceY = surfaceY;
            CellSize = cellSize;
            LayerHeight = layerHeight;
        }

        public static GridGeometry Centered(SiteAnchor anchor, int width, int length)
        {
            return new GridGeometry(
                anchor.CenterX - width * anchor.CellSize * 0.5f,
                anchor.CenterZ - length * anchor.CellSize * 0.5f,
                anchor.SurfaceY,
                anchor.CellSize,
                anchor.LayerHeight);
        }

        public Vector3 CellCenter(GridCoord c) => new Vector3(
            OriginX + (c.X + 0.5f) * CellSize,
            SurfaceY - (c.Layer + 0.5f) * LayerHeight,
            OriginZ + (c.Z + 0.5f) * CellSize);

        public Vector3 CellTopCenter(GridCoord c) => new Vector3(
            OriginX + (c.X + 0.5f) * CellSize,
            SurfaceY - c.Layer * LayerHeight,
            OriginZ + (c.Z + 0.5f) * CellSize);

        public bool TryGetColumn(float worldX, float worldZ, int width, int length, out int x, out int z)
        {
            float fx = (worldX - OriginX) / CellSize;
            float fz = (worldZ - OriginZ) / CellSize;
            x = (int)System.Math.Floor(fx);
            z = (int)System.Math.Floor(fz);
            return x >= 0 && z >= 0 && x < width && z < length;
        }
    }
}
