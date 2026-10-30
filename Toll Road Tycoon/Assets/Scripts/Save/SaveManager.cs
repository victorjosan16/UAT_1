using System;
using System.Collections.Generic;
using System.IO;

namespace TollRoadTycoon.Save
{
    public interface ISaveSerializer
    {
        string Serialize(SaveData data);
        SaveData Deserialize(string json);
    }

    public enum LoadSource
    {
        None,
        Primary,
        Backup,
    }

    /// <summary>
    /// Writes the save atomically (temp file, then replace with backup) and recovers from a
    /// corrupt primary file using the backup. Older versions are migrated in place.
    /// </summary>
    public sealed class SaveManager
    {
        private readonly string _path;
        private readonly ISaveSerializer _serializer;

        public string PrimaryPath => _path;
        public string BackupPath => _path + ".bak";
        public string TempPath => _path + ".tmp";

        public LoadSource LastLoadSource { get; private set; }
        public string LastError { get; private set; }

        public SaveManager(string path, ISaveSerializer serializer)
        {
            _path = path;
            _serializer = serializer;
        }

        public void Save(SaveData data)
        {
            data.Version = SaveData.CurrentVersion;
            string json = _serializer.Serialize(data);
            string dir = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            File.WriteAllText(TempPath, json);
            if (File.Exists(_path))
            {
                try
                {
                    File.Replace(TempPath, _path, BackupPath);
                    return;
                }
                catch (Exception)
                {
                    // Some platforms do not support File.Replace; fall back to copy + move.
                    File.Copy(_path, BackupPath, true);
                    File.Delete(_path);
                }
            }
            File.Move(TempPath, _path);
        }

        /// <summary>Returns the stored save, or null when there is none or nothing readable.</summary>
        public SaveData Load()
        {
            LastError = null;
            LastLoadSource = LoadSource.None;
            var data = TryRead(_path);
            if (data != null)
            {
                LastLoadSource = LoadSource.Primary;
                return data;
            }
            data = TryRead(BackupPath);
            if (data != null) LastLoadSource = LoadSource.Backup;
            return data;
        }

        public bool HasSave => File.Exists(_path) || File.Exists(BackupPath);

        public void Delete()
        {
            foreach (var p in new[] { _path, BackupPath, TempPath })
            {
                if (File.Exists(p)) File.Delete(p);
            }
        }

        private SaveData TryRead(string path)
        {
            if (!File.Exists(path)) return null;
            try
            {
                var data = _serializer.Deserialize(File.ReadAllText(path));
                if (data == null) throw new InvalidDataException("Empty save file.");
                if (data.Version > SaveData.CurrentVersion)
                {
                    // Written by a newer build: keep the file untouched and do not load it.
                    LastError = "Save version " + data.Version + " is newer than supported.";
                    File.Copy(path, path + ".v" + data.Version, true);
                    return null;
                }
                SaveMigrations.Migrate(data);
                return data;
            }
            catch (Exception e)
            {
                LastError = e.Message;
                return null;
            }
        }
    }

    public static class SaveMigrations
    {
        public static void Migrate(SaveData data)
        {
            if (data.Version < 1)
            {
                // v0 (pre-release) had no lane count or settings.
                if (data.LaneCount < 1) data.LaneCount = 1;
                if (data.Settings == null) data.Settings = new SettingsSave();
                data.Version = 1;
            }
            if (data.Version < 2)
            {
                // v1 stored no stats block.
                if (data.Stats == null) data.Stats = new StatsSave();
                data.Version = 2;
            }
            if (data.Booths == null) data.Booths = new List<BoothSave>();
            if (data.Workers == null) data.Workers = new List<WorkerSave>();
            if (data.UnlockedVehicles == null) data.UnlockedVehicles = new List<string>();
            if (data.Settings == null) data.Settings = new SettingsSave();
            if (data.Stats == null) data.Stats = new StatsSave();
        }
    }
}
