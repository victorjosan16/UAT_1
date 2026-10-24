using NUnit.Framework;

namespace SugarDrop.Tests
{
    /// <summary>Regenerarea vieților în timp.</summary>
    public class LifeClockTests
    {
        private const int Max = 5;
        private const int Regen = 1200;

        [Test]
        public void FullLives_ResetTheClock()
        {
            var (lives, timestamp) = LifeClock.Regenerate(5, 100, 5000, Max, Regen);
            Assert.AreEqual(5, lives);
            Assert.AreEqual(5000, timestamp);
            Assert.AreEqual(0, LifeClock.SecondsToNext(5, 100, 5000, Max, Regen));
        }

        [Test]
        public void BeforeRegenTime_NothingChanges()
        {
            var (lives, timestamp) = LifeClock.Regenerate(2, 1000, 1000 + Regen - 1, Max, Regen);
            Assert.AreEqual(2, lives);
            Assert.AreEqual(1000, timestamp);
            Assert.AreEqual(1, LifeClock.SecondsToNext(2, 1000, 1000 + Regen - 1, Max, Regen));
        }

        [Test]
        public void RegeneratesWholeLives_AndKeepsRemainder()
        {
            var (lives, timestamp) = LifeClock.Regenerate(1, 0, Regen * 2 + 300, Max, Regen);
            Assert.AreEqual(3, lives);
            Assert.AreEqual(Regen * 2, timestamp);
        }

        [Test]
        public void NeverExceedsMaximum()
        {
            var (lives, _) = LifeClock.Regenerate(0, 0, Regen * 50, Max, Regen);
            Assert.AreEqual(Max, lives);
        }

        [Test]
        public void ClockGoingBackwards_DoesNotGrantLives()
        {
            var (lives, _) = LifeClock.Regenerate(2, 10000, 500, Max, Regen);
            Assert.AreEqual(2, lives);
        }
    }
}
