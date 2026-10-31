using System;
using TollRoadTycoon.Core;
using TollRoadTycoon.Economy;
using TollRoadTycoon.Localization;
using TollRoadTycoon.Monetization;
using TollRoadTycoon.Presentation.UI;
using TollRoadTycoon.Save;
using TollRoadTycoon.Services;
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

    public enum PlacementKind
    {
        None,
        BuildService,
        MoveService,
        BuildDecoration,
        MoveDecoration,
    }

    /// <summary>
    /// Composition root on the Unity side: creates the game, world views, camera, UI and audio,
    /// drives the simulation, handles placement taps and saving on pause/quit.
    /// </summary>
    public sealed class GameRoot : MonoBehaviour
    {
        private const float AutosaveSeconds = 30f;
        private const float SlotPickRadius = 2.4f;

        private SaveService _saves;
        private float _autosaveTimer;
        private float _missionTimer;
        private WorldView _world;
        private VehicleViewManager _vehicles;
        private CoinPopups _popups;
        private BuildingsView _buildings;
        private UIManager _ui;
        private IAdService _adService;

        public static GameRoot Instance { get; private set; }

        public GameConfig Config { get; private set; }
        public TycoonGame Game { get; private set; }
        /// <summary>The road currently simulated (active section).</summary>
        public GameSimulation Sim => Game.Active;
        public SettingsSave Settings { get; private set; } = new SettingsSave();
        public AudioManager Audio { get; private set; }
        public CameraController CameraController { get; private set; }
        public WorldView World => _world;
        public GameScreen Screen { get; private set; } = GameScreen.MainMenu;
        public bool Paused { get; set; }
        public TollBooth SelectedBooth { get; private set; }

        public PlacementKind Placement { get; private set; }
        public string PlacementId { get; private set; }
        public int PlacementFromSlot { get; private set; } = -1;

        /// <summary>Raised when the simulated road changes (new game, load, section switch).</summary>
        public event Action SimulationReplaced;
        public event Action SelectionChanged;
        public event Action PlacementChanged;

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
            _adService = CreateAdService();

            CreateCamera();
            CreateEventSystem();

            var worldGo = new GameObject("World");
            worldGo.transform.SetParent(transform, false);
            _world = worldGo.AddComponent<WorldView>();
            _vehicles = new GameObject("Vehicles").AddComponent<VehicleViewManager>();
            _vehicles.transform.SetParent(worldGo.transform, false);
            _popups = new GameObject("CoinPopups").AddComponent<CoinPopups>();
            _popups.transform.SetParent(worldGo.transform, false);
            _buildings = new GameObject("Buildings").AddComponent<BuildingsView>();
            _buildings.transform.SetParent(worldGo.transform, false);

            _ui = new GameObject("UI").AddComponent<UIManager>();
            _ui.transform.SetParent(transform, false);

            LoadOrCreate();
            _ui.Initialize(this);
            if (_saves.LoadedFromBackup) _ui.Toast(Loc.T("toast.loaded_backup"));
            CameraController.Tapped += OnWorldTapped;
            ShowMainMenu();
            if (Game.PendingOfflineCents > 0) _ui.ShowOffline();
        }

        /// <summary>
        /// Test ads in the Editor and development builds; no ads in release builds until a real SDK
        /// is integrated and configured (see AdService).
        /// </summary>
        private IAdService CreateAdService()
        {
            if (Application.isEditor || Debug.isDebugBuild)
            {
                var test = new TestAdService();
                test.Started += request => _ui.ShowTestAd(test, request);
                return test;
            }
            return new DisabledAdService();
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
            CameraController.MinY = -34f;
            CameraController.MaxY = 30f;
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
            int seed = Environment.TickCount;
            var game = data != null
                ? TycoonGame.FromSave(Config, data, DateTime.UtcNow, _adService, seed)
                : new TycoonGame(Config, _adService, seed);
            ReplaceGame(game);
            ApplySettings();
        }

        private void ReplaceGame(TycoonGame game)
        {
            if (Game != null)
            {
                Game.ActiveSectionChanged -= OnActiveSectionChanged;
                Game.ServiceIncome -= OnServiceIncome;
            }
            Game = game;
            Game.ActiveSectionChanged += OnActiveSectionChanged;
            Game.ServiceIncome += OnServiceIncome;
            Game.Missions.RefreshDaily(DateTime.UtcNow, Game.Economy.GetIncomePerMinute());
            BindActiveRoad();
        }

        private void OnActiveSectionChanged()
        {
            CancelPlacement();
            BindActiveRoad();
        }

        private void BindActiveRoad()
        {
            var sim = Game.Active;
            sim.Payments.TollCollected += OnTollCollected;
            sim.BoothController.BarrierOpened += _ => Audio.Play(Sfx.Barrier);
            _world.Bind(sim, Game.ActiveSection.Config);
            _vehicles.Bind(sim);
            _popups.Bind(sim, _world);
            _buildings.Bind(Game);
            SelectBooth(sim.Booths[0]);
            SimulationReplaced?.Invoke();
        }

        private void OnTollCollected(TollBooth booth, Traffic.Vehicle vehicle, long cents)
        {
            if (cents > 0) Audio.Play(Sfx.Coin);
        }

        private void OnServiceIncome(PlacedService service, long cents)
        {
            _buildings.ShowIncome(service, cents);
        }

        public void ApplySettings()
        {
            // First start: follow the device language when it is supported, otherwise English.
            if (string.IsNullOrEmpty(Settings.Language) || !Loc.IsSupported(Settings.Language))
            {
                Settings.Language = Loc.FromSystemLanguage(Application.systemLanguage.ToString());
            }
            Loc.Language = Settings.Language;
            Audio.SfxEnabled = Settings.SfxEnabled;
            Audio.SfxVolume = Settings.SfxVolume;
            Audio.SetMusic(Settings.MusicEnabled, Settings.MusicVolume);
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
            if (Game == null) return;
            if (!Paused) Game.Tick(Time.deltaTime);

            _autosaveTimer += Time.unscaledDeltaTime;
            if (_autosaveTimer >= AutosaveSeconds)
            {
                _autosaveTimer = 0;
                SaveNow();
            }
            _missionTimer -= Time.unscaledDeltaTime;
            if (_missionTimer <= 0)
            {
                _missionTimer = 30f;
                Game.Missions.RefreshDaily(DateTime.UtcNow, Game.Economy.GetIncomePerMinute());
            }
            UpdateCameraBounds();
        }

        /// <summary>Lets the camera pan sideways only as far as the road and buildings extend.</summary>
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

        public bool SaveNow() => _saves.Save(Game, Settings);

        private void OnApplicationPause(bool paused)
        {
            if (paused && Game != null) SaveNow();
        }

        private void OnApplicationQuit()
        {
            if (Game != null) SaveNow();
        }

        // ---- screens ------------------------------------------------------------------------

        public void ShowMainMenu()
        {
            Screen = GameScreen.MainMenu;
            Paused = false;
            CancelPlacement();
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
            ReplaceGame(new TycoonGame(Config, _adService, Environment.TickCount));
            Settings = settings;
            SaveNow();
            ShowMainMenu();
        }

        /// <summary>Switches the simulated road and moves to gameplay.</summary>
        public void EnterSection(int index)
        {
            if (Game.SwitchSection(index))
            {
                SaveNow();
                StartGameplay();
            }
        }

        public void SelectBooth(TollBooth booth)
        {
            SelectedBooth = booth;
            foreach (var view in _world.Booths) view.SetSelected(view.Booth == booth);
            SelectionChanged?.Invoke();
        }

        // ---- placement -------------------------------------------------------------------------

        public void BeginPlacement(PlacementKind kind, string id, int fromSlot = -1)
        {
            Placement = kind;
            PlacementId = id;
            PlacementFromSlot = fromSlot;
            if (Screen != GameScreen.Gameplay) StartGameplay();
            CameraController.Focus(new Vector2(0.4f, -3f));
            CameraController.Camera.orthographicSize = 14f;
            PlacementChanged?.Invoke();
        }

        public void CancelPlacement()
        {
            if (Placement == PlacementKind.None) return;
            Placement = PlacementKind.None;
            PlacementId = null;
            PlacementFromSlot = -1;
            PlacementChanged?.Invoke();
        }

        private void OnWorldTapped(Vector2 world)
        {
            if (Screen != GameScreen.Gameplay || Paused) return;
            if (Placement != PlacementKind.None)
            {
                HandlePlacementTap(world);
                return;
            }

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
                return;
            }
            int slot = NearestSlot(Progression.ServiceSlots, world);
            if (slot >= 0 && Game.ActiveSection.Services.At(slot) != null)
            {
                Audio.Play(Sfx.Click);
                _ui.ShowServiceDetail(slot);
                return;
            }
            slot = NearestSlot(Progression.DecorationSlots, world, 1.2f);
            if (slot >= 0 && Game.ActiveSection.Decorations.At(slot) != null)
            {
                Audio.Play(Sfx.Click);
                _ui.ShowDecorationDetail(slot);
            }
        }

        private ProgressionConfig Progression => Game.Progression;

        private void HandlePlacementTap(Vector2 world)
        {
            bool service = Placement == PlacementKind.BuildService || Placement == PlacementKind.MoveService;
            var slots = service ? Progression.ServiceSlots : Progression.DecorationSlots;
            int slot = NearestSlot(slots, world, service ? SlotPickRadius : 1.3f);
            if (slot < 0) return;
            var section = Game.ActiveSection;
            bool free = service ? section.Services.IsSlotFree(slot) : section.Decorations.IsSlotFree(slot);
            if (!free)
            {
                Audio.Play(Sfx.Error);
                _ui.Toast(Loc.T("toast.slot_taken"));
                return;
            }
            Audio.Play(Sfx.Click);
            switch (Placement)
            {
                case PlacementKind.BuildService:
                    _ui.ConfirmBuildService(PlacementId, slot);
                    break;
                case PlacementKind.MoveService:
                    if (section.Services.Move(PlacementFromSlot, slot) == PlacementResult.Ok) Audio.Play(Sfx.Purchase);
                    CancelPlacement();
                    break;
                case PlacementKind.BuildDecoration:
                    _ui.ConfirmBuyDecoration(PlacementId, slot);
                    break;
                case PlacementKind.MoveDecoration:
                    if (section.Decorations.Move(PlacementFromSlot, slot) == PlacementResult.Ok) Audio.Play(Sfx.Purchase);
                    CancelPlacement();
                    break;
            }
        }

        private static int NearestSlot(System.Collections.Generic.List<Vec2> slots, Vector2 world, float radius = SlotPickRadius)
        {
            int best = -1;
            float bestDistance = radius;
            for (int i = 0; i < slots.Count; i++)
            {
                float d = Vector2.Distance(world, new Vector2((float)slots[i].X, (float)slots[i].Y));
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = i;
                }
            }
            return best;
        }

        private void LateUpdate()
        {
            // Booth views are rebuilt with the road; keep the highlight on the selected booth.
            if (SelectedBooth != null)
            {
                foreach (var view in _world.Booths) view.SetSelected(view.Booth == SelectedBooth);
            }
        }

        /// <summary>Money formatting shortcut for UI code.</summary>
        public static string Format(long cents) => Money.Format(cents);
    }
}
