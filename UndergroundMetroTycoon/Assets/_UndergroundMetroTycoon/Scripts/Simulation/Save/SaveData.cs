using System;
using System.Collections.Generic;

namespace UndergroundMetroTycoon.Simulation
{
    /// <summary>
    /// Versioned save format. Plain public fields so both Unity's JsonUtility and other serializers handle it.
    /// Bump <see cref="CurrentVersion"/> and add a step to <see cref="SaveMigrator"/> when the format changes.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentVersion = 1;

        public int Version = CurrentVersion;
        public long SavedAtUtcTicks;
        public long Coins;
        public int TotalXp;
        public string EquippedToolId;

        public bool HasPlayerPose;
        public float PlayerX;
        public float PlayerY;
        public float PlayerZ;
        public float PlayerYaw;

        public List<ContractSaveData> Contracts = new List<ContractSaveData>();

        /// <summary>Contract layout currently on the site, plus HP per cell in grid index order.</summary>
        public string SiteContractId;
        public List<int> SiteCellHp = new List<int>();

        public SettingsData Settings = new SettingsData();
    }

    [Serializable]
    public sealed class ContractSaveData
    {
        public string Id;
        public int State;
        public int CompletionCount;
        public int Instance;
        public int LastClaimedInstance;
    }

    [Serializable]
    public sealed class SettingsData
    {
        public bool SoundEnabled = true;

        /// <summary>0 = Low, 1 = Medium, 2 = High.</summary>
        public int GraphicsQuality = 1;
    }

    /// <summary>File wrapper carrying a checksum of the payload so corruption is detected on load.</summary>
    [Serializable]
    public sealed class SaveEnvelope
    {
        public int Format = 1;
        public int DataVersion;
        public string Checksum;
        public string Payload;
    }
}
