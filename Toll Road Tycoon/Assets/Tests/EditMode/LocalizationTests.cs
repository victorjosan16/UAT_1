using System.Linq;
using NUnit.Framework;
using TollRoadTycoon.Localization;

namespace TollRoadTycoon.Tests
{
    public class LocalizationTests
    {
        [Test]
        public void EveryRomanianKeyHasAnEnglishTranslation()
        {
            var missing = Loc.Keys(Loc.Romanian).Except(Loc.Keys(Loc.English)).ToList();
            var extra = Loc.Keys(Loc.English).Except(Loc.Keys(Loc.Romanian)).ToList();
            CollectionAssert.IsEmpty(missing, "missing in English");
            CollectionAssert.IsEmpty(extra, "missing in Romanian");
        }

        [Test]
        public void SwitchingLanguageChangesText()
        {
            Loc.Language = Loc.Romanian;
            Assert.AreEqual("JOACĂ", Loc.T("menu.play"));
            Loc.Language = Loc.English;
            Assert.AreEqual("PLAY", Loc.T("menu.play"));
            Loc.Language = Loc.Romanian;
            Assert.AreEqual("unknown.key", Loc.T("unknown.key"));
        }
    }
}
