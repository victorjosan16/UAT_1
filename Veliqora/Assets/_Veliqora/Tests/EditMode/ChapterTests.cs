using System.Collections.Generic;
using NUnit.Framework;
using Veliqora.Core.Economy;
using Veliqora.Core.Progression;
using Veliqora.Core.Save;

namespace Veliqora.Tests.EditMode
{
    public class ChapterTests
    {
        [Test]
        public void Chapters_Are30Levels_With90Stars()
        {
            var all = Chapters.All(300);
            Assert.AreEqual(10, all.Count);
            Assert.AreEqual(1, all[0].FirstLevel);
            Assert.AreEqual(30, all[0].LastLevel);
            Assert.AreEqual(90, all[0].MaxStars);
            Assert.AreEqual("Crystal Lake", all[1].Name);
            Assert.AreEqual(2, Chapters.IndexOf(31));
            Assert.AreEqual(1, Chapters.IndexOf(30));
        }

        [Test]
        public void ChapterChest_IsGrantedOnce_WhenAllLevelsAreDone()
        {
            var eco = new EconomyService(new EconomyState(), EconomyConfig.FromJson(System.IO.File.ReadAllText(
                TestUtil.FindProjectFile("Assets/_Veliqora/Resources/Veliqora/Config/economy.json"))));
            var p = new ProgressData();
            var claimed = new HashSet<string>();
            var c = Chapters.Get(1, 300);
            for (int n = 1; n <= 29; n++) ProgressRules.RecordCompletion(p, n, 3, 300);
            Assert.AreEqual(0, Chapters.TryGrantReward(p, c, eco, claimed), "not before the last level");
            Assert.AreEqual(87, Chapters.Stars(p, c));
            ProgressRules.RecordCompletion(p, 30, 2, 300);
            long before = eco.Coins;
            Assert.AreEqual(Chapters.RewardCoins, Chapters.TryGrantReward(p, c, eco, claimed));
            Assert.AreEqual(before + Chapters.RewardCoins, eco.Coins);
            Assert.AreEqual(0, Chapters.TryGrantReward(p, c, eco, claimed), "never twice");
            Assert.AreEqual(89, Chapters.Stars(p, c));
        }
    }
}
