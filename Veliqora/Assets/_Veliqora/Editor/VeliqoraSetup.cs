using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Veliqora.Infrastructure;
using Veliqora.UI;
#if UNITY_2021_2_OR_NEWER
using UnityEditor.Build;
#endif

namespace Veliqora.EditorTools
{
    /// <summary>
    /// "Veliqora > Setup Project": idempotently creates folders, scenes (each with a SceneEntry marker), Build
    /// Settings order, Android player settings and PLACEHOLDER icons. Safe to run repeatedly.
    /// </summary>
    public static class VeliqoraSetup
    {
        public const string Root = "Assets/_Veliqora";
        public const string ScenesDir = Root + "/Scenes";
        public const string IconsDir = Root + "/Art/Icons";
        public const string PackageId = "com.animatix.veliqora"; // PLACEHOLDER — verify availability before release

        public static readonly ScreenId[] SceneOrder =
        {
            ScreenId.Bootstrap, ScreenId.Home, ScreenId.LevelMap, ScreenId.Gameplay,
            ScreenId.LevelComplete, ScreenId.Shop, ScreenId.Customization,
        };

        [MenuItem("Veliqora/Setup Project", priority = 0)]
        public static void SetupProject()
        {
            EnsureFolders();
            var scenes = EnsureScenes();
            EditorBuildSettings.scenes = scenes.ConvertAll(p => new EditorBuildSettingsScene(p, true)).ToArray();
            ConfigurePlayer();
            GeneratePlaceholderArt();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            var problems = VeliqoraValidator.ValidateProject(false);
            string summary = problems.Count == 0 ? "No problems found." : problems.Count + " problem(s) — see Console.";
            Debug.Log("[Veliqora] Setup complete. " + summary);
            EditorUtility.DisplayDialog("Veliqora", "Setup complete.\n\n" + summary + "\n\nOpen Scenes/Bootstrap and press Play.", "OK");
        }

        public static void EnsureFolders()
        {
            string[] dirs =
            {
                "Art/Sprites", "Art/UI", "Art/Backgrounds", "Art/Bottles", "Art/Effects", "Art/Icons", "Art/Fonts",
                "Audio/Music", "Audio/SFX", "Audio/Mixers", "Materials", "Prefabs/UI", "Prefabs/Bottles", "Prefabs/Gameplay",
                "Prefabs/Effects", "Scenes", "ScriptableObjects/Levels", "ScriptableObjects/Economy", "ScriptableObjects/Themes",
                "Resources/Veliqora/Levels", "Resources/Veliqora/Config", "Resources/Veliqora/Art/Icons",
                "Resources/Veliqora/Art/Backgrounds", "Resources/Veliqora/Audio", "Resources/Veliqora/Fonts",
            };
            foreach (var d in dirs) Directory.CreateDirectory(Path.Combine(Root, d));
            AssetDatabase.Refresh();
        }

        public static List<string> EnsureScenes()
        {
            var paths = new List<string>();
            var original = SceneManager.GetActiveScene().path;
            foreach (var id in SceneOrder)
            {
                string path = ScenesDir + "/" + id + ".unity";
                paths.Add(path);
                if (File.Exists(path))
                {
                    // Make sure the marker exists and points to the right screen.
                    var existing = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    if (EnsureEntry(existing, id)) EditorSceneManager.SaveScene(existing);
                    continue;
                }
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EnsureEntry(scene, id);
                EditorSceneManager.SaveScene(scene, path);
            }
            if (!string.IsNullOrEmpty(original) && File.Exists(original)) EditorSceneManager.OpenScene(original, OpenSceneMode.Single);
            else EditorSceneManager.OpenScene(paths[0], OpenSceneMode.Single);
            return paths;
        }

        private static bool EnsureEntry(Scene scene, ScreenId id)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var e = root.GetComponentInChildren<SceneEntry>(true);
                if (e == null) continue;
                if (e.Screen == id) return false;
                e.Screen = id;
                EditorUtility.SetDirty(e);
                return true;
            }
            var go = new GameObject("SceneEntry (" + id + ")");
            SceneManager.MoveGameObjectToScene(go, scene);
            go.AddComponent<SceneEntry>().Screen = id;
            return true;
        }

        public static void ConfigurePlayer()
        {
            PlayerSettings.productName = "Veliqora";
            PlayerSettings.companyName = "AnimatiX";
            if (string.IsNullOrEmpty(PlayerSettings.bundleVersion) || PlayerSettings.bundleVersion == "1.0") PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.SplashScreen.backgroundColor = UIStyle.Navy;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto; // highest installed; must satisfy current Play policy
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            if (PlayerSettings.Android.bundleVersionCode < 1) PlayerSettings.Android.bundleVersionCode = 1;
#if UNITY_2021_2_OR_NEWER
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, PackageId);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
#else
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, PackageId);
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
#endif
        }

        // ---------------- placeholder art ----------------

        [MenuItem("Veliqora/Regenerate Placeholder Icons", priority = 21)]
        public static void GeneratePlaceholderArt()
        {
            Directory.CreateDirectory(IconsDir);
            WritePng(IconsDir + "/AppIcon_PLACEHOLDER.png", AppIcon(1024, false));
            WritePng(IconsDir + "/AppIconAlt_PLACEHOLDER.png", AppIcon(1024, true));
            WritePng(IconsDir + "/FeatureGraphic_PLACEHOLDER.png", FeatureGraphic(1024, 500));
            AssetDatabase.Refresh();
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconsDir + "/AppIcon_PLACEHOLDER.png");
            if (icon != null)
            {
#if UNITY_2021_2_OR_NEWER
                PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
#else
                PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, new[] { icon });
#endif
            }
        }

        private static void WritePng(string path, Texture2D tex)
        {
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        /// <summary>Droplet motif on a rounded gradient tile (PLACEHOLDER app icon).</summary>
        public static Texture2D AppIcon(int size, bool alternate)
        {
            var p = new PixelPainter(size, size);
            float s = size / 128f;
            var top = alternate ? UIStyle.Hex("#8C6CF0") : UIStyle.Hex("#5BC0EB");
            var bottom = alternate ? UIStyle.Hex("#2A1E5C") : UIStyle.Hex("#2E5DA8");
            p.Fill(q => PixelPainter.RoundedRect(q, new Rect(0, 0, size, size), 26 * s), Color.white,
                q => Color.Lerp(bottom, top, q.y / size));
            var c = new Vector2(size * 0.5f, size * 0.36f);
            p.Fill(q => PixelPainter.Circle(q, c + new Vector2(0, 6 * s), 52 * s), new Color(1, 1, 1, 0.18f));
            p.Fill(q => PixelPainter.Droplet(q, c, 34 * s), Color.white, q =>
            {
                float hue = Mathf.Repeat(0.55f + (q.x + q.y) / (size * 2.4f), 1f);
                return Color.Lerp(Color.HSVToRGB(hue, 0.7f, 1f), Color.white, Mathf.Clamp01((q.y - c.y) / (60 * s)) * 0.35f);
            });
            p.Fill(q => PixelPainter.Circle(q, c + new Vector2(-12 * s, 6 * s), 8 * s), new Color(1, 1, 1, 0.8f));
            return p.ToTexture();
        }

        /// <summary>1024x500 store feature graphic background (PLACEHOLDER; add title text in an image editor).</summary>
        public static Texture2D FeatureGraphic(int w, int h)
        {
            var p = new PixelPainter(w, h);
            p.VerticalGradient(UIStyle.Hex("#D9F1FB"), UIStyle.Hex("#7EC8F2"));
            for (int i = 0; i < 6; i++)
            {
                var c = new Vector2(w * (0.12f + i * 0.155f), h * 0.32f);
                var col = Veliqora.Presentation.LiquidPalette.Get(1 + i);
                p.Fill(q => PixelPainter.RoundedRect(q, new Rect(c.x - 34, c.y - 110, 68, 220), 30), new Color(1, 1, 1, 0.35f));
                p.Fill(q => PixelPainter.RoundedRect(q, new Rect(c.x - 26, c.y - 102, 52, 150), 24), col);
            }
            var dc = new Vector2(w * 0.5f, h * 0.62f);
            p.Fill(q => PixelPainter.Droplet(q, dc, 60), Color.white, q => Color.HSVToRGB(Mathf.Repeat(0.5f + q.x / (w * 1.5f), 1f), 0.65f, 1f));
            return p.ToTexture();
        }
    }
}
