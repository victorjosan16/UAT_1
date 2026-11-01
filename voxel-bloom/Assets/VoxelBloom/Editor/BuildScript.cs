using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace VoxelBloom.EditorTools
{
    /// <summary>
    /// Headless build entry points for CI and the command line:
    ///   Unity -batchmode -nographics -quit -projectPath voxel-bloom -buildTarget iOS     -executeMethod VoxelBloom.EditorTools.BuildScript.BuildIOS
    ///   Unity -batchmode -nographics -quit -projectPath voxel-bloom -buildTarget Android -executeMethod VoxelBloom.EditorTools.BuildScript.BuildAndroid
    /// Options come from command-line args (-name value) or environment variables. Signing secrets are only
    /// read at build time and never written into the project (ProjectSettings are not saved by CI builds).
    /// </summary>
    public static class BuildScript
    {
        public const string BundleId = "com.animatix.voxelbloom3d";
        const string DefaultVersion = "1.0.0";

        // ------------------------------------------------------------------ menu shortcuts (local builds)
        [MenuItem("Voxel Bloom/Build/iOS (Xcode project)")]
        public static void MenuIOS() => Build(BuildTarget.iOS, exitOnFinish: false);

        [MenuItem("Voxel Bloom/Build/Android (AAB)")]
        public static void MenuAndroid() => Build(BuildTarget.Android, exitOnFinish: false);

        // ------------------------------------------------------------------ CI entry points
        public static void BuildIOS() => Build(BuildTarget.iOS, exitOnFinish: true);
        public static void BuildAndroid() => Build(BuildTarget.Android, exitOnFinish: true);

        /// <summary>Entry point used by game-ci/unity-builder (it passes -customBuildTarget).</summary>
        public static void BuildFromCI()
        {
            string t = Arg("customBuildTarget", Arg("buildTarget", "Android"));
            Build(t.Equals("iOS", StringComparison.OrdinalIgnoreCase) ? BuildTarget.iOS : BuildTarget.Android, exitOnFinish: true);
        }

        /// <summary>
        /// Unity Build Automation (Cloud Build) "Pre-Export Method":
        ///   VoxelBloom.EditorTools.BuildScript.CloudBuildPreExport
        /// Creates what the repository does not contain (TMP essentials, URP asset, Bootstrap scene in
        /// Build Settings) and applies orientation/IL2CPP/ARM64. Bundle ID, version and signing are left to
        /// the Build Automation target configuration.
        /// </summary>
        public static void CloudBuildPreExport()
        {
            VoxelBloomSetup.ImportTmpEssentials();
            VoxelBloomSetup.SetupUrp();
            VoxelBloomSetup.SetupScene();
            ApplyCommon();
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            if ((int)PlayerSettings.Android.minSdkVersion < 24) PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            PlayerSettings.Android.forceInternetPermission = false;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.iOS.targetOSVersionString = "13.0";
            AssetDatabase.SaveAssets();
            Debug.Log("[VoxelBloom] Cloud Build pre-export setup done.");
        }

        static void Build(BuildTarget target, bool exitOnFinish)
        {
            int code = 1;
            try
            {
                VoxelBloomSetup.ImportTmpEssentials();
                VoxelBloomSetup.SetupUrp();
                VoxelBloomSetup.SetupScene();
                ApplyCommon();
                string output = target == BuildTarget.iOS ? ConfigureIOS() : ConfigureAndroid();

                var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
                if (scenes.Length == 0) throw new Exception("No scenes in Build Settings");

                var options = new BuildPlayerOptions
                {
                    scenes = scenes,
                    target = target,
                    targetGroup = BuildPipeline.GetBuildTargetGroup(target),
                    locationPathName = output,
                    options = Flag("development") ? BuildOptions.Development : BuildOptions.None,
                };
                Debug.Log($"[VoxelBloom] Building {target} v{PlayerSettings.bundleVersion} -> {output}");
                BuildReport report = BuildPipeline.BuildPlayer(options);
                var s = report.summary;
                Debug.Log($"[VoxelBloom] Result: {s.result}, size {s.totalSize / (1024 * 1024)} MB, {s.totalErrors} errors, {s.totalTime}");
                code = s.result == BuildResult.Succeeded ? 0 : 2;
            }
            catch (Exception e)
            {
                Debug.LogError("[VoxelBloom] Build failed: " + e);
                code = 3;
            }
            if (exitOnFinish) EditorApplication.Exit(code);
        }

        // ------------------------------------------------------------------ settings
        static void ApplyCommon()
        {
            PlayerSettings.productName = "Voxel Bloom 3D";
            if (string.IsNullOrEmpty(PlayerSettings.companyName) || PlayerSettings.companyName == "DefaultCompany") PlayerSettings.companyName = "Animatix";
            PlayerSettings.bundleVersion = Arg("buildVersion", Env("VB_VERSION", DefaultVersion));
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.colorSpace = ColorSpace.Linear;
        }

        static string ConfigureIOS()
        {
            var t = NamedBuildTarget.iOS;
            PlayerSettings.SetApplicationIdentifier(t, Env("IOS_BUNDLE_ID", BundleId));
            PlayerSettings.SetScriptingBackend(t, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetArchitecture(t, 1); // 1 = ARM64
            PlayerSettings.iOS.buildNumber = Arg("iosBuildNumber", Env("VB_BUILD_NUMBER", "1"));
            PlayerSettings.iOS.targetOSVersionString = "13.0";
            PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneAndiPad;
            PlayerSettings.iOS.requiresFullScreen = true;
            PlayerSettings.iOS.appleEnableAutomaticSigning = true;
            string team = Env("APPLE_TEAM_ID", "");
            if (!string.IsNullOrEmpty(team)) PlayerSettings.iOS.appleDeveloperTeamID = team;
            PlayerSettings.iOS.statusBarStyle = iOSStatusBarStyle.Default;
            PlayerSettings.iOS.hideHomeButton = false;
            // The game is fully offline: no tracking, no network usage descriptions needed.
            return Arg("customBuildPath", Env("VB_OUTPUT", "Builds/iOS"));
        }

        static string ConfigureAndroid()
        {
            var t = NamedBuildTarget.Android;
            PlayerSettings.SetApplicationIdentifier(t, BundleId);
            PlayerSettings.SetScriptingBackend(t, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            if ((int)PlayerSettings.Android.minSdkVersion < 24) PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.forceInternetPermission = false;
            int versionCode = int.TryParse(Arg("androidVersionCode", Env("VB_BUILD_NUMBER", "1")), out var vc) ? vc : 1;
            PlayerSettings.Android.bundleVersionCode = Math.Max(1, versionCode);

            bool aab = !Flag("apk") && Env("VB_ANDROID_FORMAT", "aab") != "apk";
            EditorUserBuildSettings.buildAppBundle = aab;

            // Signing: only from CI secrets / args. Never stored in the repository.
            string ks = Arg("androidKeystoreName", Env("ANDROID_KEYSTORE_NAME", ""));
            if (!string.IsNullOrEmpty(ks) && File.Exists(ks))
            {
                PlayerSettings.Android.useCustomKeystore = true;
                PlayerSettings.Android.keystoreName = Path.GetFullPath(ks);
                PlayerSettings.Android.keystorePass = Arg("androidKeystorePass", Env("ANDROID_KEYSTORE_PASS", ""));
                PlayerSettings.Android.keyaliasName = Arg("androidKeyaliasName", Env("ANDROID_KEYALIAS_NAME", ""));
                PlayerSettings.Android.keyaliasPass = Arg("androidKeyaliasPass", Env("ANDROID_KEYALIAS_PASS", ""));
                Debug.Log("[VoxelBloom] Android: signing with provided keystore.");
            }
            else Debug.LogWarning("[VoxelBloom] Android: no keystore provided - debug-signed build (not uploadable to Google Play).");

            string def = aab ? "Builds/Android/VoxelBloom3D.aab" : "Builds/Android/VoxelBloom3D.apk";
            string path = Arg("customBuildPath", Env("VB_OUTPUT", def));
            if (Directory.Exists(path) || !Path.HasExtension(path)) path = Path.Combine(path, Path.GetFileName(def));
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            return path;
        }

        // ------------------------------------------------------------------ helpers
        static string Arg(string name, string fallback)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == "-" + name && !args[i + 1].StartsWith("-")) return args[i + 1];
            return fallback;
        }

        static bool Flag(string name) => Environment.GetCommandLineArgs().Contains("-" + name);

        static string Env(string name, string fallback)
        {
            var v = Environment.GetEnvironmentVariable(name);
            return string.IsNullOrEmpty(v) ? fallback : v;
        }
    }
}
