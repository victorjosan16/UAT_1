using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TollRoadTycoon.EditorTools
{
    /// <summary>
    /// One-time project configuration done through Unity's own APIs (so Unity writes valid scene and
    /// settings files): Boot scene in build settings, portrait orientation, Android identifiers.
    /// Runs automatically on first import and is available under Tools > Toll Road Tycoon.
    /// </summary>
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        public const string BootScenePath = "Assets/Scenes/Boot.unity";
        private const string DoneKey = "TollRoadTycoon.ProjectSetup.v1";

        static ProjectSetup()
        {
            if (Application.isBatchMode || SessionState.GetBool(DoneKey, false)) return;
            SessionState.SetBool(DoneKey, true);
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(BootScenePath) || PlayerSettings.defaultInterfaceOrientation != UIOrientation.Portrait) Run();
            };
        }

        [MenuItem("Tools/Toll Road Tycoon/Configure Project")]
        public static void Run()
        {
            EnsureBootScene();
            ConfigurePlayer();
            AssetDatabase.SaveAssets();
            Debug.Log("[Toll Road Tycoon] Project configured: Boot scene, portrait, Android ARM64/IL2CPP.");
        }

        private static void EnsureBootScene()
        {
            Directory.CreateDirectory("Assets/Scenes");
            if (!File.Exists(BootScenePath))
            {
                if (!Application.isBatchMode && EditorSceneManager.GetActiveScene().isDirty && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                // The game builds its world at runtime (GameBootstrap); the scene only needs to exist.
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, BootScenePath);
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(BootScenePath, true) };
        }

        private static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "Animatix";
            PlayerSettings.productName = "Toll Road Tycoon";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            // Provisional identifier: verify availability on Google Play before release.
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.animatix.tollroadtycoon");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            if (string.IsNullOrEmpty(PlayerSettings.bundleVersion) || PlayerSettings.bundleVersion == "1.0")
            {
                PlayerSettings.bundleVersion = "0.1.0";
            }
        }
    }
}
