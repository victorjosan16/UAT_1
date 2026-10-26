using System.Collections.Generic;

namespace UndergroundMetroTycoon.Simulation
{
    /// <summary>Upgrades older save data to <see cref="SaveData.CurrentVersion"/>, one version step at a time.</summary>
    public static class SaveMigrator
    {
        public static bool TryMigrate(SaveData data, out string error)
        {
            error = null;
            if (data == null)
            {
                error = "Save data is empty.";
                return false;
            }
            if (data.Version > SaveData.CurrentVersion)
            {
                error = $"Save version {data.Version} was written by a newer build (supported: {SaveData.CurrentVersion}).";
                return false;
            }

            if (data.Version < 1)
            {
                // Pre-release files had no version field; normalise them to version 1.
                data.Version = 1;
            }

            // Future steps go here, e.g. if (data.Version == 1) { ...; data.Version = 2; }

            if (data.Contracts == null) data.Contracts = new List<ContractSaveData>();
            if (data.SiteCellHp == null) data.SiteCellHp = new List<int>();
            if (data.Settings == null) data.Settings = new SettingsData();
            return true;
        }
    }
}
