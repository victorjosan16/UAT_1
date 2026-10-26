using System;

namespace UndergroundMetroTycoon.Simulation
{
    [Serializable]
    public sealed class ProgressionDefinition
    {
        public int BaseXpPerLevel = 100;
        public float GrowthPerLevel = 1.5f;
        public int MaxLevel = 50;
    }

    /// <summary>Experience and level. Level is always derived from total XP, so it cannot drift out of sync.</summary>
    public sealed class PlayerProgression
    {
        private const int XpCap = int.MaxValue / 2;
        private readonly ProgressionDefinition _definition;

        public PlayerProgression(ProgressionDefinition definition, int totalXp = 0)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            TotalXp = Math.Max(0, Math.Min(XpCap, totalXp));
            Recompute();
        }

        public int TotalXp { get; private set; }
        public int Level { get; private set; }
        public int XpIntoLevel { get; private set; }
        public int XpForNextLevel => IsMaxLevel ? 0 : XpRequiredForLevel(Level);
        public bool IsMaxLevel => Level >= _definition.MaxLevel;

        /// <summary>XP needed to advance from <paramref name="level"/> to the next level.</summary>
        public int XpRequiredForLevel(int level)
        {
            double value = Math.Max(1, _definition.BaseXpPerLevel) * Math.Pow(Math.Max(1f, _definition.GrowthPerLevel), Math.Max(0, level - 1));
            return (int)Math.Min(XpCap, Math.Round(value));
        }

        /// <summary>Adds XP and returns how many levels were gained.</summary>
        public int AddXp(int amount)
        {
            if (amount <= 0) return 0;
            int previousLevel = Level;
            TotalXp = (int)Math.Min(XpCap, (long)TotalXp + amount);
            Recompute();
            return Level - previousLevel;
        }

        private void Recompute()
        {
            int level = 1;
            int remaining = TotalXp;
            int maxLevel = Math.Max(1, _definition.MaxLevel);
            while (level < maxLevel)
            {
                int needed = XpRequiredForLevel(level);
                if (remaining < needed) break;
                remaining -= needed;
                level++;
            }
            Level = level;
            XpIntoLevel = remaining;
        }
    }
}
