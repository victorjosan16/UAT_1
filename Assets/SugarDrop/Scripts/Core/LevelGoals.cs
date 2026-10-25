using System.Collections.Generic;

namespace SugarDrop
{
    public enum GoalKind { Ingredients, Collect, Frosting, Score }

    /// <summary>Un obiectiv din HUD: ce (și ce culoare), cât trebuie și cât a mai rămas.</summary>
    public sealed class GoalItem
    {
        public GoalKind Kind;
        public int Color = -1;
        public int Target;
        public int Remaining;
        public bool Done => Remaining <= 0;
    }

    /// <summary>Obiectivele nivelului și progresul lor (pur, testabil).</summary>
    public sealed class LevelGoals
    {
        public readonly List<GoalItem> Items = new();

        public LevelGoals(LevelConfig config, int frostingLayers)
        {
            if (config.Ingredients > 0) Add(GoalKind.Ingredients, -1, config.Ingredients);
            for (int i = 0; i < config.CollectColors.Length; i++) Add(GoalKind.Collect, config.CollectColors[i], config.CollectCounts[i]);
            if (frostingLayers > 0) Add(GoalKind.Frosting, -1, frostingLayers);
            if (config.ScoreTarget > 0 || Items.Count == 0) Add(GoalKind.Score, -1, System.Math.Max(1, config.ScoreTarget));
        }

        private void Add(GoalKind kind, int color, int target) =>
            Items.Add(new GoalItem { Kind = kind, Color = color, Target = target, Remaining = target });

        public bool IsComplete
        {
            get
            {
                foreach (GoalItem item in Items)
                    if (!item.Done) return false;
                return true;
            }
        }

        /// <summary>O piesă colorată a dispărut (inclusiv cele speciale). Întoarce true dacă un obiectiv s-a schimbat.</summary>
        public bool OnPieceCleared(int color, TileKind kind)
        {
            if (color < 0 || kind == TileKind.Rainbow || kind == TileKind.Biscuit || kind == TileKind.Ingredient || kind == TileKind.Chocolate)
                return false;
            return Decrease(GoalKind.Collect, color);
        }

        /// <summary>Aplică un pas de pe tablă (piese eliminate, glazură topită, bomboane ajunse în borcan).</summary>
        public bool Apply(BoardStep step)
        {
            bool changed = false;
            foreach (PieceInfo piece in step.Cleared) changed |= OnPieceCleared(piece.Color, piece.Kind);
            for (int i = 0; i < step.FrostingCells.Count; i++) changed |= OnFrostingCleared();
            for (int i = 0; i < step.Exits.Count; i++) changed |= OnIngredientCollected();
            return changed;
        }

        public bool OnFrostingCleared() => Decrease(GoalKind.Frosting, -1);
        public bool OnIngredientCollected() => Decrease(GoalKind.Ingredients, -1);

        public bool OnScore(int score)
        {
            bool changed = false;
            foreach (GoalItem item in Items)
            {
                if (item.Kind != GoalKind.Score) continue;
                int remaining = System.Math.Max(0, item.Target - score);
                changed |= remaining != item.Remaining;
                item.Remaining = remaining;
            }
            return changed;
        }

        private bool Decrease(GoalKind kind, int color)
        {
            foreach (GoalItem item in Items)
            {
                if (item.Kind != kind || item.Color != color || item.Done) continue;
                item.Remaining--;
                return true;
            }
            return false;
        }
    }
}
