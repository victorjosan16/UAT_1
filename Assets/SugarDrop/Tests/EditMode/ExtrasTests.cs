using System;
using NUnit.Framework;

namespace SugarDrop.Tests
{
    public class ExtrasTests
    {
        [Test]
        public void Wheel_PicksEverySliceSometimes_AndStaysInRange()
        {
            var seen = new bool[WheelRules.Slices.Length];
            for (int i = 0; i < 1000; i++)
            {
                int slice = WheelRules.PickSlice(i / 1000.0);
                Assert.That(slice, Is.InRange(0, WheelRules.Slices.Length - 1));
                seen[slice] = true;
            }
            Assert.IsTrue(Array.TrueForAll(seen, s => s), "fiecare felie poate fi câștigată");
            Assert.AreEqual(WheelRules.PickSlice(0.0), 0);
            Assert.That(WheelRules.PickSlice(1.0), Is.InRange(0, WheelRules.Slices.Length - 1));
        }

        [Test]
        public void Weekend_IsSaturdayAndSunday_WithCountdowns()
        {
            var friday = new DateTime(2026, 10, 23, 18, 0, 0);
            var saturday = new DateTime(2026, 10, 24, 10, 0, 0);
            var sunday = new DateTime(2026, 10, 25, 22, 0, 0);
            Assert.IsFalse(SugarEvents.IsWeekend(friday));
            Assert.IsTrue(SugarEvents.IsWeekend(saturday));
            Assert.IsTrue(SugarEvents.IsWeekend(sunday));
            Assert.AreEqual(TimeSpan.FromHours(6), SugarEvents.Countdown(friday), "începe sâmbătă la 00:00");
            Assert.AreEqual(TimeSpan.FromHours(2), SugarEvents.Countdown(sunday), "se termină luni la 00:00");
            Assert.AreEqual(2, SugarEvents.CoinMultiplier(saturday));
            Assert.AreEqual(1, SugarEvents.CoinMultiplier(friday));
            Assert.AreEqual(2, WheelRules.FreeSpinsPerDay(sunday));
        }

        [Test]
        public void Costumes_UnlockInOrder_AndAreTranslated()
        {
            int previous = -1;
            foreach (Costume costume in CostumeRules.All)
            {
                Assert.Greater(CostumeRules.StarsNeeded(costume), previous);
                previous = CostumeRules.StarsNeeded(costume);
                string name = CostumeRules.NameKey(costume);
                Assert.AreNotEqual(name, SugarLoc.Translate(name, SugarLanguage.English), name);
            }
            Assert.IsTrue(CostumeRules.Unlocked(Costume.None, 0));
            Assert.IsFalse(CostumeRules.Unlocked(Costume.Crown, 10));
        }

        [Test]
        public void WheelPrizes_AreTranslated()
        {
            foreach (WheelPrize prize in WheelRules.Slices)
            {
                string key = SugarWheel.PrizeKey(prize);
                Assert.AreNotEqual(key, SugarLoc.Translate(key, SugarLanguage.Russian), key);
            }
        }
    }
}
