using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Veliqora.EditorTools
{
    /// <summary>
    /// Android build entry points (menu + command line). Signing secrets are read from environment variables and
    /// are never stored in the project:
    ///   VELIQORA_KEYSTORE_PATH, VELIQORA_KEYSTORE_PASS, VELIQORA_KEY_ALIAS, VELIQORA_KEY_PASS
    /// CLI example:
    ///   Unity -batchmode -quit -projectPath Veliqora -buildTarget Android -executeMethod Veliqora.EditorTools.VeliqoraBuild.BuildReleaseAabCli
    /// </summary>
    public static class VeliqoraBuild
    {
        public const string OutDir = "Builds/Android";

        [MenuItem("Veliqora/Build/Android Debug APK", priority = 30)]
        public static void BuildDebugApkMenu() => BuildDebugApk();

        [MenuItem("Veliqora/Build/Android Release AAB", priority = 31)]
        public static void BuildReleaseAabMenu() => BuildReleaseAab(fromCli: false);

        /// <summary>
        /// Unity Build Automation (Unity Cloud) → Advanced settings → Pre-export method:
        /// <c>Veliqora.EditorTools.VeliqoraBuild.CloudPreExport</c>. The repo does not commit ProjectSettings.asset,
        /// so this applies the player settings (package, portrait, IL2CPP, ARM64, min SDK 26) and makes sure the
        /// 8 scenes are in Build Settings before the cloud build exports the player.
        /// </summary>
        public static void CloudPreExport()
        {
            VeliqoraSetup.EnsureScenes();
            VeliqoraSetup.ConfigurePlayer();
            Debug.Log("[Veliqora] Cloud pre-export: player settings applied, scenes: " + string.Join(", ", Scenes()));
        }

        public static void BuildDebugApkCli() { if (!BuildDebugApk()) EditorApplication.Exit(1); }
        public static void BuildReleaseAabCli() { if (!BuildReleaseAab(fromCli: true)) EditorApplication.Exit(1); }

        private static string[] Scenes()
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0) throw new InvalidOperationException("No scenes in Build Settings — run Veliqora > Setup Project first.");
            return scenes;
        }

        public static bool BuildDebugApk()
        {
            VeliqoraSetup.ConfigurePlayer();
            EditorUserBuildSettings.buildAppBundle = false;
            Directory.CreateDirectory(OutDir);
            var options = new BuildPlayerOptions
            {
                scenes = Scenes(),
                locationPathName = Path.Combine(OutDir, "Veliqora-debug.apk"),
                target = BuildTarget.Android,
                options = BuildOptions.Development,
            };
            return Report(BuildPipeline.BuildPlayer(options));
        }

        public static bool BuildReleaseAab(bool fromCli)
        {
            VeliqoraSetup.ConfigurePlayer();
            var problems = VeliqoraValidator.ValidateProject(true);
            if (problems.Count > 0)
            {
                Debug.LogError("[Veliqora] Release build blocked by validation problems.");
                return false;
            }
            if (!ConfigureSigning())
            {
                string msg = "Release signing not configured. Set VELIQORA_KEYSTORE_PATH, VELIQORA_KEYSTORE_PASS, VELIQORA_KEY_ALIAS, VELIQORA_KEY_PASS (see Docs/BUILD_AND_RELEASE.md).";
                Debug.LogError("[Veliqora] " + msg);
                if (!fromCli) EditorUtility.DisplayDialog("Veliqora", msg, "OK");
                return false;
            }
            EditorUserBuildSettings.buildAppBundle = true;
            Directory.CreateDirectory(OutDir);
            var options = new BuildPlayerOptions
            {
                scenes = Scenes(),
                locationPathName = Path.Combine(OutDir, "Veliqora-" + PlayerSettings.bundleVersion + "-" + PlayerSettings.Android.bundleVersionCode + ".aab"),
                target = BuildTarget.Android,
                options = BuildOptions.None,
            };
            bool ok = Report(BuildPipeline.BuildPlayer(options));
            // Don't leave passwords in memory-backed PlayerSettings longer than needed.
            PlayerSettings.Android.keystorePass = "";
            PlayerSettings.Android.keyaliasPass = "";
            return ok;
        }

        private static bool ConfigureSigning()
        {
            string path = Environment.GetEnvironmentVariable("VELIQORA_KEYSTORE_PATH");
            string storePass = Environment.GetEnvironmentVariable("VELIQORA_KEYSTORE_PASS");
            string alias = Environment.GetEnvironmentVariable("VELIQORA_KEY_ALIAS");
            string aliasPass = Environment.GetEnvironmentVariable("VELIQORA_KEY_PASS");
            if (string.IsNullOrEmpty(path) || !File.Exists(path) || string.IsNullOrEmpty(storePass) || string.IsNullOrEmpty(alias) || string.IsNullOrEmpty(aliasPass))
                return false;
            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = path;
            PlayerSettings.Android.keystorePass = storePass;
            PlayerSettings.Android.keyaliasName = alias;
            PlayerSettings.Android.keyaliasPass = aliasPass;
            return true;
        }

        private static bool Report(BuildReport report)
        {
            var s = report.summary;
            if (s.result == BuildResult.Succeeded)
            {
                Debug.Log("[Veliqora] Build succeeded: " + s.outputPath + " (" + (s.totalSize / (1024f * 1024f)).ToString("0.0") + " MB, " + s.totalTime + ")");
                return true;
            }
            Debug.LogError("[Veliqora] Build " + s.result + " with " + s.totalErrors + " error(s).");
            return false;
        }
    }
}
