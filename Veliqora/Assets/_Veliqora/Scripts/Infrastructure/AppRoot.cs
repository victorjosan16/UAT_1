using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Veliqora.Ads;
using Veliqora.Audio;
using Veliqora.Core.Ads;
using Veliqora.Core.Economy;
using Veliqora.Core.Levels;
using Veliqora.Core.Save;
using Veliqora.Presentation;
using Veliqora.Settings;
using Veliqora.UI;

namespace Veliqora.Infrastructure
{
    public enum ScreenId { Bootstrap, Home, LevelMap, Gameplay, LevelComplete, Shop, Customization }

    /// <summary>All long-lived services, created once by the composition root and handed to screens.</summary>
    public sealed class AppServices
    {
        public SaveService SaveService;
        public SaveData Save;
        public LevelRepository Levels;
        public EconomyConfig EconomyConfig;
        public EconomyService Economy;
        public SettingsService Settings;
        public ThemeService Themes;
        public AudioService Audio;
        public HapticsService Haptics;
        public IAdsService Ads;
        public AdsConfig AdsConfig;
        public RewardedAdGate AdGate;
        public SceneNavigator Navigator;
        public AppRoot Root;

        public void MarkDirty() => Root.MarkDirty();
        public void SaveNow() => Root.SaveNow();
    }

    /// <summary>
    /// Composition root (the only global). Boots before the first scene, survives scene loads, owns autosave,
    /// Android back/pause handling, and the ads gate clock. Screens receive AppServices explicitly.
    /// </summary>
    public sealed class AppRoot : MonoBehaviour
    {
        public static AppRoot Instance { get; private set; }
        public AppServices Services { get; private set; }
        public bool Booted { get; private set; }
        public string StartupMessage { get; private set; }
        public string FatalError { get; private set; }

        private string _saveDirectory;
        private bool _dirty;
        private float _lastSave;
        private double _activeClock; // advances only while the app is in the foreground
        private const float SaveThrottle = 0.75f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoCreate()
        {
            if (Instance != null) return;
            var go = new GameObject("VeliqoraApp");
            DontDestroyOnLoad(go);
            go.AddComponent<AppRoot>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.SystemSetting;
            Boot();
        }

        private void Boot()
        {
            try
            {
                var s = new AppServices { Root = this };

                // 1) Save (never throws; recovers from backup or starts fresh with a message)
                s.SaveService = new SaveService(new FileSaveStorage(_saveDirectory ?? Application.persistentDataPath));
                var load = s.SaveService.Load();
                s.Save = load.Data;
                StartupMessage = load.UserMessage;
                if (load.TechnicalDetail != null) Debug.LogWarning("[Veliqora] Save recovery: " + load.TechnicalDetail);

                // 2) Content
                var levelsText = Resources.Load<TextAsset>("Veliqora/Levels/levels");
                if (levelsText == null) throw new InvalidOperationException("Level catalog missing (Resources/Veliqora/Levels/levels.json). Run Veliqora > Generate Levels.");
                s.Levels = new LevelRepository(LevelCatalog.FromJson(levelsText.text));
                var ecoText = Resources.Load<TextAsset>("Veliqora/Config/economy");
                if (ecoText == null) throw new InvalidOperationException("Economy config missing (Resources/Veliqora/Config/economy.json).");
                s.EconomyConfig = EconomyConfig.FromJson(ecoText.text);

                // 3) Content-version migration: drop in-progress puzzle if layouts may have changed; clamp progress.
                if (s.Save.ContentVersion != s.Levels.ContentVersion)
                {
                    s.Save.Session = null;
                    s.Save.ContentVersion = s.Levels.ContentVersion;
                }
                var prog = s.Save.Progress;
                prog.HighestUnlocked = Mathf.Clamp(prog.HighestUnlocked, 1, s.Levels.Count);
                prog.CurrentLevel = Mathf.Clamp(prog.CurrentLevel, 1, s.Levels.Count);

                // 4) Economy, settings, cosmetics
                s.Economy = new EconomyService(s.Save.Economy, s.EconomyConfig);
                s.Economy.EnsureStartingGrant();
                s.Economy.Changed += MarkDirty;
                s.Settings = new SettingsService(s.Save.Settings, MarkDirty);
                s.Themes = new ThemeService(s.EconomyConfig, s.Save.Cosmetics, s.Economy, MarkDirty);

                // 5) Audio / haptics
                var audioGo = new GameObject("Audio");
                audioGo.transform.SetParent(transform, false);
                s.Audio = audioGo.AddComponent<AudioService>();
                s.Audio.Init(s.Save.Settings);
                s.Settings.Changed += s.Audio.ApplySettings;
                s.Haptics = new HapticsService(s.Save.Settings);

                // 6) Ads (mock in editor/dev; disabled in release unless a real adapter is configured)
                s.AdsConfig = AdsConfig.Load();
                s.Ads = AdsFactory.Create(s.AdsConfig, s.Save.Settings);
                s.AdGate = new RewardedAdGate(s.Ads, () => _activeClock, s.AdsConfig.RewardedTimeoutSeconds);

                // 7) Navigation
                s.Navigator = new SceneNavigator(this, s);
                Services = s;
                Booted = true;
                SaveNow();
            }
            catch (Exception e)
            {
                FatalError = e.Message;
                Debug.LogError("[Veliqora] Boot failed: " + e);
            }
        }

        private void Start()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("VeliqoraCamera");
                DontDestroyOnLoad(camGo);
                cam = camGo.AddComponent<Camera>();
                camGo.tag = "MainCamera";
            }
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = UIStyle.Navy;
            cam.orthographic = true;
            cam.cullingMask = 0; // UI uses screen-space overlay canvases

            UIFactory.EnsureEventSystem();
            if (!Booted)
            {
                ShowFatal();
                return;
            }

            // If a specific screen's scene was opened directly in the Editor, build that screen in place.
            var entry = FindEntryInActiveScene();
            var target = entry != null ? entry.Screen : ScreenId.Bootstrap;
            if (target == ScreenId.Bootstrap) Services.Navigator.Go(ScreenId.Home);
            else Services.Navigator.BuildInCurrentScene(target, null);

            if (!string.IsNullOrEmpty(StartupMessage))
                Tween.Delay(this, 0.6f, () => Overlays.Message("Heads up", StartupMessage));
        }

        private static SceneEntry FindEntryInActiveScene()
        {
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                var e = root.GetComponentInChildren<SceneEntry>(true);
                if (e != null) return e;
            }
            return null;
        }

        private void ShowFatal()
        {
            var canvas = UIFactory.CreateCanvas("FatalCanvas", 999);
            UIFactory.Image(canvas.transform, "Bg", SpriteLibrary.Pixel, UIStyle.Navy);
            var t = UIFactory.Text(canvas.transform, "Veliqora couldn't start.\n\n" + FatalError, UIStyle.FontBody, Color.white, TextAnchor.MiddleCenter, FontStyle.Normal);
            UIFactory.Stretch(t.rectTransform, 80);
        }

        /// <summary>Deletes all local data and re-boots services from a fresh save, then returns Home.</summary>
        public void ResetAllProgress()
        {
            if (!Booted) return;
            Services.SaveService.DeleteAll();
            Reboot(_saveDirectory);
        }

        /// <summary>
        /// Tears down services and boots again from the save in <paramref name="saveDirectory"/> (null = default
        /// persistentDataPath). Used by Reset Progress and by PlayMode tests to isolate save data.
        /// </summary>
        public void Reboot(string saveDirectory, bool goHome = true)
        {
            Overlays.CloseAll();
            if (Services != null)
            {
                Services.Ads.HideBanner();
                if (Services.Audio != null) Destroy(Services.Audio.gameObject);
                Services.Navigator.Dispose();
            }
            _saveDirectory = saveDirectory;
            Booted = false;
            Services = null;
            Boot();
            if (!Booted) { ShowFatal(); return; }
            if (goHome) Services.Navigator.Go(ScreenId.Home);
        }

        // ---------------- autosave ----------------

        public void MarkDirty() => _dirty = true;

        public void SaveNow()
        {
            if (Services == null) return;
            _dirty = false;
            _lastSave = Time.unscaledTime;
            if (!Services.SaveService.Save(Services.Save))
                Debug.LogWarning("[Veliqora] Save failed: " + Services.SaveService.LastError);
        }

        private void Update()
        {
            _activeClock += Mathf.Min(Time.unscaledDeltaTime, 0.5f);
            if (!Booted) return;
            Services.AdGate.Tick();
            if (BackPressed()) HandleBack();
        }

        private void LateUpdate()
        {
            if (_dirty && Time.unscaledTime - _lastSave > SaveThrottle) SaveNow();
        }

        private void OnApplicationPause(bool paused)
        {
            if (!Booted) return;
            if (paused) SaveNow();
            Services.Audio.PauseAll(paused);
            Services.Navigator.Current?.OnAppPause(paused);
        }

        private void OnApplicationQuit()
        {
            if (Booted) SaveNow();
        }

        // ---------------- back button ----------------

        private static bool BackPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            return kb != null && kb.escapeKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Escape);
#else
            return false;
#endif
        }

        public void HandleBack()
        {
            if (Services.Navigator.IsTransitioning) return;
            if (Overlays.HandleBack()) return;
            var current = Services.Navigator.Current;
            if (current != null && current.HandleBack()) return;
            Services.Navigator.DefaultBack();
        }
    }
}
