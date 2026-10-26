using System;

namespace UndergroundMetroTycoon.Simulation
{
    /// <summary>Terrain layers, ordered from softest to hardest.</summary>
    public enum TerrainMaterial
    {
        SoftSoil = 0,
        CompactSoil = 1,
        Clay = 2,
        Stone = 3,
        HardRock = 4,
        ExtremeRock = 5
    }

    /// <summary>Balancing data for one terrain material. Configurable through GameConfig.</summary>
    [Serializable]
    public sealed class TerrainMaterialDefinition
    {
        public TerrainMaterial Material;
        public string DisplayName;
        public int MaxHp = 10;
        public int RequiredToolLevel = 1;
        public int CoinValue = 1;
        public int XpValue = 1;
        public int MaterialYield = 1;
    }
}
