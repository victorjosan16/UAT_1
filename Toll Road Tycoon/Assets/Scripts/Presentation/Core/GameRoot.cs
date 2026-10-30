using TollRoadTycoon.Core;
using TollRoadTycoon.Localization;
using TollRoadTycoon.Presentation.UI;
using TollRoadTycoon.Save;
using TollRoadTycoon.TollSystem;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TollRoadTycoon.Presentation
{
    public enum GameScreen
    {
        MainMenu,
        Gameplay,
    }

    /// <summary>
    /// Composition root on the Unity side: creates the simulation, world views, camera, UI and audio,
    /// drives the fixed-step simulation and handles saving on pause/quit.
    /// </summary>
    public sealed class GameRoot : MonoBehaviour
    {
        private const float AutosaveSeconds = 30f;

        private SaveService _saves;
        private float _autosaveTimer;
        private WorldView _world;
        private VehicleViewManager _vehicles;
        private CoinPopups _popups;
        private UIManager _ui;

        public static GameRoot Instance { get; private set; }

        public GameConfig Config { get; private set; }
        public GameSimulation Sim { get; private set; }
        public SettingsSave Settings { get; private set; } = new SettingsSave();
        public AudioManager Audio { get; private set; }
        public CameraController CameraController { get; private set; }
        public WorldView World => _world;
        public GameScreen Screen { get; private set; } = GameScreen.MainMenu;
        public bool Paused { get; set; }
        public TollBooth SelectedBooth { get; private set; }

        public event System.Action SimulationReplaced;
        public event System.Action SelectionChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Application.targetFrameRate = 60;
            UnityEngine.Screen.sleepTimeout = SleepTimeout.NeverSleep;

            Config = GameConfigAsset.LoadOrDefault();
            _saves = new SaveService();
            Audio = gameObject.AddComponent<AudioManager>();

            CreateCamera();
            CreateEventSystem();

            var worldGo = new GameObject("World");
            worldGo.transform.SetParent(transform, false);
            _world = worldGo.AddComponent<WorldView>();
            _vehicles = new GameObject("Vehicles").AddComponent<VehicleViewManager>();
            _vehicles.transform.SetParent(worldGo.transform, false);
            _popups = new GameObject("CoinPopups").AddComponent<CoinPopups>();
            _popups.transform.SetParent(worldGo.transform, false);

            LoadOrCreate();

            _ui = new GameObject("UI").AddComponent<UIManager>();
            _ui.transform.SetParent(transform, false);
            _ui.Initialize(this);
            if (_saves.LoadedFromBackup) _ui.Toast(Loc.T("toast.loaded_backup"));
            CameraController.Tapped += OnWorldTapped;
            ShowMainMenu();
        }

        private void CreateCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
            }
            cam.transform.SetParent(transform, false);
            cam.transform.position = new Vector3(-0.5f, -6f, -10f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Palette.Grass;
            CameraController = cam.gameObject.GetComponent<CameraController>();
            if (CameraController == null) CameraController = cam.gameObject.AddComponent<CameraController>();
        }

        private static void CreateEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
            DontDestroyOnLoad(go);
        }

        private void LoadOrCreate()
        {
            var data = _saves.Load();
            Settings = data?.Settings ?? new SettingsSave();
            var sim = data != null ? GameSimulation.FromSave(Config, data, System.Environment.TickCount) : new GameSimulation(Config, System.Environment.TickCount);
            ReplaceSimulation(sim);
            ApplySettings();
        }

        private void ReplaceSimulation(GameSimulation sim)
        {
            if (Sim != null) Sim.Payments.TollCollected -= OnTollCollected;
            Sim = sim;
            Sim.Payments.TollCollected += OnTollCollected;
            Sim.BoothController.BarrierOpened += _ => Audio.Play(Sfx.Barrier);
            _world.Bind(sim);
            _vehicles.Bind(sim);
            _popups.Bind(sim, _world);
            SelectBooth(sim.Booths[0]);
            SimulationReplaced?.Invoke();
        }

        private void OnTollCollected(TollBooth booth, Traffic.Vehicle vehicle, long cents)
        {
            if (cents > 0) Audio.Play(Sfx.Coin);
        }

        public void ApplySettings()
        {
            Loc.Language = Settings.Language;
            Audio.SfxEnabled = Settings.SfxEnabled;
            Audio.SfxVolume = Settings.SfxVolume;
            int levels = QualitySettings.names.Length;
            if (levels > 0)
            {
                // Map Low/Medium/High onto the project's quality levels.
                int index = Mathf.RoundToInt(Mathf.Clamp(Settings.GraphicsQuality, 0, 2) / 2f * (levels - 1));
                QualitySettings.SetQualityLevel(index, true);
            }
        }

        private void Update()
        {
            if (Sim == null) return;
            if (!Paused) Sim.Tick(Time.deltaTime);

            _autosaveTimer += Time.unscaledDeltaTime;
            if (_autosaveTimer >= AutosaveSeconds)
            {
                _autosaveTimer = 0;
                SaveNow();
            }
            UpdateCameraBounds();
        }

        /// <summary>Lets the camera pan sideways only as far as the road is wider than the view.</summary>
        private void UpdateCameraBounds()
        {
            var cam = CameraController.Camera;
            float halfWidth = cam.orthographicSize * cam.aspect;
            float center = (_world.MinX + _world.MaxX) * 0.5f;
            float min = Mathf.Min(_world.MinX + halfWidth, center);
            float max = Mathf.Max(_world.MaxX - halfWidth, center);
            if (!Mathf.Approximately(min, CameraController.MinX) || !Mathf.Approximately(max, CameraController.MaxX))
            {
                CameraController.SetHorizontalBounds(min, max);
            }
        }

        public bool SaveNow() => _saves.Save(Sim, Settings);

        private void OnApplicationPause(bool paused)
        {
            if (paused && Sim != null) SaveNow();
        }

        private void OnApplicationQuit()
        {
            if (Sim != null) SaveNow();
        }

        public void ShowMainMenu()
        {
            Screen = GameScreen.MainMenu;
            Paused = false;
            CameraController.enabled = false;
            CameraController.transform.position = new Vector3(-0.5f, -6f, -10f);
            CameraController.Camera.orthographicSize = 11f;
            _ui.ShowMainMenu();
        }

        public void StartGameplay()
        {
            Screen = GameScreen.Gameplay;
            Paused = false;
            CameraController.enabled = true;
            CameraController.Camera.orthographicSize = 10.5f;
            CameraController.Focus(new Vector2(0.4f, -3f));
            _ui.ShowGameplay();
        }

        public void ExitToMenu()
        {
            SaveNow();
            ShowMainMenu();
        }

        public void ResetProgress()
        {
            _saves.DeleteAll();
            var settings = Settings;
            ReplaceSimulation(new GameSimulation(Config, System.Environment.TickCount));
            Settings = settings;
            SaveNow();
            ShowMainMenu();
        }

        public void SelectBooth(TollBooth booth)
        {
            SelectedBooth = booth;
            foreach (var view in _world.Booths) view.SetSelected(view.Booth == booth);
            SelectionChanged?.Invoke();
        }

        private void OnWorldTapped(Vector2 world)
        {
            if (Screen != GameScreen.Gameplay || Paused) return;
            var view = _world.PickBooth(world);
            if (view != null)
            {
                Audio.Play(Sfx.Click);
                SelectBooth(view.Booth);
                _ui.ShowBoothUpgrade(view.Booth);
                return;
            }
            if (_world.NextSlotRect.HasValue && _world.NextSlotRect.Value.Contains(world))
            {
                Audio.Play(Sfx.Click);
                _ui.ConfirmBuildBooth();
            }
        }

        private void LateUpdate()
        {
            // Booth views are rebuilt with the road; keep the highlight on the selected booth.
            if (SelectedBooth != null)
            {
                foreach (var view in _world.Booths) view.SetSelected(view.Booth == SelectedBooth);
            }
        }
    }
}
