using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Veliqora.Core.Ads;
using Veliqora.UI;
using Veliqora.UI.Screens;

namespace Veliqora.Infrastructure
{
    /// <summary>
    /// Scene-per-screen navigation with a short fade. Uses the real scenes when they're in Build Settings
    /// (created by "Veliqora > Setup Project"); otherwise falls back to an empty runtime scene so a fresh
    /// checkout is still playable before setup.
    /// </summary>
    public sealed class SceneNavigator
    {
        private readonly AppRoot _root;
        private readonly AppServices _services;
        private CanvasGroup _fade;
        private int _runtimeSceneCounter;

        public ScreenBase Current { get; private set; }
        public ScreenId CurrentId { get; private set; } = ScreenId.Bootstrap;
        public object CurrentArgs { get; private set; }
        public ScreenId PreviousId { get; private set; } = ScreenId.MainMenu;
        public object PreviousArgs { get; private set; }
        public bool IsTransitioning { get; private set; }

        public event Action<ScreenId> ScreenShown;

        public SceneNavigator(AppRoot root, AppServices services)
        {
            _root = root;
            _services = services;
        }

        private CanvasGroup Fade
        {
            get
            {
                if (_fade != null) return _fade;
                var canvas = UIFactory.CreateCanvas("VeliqoraFadeCanvas", 900);
                UnityEngine.Object.DontDestroyOnLoad(canvas.gameObject);
                UIFactory.Image(canvas.transform, "Fade", SpriteLibrary.Pixel, UIStyle.Navy, true);
                _fade = canvas.gameObject.AddComponent<CanvasGroup>();
                _fade.alpha = 0f;
                _fade.blocksRaycasts = false;
                return _fade;
            }
        }

        public void Dispose()
        {
            if (_fade != null) UnityEngine.Object.Destroy(_fade.gameObject);
            _fade = null;
        }

        public void Go(ScreenId id, object args = null)
        {
            if (IsTransitioning) return;
            _root.StartCoroutine(GoRoutine(id, args));
        }

        /// <summary>Back behaviour when the screen/overlays didn't consume it.</summary>
        public void DefaultBack()
        {
            switch (CurrentId)
            {
                case ScreenId.MainMenu:
                    Overlays.Confirm("Leave Veliqora?", "Your progress is saved.", "Quit", Application.Quit, "Stay", UIStyle.ButtonKind.Danger);
                    break;
                case ScreenId.LevelMap:
                    Go(ScreenId.MainMenu);
                    break;
                case ScreenId.LevelComplete:
                    Go(ScreenId.LevelMap);
                    break;
                case ScreenId.Shop:
                case ScreenId.Themes:
                case ScreenId.Settings:
                    GoBackToPrevious();
                    break;
                default:
                    Go(ScreenId.MainMenu);
                    break;
            }
        }

        /// <summary>Returns to the screen that opened Shop/Customization (falls back to Home).</summary>
        public void GoBackToPrevious()
        {
            var target = PreviousId;
            if (target == CurrentId || target == ScreenId.Bootstrap || target == ScreenId.LevelComplete) target = ScreenId.MainMenu;
            Go(target, PreviousArgs);
        }

        private IEnumerator GoRoutine(ScreenId id, object args)
        {
            IsTransitioning = true;
            var fade = Fade;
            fade.blocksRaycasts = true;
            float d = MotionPrefs.Dur(0.2f);
            yield return FadeTo(fade, 1f, d);

            Overlays.CloseAll();
            _services.SaveNow();
            // Remember where Shop/Customization should return to.
            if (id == ScreenId.Shop || id == ScreenId.Themes || id == ScreenId.Settings)
            {
                if (CurrentId != ScreenId.Shop && CurrentId != ScreenId.Themes && CurrentId != ScreenId.Settings)
                {
                    PreviousId = CurrentId;
                    PreviousArgs = CurrentArgs;
                }
            }

            string sceneName = id.ToString();
            if (Application.CanStreamedLevelBeLoaded(sceneName))
            {
                var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
                while (op != null && !op.isDone) yield return null;
            }
            else
            {
                var old = SceneManager.GetActiveScene();
                var scene = SceneManager.CreateScene(sceneName + "_Runtime" + (++_runtimeSceneCounter));
                SceneManager.SetActiveScene(scene);
                if (old.IsValid() && old.isLoaded && SceneManager.sceneCount > 1)
                {
                    var unload = SceneManager.UnloadSceneAsync(old);
                    while (unload != null && !unload.isDone) yield return null;
                }
            }

            BuildInCurrentScene(id, args);
            yield return null; // let layout settle one frame before revealing
            yield return FadeTo(fade, 0f, d);
            fade.blocksRaycasts = false;
            IsTransitioning = false;
        }

        private static IEnumerator FadeTo(CanvasGroup g, float target, float duration)
        {
            float start = g.alpha;
            if (duration <= 0f) { g.alpha = target; yield break; }
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                g.alpha = Mathf.Lerp(start, target, t / duration);
                yield return null;
            }
            g.alpha = target;
        }

        /// <summary>Instantiates the screen controller for <paramref name="id"/> in the active scene.</summary>
        public void BuildInCurrentScene(ScreenId id, object args)
        {
            var go = new GameObject("Screen_" + id);
            ScreenBase screen;
            switch (id)
            {
                case ScreenId.LevelMap: screen = go.AddComponent<LevelMapScreen>(); break;
                case ScreenId.Gameplay: screen = go.AddComponent<GameplayScreen>(); break;
                case ScreenId.LevelComplete: screen = go.AddComponent<LevelCompleteScreen>(); break;
                case ScreenId.Shop: screen = go.AddComponent<ShopScreen>(); break;
                case ScreenId.Themes: screen = go.AddComponent<ThemesScreen>(); break;
                case ScreenId.Settings: screen = go.AddComponent<SettingsScreen>(); break;
                default: screen = go.AddComponent<MainMenuScreen>(); id = ScreenId.MainMenu; break;
            }
            Current = screen;
            CurrentId = id;
            CurrentArgs = args;
            screen.Setup(_services, args);

            if (_services.Ads.SupportsBanner && screen.AllowsBanner) _services.Ads.ShowBanner(AdPlacements.Banner);
            else _services.Ads.HideBanner();
            ScreenShown?.Invoke(id);
        }
    }
}
