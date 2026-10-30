using TollRoadTycoon.Core;
using UnityEngine;

namespace TollRoadTycoon.Presentation
{
    /// <summary>
    /// ScriptableObject wrapper so designers can tune every value in the Inspector.
    /// Place an instance at Resources/GameConfig to override the built-in defaults.
    /// </summary>
    [CreateAssetMenu(menuName = "Toll Road Tycoon/Game Config", fileName = "GameConfig")]
    public sealed class GameConfigAsset : ScriptableObject
    {
        public GameConfig Config = GameConfig.CreateDefault();

        public static GameConfig LoadOrDefault()
        {
            var asset = Resources.Load<GameConfigAsset>("GameConfig");
            if (asset != null && asset.Config != null && asset.Config.VehicleTypes.Count > 0) return asset.Config;
            return GameConfig.CreateDefault();
        }
    }
}
