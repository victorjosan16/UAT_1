using System;

namespace UndergroundMetroTycoon.Simulation
{
    /// <summary>Builds the deterministic terrain grid for a contract's construction site.</summary>
    public static class SiteGenerator
    {
        public static ExcavationGrid Build(ContractDefinition contract, GameContent content)
        {
            if (contract == null) throw new ArgumentNullException(nameof(contract));
            if (content == null) throw new ArgumentNullException(nameof(content));

            int width = contract.SiteWidth;
            int length = contract.SiteLength;
            int layers = contract.Layers.Count;
            GridGeometry layout = GridGeometry.Centered(content.Site, width, length);
            var cells = new TerrainCell[width * layers * length];

            for (int layer = 0; layer < layers; layer++)
            {
                ContractLayerSpec spec = contract.Layers[layer];
                TerrainMaterialDefinition material = content.GetTerrain(spec.Material)
                    ?? throw new InvalidOperationException($"Terrain material {spec.Material} is not configured.");
                for (int z = 0; z < length; z++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int index = (layer * length + z) * width + x;
                        string owner = spec.Contains(x, z) ? contract.Id : null;
                        cells[index] = new TerrainCell(index, $"{contract.Id}:{x}:{layer}:{z}", new GridCoord(x, layer, z), material, owner);
                    }
                }
            }

            return new ExcavationGrid(width, layers, length, layout, contract.Id, cells);
        }
    }
}
