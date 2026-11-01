using System;

namespace VoxelBloom.Core
{
    /// <summary>Reward of an event claim (already applied to the save / wallet / boosters).</summary>
    public struct EventReward { public int Coins, Gems, Boosters; }

    /// <summary>
    /// "Evenimente": daily challenge (finish 5 levels today), special mission (deliver 200 cubes) and the 7-day
    /// "Sezonul Curcubeu" (earn 15 stars before the season ends). All progress lives in SaveData; time is unix seconds.
    /// </summary>
    public sealed class ConveyorEvents
    {
        public const int DailyGoal = 5, MissionGoal = 200, SeasonGoal = 15;
        public const long SeasonLength = 7 * 86400L;
        static readonly long SeasonEpoch = new DateTimeOffset(2026, 1, 5, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds(); // a Monday
        readonly SaveData save;
        readonly Wallet wallet;
        readonly BoosterInventory boosters;

        public ConveyorEvents(SaveData save, Wallet wallet, BoosterInventory boosters) { this.save = save; this.wallet = wallet; this.boosters = boosters; }

        public static long Day(long now) => now / 86400L;
        public static int SeasonOf(long now) => (int)Math.Max(0, (now - SeasonEpoch) / SeasonLength);
        public static long SeasonEndsAt(long now) => SeasonEpoch + (SeasonOf(now) + 1) * SeasonLength;

        /// <summary>Rolls the daily challenge and the season over when their period changed.</summary>
        public void Refresh(long now)
        {
            long day = Day(now);
            if (save.DailyChallengeDay != day) { save.DailyChallengeDay = day; save.DailyChallengeCount = 0; save.DailyChallengeClaimed = false; }
            int season = SeasonOf(now);
            if (save.SeasonId != season) { save.SeasonId = season; save.SeasonStars = 0; save.SeasonClaimed = false; }
        }

        public void OnLevelCompleted(int stars, long now)
        {
            Refresh(now);
            if (save.DailyChallengeCount < DailyGoal) save.DailyChallengeCount++;
            save.SeasonStars += Math.Max(0, stars);
        }

        public void OnCubesDelivered(int n) { if (n > 0) save.MissionCubes = Math.Min(MissionGoal, save.MissionCubes + n); }

        public bool CanClaimDaily(long now) { Refresh(now); return !save.DailyChallengeClaimed && save.DailyChallengeCount >= DailyGoal; }
        public bool CanClaimMission => save.MissionCubes >= MissionGoal;
        public bool CanClaimSeason(long now) { Refresh(now); return !save.SeasonClaimed && save.SeasonStars >= SeasonGoal; }

        public EventReward ClaimDaily(long now)
        {
            if (!CanClaimDaily(now)) return default;
            save.DailyChallengeClaimed = true;
            return Grant(100, 5, 0);
        }

        /// <summary>The mission repeats: claiming resets the cube counter.</summary>
        public EventReward ClaimMission()
        {
            if (!CanClaimMission) return default;
            save.MissionCubes = 0;
            save.MissionsDone++;
            return Grant(150, 0, 0);
        }

        public EventReward ClaimSeason(long now)
        {
            if (!CanClaimSeason(now)) return default;
            save.SeasonClaimed = true;
            return Grant(0, 20, 1);
        }

        EventReward Grant(int coins, int gems, int eachBooster)
        {
            if (coins > 0) { wallet.Add(coins); save.Coins = wallet.Coins; }
            save.Gems += gems;
            if (eachBooster > 0)
                foreach (var t in new[] { BoosterType.Shuffle, BoosterType.ColorShuffle, BoosterType.ExtraStation }) boosters.Add(t, eachBooster);
            return new EventReward { Coins = coins, Gems = gems, Boosters = eachBooster };
        }

        public bool AnyClaimable(long now) => CanClaimDaily(now) || CanClaimMission || CanClaimSeason(now);
    }
}
