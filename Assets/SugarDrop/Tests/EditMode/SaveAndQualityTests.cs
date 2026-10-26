using System.Collections.Generic;
using NUnit.Framework;

namespace SugarDrop.Tests
{
    public class SaveAndQualityTests
    {
        [Test]
        public void CloudSave_WinsOnlyWhenItIsFurther()
        {
            var phone = new SugarSaveData { level = 5, stars = new List<int> { 3, 3, 2, 1 } };
            var cloud = new SugarSaveData { level = 12 };
            Assert.IsTrue(SugarSave.IsAhead(cloud, phone));
            Assert.IsFalse(SugarSave.IsAhead(phone, cloud));

            var sameLevelMoreStars = new SugarSaveData { level = 5, stars = new List<int> { 3, 3, 3, 3 } };
            Assert.IsTrue(SugarSave.IsAhead(sameLevelMoreStars, phone));
            Assert.IsFalse(SugarSave.IsAhead(phone, phone), "aceeași salvare nu e „mai avansată”");
            Assert.IsFalse(SugarSave.IsAhead(null, phone));
        }

        [Test]
        public void SaveJson_RoundTrips()
        {
            var data = new SugarSaveData { level = 7, nickname = "Ana", bestScores = new List<int> { 100, 200 } };
            SugarSaveData copy = SugarSave.FromJson(UnityEngine.JsonUtility.ToJson(data));
            Assert.AreEqual(7, copy.level);
            Assert.AreEqual("Ana", copy.nickname);
            CollectionAssert.AreEqual(data.bestScores, copy.bestScores);
            Assert.IsNull(SugarSave.FromJson(""));
        }

        [Test]
        public void Quality_FollowsThePhoneMemory()
        {
            Assert.AreEqual(GraphicsQuality.Low, SugarQuality.Auto(2048));
            Assert.AreEqual(GraphicsQuality.Medium, SugarQuality.Auto(4096));
            Assert.AreEqual(GraphicsQuality.High, SugarQuality.Auto(8192));
            Assert.AreEqual(GraphicsQuality.High, SugarQuality.Auto(0), "memorie necunoscută: calitate înaltă");
        }
    }
}
