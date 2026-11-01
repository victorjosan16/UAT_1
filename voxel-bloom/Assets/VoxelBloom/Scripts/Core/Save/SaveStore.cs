using System;
using System.IO;
using System.Text;

namespace VoxelBloom.Core
{
    public interface ISaveStorage
    {
        string ReadMain();
        string ReadBackup();
        /// <summary>Atomically replaces the main save; the previous main becomes the backup.</summary>
        void WriteAtomic(string content);
    }

    /// <summary>File storage: write temp -> replace main (old main kept as .bak). Works with any directory.</summary>
    public sealed class FileSaveStorage : ISaveStorage
    {
        readonly string main, backup, temp;

        public FileSaveStorage(string directory, string fileName = "voxelbloom_save.json")
        {
            Directory.CreateDirectory(directory);
            main = Path.Combine(directory, fileName);
            backup = main + ".bak";
            temp = main + ".tmp";
        }

        public string ReadMain() => File.Exists(main) ? File.ReadAllText(main, Encoding.UTF8) : null;
        public string ReadBackup() => File.Exists(backup) ? File.ReadAllText(backup, Encoding.UTF8) : null;

        public void WriteAtomic(string content)
        {
            File.WriteAllText(temp, content, Encoding.UTF8);
            if (File.Exists(main))
            {
                try { File.Replace(temp, main, backup, true); return; }
                catch (PlatformNotSupportedException) { }
                catch (IOException) { }
                File.Copy(main, backup, true);
                File.Delete(main);
            }
            File.Move(temp, main);
        }
    }

    /// <summary>In-memory storage for tests.</summary>
    public sealed class MemorySaveStorage : ISaveStorage
    {
        public string Main, Backup;
        public string ReadMain() => Main;
        public string ReadBackup() => Backup;
        public void WriteAtomic(string content) { Backup = Main; Main = content; }
    }

    public enum LoadSource { Main, Backup, New }

    /// <summary>Loads with corruption recovery (main -> backup -> fresh) and saves atomically.</summary>
    public sealed class SaveManager
    {
        readonly ISaveStorage storage;
        readonly GameConfig config;
        public SaveData Data { get; private set; }
        public LoadSource LoadedFrom { get; private set; }
        public string LastError { get; private set; }

        public SaveManager(ISaveStorage storage, GameConfig config)
        {
            this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
            this.config = config;
        }

        public SaveData Load()
        {
            if (TryParse(Safe(storage.ReadMain), out var d)) { LoadedFrom = LoadSource.Main; return Data = d; }
            if (TryParse(Safe(storage.ReadBackup), out d)) { LoadedFrom = LoadSource.Backup; return Data = d; }
            LoadedFrom = LoadSource.New;
            return Data = SaveData.CreateNew(config);
        }

        public void Save()
        {
            if (Data == null) return;
            try { storage.WriteAtomic(SaveSerializer.ToJson(Data)); LastError = null; }
            catch (Exception e) { LastError = e.Message; }
        }

        string Safe(Func<string> read)
        {
            try { return read(); } catch (Exception e) { LastError = e.Message; return null; }
        }

        bool TryParse(string json, out SaveData data)
        {
            data = null;
            if (string.IsNullOrWhiteSpace(json)) return false;
            try { data = SaveSerializer.FromJson(json, config); return true; }
            catch (Exception e) { LastError = e.Message; return false; }
        }
    }
}
