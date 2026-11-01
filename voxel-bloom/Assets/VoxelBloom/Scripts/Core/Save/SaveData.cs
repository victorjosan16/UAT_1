using System;
using System.Collections.Generic;

namespace VoxelBloom.Core
{
    public sealed class SettingsData
    {
        public bool Music = true, Sound = true, Vibration = true, ReducedEffects;
        public int Quality = 2; // 0 Low, 1 Medium, 2 High
        public float MusicVolume = 0.6f, SoundVolume = 0.9f;
    }

    /// <summary>All persistent player state. Serialized as versioned JSON by SaveSerializer.</summary>
    public sealed class SaveData
    {
        public const int CurrentVersion = 2;

        public int Version = CurrentVersion;
        public int Coins;
        public int Lives;
        public long NextLifeAt;
        public int HighestUnlockedLevel = 1;
        public int LastPlayedLevel = 1;
        /// <summary>Best stars per completed level id (1..3).</summary>
        public readonly Dictionary<int, int> Stars = new Dictionary<int, int>();
        public readonly Dictionary<BoosterType, int> Boosters = new Dictionary<BoosterType, int>();
        public readonly HashSet<string> CollectionUnlocked = new HashSet<string>();
        public long DailyLastClaimDay = -1;
        public int DailyStreak;
        public SettingsData Settings = new SettingsData();
        /// <summary>Snapshot of an unfinished level (GameSession.ToSnapshot) or null.</summary>
        public Dictionary<string, object> ActiveLevel;
        /// <summary>Receipt id of the last level completion and whether its reward was already doubled.</summary>
        public long LastReceiptId;
        public bool LastReceiptDoubled;
        public long ReceiptCounter;
        public int TotalScore;

        public static SaveData CreateNew(GameConfig cfg)
        {
            var s = new SaveData
            {
                Coins = cfg?.Economy.StartCoins ?? 500,
                Lives = cfg?.Economy.StartLives ?? 5,
            };
            if (cfg != null)
                foreach (var kv in cfg.Boosters) s.Boosters[kv.Key] = kv.Value.StartCount;
            return s;
        }

        public int TotalStars
        {
            get { int t = 0; foreach (var v in Stars.Values) t += v; return t; }
        }
    }

    /// <summary>Versioned JSON (de)serialization with forward migration.</summary>
    public static class SaveSerializer
    {
        public static string ToJson(SaveData s)
        {
            var stars = new Dictionary<string, object>();
            foreach (var kv in s.Stars) stars[kv.Key.ToString()] = (double)kv.Value;
            var boosters = new Dictionary<string, object>();
            foreach (var kv in s.Boosters) boosters[kv.Key.ToString()] = (double)kv.Value;
            var coll = new List<object>();
            foreach (var id in s.CollectionUnlocked) coll.Add(id);
            var st = s.Settings ?? new SettingsData();
            var d = new Dictionary<string, object>
            {
                { "version", (double)SaveData.CurrentVersion },
                { "coins", (double)s.Coins },
                { "lives", (double)s.Lives },
                { "nextLifeAt", (double)s.NextLifeAt },
                { "highestUnlockedLevel", (double)s.HighestUnlockedLevel },
                { "lastPlayedLevel", (double)s.LastPlayedLevel },
                { "stars", stars },
                { "boosters", boosters },
                { "collection", coll },
                { "dailyLastClaimDay", (double)s.DailyLastClaimDay },
                { "dailyStreak", (double)s.DailyStreak },
                { "settings", new Dictionary<string, object>
                    {
                        { "music", st.Music }, { "sound", st.Sound }, { "vibration", st.Vibration },
                        { "reducedEffects", st.ReducedEffects }, { "quality", (double)st.Quality },
                        { "musicVolume", (double)st.MusicVolume }, { "soundVolume", (double)st.SoundVolume },
                    }
                },
                { "activeLevel", s.ActiveLevel },
                { "lastReceiptId", (double)s.LastReceiptId },
                { "lastReceiptDoubled", s.LastReceiptDoubled },
                { "receiptCounter", (double)s.ReceiptCounter },
                { "totalScore", (double)s.TotalScore },
            };
            return MiniJson.Serialize(d);
        }

        /// <summary>Parses and migrates. Throws FormatException on corrupt data.</summary>
        public static SaveData FromJson(string json, GameConfig cfg)
        {
            var d = MiniJson.Obj(MiniJson.Parse(json)) ?? throw new FormatException("Save root must be an object");
            int version = MiniJson.GetInt(d, "version", 1);
            if (version > SaveData.CurrentVersion) throw new FormatException("Save version " + version + " is newer than supported");
            var s = SaveData.CreateNew(cfg);
            s.Coins = Math.Max(0, MiniJson.GetInt(d, "coins", s.Coins));
            s.Lives = Math.Max(0, MiniJson.GetInt(d, "lives", s.Lives));
            s.NextLifeAt = MiniJson.GetLong(d, "nextLifeAt");
            s.HighestUnlockedLevel = Math.Max(1, MiniJson.GetInt(d, "highestUnlockedLevel", 1));
            s.LastPlayedLevel = Math.Max(1, MiniJson.GetInt(d, "lastPlayedLevel", 1));
            var stars = MiniJson.GetObj(d, "stars");
            if (stars != null)
                foreach (var kv in stars)
                    if (int.TryParse(kv.Key, out int id) && kv.Value is double v) s.Stars[id] = Math.Max(1, Math.Min(3, (int)v));
            var boosters = MiniJson.GetObj(d, "boosters");
            if (boosters != null)
                foreach (var kv in boosters)
                    if (Enum.TryParse(kv.Key, out BoosterType t) && kv.Value is double v) s.Boosters[t] = Math.Max(0, (int)v);
            foreach (var o in MiniJson.GetArr(d, "collection") ?? new List<object>()) if (o is string id) s.CollectionUnlocked.Add(id);
            s.DailyLastClaimDay = MiniJson.GetLong(d, "dailyLastClaimDay", -1);
            s.DailyStreak = MiniJson.GetInt(d, "dailyStreak");
            var st = MiniJson.GetObj(d, "settings");
            if (st != null)
            {
                s.Settings.Music = MiniJson.GetBool(st, "music", true);
                s.Settings.Sound = MiniJson.GetBool(st, "sound", true);
                s.Settings.Vibration = MiniJson.GetBool(st, "vibration", true);
                s.Settings.ReducedEffects = MiniJson.GetBool(st, "reducedEffects");
                s.Settings.Quality = Math.Max(0, Math.Min(2, MiniJson.GetInt(st, "quality", 2)));
                s.Settings.MusicVolume = (float)MiniJson.GetDouble(st, "musicVolume", 0.6);
                s.Settings.SoundVolume = (float)MiniJson.GetDouble(st, "soundVolume", 0.9);
            }
            s.ActiveLevel = MiniJson.GetObj(d, "activeLevel");
            s.LastReceiptId = MiniJson.GetLong(d, "lastReceiptId");
            s.LastReceiptDoubled = MiniJson.GetBool(d, "lastReceiptDoubled");
            s.ReceiptCounter = MiniJson.GetLong(d, "receiptCounter");
            s.TotalScore = MiniJson.GetInt(d, "totalScore");

            // ---- migrations
            if (version < 2)
            {
                // v1 had no Eliminate booster: grant the starting amount once.
                if (cfg != null && cfg.Boosters.TryGetValue(BoosterType.Eliminate, out var e) && !s.Boosters.ContainsKey(BoosterType.Eliminate))
                    s.Boosters[BoosterType.Eliminate] = e.StartCount;
            }
            s.Version = SaveData.CurrentVersion;
            return s;
        }
    }
}
