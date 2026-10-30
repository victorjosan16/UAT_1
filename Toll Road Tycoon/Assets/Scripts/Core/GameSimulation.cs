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
        private readonly HashSet<string> _unlockedVehicles = new HashSet<string>();
        private double _accumulator;

        public GameConfig Config { get; }
        public SimRandom Random { get; }
        public double Time { get; private set; }

        public EconomyManager Economy { get; }
        public GameStats Stats { get; } = new GameStats();
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
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Random = new SimRandom(seed);
            Economy = new EconomyManager(config.StartingBalanceCents, config.IncomeWindowSeconds);
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
            Expansion.BoothBuilt += _ => RaiseChanged();
            Expansion.LaneBuilt += _ => RaiseChanged();

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
                Stats = new StatsSave
                {
                    VehiclesProcessed = Stats.VehiclesProcessed,
                    HeavyVehiclesProcessed = Stats.HeavyVehiclesProcessed,
                    EmergencyVehiclesPassed = Stats.EmergencyVehiclesPassed,
                    TollCentsCollected = Stats.TollCentsCollected,
                    VehiclesSpawned = Stats.VehiclesSpawned,
                    TotalQueueWaitSeconds = Stats.TotalQueueWaitSeconds,
                    QueueWaitSamples = Stats.QueueWaitSamples,
                    PlayTimeSeconds = Stats.PlayTimeSeconds,
                },
            };
            foreach (var w in Workers.Workers)
            {
                data.Workers.Add(new WorkerSave { Id = w.Id, Level = w.Level });
            }
            foreach (var b in _booths)
            {
                data.Booths.Add(new BoothSave
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
            foreach (var id in _unlockedVehicles) data.UnlockedVehicles.Add(id);
            data.UnlockedVehicles.Sort(StringComparer.Ordinal);
            return data;
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
            var up = Config.BoothUpgrades;

            int lanes = Clamp(data.LaneCount, 1, Config.MaxLanes);
            for (int lane = 1; lane < lanes; lane++) Lanes.ActivateLane(lane);

            foreach (var id in data.UnlockedVehicles)
            {
                if (Config.FindVehicleType(id) != null) _unlockedVehicles.Add(id);
            }

            // Rebuild workers from scratch.
            var starter = _booths[0];
            Workers.Unassign(Workers.Workers[0]);
            var workersById = new Dictionary<int, Worker>();
            bool first = true;
            foreach (var ws in data.Workers)
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
            foreach (var bs in data.Booths)
            {
                if (bs.Slot < 0 || bs.Slot >= Config.MaxBooths || !seenSlots.Add(bs.Slot)) continue;
            }
            int boothCount = Math.Max(1, seenSlots.Count);
            for (int slot = 1; slot < boothCount; slot++) Expansion.AddBooth(slot);

            var usedWorkers = new HashSet<Worker>();
            var appliedSlots = new HashSet<int>();
            foreach (var bs in data.Booths)
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
            if (data.Booths.Count == 0 && Workers.Workers.Count > 0 && starter.AssignedWorker == null)
            {
                Workers.Assign(Workers.Workers[0], starter);
            }

            Economy.RestoreTotals(data.BalanceCents, data.TotalEarningsCents, data.TotalExpensesCents);
            var s = data.Stats;
            Stats.VehiclesProcessed = Math.Max(0, s.VehiclesProcessed);
            Stats.HeavyVehiclesProcessed = Math.Max(0, s.HeavyVehiclesProcessed);
            Stats.EmergencyVehiclesPassed = Math.Max(0, s.EmergencyVehiclesPassed);
            Stats.TollCentsCollected = Math.Max(0, s.TollCentsCollected);
            Stats.VehiclesSpawned = Math.Max(0, s.VehiclesSpawned);
            Stats.TotalQueueWaitSeconds = Math.Max(0, s.TotalQueueWaitSeconds);
            Stats.QueueWaitSamples = Math.Max(0, s.QueueWaitSamples);
            Stats.PlayTimeSeconds = Math.Max(0, s.PlayTimeSeconds);
            Economy.ResetIncomeWindow(Time);
            RaiseChanged();
        }

        private static int Clamp(int v, int min, int max) => v < min ? min : v > max ? max : v;
    }
}
