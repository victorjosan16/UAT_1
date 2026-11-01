using System;
using System.IO;
using UnityEngine;
using VoxelBloom.Core;

namespace VoxelBloom
{
    /// <summary>
    /// Composition root holding the long-lived services. Created once by App; modules talk through
    /// these services and C# events instead of a monolithic GameManager.
    /// </summary>
    public static class Services
    {
        public static DataRepository Data { get; private set; }
        public static GameConfig Config => Data.Config;
        public static SaveManager SaveManager { get; private set; }
        public static SaveData Save => SaveManager.Data;
        public static Wallet Wallet { get; private set; }
        public static Lives Lives { get; private set; }
        public static BoosterInventory Boosters { get; private set; }
        public static DailyRewards Daily { get; private set; }
        public static Progression Progress { get; private set; }
        public static ConveyorEvents Events { get; private set; }
        public static IRewardedAdService Ads { get; private set; }
        public static AudioService Audio { get; set; }
        public static bool Initialized { get; private set; }

        public static event Action SettingsChanged;

        public static long Now => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        public static void Initialize()
        {
            if (Initialized) return;
            Data = new DataRepository();
            Data.LoadConfig();

            string dir = Path.Combine(Application.persistentDataPath, "save");
            SaveManager = new SaveManager(new FileSaveStorage(dir), Config);
            var s = SaveManager.Load();
            if (SaveManager.LoadedFrom == LoadSource.Backup) Debug.LogWarning("[VoxelBloom] Main save was corrupt; restored backup. " + SaveManager.LastError);

            Wallet = new Wallet(s.Coins);
            Wallet.Changed += c => { Save.Coins = c; };
            Lives = new Lives(s.Lives, Config.Economy.MaxLives, Config.Economy.LifeRegenMinutes, s.NextLifeAt);
            Lives.Update(Now);
            Boosters = new BoosterInventory();
            foreach (var kv in s.Boosters) Boosters.Set(kv.Key, kv.Value);
            Boosters.Changed += (t, n) => { Save.Boosters[t] = n; };
            Daily = new DailyRewards(Config.Economy.DailyRewards, s.DailyLastClaimDay, s.DailyStreak);
            Progress = new Progression(Config, s, Wallet);
            Events = new ConveyorEvents(s, Wallet, Boosters);
            Events.Refresh(Now);
            Ads = AdServiceFactory.Create();
            Initialized = true;
            Commit();
        }

        /// <summary>Copies live service state into SaveData and writes it atomically.</summary>
        public static void Commit()
        {
            if (!Initialized) return;
            var s = Save;
            Lives.Update(Now);
            s.Coins = Wallet.Coins;
            s.Lives = Lives.Count;
            s.NextLifeAt = Lives.NextLifeAt;
            s.DailyLastClaimDay = Daily.LastClaimDay;
            s.DailyStreak = Daily.Streak;
            foreach (var kv in Boosters.All) s.Boosters[kv.Key] = kv.Value;
            SaveManager.Save();
            if (SaveManager.LastError != null) Debug.LogWarning("[VoxelBloom] Save failed: " + SaveManager.LastError);
        }

        public static void NotifySettingsChanged()
        {
            ApplyQuality(Save.Settings.Quality);
            SettingsChanged?.Invoke();
            Commit();
        }

        public static void ApplyQuality(int preset)
        {
            Application.targetFrameRate = preset == 0 ? 30 : 60;
            QualitySettings.vSyncCount = 0;
            int levels = QualitySettings.names.Length;
            if (levels > 0) QualitySettings.SetQualityLevel(Mathf.Clamp(Mathf.RoundToInt(preset / 2f * (levels - 1)), 0, levels - 1), true);
        }

        public static bool ReducedEffects => Initialized && (Save.Settings.ReducedEffects || Save.Settings.Quality == 0);
    }
}
