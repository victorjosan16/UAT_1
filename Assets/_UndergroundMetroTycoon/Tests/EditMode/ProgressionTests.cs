using NUnit.Framework;
using UndergroundMetroTycoon.Simulation;

namespace UndergroundMetroTycoon.Tests
{
    public class ProgressionTests
    {
        private static ProgressionDefinition Def => new ProgressionDefinition { BaseXpPerLevel = 100, GrowthPerLevel = 1.5f, MaxLevel = 5 };

        [Test]
        public void LevelThresholds_FollowGrowthCurve()
        {
            var p = new PlayerProgression(Def);
            Assert.AreEqual(100, p.XpRequiredForLevel(1));
            Assert.AreEqual(150, p.XpRequiredForLevel(2));
            Assert.AreEqual(225, p.XpRequiredForLevel(3));
        }

        [Test]
        public void AddXp_ReportsLevelsGained_AndCarriesRemainder()
        {
            var p = new PlayerProgression(Def);
            Assert.AreEqual(0, p.AddXp(99));
            Assert.AreEqual(1, p.Level);
            Assert.AreEqual(2, p.AddXp(160));
            Assert.AreEqual(3, p.Level);
            Assert.AreEqual(9, p.XpIntoLevel);
        }

        [Test]
        public void Level_IsCappedAtMax()
        {
            var p = new PlayerProgression(Def, 1000000);
            Assert.AreEqual(5, p.Level);
            Assert.IsTrue(p.IsMaxLevel);
            Assert.AreEqual(0, p.XpForNextLevel);
        }

        [Test]
        public void NegativeXp_IsIgnored()
        {
            var p = new PlayerProgression(Def, -10);
            Assert.AreEqual(0, p.TotalXp);
            Assert.AreEqual(0, p.AddXp(-5));
        }
    }
}
