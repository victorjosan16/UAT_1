using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using VoxelBloom.Core;

namespace VoxelBloom.EditorTools
{
    /// <summary>One-click project configuration. Preserves existing signing/keystore settings (never touched).</summary>
    public static class VoxelBloomSetup
    {
        const string SettingsDir = "Assets/VoxelBloom/Settings";
        const string ScenePath = "Assets/VoxelBloom/Scenes/Bootstrap/Bootstrap.unity";
        const string PackageId = "com.animatix.voxelbloom3d";

        [MenuItem("Voxel Bloom/Setup Project (URP + Android + Scene)", priority = 1)]
        public static void SetupAll()
        {
            SetupUrp();
            SetupAndroid();
            SetupScene();
            AssetDatabase.SaveAssets();
            Debug.Log("[VoxelBloom] Setup complete. Press Play in " + ScenePath + " (any scene works: the game boots itself).");
        }

        [MenuItem("Voxel Bloom/1. Configure URP")]
        public static void SetupUrp()
        {
            if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset) { Debug.Log("[VoxelBloom] URP already active - kept."); return; }
            Directory.CreateDirectory(SettingsDir);
            var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(rendererData, SettingsDir + "/VoxelBloom_Renderer.asset");
            var asset = UniversalRenderPipelineAsset.Create(rendererData);
            asset.msaaSampleCount = 4;
            asset.supportsHDR = false;
            asset.renderScale = 1f;
            AssetDatabase.CreateAsset(asset, SettingsDir + "/VoxelBloom_URP.asset");
            GraphicsSettings.defaultRenderPipeline = asset;
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = asset;
            }
            Debug.Log("[VoxelBloom] URP asset created and assigned.");
        }

        [MenuItem("Voxel Bloom/2. Configure Android Player")]
        public static void SetupAndroid()
        {
            PlayerSettings.productName = "Voxel Bloom 3D";
            if (string.IsNullOrEmpty(PlayerSettings.companyName) || PlayerSettings.companyName == "DefaultCompany") PlayerSettings.companyName = "Animatix";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, PackageId);
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            if ((int)PlayerSettings.Android.minSdkVersion < 26) PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.forceInternetPermission = false;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            EditorUserBuildSettings.buildAppBundle = true;
            // Signing (keystore, aliases, passwords) is intentionally left untouched.
            Debug.Log("[VoxelBloom] Android: " + PackageId + ", IL2CPP, ARM64, portrait, AAB. Signing untouched.");
        }

        [MenuItem("Voxel Bloom/3. Create Bootstrap Scene")]
        public static void SetupScene()
        {
            if (!File.Exists(ScenePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            var scenes = EditorBuildSettings.scenes.ToList();
            if (!scenes.Any(s => s.path == ScenePath)) scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        [MenuItem("Voxel Bloom/Validate All Levels (solver)")]
        public static void ValidateLevels()
        {
            int ok = 0, bad = 0;
            for (int id = 1; id <= 120; id++)
            {
                var ta = Resources.Load<TextAsset>("VoxelBloom/Levels/level_" + id.ToString("000"));
                if (ta == null) { Debug.LogError("Missing level " + id); bad++; continue; }
                try
                {
                    var l = LevelDefinition.FromJson(ta.text);
                    var r = GreedySolver.Solve(l);
                    if (r.Won) ok++;
                    else { bad++; Debug.LogError("Level " + id + " not solved by heuristic: " + r.Fail); }
                }
                catch (System.Exception e) { bad++; Debug.LogError("Level " + id + ": " + e.Message); }
            }
            Debug.Log("[VoxelBloom] Levels valid: " + ok + ", failing: " + bad + " (heuristic witness solver; see Docs/Solver.md).");
        }

        [MenuItem("Voxel Bloom/Delete Save Data")]
        public static void DeleteSave()
        {
            string dir = Path.Combine(Application.persistentDataPath, "save");
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
            Debug.Log("[VoxelBloom] Save data deleted: " + dir);
        }
    }
}
