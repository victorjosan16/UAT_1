using System;

namespace VoxelBloom.Core
{
    /// <summary>Coin balance that can never go negative.</summary>
    public sealed class Wallet
    {
        public int Coins { get; private set; }
        public event Action<int> Changed;

        public Wallet(int coins) { Coins = Math.Max(0, coins); }

        public void Add(int amount)
        {
            if (amount <= 0) return;
            Coins = (int)Math.Min(int.MaxValue, (long)Coins + amount);
            Changed?.Invoke(Coins);
        }

        public bool CanAfford(int price) => price >= 0 && Coins >= price;

        public bool TrySpend(int price)
        {
            if (!CanAfford(price)) return false;
            Coins -= price;
            Changed?.Invoke(Coins);
            return true;
        }
    }

    /// <summary>Lives with time-based regeneration. Time is injected (UTC unix seconds) for testability.</summary>
    public sealed class Lives
    {
        public int Max { get; }
        public int RegenSeconds { get; }
        public int Count { get; private set; }
        /// <summary>Unix time at which the next life is granted (0 when full).</summary>
        public long NextLifeAt { get; private set; }

        public Lives(int count, int max, int regenMinutes, long nextLifeAt)
        {
            Max = Math.Max(1, max);
            RegenSeconds = Math.Max(60, regenMinutes * 60);
            Count = Math.Max(0, Math.Min(count, Max));
            NextLifeAt = Count >= Max ? 0 : nextLifeAt;
        }

        public void Update(long now)
        {
            if (Count >= Max) { NextLifeAt = 0; return; }
            if (NextLifeAt == 0) NextLifeAt = now + RegenSeconds;
            while (Count < Max && now >= NextLifeAt)
            {
                Count++;
                NextLifeAt = Count >= Max ? 0 : NextLifeAt + RegenSeconds;
            }
        }

        public bool TryConsume(long now)
        {
            Update(now);
            if (Count <= 0) return false;
            if (Count == Max) NextLifeAt = now + RegenSeconds;
            Count--;
            return true;
        }

        public void Refill() { Count = Max; NextLifeAt = 0; }

        public void Add(int n, long now)
        {
            if (n <= 0) return;
            Count = Math.Min(Max, Count + n);
            if (Count >= Max) NextLifeAt = 0;
            else if (NextLifeAt == 0) NextLifeAt = now + RegenSeconds;
        }

        public long SecondsToNext(long now) => Count >= Max || NextLifeAt == 0 ? 0 : Math.Max(0, NextLifeAt - now);
    }

    /// <summary>Daily reward with a 7-day streak. Day boundaries are UTC midnight (consistent reset policy).</summary>
    public sealed class DailyRewards
    {
        readonly int[] rewards;
        public long LastClaimDay { get; private set; } = -1;
        public int Streak { get; private set; }

        public DailyRewards(int[] rewards, long lastClaimDay, int streak)
        {
            this.rewards = rewards == null || rewards.Length == 0 ? new[] { 100 } : rewards;
            LastClaimDay = lastClaimDay;
            Streak = Math.Max(0, streak);
        }

        public static long DayOf(long unixSeconds) => (long)Math.Floor(unixSeconds / 86400.0);

        public bool CanClaim(long now) => DayOf(now) > LastClaimDay;

        /// <summary>Reward for the claim that would happen now (streak resets after a missed day).</summary>
        public int PendingReward(long now)
        {
            int nextStreak = DayOf(now) == LastClaimDay + 1 ? Streak + 1 : 1;
            return rewards[(nextStreak - 1) % rewards.Length];
        }

        public int StreakIfClaimed(long now) => DayOf(now) == LastClaimDay + 1 ? Streak + 1 : 1;

        /// <summary>Claims once per UTC day; returns 0 if already claimed (no double claims).</summary>
        public int Claim(long now, Wallet wallet)
        {
            if (!CanClaim(now)) return 0;
            int amount = PendingReward(now);
            Streak = StreakIfClaimed(now);
            LastClaimDay = DayOf(now);
            wallet.Add(amount);
            return amount;
        }
    }
}
