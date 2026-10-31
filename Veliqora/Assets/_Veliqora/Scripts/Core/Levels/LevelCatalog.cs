using System;
using System.Collections.Generic;
using Veliqora.Core.Json;

namespace Veliqora.Core.Levels
{
    /// <summary>Versioned collection of levels, serialized as JSON (Resources/Veliqora/Levels/levels.json).</summary>
    public sealed class LevelCatalog
    {
        public const int CurrentGeneratorVersion = 1;

        public int ContentVersion = 4; // 4: no tutorial levels, complete puzzles from level 1, 8-color progression
        public int GeneratorVersion = CurrentGeneratorVersion;
        public ulong BaseSeed;
        public string GeneratedAtUtc = "";
        public List<LevelDefinition> Levels = new List<LevelDefinition>();

        public string ToJson()
        {
            var levels = new List<object>();
            foreach (var l in Levels) levels.Add(l.ToJson());
            var root = new Dictionary<string, object>
            {
                ["contentVersion"] = (long)ContentVersion,
                ["generatorVersion"] = (long)GeneratorVersion,
                ["baseSeed"] = BaseSeed.ToString(),
                ["generatedAtUtc"] = GeneratedAtUtc,
                ["levelCount"] = (long)Levels.Count,
                ["levels"] = levels,
            };
            return MiniJson.Serialize(root, pretty: true);
        }

        public static LevelCatalog FromJson(string json)
        {
            var root = MiniJson.AsObject(MiniJson.Parse(json));
            var cat = new LevelCatalog
            {
                ContentVersion = MiniJson.GetInt(root, "contentVersion", 1),
                GeneratorVersion = MiniJson.GetInt(root, "generatorVersion", 1),
                BaseSeed = ulong.Parse(MiniJson.GetString(root, "baseSeed", "0")),
                GeneratedAtUtc = MiniJson.GetString(root, "generatedAtUtc", ""),
            };
            foreach (var o in MiniJson.GetList(root, "levels")) cat.Levels.Add(LevelDefinition.FromJson(MiniJson.AsObject(o)));
            cat.Levels.Sort((a, b) => a.Number.CompareTo(b.Number));
            return cat;
        }
    }

    /// <summary>Read-only access to the level catalog by 1-based level number.</summary>
    public sealed class LevelRepository
    {
        private readonly LevelCatalog _catalog;

        public LevelRepository(LevelCatalog catalog)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            for (int i = 0; i < catalog.Levels.Count; i++)
                if (catalog.Levels[i].Number != i + 1)
                    throw new FormatException("Level numbers must be contiguous from 1; found " + catalog.Levels[i].Number + " at index " + i);
        }

        public int Count => _catalog.Levels.Count;
        public int ContentVersion => _catalog.ContentVersion;
        public LevelCatalog Catalog => _catalog;

        public bool Has(int number) => number >= 1 && number <= Count;

        public LevelDefinition Get(int number)
        {
            if (!Has(number)) throw new ArgumentOutOfRangeException(nameof(number), "No level " + number);
            return _catalog.Levels[number - 1];
        }
    }
}
