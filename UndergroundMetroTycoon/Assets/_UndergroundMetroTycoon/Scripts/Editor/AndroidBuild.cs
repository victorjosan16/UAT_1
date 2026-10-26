using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace UndergroundMetroTycoon.EditorTools
{
    /// <summary>Builds a development APK of the Phase 1 scene, from the menu or the command line.</summary>
    public static class AndroidBuild
    {
        public const string OutputPath = "Builds/Android/UndergroundMetroTycoon-phase1.apk";

        [MenuItem("Underground Metro Tycoon/Build Android Test APK", priority = 20)]
        public static void BuildFromMenu()
        {
            BuildReport report = Build();
            if (report != null && report.summary.result == BuildResult.Succeeded) EditorUtility.RevealInFinder(OutputPath);
        }

        /// <summary>Unity -batchmode -quit -projectPath . -executeMethod UndergroundMetroTycoon.EditorTools.AndroidBuild.BuildFromCommandLine</summary>
        public static void BuildFromCommandLine()
        {
            ProjectSetup.RunSetupBatch();
            BuildReport report = Build();
            EditorApplication.Exit(report != null && report.summary.result == BuildResult.Succeeded ? 0 : 1);
        }

        private static BuildReport Build()
        {
            if (!File.Exists(ProjectSetup.ScenePath))
            {
                Debug.LogError("[UMT] Scene missing. Run 'Underground Metro Tycoon > Setup Project (Phase 1)' first.");
                return null;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath) ?? "Builds");
            EditorUserBuildSettings.buildAppBundle = false;
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ProjectSetup.ScenePath },
                locationPathName = OutputPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.Development
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            Debug.Log($"[UMT] Android build {report.summary.result}: {report.summary.totalSize / (1024 * 1024)} MB, {report.summary.totalErrors} errors, output {OutputPath}");
            return report;
        }
    }
}
