using System;
using System.Collections.Generic;

namespace SugarDrop
{
    public enum MissionKind { WinLevels, ClearCandies, UseSpecials, BreakObstacles, EarnStars, MakeCombos }

    /// <summary>O misiune a zilei: ce trebuie făcut, de câte ori și câte monede dă.</summary>
    public readonly struct Mission
    {
        public readonly MissionKind Kind;
        public readonly int Target;
        public readonly int Reward;

        public Mission(MissionKind kind, int target, int reward)
        {
            Kind = kind;
            Target = target;
            Reward = reward;
        }

        /// <summary>Textul misiunii (cheie SugarLoc cu {0} = ținta).</summary>
        public string TextKey => Kind switch
        {
            MissionKind.WinLevels => "Câștigă {0} niveluri",
            MissionKind.ClearCandies => "Elimină {0} bomboane",
            MissionKind.UseSpecials => "Activează {0} piese speciale",
            MissionKind.BreakObstacles => "Sparge {0} obstacole",
            MissionKind.EarnStars => "Strânge {0} stele",
            _ => "Fă {0} combinații",
        };
    }

    /// <summary>Cadoul zilei (seria de 7 zile).</summary>
    public readonly struct DailyGift
    {
        public readonly int Coins;
        public readonly int Bombs;
        public readonly int ExtraMoves;
        public readonly int Shuffles;

        public DailyGift(int coins, int bombs, int extraMoves, int shuffles)
        {
            Coins = coins;
            Bombs = bombs;
            ExtraMoves = extraMoves;
            Shuffles = shuffles;
        }
    }

    /// <summary>
    /// Regulile zilnice (pure, testabile): cadoul pe seria de 7 zile și cele 3 misiuni ale zilei, alese la fel
    /// pentru o zi dată. Ziua = numărul de zile din calendarul local de la 1 ianuarie 2020.
    /// </summary>
    public static class DailyRules
    {
        public const int StreakLength = 7;
        public const int MissionsPerDay = 3;
        /// <summary>Monedele în plus când toate misiunile zilei sunt revendicate.</summary>
        public const int AllMissionsBonus = 100;

        private static readonly DateTime Epoch = new(2020, 1, 1);

        public static int DayOf(DateTime localTime) => (int)(localTime.Date - Epoch).TotalDays;
        public static int Today => DayOf(DateTime.Now);

        /// <summary>Cât timp până la miezul nopții (când apar cadoul și misiunile noi).</summary>
        public static TimeSpan UntilTomorrow => DateTime.Now.Date.AddDays(1) - DateTime.Now;

        private static readonly DailyGift[] Gifts =
        {
            new(50, 0, 0, 0), new(0, 1, 0, 0), new(100, 0, 0, 0), new(0, 0, 1, 0), new(150, 0, 0, 0), new(0, 0, 0, 1), new(300, 1, 1, 1),
        };

        /// <param name="day">Ziua din serie, 1–7.</param>
        public static DailyGift GiftFor(int day) => Gifts[Math.Clamp(day, 1, StreakLength) - 1];

        public static bool CanClaimGift(int lastClaimDay, int today) => today > lastClaimDay;

        /// <summary>Ziua din serie pentru cadoul de azi: continuă dacă ieri s-a revendicat, altfel o ia de la 1.</summary>
        public static int NextStreak(int lastClaimDay, int streak, int today) =>
            today == lastClaimDay + 1 ? streak % StreakLength + 1 : 1;

        /// <summary>Cele 3 misiuni ale zilei (tipuri diferite; țintele cresc puțin odată cu progresul jucătorului).</summary>
        public static Mission[] MissionsFor(int day, int playerLevel)
        {
            var random = new Random(day * 7349 + 11);
            var kinds = new List<MissionKind>((MissionKind[])Enum.GetValues(typeof(MissionKind)));
            // Obstacolele apar abia de la nivelul 4.
            if (playerLevel < 6) kinds.Remove(MissionKind.BreakObstacles);
            var missions = new Mission[MissionsPerDay];
            float scale = 1f + Math.Min(1f, Math.Max(0, playerLevel - 1) / 60f);
            for (int i = 0; i < MissionsPerDay; i++)
            {
                MissionKind kind = kinds[random.Next(kinds.Count)];
                kinds.Remove(kind);
                missions[i] = kind switch
                {
                    MissionKind.WinLevels => new Mission(kind, 2 + random.Next(2), 60),
                    MissionKind.ClearCandies => new Mission(kind, Round((120 + random.Next(4) * 40) * scale, 10), 50),
                    MissionKind.UseSpecials => new Mission(kind, Round((8 + random.Next(5)) * scale, 1), 60),
                    MissionKind.BreakObstacles => new Mission(kind, Round((15 + random.Next(3) * 5) * scale, 5), 60),
                    MissionKind.EarnStars => new Mission(kind, 4 + random.Next(3), 70),
                    _ => new Mission(kind, 1 + random.Next(2), 80),
                };
            }
            return missions;
        }

        private static int Round(float value, int step) => Math.Max(step, (int)Math.Round(value / step) * step);
    }

    /// <summary>Starea zilnică a jucătorului (în salvare): cadoul, misiunile de azi și progresul lor.</summary>
    public static class SugarDaily
    {
        private static SugarSaveData Data => SugarSave.Data;

        public static bool GiftAvailable => DailyRules.CanClaimGift(Data.giftDay, DailyRules.Today);

        /// <summary>Ziua din serie (1–7) pentru cadoul care se poate revendica acum.</summary>
        public static int GiftStreakDay => DailyRules.NextStreak(Data.giftDay, Data.giftStreak, DailyRules.Today);

        /// <summary>Revendică cadoul de azi; multiplier = 2 pentru „dublu cu reclamă” (doar monedele).</summary>
        public static DailyGift ClaimGift(int multiplier = 1)
        {
            int today = DailyRules.Today;
            int streak = DailyRules.NextStreak(Data.giftDay, Data.giftStreak, today);
            DailyGift gift = DailyRules.GiftFor(streak);
            Data.coins += gift.Coins * multiplier;
            Data.bombs += gift.Bombs;
            Data.extraMoves += gift.ExtraMoves;
            Data.shuffles += gift.Shuffles;
            Data.giftDay = today;
            Data.giftStreak = streak;
            SugarSave.Save();
            return gift;
        }

        /// <summary>Misiunile de azi (le generează și resetează progresul la o zi nouă).</summary>
        public static Mission[] Missions
        {
            get
            {
                EnsureToday();
                return DailyRules.MissionsFor(Data.missionDay, Data.missionLevel);
            }
        }

        public static int Progress(int index)
        {
            EnsureToday();
            return Data.missionProgress[index];
        }

        public static bool Claimed(int index)
        {
            EnsureToday();
            return Data.missionClaimed[index] != 0;
        }

        public static bool CanClaim(int index) => !Claimed(index) && Progress(index) >= Missions[index].Target;

        public static bool AllClaimed
        {
            get
            {
                for (int i = 0; i < DailyRules.MissionsPerDay; i++)
                    if (!Claimed(i)) return false;
                return true;
            }
        }

        public static bool AnyClaimable
        {
            get
            {
                for (int i = 0; i < DailyRules.MissionsPerDay; i++)
                    if (CanClaim(i)) return true;
                return false;
            }
        }

        /// <summary>Adaugă progres; întoarce indexul unei misiuni tocmai îndeplinite (sau -1).</summary>
        public static int AddProgress(MissionKind kind, int amount)
        {
            if (amount <= 0) return -1;
            Mission[] missions = Missions;
            int completed = -1;
            for (int i = 0; i < missions.Length; i++)
            {
                if (missions[i].Kind != kind || Data.missionClaimed[i] != 0) continue;
                int before = Data.missionProgress[i];
                Data.missionProgress[i] = Math.Min(missions[i].Target, before + amount);
                if (before < missions[i].Target && Data.missionProgress[i] >= missions[i].Target) completed = i;
            }
            return completed;
        }

        /// <summary>Revendică o misiune îndeplinită; întoarce monedele primite (inclusiv bonusul pentru toate trei).</summary>
        public static int Claim(int index)
        {
            if (!CanClaim(index)) return 0;
            int coins = Missions[index].Reward;
            Data.missionClaimed[index] = 1;
            if (AllClaimed) coins += DailyRules.AllMissionsBonus;
            Data.coins += coins;
            SugarSave.Save();
            return coins;
        }

        private static void EnsureToday()
        {
            int today = DailyRules.Today;
            SugarSaveData data = Data;
            if (data.missionDay == today && data.missionProgress.Count == DailyRules.MissionsPerDay && data.missionClaimed.Count == DailyRules.MissionsPerDay)
                return;
            data.missionDay = today;
            data.missionLevel = Math.Max(1, data.level);
            data.missionProgress = new List<int>(new int[DailyRules.MissionsPerDay]);
            data.missionClaimed = new List<int>(new int[DailyRules.MissionsPerDay]);
            SugarSave.Save();
        }
    }
}
