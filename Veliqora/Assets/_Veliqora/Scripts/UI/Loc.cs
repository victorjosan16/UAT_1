using UnityEngine;
using Veliqora.Core.Localization;

namespace Veliqora.UI
{
    /// <summary>
    /// Global access to the active string table (Resources/Veliqora/Config/strings_&lt;lang&gt;.json). English source
    /// strings are the keys, so missing translations fall back to English automatically.
    /// </summary>
    public static class Loc
    {
        public static Localizer Current { get; private set; } = Localizer.English;
        public static readonly string[] Supported = { "en", "ro" };

        public static string DeviceLanguage() => Application.systemLanguage == SystemLanguage.Romanian ? "ro" : "en";

        public static void Load(string language)
        {
            if (string.IsNullOrEmpty(language)) language = DeviceLanguage();
            if (language == "en") { Current = Localizer.English; return; }
            var text = Resources.Load<TextAsset>("Veliqora/Config/strings_" + language);
            try { Current = text != null ? Localizer.FromJson(text.text) : Localizer.English; }
            catch (System.Exception e) { Debug.LogWarning("[Veliqora] strings_" + language + " invalid: " + e.Message); Current = Localizer.English; }
        }

        public static string T(string english) => Current.T(english);
        public static string F(string englishFormat, params object[] args) => Current.F(englishFormat, args);
        public static string LanguageName(string code) => code == "ro" ? "Română" : "English";
    }
}
