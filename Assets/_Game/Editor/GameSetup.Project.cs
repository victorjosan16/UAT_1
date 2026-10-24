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
            foreach (var path in new[] { MaterialsDir, MeshesDir, FontsDir, ResourcesDir, DataDir, PrefabsDir, ScenesDir, SettingsDir })
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
            if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset existing)
            {
                ConfigurePipeline(existing);
                return;
            }

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

            ConfigurePipeline(pipeline);
            GraphicsSettings.defaultRenderPipeline = pipeline;
        }

        /// <summary>
        /// Setări URP potrivite pentru mobil (valorile de pornire; QualityProfile le ajustează la rulare):
        /// fără HDR, o singură cascadă de umbre, distanță de umbre mică, MSAA 2x.
        /// </summary>
        private static void ConfigurePipeline(UniversalRenderPipelineAsset pipeline)
        {
            pipeline.supportsHDR = false;
            pipeline.shadowCascadeCount = 1;
            pipeline.shadowDistance = 30f;
            pipeline.msaaSampleCount = 2;
            pipeline.renderScale = 1f;
            EditorUtility.SetDirty(pipeline);
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

            // iOS: același identificator; jocul e doar în portret (zona sigură e tratată de SafeAreaFitter).
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.victorjosan.quarryempire");
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            SetupIconAndSplash();

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

        /// <summary>Iconița aplicației (Art/Icon/AppIcon.png) și ecranul de pornire cu logo-ul jocului.</summary>
        private static void SetupIconAndSplash()
        {
            const string iconPath = Root + "/Art/Icon/AppIcon.png";
            const string squarePath = Root + "/Art/Icon/AppIconSquare.png";
            ConfigureTexture(iconPath, TextureImporterType.Sprite);
            ConfigureTexture(squarePath, TextureImporterType.Default);

            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
            var square = AssetDatabase.LoadAssetAtPath<Texture2D>(squarePath);
            var logo = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            if (icon != null) PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
            // iOS nu acceptă transparență în iconiță: folosim varianta pătrată.
            if (square != null) PlayerSettings.SetIcons(NamedBuildTarget.iOS, new[] { square }, IconKind.Any);

            PlayerSettings.SplashScreen.show = true;
            PlayerSettings.SplashScreen.showUnityLogo = false; // permis în Unity 6 Personal; altfel Unity îl afișează oricum
            PlayerSettings.SplashScreen.backgroundColor = new Color(0.95f, 0.55f, 0.24f);
            PlayerSettings.SplashScreen.animationMode = PlayerSettings.SplashScreen.AnimationMode.Dolly;
            if (logo != null) PlayerSettings.SplashScreen.logos = new[] { PlayerSettings.SplashScreenLogo.Create(1.5f, logo) };
        }

        private static void ConfigureTexture(string path, TextureImporterType type)
        {
            if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) return;
            if (importer.textureType == type && !importer.mipmapEnabled && importer.alphaIsTransparency) return;
            importer.textureType = type;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }
}
