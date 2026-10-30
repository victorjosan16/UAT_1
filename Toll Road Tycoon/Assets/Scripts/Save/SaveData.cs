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
        public const int CurrentVersion = 2;

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
        public string Language = "ro";
    }
}
