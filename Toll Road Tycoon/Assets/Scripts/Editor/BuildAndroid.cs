using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace TollRoadTycoon.EditorTools
{
    /// <summary>
    /// Command-line Android builds, e.g.
    /// Unity -batchmode -quit -projectPath "Toll Road Tycoon" -executeMethod TollRoadTycoon.EditorTools.BuildAndroid.Apk
    /// Signing is not configured here: release keystores must be supplied by the developer.
    /// </summary>
    public static class BuildAndroid
    {
        [MenuItem("Tools/Toll Road Tycoon/Build Android APK (test)")]
        public static void Apk() => Build(false);

        [MenuItem("Tools/Toll Road Tycoon/Build Android AAB")]
        public static void Aab() => Build(true);

        private static void Build(bool appBundle)
        {
            ProjectSetup.Run();
            EditorUserBuildSettings.buildAppBundle = appBundle;
            Directory.CreateDirectory("Builds/Android");
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ProjectSetup.BootScenePath },
                locationPathName = "Builds/Android/TollRoadTycoon" + (appBundle ? ".aab" : ".apk"),
                target = BuildTarget.Android,
                options = appBundle ? BuildOptions.None : BuildOptions.Development,
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
