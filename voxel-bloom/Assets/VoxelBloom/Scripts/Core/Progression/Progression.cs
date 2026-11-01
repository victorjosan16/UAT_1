using System;
using System.Collections.Generic;

namespace VoxelBloom.Core
{
    public sealed class CompletionReceipt
    {
        public long Id;
        public int LevelId, Stars, Coins, Score;
        public bool FirstClear, Hard, NewBest;
    }

    /// <summary>Level/chapter unlocking, stars, rewards and collection unlocks on top of SaveData.</summary>
    public sealed class Progression
    {
        readonly GameConfig cfg;
        readonly SaveData save;
        readonly Wallet wallet;

        public Progression(GameConfig cfg, SaveData save, Wallet wallet)
        {
            this.cfg = cfg; this.save = save; this.wallet = wallet;
        }

        public int StarsFor(int level) => save.Stars.TryGetValue(level, out var s) ? s : 0;
        public bool IsCompleted(int level) => save.Stars.ContainsKey(level);
        public int TotalStars => save.TotalStars;

        public bool IsChapterUnlocked(ChapterInfo c)
            => c.Id == 1 || (save.HighestUnlockedLevel >= c.FirstLevel && TotalStars >= c.UnlockStars);

        public bool IsLevelUnlocked(int level)
        {
            if (level < 1 || level > cfg.TotalLevels) return false;
            var c = cfg.ChapterOf(level);
            return level <= save.HighestUnlockedLevel && c != null && IsChapterUnlocked(c);
        }

        /// <summary>Latest unlocked level (for PLAY). Falls back to the last level of the last unlocked chapter.</summary>
        public int LatestPlayableLevel()
        {
            int l = Math.Min(save.HighestUnlockedLevel, Math.Max(1, cfg.TotalLevels));
            while (l > 1 && !IsLevelUnlocked(l)) l--;
            return l;
        }

        public int ChapterStars(ChapterInfo c)
        {
            int t = 0;
            for (int l = c.FirstLevel; l <= c.LastLevel; l++) t += StarsFor(l);
            return t;
        }

        public bool IsHard(int level) => cfg.HardEvery > 0 && level % cfg.HardEvery == 0;

        /// <summary>Records a win exactly once per call and grants coins. Replays pay 25% of the reward.</summary>
        public CompletionReceipt CompleteLevel(LevelDefinition level, int moves, int score, int maxCombo)
        {
            int stars = level.StarsForScore(score);
            bool first = !IsCompleted(level.Id);
            int prev = StarsFor(level.Id);
            if (stars > prev) save.Stars[level.Id] = stars;
            if (level.Id + 1 > save.HighestUnlockedLevel && level.Id < cfg.TotalLevels) save.HighestUnlockedLevel = level.Id + 1;
            if (!string.IsNullOrEmpty(level.ModelId)) save.CollectionUnlocked.Add(level.ModelId);

            int coins = level.RewardCoins + (stars - 1) * 10 + maxCombo * cfg.Economy.ComboCoinBonus;
            if (!first) coins = Math.Max(5, coins / 4);
            wallet.Add(coins);
            save.Coins = wallet.Coins;
            if (first && (level.Hard || IsHard(level.Id))) save.Gems += 10; // HARD levels also pay gems
            save.TotalScore += score;
            save.ActiveLevel = null;
            save.ReceiptCounter++;
            save.LastReceiptId = save.ReceiptCounter;
            save.LastReceiptDoubled = false;
            return new CompletionReceipt
            {
                Id = save.LastReceiptId, LevelId = level.Id, Stars = stars, Coins = coins, Score = score,
                FirstClear = first, Hard = level.Hard || IsHard(level.Id), NewBest = stars > prev,
            };
        }

        /// <summary>Doubles a receipt's coins once. Caller must only invoke after a verified rewarded-ad completion.</summary>
        public bool TryDoubleReward(CompletionReceipt r)
        {
            if (r == null || r.Id != save.LastReceiptId || save.LastReceiptDoubled) return false;
            save.LastReceiptDoubled = true;
            wallet.Add(r.Coins);
            save.Coins = wallet.Coins;
            return true;
        }

        // ------------------------------------------------------------------ conveyor sort (rules v3)
        public int ConveyorStarsFor(int level) => save.ConveyorStars.TryGetValue(level, out var s) ? s : 0;
        public bool IsConveyorUnlocked(int level, int total) => level >= 1 && level <= total && level <= save.ConveyorUnlocked;
        public int LatestConveyorLevel(int total) => Math.Max(1, Math.Min(save.ConveyorUnlocked, Math.Max(1, total)));
        public int ConveyorTotalStars { get { int t = 0; foreach (var v in save.ConveyorStars.Values) t += v; return t; } }

        /// <summary>Stars: 3 without boosters, 2 with one or two, 1 otherwise.</summary>
        public static int ConveyorStarsForBoosters(int boostersUsed) => boostersUsed == 0 ? 3 : boostersUsed <= 2 ? 2 : 1;

        /// <summary>Records a conveyor win exactly once per call: stars, unlock, coins (replays pay 25%), gems every 5th level.</summary>
        public CompletionReceipt CompleteConveyor(ConveyorLevel level, int boostersUsed, int total)
        {
            int stars = ConveyorStarsForBoosters(boostersUsed);
            int prev = ConveyorStarsFor(level.Id);
            bool first = prev == 0;
            if (stars > prev) save.ConveyorStars[level.Id] = stars;
            if (level.Id + 1 > save.ConveyorUnlocked && level.Id < total) save.ConveyorUnlocked = level.Id + 1;
            int coins = level.RewardCoins + (stars - 1) * 10;
            if (!first) coins = Math.Max(5, coins / 4);
            wallet.Add(coins);
            save.Coins = wallet.Coins;
            bool gemLevel = level.Id % 5 == 0;
            if (first && gemLevel) save.Gems += 10;
            save.ConveyorActive = null;
            save.ReceiptCounter++;
            save.LastReceiptId = save.ReceiptCounter;
            save.LastReceiptDoubled = false;
            return new CompletionReceipt { Id = save.LastReceiptId, LevelId = level.Id, Stars = stars, Coins = coins, FirstClear = first, Hard = first && gemLevel, NewBest = stars > prev };
        }

        public bool IsModelUnlocked(string modelId) => save.CollectionUnlocked.Contains(modelId);

        public int CollectedIn(string category, out int total)
        {
            int got = 0; total = 0;
            foreach (var e in cfg.Collection)
            {
                if (e.Category != category) continue;
                total++;
                if (IsModelUnlocked(e.Id)) got++;
            }
            return got;
        }
    }
}
