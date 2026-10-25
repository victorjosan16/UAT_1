namespace SugarDrop
{
    /// <summary>
    /// Punctajul și pragurile de stele. Fiecare cascadă înmulțește punctele pieselor (×2, ×3…).
    /// Victoria dă mereu cel puțin o stea; a doua și a treia vin din scor.
    /// </summary>
    public static class ScoreRules
    {
        public const int TilePoints = 20;
        public const int MaxCascadeMultiplier = 6;
        public const int SpecialCreated = 60;
        public const int SpecialActivated = 100;
        public const int Obstacle = 40;
        public const int Frosting = 50;
        public const int Ingredient = 500;
        /// <summary>Fiecare mutare rămasă la final (devine capac special care explodează).</summary>
        public const int FinaleMove = 200;

        /// <summary>Punctele unei piese eliminate în cascada dată (0 = mutarea jucătorului).</summary>
        public static int TileValue(int cascade) =>
            TilePoints * System.Math.Min(MaxCascadeMultiplier, 1 + System.Math.Max(0, cascade));

        /// <summary>Punctele unui pas de pe tablă (aceleași reguli în joc și în simulatorul de echilibru).</summary>
        public static int PointsFor(BoardStep step)
        {
            int points = 0;
            foreach (PieceInfo piece in step.Cleared)
                points += piece.Kind == TileKind.Biscuit || piece.Kind == TileKind.Chocolate ? Obstacle : TileValue(step.Cascade);
            points += step.Activations.Count * SpecialActivated;
            points += (step.BlockerHits.Count + step.Unfrozen.Count) * Obstacle;
            points += step.FrostingCells.Count * Frosting;
            points += step.Exits.Count * Ingredient;
            if (step.Kind == StepKind.Clear) points += step.Changed.Count * SpecialCreated;
            return points;
        }

        public static int TwoStars(LevelConfig config) =>
            config.Type == LevelType.Score ? config.ScoreTarget * 3 / 2 : config.Moves * 150 + config.Ingredients * Ingredient;

        public static int ThreeStars(LevelConfig config) =>
            config.Type == LevelType.Score ? config.ScoreTarget * 11 / 5 : TwoStars(config) * 8 / 5;

        public static int Stars(int score, LevelConfig config) =>
            score >= ThreeStars(config) ? 3 : score >= TwoStars(config) ? 2 : 1;
    }
}
