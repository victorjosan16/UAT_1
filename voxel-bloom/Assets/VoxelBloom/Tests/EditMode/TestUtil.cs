using System.Collections.Generic;
using System.IO;
using System.Text;
using VoxelBloom.Core;

namespace VoxelBloom.Tests
{
    static class TestUtil
    {
        public const string DataDir = "Assets/VoxelBloom/Data/Resources/VoxelBloom";

        /// <summary>3x3x3 cube: colour 0 shell (26), colour 1 centre (1). Columns: index 0 = top block.</summary>
        public static LevelDefinition Cube(params int[][][] columns) => Shape(3, 3, 3, Cells333(), columns);

        static string Cells333()
        {
            var sb = new StringBuilder();
            for (int z = 0; z < 3; z++) for (int y = 0; y < 3; y++) for (int x = 0; x < 3; x++) sb.Append(x == 1 && y == 1 && z == 1 ? '1' : '0');
            return sb.ToString();
        }

        public static LevelDefinition Shape(int x, int y, int z, string voxels, int[][][] columns)
        {
            var cols = new AttackBlock[GameSession.Columns][];
            for (int c = 0; c < GameSession.Columns; c++)
            {
                var src = c < columns.Length ? columns[c] : new int[0][];
                cols[c] = new AttackBlock[src.Length];
                for (int i = 0; i < src.Length; i++) cols[c][i] = new AttackBlock(src[i][0], src[i][1]);
            }
            return new LevelDefinition
            {
                Id = 1, Name = "Shape", Grid = new VoxelGrid(x, y, z, new[] { "#FF0000", "#00FF00" }, VoxelGrid.Decode(voxels)),
                Columns = cols, StarScoreThree = 100, StarScoreTwo = 50, RewardCoins = 50,
            };
        }

        public static int[][] Col(params int[] colorCountPairs)
        {
            var r = new int[colorCountPairs.Length / 2][];
            for (int i = 0; i < r.Length; i++) r[i] = new[] { colorCountPairs[2 * i], colorCountPairs[2 * i + 1] };
            return r;
        }

        public static int Idx(int x, int y, int z) => x + 3 * (y + 3 * z);

        public static GameConfig Config() => GameConfig.FromJson(File.ReadAllText(Path.Combine(DataDir, "Config/game_config.json")));

        public static LevelDefinition Level(int id) => LevelDefinition.FromJson(File.ReadAllText(Path.Combine(DataDir, "Levels/level_" + id.ToString("000") + ".json")));
    }
}
