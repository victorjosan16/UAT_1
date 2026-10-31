using System;
using System.Collections.Generic;
using Veliqora.Core.Economy;
using Veliqora.Core.Json;

namespace Veliqora.Core.Progression
{
    /// <summary>Persistent counters used by achievements.</summary>
    public sealed class PlayerStats
    {
        public int HardCompleted;
        public int BossCompleted;
        public int BoostersUsed;
        public int DailyClaims;
        public int ObjectivesCompleted;
        public int MysteryRevealed;
        public int CorksOpened;
    }

    public sealed class DailyState
    {
        /// <summary>Day number (days since 2000-01-01, device local date) of the last claim; -1 = never.</summary>
        public int LastClaimDay = -1;
        public int Streak;
    }

    public sealed class DailyRewardDef
    {
        public int Coins;
        public BoosterType? Booster;
        public int BoosterQuantity;
    }

    public sealed class AchievementDef
    {
        public string Id;
        public string Title;
        public string Metric;   // levels | stars | threeStars | hard | boss | boosters | daily | objectives | themes | mystery | corks
        public int Target;
        public int RewardCoins;
    }

    /// <summary>Daily reward and achievement definitions (Resources/Veliqora/Config/progression.json).</summary>
    public sealed class ProgressionConfig
    {
        public List<DailyRewardDef> Daily = new List<DailyRewardDef>();
        public List<AchievementDef> Achievements = new List<AchievementDef>();

        public static ProgressionConfig FromJson(string json)
        {
            var root = MiniJson.AsObject(MiniJson.Parse(json));
            var cfg = new ProgressionConfig();
            foreach (var o in MiniJson.GetList(root, "daily"))
            {
                var d = MiniJson.AsObject(o);
                var def = new DailyRewardDef { Coins = Math.Max(0, MiniJson.GetInt(d, "coins")), BoosterQuantity = MiniJson.GetInt(d, "quantity", 1) };
                var b = MiniJson.GetString(d, "booster");
                if (!string.IsNullOrEmpty(b)) def.Booster = EconomyConfig.ParseBooster(b);
                cfg.Daily.Add(def);
            }
            foreach (var o in MiniJson.GetList(root, "achievements"))
            {
                var d = MiniJson.AsObject(o);
                cfg.Achievements.Add(new AchievementDef
                {
                    Id = MiniJson.GetString(d, "id"),
                    Title = MiniJson.GetString(d, "title", ""),
                    Metric = MiniJson.GetString(d, "metric", "levels"),
                    Target = Math.Max(1, MiniJson.GetInt(d, "target", 1)),
                    RewardCoins = Math.Max(0, MiniJson.GetInt(d, "reward")),
                });
            }
            if (cfg.Daily.Count == 0) throw new FormatException("progression.json needs at least one daily reward");
            return cfg;
        }
    }

    public static class DayClock
    {
        private static readonly DateTime Epoch = new DateTime(2000, 1, 1);
        public static int DayNumber(DateTime local) => (int)(local.Date - Epoch).TotalDays;
    }

    /// <summary>
    /// 7-step daily reward cycle. One claim per calendar day (idempotent via the economy ledger key "daily:&lt;day&gt;");
    /// missing a day resets the streak. An optional rewarded ad doubles the coins once per day.
    /// </summary>
    public sealed class DailyRewardService
    {
        private readonly DailyState _state;
        private readonly ProgressionConfig _cfg;
        private readonly EconomyService _economy;

        public DailyRewardService(DailyState state, ProgressionConfig cfg, EconomyService economy)
        {
            _state = state; _cfg = cfg; _economy = economy;
        }

        public DailyState State => _state;
        public bool CanClaim(int today) => today > _state.LastClaimDay;

        /// <summary>Streak position (1-based) the next claim would have on <paramref name="today"/>.</summary>
        public int NextStreak(int today) => _state.LastClaimDay == today - 1 ? _state.Streak + 1 : 1;

        public DailyRewardDef RewardFor(int streak) => _cfg.Daily[(Math.Max(1, streak) - 1) % _cfg.Daily.Count];

        public DailyRewardDef Claim(int today)
        {
            if (!CanClaim(today)) return null;
            int streak = NextStreak(today);
            var reward = RewardFor(streak);
            string tx = "daily:" + today;
            if (reward.Coins > 0) _economy.GrantCoins(reward.Coins, tx);
            if (reward.Booster.HasValue) _economy.GrantBooster(reward.Booster.Value, Math.Max(1, reward.BoosterQuantity), tx + ":b");
            _state.LastClaimDay = today;
            _state.Streak = streak;
            return reward;
        }

        /// <summary>Grants the coin part again after a completed rewarded ad; at most once per day.</summary>
        public bool ClaimAdDouble(int today)
        {
            if (_state.LastClaimDay != today) return false;
            var reward = RewardFor(_state.Streak);
            return reward.Coins > 0 && _economy.GrantCoins(reward.Coins, "dailyad:" + today).Success;
        }
    }

    public struct AchievementStatus
    {
        public AchievementDef Def;
        public int Progress;
        public bool Claimed;
        public bool Complete => Progress >= Def.Target;
    }

    /// <summary>Achievement progress from stats/progress; each reward can be claimed exactly once ("ach:&lt;id&gt;").</summary>
    public sealed class AchievementService
    {
        private readonly ProgressionConfig _cfg;
        private readonly HashSet<string> _claimed;
        private readonly EconomyService _economy;
        private readonly Func<string, int> _metric;

        public AchievementService(ProgressionConfig cfg, HashSet<string> claimed, EconomyService economy, Func<string, int> metric)
        {
            _cfg = cfg; _claimed = claimed; _economy = economy; _metric = metric;
        }

        public List<AchievementStatus> All()
        {
            var list = new List<AchievementStatus>();
            foreach (var a in _cfg.Achievements)
                list.Add(new AchievementStatus { Def = a, Progress = Math.Min(a.Target, _metric(a.Metric)), Claimed = _claimed.Contains(a.Id) });
            return list;
        }

        public int ClaimableCount()
        {
            int n = 0;
            foreach (var s in All()) if (s.Complete && !s.Claimed) n++;
            return n;
        }

        public bool Claim(string id)
        {
            foreach (var s in All())
            {
                if (s.Def.Id != id) continue;
                if (!s.Complete || s.Claimed) return false;
                if (s.Def.RewardCoins > 0 && !_economy.GrantCoins(s.Def.RewardCoins, "ach:" + id).Success) return false;
                _claimed.Add(id);
                return true;
            }
            return false;
        }

        /// <summary>Ids that are complete but were not complete in <paramref name="before"/> (for unlock notifications).</summary>
        public static List<string> NewlyCompleted(List<AchievementStatus> before, List<AchievementStatus> after)
        {
            var was = new HashSet<string>();
            foreach (var b in before) if (b.Complete) was.Add(b.Def.Id);
            var list = new List<string>();
            foreach (var a in after) if (a.Complete && !was.Contains(a.Def.Id)) list.Add(a.Def.Id);
            return list;
        }
    }
}
