namespace SugarDrop
{
    /// <summary>
    /// Punctajul și pragurile de stele. Grupurile mari dau bonus, piesele speciale și combinațiile și mai mult.
    /// Victoria dă mereu cel puțin o stea; a doua și a treia vin din scor.
    /// </summary>
    public static class ScoreRules
    {
        public const int TilePoints = 20;
        public const int SpecialCreated = 60;
        public const int SpecialActivated = 100;
        public const int ComboBonus = 300;
        public const int Obstacle = 40;
        public const int Frosting = 50;
        public const int Ingredient = 500;
        /// <summary>Fiecare mutare rămasă la final (devine capac special care explodează).</summary>
        public const int FinaleMove = 200;

        /// <summary>Bonusul unui grup mare (crește repede cu mărimea: 5 → 40, 7 → 160, 10 → 490).</summary>
        public static int GroupBonus(int size) => size <= 3 ? 0 : (size - 3) * (size - 3) * 10;

        /// <summary>Punctele unui pas de pe tablă (aceleași reguli în joc și în simulatorul de echilibru).</summary>
        public static int PointsFor(BoardStep step)
        {
            int points = 0;
            foreach (PieceInfo piece in step.Cleared)
                points += piece.Kind == TileKind.Crate || piece.Kind == TileKind.Chocolate ? Obstacle : TilePoints;
            points += GroupBonus(step.GroupSize);
            points += step.Activations.Count * SpecialActivated + (step.Combo ? ComboBonus : 0);
            points += step.ObstacleHits.Count * Obstacle;
            points += step.FrostingCells.Count * Frosting;
            points += step.Exits.Count * Ingredient;
            if (step.Kind == StepKind.Clear) points += step.Changed.Count * SpecialCreated;
            return points;
        }

        public static int TwoStars(LevelConfig config) =>
            config.Type == LevelType.Score ? config.ScoreTarget * 3 / 2 : config.Moves * 170 + config.Ingredients * Ingredient;

        public static int ThreeStars(LevelConfig config) =>
            config.Type == LevelType.Score ? config.ScoreTarget * 11 / 5 : TwoStars(config) * 8 / 5;

        public static int Stars(int score, LevelConfig config) =>
            score >= ThreeStars(config) ? 3 : score >= TwoStars(config) ? 2 : 1;
    }
}
