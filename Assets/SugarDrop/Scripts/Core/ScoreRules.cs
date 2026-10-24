namespace SugarDrop
{
    /// <summary>
    /// Punctajul și pragurile de stele. Victoria (obiectivul din borcan) dă mereu cel puțin o stea;
    /// a doua și a treia vin din scor. Valorile sunt alese așa încât 2–3 stele să ceară un joc bun.
    /// </summary>
    public static class ScoreRules
    {
        public const int TilePoints = 20;
        /// <summary>Puncte în plus pentru fiecare capac dintr-un lanț lung (a câta piesă din mutare).</summary>
        public const int ChainBonus = 5;
        public const int MaxChainBonus = 100;
        public const int SpecialCreated = 60;
        public const int SpecialActivated = 100;
        public const int Obstacle = 40;
        public const int CandyInJar = 10;
        /// <summary>Fiecare mutare rămasă la final (devine capac special care explodează).</summary>
        public const int FinaleMove = 200;

        public static int TileValue(int chainIndex) =>
            TilePoints + System.Math.Min(MaxChainBonus, chainIndex * ChainBonus);

        public static int TwoStars(LevelConfig config) =>
            config.Columns * config.Rows / 2 * TilePoints + config.Goal * CandyInJar + config.Moves * 120;

        public static int ThreeStars(LevelConfig config) => TwoStars(config) * 8 / 5;

        public static int Stars(int score, LevelConfig config) =>
            score >= ThreeStars(config) ? 3 : score >= TwoStars(config) ? 2 : 1;
    }
}
