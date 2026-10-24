using System.Collections.Generic;
using System.IO;
using LastBlock.Core;
using LastBlock.Effects;
using LastBlock.Gameplay;
using LastBlock.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;
#if LASTBLOCK_URP
using UnityEngine.Rendering.Universal;
#endif

namespace LastBlock.EditorTools
{
    /// <summary>
    /// Tools → Last Block → Setup Game.
    /// Generates textures, materials, the block prefab and the complete MainGame scene, then wires every
    /// reference. Safe to run repeatedly: assets are updated in place (GUIDs kept) and the scene is rebuilt
    /// from scratch, so nothing is duplicated.
    /// </summary>
    public static class LastBlockSetup
    {
        const string ScenePath = "Assets/Scenes/MainGame.unity";
        const string MaterialsDir = "Assets/Materials";
        const string TexturesDir = "Assets/Materials/Textures";
        const string PrefabsDir = "Assets/Prefabs";
        const string SettingsDir = "Assets/Settings";
        const string BlockPrefabPath = "Assets/Prefabs/Block.prefab";

        static readonly Color[] BlockPalette =
        {
            new Color(0.95f, 0.45f, 0.40f), // coral
            new Color(0.98f, 0.68f, 0.32f), // orange
            new Color(0.98f, 0.85f, 0.40f), // yellow
            new Color(0.45f, 0.82f, 0.60f), // mint
            new Color(0.42f, 0.70f, 0.95f), // sky
            new Color(0.70f, 0.55f, 0.90f)  // lavender
        };

        static readonly Color TextDark = new Color(0.12f, 0.2f, 0.35f);
        static readonly Color ButtonBlue = new Color(0.25f, 0.5f, 0.95f);
        static readonly Color ButtonCoral = new Color(0.95f, 0.42f, 0.38f);
        static readonly Color ButtonGreen = new Color(0.3f, 0.75f, 0.45f);

        [MenuItem("Tools/Last Block/Setup Game", priority = 0)]
        public static void SetupGame()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!EnsureTextMeshProResources()) return;

            EnsureFolder(MaterialsDir);
            EnsureFolder(TexturesDir);
            EnsureFolder(PrefabsDir);
            EnsureFolder("Assets/Scenes");

            bool urp = EnsureRenderPipeline();
            ConfigurePlayerSettings();

            // ---- assets
            Texture2D woodTexture = CreateTextureAsset(TexturesDir + "/WoodGrain.png", GenerateWoodTexture(256), TextureWrapMode.Repeat);
            Texture2D gradientTexture = CreateTextureAsset(TexturesDir + "/BackgroundGradient.png", GenerateGradientTexture(), TextureWrapMode.Clamp);

            Shader litShader = FindShader(urp ? "Universal Render Pipeline/Lit" : "Standard", "Standard");
            Shader unlitShader = FindShader(urp ? "Universal Render Pipeline/Unlit" : "Unlit/Texture", "Unlit/Texture");
            Shader particleShader = FindShader(urp ? "Universal Render Pipeline/Particles/Unlit" : "Particles/Standard Unlit", "Sprites/Default");

            Material[] blockMaterials = new Material[BlockPalette.Length];
            for (int i = 0; i < BlockPalette.Length; i++)
            {
                Material material = CreateOrUpdateMaterial($"{MaterialsDir}/Block_{i:00}.mat", litShader);
                SetColor(material, BlockPalette[i]);
                SetTexture(material, woodTexture);
                SetSmoothness(material, 0.25f);
                blockMaterials[i] = material;
            }

            Material platformMaterial = CreateOrUpdateMaterial(MaterialsDir + "/Platform.mat", litShader);
            SetColor(platformMaterial, new Color(0.93f, 0.95f, 1f));
            SetSmoothness(platformMaterial, 0.1f);

            Material backgroundMaterial = CreateOrUpdateMaterial(MaterialsDir + "/Background.mat", unlitShader);
            SetColor(backgroundMaterial, Color.white);
            SetTexture(backgroundMaterial, gradientTexture);

            Material particleMaterial = CreateOrUpdateMaterial(MaterialsDir + "/Particles.mat", particleShader);
            SetColor(particleMaterial, Color.white);

            AssetDatabase.SaveAssets();

            // ---- scene (rebuilt from scratch every run → no duplicates)
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject blockPrefab = CreateBlockPrefab(blockMaterials[0]);

            CreateLighting();
            CreatePlatform(platformMaterial);
            CameraController cameraController = CreateCamera(backgroundMaterial);

            GameObject towerRoot = new GameObject("Tower");
            TowerGenerator generator = towerRoot.AddComponent<TowerGenerator>();
            Assign(generator, "blockPrefab", blockPrefab);
            AssignArray(generator, "blockMaterials", blockMaterials);

            GameObject systems = new GameObject("GameSystems");
            GameManager gameManager = systems.AddComponent<GameManager>();
            LevelManager levelManager = systems.AddComponent<LevelManager>();
            ScoreManager scoreManager = systems.AddComponent<ScoreManager>();
            TowerStabilityChecker checker = systems.AddComponent<TowerStabilityChecker>();
            BlockDragController drag = systems.AddComponent<BlockDragController>();
            SimpleEffects effects = systems.AddComponent<SimpleEffects>(); // RequireComponent adds the AudioSource.
            Assign(effects, "particleMaterial", particleMaterial);

            Assign(drag, "worldCamera", cameraController.GetComponent<Camera>());
            Assign(drag, "cameraController", cameraController);
            Assign(drag, "towerGenerator", generator);

            UIManager uiManager = CreateUI();
            CreateEventSystem();

            Assign(gameManager, "towerGenerator", generator);
            Assign(gameManager, "stabilityChecker", checker);
            Assign(gameManager, "dragController", drag);
            Assign(gameManager, "levelManager", levelManager);
            Assign(gameManager, "scoreManager", scoreManager);
            Assign(gameManager, "uiManager", uiManager);
            Assign(gameManager, "cameraController", cameraController);
            Assign(gameManager, "effects", effects);

            // Preview tower so the scene looks right in edit mode; replaced at runtime by the current level.
            LevelConfig preview = LevelManager.GetLevelConfig(1);
            generator.Generate(preview);
            cameraController.FrameTower(generator.TowerCenter, generator.GetTowerHeight(preview.layers), generator.BlockLength, true);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[Last Block] Setup complete. Scene saved to {ScenePath}. Render pipeline: {(urp ? "URP" : "Built-in (URP not found)")}. Press Play.");
        }

        [MenuItem("Tools/Last Block/Reset Saved Progress", priority = 20)]
        public static void ResetProgress()
        {
            PlayerPrefs.DeleteKey(LevelManager.HighestUnlockedKey);
            PlayerPrefs.DeleteKey(ScoreManager.BestScoreKey);
            PlayerPrefs.Save();
            Debug.Log("[Last Block] Saved progress cleared.");
        }

        // ================================================================ project settings

        /// <summary>TMP needs its essential resources (default font + settings) before UI text can be created.</summary>
        static bool EnsureTextMeshProResources()
        {
            if (Resources.Load<TMP_Settings>("TMP Settings") != null) return true;

            string[] packagePaths =
            {
                "Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage",
                "Packages/com.unity.textmeshpro/Package Resources/TMP Essential Resources.unitypackage"
            };
            foreach (string path in packagePaths)
            {
                if (!File.Exists(Path.GetFullPath(path))) continue;
                AssetDatabase.ImportPackage(path, false);
                EditorUtility.DisplayDialog("Last Block Setup",
                    "TextMesh Pro essential resources are being imported.\n\nWhen the import finishes, run Tools → Last Block → Setup Game again.",
                    "OK");
                return false;
            }

            EditorApplication.ExecuteMenuItem("Window/TextMeshPro/Import TMP Essential Resources");
            EditorUtility.DisplayDialog("Last Block Setup",
                "Please click 'Import TMP Essentials' in the TextMesh Pro window, then run Tools → Last Block → Setup Game again.",
                "OK");
            return false;
        }

        /// <summary>Makes sure a URP asset is active. Returns false if the project falls back to the built-in pipeline.</summary>
        static bool EnsureRenderPipeline()
        {
            if (GraphicsSettings.currentRenderPipeline != null) return true;

            RenderPipelineAsset asset = null;
            string[] guids = AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset");
            if (guids.Length > 0)
                asset = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));

#if LASTBLOCK_URP
            if (asset == null)
            {
                EnsureFolder(SettingsDir);
                UniversalRendererData rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(rendererData, SettingsDir + "/LastBlock_Renderer.asset");
                UniversalRenderPipelineAsset urpAsset = UniversalRenderPipelineAsset.Create(rendererData);
                urpAsset.shadowDistance = 25f;
                AssetDatabase.CreateAsset(urpAsset, SettingsDir + "/LastBlock_URP.asset");
                AssetDatabase.SaveAssets();
                asset = urpAsset;
            }
#endif
            if (asset == null)
            {
                Debug.LogWarning("[Last Block] URP is not installed. Using the built-in render pipeline (Standard shader).");
                return false;
            }

            GraphicsSettings.defaultRenderPipeline = asset;
            if (QualitySettings.renderPipeline == null) QualitySettings.renderPipeline = asset;
            return true;
        }

        static void ConfigurePlayerSettings()
        {
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.productName = "Last Block";

            string androidId = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
            if (string.IsNullOrEmpty(androidId) || androidId.Contains("DefaultCompany"))
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.lastblock.game");
            string iosId = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.iOS);
            if (string.IsNullOrEmpty(iosId) || iosId.Contains("DefaultCompany"))
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.lastblock.game");
        }

        static void AddSceneToBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath, true) };
            foreach (EditorBuildSettingsScene existing in EditorBuildSettings.scenes)
            {
                if (existing.path != ScenePath) scenes.Add(existing);
            }
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        // ================================================================ assets

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        static Shader FindShader(string preferred, string fallback)
        {
            Shader shader = Shader.Find(preferred);
            if (shader == null) shader = Shader.Find(fallback);
            if (shader == null) Debug.LogError($"[Last Block] Shader not found: {preferred} / {fallback}");
            return shader;
        }

        static Material CreateOrUpdateMaterial(string path, Shader shader)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else if (shader != null && material.shader != shader)
            {
                material.shader = shader;
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        static void SetColor(Material material, Color color)
        {
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        }

        static void SetTexture(Material material, Texture texture)
        {
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
        }

        static void SetSmoothness(Material material, float value)
        {
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", value);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", value);
        }

        static Texture2D CreateTextureAsset(string path, Texture2D source, TextureWrapMode wrapMode)
        {
            File.WriteAllBytes(path, source.EncodeToPNG());
            Object.DestroyImmediate(source);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                importer.textureType = TextureImporterType.Default;
                importer.wrapMode = wrapMode;
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = wrapMode == TextureWrapMode.Repeat;
                importer.sRGBTexture = true;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>Light, near-white wood grain so the block colour tint stays vivid.</summary>
        static Texture2D GenerateWoodTexture(int size)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)size;
                    float v = y / (float)size;
                    float warp = Mathf.PerlinNoise(u * 3f, v * 12f) * 2f;
                    float rings = Mathf.Sin((v * 14f + warp) * Mathf.PI) * 0.5f + 0.5f;
                    float fine = Mathf.PerlinNoise(u * 40f + 10f, v * 160f);
                    float value = Mathf.Lerp(0.8f, 1f, rings * 0.65f + fine * 0.35f);
                    pixels[y * size + x] = new Color(value, value * 0.97f, value * 0.92f, 1f);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        /// <summary>Soft blue vertical gradient (light at the bottom, deeper blue at the top).</summary>
        static Texture2D GenerateGradientTexture()
        {
            const int height = 256;
            Texture2D texture = new Texture2D(4, height, TextureFormat.RGBA32, false);
            Color bottom = new Color(0.88f, 0.94f, 1f);
            Color top = new Color(0.52f, 0.72f, 0.95f);
            for (int y = 0; y < height; y++)
            {
                Color color = Color.Lerp(bottom, top, Mathf.SmoothStep(0f, 1f, y / (float)(height - 1)));
                for (int x = 0; x < 4; x++) texture.SetPixel(x, y, color);
            }
            texture.Apply();
            return texture;
        }

        static GameObject CreateBlockPrefab(Material defaultMaterial)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = "Block";
            block.GetComponent<MeshRenderer>().sharedMaterial = defaultMaterial;

            Rigidbody body = block.AddComponent<Rigidbody>();
            body.mass = 1f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.Discrete;
            block.AddComponent<BlockController>();

            // Overwriting keeps the prefab GUID, so existing references stay valid.
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(block, BlockPrefabPath);
            Object.DestroyImmediate(block);
            return prefab;
        }

        // ================================================================ scene objects

        static void CreateLighting()
        {
            GameObject lightObject = new GameObject("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.96f, 0.9f);
            light.intensity = 1.3f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.65f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -35f, 0f);

            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.78f, 0.86f, 1f);
            RenderSettings.ambientEquatorColor = new Color(0.68f, 0.74f, 0.86f);
            RenderSettings.ambientGroundColor = new Color(0.48f, 0.5f, 0.56f);
            RenderSettings.fog = false;
        }

        static void CreatePlatform(Material material)
        {
            GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            platform.name = "Platform";
            // Cylinder mesh is 2 units tall: scale Y 0.15 → 0.3 thick, top surface exactly at y = 0.
            platform.transform.position = new Vector3(0f, -0.15f, 0f);
            platform.transform.localScale = new Vector3(2.6f, 0.15f, 2.6f);
            platform.GetComponent<MeshRenderer>().sharedMaterial = material;

            // The default CapsuleCollider is rounded; a box gives a perfectly flat top.
            Object.DestroyImmediate(platform.GetComponent<Collider>());
            BoxCollider box = platform.AddComponent<BoxCollider>();
            box.center = Vector3.zero;
            box.size = new Vector3(1f, 2f, 1f);
        }

        static CameraController CreateCamera(Material backgroundMaterial)
        {
            GameObject cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.7f, 0.82f, 0.97f);
            camera.fieldOfView = 45f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 60f;
            cameraObject.AddComponent<AudioListener>();

            GameObject background = GameObject.CreatePrimitive(PrimitiveType.Quad);
            background.name = "BackgroundGradient";
            Object.DestroyImmediate(background.GetComponent<Collider>()); // Must never block raycasts.
            background.transform.SetParent(cameraObject.transform, false);
            MeshRenderer renderer = background.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = backgroundMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            CameraController controller = cameraObject.AddComponent<CameraController>();
            Assign(controller, "backgroundQuad", background.transform);
            return controller;
        }

        static void CreateEventSystem()
        {
            GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem));
            bool added = false;
#if ENABLE_INPUT_SYSTEM
            // Resolved by reflection so this tool compiles with or without the Input System package.
            System.Type moduleType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (moduleType != null)
            {
                eventSystem.AddComponent(moduleType);
                added = true;
            }
#endif
            if (!added) eventSystem.AddComponent<StandaloneInputModule>();
        }

        // ================================================================ UI

        static UIManager CreateUI()
        {
            GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform));
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();
            UIManager ui = canvasObject.AddComponent<UIManager>();

            RectTransform safeArea = CreateRect("SafeArea", canvasObject.transform);
            Stretch(safeArea);
            safeArea.gameObject.AddComponent<SafeAreaFitter>();

            // ---- HUD (top)
            TMP_Text levelText = CreateText("LevelText", safeArea, "LEVEL 1", 84, TextDark, true);
            Place(levelText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(1000f, 110f));
            TMP_Text scoreText = CreateText("ScoreText", safeArea, "SCORE: 0", 52, TextDark, false);
            Place(scoreText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -200f), new Vector2(1000f, 70f));
            TMP_Text removeText = CreateText("RemoveText", safeArea, "REMOVE: 2 BLOCKS", 52, new Color(0.85f, 0.35f, 0.3f), true);
            Place(removeText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -270f), new Vector2(1000f, 70f));

            TMP_Text holdText = CreateText("HoldText", safeArea, "HOLD STEADY... 5", 64, new Color(0.95f, 0.55f, 0.1f), true);
            Place(holdText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -370f), new Vector2(1000f, 90f));
            holdText.gameObject.SetActive(false);

            TMP_Text toastText = CreateText("ToastText", safeArea, "", 60, TextDark, true);
            Place(toastText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 330f), new Vector2(1000f, 90f));
            toastText.gameObject.SetActive(false);

            // ---- bottom
            Button restartButton = CreateButton("RestartButton", safeArea, "RESTART", ButtonBlue, out _);
            Place((RectTransform)restartButton.transform, new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(460f, 140f));

            // ---- victory panel
            GameObject victoryPanel = CreatePanel("VictoryPanel", canvasObject.transform, out RectTransform victoryCard);
            TMP_Text victoryTitle = CreateText("Title", victoryCard, "LEVEL COMPLETE!", 76, new Color(0.25f, 0.65f, 0.35f), true);
            Place(victoryTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -130f), new Vector2(820f, 120f));
            TMP_Text victoryScore = CreateText("Score", victoryCard, "SCORE: 0", 56, TextDark, false);
            Place(victoryScore.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(820f, 160f));
            Button nextButton = CreateButton("NextButton", victoryCard, "NEXT LEVEL", ButtonGreen, out TMP_Text nextLabel);
            Place((RectTransform)nextButton.transform, new Vector2(0.5f, 0f), new Vector2(0f, 130f), new Vector2(520f, 150f));
            victoryPanel.SetActive(false);

            // ---- game over panel
            GameObject gameOverPanel = CreatePanel("GameOverPanel", canvasObject.transform, out RectTransform gameOverCard);
            TMP_Text gameOverTitle = CreateText("Title", gameOverCard, "TOWER COLLAPSED!", 74, ButtonCoral, true);
            Place(gameOverTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -130f), new Vector2(820f, 120f));
            TMP_Text gameOverScore = CreateText("Score", gameOverCard, "SCORE: 0", 56, TextDark, false);
            Place(gameOverScore.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(820f, 160f));
            Button tryAgainButton = CreateButton("TryAgainButton", gameOverCard, "TRY AGAIN", ButtonCoral, out _);
            Place((RectTransform)tryAgainButton.transform, new Vector2(0.5f, 0f), new Vector2(0f, 130f), new Vector2(520f, 150f));
            gameOverPanel.SetActive(false);

            Assign(ui, "levelText", levelText);
            Assign(ui, "scoreText", scoreText);
            Assign(ui, "removeText", removeText);
            Assign(ui, "holdText", holdText);
            Assign(ui, "toastText", toastText);
            Assign(ui, "restartButton", restartButton);
            Assign(ui, "victoryPanel", victoryPanel);
            Assign(ui, "victoryCard", victoryCard);
            Assign(ui, "victoryTitleText", victoryTitle);
            Assign(ui, "victoryScoreText", victoryScore);
            Assign(ui, "nextButton", nextButton);
            Assign(ui, "nextButtonLabel", nextLabel);
            Assign(ui, "gameOverPanel", gameOverPanel);
            Assign(ui, "gameOverCard", gameOverCard);
            Assign(ui, "gameOverScoreText", gameOverScore);
            Assign(ui, "tryAgainButton", tryAgainButton);
            return ui;
        }

        static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static void Place(RectTransform rect, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        static TMP_Text CreateText(string name, Transform parent, string text, float size, Color color, bool bold)
        {
            RectTransform rect = CreateRect(name, parent);
            TextMeshProUGUI tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            // HUD text must not swallow taps meant for the tower.
            tmp.raycastTarget = false;
            return tmp;
        }

        static Button CreateButton(string name, Transform parent, string label, Color color, out TMP_Text labelText)
        {
            RectTransform rect = CreateRect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            image.type = Image.Type.Sliced;
            image.color = color;
            Button button = rect.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.95f, 0.95f, 0.95f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            button.colors = colors;

            labelText = CreateText("Label", rect, label, 54, Color.white, true);
            Stretch(labelText.rectTransform);
            return button;
        }

        /// <summary>Full-screen dimmer (blocks input to the game) with a centred card.</summary>
        static GameObject CreatePanel(string name, Transform parent, out RectTransform card)
        {
            RectTransform panel = CreateRect(name, parent);
            Stretch(panel);
            Image dimmer = panel.gameObject.AddComponent<Image>();
            dimmer.color = new Color(0.08f, 0.12f, 0.22f, 0.55f);

            card = CreateRect("Card", panel);
            Place(card, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 720f));
            Image cardImage = card.gameObject.AddComponent<Image>();
            cardImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            cardImage.type = Image.Type.Sliced;
            cardImage.color = new Color(1f, 1f, 1f, 0.97f);
            return panel.gameObject;
        }

        // ================================================================ serialized wiring

        static void Assign(Object target, string fieldName, Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(fieldName);
            if (property == null)
            {
                Debug.LogError($"[Last Block] Field '{fieldName}' not found on {target.GetType().Name}.");
                return;
            }
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void AssignArray(Object target, string fieldName, Object[] values)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(fieldName);
            if (property == null || !property.isArray)
            {
                Debug.LogError($"[Last Block] Array field '{fieldName}' not found on {target.GetType().Name}.");
                return;
            }
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
