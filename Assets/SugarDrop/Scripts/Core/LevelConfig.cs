using UnityEngine;

namespace SugarDrop
{
    /// <summary>
    /// Parametrii unui nivel, calculați din numărul nivelului (dificultate crescătoare, fără limită de niveluri).
    /// Aici se reglează tot echilibrul jocului. Fiecare al 10-lea nivel e „Boss”: mai multe bomboane și obstacole,
    /// dar și mai multe mutări și o recompensă dublă.
    /// </summary>
    public readonly struct LevelConfig
    {
        public const int BossEvery = 10;

        public readonly int Level;
        public readonly int Columns;
        public readonly int Rows;
        public readonly int Colors;
        public readonly int Moves;
        public readonly int Candies;
        public readonly int Goal;
        /// <summary>Câți biscuiți (blocuri care nu se mută și opresc bomboanele).</summary>
        public readonly int Biscuits;
        /// <summary>Câte lovituri îi trebuie unui biscuit ca să se spargă.</summary>
        public readonly int BiscuitHits;
        /// <summary>Câte capace înghețate (nu se mută până nu le spargi gheața cu o aliniere).</summary>
        public readonly int Frozen;
        public readonly bool IsBoss;

        private LevelConfig(int level, int colors, int moves, int candies, int goal, int biscuits, int biscuitHits, int frozen, bool boss)
        {
            Level = level;
            Columns = 8;
            Rows = 8;
            Colors = colors;
            Moves = moves;
            Candies = candies;
            Goal = goal;
            Biscuits = biscuits;
            BiscuitHits = biscuitHits;
            Frozen = frozen;
            IsBoss = boss;
        }

        public static bool IsBossLevel(int level) => level > 0 && level % BossEvery == 0;

        public static LevelConfig For(int level)
        {
            level = Mathf.Max(1, level);
            bool boss = IsBossLevel(level);

            // Mai multe culori = mai greu de aliniat.
            int colors = level < 3 ? 3 : level < 8 ? 4 : 5;
            // Mai puține mutări pe măsură ce avansezi (dar niciodată sub 10).
            int moves = Mathf.Max(10, 18 - level / 3);
            // Bomboane mai mari și mai puține (se văd bine pe telefon și încap în zona de sus).
            int candies = Mathf.Min(84, 34 + level * 4);
            // Procentul cerut crește de la 55% la 75%.
            float share = Mathf.Lerp(0.55f, 0.75f, Mathf.Clamp01((level - 1) / 25f));

            // Obstacole: biscuiți de la nivelul 4, gheață de la nivelul 6, biscuiți tari (2 lovituri) de la 15.
            int biscuits = level < 4 ? 0 : Mathf.Min(8, 2 + (level - 4) / 3);
            int frozen = level < 6 ? 0 : Mathf.Min(10, 2 + (level - 6) / 2);
            int biscuitHits = level >= 15 ? 2 : 1;

            if (boss)
            {
                candies = Mathf.Min(92, candies + 14);
                moves += 5;
                biscuits = Mathf.Min(10, biscuits + 2);
                frozen = Mathf.Min(14, frozen + 3);
            }

            int goal = Mathf.RoundToInt(candies * share);
            return new LevelConfig(level, colors, moves, candies, goal, biscuits, biscuitHits, frozen, boss);
        }
    }
}
