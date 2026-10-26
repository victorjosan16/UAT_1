using System.Collections.Generic;
using UndergroundMetroTycoon.Simulation;
using UnityEngine;

namespace UndergroundMetroTycoon.Game
{
    /// <summary>Designer-editable balancing data. Converted to <see cref="GameContent"/> when the game starts.</summary>
    [CreateAssetMenu(menuName = "Underground Metro Tycoon/Game Config", fileName = "GameConfig")]
    public sealed class GameConfig : ScriptableObject
    {
        public long StartingCoins;
        public string StartingToolId = DefaultContent.BasicShovelId;
        public SiteAnchor Site = new SiteAnchor();
        public ProgressionDefinition Progression = new ProgressionDefinition();
        public List<TerrainMaterialDefinition> TerrainMaterials = new List<TerrainMaterialDefinition>();
        public List<ToolDefinition> Tools = new List<ToolDefinition>();
        public List<ContractDefinition> Contracts = new List<ContractDefinition>();

        public GameContent ToContent()
        {
            return new GameContent
            {
                StartingCoins = StartingCoins,
                StartingToolId = StartingToolId,
                Site = Site,
                Progression = Progression,
                TerrainMaterials = new List<TerrainMaterialDefinition>(TerrainMaterials),
                Tools = new List<ToolDefinition>(Tools),
                Contracts = new List<ContractDefinition>(Contracts)
            };
        }

        [ContextMenu("Reset To Phase 1 Defaults")]
        public void ResetToDefaults()
        {
            GameContent d = DefaultContent.Create();
            StartingCoins = d.StartingCoins;
            StartingToolId = d.StartingToolId;
            Site = d.Site;
            Progression = d.Progression;
            TerrainMaterials = d.TerrainMaterials;
            Tools = d.Tools;
            Contracts = d.Contracts;
        }
    }
}
