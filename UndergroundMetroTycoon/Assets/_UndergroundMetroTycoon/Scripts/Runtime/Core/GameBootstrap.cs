using System;
using System.Collections.Generic;
using UndergroundMetroTycoon.Simulation;
using UnityEngine;

namespace UndergroundMetroTycoon.Game
{
    /// <summary>
    /// Composition root for the Phase 1 scene. Loads (or creates) the simulation session, builds the procedural
    /// world, player, camera, effects and UI, wires events between them and handles saving.
    /// Everything it creates lives under one runtime root so a progress reset can rebuild cleanly.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class GameBootstrap : MonoBehaviour, IGameUIActions
    {
        [SerializeField] private GameConfig config;
        [Tooltip("Lit material used as the template for all procedural geometry. Created by the editor setup.")]
        [SerializeField] private Material baseMaterial;
        [SerializeField] private string saveFileName = "umt_save.json";
        [SerializeField] private float autosaveInterval = 10f;

        private static readonly Vector3 DefaultSpawn = new Vector3(-2.2f, 0f, -3.6f);

        private GameContent _content;
        private SaveStore _store;
        private MaterialLibrary _materials;
        private Transform _runtimeRoot;
        private ExcavationSiteView _siteView;
        private PlayerController _player;
        private DigController _dig;
        private IsometricCameraController _camera;
        private GameUI _ui;
        private SfxPlayer _sfx;
        private ExcavationVfx _vfx;
        private Light _sun;
        private float _autosaveTimer;
        private bool _pendingReset;
        private bool _savingSuspended;
        private string _startupMessage;

        public GameSession Session { get; private set; }
        public PlayerController Player => _player;

        /// <summary>Used by tests and editor tooling to configure a bootstrap created from code.</summary>
        public void Configure(GameConfig gameConfig, Material material, string saveFile)
        {
            config = gameConfig;
            baseMaterial = material;
            if (!string.IsNullOrEmpty(saveFile)) saveFileName = saveFile;
        }

        private void Awake()
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            _content = LoadContent();
            _materials = new MaterialLibrary(baseMaterial);
            _store = new SaveStore(Application.persistentDataPath, saveFileName, new JsonUtilitySaveSerializer());
            Initialize();
        }

        private void Initialize()
        {
            Session = LoadSession();
            _runtimeRoot = new GameObject("Runtime").transform;
            _runtimeRoot.SetParent(transform, false);

            _sfx = new GameObject("Audio").AddComponent<SfxPlayer>();
            _sfx.transform.SetParent(_runtimeRoot, false);
            _sfx.Init();

            var site = Session.Content.Site;
            SiteEnvironmentBuilder.Result environment = SiteEnvironmentBuilder.Build(_runtimeRoot, _materials, new Vector2(site.CenterX, site.CenterZ));
            _sun = environment.Sun;

            _vfx = new GameObject("VFX").AddComponent<ExcavationVfx>();
            _vfx.transform.SetParent(_runtimeRoot, false);
            _vfx.Init(_materials);

            _siteView = new GameObject("ExcavationSite").AddComponent<ExcavationSiteView>();
            _siteView.transform.SetParent(_runtimeRoot, false);
            _siteView.Init(_materials);
            _siteView.Rebuild(Session.Grid);

            var mover = new KinematicMover
            {
                Ground = Session.Grid,
                MinX = environment.WalkBounds.xMin,
                MaxX = environment.WalkBounds.xMax,
                MinZ = environment.WalkBounds.yMin,
                MaxZ = environment.WalkBounds.yMax
            };
            mover.SetObstacles(environment.Obstacles);

            var cameraGo = new GameObject("IsometricCamera") { tag = "MainCamera" };
            cameraGo.transform.SetParent(_runtimeRoot, false);
            var cam = cameraGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.55f, 0.78f, 0.95f);
            cameraGo.AddComponent<AudioListener>();

            var playerGo = new GameObject("Player");
            playerGo.transform.SetParent(_runtimeRoot, false);
            CharacterRig rig = CharacterRig.Build(playerGo.transform, _materials);
            _player = playerGo.AddComponent<PlayerController>();
            _player.Init(mover, rig, cameraGo.transform);
            PlayerPose pose = Session.PlayerPose;
            if (pose.IsSet) _player.Teleport(new Vector3(pose.X, pose.Y, pose.Z), pose.Yaw);
            else _player.Teleport(DefaultSpawn, 45f);

            _dig = playerGo.AddComponent<DigController>();
            _dig.Init(Session, _player, _siteView);
            _dig.BlockedHint += OnDigBlocked;

            _ui = GameUI.Build(_runtimeRoot, Session, this, _sfx);
            _player.SetJoystick(_ui.Joystick);

            _camera = cameraGo.AddComponent<IsometricCameraController>();
            _camera.Init(playerGo.transform, environment.WalkBounds, _ui.Joystick);
            _ui.Feedback.SetCamera(cam);

            SubscribeSessionEvents();
            ApplySettings();

            if (!string.IsNullOrEmpty(_startupMessage)) _ui.Feedback.ShowToast(_startupMessage);
            else if (Session.Contracts.ActiveContractId == null && Session.Contracts.GetProgress(DefaultContent.GardenTrenchId)?.CompletionCount == 0)
                _ui.Feedback.ShowToast("Welcome! Open CONTRACTS and accept your first job.");
            _startupMessage = null;
            _autosaveTimer = autosaveInterval;
        }

        private GameContent LoadContent()
        {
            GameContent content = config != null ? config.ToContent() : DefaultContent.Create();
            List<string> errors = ContentValidator.Validate(content);
            if (errors.Count == 0) return content;
            foreach (string error in errors) Debug.LogError("[UMT] GameConfig: " + error);
            Debug.LogError("[UMT] GameConfig is invalid; falling back to built-in Phase 1 defaults.");
            return DefaultContent.Create();
        }

        private GameSession LoadSession()
        {
            SaveLoadResult result = _store.Load();
            switch (result.Status)
            {
                case SaveLoadStatus.Loaded:
                case SaveLoadStatus.LoadedFromBackup:
                    var warnings = new List<string>();
                    GameSession session = GameSession.Restore(_content, result.Data, warnings);
                    foreach (string w in warnings) Debug.LogWarning("[UMT] Save repaired: " + w);
                    if (result.Status == SaveLoadStatus.LoadedFromBackup)
                    {
                        Debug.LogWarning("[UMT] " + result.Message);
                        _startupMessage = "Save restored from backup.";
                    }
                    return session;
                case SaveLoadStatus.Corrupted:
                    Debug.LogError("[UMT] " + result.Message + " Starting a new game.");
                    _startupMessage = "Save file was damaged. Starting fresh.";
                    return GameSession.CreateNew(_content);
                default:
                    return GameSession.CreateNew(_content);
            }
        }

        private void SubscribeSessionEvents()
        {
            Session.SiteRebuilt += OnSiteRebuilt;
            Session.CellDamaged += OnCellDamaged;
            Session.CellExcavated += OnCellExcavated;
            Session.ContractStateChanged += OnContractStateChanged;
            Session.ContractRewardClaimed += OnContractRewardClaimed;
            Session.LevelChanged += OnLevelChanged;
        }

        private void UnsubscribeSessionEvents()
        {
            if (Session == null) return;
            Session.SiteRebuilt -= OnSiteRebuilt;
            Session.CellDamaged -= OnCellDamaged;
            Session.CellExcavated -= OnCellExcavated;
            Session.ContractStateChanged -= OnContractStateChanged;
            Session.ContractRewardClaimed -= OnContractRewardClaimed;
            Session.LevelChanged -= OnLevelChanged;
        }

        private void OnSiteRebuilt(ExcavationGrid grid)
        {
            _dig.ResetTarget();
            _siteView.Rebuild(grid);
            _player.Mover.Ground = grid;
            // Re-seat the player on the regenerated terrain so nobody ends up inside a refilled hole.
            _player.Teleport(_player.transform.position, _player.Yaw);
            _ui.Hud.Refresh();
        }

        private void OnCellDamaged(TerrainCell cell, int damage)
        {
            _siteView.OnCellDamaged(cell);
            Vector3 top = Session.Grid.Layout.CellTopCenter(cell.Coord).ToUnity();
            _vfx.EmitHit(top, cell.Material);
            _sfx.PlayHit(cell.Material);
        }

        private void OnCellExcavated(TerrainCell cell, long coins, int xp)
        {
            _siteView.OnCellExcavated(cell);
            Vector3 center = Session.Grid.Layout.CellCenter(cell.Coord).ToUnity();
            _vfx.EmitBreak(center, cell.Material);
            _sfx.PlayBreak();
            if (coins > 0)
            {
                _ui.Feedback.ShowFloatingText(center + Vector3.up * 0.8f, "+" + coins, Palette.Yellow);
                _ui.Feedback.FlyCoinsFromWorld(center, Mathf.Clamp((int)coins / 2, 1, 4));
            }
            _ui.Hud.Refresh();
        }

        private void OnContractStateChanged(string contractId, ContractState state)
        {
            _ui.Hud.Refresh();
            _ui.Contracts.RefreshIfOpen();
            if (state == ContractState.ReadyForCompletion)
            {
                ExcavationGrid grid = Session.Grid;
                Vector3 center = new Vector3(Session.Content.Site.CenterX, grid.Layout.SurfaceY, Session.Content.Site.CenterZ);
                _vfx.EmitCelebration(center);
                _sfx.PlayFanfare();
                _ui.Feedback.ShowBanner("CONTRACT COMPLETE!", "Tap CLAIM to collect your payment");
                SaveNow();
            }
        }

        private void OnContractRewardClaimed(ContractDefinition def, long coins, int xp)
        {
            _ui.Feedback.FlyCoinsFromCenter(Mathf.Clamp((int)(coins / 25), 8, 24));
            _ui.Feedback.ShowToast("+" + HudView.FormatCoins(coins) + " coins   +" + xp + " XP");
            _ui.Hud.Refresh();
            SaveNow();
        }

        private void OnLevelChanged(int previous, int level)
        {
            _sfx.PlayLevelUp();
            _ui.Feedback.ShowBanner("LEVEL " + level + "!", "New contracts may be available");
            _ui.Hud.Refresh();
        }

        private void OnDigBlocked(DigOutcome reason)
        {
            switch (reason)
            {
                case DigOutcome.NoActiveContract:
                    _ui.Feedback.ShowToast("Accept a contract to start digging here.");
                    _ui.Hud.PulseContractsButton();
                    break;
                case DigOutcome.ToolTooWeak:
                    _ui.Feedback.ShowToast("This ground needs a stronger tool.");
                    break;
                case DigOutcome.OutsideContract:
                    _ui.Feedback.ShowToast("This area is not part of your contract.");
                    break;
            }
        }

        // ---- IGameUIActions -------------------------------------------------------------------------

        public void AcceptContract(string contractId)
        {
            ContractActionResult result = Session.AcceptContract(contractId);
            if (result == ContractActionResult.Ok)
            {
                ContractDefinition def = Session.Contracts.GetDefinition(contractId);
                _ui.Feedback.ShowToast("Contract accepted: " + def.Title + ". Walk up to the soil to dig!");
                SaveNow();
            }
            else
            {
                _ui.Feedback.ShowToast(DescribeFailure(result));
            }
        }

        public void ClaimContract(string contractId)
        {
            ContractActionResult result = Session.ClaimContract(contractId);
            if (result != ContractActionResult.Ok) _ui.Feedback.ShowToast(DescribeFailure(result));
        }

        public void OpenContracts()
        {
            _sfx.PlayClick();
            _ui.Settings.Close();
            _ui.Contracts.Open();
        }

        public void OpenSettings()
        {
            _sfx.PlayClick();
            _ui.Contracts.Close();
            _ui.Settings.Open();
        }

        public void SetSoundEnabled(bool enabled)
        {
            Session.Settings.SoundEnabled = enabled;
            Session.MarkDirty();
            ApplySettings();
        }

        public void SetGraphicsQuality(int quality)
        {
            Session.Settings.GraphicsQuality = Mathf.Clamp(quality, 0, 2);
            Session.MarkDirty();
            ApplySettings();
        }

        public void ResetProgress() => _pendingReset = true;

        // ---- Lifecycle & saving ----------------------------------------------------------------------

        private void Update()
        {
            if (_pendingReset)
            {
                _pendingReset = false;
                PerformReset();
                return;
            }

            _autosaveTimer -= Time.unscaledDeltaTime;
            if (_autosaveTimer <= 0f)
            {
                _autosaveTimer = autosaveInterval;
                if (Session.IsDirty) SaveNow();
            }
        }

        private void PerformReset()
        {
            _savingSuspended = true;
            UnsubscribeSessionEvents();
            _store.DeleteAll();
            DestroyImmediate(_runtimeRoot.gameObject);
            _materials.DestroyAll();
            _savingSuspended = false;
            Initialize();
            SaveNow();
        }

        public void SaveNow()
        {
            if (_savingSuspended || Session == null || _store == null) return;
            if (_player != null)
            {
                Vector3 p = _player.transform.position;
                Session.PlayerPose = new PlayerPose { IsSet = true, X = p.x, Y = p.y, Z = p.z, Yaw = _player.Yaw };
            }
            if (_store.Save(Session.CreateSaveData(DateTime.UtcNow.Ticks), out string error)) Session.ClearDirty();
            else Debug.LogWarning("[UMT] Save failed: " + error);
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) SaveNow();
        }

        private void OnApplicationQuit() => SaveNow();

        private void OnDestroy()
        {
            UnsubscribeSessionEvents();
            _materials?.DestroyAll();
        }

        private void ApplySettings()
        {
            SettingsData s = Session.Settings;
            _sfx.Enabled = s.SoundEnabled;
            AudioListener.volume = s.SoundEnabled ? 1f : 0f;
            switch (s.GraphicsQuality)
            {
                case 0:
                    _sun.shadows = LightShadows.None;
                    _vfx.Intensity = 0.5f;
                    Application.targetFrameRate = 30;
                    break;
                case 1:
                    _sun.shadows = LightShadows.Hard;
                    _vfx.Intensity = 0.8f;
                    Application.targetFrameRate = 60;
                    break;
                default:
                    _sun.shadows = LightShadows.Soft;
                    _vfx.Intensity = 1f;
                    Application.targetFrameRate = 60;
                    break;
            }
        }

        private static string DescribeFailure(ContractActionResult result)
        {
            switch (result)
            {
                case ContractActionResult.AnotherContractActive: return "Finish your current contract first.";
                case ContractActionResult.Locked: return "This contract is still locked.";
                case ContractActionResult.AlreadyClaimed: return "Reward already collected.";
                case ContractActionResult.InvalidState: return "That contract can't do that right now.";
                default: return "Unknown contract.";
            }
        }
    }
}
