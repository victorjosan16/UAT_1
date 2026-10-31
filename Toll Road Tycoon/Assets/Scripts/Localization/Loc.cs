using System;
using System.Collections.Generic;

namespace TollRoadTycoon.Localization
{
    public sealed class LanguageInfo
    {
        public string Code;
        public string NativeName;
        /// <summary>Unity SystemLanguage names that map to this language.</summary>
        public string[] SystemLanguages;
    }

    /// <summary>
    /// String tables. English is the default and the fallback for any missing text; the other
    /// languages cover Romania and its region. Tables live in Strings/Loc.*.cs.
    /// </summary>
    public static partial class Loc
    {
        public const string English = "en";
        public const string Romanian = "ro";
        public const string Default = English;

        public static readonly IReadOnlyList<LanguageInfo> Languages = new[]
        {
            new LanguageInfo { Code = "en", NativeName = "English", SystemLanguages = new[] { "English" } },
            new LanguageInfo { Code = "ro", NativeName = "Română", SystemLanguages = new[] { "Romanian" } },
            new LanguageInfo { Code = "hu", NativeName = "Magyar", SystemLanguages = new[] { "Hungarian" } },
            new LanguageInfo { Code = "bg", NativeName = "Български", SystemLanguages = new[] { "Bulgarian" } },
            new LanguageInfo { Code = "uk", NativeName = "Українська", SystemLanguages = new[] { "Ukrainian" } },
            new LanguageInfo { Code = "ru", NativeName = "Русский", SystemLanguages = new[] { "Russian", "Belarusian" } },
            new LanguageInfo { Code = "pl", NativeName = "Polski", SystemLanguages = new[] { "Polish" } },
            new LanguageInfo { Code = "de", NativeName = "Deutsch", SystemLanguages = new[] { "German" } },
            new LanguageInfo { Code = "tr", NativeName = "Türkçe", SystemLanguages = new[] { "Turkish" } },
        };

        private static string _language = Default;

        public static event Action LanguageChanged;

        public static string Language
        {
            get => _language;
            set
            {
                string lang = IsSupported(value) ? value : Default;
                if (lang == _language) return;
                _language = lang;
                LanguageChanged?.Invoke();
            }
        }

        public static bool IsSupported(string code) => Table(code) != null;

        public static LanguageInfo Info(string code)
        {
            foreach (var l in Languages)
            {
                if (l.Code == code) return l;
            }
            return Languages[0];
        }

        /// <summary>Language for a device language name (Unity SystemLanguage), English when unsupported.</summary>
        public static string FromSystemLanguage(string systemLanguage)
        {
            foreach (var l in Languages)
            {
                if (Array.IndexOf(l.SystemLanguages, systemLanguage) >= 0) return l.Code;
            }
            return Default;
        }

        /// <summary>Next language in the list (wraps around), for the settings selector.</summary>
        public static string Cycle(string code, int direction)
        {
            int index = 0;
            for (int i = 0; i < Languages.Count; i++)
            {
                if (Languages[i].Code == code) index = i;
            }
            index = ((index + direction) % Languages.Count + Languages.Count) % Languages.Count;
            return Languages[index].Code;
        }

        public static string T(string key)
        {
            var table = Table(_language);
            if (table != null && table.TryGetValue(key, out var text)) return text;
            if (En.TryGetValue(key, out text)) return text;
            return key;
        }

        public static string F(string key, params object[] args) => string.Format(T(key), args);

        public static IEnumerable<string> Keys(string language) => Table(language)?.Keys ?? (IEnumerable<string>)Array.Empty<string>();

        public static string Get(string language, string key)
        {
            var table = Table(language);
            return table != null && table.TryGetValue(key, out var text) ? text : null;
        }

        private static Dictionary<string, string> Table(string code)
        {
            switch (code)
            {
                case "en": return En;
                case "ro": return Ro;
                case "hu": return Hu;
                case "bg": return Bg;
                case "uk": return Uk;
                case "ru": return Ru;
                case "pl": return Pl;
                case "de": return De;
                case "tr": return Tr;
                default: return null;
            }
        }
    }
}
