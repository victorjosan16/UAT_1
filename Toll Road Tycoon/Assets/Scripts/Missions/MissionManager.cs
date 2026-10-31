using System;
using System.Collections.Generic;
using TollRoadTycoon.Core;
using TollRoadTycoon.Economy;

namespace TollRoadTycoon.Missions
{
    public enum MissionType
    {
        ProcessCars,
        EarnMoney,
        ServeTrucks,
        UpgradeWorker,
        BuildBooth,
        BuildService,
        ReachIncome,
        UnlockRoad,
        SpeedUpgrade,
        CapacityUpgrade,
        UnlockVehicle,
    }

    /// <summary>Current value of a progress counter (cumulative counters, or a live value for ReachIncome).</summary>
    public interface IProgressSource
    {
        long GetCounter(MissionType type);
        bool IsVehicleUnlocked(string id);
    }

    public sealed class Mission
    {
        public string Id;
        public MissionType Type;
        public long Target;
        /// <summary>Counter value when the mission started (0 for milestones).</summary>
        public long Baseline;
        public long RewardCents;
        public string Param;
        public bool Claimed;
    }

    /// <summary>
    /// Daily missions (refreshed on each new UTC day) and the first-play milestone chain that also
    /// serves as the tutorial. Rewards are granted exactly once.
    /// </summary>
    public sealed class MissionManager
    {
        private readonly IProgressSource _source;
        private readonly EconomyManager _economy;
        private readonly int _dailyCount;
        private readonly List<Mission> _daily = new List<Mission>();
        private readonly List<Mission> _milestones = new List<Mission>();

        public IReadOnlyList<Mission> Daily => _daily;
        public IReadOnlyList<Mission> Milestones => _milestones;
        public string DailyDate { get; private set; } = "";
        public int MilestoneIndex { get; private set; }

        public event Action Changed;

        public MissionManager(IProgressSource source, EconomyManager economy, int dailyCount)
        {
            _source = source;
            _economy = economy;
            _dailyCount = dailyCount;
            BuildMilestones();
        }

        /// <summary>Tutorial / milestone chain from the design document.</summary>
        private void BuildMilestones()
        {
            void M(string id, MissionType type, long target, long rewardDollars, string param = null)
            {
                _milestones.Add(new Mission { Id = id, Type = type, Target = target, RewardCents = Money.FromDollars(rewardDollars), Param = param });
            }
            M("m_cars10", MissionType.ProcessCars, 10, 50);
            M("m_earn100", MissionType.EarnMoney, Money.FromDollars(100), 50);
            M("m_speed", MissionType.SpeedUpgrade, 1, 75);
            M("m_booth2", MissionType.BuildBooth, 1, 150);
            M("m_suv", MissionType.UnlockVehicle, 1, 200, "suv");
            M("m_worker", MissionType.UpgradeWorker, 1, 250);
            M("m_booth3", MissionType.BuildBooth, 2, 400);
            M("m_capacity", MissionType.CapacityUpgrade, 1, 400);
            M("m_service", MissionType.BuildService, 1, 500);
            M("m_trucks", MissionType.UnlockVehicle, 1, 1500, "truck");
            M("m_road2", MissionType.UnlockRoad, 1, 5000);
        }

        public Mission CurrentMilestone => MilestoneIndex < _milestones.Count ? _milestones[MilestoneIndex] : null;

        public long Progress(Mission m)
        {
            if (m.Type == MissionType.UnlockVehicle) return _source.IsVehicleUnlocked(m.Param) ? 1 : 0;
            long value = _source.GetCounter(m.Type) - m.Baseline;
            return Math.Max(0, Math.Min(value, m.Target));
        }

        public bool IsComplete(Mission m) => Progress(m) >= m.Target;

        public bool CanClaim(Mission m)
        {
            if (m == null || m.Claimed || !IsComplete(m)) return false;
            // Milestones are claimed in order.
            return !_milestones.Contains(m) || m == CurrentMilestone;
        }

        public bool Claim(Mission m)
        {
            if (!CanClaim(m)) return false;
            m.Claimed = true;
            _economy.AddMoney(m.RewardCents, MoneySource.Reward);
            if (m == CurrentMilestone) MilestoneIndex++;
            Changed?.Invoke();
            return true;
        }

        /// <summary>Creates a new set of daily missions when the UTC date changes.</summary>
        public void RefreshDaily(DateTime utcNow, long incomePerMinuteCents)
        {
            string date = utcNow.ToUniversalTime().ToString("yyyy-MM-dd");
            if (date == DailyDate && _daily.Count > 0) return;
            DailyDate = date;
            _daily.Clear();

            // Targets scale with how far the player has come, so missions stay reachable in a day.
            long income = Math.Max(Money.FromDollars(60), incomePerMinuteCents);
            long cars = _source.GetCounter(MissionType.ProcessCars);
            var day = utcNow.ToUniversalTime();
            int seed = day.Year * 10000 + day.Month * 100 + day.Day;
            var random = new SimRandom(seed);
            var pool = new List<Func<Mission>>
            {
                () => NewDaily("d_cars", MissionType.ProcessCars, RoundTarget(Math.Max(50, cars / 10 + 50)), income * 4),
                () => NewDaily("d_earn", MissionType.EarnMoney, RoundMoney(income * 30), income * 5),
                () => NewDaily("d_trucks", MissionType.ServeTrucks, 25, income * 6),
                () => NewDaily("d_worker", MissionType.UpgradeWorker, 1, income * 3),
                () => NewDaily("d_booth", MissionType.BuildBooth, 1, income * 4),
                () => NewDaily("d_service", MissionType.BuildService, 1, income * 4),
                () => NewDaily("d_income", MissionType.ReachIncome, RoundMoney(income * 3 / 2), income * 5),
                () => NewDaily("d_speed", MissionType.SpeedUpgrade, 2, income * 3),
            };
            for (int i = 0; i < _dailyCount && pool.Count > 0; i++)
            {
                int k = random.Range(0, pool.Count);
                _daily.Add(pool[k]());
                pool.RemoveAt(k);
            }
            Changed?.Invoke();
        }

        private Mission NewDaily(string id, MissionType type, long target, long reward)
        {
            long baseline = type == MissionType.ReachIncome ? 0 : _source.GetCounter(type);
            return new Mission { Id = id, Type = type, Target = target, Baseline = baseline, RewardCents = RoundMoney(Math.Max(Money.FromDollars(50), reward)) };
        }

        private static long RoundTarget(long v) => Math.Max(10, v / 10 * 10);

        private static long RoundMoney(long cents) => Math.Max(Money.CentsPerDollar, cents / Money.CentsPerDollar * Money.CentsPerDollar);

        // ---- save support ----------------------------------------------------------------

        internal void RestoreMilestones(int index)
        {
            MilestoneIndex = Math.Max(0, Math.Min(index, _milestones.Count));
            for (int i = 0; i < _milestones.Count; i++) _milestones[i].Claimed = i < MilestoneIndex;
        }

        internal void RestoreDaily(string date, IEnumerable<Mission> missions)
        {
            _daily.Clear();
            DailyDate = date ?? "";
            foreach (var m in missions) _daily.Add(m);
        }
    }
}
