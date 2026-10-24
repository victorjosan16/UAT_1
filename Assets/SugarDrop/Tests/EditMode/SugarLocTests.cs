using NUnit.Framework;

namespace SugarDrop.Tests
{
    public class SugarLocTests
    {
        [Test]
        public void Translate_UsesEnglishOnlyWhenAsked()
        {
            Assert.AreEqual("PLAY", SugarLoc.Translate("JOACĂ", true));
            Assert.AreEqual("JOACĂ", SugarLoc.Translate("JOACĂ", false));
        }

        [Test]
        public void UnknownText_IsReturnedUnchanged()
        {
            Assert.AreEqual("xyz", SugarLoc.Translate("xyz", true));
        }

        [Test]
        public void FormatStrings_KeepTheirPlaceholders()
        {
            Assert.AreEqual("LEVEL 7", string.Format(SugarLoc.Translate("NIVELUL {0}", true), 7));
        }
    }
}
