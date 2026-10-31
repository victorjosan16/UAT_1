using System;
using System.Collections.Generic;
using System.Globalization;
using TollRoadTycoon.Economy;
using TollRoadTycoon.RoadExpansion;
using TollRoadTycoon.Save;
using TollRoadTycoon.TollSystem;
using TollRoadTycoon.Traffic;
using TollRoadTycoon.Upgrades;
using TollRoadTycoon.Workers;

namespace TollRoadTycoon.Core
{
    /// <summary>
    /// Root of the engine-independent game: owns every service and advances them with a fixed step.
    /// Unity views only read from it and call its purchase APIs.
    /// </summary>
    public sealed class GameSimulation
    {
        public const int MaxStepsPerTick = 8;

        private readonly List<TollBooth> _booths = new List<TollBooth>();
        private readonly HashSet<string> _unlockedVehicles;
        private double _accumulator;

        public GameConfig Config { get; }
        public SimRandom Random { get; }
        public double Time { get; private set; }

        public EconomyManager Economy { get; }
        public GameStats Stats { get; }
        public LaneManager Lanes { get; }
        public QueueManager Queues { get; }
        public TrafficManager Traffic { get; }
        public VehicleSpawner Spawner { get; }
        public PaymentProcessor Payments { get; }
        public TollBoothController BoothController { get; }
        public WorkerManager Workers { get; }
        public UpgradeManager Upgrades { get; }
        public RoadExpansionManager Expansion { get; }

        public IReadOnlyList<TollBooth> Booths => _booths;
        public IReadOnlyCollection<string> UnlockedVehicles => _unlockedVehicles;

        /// <summary>Raised after any purchase or structural change so UI can refresh.</summary>
        public event Action StateChanged;

        /// <summary>Simulation interpolation factor in [0,1) between the last two fixed steps.</summary>
        public double InterpolationAlpha => Config.FixedTimeStep > 0 ? _accumulator / Config.FixedTimeStep : 0;

        public GameSimulation(GameConfig config, int seed = 12345)
            : this(config, seed, null, null, null)
        {
        }

        /// <summary>
        /// Builds one road. Economy, statistics and vehicle unlocks can be shared between roads
        /// (road sections); when null, the road owns its own (standalone game / tests).
        /// </summary>
        public GameSimulation(GameConfig config, int seed, EconomyManager sharedEconomy, GameStats sharedStats, HashSet<string> sharedUnlocks)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Random = new SimRandom(seed);
            Economy = sharedEconomy ?? new EconomyManager(config.StartingBalanceCents, config.IncomeWindowSeconds);
            Stats = sharedStats ?? new GameStats();
            _unlockedVehicles = sharedUnlocks ?? new HashSet<string>();
            Lanes = new LaneManager(config.Layout, config.MaxLanes, config.MaxBooths);
            Queues = new QueueManager(Lanes, config.Traffic, _booths, Random);
            Traffic = new TrafficManager(Lanes, Queues, config.Traffic);
            Spawner = new VehicleSpawner(config, Lanes, Traffic, Random, _unlockedVehicles);
            Payments = new PaymentProcessor(Economy, Stats);
            BoothController = new TollBoothController(Payments, Lanes, Stats);
            Workers = new WorkerManager(config.Workers, Economy);
            Upgrades = new UpgradeManager(config, Economy, Workers, _unlockedVehicles);
            Expansion = new RoadExpansionManager(config, Economy, Lanes, Workers, _booths);

            BoothController.BoothClosed += Workers.OnBoothClosed;
            Traffic.VehicleSpawned += _ => Stats.VehiclesSpawned++;
            Traffic.VehicleDespawned += v => Payments.ForgetTrip(v.TripId);
            Workers.Changed += RaiseChanged;
            Upgrades.Changed += RaiseChanged;
            Expansion.BoothBuilt += _ =>
            {
                Stats.BoothsBuilt++;
                RaiseChanged();
            };
            Expansion.LaneBuilt += _ =>
            {
                Stats.LanesBuilt++;
                RaiseChanged();
            };
            Workers.WorkerUpgraded += _ => Stats.WorkerUpgrades++;
            Upgrades.Upgraded += (_, type) =>
            {
                if (type == BoothUpgradeType.Speed) Stats.SpeedUpgrades++;
                if (type == BoothUpgradeType.Capacity) Stats.CapacityUpgrades++;
            };

            SetupNewGame();
        }

        private void SetupNewGame()
        {
            foreach (var type in Config.VehicleTypes)
            {
                if (type.UnlockedByDefault) _unlockedVehicles.Add(type.Id);
            }
            Lanes.ActivateLane(0);
            var booth = Expansion.AddBooth(0);
            var worker = Workers.AddWorker(1);
            Workers.Assign(worker, booth);
            Economy.ResetIncomeWindow(Time);
        }

        /// <summary>Advances by real elapsed time using fixed steps (bounded to avoid a spiral of death).</summary>
        public void Tick(double deltaSeconds)
        {
            if (deltaSeconds <= 0 || double.IsNaN(deltaSeconds)) return;
            double step = Config.FixedTimeStep;
            _accumulator = Math.Min(_accumulator + deltaSeconds, step * MaxStepsPerTick);
            while (_accumulator >= step)
            {
                Step(step);
                _accumulator -= step;
            }
        }

        public void Step(double dt)
        {
            Time += dt;
            Stats.PlayTimeSeconds += dt;
            Economy.Tick(Time);
            Spawner.Tick(Time, dt);
            Traffic.Tick(Time, dt);
            foreach (var booth in _booths) BoothController.Tick(booth, Time, dt);
        }

        /// <summary>Runs the simulation for a duration in fixed steps (tests and balancing tools).</summary>
        public void RunFor(double seconds)
        {
            int steps = (int)Math.Round(seconds / Config.FixedTimeStep);
            for (int i = 0; i < steps; i++) Step(Config.FixedTimeStep);
        }

        public double TheoreticalThroughputPerSecond
        {
            get
            {
                double total = 0;
                foreach (var b in _booths)
                {
                    if (b.IsAcceptingVehicles) total += 1.0 / b.CycleSeconds;
                }
                return total;
            }
        }

        private void RaiseChanged() => StateChanged?.Invoke();

        // ---- Save / load -------------------------------------------------------------------

        public SaveData CaptureSave(DateTime utcNow, string gameVersion, SettingsSave settings)
        {
            var data = new SaveData
            {
                GameVersion = gameVersion ?? "",
                LastSeenUtc = utcNow.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture),
                BalanceCents = Economy.Balance,
                TotalEarningsCents = Economy.TotalEarnings,
                TotalExpensesCents = Economy.TotalExpenses,
                LaneCount = Lanes.ActiveLaneCount,
                Settings = settings ?? new SettingsSave(),
                Stats = CaptureStats(Stats),
            };
            CaptureRoad(data.Booths, data.Workers);
            foreach (var id in _unlockedVehicles) data.UnlockedVehicles.Add(id);
            data.UnlockedVehicles.Sort(StringComparer.Ordinal);
            return data;
        }

        /// <summary>Road state only (lanes, booths, workers) for multi-section games.</summary>
        public SectionSave CaptureSection(string id)
        {
            var data = new SectionSave { Id = id, Unlocked = true, LaneCount = Lanes.ActiveLaneCount };
            CaptureRoad(data.Booths, data.Workers);
            return data;
        }

        private void CaptureRoad(List<BoothSave> booths, List<WorkerSave> workers)
        {
            foreach (var w in Workers.Workers)
            {
                workers.Add(new WorkerSave { Id = w.Id, Level = w.Level });
            }
            foreach (var b in _booths)
            {
                booths.Add(new BoothSave
                {
                    Slot = b.SlotIndex,
                    SpeedLevel = b.SpeedLevel,
                    TollLevel = b.TollLevel,
                    CapacityLevel = b.CapacityLevel,
                    BarrierLevel = b.BarrierLevel,
                    Automatic = b.Mode == BoothMode.Automatic,
                    // A worker pending release is saved as already released.
                    WorkerId = b.AssignedWorker != null && !b.PendingClose ? b.AssignedWorker.Id : 0,
                });
            }
        }

        /// <summary>Restores a road from a section save (values clamped to the config).</summary>
        public void ApplySection(SectionSave section)
        {
            ApplyRoad(section.LaneCount, section.Booths, section.Workers);
            RaiseChanged();
        }

        /// <summary>Builds a simulation from a save, clamping every value to what the config allows.</summary>
        public static GameSimulation FromSave(GameConfig config, SaveData data, int seed = 12345)
        {
            var sim = new GameSimulation(config, seed);
            sim.ApplySave(data);
            return sim;
        }

        private void ApplySave(SaveData data)
        {
            SaveMigrations.Migrate(data);
            foreach (var id in data.UnlockedVehicles)
            {
                if (Config.FindVehicleType(id) != null) _unlockedVehicles.Add(id);
            }
            ApplyRoad(data.LaneCount, data.Booths, data.Workers);
            ApplyGlobals(data);
        }

        private void ApplyRoad(int laneCount, List<BoothSave> boothSaves, List<WorkerSave> workerSaves)
        {
            var up = Config.BoothUpgrades;
            int lanes = Clamp(laneCount, 1, Config.MaxLanes);
            for (int lane = 1; lane < lanes; lane++) Lanes.ActivateLane(lane);

            // Rebuild workers from scratch.
            var starter = _booths[0];
            Workers.Unassign(Workers.Workers[0]);
            var workersById = new Dictionary<int, Worker>();
            bool first = true;
            foreach (var ws in workerSaves)
            {
                if (ws.Id <= 0 || workersById.ContainsKey(ws.Id)) continue;
                Worker worker;
                if (first)
                {
                    worker = Workers.Workers[0];
                    worker.Level = Clamp(ws.Level, 1, Config.Workers.MaxLevel);
                    first = false;
                }
                else
                {
                    worker = Workers.AddWorker(Clamp(ws.Level, 1, Config.Workers.MaxLevel));
                }
                workersById[ws.Id] = worker;
            }

            var seenSlots = new HashSet<int>();
            foreach (var bs in boothSaves)
            {
                if (bs.Slot < 0 || bs.Slot >= Config.MaxBooths || !seenSlots.Add(bs.Slot)) continue;
            }
            int boothCount = Math.Max(1, seenSlots.Count);
            for (int slot = 1; slot < boothCount; slot++) Expansion.AddBooth(slot);

            var usedWorkers = new HashSet<Worker>();
            var appliedSlots = new HashSet<int>();
            foreach (var bs in boothSaves)
            {
                if (bs.Slot < 0 || bs.Slot >= _booths.Count || !appliedSlots.Add(bs.Slot)) continue;
                var booth = _booths[bs.Slot];
                booth.SpeedLevel = Clamp(bs.SpeedLevel, 0, up.MaxSpeedLevel);
                booth.TollLevel = Clamp(bs.TollLevel, 0, up.MaxTollLevel);
                booth.CapacityLevel = Clamp(bs.CapacityLevel, 0, up.MaxCapacityLevel);
                booth.BarrierLevel = Clamp(bs.BarrierLevel, 0, up.MaxBarrierLevel);
                booth.Mode = bs.Automatic ? BoothMode.Automatic : BoothMode.Manual;
                if (booth.Mode == BoothMode.Manual && workersById.TryGetValue(bs.WorkerId, out var w) && usedWorkers.Add(w))
                {
                    Workers.Assign(w, booth);
                }
            }
            if (boothSaves.Count == 0 && Workers.Workers.Count > 0 && starter.AssignedWorker == null)
            {
                Workers.Assign(Workers.Workers[0], starter);
            }
        }

        private void ApplyGlobals(SaveData data)
        {
            Economy.RestoreTotals(data.BalanceCents, data.TotalEarningsCents, data.TotalExpensesCents);
            var s = data.Stats;
            RestoreStats(Stats, s);
            Economy.ResetIncomeWindow(Time);
            RaiseChanged();
        }

        internal static void RestoreStats(GameStats stats, StatsSave s)
        {
            stats.VehiclesProcessed = Math.Max(0, s.VehiclesProcessed);
            stats.HeavyVehiclesProcessed = Math.Max(0, s.HeavyVehiclesProcessed);
            stats.EmergencyVehiclesPassed = Math.Max(0, s.EmergencyVehiclesPassed);
            stats.TollCentsCollected = Math.Max(0, s.TollCentsCollected);
            stats.VehiclesSpawned = Math.Max(0, s.VehiclesSpawned);
            stats.TotalQueueWaitSeconds = Math.Max(0, s.TotalQueueWaitSeconds);
            stats.QueueWaitSamples = Math.Max(0, s.QueueWaitSamples);
            stats.PlayTimeSeconds = Math.Max(0, s.PlayTimeSeconds);
            stats.BoothsBuilt = Math.Max(0, s.BoothsBuilt);
            stats.LanesBuilt = Math.Max(0, s.LanesBuilt);
            stats.SpeedUpgrades = Math.Max(0, s.SpeedUpgrades);
            stats.CapacityUpgrades = Math.Max(0, s.CapacityUpgrades);
            stats.WorkerUpgrades = Math.Max(0, s.WorkerUpgrades);
            stats.ServicesBuilt = Math.Max(0, s.ServicesBuilt);
            stats.SectionsUnlocked = Math.Max(0, s.SectionsUnlocked);
            stats.ServiceCustomers = Math.Max(0, s.ServiceCustomers);
            stats.ServiceIncomeCents = Math.Max(0, s.ServiceIncomeCents);
            stats.OfflineIncomeCents = Math.Max(0, s.OfflineIncomeCents);
            stats.PassiveIncomeCents = Math.Max(0, s.PassiveIncomeCents);
            stats.BoostsUsed = Math.Max(0, s.BoostsUsed);
        }

        internal static StatsSave CaptureStats(GameStats st)
        {
            return new StatsSave
            {
                VehiclesProcessed = st.VehiclesProcessed,
                HeavyVehiclesProcessed = st.HeavyVehiclesProcessed,
                EmergencyVehiclesPassed = st.EmergencyVehiclesPassed,
                TollCentsCollected = st.TollCentsCollected,
                VehiclesSpawned = st.VehiclesSpawned,
                TotalQueueWaitSeconds = st.TotalQueueWaitSeconds,
                QueueWaitSamples = st.QueueWaitSamples,
                PlayTimeSeconds = st.PlayTimeSeconds,
                BoothsBuilt = st.BoothsBuilt,
                LanesBuilt = st.LanesBuilt,
                SpeedUpgrades = st.SpeedUpgrades,
                CapacityUpgrades = st.CapacityUpgrades,
                WorkerUpgrades = st.WorkerUpgrades,
                ServicesBuilt = st.ServicesBuilt,
                SectionsUnlocked = st.SectionsUnlocked,
                ServiceCustomers = st.ServiceCustomers,
                ServiceIncomeCents = st.ServiceIncomeCents,
                OfflineIncomeCents = st.OfflineIncomeCents,
                PassiveIncomeCents = st.PassiveIncomeCents,
                BoostsUsed = st.BoostsUsed,
            };
        }

        private static int Clamp(int v, int min, int max) => v < min ? min : v > max ? max : v;
    }
}
