using System.Collections.Generic;

namespace UndergroundMetroTycoon.Simulation
{
    /// <summary>Checks configuration for mistakes that would break gameplay (including soft-locks).</summary>
    public static class ContentValidator
    {
        public const int MaxSiteSize = 16;
        public const int MaxLayers = 8;

        public static List<string> Validate(GameContent content)
        {
            var errors = new List<string>();
            if (content == null)
            {
                errors.Add("Content is null.");
                return errors;
            }

            if (content.StartingCoins < 0) errors.Add("Starting coins cannot be negative.");
            if (content.Site == null || content.Site.CellSize <= 0f || content.Site.LayerHeight <= 0f) errors.Add("Site cell size and layer height must be positive.");
            if (content.Progression == null || content.Progression.BaseXpPerLevel <= 0 || content.Progression.MaxLevel < 1) errors.Add("Progression settings are invalid.");

            var materials = new HashSet<TerrainMaterial>();
            foreach (TerrainMaterialDefinition t in content.TerrainMaterials)
            {
                if (t == null) { errors.Add("Null terrain material entry."); continue; }
                if (!materials.Add(t.Material)) errors.Add($"Duplicate terrain material {t.Material}.");
                if (t.MaxHp <= 0) errors.Add($"Terrain {t.Material} must have positive HP.");
                if (t.CoinValue < 0 || t.XpValue < 0) errors.Add($"Terrain {t.Material} rewards cannot be negative.");
            }

            var toolIds = new HashSet<string>();
            foreach (ToolDefinition tool in content.Tools)
            {
                if (tool == null || string.IsNullOrEmpty(tool.Id)) { errors.Add("Tool without id."); continue; }
                if (!toolIds.Add(tool.Id)) errors.Add($"Duplicate tool id {tool.Id}.");
                if (tool.Power <= 0 || tool.SwingsPerSecond <= 0f || tool.Reach <= 0f) errors.Add($"Tool {tool.Id} needs positive power, speed and reach.");
            }

            ToolDefinition startTool = content.GetTool(content.StartingToolId);
            if (startTool == null) errors.Add($"Starting tool '{content.StartingToolId}' is not defined.");

            var contractIds = new HashSet<string>();
            foreach (ContractDefinition c in content.Contracts)
            {
                if (c == null || string.IsNullOrEmpty(c.Id)) { errors.Add("Contract without id."); continue; }
                if (!contractIds.Add(c.Id)) errors.Add($"Duplicate contract id {c.Id}.");
            }

            bool hasStarter = false, hasRepeatable = false;
            foreach (ContractDefinition c in content.Contracts)
            {
                if (c == null || string.IsNullOrEmpty(c.Id)) continue;
                if (c.SiteWidth < 1 || c.SiteLength < 1 || c.SiteWidth > MaxSiteSize || c.SiteLength > MaxSiteSize) errors.Add($"Contract {c.Id} site size is out of range.");
                if (c.Layers == null || c.Layers.Count == 0 || c.Layers.Count > MaxLayers) { errors.Add($"Contract {c.Id} needs 1-{MaxLayers} layers."); continue; }
                if (c.CoinReward < 0 || c.XpReward < 0) errors.Add($"Contract {c.Id} rewards cannot be negative.");
                if (!string.IsNullOrEmpty(c.PrerequisiteContractId) && !contractIds.Contains(c.PrerequisiteContractId)) errors.Add($"Contract {c.Id} has unknown prerequisite {c.PrerequisiteContractId}.");

                bool layersValid = true;
                foreach (ContractLayerSpec layer in c.Layers)
                {
                    if (layer == null || layer.MinX < 0 || layer.MinZ < 0 || layer.MaxX >= c.SiteWidth || layer.MaxZ >= c.SiteLength || layer.MinX > layer.MaxX || layer.MinZ > layer.MaxZ)
                    {
                        errors.Add($"Contract {c.Id} has a layer rectangle outside its site.");
                        layersValid = false;
                    }
                    else if (content.GetTerrain(layer.Material) == null)
                    {
                        errors.Add($"Contract {c.Id} uses unconfigured terrain {layer.Material}.");
                        layersValid = false;
                    }
                }
                if (!layersValid) continue;

                if (content.Site != null && content.Site.LayerHeight > KinematicMover.DefaultMaxStepUp)
                    errors.Add($"Layer height {content.Site.LayerHeight} is taller than the walkable step {KinematicMover.DefaultMaxStepUp}.");
                else if (content.Site != null && !IsFinishedPitEscapable(c, content.Site.LayerHeight))
                    errors.Add($"Contract {c.Id}: the finished pit has no walkable way out (add a step row like residential_foundation).");

                bool diggableWithStartTool = startTool != null && IsDiggable(c, content, startTool);
                if (diggableWithStartTool && c.RequiredLevel <= 1 && string.IsNullOrEmpty(c.PrerequisiteContractId)) hasStarter = true;
                if (diggableWithStartTool && c.Repeatable && c.RequiredLevel <= 1) hasRepeatable = true;
            }

            if (!hasStarter) errors.Add("No starter contract can be completed at level 1 with the starting tool.");
            if (!hasRepeatable) errors.Add("No repeatable contract is diggable with the starting tool; players could get stuck.");
            return errors;
        }

        /// <summary>
        /// Flood-fills the finished pit from the surrounding ground, moving only across steps the player can climb.
        /// Every dug column must be reachable, otherwise a digger could end up trapped.
        /// </summary>
        private static bool IsFinishedPitEscapable(ContractDefinition c, float layerHeight)
        {
            int w = c.SiteWidth, l = c.SiteLength;
            var depth = new int[w, l];
            for (int x = 0; x < w; x++)
                for (int z = 0; z < l; z++)
                {
                    int d = 0;
                    while (d < c.Layers.Count && c.Layers[d].Contains(x, z)) d++;
                    depth[x, z] = d;
                }

            var reached = new bool[w, l];
            var queue = new Queue<(int x, int z)>();
            for (int x = 0; x < w; x++)
                for (int z = 0; z < l; z++)
                {
                    bool onEdge = x == 0 || z == 0 || x == w - 1 || z == l - 1;
                    if (onEdge && depth[x, z] * layerHeight <= KinematicMover.DefaultMaxStepUp + 1e-4f)
                    {
                        reached[x, z] = true;
                        queue.Enqueue((x, z));
                    }
                }

            int[] dx = { 1, -1, 0, 0 }, dz = { 0, 0, 1, -1 };
            while (queue.Count > 0)
            {
                (int x, int z) = queue.Dequeue();
                for (int i = 0; i < 4; i++)
                {
                    int nx = x + dx[i], nz = z + dz[i];
                    if (nx < 0 || nz < 0 || nx >= w || nz >= l || reached[nx, nz]) continue;
                    // Moving from the deeper neighbour up to (x, z) must be a climbable step.
                    if ((depth[nx, nz] - depth[x, z]) * layerHeight > KinematicMover.DefaultMaxStepUp + 1e-4f) continue;
                    reached[nx, nz] = true;
                    queue.Enqueue((nx, nz));
                }
            }

            for (int x = 0; x < w; x++)
                for (int z = 0; z < l; z++)
                    if (depth[x, z] > 0 && !reached[x, z]) return false;
            return true;
        }

        private static bool IsDiggable(ContractDefinition c, GameContent content, ToolDefinition tool)
        {
            foreach (ContractLayerSpec layer in c.Layers)
            {
                TerrainMaterialDefinition t = content.GetTerrain(layer.Material);
                if (t == null || t.RequiredToolLevel > tool.Level) return false;
            }
            return true;
        }
    }
}
