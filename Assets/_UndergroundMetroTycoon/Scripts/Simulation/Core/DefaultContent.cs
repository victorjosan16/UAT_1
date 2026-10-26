using System.Collections.Generic;

namespace UndergroundMetroTycoon.Simulation
{
    /// <summary>Phase 1 default balancing. Also the source for the GameConfig asset created by the editor setup.</summary>
    public static class DefaultContent
    {
        public const string BasicShovelId = "basic_shovel";
        public const string GardenTrenchId = "garden_trench";
        public const string SmallTrenchId = "small_trench";
        public const string ResidentialFoundationId = "residential_foundation";

        public static GameContent Create()
        {
            return new GameContent
            {
                StartingCoins = 0,
                StartingToolId = BasicShovelId,
                Site = new SiteAnchor { CenterX = 0f, CenterZ = 0f, SurfaceY = 0f, CellSize = 1f, LayerHeight = 0.5f },
                Progression = new ProgressionDefinition { BaseXpPerLevel = 100, GrowthPerLevel = 1.5f, MaxLevel = 50 },
                TerrainMaterials = new List<TerrainMaterialDefinition>
                {
                    Terrain(TerrainMaterial.SoftSoil, "Soft Soil", 10, 1, 5, 1),
                    Terrain(TerrainMaterial.CompactSoil, "Compact Soil", 25, 1, 8, 2),
                    Terrain(TerrainMaterial.Clay, "Clay", 45, 2, 14, 3),
                    Terrain(TerrainMaterial.Stone, "Stone", 100, 3, 30, 6),
                    Terrain(TerrainMaterial.HardRock, "Hard Rock", 250, 5, 70, 12),
                    Terrain(TerrainMaterial.ExtremeRock, "Extreme Rock", 600, 8, 160, 25)
                },
                Tools = new List<ToolDefinition>
                {
                    new ToolDefinition { Id = BasicShovelId, DisplayName = "Basic Shovel", Level = 1, Power = 5, SwingsPerSecond = 1.4f, Reach = 1.6f, PurchaseCost = 0 }
                },
                Contracts = new List<ContractDefinition>
                {
                    new ContractDefinition
                    {
                        Id = GardenTrenchId,
                        Title = "Garden Trench",
                        Description = "A neighbour needs a garden bed. Dig out 20 blocks of soft soil.",
                        Difficulty = ContractDifficulty.Beginner,
                        CoinReward = 500,
                        XpReward = 60,
                        RequiredLevel = 1,
                        SiteWidth = 5,
                        SiteLength = 4,
                        Layers = new List<ContractLayerSpec> { Layer(TerrainMaterial.SoftSoil, 0, 0, 4, 3) }
                    },
                    new ContractDefinition
                    {
                        Id = SmallTrenchId,
                        Title = "Small Trench",
                        Description = "Quick repeatable job: dig a short cable trench. Always available.",
                        Difficulty = ContractDifficulty.Beginner,
                        CoinReward = 120,
                        XpReward = 25,
                        RequiredLevel = 1,
                        PrerequisiteContractId = GardenTrenchId,
                        Repeatable = true,
                        SiteWidth = 4,
                        SiteLength = 3,
                        Layers = new List<ContractLayerSpec> { Layer(TerrainMaterial.SoftSoil, 0, 0, 3, 2) }
                    },
                    new ContractDefinition
                    {
                        Id = ResidentialFoundationId,
                        Title = "Residential Foundation",
                        Description = "Excavate a two-level foundation pit. The lower layer is compact soil.",
                        Difficulty = ContractDifficulty.Beginner,
                        CoinReward = 1200,
                        XpReward = 150,
                        RequiredLevel = 2,
                        PrerequisiteContractId = GardenTrenchId,
                        SiteWidth = 6,
                        SiteLength = 5,
                        Layers = new List<ContractLayerSpec>
                        {
                            Layer(TerrainMaterial.SoftSoil, 0, 0, 5, 4),
                            // Row z = 0 stays solid as a step so the player can climb out of the pit.
                            Layer(TerrainMaterial.CompactSoil, 0, 1, 5, 4)
                        }
                    }
                }
            };
        }

        private static TerrainMaterialDefinition Terrain(TerrainMaterial material, string name, int hp, int toolLevel, int coins, int xp) =>
            new TerrainMaterialDefinition { Material = material, DisplayName = name, MaxHp = hp, RequiredToolLevel = toolLevel, CoinValue = coins, XpValue = xp, MaterialYield = 1 };

        private static ContractLayerSpec Layer(TerrainMaterial material, int minX, int minZ, int maxX, int maxZ) =>
            new ContractLayerSpec { Material = material, MinX = minX, MinZ = minZ, MaxX = maxX, MaxZ = maxZ };
    }
}
