using System;
using System.Collections.Generic;
using TollRoadTycoon.Economy;

namespace TollRoadTycoon.Core
{
    [Serializable]
    public sealed class RoadSectionConfig
    {
        public string Id;
        public string NameKey;
        public int KmFrom;
        public int KmTo;
        public long UnlockPriceCents;
        /// <summary>Booths the previous section must have built before this one can be bought.</summary>
        public int RequiredPreviousBooths;
        /// <summary>Income per minute (cents) the active road must reach before this one can be bought.</summary>
        public long RequiredIncomePerMinuteCents;
        public int MaxBooths = 6;
        public int MaxLanes = 3;
        public double PriceMultiplier = 1;
        public double TollMultiplier = 1;
        public double DemandMultiplier = 1;
        public double AutomationCostMultiplier = 1;
        public int AutomationRequiredSpeedLevel = 3;
        public List<string> AllowedVehicles = new List<string>();
        public List<string> AllowedServices = new List<string>();
        /// <summary>Grass tint and scenery seed so every section looks different.</summary>
        public string GrassColor = "A8D78D";
        public int SceneryDensity = 260;

        public bool AllowsVehicle(string id) => AllowedVehicles.Count == 0 || AllowedVehicles.Contains(id);
        public bool AllowsService(string id) => AllowedServices.Contains(id);
    }

    [Serializable]
    public sealed class ServiceConfig
    {
        public string Id;
        public string NameKey;
        public long BaseCostCents;
        /// <summary>Customers served at the same time.</summary>
        public int Capacity;
        public double ServiceSeconds;
        /// <summary>Money one customer spends at level 1.</summary>
        public long SpendCents;
        /// <summary>Chance that a car / heavy vehicle that just paid the toll stops here.</summary>
        public double VisitChance;
        public double HeavyVisitChance;
        public int MaxLevel = 5;
        public CostCurve UpgradeCost;
        /// <summary>Each level adds this fraction to spending and one customer slot every two levels.</summary>
        public double SpendBonusPerLevel = 0.35;
    }

    [Serializable]
    public sealed class DecorationConfig
    {
        public string Id;
        public string NameKey;
        public long PriceCents;
    }

    [Serializable]
    public sealed class ProgressionConfig
    {
        public List<RoadSectionConfig> Sections = new List<RoadSectionConfig>();
        public List<ServiceConfig> Services = new List<ServiceConfig>();
        public List<DecorationConfig> Decorations = new List<DecorationConfig>();

        /// <summary>World positions of building slots beside the road (same layout on every section).</summary>
        public List<Vec2> ServiceSlots = new List<Vec2>
        {
            new Vec2(8.2, -9), new Vec2(8.2, -15), new Vec2(8.2, 6.5), new Vec2(8.2, 13),
            new Vec2(-10.4, -9), new Vec2(-10.4, -15), new Vec2(-10.4, 6.5), new Vec2(-10.4, 13),
        };

        /// <summary>World positions of small decoration slots near the plaza.</summary>
        public List<Vec2> DecorationSlots = new List<Vec2>
        {
            new Vec2(6.0, 2.6), new Vec2(6.0, -6.2), new Vec2(7.0, 3.8), new Vec2(6.2, -8.0),
            new Vec2(-7.9, 2.5), new Vec2(-7.9, -3.0), new Vec2(-7.9, -6.0), new Vec2(6.6, 9.5),
        };

        public double OfflineCapHours = 8;
        /// <summary>Inactive roads and offline time use estimates scaled down by this factor.</summary>
        public double EstimateSafetyFactor = 0.85;

        public double BoostSeconds = 120;
        public long BoostBasisPoints = 20000;
        public long BoostCashPriceCents = Money.FromDollars(2500);
        public double BoostCashSeconds = 300;
        /// <summary>Instant cash reward = this many minutes of current income (at least the minimum).</summary>
        public double InstantCashMinutes = 3;
        public long InstantCashMinimumCents = Money.FromDollars(200);

        public int DailyMissionCount = 4;

        public RoadSectionConfig FindSection(string id) => Sections.Find(s => s.Id == id);
        public ServiceConfig FindService(string id) => Services.Find(s => s.Id == id);
        public DecorationConfig FindDecoration(string id) => Decorations.Find(d => d.Id == id);

        public static ProgressionConfig CreateDefault()
        {
            var p = new ProgressionConfig();
            var light = new List<string> { "compact", "sedan", "suv", "van", "emergency" };
            var medium = new List<string>(light) { "truck" };
            var all = new List<string>(medium) { "bus", "heavy_truck" };

            p.Sections.Add(new RoadSectionConfig
            {
                Id = "s1", NameKey = "section.s1", KmFrom = 0, KmTo = 5, UnlockPriceCents = 0,
                MaxBooths = 4, MaxLanes = 2, AllowedVehicles = light,
                AllowedServices = new List<string> { "parking", "cafe" },
            });
            p.Sections.Add(new RoadSectionConfig
            {
                Id = "s2", NameKey = "section.s2", KmFrom = 5, KmTo = 10, UnlockPriceCents = Money.FromDollars(20000),
                RequiredPreviousBooths = 3, MaxBooths = 5, MaxLanes = 2,
                PriceMultiplier = 4, TollMultiplier = 1.6, DemandMultiplier = 1.25, AllowedVehicles = medium,
                AllowedServices = new List<string> { "parking", "cafe", "gas", "rest_area" },
                GrassColor = "9FD17F", SceneryDensity = 220,
            });
            p.Sections.Add(new RoadSectionConfig
            {
                Id = "s3", NameKey = "section.s3", KmFrom = 10, KmTo = 20, UnlockPriceCents = Money.FromDollars(200000),
                RequiredPreviousBooths = 4, MaxBooths = 6, MaxLanes = 3,
                PriceMultiplier = 15, TollMultiplier = 2.6, DemandMultiplier = 1.5, AllowedVehicles = all,
                AllowedServices = new List<string> { "parking", "cafe", "gas", "rest_area", "store", "car_wash" },
                GrassColor = "B5D98A", SceneryDensity = 180,
            });
            p.Sections.Add(new RoadSectionConfig
            {
                Id = "s4", NameKey = "section.s4", KmFrom = 20, KmTo = 30, UnlockPriceCents = Money.FromDollars(2000000),
                RequiredPreviousBooths = 5, RequiredIncomePerMinuteCents = Money.FromDollars(15000), MaxBooths = 6, MaxLanes = 3,
                PriceMultiplier = 60, TollMultiplier = 4.2, DemandMultiplier = 1.9,
                AutomationCostMultiplier = 0.5, AutomationRequiredSpeedLevel = 1, AllowedVehicles = all,
                AllowedServices = new List<string> { "parking", "cafe", "gas", "rest_area", "store", "car_wash", "charging" },
                GrassColor = "A3CF86", SceneryDensity = 150,
            });
            p.Sections.Add(new RoadSectionConfig
            {
                Id = "s5", NameKey = "section.s5", KmFrom = 30, KmTo = 50, UnlockPriceCents = Money.FromDollars(20000000),
                RequiredPreviousBooths = 6, RequiredIncomePerMinuteCents = Money.FromDollars(80000), MaxBooths = 6, MaxLanes = 3,
                PriceMultiplier = 250, TollMultiplier = 7, DemandMultiplier = 2.3,
                AutomationCostMultiplier = 0.4, AutomationRequiredSpeedLevel = 1, AllowedVehicles = all,
                AllowedServices = new List<string> { "parking", "cafe", "gas", "rest_area", "store", "car_wash", "charging" },
                GrassColor = "98CC7B", SceneryDensity = 200,
            });

            p.Services.Add(Service("parking", 600, 6, 20, 2, 0.10, 0.06, 1.7));
            p.Services.Add(Service("cafe", 1000, 4, 14, 6, 0.08, 0.10, 1.75));
            p.Services.Add(Service("gas", 4000, 4, 10, 18, 0.10, 0.30, 1.8));
            p.Services.Add(Service("rest_area", 3000, 6, 12, 5, 0.12, 0.22, 1.75));
            p.Services.Add(Service("store", 15000, 4, 12, 30, 0.08, 0.10, 1.8));
            p.Services.Add(Service("car_wash", 25000, 2, 16, 60, 0.05, 0.04, 1.85));
            p.Services.Add(Service("charging", 120000, 4, 25, 250, 0.06, 0.02, 1.9));

            p.Decorations.Add(Decor("bench", 150));
            p.Decorations.Add(Decor("lamp", 250));
            p.Decorations.Add(Decor("flowers", 400));
            p.Decorations.Add(Decor("bin", 100));
            p.Decorations.Add(Decor("cone", 80));
            p.Decorations.Add(Decor("fence", 300));
            return p;
        }

        private static ServiceConfig Service(string id, long costDollars, int capacity, double seconds, long spendDollars,
            double chance, double heavyChance, double growth)
        {
            return new ServiceConfig
            {
                Id = id,
                NameKey = "service." + id,
                BaseCostCents = Money.FromDollars(costDollars),
                Capacity = capacity,
                ServiceSeconds = seconds,
                SpendCents = Money.FromDollars(spendDollars),
                VisitChance = chance,
                HeavyVisitChance = heavyChance,
                UpgradeCost = new CostCurve(Money.FromDollars(costDollars / 2), growth),
            };
        }

        private static DecorationConfig Decor(string id, long priceDollars)
        {
            return new DecorationConfig { Id = id, NameKey = "decor." + id, PriceCents = Money.FromDollars(priceDollars) };
        }
    }
}
