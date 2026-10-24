using System;
using UnityEngine;

namespace LastBlock.Core
{
    /// <summary>
    /// Static description of one level. Values are hand-tuned (not random) so every level is solvable.
    /// </summary>
    [Serializable]
    public struct LevelConfig
    {
        public int levelNumber;
        public int layers;
        public int blocksToRemove;
        /// <summary>How many tower blocks may fall before the tower counts as collapsed.</summary>
        public int allowedFallenBlocks;
        public float staticFriction;
        public float dynamicFriction;
        /// <summary>Layers whose middle block is missing from the start (makes those layers unusable).</summary>
        public int[] preRemovedMiddleLayers;

        public bool IsMiddlePreRemoved(int layer)
        {
            if (preRemovedMiddleLayers == null) return false;
            return Array.IndexOf(preRemovedMiddleLayers, layer) >= 0;
        }

        /// <summary>
        /// Conservative count of blocks that can be removed safely:
        /// one middle block from every intact, non-top layer.
        /// </summary>
        public int SafeRemovableBlocks
        {
            get
            {
                int count = 0;
                for (int layer = 0; layer < layers - 1; layer++)
                {
                    if (!IsMiddlePreRemoved(layer)) count++;
                }
                return count;
            }
        }
    }

    /// <summary>
    /// Holds the 10 level definitions and the locally saved progress (PlayerPrefs).
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        public const int TotalLevels = 10;
        public const string HighestUnlockedKey = "LastBlock.HighestUnlockedLevel";

        public int CurrentLevel { get; private set; } = 1;

        public int HighestUnlockedLevel => Mathf.Clamp(PlayerPrefs.GetInt(HighestUnlockedKey, 1), 1, TotalLevels);

        public bool IsLastLevel(int level) => level >= TotalLevels;

        public LevelConfig SetCurrentLevel(int level)
        {
            CurrentLevel = Mathf.Clamp(level, 1, TotalLevels);
            return GetLevelConfig(CurrentLevel);
        }

        /// <summary>Unlocks the level after <paramref name="completedLevel"/> and saves it.</summary>
        public void MarkLevelCompleted(int completedLevel)
        {
            int next = Mathf.Clamp(completedLevel + 1, 1, TotalLevels);
            if (next > HighestUnlockedLevel)
            {
                PlayerPrefs.SetInt(HighestUnlockedKey, next);
                PlayerPrefs.Save();
            }
        }

        [ContextMenu("Reset Progress")]
        public void ResetProgress()
        {
            PlayerPrefs.DeleteKey(HighestUnlockedKey);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Level table. Layers follow the design (level + 2). Difficulty grows through more blocks to remove,
        /// lower friction, zero tolerance for fallen blocks and some pre-removed middle blocks.
        /// </summary>
        public static LevelConfig GetLevelConfig(int level)
        {
            level = Mathf.Clamp(level, 1, TotalLevels);
            switch (level)
            {
                case 1: return Make(1, 2, 1, 0.70f, 0.55f);
                case 2: return Make(2, 2, 1, 0.70f, 0.55f);
                case 3: return Make(3, 3, 1, 0.65f, 0.50f);
                case 4: return Make(4, 3, 1, 0.65f, 0.50f);
                case 5: return Make(5, 4, 0, 0.60f, 0.47f);
                case 6: return Make(6, 4, 0, 0.60f, 0.47f, 3);
                case 7: return Make(7, 5, 0, 0.55f, 0.45f, 2);
                case 8: return Make(8, 5, 0, 0.55f, 0.43f, 2, 6);
                case 9: return Make(9, 6, 0, 0.50f, 0.42f, 3, 7);
                default: return Make(10, 7, 0, 0.50f, 0.40f, 2, 5, 8);
            }
        }

        static LevelConfig Make(int level, int toRemove, int allowedFallen, float staticFriction,
            float dynamicFriction, params int[] preRemovedMiddles)
        {
            return new LevelConfig
            {
                levelNumber = level,
                layers = level + 2,
                blocksToRemove = toRemove,
                allowedFallenBlocks = allowedFallen,
                staticFriction = staticFriction,
                dynamicFriction = dynamicFriction,
                preRemovedMiddleLayers = preRemovedMiddles ?? Array.Empty<int>()
            };
        }
    }
}
