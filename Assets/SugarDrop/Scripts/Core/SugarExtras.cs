using System;

namespace SugarDrop
{
    // ------------------------------------------------------------ Roata norocului

    public enum WheelPrize { Life, FullLives, ExtraMoves, Coins50, Coins150, Bomb, StartRocket, Shuffle }

    /// <summary>
    /// Roata norocului: 8 felii (mai ales vieți și mutări), o rotire gratuită pe zi (două în weekend) și până la
    /// 3 rotiri în plus cu reclamă. Felia câștigătoare se alege după ponderi, apoi roata se oprește pe ea.
    /// </summary>
    public static class WheelRules
    {
        public const int AdSpinsPerDay = 3;

        /// <summary>Feliile, în ordinea de pe roată (în sensul acelor de ceasornic, de sus).</summary>
        public static readonly WheelPrize[] Slices =
        {
            WheelPrize.Life, WheelPrize.Coins50, WheelPrize.ExtraMoves, WheelPrize.Bomb,
            WheelPrize.FullLives, WheelPrize.Coins150, WheelPrize.StartRocket, WheelPrize.Shuffle,
        };

        private static int Weight(WheelPrize prize) => prize switch
        {
            WheelPrize.Life => 22,
            WheelPrize.ExtraMoves => 20,
            WheelPrize.Coins50 => 18,
            WheelPrize.Bomb => 10,
            WheelPrize.Shuffle => 10,
            WheelPrize.StartRocket => 9,
            WheelPrize.FullLives => 6,
            _ => 5,
        };

        /// <summary>Indexul feliei câștigătoare pentru un număr aleator 0 ≤ roll &lt; 1.</summary>
        public static int PickSlice(double roll)
        {
            int total = 0;
            foreach (WheelPrize prize in Slices) total += Weight(prize);
            double target = Math.Clamp(roll, 0.0, 0.999999) * total;
            for (int i = 0; i < Slices.Length; i++)
            {
                target -= Weight(Slices[i]);
                if (target < 0) return i;
            }
            return Slices.Length - 1;
        }

        public static int FreeSpinsPerDay(DateTime localTime) => SugarEvents.IsWeekend(localTime) ? 2 : 1;
    }

    /// <summary>Starea roții (în salvare) și acordarea premiilor.</summary>
    public static class SugarWheel
    {
        private static SugarSaveData Data => SugarSave.Data;

        private static void EnsureToday()
        {
            int today = DailyRules.Today;
            if (Data.wheelDay == today) return;
            Data.wheelDay = today;
            Data.wheelFreeUsed = 0;
            Data.wheelAdUsed = 0;
        }

        public static int FreeSpinsLeft
        {
            get
            {
                EnsureToday();
                return Math.Max(0, WheelRules.FreeSpinsPerDay(DateTime.Now) - Data.wheelFreeUsed);
            }
        }

        public static int AdSpinsLeft
        {
            get
            {
                EnsureToday();
                return Math.Max(0, WheelRules.AdSpinsPerDay - Data.wheelAdUsed);
            }
        }

        /// <summary>Consumă o rotire (gratuită dacă mai există, altfel cu reclamă).</summary>
        public static void UseSpin(bool free)
        {
            EnsureToday();
            if (free) Data.wheelFreeUsed++;
            else Data.wheelAdUsed++;
            SugarSave.Save();
        }

        public static void Grant(WheelPrize prize)
        {
            SugarSaveData data = Data;
            switch (prize)
            {
                case WheelPrize.Life: SugarSave.AddLife(); return;
                case WheelPrize.FullLives: data.lives = SugarSave.MaxLives; break;
                case WheelPrize.ExtraMoves: data.extraMoves++; break;
                case WheelPrize.Coins50: data.coins += 50; break;
                case WheelPrize.Coins150: data.coins += 150; break;
                case WheelPrize.Bomb: data.bombs++; break;
                case WheelPrize.StartRocket: data.startRockets++; break;
                case WheelPrize.Shuffle: data.shuffles++; break;
            }
            SugarSave.Save();
        }

        /// <summary>Textul premiului (cheie SugarLoc).</summary>
        public static string PrizeKey(WheelPrize prize) => prize switch
        {
            WheelPrize.Life => "+1 viață",
            WheelPrize.FullLives => "Vieți pline!",
            WheelPrize.ExtraMoves => "+1 ajutor „+5 mutări”",
            WheelPrize.Coins50 => "+50 monede",
            WheelPrize.Coins150 => "+150 monede",
            WheelPrize.Bomb => "+1 bombă",
            WheelPrize.StartRocket => "+1 rachetă la start",
            _ => "+1 amestecare",
        };
    }

    // ------------------------------------------------------------ Evenimente

    /// <summary>„Weekend dulce”: sâmbăta și duminica monedele din niveluri sunt duble și roata are o rotire gratuită în plus.</summary>
    public static class SugarEvents
    {
        public static bool IsWeekend(DateTime localTime) =>
            localTime.DayOfWeek == DayOfWeek.Saturday || localTime.DayOfWeek == DayOfWeek.Sunday;

        public static bool WeekendNow => IsWeekend(DateTime.Now);

        /// <summary>Cât mai ține evenimentul (până luni la 00:00) sau cât până începe (sâmbătă la 00:00).</summary>
        public static TimeSpan Countdown(DateTime localTime)
        {
            DateTime day = localTime.Date;
            if (IsWeekend(localTime))
            {
                int toMonday = ((int)DayOfWeek.Monday - (int)day.DayOfWeek + 7) % 7;
                return day.AddDays(toMonday == 0 ? 7 : toMonday) - localTime;
            }
            int toSaturday = ((int)DayOfWeek.Saturday - (int)day.DayOfWeek + 7) % 7;
            return day.AddDays(toSaturday) - localTime;
        }

        public static int CoinMultiplier(DateTime localTime) => IsWeekend(localTime) ? 2 : 1;
    }

    // ------------------------------------------------------------ Garderoba lui Bobo

    public enum Costume { None, PartyHat, Bow, Crown, ChefHat, Sunglasses, WizardHat }

    /// <summary>Costumele lui Bobo se deblochează cu stelele strânse pe toate nivelurile.</summary>
    public static class CostumeRules
    {
        public static readonly Costume[] All =
            { Costume.None, Costume.PartyHat, Costume.Bow, Costume.Crown, Costume.ChefHat, Costume.Sunglasses, Costume.WizardHat };

        public static int StarsNeeded(Costume costume) => costume switch
        {
            Costume.PartyHat => 5,
            Costume.Bow => 20,
            Costume.Crown => 45,
            Costume.ChefHat => 75,
            Costume.Sunglasses => 110,
            Costume.WizardHat => 160,
            _ => 0,
        };

        public static bool Unlocked(Costume costume, int stars) => stars >= StarsNeeded(costume);

        /// <summary>Numele costumului (cheie SugarLoc).</summary>
        public static string NameKey(Costume costume) => costume switch
        {
            Costume.PartyHat => "Coif de petrecere",
            Costume.Bow => "Fundiță",
            Costume.Crown => "Coroană",
            Costume.ChefHat => "Bonetă de bucătar",
            Costume.Sunglasses => "Ochelari de soare",
            Costume.WizardHat => "Pălărie de vrăjitor",
            _ => "Fără costum",
        };

        /// <summary>Costumul purtat acum (doar dacă e deblocat).</summary>
        public static Costume Current
        {
            get
            {
                var costume = (Costume)Math.Clamp(SugarSave.Data.costume, 0, All.Length - 1);
                return Unlocked(costume, SugarSave.TotalStars(SugarSave.Data)) ? costume : Costume.None;
            }
        }

        /// <summary>Ochelarii stau pe ochi; restul costumelor, pe cap (în locul cireșei).</summary>
        public static bool OnEyes(Costume costume) => costume == Costume.Sunglasses;
    }

    // ------------------------------------------------------------ Boostere de start

    public enum StartBooster { Rocket, Bomb }

    public static class StartBoosters
    {
        public const int Price = 150;

        public static int Count(StartBooster booster) =>
            booster == StartBooster.Rocket ? SugarSave.Data.startRockets : SugarSave.Data.startBombs;

        public static void Add(StartBooster booster, int amount)
        {
            if (booster == StartBooster.Rocket) SugarSave.Data.startRockets = Math.Max(0, SugarSave.Data.startRockets + amount);
            else SugarSave.Data.startBombs = Math.Max(0, SugarSave.Data.startBombs + amount);
        }

        /// <summary>Boosterele de start apar de la nivelul 2 (nivelul 1 e antrenamentul).</summary>
        public static bool Offered(int level) => level >= 2;
    }
}
