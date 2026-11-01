using System.Collections.Generic;
using UnityEngine;
using VoxelBloom.Core;

namespace VoxelBloom
{
    /// <summary>Loads JSON data shipped in Resources/VoxelBloom and caches parsed objects.</summary>
    public sealed class DataRepository
    {
        public const string Root = "VoxelBloom/";
        readonly Dictionary<int, LevelDefinition> levels = new Dictionary<int, LevelDefinition>();
        readonly Dictionary<string, VoxelGrid> models = new Dictionary<string, VoxelGrid>();
        readonly Dictionary<string, AnimProfile> anims = new Dictionary<string, AnimProfile>();
        public GameConfig Config { get; private set; }

        public void LoadConfig()
        {
            var ta = Resources.Load<TextAsset>(Root + "Config/game_config");
            if (ta == null)
            {
                Debug.LogError("[VoxelBloom] game_config.json missing - run `node tools/build-data.mjs`.");
                Config = new GameConfig();
                return;
            }
            Config = GameConfig.FromJson(ta.text);
        }

        public LevelDefinition GetLevel(int id)
        {
            if (levels.TryGetValue(id, out var l)) return l;
            var ta = Resources.Load<TextAsset>(Root + "Levels/level_" + id.ToString("000"));
            if (ta == null) { Debug.LogError("[VoxelBloom] Level " + id + " missing"); return null; }
            try
            {
                l = LevelDefinition.FromJson(ta.text);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[VoxelBloom] Level " + id + " invalid: " + e.Message);
                return null;
            }
            finally { Resources.UnloadAsset(ta); }
            levels[id] = l;
            return l;
        }

        public VoxelGrid GetModel(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (models.TryGetValue(id, out var m)) return m;
            var ta = Resources.Load<TextAsset>(Root + "Models/" + id);
            if (ta == null) { Debug.LogWarning("[VoxelBloom] Model " + id + " missing"); return null; }
            var d = MiniJson.Obj(MiniJson.Parse(ta.text));
            m = VoxelGrid.FromJson(d);
            anims[id] = AnimProfile.FromJson(MiniJson.GetObj(d, "anim"));
            Resources.UnloadAsset(ta);
            models[id] = m;
            return m;
        }

        /// <summary>Idle animation profile of a model (null when the model has none).</summary>
        public AnimProfile GetAnim(string id)
        {
            if (GetModel(id) == null) return null;
            return anims.TryGetValue(id, out var a) ? a : null;
        }
    }
}
