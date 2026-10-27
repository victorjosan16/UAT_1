using System.Collections.Generic;

namespace SugarDrop
{
    public enum GoalKind { Ingredients, Collect, Frosting, Score, Boss, Crates, Ice, Chains, Chocolate, Specials, Flowers, TimeBombs }

    /// <summary>Câte obstacole are tabla (la start), pentru țintele obiectivelor cu obstacole.</summary>
    public struct ObstacleCounts
    {
        public int Crates;
        public int IceLayers;
        public int Chains;
        public int Chocolate;
        public int Pots;
    }

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

        /// <summary>Scorul cerut dacă un nivel cu obstacole n-a primit niciun obstacol (tablă prea mică).</summary>
        public const int FallbackScore = 6400;

        /// <param name="bossHealth">Viața Boss-ului (0 dacă nivelul nu e o luptă).</param>
        /// <param name="obstacles">Obstacolele de pe tablă la start (țintele nivelurilor cu lăzi, gheață, lanțuri, ciocolată).</param>
        public LevelGoals(LevelConfig config, int frostingLayers, int bossHealth = 0, ObstacleCounts obstacles = default)
        {
            if (bossHealth > 0) Add(GoalKind.Boss, -1, bossHealth);
            if (config.Ingredients > 0) Add(GoalKind.Ingredients, -1, config.Ingredients);
            for (int i = 0; i < config.CollectColors.Length; i++) Add(GoalKind.Collect, config.CollectColors[i], config.CollectCounts[i]);
            if (frostingLayers > 0) Add(GoalKind.Frosting, -1, frostingLayers);
            switch (config.Type)
            {
                case LevelType.Crates when obstacles.Crates > 0: Add(GoalKind.Crates, -1, obstacles.Crates); break;
                case LevelType.Ice when obstacles.IceLayers > 0: Add(GoalKind.Ice, -1, obstacles.IceLayers); break;
                case LevelType.Chains when obstacles.Chains > 0: Add(GoalKind.Chains, -1, obstacles.Chains); break;
                case LevelType.Chocolate when obstacles.Chocolate > 0: Add(GoalKind.Chocolate, -1, obstacles.Chocolate); break;
                case LevelType.Garden when obstacles.Pots > 0: Add(GoalKind.Flowers, -1, obstacles.Pots); break;
                case LevelType.Defuse when config.TimeBombGoal > 0: Add(GoalKind.TimeBombs, -1, config.TimeBombGoal); break;
            }
            if (config.SpecialsTarget > 0) Add(GoalKind.Specials, -1, config.SpecialsTarget);
            if (config.ScoreTarget > 0) Add(GoalKind.Score, -1, config.ScoreTarget);
            else if (Items.Count == 0 && config.Type != LevelType.Boss && config.Type != LevelType.Bonus) Add(GoalKind.Score, -1, FallbackScore);
        }

        private void Add(GoalKind kind, int color, int target) =>
            Items.Add(new GoalItem { Kind = kind, Color = color, Target = target, Remaining = target });

        /// <summary>Toate obiectivele îndeplinite (nivelul bonus nu are obiective: se termină când expiră timpul).</summary>
        public bool IsComplete
        {
            get
            {
                if (Items.Count == 0) return false;
                foreach (GoalItem item in Items)
                    if (!item.Done) return false;
                return true;
            }
        }

        /// <summary>O bomboană colorată a dispărut. Întoarce true dacă un obiectiv s-a schimbat.</summary>
        public bool OnPieceCleared(int color, TileKind kind)
        {
            if (color < 0 || kind != TileKind.Normal)
                return false;
            return Decrease(GoalKind.Collect, color);
        }

        /// <summary>Aplică un pas de pe tablă (piese eliminate, glazură topită, bomboane ajunse în borcan).</summary>
        public bool Apply(BoardStep step)
        {
            bool changed = false;
            foreach (PieceInfo piece in step.Cleared)
            {
                changed |= OnPieceCleared(piece.Color, piece.Kind);
                if (piece.Kind == TileKind.Crate) changed |= Decrease(GoalKind.Crates, -1);
                else if (piece.Kind == TileKind.Chocolate) changed |= Decrease(GoalKind.Chocolate, -1);
                else if (piece.Kind == TileKind.Pot) changed |= Decrease(GoalKind.Flowers, -1);
                if (piece.Timer > 0) changed |= Decrease(GoalKind.TimeBombs, -1);
            }
            for (int i = 0; i < step.IceLayersBroken; i++) changed |= Decrease(GoalKind.Ice, -1);
            for (int i = 0; i < step.ChainsBroken; i++) changed |= Decrease(GoalKind.Chains, -1);
            for (int i = 0; i < step.Activations.Count; i++) changed |= Decrease(GoalKind.Specials, -1);
            for (int i = 0; i < step.FrostingCells.Count; i++) changed |= OnFrostingCleared();
            for (int i = 0; i < step.Exits.Count; i++) changed |= OnIngredientCollected();
            return changed;
        }

        /// <summary>Viața rămasă a Boss-ului (obiectivul e îndeplinit când ajunge la 0).</summary>
        public bool OnBossHealth(int health)
        {
            bool changed = false;
            foreach (GoalItem item in Items)
            {
                if (item.Kind != GoalKind.Boss || item.Remaining == health) continue;
                item.Remaining = System.Math.Max(0, health);
                changed = true;
            }
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
