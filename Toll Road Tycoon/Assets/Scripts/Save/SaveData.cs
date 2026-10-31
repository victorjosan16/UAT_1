using System;
using System.Collections.Generic;

namespace TollRoadTycoon.Save
{
    /// <summary>
    /// Versioned save model. Only public fields and lists so it serialises with Unity's JsonUtility
    /// as well as System.Text.Json in tests. Vehicles in transit are deliberately not saved:
    /// their toll was not paid yet, so dropping them can never double-award money.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentVersion = 3;

        public int Version = CurrentVersion;
        public string GameVersion = "";
        public string LastSeenUtc = "";

        public long BalanceCents;
        public long TotalEarningsCents;
        public long TotalExpensesCents;

        public int LaneCount = 1;
        public List<BoothSave> Booths = new List<BoothSave>();
        public List<WorkerSave> Workers = new List<WorkerSave>();
        public List<string> UnlockedVehicles = new List<string>();
        public StatsSave Stats = new StatsSave();
        public SettingsSave Settings = new SettingsSave();

        // v3: road sections, services, decorations, missions, boosts, offline income.
        public int ActiveSection;
        public List<SectionSave> Sections = new List<SectionSave>();
        public string DailyMissionDate = "";
        public List<MissionSave> DailyMissions = new List<MissionSave>();
        public int MilestoneIndex;
        public long PendingOfflineCents;
        public double BoostSecondsLeft;
        public long BoostBasisPoints = 10000;
    }

    [Serializable]
    public sealed class SectionSave
    {
        public string Id = "";
        public bool Unlocked;
        public int LaneCount = 1;
        public List<BoothSave> Booths = new List<BoothSave>();
        public List<WorkerSave> Workers = new List<WorkerSave>();
        public List<ServiceSave> Services = new List<ServiceSave>();
        public List<DecorationSave> Decorations = new List<DecorationSave>();
    }

    [Serializable]
    public sealed class ServiceSave
    {
        public string Id = "";
        public int Slot;
        public int Level = 1;
    }

    [Serializable]
    public sealed class DecorationSave
    {
        public string Id = "";
        public int Slot;
    }

    [Serializable]
    public sealed class MissionSave
    {
        public string Id = "";
        public int Type;
        public long Target;
        public long Baseline;
        public long RewardCents;
        public string Param = "";
        public bool Claimed;
    }

    [Serializable]
    public sealed class BoothSave
    {
        public int Slot;
        public int SpeedLevel;
        public int TollLevel;
        public int CapacityLevel;
        public int BarrierLevel;
        public bool Automatic;
        /// <summary>Worker id or 0 when unstaffed.</summary>
        public int WorkerId;
    }

    [Serializable]
    public sealed class WorkerSave
    {
        public int Id;
        public int Level = 1;
    }

    [Serializable]
    public sealed class StatsSave
    {
        public long VehiclesProcessed;
        public long HeavyVehiclesProcessed;
        public long EmergencyVehiclesPassed;
        public long TollCentsCollected;
        public long VehiclesSpawned;
        public double TotalQueueWaitSeconds;
        public long QueueWaitSamples;
        public double PlayTimeSeconds;
        public long BoothsBuilt;
        public long LanesBuilt;
        public long SpeedUpgrades;
        public long CapacityUpgrades;
        public long WorkerUpgrades;
        public long ServicesBuilt;
        public long SectionsUnlocked;
        public long ServiceCustomers;
        public long ServiceIncomeCents;
        public long OfflineIncomeCents;
        public long PassiveIncomeCents;
        public long BoostsUsed;
    }

    [Serializable]
    public sealed class SettingsSave
    {
        public bool MusicEnabled = true;
        public bool SfxEnabled = true;
        public bool VibrationEnabled = true;
        public float MusicVolume = 0.6f;
        public float SfxVolume = 0.8f;
        public int GraphicsQuality = 2;
        /// <summary>Language code; empty means "use the device language" (English when unsupported).</summary>
        public string Language = "";
    }
}
