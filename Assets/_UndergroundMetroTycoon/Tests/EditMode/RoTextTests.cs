using NUnit.Framework;
using UndergroundMetroTycoon.Simulation;

namespace UndergroundMetroTycoon.Tests
{
    public class RoTextTests
    {
        [TestCase(1, "1 bloc")]
        [TestCase(7, "7 blocuri")]
        [TestCase(19, "19 blocuri")]
        [TestCase(20, "20 de blocuri")]
        [TestCase(54, "54 de blocuri")]
        [TestCase(101, "101 blocuri")]
        [TestCase(112, "112 blocuri")]
        [TestCase(120, "120 de blocuri")]
        [TestCase(200, "200 de blocuri")]
        public void Count_UsesRomanianDeRule(int n, string expected)
        {
            Assert.AreEqual(expected, RoText.Count(n, "bloc", "blocuri"));
        }

        [Test]
        public void Objectives_ForDefaultContracts()
        {
            GameContent content = DefaultContent.Create();
            Assert.AreEqual("Sapă 20 de blocuri de pământ", RoText.ContractObjective(content.GetContract(DefaultContent.GardenTrenchId)));
            Assert.AreEqual("Sapă 12 blocuri de pământ", RoText.ContractObjective(content.GetContract(DefaultContent.SmallTrenchId)));
            Assert.AreEqual("Sapă 54 de blocuri de pământ", RoText.ContractObjective(content.GetContract(DefaultContent.ResidentialFoundationId)));
        }

        [Test]
        public void Objective_ForRock_OmitsSoil()
        {
            var contract = new ContractDefinition { SiteWidth = 1, SiteLength = 1, Layers = { new ContractLayerSpec { Material = TerrainMaterial.Stone } } };
            Assert.AreEqual("Sapă 1 bloc", RoText.ContractObjective(contract));
        }

        [Test]
        public void Difficulty_IsTranslated()
        {
            Assert.AreEqual("UȘOR", RoText.Difficulty(ContractDifficulty.Beginner));
            Assert.AreEqual("MEDIU", RoText.Difficulty(ContractDifficulty.Intermediate));
            Assert.AreEqual("DIFICIL", RoText.Difficulty(ContractDifficulty.Advanced));
        }
    }
}
