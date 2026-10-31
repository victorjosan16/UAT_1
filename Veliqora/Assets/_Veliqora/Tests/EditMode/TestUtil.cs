using System;
using System.IO;
using Veliqora.Core.Levels;
using Veliqora.Core.Puzzle;

namespace Veliqora.Tests.EditMode
{
    internal static class TestUtil
    {
        public const string LevelsRelativePath = "Assets/_Veliqora/Resources/Veliqora/Levels/levels.json";
        public const string EconomyRelativePath = "Assets/_Veliqora/Resources/Veliqora/Config/economy.json";

        /// <summary>Finds a project-relative file both inside Unity (cwd = project root) and under dotnet test.</summary>
        public static string FindProjectFile(string relative)
        {
            string direct = Path.Combine(Directory.GetCurrentDirectory(), relative);
            if (File.Exists(direct)) return direct;
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                string candidate = Path.Combine(dir.FullName, relative);
                if (File.Exists(candidate)) return candidate;
                dir = dir.Parent;
            }
            throw new FileNotFoundException("Could not locate " + relative);
        }

        private static LevelCatalog _catalog;
        public static LevelCatalog Catalog => _catalog ?? (_catalog = LevelCatalog.FromJson(File.ReadAllText(FindProjectFile(LevelsRelativePath))));

        public static GameState S(int capacity, params int[][] bottles) => GameState.FromArrays(capacity, bottles);
        public static int[] B(params int[] units) => units;

        public static LevelDefinition MakeLevel(int number, int capacity, params int[][] bottles)
        {
            var state = GameState.FromArrays(capacity, bottles);
            var solve = Veliqora.Core.Solver.PuzzleSolver.Solve(state);
            return new LevelDefinition
            {
                Number = number,
                Seed = 42,
                Capacity = capacity,
                ColorCount = LevelGenerator.CountColors(bottles),
                Bottles = bottles,
                ParMoves = solve.Length,
                Solution = LevelDefinition.EncodeMoves(solve.Moves),
                Tier = DifficultyTiers.ForLevel(number),
            };
        }
    }
}
