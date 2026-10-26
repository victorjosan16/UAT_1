using System.IO;
using UndergroundMetroTycoon.Game;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
#if UMT_URP
using UnityEngine.Rendering.Universal;
#endif

namespace UndergroundMetroTycoon.EditorTools
{
    /// <summary>
    /// One-click project configuration: URP pipeline asset, base material, GameConfig asset, the Phase 1 scene,
    /// build settings and Android player settings. Safe to run repeatedly; existing assets are reused.
    /// </summary>
    public static class ProjectSetup
    {
        public const string Root = "Assets/_UndergroundMetroTycoon";
        public const string ScenePath = Root + "/Scenes/Phase1_ConstructionSite.unity";
        private const string ConfigPath = Root + "/ScriptableObjects/GameConfig.asset";
        private const string MaterialPath = Root + "/Materials/UMT_BaseLit.mat";
        private const string PipelinePath = Root + "/Settings/UMT_URP_Mobile.asset";
        private const string RendererPath = Root + "/Settings/UMT_URP_Mobile_Renderer.asset";

        [MenuItem("Underground Metro Tycoon/Setup Project (Phase 1)", priority = 0)]
        public static void RunSetup()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            RunSetupInternal();
        }

        /// <summary>Entry point for command-line use: -executeMethod UndergroundMetroTycoon.EditorTools.ProjectSetup.RunSetupBatch</summary>
        public static void RunSetupBatch() => RunSetupInternal();

        private static void RunSetupInternal()
        {
            EnsureFolder(Root + "/Scenes");
            EnsureFolder(Root + "/ScriptableObjects");
            EnsureFolder(Root + "/Materials");
            EnsureFolder(Root + "/Settings");

            EnsureRenderPipeline();
            Material material = EnsureBaseMaterial();
            GameConfig config = EnsureConfig();
            CreateScene(config, material);
            ConfigurePlayerSettings();
            bool restartNeeded = EnsureLegacyInputEnabled();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[UMT] Project setup complete. Open " + ScenePath + " and press Play.");
            if (restartNeeded && !Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("Underground Metro Tycoon",
                    "Active Input Handling was switched to 'Both' so the touch joystick works. Please restart the Unity Editor.", "OK");
            }
        }

        private static void EnsureRenderPipeline()
        {
#if UMT_URP
            if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset) return;
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(rendererData, RendererPath);
                pipeline = UniversalRenderPipelineAsset.Create(rendererData);
                pipeline.shadowDistance = 35f;
                pipeline.msaaSampleCount = 2;
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
            }
            GraphicsSettings.defaultRenderPipeline = pipeline;
            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipeline;
            }
            QualitySettings.SetQualityLevel(current, false);
            Debug.Log("[UMT] Assigned URP pipeline asset " + PipelinePath);
#else
            Debug.LogWarning("[UMT] URP package not found; the game will run on the Built-in render pipeline.");
#endif
        }

        private static Material EnsureBaseMaterial()
        {
            bool urp = GraphicsSettings.defaultRenderPipeline != null;
            Shader shader = urp
                ? Shader.Find("Universal Render Pipeline/Simple Lit") ?? Shader.Find("Universal Render Pipeline/Lit")
                : Shader.Find("Standard");
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(shader) { name = "UMT_BaseLit" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            else if (shader != null && material.shader != shader)
            {
                material.shader = shader;
            }
            material.enableInstancing = true;
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.12f);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.12f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameConfig EnsureConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            if (config != null) return config;
            config = ScriptableObject.CreateInstance<GameConfig>();
            config.ResetToDefaults();
            AssetDatabase.CreateAsset(config, ConfigPath);
            return config;
        }

        private static void CreateScene(GameConfig config, Material material)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var go = new GameObject("GameBootstrap");
            var bootstrap = go.AddComponent<GameBootstrap>();
            var so = new SerializedObject(bootstrap);
            so.FindProperty("config").objectReferenceValue = config;
            so.FindProperty("baseMaterial").objectReferenceValue = material;
            so.ApplyModifiedPropertiesWithoutUndo();

            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            // Saved with the scene so the fog shader variants used at runtime survive build stripping.
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 48f;
            RenderSettings.fogEndDistance = 120f;
            EditorSceneManager.SaveScene(scene, ScenePath);

            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath, true) };
            foreach (EditorBuildSettingsScene s in EditorBuildSettings.scenes)
                if (s.path != ScenePath) scenes.Add(s);
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void ConfigurePlayerSettings()
        {
            PlayerSettings.productName = "Underground Metro Tycoon";
            if (string.IsNullOrEmpty(PlayerSettings.companyName) || PlayerSettings.companyName == "DefaultCompany")
                PlayerSettings.companyName = "UndergroundMetro";

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;

            string id = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
            if (string.IsNullOrEmpty(id) || id.Contains("DefaultCompany") || id == "com.Company.ProductName")
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.undergroundmetro.tycoon");

            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        }

        /// <summary>The joystick uses UGUI with the StandaloneInputModule, which requires the legacy Input Manager.</summary>
        private static bool EnsureLegacyInputEnabled()
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (assets == null || assets.Length == 0) return false;
            var so = new SerializedObject(assets[0]);
            SerializedProperty handler = so.FindProperty("activeInputHandler");
            if (handler == null || handler.intValue != 1) return false; // 0 = old, 1 = new only, 2 = both
            handler.intValue = 2;
            so.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }

    /// <summary>Offers to run the setup the first time the project is opened without the Phase 1 scene.</summary>
    [InitializeOnLoad]
    internal static class FirstOpenSetupPrompt
    {
        static FirstOpenSetupPrompt()
        {
            if (Application.isBatchMode) return;
            EditorApplication.delayCall += () =>
            {
                if (File.Exists(ProjectSetup.ScenePath)) return;
                string key = "UMT.SetupPrompted." + Application.dataPath.GetHashCode();
                if (EditorPrefs.GetBool(key, false)) return;
                EditorPrefs.SetBool(key, true);
                if (EditorUtility.DisplayDialog("Underground Metro Tycoon",
                        "Create the Phase 1 scene, URP settings and game config now?\n(You can run it later from the 'Underground Metro Tycoon' menu.)",
                        "Set up now", "Later"))
                {
                    ProjectSetup.RunSetup();
                }
            };
        }
    }
}
