using System;
using System.Collections.Generic;

namespace UndergroundMetroTycoon.Simulation
{
    /// <summary>World placement of the construction site grid.</summary>
    [Serializable]
    public sealed class SiteAnchor
    {
        public float CenterX;
        public float CenterZ;
        public float SurfaceY;
        public float CellSize = 1f;
        public float LayerHeight = 0.5f;
    }

    /// <summary>All balancing data the simulation needs. Built from the GameConfig ScriptableObject at runtime.</summary>
    public sealed class GameContent
    {
        public long StartingCoins;
        public string StartingToolId;
        public SiteAnchor Site = new SiteAnchor();
        public ProgressionDefinition Progression = new ProgressionDefinition();
        public List<TerrainMaterialDefinition> TerrainMaterials = new List<TerrainMaterialDefinition>();
        public List<ToolDefinition> Tools = new List<ToolDefinition>();
        public List<ContractDefinition> Contracts = new List<ContractDefinition>();

        public TerrainMaterialDefinition GetTerrain(TerrainMaterial material)
        {
            foreach (TerrainMaterialDefinition t in TerrainMaterials)
            {
                if (t != null && t.Material == material) return t;
            }
            return null;
        }

        public ToolDefinition GetTool(string id)
        {
            foreach (ToolDefinition t in Tools)
            {
                if (t != null && t.Id == id) return t;
            }
            return null;
        }

        public ContractDefinition GetContract(string id)
        {
            foreach (ContractDefinition c in Contracts)
            {
                if (c != null && c.Id == id) return c;
            }
            return null;
        }
    }
}
