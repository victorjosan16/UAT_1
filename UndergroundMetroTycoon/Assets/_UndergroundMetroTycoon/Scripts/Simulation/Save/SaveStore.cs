using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace UndergroundMetroTycoon.Simulation
{
    public enum SaveLoadStatus
    {
        NoSave,
        Loaded,
        LoadedFromBackup,
        Corrupted
    }

    public readonly struct SaveLoadResult
    {
        public readonly SaveLoadStatus Status;
        public readonly SaveData Data;
        public readonly string Message;

        public SaveLoadResult(SaveLoadStatus status, SaveData data, string message)
        {
            Status = status;
            Data = data;
            Message = message;
        }
    }

    /// <summary>
    /// Local save file with atomic replacement, a rolling backup of the previous save and checksum validation.
    /// </summary>
    public sealed class SaveStore
    {
        private readonly ISaveSerializer _serializer;

        public SaveStore(string directory, string fileName, ISaveSerializer serializer)
        {
            if (string.IsNullOrEmpty(directory)) throw new ArgumentException("Save directory required.", nameof(directory));
            if (string.IsNullOrEmpty(fileName)) throw new ArgumentException("Save file name required.", nameof(fileName));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            Directory = directory;
            MainPath = Path.Combine(directory, fileName);
            BackupPath = MainPath + ".bak";
            TempPath = MainPath + ".tmp";
        }

        public string Directory { get; }
        public string MainPath { get; }
        public string BackupPath { get; }
        public string TempPath { get; }

        public bool Save(SaveData data, out string error)
        {
            error = null;
            if (data == null)
            {
                error = "Nothing to save.";
                return false;
            }
            try
            {
                System.IO.Directory.CreateDirectory(Directory);
                string payload = _serializer.Serialize(data);
                var envelope = new SaveEnvelope { DataVersion = data.Version, Checksum = ComputeChecksum(payload), Payload = payload };
                WriteDurable(TempPath, _serializer.Serialize(envelope));

                if (File.Exists(MainPath))
                {
                    try
                    {
                        File.Replace(TempPath, MainPath, BackupPath, true);
                    }
                    catch (Exception e) when (e is PlatformNotSupportedException || e is IOException || e is UnauthorizedAccessException)
                    {
                        File.Copy(MainPath, BackupPath, true);
                        File.Delete(MainPath);
                        File.Move(TempPath, MainPath);
                    }
                }
                else
                {
                    File.Move(TempPath, MainPath);
                }
                return true;
            }
            catch (Exception e)
            {
                error = e.Message;
                return false;
            }
        }

        public SaveLoadResult Load()
        {
            bool anyFile = File.Exists(MainPath) || File.Exists(BackupPath) || File.Exists(TempPath);
            if (!anyFile) return new SaveLoadResult(SaveLoadStatus.NoSave, null, "No save file found.");

            if (TryRead(MainPath, out SaveData data, out string mainError))
                return new SaveLoadResult(SaveLoadStatus.Loaded, data, null);

            if (File.Exists(MainPath)) PreserveCorruptFile();

            if (TryRead(BackupPath, out data, out string backupError) || TryRead(TempPath, out data, out backupError))
                return new SaveLoadResult(SaveLoadStatus.LoadedFromBackup, data, $"Main save unreadable ({mainError}); restored backup.");

            return new SaveLoadResult(SaveLoadStatus.Corrupted, null, $"Save unreadable ({mainError}; backup: {backupError}).");
        }

        public void DeleteAll()
        {
            foreach (string path in new[] { MainPath, BackupPath, TempPath })
            {
                try
                {
                    if (File.Exists(path)) File.Delete(path);
                }
                catch (IOException)
                {
                    // Best effort; a stale file is overwritten by the next save anyway.
                }
            }
        }

        public static string ComputeChecksum(string payload)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(payload ?? string.Empty));
                var sb = new StringBuilder(hash.Length * 2);
                foreach (byte b in hash) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        private bool TryRead(string path, out SaveData data, out string error)
        {
            data = null;
            error = null;
            try
            {
                if (!File.Exists(path))
                {
                    error = "missing";
                    return false;
                }
                SaveEnvelope envelope = _serializer.Deserialize<SaveEnvelope>(File.ReadAllText(path, Encoding.UTF8));
                if (envelope == null || string.IsNullOrEmpty(envelope.Payload))
                {
                    error = "empty envelope";
                    return false;
                }
                if (!string.Equals(envelope.Checksum, ComputeChecksum(envelope.Payload), StringComparison.Ordinal))
                {
                    error = "checksum mismatch";
                    return false;
                }
                data = _serializer.Deserialize<SaveData>(envelope.Payload);
                return SaveMigrator.TryMigrate(data, out error);
            }
            catch (Exception e)
            {
                data = null;
                error = e.GetType().Name + ": " + e.Message;
                return false;
            }
        }

        private void PreserveCorruptFile()
        {
            try
            {
                File.Copy(MainPath, MainPath + ".corrupt", true);
            }
            catch (Exception)
            {
                // Diagnostics only.
            }
        }

        private static void WriteDurable(string path, string text)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }
        }
    }
}
