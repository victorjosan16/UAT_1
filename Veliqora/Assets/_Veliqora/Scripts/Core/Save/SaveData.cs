using System.Collections.Generic;
using Veliqora.Core.Economy;
using Veliqora.Core.Session;

namespace Veliqora.Core.Save
{
    public sealed class ProgressData
    {
        public int HighestUnlocked = 1;
        public int CurrentLevel = 1;
        public Dictionary<int, int> BestStars = new Dictionary<int, int>();
        public HashSet<int> ClaimedChests = new HashSet<int>();

        public int Stars(int level) => BestStars.TryGetValue(level, out int s) ? s : 0;
        public bool IsCompleted(int level) => Stars(level) > 0;
        public int TotalStars
        {
            get
            {
                int t = 0;
                foreach (var kv in BestStars) t += kv.Value;
                return t;
            }
        }
    }

    public sealed class SettingsData
    {
        public bool Music = true;
        public bool Sound = true;
        public float MusicVolume = 0.6f;
        public float SoundVolume = 0.9f;
        public bool Haptics = true;
        public bool ReducedMotion;
        public bool ColorblindSymbols;
        /// <summary>Animation speed multiplier: 0.75 (relaxed), 1 (normal), 1.5 (fast).</summary>
        public float AnimationSpeed = 1f;
        public bool LightGameplayBackground;
        /// <summary>Mock ads only: simulate offline/no-fill for testing failure paths.</summary>
        public bool DebugSimulateAdFailure;
    }

    public sealed class CosmeticsSelection
    {
        public string Theme = "dark_aurora";
        public string Bottle = "bottle_classic";
    }

    /// <summary>Root of the local save file (see SaveSerializer for the on-disk envelope).</summary>
    public sealed class SaveData
    {
        public const int CurrentSchemaVersion = 2;

        public int SchemaVersion = CurrentSchemaVersion;
        public int ContentVersion = 1;
        public string CreatedUtc = "";
        public string UpdatedUtc = "";
        public ProgressData Progress = new ProgressData();
        public EconomyState Economy = new EconomyState();
        public SettingsData Settings = new SettingsData();
        public CosmeticsSelection Cosmetics = new CosmeticsSelection();
        public HashSet<string> TutorialsSeen = new HashSet<string>();
        public SessionSnapshot Session;   // in-progress puzzle, may be null
    }
}
