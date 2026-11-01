using System;
using System.Collections.Generic;

namespace VoxelBloom.Core
{
    public enum PurchaseResult { Success, NotEnoughCoins, InvalidBooster }

    /// <summary>Persistent booster counts and coin purchases. Coins are only deducted together with the grant.</summary>
    public sealed class BoosterInventory
    {
        readonly Dictionary<BoosterType, int> counts = new Dictionary<BoosterType, int>();
        public event Action<BoosterType, int> Changed;

        public int Count(BoosterType t) => counts.TryGetValue(t, out var n) ? n : 0;

        public void Set(BoosterType t, int n)
        {
            counts[t] = Math.Max(0, n);
            Changed?.Invoke(t, counts[t]);
        }

        public void Add(BoosterType t, int n) { if (n > 0) Set(t, Count(t) + n); }

        public bool TryConsume(BoosterType t)
        {
            int n = Count(t);
            if (n <= 0) return false;
            Set(t, n - 1);
            return true;
        }

        public PurchaseResult Buy(BoosterType t, GameConfig config, Wallet wallet, int quantity = 1)
        {
            if (quantity <= 0 || !config.Boosters.ContainsKey(t)) return PurchaseResult.InvalidBooster;
            long price = (long)config.Booster(t).Price * quantity;
            if (price > int.MaxValue || !wallet.TrySpend((int)price)) return PurchaseResult.NotEnoughCoins;
            Add(t, quantity);
            return PurchaseResult.Success;
        }

        public IEnumerable<KeyValuePair<BoosterType, int>> All => counts;
    }
}
