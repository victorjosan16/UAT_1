using NUnit.Framework;
using UndergroundMetroTycoon.Simulation;

namespace UndergroundMetroTycoon.Tests
{
    public class ContentValidatorTests
    {
        [Test]
        public void DefaultContent_IsValid()
        {
            CollectionAssert.IsEmpty(ContentValidator.Validate(DefaultContent.Create()));
        }

        [Test]
        public void MissingRepeatableContract_IsReportedAsSoftLock()
        {
            GameContent content = DefaultContent.Create();
            content.GetContract(DefaultContent.SmallTrenchId).Repeatable = false;
            Assert.IsTrue(ContentValidator.Validate(content).Exists(e => e.Contains("repeatable")));
        }

        [Test]
        public void StarterContractNeedingBetterTool_IsReported()
        {
            GameContent content = DefaultContent.Create();
            content.GetContract(DefaultContent.GardenTrenchId).Layers[0].Material = TerrainMaterial.Stone;
            Assert.IsTrue(ContentValidator.Validate(content).Exists(e => e.Contains("starter")));
        }

        [Test]
        public void PitWithoutStep_IsReportedAsTrap()
        {
            GameContent content = DefaultContent.Create();
            content.GetContract(DefaultContent.ResidentialFoundationId).Layers[1].MinZ = 0;
            Assert.IsTrue(ContentValidator.Validate(content).Exists(e => e.Contains("way out")));
        }

        [Test]
        public void LayerOutsideSite_IsReported()
        {
            GameContent content = DefaultContent.Create();
            content.GetContract(DefaultContent.GardenTrenchId).Layers[0].MaxX = 99;
            Assert.IsTrue(ContentValidator.Validate(content).Exists(e => e.Contains("outside")));
        }
    }
}
