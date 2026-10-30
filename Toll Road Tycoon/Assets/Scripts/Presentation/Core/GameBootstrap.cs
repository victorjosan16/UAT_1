using UnityEngine;

namespace TollRoadTycoon.Presentation
{
    /// <summary>
    /// Boot step: runs after the first scene loads and creates the single GameRoot, so the game
    /// starts from the Boot scene (or any scene) without hand-wired prefabs.
    /// </summary>
    public static class GameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (GameRoot.Instance != null) return;
            var go = new GameObject("TollRoadTycoon");
            go.AddComponent<GameRoot>();
        }
    }
}
