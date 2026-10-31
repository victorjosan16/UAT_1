using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TollRoadTycoon.Localization;

namespace TollRoadTycoon.Tests
{
    public class LocalizationTests
    {
        private static readonly Regex Placeholder = new Regex(@"\{\d+(:[^}]*)?\}");

        private static string[] Codes => Loc.Languages.Select(l => l.Code).ToArray();

        [TestCaseSource(nameof(Codes))]
        public void LanguageHasExactlyTheEnglishKeys(string code)
        {
            var missing = Loc.Keys(Loc.English).Except(Loc.Keys(code)).ToList();
            var extra = Loc.Keys(code).Except(Loc.Keys(Loc.English)).ToList();
            CollectionAssert.IsEmpty(missing, code + " is missing keys");
            CollectionAssert.IsEmpty(extra, code + " has unknown keys");
        }

        [TestCaseSource(nameof(Codes))]
        public void PlaceholdersMatchEnglish(string code)
        {
            foreach (var key in Loc.Keys(Loc.English))
            {
                var expected = Placeholder.Matches(Loc.Get(Loc.English, key)).Cast<Match>().Select(m => m.Value).OrderBy(x => x).ToList();
                var actual = Placeholder.Matches(Loc.Get(code, key)).Cast<Match>().Select(m => m.Value).OrderBy(x => x).ToList();
                CollectionAssert.AreEqual(expected, actual, $"{code}: {key}");
                Assert.IsNotEmpty(Loc.Get(code, key).Trim(), $"{code}: {key} is empty");
                // Every text must format without throwing.
                Assert.DoesNotThrow(() => string.Format(Loc.Get(code, key), 1, 2, 3));
            }
        }

        [Test]
        public void EnglishIsTheDefaultAndFallback()
        {
            Assert.AreEqual("en", Loc.Default);
            Assert.AreEqual("en", Loc.FromSystemLanguage("Japanese"));
            Assert.AreEqual("ro", Loc.FromSystemLanguage("Romanian"));
            Assert.AreEqual("ru", Loc.FromSystemLanguage("Belarusian"));
            Loc.Language = "xx";
            Assert.AreEqual("en", Loc.Language);
            Assert.AreEqual("PLAY", Loc.T("menu.play"));
            Loc.Language = "ro";
            Assert.AreEqual("JOACĂ", Loc.T("menu.play"));
            Loc.Language = "de";
            Assert.AreEqual("SPIELEN", Loc.T("menu.play"));
            Loc.Language = Loc.Default;
            Assert.AreEqual("unknown.key", Loc.T("unknown.key"));
        }

        [Test]
        public void CycleVisitsEveryLanguage()
        {
            string code = Loc.Default;
            var seen = new System.Collections.Generic.HashSet<string>();
            for (int i = 0; i < Loc.Languages.Count; i++)
            {
                seen.Add(code);
                code = Loc.Cycle(code, 1);
            }
            Assert.AreEqual(Loc.Languages.Count, seen.Count);
            Assert.AreEqual(Loc.Default, code);
            Assert.AreEqual(Loc.Languages[Loc.Languages.Count - 1].Code, Loc.Cycle(Loc.Default, -1));
        }
    }
}
