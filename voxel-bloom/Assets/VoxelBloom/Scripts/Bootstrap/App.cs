using UnityEngine;
using UnityEngine.EventSystems;
using VoxelBloom.Core;

namespace VoxelBloom
{
    /// <summary>
    /// Entry point. Boots automatically after the first scene loads (no scene wiring required):
    /// services -> audio -> event system -> router -> Home screen.
    /// </summary>
    public sealed class App : MonoBehaviour
    {
        public static App Instance { get; private set; }
        public static readonly GameStateMachine State = new GameStateMachine();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Instance != null) return;
            var go = new GameObject("VoxelBloomApp");
            DontDestroyOnLoad(go);
            go.AddComponent<App>();
        }

        void Awake()
        {
            Instance = this;
            Screen.orientation = ScreenOrientation.Portrait;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Input.multiTouchEnabled = true;
            Services.Initialize();
            Services.ApplyQuality(Services.Save.Settings.Quality);
            Services.Audio = gameObject.AddComponent<AudioService>();

            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                DontDestroyOnLoad(es);
            }
            // a camera must exist so Unity does not warn "No cameras rendering"; UI is overlay
            if (Camera.main == null)
            {
                var cam = new GameObject("MainCamera", typeof(Camera)).GetComponent<Camera>();
                cam.tag = "MainCamera";
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Theme.SkyBottom;
                cam.cullingMask = 0;
                DontDestroyOnLoad(cam.gameObject);
            }
            Router.Create();
            State.TryTransition(GameState.Home);
            Router.Instance.Show<TitleScreen>();
        }

        void OnApplicationPause(bool paused)
        {
            if (!paused) return;
            GameplayController.Active?.SaveProgress();
            ConveyorController.Active?.SaveProgress();
            Services.Commit();
        }

        void OnApplicationQuit()
        {
            GameplayController.Active?.SaveProgress();
            ConveyorController.Active?.SaveProgress();
            Services.Commit();
        }
    }
}
