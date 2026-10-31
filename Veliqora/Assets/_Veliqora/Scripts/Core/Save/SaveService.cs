using System;
using System.Collections.Generic;
using System.IO;
using Veliqora.Core.Economy;

namespace Veliqora.Core.Save
{
    /// <summary>Abstract storage so the save logic is testable without a file system.</summary>
    public interface ISaveStorage
    {
        bool Exists(string name);
        string Read(string name);
        /// <summary>Atomically replaces <paramref name="name"/>; the previous content becomes <paramref name="backupName"/>.</summary>
        void WriteAtomic(string name, string backupName, string content);
        void Delete(string name);
    }

    /// <summary>File storage: write temp file, flush, then swap into place keeping the previous file as backup.</summary>
    public sealed class FileSaveStorage : ISaveStorage
    {
        private readonly string _dir;

        public FileSaveStorage(string directory)
        {
            _dir = directory ?? throw new ArgumentNullException(nameof(directory));
            Directory.CreateDirectory(_dir);
        }

        private string P(string name) => Path.Combine(_dir, name);

        public bool Exists(string name) => File.Exists(P(name));
        public string Read(string name) => File.ReadAllText(P(name));
        public void Delete(string name) { if (File.Exists(P(name))) File.Delete(P(name)); }

        public void WriteAtomic(string name, string backupName, string content)
        {
            string target = P(name), tmp = P(name + ".tmp"), bak = P(backupName);
            using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var w = new StreamWriter(fs, new System.Text.UTF8Encoding(false)))
            {
                w.Write(content);
                w.Flush();
                fs.Flush(true);
            }
            if (File.Exists(target))
            {
                try
                {
                    File.Replace(tmp, target, bak, true);
                    return;
                }
                catch (Exception e) when (e is PlatformNotSupportedException || e is IOException || e is UnauthorizedAccessException)
                {
                    // Fallback: keep a backup copy first so there's always a recoverable file on disk.
                    File.Copy(target, bak, true);
                    File.Delete(target);
                }
            }
            File.Move(tmp, target);
        }
    }

    /// <summary>In-memory storage for tests.</summary>
    public sealed class MemorySaveStorage : ISaveStorage
    {
        public readonly Dictionary<string, string> Files = new Dictionary<string, string>();
        public bool FailWrites;

        public bool Exists(string name) => Files.ContainsKey(name);
        public string Read(string name) => Files.TryGetValue(name, out var s) ? s : throw new FileNotFoundException(name);
        public void Delete(string name) => Files.Remove(name);

        public void WriteAtomic(string name, string backupName, string content)
        {
            if (FailWrites) throw new IOException("Simulated write failure");
            if (Files.TryGetValue(name, out var prev)) Files[backupName] = prev;
            Files[name] = content;
        }
    }

    public enum SaveLoadSource { Primary, Backup, New }

    public sealed class SaveLoadResult
    {
        public SaveData Data;
        public SaveLoadSource Source;
        public bool PrimaryWasCorrupt;
        /// <summary>User-facing message when recovery happened (null when everything was fine).</summary>
        public string UserMessage;
        public string TechnicalDetail;
    }

    /// <summary>Loads with backup recovery and saves atomically. Never throws on load; worst case returns a new save.</summary>
    public sealed class SaveService
    {
        public const string PrimaryName = "veliqora_save.json";
        public const string BackupName = "veliqora_save.bak.json";

        private readonly ISaveStorage _storage;
        private readonly Func<DateTime> _clock;

        public string LastError { get; private set; }

        public SaveService(ISaveStorage storage, Func<DateTime> clock = null)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _clock = clock ?? (() => DateTime.UtcNow);
        }

        public SaveLoadResult Load()
        {
            var result = new SaveLoadResult();
            string primaryError = null;

            if (_storage.Exists(PrimaryName))
            {
                if (TryRead(PrimaryName, out var data, out primaryError))
                {
                    result.Data = data;
                    result.Source = SaveLoadSource.Primary;
                    return result;
                }
                result.PrimaryWasCorrupt = true;
            }

            if (_storage.Exists(BackupName) && TryRead(BackupName, out var backup, out var backupError))
            {
                result.Data = backup;
                result.Source = SaveLoadSource.Backup;
                result.UserMessage = "We restored your progress from a backup.";
                result.TechnicalDetail = primaryError;
                return result;
            }

            result.Data = CreateNew();
            result.Source = SaveLoadSource.New;
            if (result.PrimaryWasCorrupt)
            {
                result.UserMessage = "Your save data couldn't be read, so a fresh game was started.";
                result.TechnicalDetail = primaryError;
            }
            return result;
        }

        public SaveData CreateNew()
        {
            string now = _clock().ToString("o");
            return new SaveData { CreatedUtc = now, UpdatedUtc = now };
        }

        public bool Save(SaveData data)
        {
            try
            {
                data.UpdatedUtc = _clock().ToString("o");
                _storage.WriteAtomic(PrimaryName, BackupName, SaveSerializer.Serialize(data));
                LastError = null;
                return true;
            }
            catch (Exception e)
            {
                LastError = e.Message;
                return false;
            }
        }

        public void DeleteAll()
        {
            _storage.Delete(PrimaryName);
            _storage.Delete(BackupName);
        }

        private bool TryRead(string name, out SaveData data, out string error)
        {
            data = null;
            error = null;
            try
            {
                data = SaveSerializer.Deserialize(_storage.Read(name));
                return true;
            }
            catch (SaveFormatException e) { error = e.Message; }
            catch (IOException e) { error = e.Message; }
            catch (UnauthorizedAccessException e) { error = e.Message; }
            return false;
        }
    }

    /// <summary>Applies progress rules after a level is completed (unlock next, best stars).</summary>
    public static class ProgressRules
    {
        public static void RecordCompletion(ProgressData p, int level, int stars, int levelCount)
        {
            if (stars > p.Stars(level)) p.BestStars[level] = stars;
            int next = Math.Min(levelCount, level + 1);
            if (next > p.HighestUnlocked) p.HighestUnlocked = next;
            p.CurrentLevel = Math.Min(levelCount, Math.Max(p.CurrentLevel, level + 1));
        }

        public static bool IsUnlocked(ProgressData p, int level) => level >= 1 && level <= p.HighestUnlocked;
    }
}
