using System;
using System.Collections.Generic;
using TollRoadTycoon.Economy;

namespace TollRoadTycoon.Core
{
    [Serializable]
    public sealed class VehicleTypeConfig
    {
        public string Id;
        public string NameKey;
        public long TollCents;
        public double Length;
        public double Width;
        public double MaxSpeed;
        public double Acceleration;
        public double Deceleration;
        /// <summary>Multiplier applied to the booth processing time (trucks take longer to pay).</summary>
        public double ServiceTimeMultiplier;
        public double SpawnWeight;
        public bool UnlockedByDefault;
        public long UnlockPriceCents;
        public bool IsHeavy;
        public bool IsEmergency;

        public VehicleTypeConfig Clone() => (VehicleTypeConfig)MemberwiseClone();
    }

    /// <summary>World-space layout of the starter road (units ~ metres / 4, road runs bottom to top).</summary>
    [Serializable]
    public sealed class RoadLayoutConfig
    {
        /// <summary>X of the entry/exit lanes in purchase order.</summary>
        public double[] LaneX = { 0.0, 1.6, -1.6 };
        /// <summary>X of the toll booth slots in purchase order (centre outwards).</summary>
        public double[] BoothSlotX = { 0.8, -0.8, 2.4, -2.4, 4.0, -4.0 };
        public double SpawnY = -34;
        public double DecisionY = -18;
        public double BoothLaneStartY = -14;
        public double StopLineY = 0;
        public double BarrierY = 0.35;
        public double BoothLaneEndY = 3;
        public double ExitStartY = 7;
        public double DespawnY = 34;
        public double OncomingLaneX = -6.2;
        /// <summary>A vehicle picks its booth this far before the plaza entry point.</summary>
        public double DecisionLookahead = 7;
    }

    [Serializable]
    public sealed class TrafficConfig
    {
        /// <summary>Tolled vehicles per second with one lane and every vehicle type locked.</summary>
        public double BaseArrivalsPerSecond = 0.45;
        public double ArrivalsPerExtraLane = 0.35;
        /// <summary>Each unlocked vehicle type beyond the defaults adds this fraction of demand.</summary>
        public double ArrivalBonusPerUnlockedType = 0.12;
        public double OncomingArrivalsPerSecond = 0.25;
        public double MinGap = 0.45;
        public double SpawnClearance = 1.2;
        /// <summary>Pending arrivals are capped so a blocked spawn point cannot cause a burst later.</summary>
        public double MaxPendingArrivals = 2;
    }

    [Serializable]
    public sealed class BoothUpgradeConfig
    {
        public int MaxSpeedLevel = 10;
        public double SpeedBonusPerLevel = 0.15;
        public CostCurve SpeedCost = new CostCurve(Money.FromDollars(100), 1.55);

        public int MaxTollLevel = 10;
        public long TollBasisPointsPerLevel = 2500;
        public CostCurve TollCost = new CostCurve(Money.FromDollars(150), 1.6);

        public int BaseQueueCapacity = 4;
        public int MaxCapacityLevel = 4;
        public CostCurve CapacityCost = new CostCurve(Money.FromDollars(200), 1.9);

        public double BaseBarrierSeconds = 0.55;
        public int MaxBarrierLevel = 5;
        public double BarrierBonusPerLevel = 0.25;
        public CostCurve BarrierCost = new CostCurve(Money.FromDollars(80), 1.6);

        public long AutomationCostCents = Money.FromDollars(3000);
        public int AutomationRequiredSpeedLevel = 3;
        public double AutomaticProcessingSeconds = 1.6;
    }

    [Serializable]
    public sealed class WorkerConfig
    {
        public double[] ProcessingSecondsByLevel = { 3.0, 2.7, 2.4, 2.1, 1.8, 1.6, 1.45, 1.3, 1.2, 1.1 };
        public CostCurve HireCost = new CostCurve(Money.FromDollars(200), 1.7);
        public CostCurve UpgradeCost = new CostCurve(Money.FromDollars(150), 1.65);
        public int MaxLevel => ProcessingSecondsByLevel.Length;
    }

    [Serializable]
    public sealed class ExpansionConfig
    {
        /// <summary>Price of each booth slot; slot 0 is the free starter booth.</summary>
        public long[] BoothSlotPrices =
        {
            0, Money.FromDollars(400), Money.FromDollars(1200), Money.FromDollars(3500),
            Money.FromDollars(10000), Money.FromDollars(30000),
        };
        /// <summary>Number of road lanes required before a booth slot can be bought.</summary>
        public int[] BoothSlotRequiredLanes = { 1, 1, 2, 2, 3, 3 };
        public long[] LanePrices = { 0, Money.FromDollars(1500), Money.FromDollars(7500) };
    }

    /// <summary>All gameplay tunables. Unity wraps this in a ScriptableObject; tests use CreateDefault().</summary>
    [Serializable]
    public sealed class GameConfig
    {
        public double FixedTimeStep = 1.0 / 30.0;
        public long StartingBalanceCents = Money.FromDollars(250);
        public double IncomeWindowSeconds = 60;
        public List<VehicleTypeConfig> VehicleTypes = new List<VehicleTypeConfig>();
        public RoadLayoutConfig Layout = new RoadLayoutConfig();
        public TrafficConfig Traffic = new TrafficConfig();
        public BoothUpgradeConfig BoothUpgrades = new BoothUpgradeConfig();
        public WorkerConfig Workers = new WorkerConfig();
        public ExpansionConfig Expansion = new ExpansionConfig();

        public int MaxBooths => Math.Min(Layout.BoothSlotX.Length, Expansion.BoothSlotPrices.Length);
        public int MaxLanes => Math.Min(Layout.LaneX.Length, Expansion.LanePrices.Length);

        public VehicleTypeConfig FindVehicleType(string id)
        {
            foreach (var type in VehicleTypes)
            {
                if (type.Id == id) return type;
            }
            return null;
        }

        public static GameConfig CreateDefault()
        {
            var config = new GameConfig();
            config.VehicleTypes.AddRange(new[]
            {
                Vehicle("compact", 5, 0.95, 0.62, 6.0, 4.0, 8.0, 1.0, 10, true, 0),
                Vehicle("sedan", 8, 1.10, 0.66, 6.0, 3.6, 7.5, 1.0, 8, true, 0),
                Vehicle("suv", 10, 1.20, 0.72, 5.6, 3.2, 7.0, 1.1, 6, false, 600),
                Vehicle("van", 15, 1.30, 0.74, 5.2, 2.8, 6.5, 1.2, 5, false, 2000),
                Vehicle("truck", 25, 1.80, 0.80, 4.6, 2.0, 5.0, 1.5, 4, false, 5000, heavy: true),
                Vehicle("bus", 35, 2.20, 0.82, 4.4, 1.8, 4.5, 1.6, 2, false, 12000, heavy: true),
                Vehicle("heavy_truck", 50, 2.60, 0.84, 4.0, 1.5, 4.0, 1.8, 2, false, 30000, heavy: true),
                Vehicle("emergency", 0, 1.20, 0.70, 7.0, 5.0, 9.0, 0.3, 0.3, true, 0, emergency: true),
            });
            return config;
        }

        private static VehicleTypeConfig Vehicle(string id, long tollDollars, double length, double width,
            double maxSpeed, double accel, double decel, double serviceMultiplier, double weight,
            bool unlocked, long unlockDollars, bool heavy = false, bool emergency = false)
        {
            return new VehicleTypeConfig
            {
                Id = id,
                NameKey = "vehicle." + id,
                TollCents = Money.FromDollars(tollDollars),
                Length = length,
                Width = width,
                MaxSpeed = maxSpeed,
                Acceleration = accel,
                Deceleration = decel,
                ServiceTimeMultiplier = serviceMultiplier,
                SpawnWeight = weight,
                UnlockedByDefault = unlocked,
                UnlockPriceCents = Money.FromDollars(unlockDollars),
                IsHeavy = heavy,
                IsEmergency = emergency,
            };
        }
    }
}
