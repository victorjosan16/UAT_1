using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace QuarryEmpire.EditorTools
{
    /// <summary>Setări de proiect: foldere, layere, URP, Android.</summary>
    public static partial class GameSetup
    {
        private static void EnsureFolders()
        {
            foreach (var path in new[] { MaterialsDir, DataDir, PrefabsDir, ScenesDir, SettingsDir })
                EnsureFolder(path);
        }

        private static void SetupLayers()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");
            layers.GetArrayElementAtIndex(BlockLayer).stringValue = "Block";
            layers.GetArrayElementAtIndex(ResourceLayer).stringValue = "Resource";
            tagManager.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetupRenderPipeline()
        {
            if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset) return;

            string assetPath = SettingsDir + "/URP_Asset.asset";
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(assetPath);
            if (pipeline == null)
            {
                var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                var rendererSo = new SerializedObject(rendererData);
                SerializedProperty postProcess = rendererSo.FindProperty("postProcessData");
                if (postProcess != null)
                {
                    postProcess.objectReferenceValue = AssetDatabase.LoadAssetAtPath<PostProcessData>(
                        "Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset");
                    rendererSo.ApplyModifiedPropertiesWithoutUndo();
                }
                AssetDatabase.CreateAsset(rendererData, SettingsDir + "/URP_Renderer.asset");

                pipeline = UniversalRenderPipelineAsset.Create(rendererData);
                AssetDatabase.CreateAsset(pipeline, assetPath);
            }

            GraphicsSettings.defaultRenderPipeline = pipeline;
        }

        private static void SetupPlayerSettings()
        {
            PlayerSettings.companyName = "VictorJosan";
            PlayerSettings.productName = "Quarry Empire";
            PlayerSettings.bundleVersion = "0.9.0";
            PlayerSettings.Android.bundleVersionCode = 1;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.victorjosan.quarryempire");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            // Active Input Handling = Input System Package (New). Nu are API public, deci îl setăm direct.
            Object[] projectSettings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (projectSettings.Length == 0) return;
            var so = new SerializedObject(projectSettings[0]);
            SerializedProperty inputHandler = so.FindProperty("activeInputHandler");
            if (inputHandler != null && inputHandler.intValue != 1)
            {
                inputHandler.intValue = 1;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
