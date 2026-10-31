using System;
using System.Collections.Generic;
using System.Globalization;
using Veliqora.Core.Json;

namespace Veliqora.Core.Localization
{
    /// <summary>
    /// Language-ready string table. Keys are the English source strings, so untranslated text safely falls back to
    /// English and adding a language is just adding a JSON file: {"lang":"ro","strings":{"Play":"Joacă", …}}.
    /// Format strings use {0}, {1} placeholders.
    /// </summary>
    public sealed class Localizer
    {
        private readonly Dictionary<string, string> _table;
        public string Language { get; }

        public Localizer(string language, Dictionary<string, string> table)
        {
            Language = string.IsNullOrEmpty(language) ? "en" : language;
            _table = table ?? new Dictionary<string, string>();
        }

        public static Localizer English => new Localizer("en", null);

        public static Localizer FromJson(string json)
        {
            var root = MiniJson.AsObject(MiniJson.Parse(json));
            var table = new Dictionary<string, string>();
            foreach (var kv in MiniJson.GetObject(root, "strings"))
                if (kv.Value is string v && !string.IsNullOrEmpty(v)) table[kv.Key] = v;
            return new Localizer(MiniJson.GetString(root, "lang", "en"), table);
        }

        public int Count => _table.Count;
        public bool Has(string key) => _table.ContainsKey(key);

        public string T(string english)
        {
            if (string.IsNullOrEmpty(english)) return english;
            string v;
            return _table.TryGetValue(english, out v) ? v : english;
        }

        public string F(string englishFormat, params object[] args)
        {
            string fmt = T(englishFormat);
            try { return string.Format(CultureInfo.InvariantCulture, fmt, args); }
            catch (FormatException) { return string.Format(CultureInfo.InvariantCulture, englishFormat, args); }
        }
    }
}
