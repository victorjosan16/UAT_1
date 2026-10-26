using NUnit.Framework;
using UndergroundMetroTycoon.Simulation;

namespace UndergroundMetroTycoon.Tests
{
    public class WalletTests
    {
        [Test]
        public void NewWallet_NegativeStart_ClampsToZero()
        {
            Assert.AreEqual(0, new Wallet(-50).Coins);
        }

        [Test]
        public void TrySpend_InsufficientFunds_FailsWithoutChangingBalance()
        {
            var wallet = new Wallet(10);
            Assert.IsFalse(wallet.TrySpend(11));
            Assert.AreEqual(10, wallet.Coins);
        }

        [Test]
        public void TrySpend_ExactBalance_ReachesZero()
        {
            var wallet = new Wallet(10);
            Assert.IsTrue(wallet.TrySpend(10));
            Assert.AreEqual(0, wallet.Coins);
        }

        [Test]
        public void CreditAndSpend_RejectNonPositiveAmounts()
        {
            var wallet = new Wallet(5);
            Assert.IsFalse(wallet.Credit(0));
            Assert.IsFalse(wallet.Credit(-3));
            Assert.IsFalse(wallet.TrySpend(-3));
            Assert.AreEqual(5, wallet.Coins);
        }

        [Test]
        public void Credit_SaturatesInsteadOfOverflowing()
        {
            var wallet = new Wallet(long.MaxValue - 1);
            wallet.Credit(100);
            Assert.AreEqual(long.MaxValue, wallet.Coins);
        }

        [Test]
        public void Changed_ReportsPreviousAndCurrent()
        {
            var wallet = new Wallet(3);
            long seenPrevious = -1, seenCurrent = -1;
            wallet.Changed += (p, c) => { seenPrevious = p; seenCurrent = c; };
            wallet.Credit(4);
            Assert.AreEqual(3, seenPrevious);
            Assert.AreEqual(7, seenCurrent);
        }
    }
}
