using System;
using System.Collections.Generic;

namespace VoxelBloom.Core
{
    public sealed class ChapterInfo
    {
        public int Id, FirstLevel, LastLevel, UnlockStars;
        public string Name, ThumbnailModel;
        public int LevelCount => LastLevel - FirstLevel + 1;
        public bool Contains(int level) => level >= FirstLevel && level <= LastLevel;
    }

    public sealed class CollectionEntry
    {
        public string Id, Name, Category;
        public int Voxels;
        /// <summary>Level that unlocks this model (0 = no level uses it).</summary>
        public int UnlockLevel;
    }

    public sealed class BoosterConfig
    {
        public int Price, StartCount, Voxels;
    }

    public sealed class EconomyConfig
    {
        public int StartCoins = 500, StartLives = 5, MaxLives = 5, LifeRegenMinutes = 30, ComboCoinBonus = 5;
        public int[] DailyRewards = { 100, 150, 200, 250, 300, 400, 600 };
    }

    public sealed class GameConfig
    {
        public int Version;
        public List<ChapterInfo> Chapters = new List<ChapterInfo>();
        public List<CollectionEntry> Collection = new List<CollectionEntry>();
        public EconomyConfig Economy = new EconomyConfig();
        public readonly Dictionary<BoosterType, BoosterConfig> Boosters = new Dictionary<BoosterType, BoosterConfig>();
        public int HardEvery = 5;
        public float HardRewardMultiplier = 2.5f;

        public int TotalLevels => Chapters.Count == 0 ? 0 : Chapters[Chapters.Count - 1].LastLevel;

        public ChapterInfo ChapterOf(int level)
        {
            foreach (var c in Chapters) if (c.Contains(level)) return c;
            return null;
        }

        public BoosterConfig Booster(BoosterType t) => Boosters.TryGetValue(t, out var b) ? b : new BoosterConfig { Price = int.MaxValue };

        public static GameConfig FromJson(string json)
        {
            var d = MiniJson.Obj(MiniJson.Parse(json)) ?? throw new FormatException("Config is not an object");
            var cfg = new GameConfig { Version = MiniJson.GetInt(d, "version", 1) };
            foreach (var o in MiniJson.GetArr(d, "chapters") ?? new List<object>())
            {
                var c = MiniJson.Obj(o);
                cfg.Chapters.Add(new ChapterInfo
                {
                    Id = MiniJson.GetInt(c, "id"),
                    Name = MiniJson.GetString(c, "name", "Chapter"),
                    FirstLevel = MiniJson.GetInt(c, "firstLevel"),
                    LastLevel = MiniJson.GetInt(c, "lastLevel"),
                    UnlockStars = MiniJson.GetInt(c, "unlockStars"),
                    ThumbnailModel = MiniJson.GetString(c, "thumbnailModel", ""),
                });
            }
            foreach (var o in MiniJson.GetArr(d, "collection") ?? new List<object>())
            {
                var c = MiniJson.Obj(o);
                cfg.Collection.Add(new CollectionEntry
                {
                    Id = MiniJson.GetString(c, "id"),
                    Name = MiniJson.GetString(c, "name"),
                    Category = MiniJson.GetString(c, "category", "Animals"),
                    Voxels = MiniJson.GetInt(c, "voxels"),
                    UnlockLevel = MiniJson.GetInt(c, "unlockLevel"),
                });
            }
            var e = MiniJson.GetObj(d, "economy");
            if (e != null)
            {
                cfg.Economy.StartCoins = MiniJson.GetInt(e, "startCoins", 500);
                cfg.Economy.StartLives = MiniJson.GetInt(e, "startLives", 5);
                cfg.Economy.MaxLives = MiniJson.GetInt(e, "maxLives", 5);
                cfg.Economy.LifeRegenMinutes = Math.Max(1, MiniJson.GetInt(e, "lifeRegenMinutes", 30));
                cfg.Economy.ComboCoinBonus = MiniJson.GetInt(e, "comboCoinBonus", 5);
                var dr = MiniJson.GetIntArray(e, "dailyRewards");
                if (dr.Length > 0) cfg.Economy.DailyRewards = dr;
            }
            var h = MiniJson.GetObj(d, "hardLevels");
            if (h != null)
            {
                cfg.HardEvery = MiniJson.GetInt(h, "every", 5);
                cfg.HardRewardMultiplier = (float)MiniJson.GetDouble(h, "rewardMultiplier", 2.5);
            }
            var b = MiniJson.GetObj(d, "boosters");
            ReadBooster(b, "hammer", BoosterType.Hammer, cfg);
            ReadBooster(b, "shuffle", BoosterType.Shuffle, cfg);
            ReadBooster(b, "colorBomb", BoosterType.ColorBomb, cfg);
            ReadBooster(b, "eliminate", BoosterType.Eliminate, cfg);
            ReadBooster(b, "undo", BoosterType.Undo, cfg);
            ReadBooster(b, "extraStation", BoosterType.ExtraStation, cfg);
            ReadBooster(b, "colorShuffle", BoosterType.ColorShuffle, cfg);
            return cfg;
        }

        static void ReadBooster(Dictionary<string, object> b, string key, BoosterType t, GameConfig cfg)
        {
            var o = MiniJson.GetObj(b, key);
            if (o == null) return;
            cfg.Boosters[t] = new BoosterConfig
            {
                Price = MiniJson.GetInt(o, "price", 999999),
                StartCount = MiniJson.GetInt(o, "startCount"),
                Voxels = MiniJson.GetInt(o, "voxels"),
            };
        }
    }
}
