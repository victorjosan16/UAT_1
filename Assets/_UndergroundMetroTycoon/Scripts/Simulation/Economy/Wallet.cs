using System;

namespace UndergroundMetroTycoon.Simulation
{
    /// <summary>Single source of truth for the player's coins. The balance can never become negative.</summary>
    public sealed class Wallet
    {
        public Wallet(long initialCoins = 0)
        {
            Coins = Math.Max(0, initialCoins);
        }

        public long Coins { get; private set; }

        /// <summary>Raised with (previous, current) balance.</summary>
        public event Action<long, long> Changed;

        public bool Credit(long amount)
        {
            if (amount <= 0) return false;
            long previous = Coins;
            Coins = amount > long.MaxValue - Coins ? long.MaxValue : Coins + amount;
            Changed?.Invoke(previous, Coins);
            return true;
        }

        public bool CanAfford(long amount) => amount >= 0 && Coins >= amount;

        public bool TrySpend(long amount)
        {
            if (amount <= 0 || Coins < amount) return false;
            long previous = Coins;
            Coins -= amount;
            Changed?.Invoke(previous, Coins);
            return true;
        }
    }
}
