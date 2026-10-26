using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

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

        [Test]
        public void EveryRow_HasAllLanguagesWithTheSamePlaceholders()
        {
            int languages = System.Enum.GetValues(typeof(SugarLanguage)).Length;
            foreach (string[] row in SugarLoc.Rows)
            {
                Assert.AreEqual(languages, row.Length, "Rând incomplet: " + row[0]);
                string placeholders = Placeholders(row[0]);
                foreach (string text in row)
                {
                    Assert.IsFalse(string.IsNullOrWhiteSpace(text), "Traducere goală pentru: " + row[0]);
                    Assert.AreEqual(placeholders, Placeholders(text), "Alte {0}/{1} în: " + text);
                }
            }
        }

        [Test]
        public void PhoneLanguage_FallsBackToEnglish()
        {
            Assert.AreEqual(SugarLanguage.Romanian, SugarLoc.FromSystem(SystemLanguage.Romanian));
            Assert.AreEqual(SugarLanguage.Spanish, SugarLoc.FromSystem(SystemLanguage.Spanish));
            Assert.AreEqual(SugarLanguage.English, SugarLoc.FromSystem(SystemLanguage.Japanese));
            Assert.AreEqual(SugarLanguage.English, SugarLoc.FromSystem(SystemLanguage.Unknown));
        }

        [Test]
        public void Translate_PicksTheRequestedLanguage()
        {
            Assert.AreEqual("JUGAR", SugarLoc.Translate("JOACĂ", SugarLanguage.Spanish));
            Assert.AreEqual("SPIELEN", SugarLoc.Translate("JOACĂ", SugarLanguage.German));
            Assert.AreEqual("JOACĂ", SugarLoc.Translate("JOACĂ", SugarLanguage.Romanian));
        }

        [Test]
        public void LanguageCodes_AreUniqueAndNamed()
        {
            var codes = new System.Collections.Generic.HashSet<string>();
            foreach (SugarLanguage language in SugarLoc.Languages)
            {
                Assert.IsTrue(codes.Add(SugarLoc.Code(language)));
                Assert.IsFalse(string.IsNullOrEmpty(SugarLoc.NativeName(language)));
            }
            Assert.AreEqual(SugarLanguage.English, SugarLoc.Languages[0], "Engleza e prima (limba implicită)");
        }

        private static string Placeholders(string text)
        {
            var found = new System.Collections.Generic.SortedSet<string>();
            foreach (Match match in Regex.Matches(text, @"\{\d\}")) found.Add(match.Value);
            return string.Join(",", found);
        }
    }
}
