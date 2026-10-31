using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Veliqora.Core.Localization;
using Veliqora.Core.Progression;

namespace Veliqora.Tests.EditMode
{
    /// <summary>The Romanian table must cover every UI string passed to Loc.T / Loc.F and every config title.</summary>
    public class LocalizationTests
    {
        private static Localizer Ro() =>
            Localizer.FromJson(File.ReadAllText(TestUtil.FindProjectFile("Assets/_Veliqora/Resources/Veliqora/Config/strings_ro.json")));

        private static IEnumerable<string> CodeStrings()
        {
            string scripts = Directory.GetParent(Path.GetDirectoryName(TestUtil.FindProjectFile("Assets/_Veliqora/Scripts/UI/Loc.cs"))).FullName;
            // Simple, direct calls only: Loc.T("literal") / Loc.F("literal", ...)
            var rx = new Regex("Loc\\.[TF]\\(\"((?:[^\"\\\\]|\\\\.)*)\"");
            foreach (var file in Directory.GetFiles(scripts, "*.cs", SearchOption.AllDirectories))
                foreach (Match m in rx.Matches(File.ReadAllText(file)))
                    yield return Regex.Unescape(m.Groups[1].Value);
        }

        [Test]
        public void Romanian_CoversEveryDirectLocCall()
        {
            var ro = Ro();
            var missing = new List<string>();
            foreach (var s in CodeStrings())
                if (!ro.Has(s)) missing.Add(s);
            Assert.IsEmpty(missing, "untranslated: " + string.Join(" | ", missing));
        }

        [Test]
        public void Romanian_CoversAchievementTitles()
        {
            var ro = Ro();
            var cfg = ProgressionConfig.FromJson(File.ReadAllText(TestUtil.FindProjectFile("Assets/_Veliqora/Resources/Veliqora/Config/progression.json")));
            foreach (var a in cfg.Achievements) Assert.IsTrue(ro.Has(a.Title), a.Title);
        }

        [Test]
        public void Formats_KeepPlaceholders()
        {
            var ro = Ro();
            string lvl = ro.F("Level {0}", 7);
            StringAssert.Contains("7", lvl);
            Assert.AreNotEqual("Level 7", lvl);
            StringAssert.Contains("3", ro.F("Moves: {0}", 3));
            Assert.AreEqual("Totally new", ro.T("Totally new"), "missing keys fall back to English");
        }
    }
}
