using System;
using System.Collections.Generic;

namespace UndergroundMetroTycoon.Simulation
{
    public enum ContractDifficulty
    {
        Beginner = 0,
        Intermediate = 1,
        Advanced = 2
    }

    /// <summary>One terrain layer of a contract site. Cells inside the rectangle belong to the contract; the rest stays solid.</summary>
    [Serializable]
    public sealed class ContractLayerSpec
    {
        public TerrainMaterial Material;
        public int MinX;
        public int MinZ;
        public int MaxX;
        public int MaxZ;

        public bool Contains(int x, int z) => x >= MinX && x <= MaxX && z >= MinZ && z <= MaxZ;
        public int CellCount => Math.Max(0, MaxX - MinX + 1) * Math.Max(0, MaxZ - MinZ + 1);
    }

    [Serializable]
    public sealed class ContractDefinition
    {
        public string Id;
        public string Title;
        public string Description;
        public ContractDifficulty Difficulty;
        public long CoinReward;
        public int XpReward;
        public int RequiredLevel = 1;
        public string PrerequisiteContractId;
        public bool Repeatable;
        public int SiteWidth = 5;
        public int SiteLength = 4;
        public List<ContractLayerSpec> Layers = new List<ContractLayerSpec>();

        public int CountContractCells()
        {
            int total = 0;
            if (Layers == null) return 0;
            foreach (ContractLayerSpec layer in Layers)
            {
                int minX = Math.Max(0, layer.MinX), maxX = Math.Min(SiteWidth - 1, layer.MaxX);
                int minZ = Math.Max(0, layer.MinZ), maxZ = Math.Min(SiteLength - 1, layer.MaxZ);
                if (maxX >= minX && maxZ >= minZ) total += (maxX - minX + 1) * (maxZ - minZ + 1);
            }
            return total;
        }
    }
}
