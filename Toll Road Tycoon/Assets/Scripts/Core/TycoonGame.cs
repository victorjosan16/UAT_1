using System;
using System.Collections.Generic;
using System.Globalization;
using TollRoadTycoon.Economy;
using TollRoadTycoon.Missions;
using TollRoadTycoon.Monetization;
using TollRoadTycoon.Save;
using TollRoadTycoon.Services;
using TollRoadTycoon.Shop;
using TollRoadTycoon.TollSystem;
using TollRoadTycoon.Traffic;

namespace TollRoadTycoon.Core
{
    public enum SectionBlock
    {
        None,
        AlreadyUnlocked,
        PreviousLocked,
        NeedsBooths,
        NeedsIncome,
        NotEnoughMoney,
    }

    /// <summary>One of the five roads: its config, saved road state and roadside buildings.</summary>
    public sealed class SectionState
    {
        public int Index;
        public RoadSectionConfig Config;
        /// <summary>Game config adjusted for this road (prices, tolls, limits).</summary>
        public GameConfig RoadConfig;
        public bool Unlocked;
        public SectionSave Road;
        public ServiceManager Services;
        public DecorationManager Decorations;
        internal SectionEstimate CachedEstimate;
        internal bool EstimateDirty = true;
        internal double PassiveRemainder;
    }

    /// <summary>
    /// Root of the full game: five road sections sharing one economy. Only the active section is
    /// simulated vehicle by vehicle; unlocked inactive sections and offline time earn from an
    /// analytical estimate, so nothing is ever counted twice.
    /// </summary>
    public sealed class TycoonGame : IProgressSource
    {
        private readonly HashSet<string> _unlocked = new HashSet<string>();
        private readonly List<SectionState> _sections = new List<SectionState>();
        private readonly int _seed;

        public GameConfig Config { get; }
        public ProgressionConfig Progression => Config.Progression;
        public EconomyManager Economy { get; }
        public GameStats Stats { get; } = new GameStats();
        public SimRandom Random { get; }
        public MissionManager Missions { get; }
        public BoostManager Boost { get; } = new BoostManager();
        public AdRewards Ads { get; }

        public IReadOnlyList<SectionState> Sections => _sections;
        public int ActiveIndex { get; private set; }
        public SectionState ActiveSection => _sections[ActiveIndex];
        public GameSimulation Active { get; private set; }

        /// <summary>Offline income waiting for the player to collect (estimate).</summary>
        public long PendingOfflineCents { get; private set; }
        public double PendingOfflineSeconds { get; private set; }

        public event Action ActiveSectionChanged;
        public event Action StateChanged;
        public event Action<PlacedService, long> ServiceIncome;

        public TycoonGame(GameConfig config, IAdService ads = null, int seed = 12345)
        {
            Config = config;
            _seed = seed;
            Random = new SimRandom(seed);
            Economy = new EconomyManager(config.StartingBalanceCents, config.IncomeWindowSeconds);
            Ads = new AdRewards(ads);
            Missions = new MissionManager(this, Economy, Progression.DailyMissionCount);
            Missions.Changed += RaiseChanged;
            foreach (var type in config.VehicleTypes)
            {
                if (type.UnlockedByDefault) _unlocked.Add(type.Id);
            }
            for (int i = 0; i < Progression.Sections.Count; i++)
            {
                var sc = Progression.Sections[i];
                var state = new SectionState
                {
                    Index = i,
                    Config = sc,
                    RoadConfig = config.ForSection(sc),
                    Unlocked = i == 0,
                };
                state.Services = new ServiceManager(Progression, sc, Economy, Stats, Random);
                state.Decorations = new DecorationManager(Progression, Economy);
                state.Services.Changed += () => OnSectionChanged(state);
                state.Decorations.Changed += RaiseChanged;
                state.Services.CustomerPaid += (s, c) => ServiceIncome?.Invoke(s, c);
                _sections.Add(state);
            }
            _sections[0].Road = NewRoad(_sections[0]);
            Activate(0);
        }

        // ---- sections -------------------------------------------------------------------------

        private SectionSave NewRoad(SectionState s)
        {
            var sim = new GameSimulation(s.RoadConfig, _seed, new EconomyManager(0), new GameStats(), new HashSet<string>(_unlocked));
            return sim.CaptureSection(s.Config.Id);
        }

        private void Activate(int index)
        {
            var section = _sections[index];
            if (Active != null)
            {
                Active.Payments.TollCollected -= OnTollCollected;
                Active.StateChanged -= OnActiveChanged;
            }
            ActiveIndex = index;
            Active = new GameSimulation(section.RoadConfig, _seed + index * 7919, Economy, Stats, _unlocked);
            Active.ApplySection(section.Road);
            Active.Payments.TollCollected += OnTollCollected;
            Active.StateChanged += OnActiveChanged;
            Active.Upgrades.VehicleUnlocked += _ => InvalidateEstimates();
            Economy.ResetIncomeWindow(Active.Time);
            ActiveSectionChanged?.Invoke();
            RaiseChanged();
        }

        /// <summary>Saves the active road and starts simulating another unlocked one.</summary>
        public bool SwitchSection(int index)
        {
            if (index < 0 || index >= _sections.Count || !_sections[index].Unlocked) return false;
            if (index == ActiveIndex) return true;
            StoreActive();
            // Customers inside buildings of the road we leave are settled now (no frozen timers).
            ActiveSection.Services.Tick(double.MaxValue);
            Activate(index);
            return true;
        }

        private void StoreActive()
        {
            ActiveSection.Road = Active.CaptureSection(ActiveSection.Config.Id);
            ActiveSection.EstimateDirty = true;
        }

        public SectionBlock CanUnlock(int index)
        {
            if (index <= 0 || index >= _sections.Count) return SectionBlock.AlreadyUnlocked;
            var s = _sections[index];
            if (s.Unlocked) return SectionBlock.AlreadyUnlocked;
            var previous = _sections[index - 1];
            if (!previous.Unlocked) return SectionBlock.PreviousLocked;
            if (BoothCount(previous) < s.Config.RequiredPreviousBooths) return SectionBlock.NeedsBooths;
            if (Economy.GetIncomePerMinute() < s.Config.RequiredIncomePerMinuteCents) return SectionBlock.NeedsIncome;
            if (!Economy.CanAfford(s.Config.UnlockPriceCents)) return SectionBlock.NotEnoughMoney;
            return SectionBlock.None;
        }

        public bool Unlock(int index)
        {
            if (CanUnlock(index) != SectionBlock.None) return false;
            var s = _sections[index];
            if (!Economy.TrySpendMoney(s.Config.UnlockPriceCents, "section." + s.Config.Id)) return false;
            s.Unlocked = true;
            s.Road = NewRoad(s);
            s.EstimateDirty = true;
            Stats.SectionsUnlocked++;
            RaiseChanged();
            return true;
        }

        public int BoothCount(SectionState s) => s == ActiveSection ? Active.Booths.Count : s.Road?.Booths.Count ?? 0;

        public int LaneCount(SectionState s) => s == ActiveSection ? Active.Lanes.ActiveLaneCount : s.Road?.LaneCount ?? 0;

        // ---- income estimates ---------------------------------------------------------------

        /// <summary>Estimated toll + service income per second of a section (before the safety factor).</summary>
        public double EstimateIncomePerSecond(SectionState s)
        {
            if (!s.Unlocked) return 0;
            if (s == ActiveSection)
            {
                var live = SectionEstimator.Estimate(Active);
                return live.TollCentsPerSecond + s.Services.EstimateIncomePerSecond(live.VehiclesPerSecond, live.HeavyFraction);
            }
            if (s.EstimateDirty)
            {
                s.CachedEstimate = SectionEstimator.Estimate(s.RoadConfig, s.Road, _unlocked);
                s.EstimateDirty = false;
            }
            var e = s.CachedEstimate;
            return e.TollCentsPerSecond + s.Services.EstimateIncomePerSecond(e.VehiclesPerSecond, e.HeavyFraction);
        }

        /// <summary>Income per second used for offline time: every unlocked section, estimated.</summary>
        public double EstimateOfflineIncomePerSecond()
        {
            double total = 0;
            foreach (var s in _sections) total += EstimateIncomePerSecond(s);
            return total * Progression.EstimateSafetyFactor;
        }

        private void InvalidateEstimates()
        {
            foreach (var s in _sections) s.EstimateDirty = true;
        }

        // ---- tick -----------------------------------------------------------------------------

        public void Tick(double deltaSeconds)
        {
            if (deltaSeconds <= 0 || double.IsNaN(deltaSeconds)) return;
            Boost.Tick(deltaSeconds);
            Active.Payments.ActiveBonusBasisPoints = Boost.CurrentBasisPoints;
            Active.Tick(deltaSeconds);
            ActiveSection.Services.Tick(Active.Time);

            // Inactive unlocked roads earn their estimate; the active road earns only real payments.
            foreach (var s in _sections)
            {
                if (!s.Unlocked || s == ActiveSection) continue;
                s.PassiveRemainder += EstimateIncomePerSecond(s) * Progression.EstimateSafetyFactor * deltaSeconds;
                if (s.PassiveRemainder < 1) continue;
                long cents = (long)Math.Floor(s.PassiveRemainder);
                s.PassiveRemainder -= cents;
                Stats.PassiveIncomeCents += cents;
                Economy.AddMoney(cents, MoneySource.Passive);
            }
        }

        /// <summary>Runs the game in fixed steps (tests and tools).</summary>
        public void RunFor(double seconds)
        {
            double step = Config.FixedTimeStep;
            int steps = (int)Math.Round(seconds / step);
            for (int i = 0; i < steps; i++) Tick(step);
        }

        private void OnTollCollected(TollBooth booth, Vehicle vehicle, long cents)
        {
            ActiveSection.Services.OnVehiclePaid(vehicle.Type, Active.Time);
        }

        private void OnActiveChanged()
        {
            ActiveSection.EstimateDirty = true;
            RaiseChanged();
        }

        private void OnSectionChanged(SectionState s)
        {
            s.EstimateDirty = true;
            RaiseChanged();
        }

        private void RaiseChanged() => StateChanged?.Invoke();

        // ---- boosts, ads, offline ---------------------------------------------------------------

        public long InstantCashReward =>
            Math.Max(Progression.InstantCashMinimumCents,
                (long)(Economy.GetIncomePerMinute() * Progression.InstantCashMinutes) / Money.CentsPerDollar * Money.CentsPerDollar);

        public bool RequestAdBoost(Action<AdResult> done = null)
        {
            return Ads.Request(AdPlacements.IncomeBoost, () =>
            {
                Boost.Activate(Progression.BoostSeconds, Progression.BoostBasisPoints);
                Stats.BoostsUsed++;
                RaiseChanged();
            }, done);
        }

        public bool RequestAdInstantCash(Action<AdResult> done = null)
        {
            long reward = InstantCashReward;
            return Ads.Request(AdPlacements.InstantCash, () =>
            {
                Economy.AddMoney(reward, MoneySource.Ad);
                RaiseChanged();
            }, done);
        }

        public bool BuyBoostWithCash()
        {
            if (!Economy.TrySpendMoney(Progression.BoostCashPriceCents, "boost")) return false;
            Boost.Activate(Progression.BoostCashSeconds, Progression.BoostBasisPoints);
            Stats.BoostsUsed++;
            RaiseChanged();
            return true;
        }

        /// <summary>Collects pending offline income once (×2 only when an ad was completed).</summary>
        public long CollectOffline() => GrantOffline(1);

        public bool RequestOfflineDouble(Action<AdResult> done = null)
        {
            if (PendingOfflineCents <= 0) return false;
            return Ads.Request(AdPlacements.OfflineDouble, () => GrantOffline(2), done);
        }

        private long GrantOffline(int multiplier)
        {
            long cents = PendingOfflineCents;
            if (cents <= 0) return 0;
            PendingOfflineCents = 0;
            PendingOfflineSeconds = 0;
            long total = cents * multiplier;
            Stats.OfflineIncomeCents += total;
            Economy.AddMoney(total, MoneySource.Offline);
            RaiseChanged();
            return total;
        }

        // ---- progress source ---------------------------------------------------------------------

        public long GetCounter(MissionType type)
        {
            switch (type)
            {
                case MissionType.ProcessCars: return Stats.VehiclesProcessed;
                case MissionType.EarnMoney: return Economy.TotalEarnings;
                case MissionType.ServeTrucks: return Stats.HeavyVehiclesProcessed;
                case MissionType.UpgradeWorker: return Stats.WorkerUpgrades;
                case MissionType.BuildBooth: return Stats.BoothsBuilt;
                case MissionType.BuildService: return Stats.ServicesBuilt;
                case MissionType.ReachIncome: return Economy.GetIncomePerMinute();
                case MissionType.UnlockRoad: return Stats.SectionsUnlocked;
                case MissionType.SpeedUpgrade: return Stats.SpeedUpgrades;
                case MissionType.CapacityUpgrade: return Stats.CapacityUpgrades;
                default: return 0;
            }
        }

        public bool IsVehicleUnlocked(string id) => _unlocked.Contains(id);

        public IReadOnlyCollection<string> UnlockedVehicles => _unlocked;

        // ---- save / load ---------------------------------------------------------------------------

        public SaveData CaptureSave(DateTime utcNow, string gameVersion, SettingsSave settings)
        {
            StoreActive();
            var data = new SaveData
            {
                GameVersion = gameVersion ?? "",
                LastSeenUtc = utcNow.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture),
                BalanceCents = Economy.Balance,
                TotalEarningsCents = Economy.TotalEarnings,
                TotalExpensesCents = Economy.TotalExpenses,
                Settings = settings ?? new SettingsSave(),
                Stats = GameSimulation.CaptureStats(Stats),
                ActiveSection = ActiveIndex,
                DailyMissionDate = Missions.DailyDate,
                MilestoneIndex = Missions.MilestoneIndex,
                PendingOfflineCents = PendingOfflineCents,
                BoostSecondsLeft = Boost.SecondsLeft,
                BoostBasisPoints = Boost.BasisPoints,
            };
            // Legacy single-road fields mirror section 1 so older readers still find something sane.
            var first = _sections[0].Road;
            data.LaneCount = first.LaneCount;
            data.Booths.AddRange(first.Booths);
            data.Workers.AddRange(first.Workers);
            foreach (var id in _unlocked) data.UnlockedVehicles.Add(id);
            data.UnlockedVehicles.Sort(StringComparer.Ordinal);

            foreach (var s in _sections)
            {
                var ss = s.Road != null ? Clone(s.Road) : new SectionSave();
                ss.Id = s.Config.Id;
                ss.Unlocked = s.Unlocked;
                foreach (var b in s.Services.Placed) ss.Services.Add(new ServiceSave { Id = b.Config.Id, Slot = b.Slot, Level = b.Level });
                foreach (var d in s.Decorations.Placed) ss.Decorations.Add(new DecorationSave { Id = d.Config.Id, Slot = d.Slot });
                data.Sections.Add(ss);
            }
            foreach (var m in Missions.Daily)
            {
                data.DailyMissions.Add(new MissionSave
                {
                    Id = m.Id, Type = (int)m.Type, Target = m.Target, Baseline = m.Baseline,
                    RewardCents = m.RewardCents, Param = m.Param ?? "", Claimed = m.Claimed,
                });
            }
            return data;
        }

        private static SectionSave Clone(SectionSave s)
        {
            var c = new SectionSave { Id = s.Id, Unlocked = s.Unlocked, LaneCount = s.LaneCount };
            c.Booths.AddRange(s.Booths);
            c.Workers.AddRange(s.Workers);
            return c;
        }

        /// <summary>Restores a game and computes offline income since the save (pending until collected).</summary>
        public static TycoonGame FromSave(GameConfig config, SaveData data, DateTime utcNow, IAdService ads = null, int seed = 12345)
        {
            SaveMigrations.Migrate(data);
            var game = new TycoonGame(config, ads, seed);
            game.Apply(data);
            double elapsed = OfflineEarnings.ElapsedSeconds(data.LastSeenUtc, utcNow, game.Progression.OfflineCapHours);
            long earned = OfflineEarnings.Compute(game.EstimateOfflineIncomePerSecond(), elapsed);
            game.PendingOfflineCents = Math.Max(0, data.PendingOfflineCents) + earned;
            game.PendingOfflineSeconds = elapsed;
            return game;
        }

        private void Apply(SaveData data)
        {
            foreach (var id in data.UnlockedVehicles)
            {
                if (Config.FindVehicleType(id) != null) _unlocked.Add(id);
            }

            // v2 saves had a single road: it becomes section 1.
            var sections = data.Sections;
            if (sections.Count == 0)
            {
                sections = new List<SectionSave>
                {
                    new SectionSave { Id = _sections[0].Config.Id, Unlocked = true, LaneCount = data.LaneCount, Booths = data.Booths, Workers = data.Workers },
                };
            }
            foreach (var ss in sections)
            {
                var s = _sections.Find(x => x.Config.Id == ss.Id);
                if (s == null) continue;
                s.Unlocked = ss.Unlocked || s.Index == 0;
                if (!s.Unlocked) continue;
                // Rebuild through a simulation so every value is clamped to this section's limits.
                var sim = new GameSimulation(s.RoadConfig, _seed, new EconomyManager(0), new GameStats(), new HashSet<string>(_unlocked));
                sim.ApplySection(ss);
                s.Road = sim.CaptureSection(s.Config.Id);
                foreach (var b in ss.Services) s.Services.Restore(b.Id, b.Slot, b.Level);
                foreach (var d in ss.Decorations) s.Decorations.Restore(d.Id, d.Slot);
                s.EstimateDirty = true;
            }

            Economy.RestoreTotals(data.BalanceCents, data.TotalEarningsCents, data.TotalExpensesCents);
            GameSimulation.RestoreStats(Stats, data.Stats);
            Missions.RestoreMilestones(data.MilestoneIndex);
            var daily = new List<Mission>();
            foreach (var m in data.DailyMissions)
            {
                if (!Enum.IsDefined(typeof(MissionType), m.Type)) continue;
                daily.Add(new Mission
                {
                    Id = m.Id, Type = (MissionType)m.Type, Target = Math.Max(1, m.Target), Baseline = Math.Max(0, m.Baseline),
                    RewardCents = Math.Max(0, m.RewardCents), Param = m.Param, Claimed = m.Claimed,
                });
            }
            Missions.RestoreDaily(data.DailyMissionDate, daily);
            Boost.Restore(data.BoostSecondsLeft, data.BoostBasisPoints);

            int active = data.ActiveSection >= 0 && data.ActiveSection < _sections.Count && _sections[data.ActiveSection].Unlocked ? data.ActiveSection : 0;
            Activate(active);
        }
    }
}
