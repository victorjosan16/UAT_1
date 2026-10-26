using NUnit.Framework;

namespace SugarDrop.Tests
{
    public class ChapterThemeTests
    {
        [Test]
        public void ThemeChangesEveryTenLevels_AndRepeatsAfterTheLast()
        {
            Assert.AreEqual(ChapterTheme.ForLevel(1).Name, ChapterTheme.ForLevel(10).Name);
            Assert.AreNotEqual(ChapterTheme.ForLevel(10).Name, ChapterTheme.ForLevel(11).Name);
            Assert.AreEqual(ChapterTheme.For(0).Name, ChapterTheme.For(ChapterTheme.Count).Name);
            Assert.AreEqual(0, ChapterTheme.ChapterOf(0));
        }

        [Test]
        public void EveryThemeName_IsTranslated()
        {
            for (int i = 0; i < ChapterTheme.Count; i++)
            {
                string name = ChapterTheme.For(i).Name;
                Assert.AreNotEqual(name, SugarLoc.Translate(name, SugarLanguage.English), "Lipsește traducerea pentru: " + name);
            }
        }
    }
}
