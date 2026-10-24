using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace QuarryEmpire.EditorTools
{
    /// <summary>
    /// Construiește automat tot ce trebuie pentru Etapa 1: layere, URP, setări Android,
    /// materiale, ScriptableObjects, prefab-uri și scena Game.
    /// Rulează din meniul „Quarry Empire → Setup Etapa 1” sau automat în build-ul din cloud.
    /// Materialele și datele se creează doar dacă lipsesc; prefab-urile și scena se refac mereu.
    /// </summary>
    public static class Stage1Setup
    {
        private const string Root = "Assets/_Game";
        private const string MaterialsDir = Root + "/Art/Materials";
        private const string DataDir = Root + "/Data";
        private const string PrefabsDir = Root + "/Prefabs";
        private const string ScenesDir = Root + "/Scenes";
        private const string SettingsDir = Root + "/Settings";
        public const string ScenePath = ScenesDir + "/Game.unity";

        private const int BlockLayer = 6;
        private const int ResourceLayer = 7;
        private const int UiLayer = 5;

        // Valorile de echilibru inițiale. După creare, se editează în asset-urile din Data/.
        private static readonly (string key, string id, string name, string color, int price)[] Resources =
        {
            ("Stone", "stone", "Piatră", "#8C8F96", 1),
            ("Copper", "copper", "Cupru", "#C8743A", 3),
            ("Iron", "iron", "Fier", "#5E6E86", 6),
            ("Gold", "gold", "Aur", "#F2C230", 15),
            ("Diamond", "diamond", "Diamant", "#5FE1F0", 50),
        };

        private static readonly (string key, string name, float hp, int hardness, int minDrops, int maxDrops, float weight)[] Blocks =
        {
            ("Stone", "Bloc de piatră", 3f, 1, 1, 2, 10f),
            ("Copper", "Bloc de cupru", 5f, 1, 1, 2, 5f),
            ("Iron", "Bloc de fier", 8f, 2, 1, 2, 3f),
            ("Gold", "Bloc de aur", 12f, 2, 1, 3, 1.5f),
            ("Diamond", "Bloc de diamant", 20f, 3, 1, 1, 0.5f),
        };

        [MenuItem("Quarry Empire/Setup Etapa 1")]
        public static void Run()
        {
            EnsureFolders();
            SetupLayers();
            SetupRenderPipeline();
            SetupPlayerSettings();

            var materials = CreateMaterials();
            var resources = CreateResourceData(materials);
            var blocks = CreateBlockData(materials, resources);
            var stats = CreatePlayerStats();

            ResourcePickup pickupPrefab = CreatePickupPrefab(materials["Stone"]);
            MineBlock blockPrefab = CreateBlockPrefab(materials["Stone"], blocks["Stone"], pickupPrefab);

            BuildScene(materials, blocks, stats, pickupPrefab, blockPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[QuarryEmpire] Etapa 1 configurată: scena " + ScenePath);
        }

        // ---------------------------------------------------------------- Proiect

        private static void EnsureFolders()
        {
            foreach (var path in new[] { MaterialsDir, DataDir, PrefabsDir, ScenesDir, SettingsDir })
                EnsureFolder(path);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            string parent = path.Substring(0, slash);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(slash + 1));
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

        // ---------------------------------------------------------------- Date

        private static Dictionary<string, Material> CreateMaterials()
        {
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null) throw new Exception("Nu găsesc shader-ul URP Lit. Este instalat pachetul URP?");

            var colors = new Dictionary<string, string>
            {
                { "Ground", "#C9A574" }, { "Player", "#2F80ED" }, { "Pickaxe", "#5A3E2B" },
            };
            foreach (var r in Resources) colors[r.key] = r.color;

            var result = new Dictionary<string, Material>();
            foreach (var pair in colors)
            {
                string path = $"{MaterialsDir}/M_{pair.Key}.mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    material = new Material(lit);
                    material.SetColor("_BaseColor", Hex(pair.Value));
                    material.SetFloat("_Smoothness", 0.1f);
                    AssetDatabase.CreateAsset(material, path);
                }
                result[pair.Key] = material;
            }
            return result;
        }

        private static Dictionary<string, ResourceData> CreateResourceData(Dictionary<string, Material> materials)
        {
            var result = new Dictionary<string, ResourceData>();
            foreach (var r in Resources)
            {
                var asset = LoadOrCreate<ResourceData>($"{DataDir}/Resource_{r.key}.asset", out bool created);
                if (created)
                {
                    var so = new SerializedObject(asset);
                    Auto(so, "Id").stringValue = r.id;
                    Auto(so, "DisplayName").stringValue = r.name;
                    Auto(so, "UiColor").colorValue = Hex(r.color);
                    Auto(so, "PickupMaterial").objectReferenceValue = materials[r.key];
                    Auto(so, "SellPrice").intValue = r.price;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                result[r.key] = asset;
            }
            return result;
        }

        private static Dictionary<string, BlockData> CreateBlockData(
            Dictionary<string, Material> materials, Dictionary<string, ResourceData> resources)
        {
            var result = new Dictionary<string, BlockData>();
            foreach (var b in Blocks)
            {
                var asset = LoadOrCreate<BlockData>($"{DataDir}/Block_{b.key}.asset", out bool created);
                if (created)
                {
                    var so = new SerializedObject(asset);
                    Auto(so, "DisplayName").stringValue = b.name;
                    Auto(so, "BlockMaterial").objectReferenceValue = materials[b.key];
                    Auto(so, "MaxHealth").floatValue = b.hp;
                    Auto(so, "Hardness").intValue = b.hardness;
                    Auto(so, "DroppedResource").objectReferenceValue = resources[b.key];
                    Auto(so, "MinDrops").intValue = b.minDrops;
                    Auto(so, "MaxDrops").intValue = b.maxDrops;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                result[b.key] = asset;
            }
            return result;
        }

        private static PlayerStats CreatePlayerStats()
        {
            // Valorile implicite din clasa PlayerStats sunt deja cele dorite.
            return LoadOrCreate<PlayerStats>($"{DataDir}/PlayerStats.asset", out _);
        }

        // ---------------------------------------------------------------- Prefab-uri

        private static ResourcePickup CreatePickupPrefab(Material material)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Pickup_Resource";
            go.layer = ResourceLayer;
            go.transform.localScale = Vector3.one * 0.3f;
            go.GetComponent<BoxCollider>().isTrigger = true;

            var meshRenderer = go.GetComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;

            var pickup = go.AddComponent<ResourcePickup>();
            SetRef(pickup, "pickupRenderer", meshRenderer);

            return SavePrefab(go, "Pickup_Resource").GetComponent<ResourcePickup>();
        }

        private static MineBlock CreateBlockPrefab(Material material, BlockData defaultData, ResourcePickup pickupPrefab)
        {
            var root = new GameObject("Block") { layer = BlockLayer };
            root.AddComponent<BoxCollider>();

            GameObject visual = Primitive(PrimitiveType.Cube, "Visual", root.transform, Vector3.zero, Vector3.one, material);
            var visualRenderer = visual.GetComponent<MeshRenderer>();

            var block = root.AddComponent<MineBlock>();
            SetRef(block, "data", defaultData);
            SetRef(block, "blockRenderer", visualRenderer);

            var feedback = root.AddComponent<BlockHitFeedback>();
            SetRef(feedback, "block", block);
            SetRef(feedback, "visual", visual.transform);
            SetRef(feedback, "targetRenderer", visualRenderer);

            var dropSpawner = root.AddComponent<BlockDropSpawner>();
            SetRef(dropSpawner, "pickupPrefab", pickupPrefab);

            return SavePrefab(root, "Block").GetComponent<MineBlock>();
        }

        // ---------------------------------------------------------------- Scena

        private static void BuildScene(
            Dictionary<string, Material> materials, Dictionary<string, BlockData> blocks,
            PlayerStats stats, ResourcePickup pickupPrefab, MineBlock blockPrefab)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.6f);

            var sun = new GameObject("Directional Light");
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.shadows = LightShadows.Soft;
            light.intensity = 1.2f;
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            Quaternion cameraRotation = Quaternion.Euler(50f, 45f, 0f);
            var followOffset = new Vector3(-7.3f, 12.3f, -7.3f);

            var cameraGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Hex("#87B9E0");
            camera.fieldOfView = 40f;
            cameraGo.AddComponent<AudioListener>();
            cameraGo.AddComponent<CinemachineBrain>();
            cameraGo.transform.SetPositionAndRotation(followOffset, cameraRotation);

            Primitive(PrimitiveType.Plane, "Ground", null, Vector3.zero, new Vector3(3f, 1f, 3f), materials["Ground"], keepCollider: true);

            // Sisteme
            var systems = new GameObject("_Systems");
            new GameObject("GameManager", typeof(GameManager)).transform.SetParent(systems.transform);
            var poolGo = new GameObject("ObjectPool");
            poolGo.transform.SetParent(systems.transform);
            var pool = poolGo.AddComponent<ObjectPool>();
            var poolSo = new SerializedObject(pool);
            SerializedProperty prewarm = poolSo.FindProperty("prewarm");
            prewarm.arraySize = 1;
            prewarm.GetArrayElementAtIndex(0).FindPropertyRelative("prefab").objectReferenceValue = pickupPrefab.gameObject;
            prewarm.GetArrayElementAtIndex(0).FindPropertyRelative("count").intValue = 30;
            poolSo.ApplyModifiedPropertiesWithoutUndo();

            // Grila de blocuri
            var gridGo = new GameObject("BlockGrid");
            gridGo.transform.position = new Vector3(0f, 0f, 6f);
            var grid = gridGo.AddComponent<DebugBlockGrid>();
            var gridSo = new SerializedObject(grid);
            gridSo.FindProperty("blockPrefab").objectReferenceValue = blockPrefab;
            SerializedProperty list = gridSo.FindProperty("blocks");
            list.arraySize = Blocks.Length;
            for (int i = 0; i < Blocks.Length; i++)
            {
                SerializedProperty element = list.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("data").objectReferenceValue = blocks[Blocks[i].key];
                element.FindPropertyRelative("weight").floatValue = Blocks[i].weight;
            }
            gridSo.FindProperty("width").intValue = 8;
            gridSo.FindProperty("depth").intValue = 8;
            gridSo.ApplyModifiedPropertiesWithoutUndo();

            Transform player = BuildPlayer(materials, stats);

            // Camera virtuală Cinemachine: urmărește jucătorul, cu rotație fixă (izometric ~50°).
            var vcamGo = new GameObject("CinemachineCamera");
            vcamGo.transform.SetPositionAndRotation(player.position + followOffset, cameraRotation);
            var vcam = vcamGo.AddComponent<CinemachineCamera>();
            CameraTarget target = vcam.Target;
            target.TrackingTarget = player;
            vcam.Target = target;
            LensSettings lens = vcam.Lens;
            lens.FieldOfView = 40f;
            vcam.Lens = lens;
            var follow = vcamGo.AddComponent<CinemachineFollow>();
            follow.FollowOffset = followOffset;
            TrackerSettings tracker = follow.TrackerSettings;
            tracker.BindingMode = BindingMode.WorldSpace;
            tracker.PositionDamping = Vector3.one * 0.3f;
            follow.TrackerSettings = tracker;

            BuildUi();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        private static Transform BuildPlayer(Dictionary<string, Material> materials, PlayerStats stats)
        {
            var player = new GameObject("Player") { tag = "Player" };
            player.transform.position = new Vector3(0f, 0.1f, 0f);

            var controller = player.AddComponent<CharacterController>();
            controller.center = new Vector3(0f, 1f, 0f);
            controller.radius = 0.4f;
            controller.height = 2f;

            Transform t = player.transform;
            Primitive(PrimitiveType.Capsule, "Body", t, new Vector3(0f, 1f, 0f), Vector3.one, materials["Player"]);
            Primitive(PrimitiveType.Cube, "Nose", t, new Vector3(0f, 1.5f, 0.4f), Vector3.one * 0.2f, materials["Pickaxe"]);

            var pivot = new GameObject("PickaxePivot").transform;
            pivot.SetParent(t, false);
            pivot.localPosition = new Vector3(0.35f, 1.3f, 0.1f);
            GameObject handle = Primitive(PrimitiveType.Cube, "Pickaxe", pivot, new Vector3(0f, 0.45f, 0f), new Vector3(0.08f, 0.9f, 0.08f), materials["Pickaxe"]);
            Primitive(PrimitiveType.Cube, "Head", handle.transform, new Vector3(0f, 0.5f, 0f), new Vector3(1.25f, 0.09f, 6.25f), materials["Pickaxe"]);

            var collectPoint = new GameObject("CollectPoint").transform;
            collectPoint.SetParent(t, false);
            collectPoint.localPosition = new Vector3(0f, 1.2f, -0.5f);

            var input = player.AddComponent<PlayerInputReader>();

            var movement = player.AddComponent<PlayerMovement>();
            SetRef(movement, "stats", stats);
            SetRef(movement, "input", input);

            var miner = player.AddComponent<PlayerMiner>();
            SetRef(miner, "stats", stats);
            SetRef(miner, "movement", movement);
            SetInt(miner, "blockLayer", 1 << BlockLayer);

            var swing = player.AddComponent<PickaxeSwing>();
            SetRef(swing, "miner", miner);
            SetRef(swing, "pivot", pivot);

            var collector = player.AddComponent<ResourceCollector>();
            SetRef(collector, "stats", stats);
            SetInt(collector, "resourceLayer", 1 << ResourceLayer);
            SetRef(collector, "collectPoint", collectPoint);

            return t;
        }

        private static void BuildUi()
        {
            var canvasGo = new GameObject("Canvas", typeof(RectTransform)) { layer = UiLayer };
            canvasGo.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();

            // Joystick: fundal + mâner. OnScreenStick simulează stick-ul stâng al unui gamepad.
            var knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            RectTransform background = UiImage("JoystickBG", canvasGo.transform, knob, new Color(1f, 1f, 1f, 0.3f), 300f);
            background.anchorMin = background.anchorMax = new Vector2(0.5f, 0f);
            background.anchoredPosition = new Vector2(0f, 350f);

            RectTransform handle = UiImage("JoystickHandle", background, knob, Color.white, 130f);
            var stick = handle.gameObject.AddComponent<OnScreenStick>();
            var stickSo = new SerializedObject(stick);
            stickSo.FindProperty("m_ControlPath").stringValue = "<Gamepad>/leftStick";
            stickSo.FindProperty("m_MovementRange").floatValue = 100f;
            stickSo.ApplyModifiedPropertiesWithoutUndo();

            // Contor de resurse (temporar).
            var textGo = new GameObject("DebugCounter", typeof(RectTransform)) { layer = UiLayer };
            var textRect = (RectTransform)textGo.transform;
            textRect.SetParent(canvasGo.transform, false);
            textRect.anchorMin = textRect.anchorMax = textRect.pivot = new Vector2(0f, 1f);
            textRect.anchoredPosition = new Vector2(40f, -80f);
            textRect.sizeDelta = new Vector2(700f, 450f);
            var text = textGo.AddComponent<Text>();
            text.font = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 44;
            text.color = Color.white;
            text.raycastTarget = false;
            textGo.AddComponent<Shadow>().effectDistance = new Vector2(2f, -2f);
            var counter = textGo.AddComponent<ResourceCounterDebug>();
            SetRef(counter, "label", text);
        }

        // ---------------------------------------------------------------- Utilitare

        private static T LoadOrCreate<T>(string path, out bool created) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            created = asset == null;
            if (created)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }
            return asset;
        }

        private static GameObject SavePrefab(GameObject instance, string name)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, $"{PrefabsDir}/{name}.prefab");
            Object.DestroyImmediate(instance);
            return prefab;
        }

        private static GameObject Primitive(PrimitiveType type, string name, Transform parent,
            Vector3 localPosition, Vector3 localScale, Material material, bool keepCollider = false)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            if (!keepCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }

        private static RectTransform UiImage(string name, Transform parent, Sprite sprite, Color color, float size)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = UiLayer };
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.sizeDelta = new Vector2(size, size);
            var image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            return rect;
        }

        /// <summary>Găsește câmpul din spatele unei proprietăți [field: SerializeField].</summary>
        private static SerializedProperty Auto(SerializedObject so, string propertyName)
        {
            return so.FindProperty($"<{propertyName}>k__BackingField")
                   ?? throw new Exception($"Nu găsesc „{propertyName}” pe {so.targetObject.GetType().Name}");
        }

        private static SerializedProperty Field(Component component, string fieldName, out SerializedObject so)
        {
            so = new SerializedObject(component);
            return so.FindProperty(fieldName)
                   ?? throw new Exception($"Nu găsesc câmpul „{fieldName}” pe {component.GetType().Name}");
        }

        private static void SetRef(Component component, string fieldName, Object value)
        {
            Field(component, fieldName, out var so).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetInt(Component component, string fieldName, int value)
        {
            Field(component, fieldName, out var so).intValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;
    }
}

/// <summary>
/// Punctul de intrare pentru Unity Build Automation (câmpul „Pre-Export Method”: QuarryCloudBuild.PreExport).
/// Rulează înainte de build și generează scena și asset-urile Etapei 1.
/// </summary>
public static class QuarryCloudBuild
{
    public static void PreExport() => QuarryEmpire.EditorTools.Stage1Setup.Run();
}
