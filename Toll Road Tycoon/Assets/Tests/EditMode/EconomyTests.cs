using NUnit.Framework;
using TollRoadTycoon.Economy;

namespace TollRoadTycoon.Tests
{
    public class EconomyTests
    {
        [Test]
        public void SpendingNeverGoesNegative()
        {
            var eco = new EconomyManager(Money.FromDollars(100));
            Assert.IsFalse(eco.TrySpendMoney(Money.FromDollars(101), "x"));
            Assert.AreEqual(Money.FromDollars(100), eco.Balance);
            Assert.AreEqual(0, eco.TotalExpenses);
            Assert.IsTrue(eco.TrySpendMoney(Money.FromDollars(100), "x"));
            Assert.AreEqual(0, eco.Balance);
            Assert.IsFalse(eco.TrySpendMoney(-5, "negative"));
        }

        [Test]
        public void TotalsTrackEarningsAndExpenses()
        {
            var eco = new EconomyManager(0);
            eco.AddMoney(500, MoneySource.Toll);
            eco.AddMoney(250, MoneySource.Reward);
            eco.AddMoney(-100, MoneySource.Toll);
            eco.TrySpendMoney(300, "x");
            Assert.AreEqual(750, eco.GetTotalEarnings());
            Assert.AreEqual(300, eco.GetTotalExpenses());
            Assert.AreEqual(450, eco.GetBalance());
        }

        [Test]
        public void IncomePerMinuteUsesRollingWindowOfTolls()
        {
            var eco = new EconomyManager(0, 60);
            eco.ResetIncomeWindow(0);
            for (int s = 1; s <= 120; s++)
            {
                eco.Tick(s);
                eco.AddMoney(100, MoneySource.Toll); // $1 per second
            }
            eco.AddMoney(1_000_000, MoneySource.Reward); // rewards are not toll income
            Assert.AreEqual(Money.FromDollars(60), eco.GetIncomePerMinute(), 200);

            eco.Tick(300); // two quiet minutes later the window is empty
            Assert.AreEqual(0, eco.GetIncomePerMinute());
        }

        [TestCase(0, "$0")]
        [TestCase(125_000, "$1,250")]
        [TestCase(1_250_050, "$12,500.50")]
        [TestCase(12_560_000, "$125K")]
        [TestCase(1_256_000_00, "$1.25M")]
        [TestCase(325_000_000, "$3.25M")]
        [TestCase(110_000_000_000, "$1.1B")]
        public void FormatsMoney(long cents, string expected)
        {
            Assert.AreEqual(expected, Money.Format(cents));
        }

        [Test]
        public void CostCurveGrowsGeometrically()
        {
            var curve = new CostCurve(Money.FromDollars(100), 2.0);
            Assert.AreEqual(Money.FromDollars(100), curve.CostForLevel(0));
            Assert.AreEqual(Money.FromDollars(200), curve.CostForLevel(1));
            Assert.AreEqual(Money.FromDollars(800), curve.CostForLevel(3));
        }

        [Test]
        public void BasisPointsRoundSafely()
        {
            Assert.AreEqual(625, Money.ApplyBasisPoints(500, 12500));
            Assert.AreEqual(0, Money.ApplyBasisPoints(0, 12500));
            Assert.AreEqual(1001, Money.ApplyBasisPoints(1001, 10000));
        }
    }
}
