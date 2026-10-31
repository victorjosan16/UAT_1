using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace TollRoadTycoon.EditorTools
{
    /// <summary>
    /// Android builds from the menu, the command line or CI, e.g.
    /// Unity -batchmode -quit -projectPath "Toll Road Tycoon" -executeMethod TollRoadTycoon.EditorTools.BuildAndroid.Apk
    /// CI uses BuildAndroid.Ci with optional arguments:
    ///   -trtFormat apk|aab  -trtBuildNumber N
    ///   -trtKeystore path -trtKeystorePass pass -trtKeyAlias alias -trtKeyAliasPass pass
    /// Without a keystore the build is signed with Unity's debug key (fine for testing, not for Google Play).
    /// </summary>
    public static class BuildAndroid
    {
        public const string OutputFolder = "Builds/Android";

        [MenuItem("Tools/Toll Road Tycoon/Build Android APK (test)")]
        public static void Apk() => Build(false, true);

        [MenuItem("Tools/Toll Road Tycoon/Build Android AAB")]
        public static void Aab() => Build(true, false);

        /// <summary>Entry point for automated builds (GitHub Actions / GameCI).</summary>
        public static void Ci()
        {
            bool aab = string.Equals(Arg("-trtFormat"), "aab", StringComparison.OrdinalIgnoreCase);
            if (int.TryParse(Arg("-trtBuildNumber"), out int buildNumber) && buildNumber > 0)
            {
                PlayerSettings.Android.bundleVersionCode = buildNumber;
            }
            ApplyKeystore();
            // APKs are test builds (test ads, development player); AABs are release builds for Google Play.
            Build(aab, !aab);
        }

        private static void ApplyKeystore()
        {
            string keystore = Arg("-trtKeystore");
            if (string.IsNullOrEmpty(keystore) || !File.Exists(keystore))
            {
                PlayerSettings.Android.useCustomKeystore = false;
                Debug.Log("[Toll Road Tycoon] No keystore given: signing with the debug key.");
                return;
            }
            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = Path.GetFullPath(keystore);
            PlayerSettings.Android.keystorePass = Arg("-trtKeystorePass");
            PlayerSettings.Android.keyaliasName = Arg("-trtKeyAlias");
            PlayerSettings.Android.keyaliasPass = Arg("-trtKeyAliasPass");
            Debug.Log("[Toll Road Tycoon] Signing with the provided keystore.");
        }

        private static string Arg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name) return args[i + 1];
            }
            return null;
        }

        private static void Build(bool appBundle, bool development)
        {
            ProjectSetup.Run();
            EditorUserBuildSettings.buildAppBundle = appBundle;
            Directory.CreateDirectory(OutputFolder);
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ProjectSetup.BootScenePath },
                locationPathName = OutputFolder + "/TollRoadTycoon" + (appBundle ? ".aab" : ".apk"),
                target = BuildTarget.Android,
                options = development ? BuildOptions.Development : BuildOptions.None,
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            Debug.Log($"[Toll Road Tycoon] Android build {report.summary.result}: {report.summary.outputPath} " +
                      $"({report.summary.totalSize / (1024 * 1024)} MB, {report.summary.totalErrors} errors)");
            if (Application.isBatchMode && report.summary.result != BuildResult.Succeeded)
            {
                throw new Exception("Android build failed: " + report.summary.result);
            }
        }
    }
}
