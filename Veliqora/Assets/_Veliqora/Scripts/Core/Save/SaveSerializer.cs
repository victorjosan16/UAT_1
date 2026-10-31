using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Veliqora.Core.Economy;
using Veliqora.Core.Json;
using Veliqora.Core.Progression;
using Veliqora.Core.Session;

namespace Veliqora.Core.Save
{
    public sealed class SaveFormatException : Exception
    {
        public SaveFormatException(string message, Exception inner = null) : base(message, inner) { }
    }

    /// <summary>
    /// On-disk envelope: {"format":"veliqora-save","schemaVersion":N,"checksum":"fnv1a64","payload":{...}}.
    /// The checksum (FNV-1a 64 over the canonical payload JSON) detects truncation/corruption; it is not a
    /// security measure against deliberate tampering.
    /// </summary>
    public static class SaveSerializer
    {
        public const string Format = "veliqora-save";

        public static string Serialize(SaveData data)
        {
            var payload = ToPayload(data);
            string canonical = MiniJson.Serialize(payload);
            var envelope = new Dictionary<string, object>
            {
                ["format"] = Format,
                ["schemaVersion"] = (long)SaveData.CurrentSchemaVersion,
                ["checksum"] = Checksum(canonical),
                ["payload"] = payload,
            };
            return MiniJson.Serialize(envelope);
        }

        public static SaveData Deserialize(string json)
        {
            Dictionary<string, object> envelope;
            try { envelope = MiniJson.AsObject(MiniJson.Parse(json)); }
            catch (Exception e) { throw new SaveFormatException("Save file is not valid JSON", e); }

            if (MiniJson.GetString(envelope, "format", "") != Format) throw new SaveFormatException("Not a Veliqora save");
            int version = MiniJson.GetInt(envelope, "schemaVersion", 0);
            if (version <= 0) throw new SaveFormatException("Missing schema version");
            if (version > SaveData.CurrentSchemaVersion) throw new SaveFormatException("Save is from a newer app version (" + version + ")");
            if (!MiniJson.TryGet(envelope, "payload", out var payloadObj)) throw new SaveFormatException("Missing payload");
            var payload = MiniJson.AsObject(payloadObj);

            string expected = MiniJson.GetString(envelope, "checksum", "");
            string actual = Checksum(MiniJson.Serialize(payload));
            if (!string.Equals(expected, actual, StringComparison.Ordinal)) throw new SaveFormatException("Checksum mismatch (corrupted save)");

            payload = SaveMigrator.Migrate(payload, version);
            try { return FromPayload(payload); }
            catch (SaveFormatException) { throw; }
            catch (Exception e) { throw new SaveFormatException("Save payload is malformed", e); }
        }

        /// <summary>Wraps an arbitrary payload (any schema version) in a valid envelope. Used for migration tests.</summary>
        public static string WrapPayload(Dictionary<string, object> payload, int schemaVersion)
        {
            var envelope = new Dictionary<string, object>
            {
                ["format"] = Format,
                ["schemaVersion"] = (long)schemaVersion,
                ["checksum"] = Checksum(MiniJson.Serialize(payload)),
                ["payload"] = payload,
            };
            return MiniJson.Serialize(envelope);
        }

        public static string Checksum(string s)
        {
            ulong h = 14695981039346656037UL;
            var bytes = Encoding.UTF8.GetBytes(s);
            for (int i = 0; i < bytes.Length; i++) { h ^= bytes[i]; h *= 1099511628211UL; }
            return h.ToString("x16", CultureInfo.InvariantCulture);
        }

        // ---------------- mapping ----------------

        private static Dictionary<string, object> ToPayload(SaveData d)
        {
            var stars = new Dictionary<string, object>();
            foreach (var kv in d.Progress.BestStars) stars[kv.Key.ToString(CultureInfo.InvariantCulture)] = (long)kv.Value;
            var chests = new List<object>();
            foreach (var c in d.Progress.ClaimedChests) chests.Add((long)c);
            var inventory = new Dictionary<string, object>();
            foreach (var kv in d.Economy.Inventory) inventory[kv.Key.ToString()] = (long)kv.Value;
            var owned = new List<object>();
            foreach (var o in d.Economy.OwnedCosmetics) owned.Add(o);
            var ledger = new List<object>();
            foreach (var l in d.Economy.Ledger) ledger.Add(l);
            var tutorials = new List<object>();
            foreach (var t in d.TutorialsSeen) tutorials.Add(t);
            var s = d.Settings;

            return new Dictionary<string, object>
            {
                ["contentVersion"] = (long)d.ContentVersion,
                ["createdUtc"] = d.CreatedUtc ?? "",
                ["updatedUtc"] = d.UpdatedUtc ?? "",
                ["progress"] = new Dictionary<string, object>
                {
                    ["highestUnlocked"] = (long)d.Progress.HighestUnlocked,
                    ["currentLevel"] = (long)d.Progress.CurrentLevel,
                    ["bestStars"] = stars,
                    ["claimedChests"] = chests,
                },
                ["economy"] = new Dictionary<string, object>
                {
                    ["coins"] = d.Economy.Coins,
                    ["inventory"] = inventory,
                    ["owned"] = owned,
                    ["ledger"] = ledger,
                    ["startingGrant"] = d.Economy.StartingGrantApplied,
                },
                ["settings"] = new Dictionary<string, object>
                {
                    ["music"] = s.Music,
                    ["sound"] = s.Sound,
                    ["musicVolume"] = (double)s.MusicVolume,
                    ["soundVolume"] = (double)s.SoundVolume,
                    ["haptics"] = s.Haptics,
                    ["reducedMotion"] = s.ReducedMotion,
                    ["colorblind"] = s.ColorblindSymbols,
                    ["animationSpeed"] = (double)s.AnimationSpeed,
                    ["lightGameplay"] = s.LightGameplayBackground,
                    ["debugAdFailure"] = s.DebugSimulateAdFailure,
                    ["language"] = s.Language ?? "",
                },
                ["stats"] = new Dictionary<string, object>
                {
                    ["hard"] = (long)d.Stats.HardCompleted,
                    ["boss"] = (long)d.Stats.BossCompleted,
                    ["boosters"] = (long)d.Stats.BoostersUsed,
                    ["daily"] = (long)d.Stats.DailyClaims,
                    ["objectives"] = (long)d.Stats.ObjectivesCompleted,
                    ["mystery"] = (long)d.Stats.MysteryRevealed,
                    ["corks"] = (long)d.Stats.CorksOpened,
                },
                ["dailyReward"] = new Dictionary<string, object> { ["lastDay"] = (long)d.Daily.LastClaimDay, ["streak"] = (long)d.Daily.Streak },
                ["achievements"] = ToList(d.ClaimedAchievements),
                ["cosmetics"] = new Dictionary<string, object>
                {
                    ["theme"] = d.Cosmetics.Theme ?? "",
                    ["bottle"] = d.Cosmetics.Bottle ?? "",
                },
                ["tutorials"] = tutorials,
                ["session"] = d.Session?.ToJson(),
            };
        }

        private static List<object> ToList(HashSet<string> set)
        {
            var list = new List<object>();
            foreach (var x in set) list.Add(x);
            return list;
        }

        private static SaveData FromPayload(Dictionary<string, object> p)
        {
            var d = new SaveData
            {
                SchemaVersion = SaveData.CurrentSchemaVersion,
                ContentVersion = MiniJson.GetInt(p, "contentVersion", 1),
                CreatedUtc = MiniJson.GetString(p, "createdUtc", ""),
                UpdatedUtc = MiniJson.GetString(p, "updatedUtc", ""),
            };

            var prog = MiniJson.GetObject(p, "progress");
            d.Progress.HighestUnlocked = Math.Max(1, MiniJson.GetInt(prog, "highestUnlocked", 1));
            d.Progress.CurrentLevel = Math.Max(1, MiniJson.GetInt(prog, "currentLevel", 1));
            foreach (var kv in MiniJson.GetObject(prog, "bestStars"))
            {
                int level = int.Parse(kv.Key, CultureInfo.InvariantCulture);
                int stars = (int)MiniJson.ToLong(kv.Value);
                if (level >= 1 && stars >= 0 && stars <= 3) d.Progress.BestStars[level] = stars;
            }
            foreach (var c in MiniJson.GetList(prog, "claimedChests")) d.Progress.ClaimedChests.Add((int)MiniJson.ToLong(c));

            var eco = MiniJson.GetObject(p, "economy");
            d.Economy.Coins = Math.Max(0, MiniJson.GetLong(eco, "coins"));
            foreach (var kv in MiniJson.GetObject(eco, "inventory"))
            {
                if (Enum.TryParse(kv.Key, out BoosterType t)) d.Economy.Inventory[t] = Math.Max(0, (int)MiniJson.ToLong(kv.Value));
            }
            foreach (var o in MiniJson.GetList(eco, "owned")) if (o is string id) d.Economy.OwnedCosmetics.Add(id);
            foreach (var l in MiniJson.GetList(eco, "ledger")) if (l is string tx) d.Economy.Ledger.Add(tx);
            d.Economy.StartingGrantApplied = MiniJson.GetBool(eco, "startingGrant");

            var s = MiniJson.GetObject(p, "settings");
            d.Settings.Music = MiniJson.GetBool(s, "music", true);
            d.Settings.Sound = MiniJson.GetBool(s, "sound", true);
            d.Settings.MusicVolume = Clamp01((float)MiniJson.GetDouble(s, "musicVolume", 0.6));
            d.Settings.SoundVolume = Clamp01((float)MiniJson.GetDouble(s, "soundVolume", 0.9));
            d.Settings.Haptics = MiniJson.GetBool(s, "haptics", true);
            d.Settings.ReducedMotion = MiniJson.GetBool(s, "reducedMotion");
            d.Settings.ColorblindSymbols = MiniJson.GetBool(s, "colorblind");
            float speed = (float)MiniJson.GetDouble(s, "animationSpeed", 1.0);
            d.Settings.AnimationSpeed = speed < 0.5f || speed > 2f ? 1f : speed;
            d.Settings.LightGameplayBackground = MiniJson.GetBool(s, "lightGameplay");
            d.Settings.DebugSimulateAdFailure = MiniJson.GetBool(s, "debugAdFailure");
            d.Settings.Language = MiniJson.GetString(s, "language", "");

            var st = MiniJson.GetObject(p, "stats");
            d.Stats.HardCompleted = Math.Max(0, MiniJson.GetInt(st, "hard"));
            d.Stats.BossCompleted = Math.Max(0, MiniJson.GetInt(st, "boss"));
            d.Stats.BoostersUsed = Math.Max(0, MiniJson.GetInt(st, "boosters"));
            d.Stats.DailyClaims = Math.Max(0, MiniJson.GetInt(st, "daily"));
            d.Stats.ObjectivesCompleted = Math.Max(0, MiniJson.GetInt(st, "objectives"));
            d.Stats.MysteryRevealed = Math.Max(0, MiniJson.GetInt(st, "mystery"));
            d.Stats.CorksOpened = Math.Max(0, MiniJson.GetInt(st, "corks"));
            var dr = MiniJson.GetObject(p, "dailyReward");
            d.Daily.LastClaimDay = MiniJson.GetInt(dr, "lastDay", -1);
            d.Daily.Streak = Math.Max(0, MiniJson.GetInt(dr, "streak"));
            foreach (var a in MiniJson.GetList(p, "achievements")) if (a is string id) d.ClaimedAchievements.Add(id);

            var cos = MiniJson.GetObject(p, "cosmetics");
            d.Cosmetics.Theme = MiniJson.GetString(cos, "theme", d.Cosmetics.Theme);
            d.Cosmetics.Bottle = MiniJson.GetString(cos, "bottle", d.Cosmetics.Bottle);

            foreach (var t in MiniJson.GetList(p, "tutorials")) if (t is string key) d.TutorialsSeen.Add(key);

            if (MiniJson.TryGet(p, "session", out var sess))
            {
                try { d.Session = SessionSnapshot.FromJson(MiniJson.AsObject(sess)); }
                catch (FormatException) { d.Session = null; } // drop a damaged in-progress puzzle, keep the rest
            }
            return d;
        }

        private static float Clamp01(float v) => v < 0 ? 0 : (v > 1 ? 1 : v);
    }

    /// <summary>
    /// Forward-only payload migrations. v1 was the prototype layout (flat "coins"/"boosters"/"stars" at the root);
    /// v2 nests progress/economy/settings and adds cosmetics + ledger.
    /// </summary>
    public static class SaveMigrator
    {
        public static Dictionary<string, object> Migrate(Dictionary<string, object> payload, int fromVersion)
        {
            var p = payload;
            if (fromVersion < 2) p = V1ToV2(p);
            if (fromVersion < 3) p = V2ToV3(p);
            return p;
        }

        /// <summary>v3 adds stats, daily reward state, claimed achievements and language; older sessions use the old board format.</summary>
        private static Dictionary<string, object> V2ToV3(Dictionary<string, object> v2)
        {
            var p = new Dictionary<string, object>(v2);
            if (!p.ContainsKey("stats")) p["stats"] = new Dictionary<string, object>();
            if (!p.ContainsKey("dailyReward")) p["dailyReward"] = new Dictionary<string, object> { ["lastDay"] = -1L, ["streak"] = 0L };
            if (!p.ContainsKey("achievements")) p["achievements"] = new List<object>();
            return p;
        }

        private static Dictionary<string, object> V1ToV2(Dictionary<string, object> v1)
        {
            var stars = MiniJson.GetObject(v1, "stars");
            var boosters = MiniJson.GetObject(v1, "boosters");
            return new Dictionary<string, object>
            {
                ["contentVersion"] = 1L,
                ["createdUtc"] = MiniJson.GetString(v1, "created", ""),
                ["updatedUtc"] = "",
                ["progress"] = new Dictionary<string, object>
                {
                    ["highestUnlocked"] = (long)MiniJson.GetInt(v1, "unlocked", 1),
                    ["currentLevel"] = (long)MiniJson.GetInt(v1, "level", 1),
                    ["bestStars"] = stars,
                    ["claimedChests"] = new List<object>(),
                },
                ["economy"] = new Dictionary<string, object>
                {
                    ["coins"] = MiniJson.GetLong(v1, "coins"),
                    ["inventory"] = boosters,
                    ["owned"] = new List<object>(),
                    ["ledger"] = new List<object>(),
                    // v1 saves already received their starting coins.
                    ["startingGrant"] = true,
                },
                ["settings"] = new Dictionary<string, object>
                {
                    ["music"] = MiniJson.GetBool(v1, "music", true),
                    ["sound"] = MiniJson.GetBool(v1, "sound", true),
                },
                ["cosmetics"] = new Dictionary<string, object>(),
                ["tutorials"] = new List<object>(),
                ["session"] = null,
            };
        }
    }
}
