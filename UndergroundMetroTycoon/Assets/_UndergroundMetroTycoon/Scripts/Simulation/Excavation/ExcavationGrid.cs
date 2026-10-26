using System;
using System.Collections.Generic;

namespace UndergroundMetroTycoon.Simulation
{
    /// <summary>Grid of terrain cells for the construction site currently on screen.</summary>
    public sealed class ExcavationGrid : IGroundHeightProvider
    {
        private readonly TerrainCell[] _cells;
        private readonly int[] _remainingContractCellsPerLayer;

        public ExcavationGrid(int width, int layers, int length, GridGeometry layout, string siteContractId, TerrainCell[] cells)
        {
            if (width <= 0 || layers <= 0 || length <= 0) throw new ArgumentException("Grid dimensions must be positive.");
            if (cells == null || cells.Length != width * layers * length) throw new ArgumentException("Cell array does not match grid dimensions.");
            Width = width;
            Layers = layers;
            Length = length;
            Layout = layout;
            SiteContractId = siteContractId;
            _cells = cells;
            _remainingContractCellsPerLayer = new int[layers];
            Recount();
        }

        public int Width { get; }
        public int Layers { get; }
        public int Length { get; }
        public GridGeometry Layout { get; }

        /// <summary>Contract whose layout generated this site.</summary>
        public string SiteContractId { get; }

        public int ContractCellCount { get; private set; }
        public int ExcavatedContractCellCount { get; private set; }
        public IReadOnlyList<TerrainCell> Cells => _cells;
        public int CellCount => _cells.Length;
        public bool IsContractComplete => ContractCellCount > 0 && ExcavatedContractCellCount >= ContractCellCount;
        public float FloorY => Layout.SurfaceY - Layers * Layout.LayerHeight;

        public int IndexOf(int x, int layer, int z) => (layer * Length + z) * Width + x;

        public bool InBounds(int x, int layer, int z) =>
            x >= 0 && z >= 0 && layer >= 0 && x < Width && z < Length && layer < Layers;

        public TerrainCell GetCell(int index) => index >= 0 && index < _cells.Length ? _cells[index] : null;

        public TerrainCell GetCell(int x, int layer, int z) => InBounds(x, layer, z) ? _cells[IndexOf(x, layer, z)] : null;

        /// <summary>
        /// A cell can be worked on when it still exists, its top face is exposed and the contract part of the layer
        /// above is fully excavated. Digging layer by layer (benching) keeps every step in the pit at most one layer
        /// high, so diggers can never trap themselves in a hole.
        /// </summary>
        public bool IsAccessible(TerrainCell cell)
        {
            if (cell == null || cell.IsExcavated) return false;
            int layer = cell.Coord.Layer;
            if (layer == 0) return true;
            if (_remainingContractCellsPerLayer[layer - 1] > 0) return false;
            TerrainCell above = GetCell(cell.Coord.X, layer - 1, cell.Coord.Z);
            return above == null || above.IsExcavated;
        }

        public int RemainingContractCellsInLayer(int layer) =>
            layer >= 0 && layer < Layers ? _remainingContractCellsPerLayer[layer] : 0;

        public float GetGroundHeight(float worldX, float worldZ)
        {
            if (!Layout.TryGetColumn(worldX, worldZ, Width, Length, out int x, out int z)) return Layout.SurfaceY;
            for (int layer = 0; layer < Layers; layer++)
            {
                if (!_cells[IndexOf(x, layer, z)].IsExcavated) return Layout.SurfaceY - layer * Layout.LayerHeight;
            }
            return FloorY;
        }

        internal void Recount()
        {
            int total = 0, done = 0;
            Array.Clear(_remainingContractCellsPerLayer, 0, _remainingContractCellsPerLayer.Length);
            foreach (TerrainCell c in _cells)
            {
                if (!c.IsContractCell) continue;
                total++;
                if (c.IsExcavated) done++;
                else _remainingContractCellsPerLayer[c.Coord.Layer]++;
            }
            ContractCellCount = total;
            ExcavatedContractCellCount = done;
        }

        internal void NotifyExcavated(TerrainCell cell)
        {
            if (!cell.IsContractCell) return;
            ExcavatedContractCellCount++;
            _remainingContractCellsPerLayer[cell.Coord.Layer]--;
        }
    }
}
