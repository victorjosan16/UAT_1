using UnityEngine;

namespace Veliqora.Data
{
    /// <summary>
    /// Optional designer-facing content override. If an asset exists at Resources/Veliqora/ContentConfig, any
    /// TextAsset assigned here replaces the default JSON file (levels / economy / progression). Empty fields fall
    /// back to the JSON in Resources, so the game works without this asset. The JSON stays the single source of
    /// truth for the pure-C# core and the offline tools (LevelGen, tests).
    /// </summary>
    [CreateAssetMenu(menuName = "Veliqora/Content Config", fileName = "ContentConfig")]
    public sealed class VeliqoraContentConfig : ScriptableObject
    {
        [Tooltip("levels.json produced by Tools/LevelGen (empty = Resources/Veliqora/Levels/levels)")]
        public TextAsset Levels;
        [Tooltip("economy.json: boosters, offers, cosmetics/themes (empty = Resources/Veliqora/Config/economy)")]
        public TextAsset Economy;
        [Tooltip("progression.json: daily rewards and achievements (empty = Resources/Veliqora/Config/progression)")]
        public TextAsset Progression;

        public const string ResourcePath = "Veliqora/ContentConfig";

        private static VeliqoraContentConfig _cached;
        private static bool _looked;

        public static VeliqoraContentConfig Active
        {
            get
            {
                if (!_looked) { _cached = Resources.Load<VeliqoraContentConfig>(ResourcePath); _looked = true; }
                return _cached;
            }
        }

        /// <summary>Override from the config asset when assigned, otherwise the default Resources file.</summary>
        public static TextAsset Load(string defaultResourcePath, System.Func<VeliqoraContentConfig, TextAsset> pick)
        {
            var cfg = Active;
            var overridden = cfg != null ? pick(cfg) : null;
            return overridden != null ? overridden : Resources.Load<TextAsset>(defaultResourcePath);
        }
    }
}
