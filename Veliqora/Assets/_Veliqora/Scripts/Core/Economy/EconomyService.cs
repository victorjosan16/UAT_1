using System;
using System.Collections.Generic;

namespace Veliqora.Core.Economy
{
    /// <summary>Persistent wallet + virtual inventory + owned cosmetics + processed transaction ledger.</summary>
    public sealed class EconomyState
    {
        public const int MaxLedger = 512;

        public long Coins;
        public Dictionary<BoosterType, int> Inventory = new Dictionary<BoosterType, int>();
        public HashSet<string> OwnedCosmetics = new HashSet<string>();
        /// <summary>Ordered ledger of processed transaction ids (bounded); makes grants/purchases idempotent.</summary>
        public List<string> Ledger = new List<string>();
        /// <summary>Permanent flag (not in the rolling ledger) so the starting grant can never repeat.</summary>
        public bool StartingGrantApplied;

        public int Count(BoosterType t) => Inventory.TryGetValue(t, out int n) ? n : 0;
    }

    public enum TxStatus { Ok, Duplicate, InsufficientFunds, UnknownItem, AlreadyOwned, NotOwned, InvalidAmount, NoneLeft }

    public sealed class TxResult
    {
        public TxStatus Status;
        public long CoinsDelta;
        public bool Success => Status == TxStatus.Ok;
        public static TxResult Of(TxStatus s, long delta = 0) => new TxResult { Status = s, CoinsDelta = delta };
        public override string ToString() => Status + " (" + CoinsDelta + ")";
    }

    public sealed class LevelReward
    {
        public int BaseCoins;
        public int StarCoins;
        public int MilestoneCoins;
        /// <summary>Hard/Boss first-clear bonus.</summary>
        public int DifficultyBonus;
        public Dictionary<BoosterType, int> Boosters = new Dictionary<BoosterType, int>();
        public bool FirstClear;
        public int Total => BaseCoins + StarCoins + MilestoneCoins + DifficultyBonus;
        public TxStatus Status;
    }

    /// <summary>
    /// All coin/booster mutations go through here. Every mutation requires a transaction id; a repeated id is a
    /// no-op that reports Duplicate (so retries, double taps and duplicate ad callbacks can't mint currency).
    /// Balances can never go negative.
    /// </summary>
    public sealed class EconomyService
    {
        private readonly EconomyState _state;
        private readonly EconomyConfig _config;
        private readonly HashSet<string> _ledgerSet;

        public event Action Changed;

        public EconomyService(EconomyState state, EconomyConfig config)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _ledgerSet = new HashSet<string>(_state.Ledger);
            foreach (var c in _config.Cosmetics) if (c.DefaultOwned || c.Price == 0) _state.OwnedCosmetics.Add(c.Id);
            if (_state.Coins < 0) _state.Coins = 0;
        }

        public EconomyState State => _state;
        public EconomyConfig Config => _config;
        public long Coins => _state.Coins;
        public int Count(BoosterType t) => _state.Count(t);
        public bool Owns(string cosmeticId) => _state.OwnedCosmetics.Contains(cosmeticId);
        public bool IsProcessed(string txId) => _ledgerSet.Contains(txId);

        /// <summary>Applies starting balances once per save (idempotent).</summary>
        public void EnsureStartingGrant()
        {
            if (_state.StartingGrantApplied) return;
            _state.Coins += _config.StartingCoins;
            foreach (var kv in _config.StartingBoosters) Add(kv.Key, kv.Value);
            _state.StartingGrantApplied = true;
            Changed?.Invoke();
        }

        public TxResult BuyOffer(string offerId, string txId)
        {
            if (IsProcessed(txId)) return TxResult.Of(TxStatus.Duplicate);
            var offer = _config.FindOffer(offerId);
            if (offer == null) return TxResult.Of(TxStatus.UnknownItem);
            if (_state.Coins < offer.Price) return TxResult.Of(TxStatus.InsufficientFunds);
            _state.Coins -= offer.Price;
            Add(offer.Booster, offer.Quantity);
            Record(txId);
            Changed?.Invoke();
            return TxResult.Of(TxStatus.Ok, -offer.Price);
        }

        public TxResult BuyCosmetic(string cosmeticId, string txId)
        {
            if (IsProcessed(txId)) return TxResult.Of(TxStatus.Duplicate);
            var item = _config.FindCosmetic(cosmeticId);
            if (item == null) return TxResult.Of(TxStatus.UnknownItem);
            if (Owns(cosmeticId)) return TxResult.Of(TxStatus.AlreadyOwned);
            if (_state.Coins < item.Price) return TxResult.Of(TxStatus.InsufficientFunds);
            _state.Coins -= item.Price;
            _state.OwnedCosmetics.Add(cosmeticId);
            Record(txId);
            Changed?.Invoke();
            return TxResult.Of(TxStatus.Ok, -item.Price);
        }

        public TxResult ConsumeBooster(BoosterType type, string txId)
        {
            if (IsProcessed(txId)) return TxResult.Of(TxStatus.Duplicate);
            if (Count(type) <= 0) return TxResult.Of(TxStatus.NoneLeft);
            _state.Inventory[type] = Count(type) - 1;
            Record(txId);
            Changed?.Invoke();
            return TxResult.Of(TxStatus.Ok);
        }

        /// <summary>Returns a consumed booster when its effect could not be applied (e.g. shuffle found no layout).</summary>
        public TxResult RefundBooster(BoosterType type, string originalTxId)
        {
            return GrantBooster(type, 1, "refund:" + originalTxId);
        }

        public TxResult GrantBooster(BoosterType type, int quantity, string txId)
        {
            if (quantity <= 0) return TxResult.Of(TxStatus.InvalidAmount);
            if (IsProcessed(txId)) return TxResult.Of(TxStatus.Duplicate);
            Add(type, quantity);
            Record(txId);
            Changed?.Invoke();
            return TxResult.Of(TxStatus.Ok);
        }

        public TxResult GrantCoins(int amount, string txId)
        {
            if (amount <= 0) return TxResult.Of(TxStatus.InvalidAmount);
            if (IsProcessed(txId)) return TxResult.Of(TxStatus.Duplicate);
            _state.Coins += amount;
            Record(txId);
            Changed?.Invoke();
            return TxResult.Of(TxStatus.Ok, amount);
        }

        /// <summary>
        /// Computes the reward for finishing a level. First clear: base + per-star coins (+ milestone every N levels).
        /// Replays: small base + coins only for newly earned stars, so replaying can't farm large amounts.
        /// </summary>
        public LevelReward ComputeLevelReward(int levelNumber, int stars, int previousBestStars, Levels.LevelKind kind = Levels.LevelKind.Normal)
        {
            var r = new LevelReward { FirstClear = previousBestStars <= 0 };
            stars = Math.Max(1, Math.Min(3, stars));
            if (r.FirstClear)
            {
                r.BaseCoins = _config.LevelBaseReward;
                r.StarCoins = stars * _config.PerStarReward;
                if (kind == Levels.LevelKind.Hard) r.DifficultyBonus = _config.HardLevelBonus;
                else if (kind == Levels.LevelKind.Boss) r.DifficultyBonus = _config.BossLevelBonus;
                if (_config.MilestoneEvery > 0 && levelNumber % _config.MilestoneEvery == 0)
                {
                    r.MilestoneCoins = _config.MilestoneCoins;
                    foreach (var kv in _config.MilestoneBoosters) r.Boosters[kv.Key] = kv.Value;
                }
            }
            else
            {
                r.BaseCoins = _config.ReplayBaseReward;
                r.StarCoins = Math.Max(0, stars - previousBestStars) * _config.StarImprovementReward;
            }
            return r;
        }

        /// <summary>Grants the level reward exactly once per attempt id.</summary>
        public LevelReward GrantLevelReward(int levelNumber, int stars, int previousBestStars, string attemptId, Levels.LevelKind kind = Levels.LevelKind.Normal)
        {
            var r = ComputeLevelReward(levelNumber, stars, previousBestStars, kind);
            string tx = "level:" + levelNumber + ":" + attemptId;
            if (IsProcessed(tx)) { r.Status = TxStatus.Duplicate; return r; }
            _state.Coins += r.Total;
            foreach (var kv in r.Boosters) Add(kv.Key, kv.Value);
            Record(tx);
            r.Status = TxStatus.Ok;
            Changed?.Invoke();
            return r;
        }

        private void Add(BoosterType t, int q)
        {
            _state.Inventory[t] = checked(Count(t) + q);
        }

        private void Record(string txId)
        {
            if (string.IsNullOrEmpty(txId)) throw new ArgumentException("Transaction id required");
            _ledgerSet.Add(txId);
            _state.Ledger.Add(txId);
            while (_state.Ledger.Count > EconomyState.MaxLedger)
            {
                // Oldest entries roll off; they belong to long-finished attempts that can no longer be replayed.
                _ledgerSet.Remove(_state.Ledger[0]);
                _state.Ledger.RemoveAt(0);
            }
        }
    }
}
