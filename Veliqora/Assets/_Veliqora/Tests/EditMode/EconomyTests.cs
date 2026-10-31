using System.IO;
using NUnit.Framework;
using Veliqora.Core.Economy;

namespace Veliqora.Tests.EditMode
{
    public class EconomyTests
    {
        private static EconomyConfig Config => EconomyConfig.FromJson(File.ReadAllText(TestUtil.FindProjectFile(TestUtil.EconomyRelativePath)));

        private static EconomyService NewService(long coins = 0)
        {
            var state = new EconomyState { Coins = coins };
            return new EconomyService(state, Config);
        }

        [Test]
        public void Config_LoadsPricesFromData()
        {
            var c = Config;
            Assert.AreEqual(4, c.Offers.Count);
            Assert.IsNotNull(c.FindOffer(BoosterType.Hint));
            Assert.Greater(c.FindOffer("offer_extra_bottle").Price, 0);
            Assert.IsTrue(c.FindCosmetic("theme_valley").DefaultOwned);
            Assert.AreEqual(10, c.Cosmetics.Count);
        }

        [Test]
        public void StartingGrant_IsAppliedOnce()
        {
            var e = NewService();
            e.EnsureStartingGrant();
            long coins = e.Coins;
            int undo = e.Count(BoosterType.Undo);
            e.EnsureStartingGrant();
            Assert.AreEqual(coins, e.Coins);
            Assert.AreEqual(undo, e.Count(BoosterType.Undo));
            Assert.AreEqual(Config.StartingCoins, coins);
        }

        [Test]
        public void Purchase_IsAtomic_AndIdempotentPerTransaction()
        {
            var e = NewService(1000);
            var offer = Config.FindOffer("offer_undo_3");
            var r1 = e.BuyOffer(offer.Id, "tx-1");
            var r2 = e.BuyOffer(offer.Id, "tx-1");
            Assert.AreEqual(TxStatus.Ok, r1.Status);
            Assert.AreEqual(TxStatus.Duplicate, r2.Status);
            Assert.AreEqual(1000 - offer.Price, e.Coins);
            Assert.AreEqual(offer.Quantity, e.Count(BoosterType.Undo));
        }

        [Test]
        public void Purchase_WithInsufficientFunds_ChangesNothing()
        {
            var e = NewService(10);
            var r = e.BuyOffer("offer_extra_bottle", "tx-2");
            Assert.AreEqual(TxStatus.InsufficientFunds, r.Status);
            Assert.AreEqual(10, e.Coins);
            Assert.AreEqual(0, e.Count(BoosterType.ExtraBottle));
            Assert.IsFalse(e.IsProcessed("tx-2"), "failed tx can be retried later");
        }

        [Test]
        public void Balance_NeverGoesNegative()
        {
            var e = NewService(150);
            for (int i = 0; i < 10; i++) e.BuyOffer("offer_undo_3", "t" + i);
            Assert.GreaterOrEqual(e.Coins, 0);
            Assert.AreEqual(50, e.Coins);
            Assert.AreEqual(TxStatus.InvalidAmount, e.GrantCoins(-5, "neg").Status);
            Assert.AreEqual(TxStatus.InvalidAmount, e.GrantBooster(BoosterType.Hint, 0, "zero").Status);
        }

        [Test]
        public void ConsumeBooster_RequiresInventory_AndIsIdempotent()
        {
            var e = NewService();
            Assert.AreEqual(TxStatus.NoneLeft, e.ConsumeBooster(BoosterType.Shuffle, "c0").Status);
            e.GrantBooster(BoosterType.Shuffle, 1, "g1");
            Assert.AreEqual(TxStatus.Ok, e.ConsumeBooster(BoosterType.Shuffle, "c1").Status);
            Assert.AreEqual(TxStatus.Duplicate, e.ConsumeBooster(BoosterType.Shuffle, "c1").Status);
            Assert.AreEqual(0, e.Count(BoosterType.Shuffle));
            e.RefundBooster(BoosterType.Shuffle, "c1");
            e.RefundBooster(BoosterType.Shuffle, "c1");
            Assert.AreEqual(1, e.Count(BoosterType.Shuffle), "refund applied once");
        }

        [Test]
        public void LevelReward_FirstClear_vs_Replay()
        {
            var e = NewService();
            var first = e.GrantLevelReward(3, 3, 0, "att-A");
            Assert.IsTrue(first.FirstClear);
            Assert.AreEqual(Config.LevelBaseReward + 3 * Config.PerStarReward, first.Total);
            var dup = e.GrantLevelReward(3, 3, 0, "att-A");
            Assert.AreEqual(TxStatus.Duplicate, dup.Status);
            Assert.AreEqual(first.Total, e.Coins);

            var replaySame = e.ComputeLevelReward(3, 3, 3);
            Assert.AreEqual(Config.ReplayBaseReward, replaySame.Total, "replays can't farm full rewards");
            var replayBetter = e.ComputeLevelReward(3, 3, 1);
            Assert.AreEqual(Config.ReplayBaseReward + 2 * Config.StarImprovementReward, replayBetter.Total);
        }

        [Test]
        public void LevelReward_Milestone_GrantsCoinsAndBoosters()
        {
            var e = NewService();
            var r = e.GrantLevelReward(Config.MilestoneEvery, 2, 0, "m");
            Assert.AreEqual(Config.MilestoneCoins, r.MilestoneCoins);
            foreach (var kv in Config.MilestoneBoosters) Assert.AreEqual(kv.Value, e.Count(kv.Key));
        }

        [Test]
        public void Cosmetics_PurchaseOnce_DefaultsOwned()
        {
            var e = NewService(2000);
            Assert.IsTrue(e.Owns("theme_valley"));
            Assert.IsTrue(e.Owns("bottle_classic"));
            Assert.AreEqual(TxStatus.Ok, e.BuyCosmetic("theme_sunset", "c1").Status);
            Assert.AreEqual(TxStatus.AlreadyOwned, e.BuyCosmetic("theme_sunset", "c2").Status);
            Assert.AreEqual(TxStatus.UnknownItem, e.BuyCosmetic("nope", "c3").Status);
            Assert.AreEqual(2000 - Config.FindCosmetic("theme_sunset").Price, e.Coins);
        }

        [Test]
        public void Ledger_IsBounded()
        {
            var e = NewService();
            for (int i = 0; i < EconomyState.MaxLedger + 50; i++) e.GrantCoins(1, "g" + i);
            Assert.AreEqual(EconomyState.MaxLedger, e.State.Ledger.Count);
            Assert.AreEqual(EconomyState.MaxLedger + 50, e.Coins);
        }
    }
}
