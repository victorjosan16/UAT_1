using System;
using QuarryEmpire;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SugarDrop.EditorTools
{
    /// <summary>
    /// Construiește automat jocul Sugar Drop: setările de proiect, tema UI, materialul fizic și scena.
    /// Rulează din meniul „Sugar Drop → Construiește jocul” sau în build-ul din cloud (SugarDropCloudBuild.PreExport).
    /// </summary>
    public static class SugarDropSetup
    {
        private const string Root = "Assets/SugarDrop";
        private const string DataDir = Root + "/Data";
        private const string ScenesDir = Root + "/Scenes";
        public const string ScenePath = ScenesDir + "/SugarDrop.unity";
        private const string FontPath = "Assets/_Game/Art/Fonts/Baloo2-ExtraBold.ttf";
        private const int UiLayer = 5;

        [MenuItem("Sugar Drop/Construiește jocul")]
        public static void Run()
        {
            // Scena nouă PRIMA (NewScene descarcă asset-urile nefolosite din memorie).
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EnsureFolder(DataDir);
            EnsureFolder(ScenesDir);
            SetupPlayerSettings();

            UiTheme theme = CreateTheme();
            PhysicsMaterial2D material = CreatePhysicsMaterial();

            Camera camera = BuildCamera();
            var systems = new GameObject("_Systems");
            systems.AddComponent<AudioManager>();

            (SugarHud hud, ResultPanel result) = BuildUi(theme);
            BuildGame(camera, material, hud, result);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("[SugarDrop] Jocul a fost construit: " + ScenePath);
        }

        // ------------------------------------------------------------ Proiect

        private static void SetupPlayerSettings()
        {
            PlayerSettings.companyName = "VictorJosan";
            PlayerSettings.productName = "Sugar Drop";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.Android.bundleVersionCode = 1;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.victorjosan.sugardrop");
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.victorjosan.sugardrop");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            // Active Input Handling = Input System Package (New).
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

        private static UiTheme CreateTheme()
        {
            UiTheme theme = LoadOrCreate<UiTheme>($"{DataDir}/SugarTheme.asset");
            Edit(theme, so =>
            {
                P(so, "Font").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
                P(so, "TmpFont").objectReferenceValue = null;
                P(so, "PanelColor").colorValue = new Color(0.45f, 0.2f, 0.38f, 0.92f);
                P(so, "PanelLightColor").colorValue = new Color(1f, 0.97f, 0.93f, 1f);
                P(so, "AccentColor").colorValue = Hex("#FFC93C");
                P(so, "PositiveColor").colorValue = Hex("#5FD068");
                P(so, "NegativeColor").colorValue = Hex("#FF5A6E");
                P(so, "TextColor").colorValue = Color.white;
                P(so, "TextDarkColor").colorValue = Hex("#5A2D4E");
                P(so, "AdColor").colorValue = Hex("#4DA8FF");
            });
            return theme;
        }

        private static PhysicsMaterial2D CreatePhysicsMaterial()
        {
            string path = $"{DataDir}/CandyPhysics.physicsMaterial2D";
            var material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(path);
            if (material == null)
            {
                material = new PhysicsMaterial2D("CandyPhysics");
                AssetDatabase.CreateAsset(material, path);
            }
            material.friction = 0.15f;
            material.bounciness = 0.05f;
            EditorUtility.SetDirty(material);
            return material;
        }

        // ------------------------------------------------------------ Scenă

        private static Camera BuildCamera()
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            go.transform.position = new Vector3(0f, 0f, -10f);
            var camera = go.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 10.2f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Palette.Background;
            go.AddComponent<AudioListener>();
            go.AddComponent<CameraFit>();
            return camera;
        }

        private static void BuildGame(Camera camera, PhysicsMaterial2D material, SugarHud hud, ResultPanel result)
        {
            var root = new GameObject("Game");
            var board = Child<Board>(root, "Board");
            var input = Child<BoardInput>(root, "BoardInput");
            Set(input, "board", board);
            Set(input, "worldCamera", camera);
            var candies = Child<CandySpawner>(root, "Candies");
            Set(candies, "material", material);
            var jar = Child<Jar>(root, "Jar");
            var playfield = Child<Playfield>(root, "Playfield");
            Set(playfield, "jar", jar);
            Set(playfield, "material", material);

            var game = root.AddComponent<SugarDropGame>();
            Set(game, "board", board);
            Set(game, "input", input);
            Set(game, "candies", candies);
            Set(game, "jar", jar);
            Set(game, "playfield", playfield);
            Set(game, "hud", hud);
            Set(game, "result", result);
        }

        private static (SugarHud, ResultPanel) BuildUi(UiTheme theme)
        {
            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            var inputModule = eventSystem.AddComponent<InputSystemUIInputModule>();

            var canvasGo = new GameObject("Canvas", typeof(RectTransform)) { layer = UiLayer };
            canvasGo.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0f;
            canvasGo.AddComponent<GraphicRaycaster>();
            var uiRoot = canvasGo.AddComponent<UiRoot>();
            Set(uiRoot, "theme", theme);
            Set(uiRoot, "inputModule", inputModule);

            Transform safe = UiChild(canvasGo.transform, "SafeArea", typeof(SafeAreaFitter));
            var hud = (SugarHud)UiChild(safe, "Hud", typeof(SugarHud)).GetComponent(typeof(SugarHud));
            var result = (ResultPanel)UiChild(canvasGo.transform, "Result", typeof(ResultPanel)).GetComponent(typeof(ResultPanel));
            return (hud, result);
        }

        // ------------------------------------------------------------ Utilitare

        private static T Child<T>(GameObject parent, string name) where T : Component
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            return go.AddComponent<T>();
        }

        private static Transform UiChild(Transform parent, string name, Type component)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = UiLayer };
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.AddComponent(component);
            return rect;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            string parent = path.Substring(0, slash);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(slash + 1));
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void Edit(Object target, Action<SerializedObject> edit)
        {
            var so = new SerializedObject(target);
            edit(so);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static SerializedProperty P(SerializedObject so, string name) =>
            so.FindProperty(name) ?? so.FindProperty($"<{name}>k__BackingField")
            ?? throw new Exception($"Nu găsesc câmpul „{name}” pe {so.targetObject.GetType().Name}");

        private static void Set(Object target, string field, Object value) => Edit(target, so => P(so, field).objectReferenceValue = value);

        private static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out Color c) ? c : Color.magenta;
    }
}

/// <summary>Punctul de intrare pentru Unity Build Automation: Pre-Export Method = SugarDropCloudBuild.PreExport.</summary>
public static class SugarDropCloudBuild
{
    public static void PreExport() => SugarDrop.EditorTools.SugarDropSetup.Run();
}
