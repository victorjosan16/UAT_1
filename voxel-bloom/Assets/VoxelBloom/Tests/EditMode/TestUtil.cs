using System.Collections.Generic;
using System.IO;
using System.Text;
using VoxelBloom.Core;

namespace VoxelBloom.Tests
{
    static class TestUtil
    {
        public const string DataDir = "Assets/VoxelBloom/Data/Resources/VoxelBloom";

        /// <summary>3x3x3 cube: colour 0 shell, colour 1 centre.</summary>
        public static LevelDefinition Cube(int[][] deck, int queueSize = 5, int moveLimit = 999)
        {
            var sb = new StringBuilder();
            for (int z = 0; z < 3; z++) for (int y = 0; y < 3; y++) for (int x = 0; x < 3; x++) sb.Append(x == 1 && y == 1 && z == 1 ? '1' : '0');
            var d = new List<AttackBlock>();
            foreach (var p in deck) d.Add(new AttackBlock(p[0], p[1]));
            return new LevelDefinition
            {
                Id = 1, Name = "Cube", Grid = new VoxelGrid(3, 3, 3, new[] { "#FF0000", "#00FF00" }, VoxelGrid.Decode(sb.ToString())),
                Deck = d.ToArray(), QueueSize = queueSize, MoveLimit = moveLimit, StarsThree = 2, StarsTwo = 4, RewardCoins = 50, VoxelCount = 27,
            };
        }

        public static int Idx(int x, int y, int z) => x + 3 * (y + 3 * z);

        public static int[][] Repeat(int color, int power, int n)
        {
            var r = new int[n][];
            for (int i = 0; i < n; i++) r[i] = new[] { color, power };
            return r;
        }

        public static GameConfig Config() => GameConfig.FromJson(File.ReadAllText(Path.Combine(DataDir, "Config/game_config.json")));

        public static LevelDefinition Level(int id) => LevelDefinition.FromJson(File.ReadAllText(Path.Combine(DataDir, "Levels/level_" + id.ToString("000") + ".json")));
    }
}
