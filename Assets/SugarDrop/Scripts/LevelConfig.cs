using UnityEngine;

namespace SugarDrop
{
    /// <summary>
    /// Parametrii unui nivel, calculați din numărul nivelului (dificultate crescătoare, fără limită de niveluri).
    /// Aici se reglează tot echilibrul jocului.
    /// </summary>
    public readonly struct LevelConfig
    {
        public readonly int Level;
        public readonly int Columns;
        public readonly int Rows;
        public readonly int Colors;
        public readonly int Moves;
        public readonly int Candies;
        public readonly int Goal;

        private LevelConfig(int level, int columns, int rows, int colors, int moves, int candies, int goal)
        {
            Level = level;
            Columns = columns;
            Rows = rows;
            Colors = colors;
            Moves = moves;
            Candies = candies;
            Goal = goal;
        }

        public static LevelConfig For(int level)
        {
            level = Mathf.Max(1, level);
            // Mai multe culori = mai greu de aliniat.
            int colors = level < 3 ? 3 : level < 8 ? 4 : 5;
            // Mai puține mutări pe măsură ce avansezi (dar niciodată sub 10).
            int moves = Mathf.Max(10, 18 - level / 3);
            int candies = Mathf.Min(140, 50 + level * 6);
            // Procentul cerut crește de la 55% la 75%.
            float share = Mathf.Lerp(0.55f, 0.75f, Mathf.Clamp01((level - 1) / 25f));
            int goal = Mathf.RoundToInt(candies * share);
            return new LevelConfig(level, 8, 8, colors, moves, candies, goal);
        }
    }
}
