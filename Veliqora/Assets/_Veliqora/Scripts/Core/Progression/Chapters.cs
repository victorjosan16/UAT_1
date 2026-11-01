using System;
using System.Collections.Generic;
using Veliqora.Core.Economy;
using Veliqora.Core.Save;

namespace Veliqora.Core.Progression
{
    /// <summary>One chapter of the progress map: 30 consecutive levels, up to 90 stars.</summary>
    public sealed class ChapterInfo
    {
        public int Index;          // 1-based
        public string Name;        // English source string (localized by the UI)
        public int FirstLevel;
        public int LastLevel;
        public int MaxStars => (LastLevel - FirstLevel + 1) * 3;
    }

    /// <summary>
    /// Progress map chapters ("Hartă de progres – Capitole"). Finishing every level of a chapter grants a chapter chest
    /// once (ledger key "chapter:&lt;index&gt;"), so the reward can never be collected twice.
    /// </summary>
    public static class Chapters
    {
        public const int LevelsPerChapter = 30;
        public const int RewardCoins = 200;

        public static readonly string[] Names =
        {
            "Quiet Forest", "Crystal Lake", "Colorful Canyon", "Magic Cave", "Temple of Colors",
            "Starlit Valley", "Ember Peaks", "Frost Harbor", "Sky Gardens", "Moon Palace",
        };

        public static int IndexOf(int level) => (Math.Max(1, level) - 1) / LevelsPerChapter + 1;

        public static ChapterInfo Get(int index, int levelCount)
        {
            int first = (index - 1) * LevelsPerChapter + 1;
            return new ChapterInfo
            {
                Index = index,
                Name = Names[(index - 1) % Names.Length],
                FirstLevel = first,
                LastLevel = Math.Min(levelCount, first + LevelsPerChapter - 1),
            };
        }

        public static List<ChapterInfo> All(int levelCount)
        {
            var list = new List<ChapterInfo>();
            for (int i = 1; (i - 1) * LevelsPerChapter < levelCount; i++) list.Add(Get(i, levelCount));
            return list;
        }

        public static int Stars(ProgressData p, ChapterInfo c)
        {
            int s = 0;
            for (int n = c.FirstLevel; n <= c.LastLevel; n++) s += p.Stars(n);
            return s;
        }

        public static bool IsComplete(ProgressData p, ChapterInfo c)
        {
            for (int n = c.FirstLevel; n <= c.LastLevel; n++) if (!p.IsCompleted(n)) return false;
            return true;
        }

        /// <summary>Grants the chapter chest if the chapter is complete and not yet rewarded. Returns coins granted (0 otherwise).</summary>
        /// <param name="claimed">Persistent claimed-reward set (SaveData.ClaimedAchievements); guards even after the bounded ledger rolls over.</param>
        public static int TryGrantReward(ProgressData p, ChapterInfo c, EconomyService economy, HashSet<string> claimed)
        {
            string key = "chapter:" + c.Index;
            if (!IsComplete(p, c) || claimed.Contains(key)) return 0;
            var r = economy.GrantCoins(RewardCoins, key);
            claimed.Add(key);
            return r.Success ? RewardCoins : 0;
        }
    }
}
