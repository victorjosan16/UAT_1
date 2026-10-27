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

        /// <summary>Versiunea afișată în magazin (primele două cifre); a treia e numărul build-ului.</summary>
        public const string VersionPrefix = "1.0";

        [MenuItem("Sugar Drop/Construiește jocul")]
        public static void Run() => Run(0);

        /// <param name="buildNumber">Numărul build-ului din Unity Build Automation (crește singur); 0 = build local.</param>
        public static void Run(int buildNumber)
        {
            // Scena nouă PRIMA (NewScene descarcă asset-urile nefolosite din memorie).
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EnsureFolder(DataDir);
            EnsureFolder(ScenesDir);
            SetupPlayerSettings(buildNumber);
            SetupIcon();

            UiTheme theme = CreateTheme();
            PhysicsMaterial2D material = CreatePhysicsMaterial();

            Camera camera = BuildCamera();
            var systems = new GameObject("_Systems");
            systems.AddComponent<AudioManager>();
            systems.AddComponent<SugarAudio>();
            var vfx = Child<SugarVfx>(systems, "Vfx");
            Set(vfx, "shakeCamera", camera);
            Child<Backdrop>(systems, "Backdrop");

            Ui ui = BuildUi(theme);
            BuildGame(camera, material, ui);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("[SugarDrop] Jocul a fost construit: " + ScenePath);
        }

        // ------------------------------------------------------------ Proiect

        private static void SetupPlayerSettings(int buildNumber)
        {
            PlayerSettings.companyName = "VictorJosan";
            PlayerSettings.productName = "Drop Quest";
            // ID-ul aplicației rămâne același (nu se mai poate schimba după publicare și păstrează salvările).
            // Google Play cere ca versionCode să crească la fiecare încărcare: folosim numărul build-ului din cloud.
            PlayerSettings.bundleVersion = $"{VersionPrefix}.{buildNumber}";
            PlayerSettings.Android.bundleVersionCode = Mathf.Max(1, buildNumber);
            PlayerSettings.iOS.buildNumber = Mathf.Max(1, buildNumber).ToString();
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.victorjosan.sugardrop");
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.victorjosan.sugardrop");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            // Codul SDK-urilor de reclame (folosit și prin reflecție) nu trebuie tăiat din build.
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.Minimal);
            LevelPlaySetup.Apply();
            OnlineSetup.Apply();

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

        /// <summary>Iconița aplicației (Art/Icon/AppIcon.png) și ecranul de pornire.</summary>
        private static void SetupIcon()
        {
            const string iconPath = Root + "/Art/Icon/AppIcon.png";
            const string squarePath = Root + "/Art/Icon/AppIconSquare.png";
            foreach (string path in new[] { iconPath, squarePath })
            {
                if (AssetImporter.GetAtPath(path) is not TextureImporter importer) continue;
                importer.textureType = path == iconPath ? TextureImporterType.Sprite : TextureImporterType.Default;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
            var square = AssetDatabase.LoadAssetAtPath<Texture2D>(squarePath);
            var logo = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            if (icon != null) PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
            if (square != null) PlayerSettings.SetIcons(NamedBuildTarget.iOS, new[] { square }, IconKind.Any);

            PlayerSettings.SplashScreen.show = true;
            PlayerSettings.SplashScreen.showUnityLogo = false;
            PlayerSettings.SplashScreen.backgroundColor = QuestStyle.Blue;
            PlayerSettings.SplashScreen.animationMode = PlayerSettings.SplashScreen.AnimationMode.Dolly;
            if (logo != null) PlayerSettings.SplashScreen.logos = new[] { PlayerSettings.SplashScreenLogo.Create(1.5f, logo) };
        }

        private static UiTheme CreateTheme()
        {
            UiTheme theme = LoadOrCreate<UiTheme>($"{DataDir}/SugarTheme.asset");
            Edit(theme, so =>
            {
                P(so, "Font").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
                P(so, "TmpFont").objectReferenceValue = null;
                // Soft Pop: butoane și panouri lavandă (text alb), suprafețe albe, accent galben, mentă, coral.
                P(so, "PanelColor").colorValue = QuestStyle.Wood;
                P(so, "PanelLightColor").colorValue = QuestStyle.Surface;
                P(so, "AccentColor").colorValue = QuestStyle.GoldDeep;
                P(so, "PositiveColor").colorValue = QuestStyle.Green;
                P(so, "NegativeColor").colorValue = QuestStyle.Red;
                P(so, "TextColor").colorValue = Color.white;
                P(so, "TextDarkColor").colorValue = QuestStyle.Text;
                P(so, "AdColor").colorValue = QuestStyle.Blue;
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
            material.friction = 0.06f; // alunecă ușor de pe capace spre găuri
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

        private struct Ui
        {
            public SugarHud Hud;
            public BoosterBar Boosters;
            public ResultPanel Result;
            public DialogPanel Dialog;
            public MapScreen Map;
            public MainMenu Menu;
            public WelcomePanel Welcome;
            public LeaderboardPanel Leaderboard;
            public TutorialCoach Coach;
            public WheelPanel Wheel;
            public WardrobePanel Wardrobe;
            public SettingsPanel Settings;
            public StoryPanel Story;
            public LevelStartPanel LevelStart;
        }

        private static void BuildGame(Camera camera, PhysicsMaterial2D material, Ui ui)
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
            var mascot = Child<Mascot>(root, "Bobo");
            var hint = Child<HintGuide>(root, "HintGuide");
            // Masca tablei stă în scenă (nu doar adăugată din cod), ca Unity s-o păstreze în build.
            var boardMask = Child<SpriteMask>(root, "BoardMask");
            Set(board, "boardMask", boardMask);

            var game = root.AddComponent<SugarDropGame>();
            Set(game, "board", board);
            Set(game, "input", input);
            Set(game, "candies", candies);
            Set(game, "jar", jar);
            Set(game, "playfield", playfield);
            Set(game, "mascot", mascot);
            Set(game, "hintGuide", hint);
            Set(game, "hud", ui.Hud);
            Set(game, "boosters", ui.Boosters);
            Set(game, "result", ui.Result);
            Set(game, "dialog", ui.Dialog);
            Set(game, "map", ui.Map);
            Set(game, "menu", ui.Menu);
            Set(game, "welcome", ui.Welcome);
            Set(game, "leaderboard", ui.Leaderboard);
            Set(game, "coach", ui.Coach);
            Set(game, "wheel", ui.Wheel);
            Set(game, "wardrobe", ui.Wardrobe);
            Set(game, "settings", ui.Settings);
            Set(game, "story", ui.Story);
            Set(game, "levelStart", ui.LevelStart);
        }

        private static Ui BuildUi(UiTheme theme)
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

            // Ordinea contează: ce e mai jos în listă se desenează deasupra.
            Transform safe = UiChild(canvasGo.transform, "SafeArea", typeof(SafeAreaFitter));
            var ui = new Ui
            {
                Hud = UiComponent<SugarHud>(safe, "Hud"),
                Boosters = UiComponent<BoosterBar>(safe, "Boosters"),
                Coach = UiComponent<TutorialCoach>(safe, "Coach"),
                Result = UiComponent<ResultPanel>(canvasGo.transform, "Result"),
                Map = UiComponent<MapScreen>(safe, "Map"),
                Menu = UiComponent<MainMenu>(canvasGo.transform, "Menu"),
                Leaderboard = UiComponent<LeaderboardPanel>(canvasGo.transform, "Leaderboard"),
                Wheel = UiComponent<WheelPanel>(canvasGo.transform, "Wheel"),
                Wardrobe = UiComponent<WardrobePanel>(canvasGo.transform, "Wardrobe"),
                Settings = UiComponent<SettingsPanel>(canvasGo.transform, "Settings"),
                LevelStart = UiComponent<LevelStartPanel>(canvasGo.transform, "LevelStart"),
                Story = UiComponent<StoryPanel>(canvasGo.transform, "Story"),
                Welcome = UiComponent<WelcomePanel>(canvasGo.transform, "Welcome"),
                Dialog = UiComponent<DialogPanel>(canvasGo.transform, "Dialog"),
            };
            // Tranziția dintre ecrane: deasupra ferestrelor, sub reclame.
            UiComponent<UITransitionController>(canvasGo.transform, "Transition");
            // Reclamele de test: ultimul copil, deasupra tuturor.
            UiChild(canvasGo.transform, "AdService", typeof(AdService));
            return ui;
        }

        private static T UiComponent<T>(Transform parent, string name) where T : Component =>
            (T)UiChild(parent, name, typeof(T)).GetComponent(typeof(T));

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
#if UNITY_CLOUD_BUILD
    /// <summary>Unity Build Automation trimite manifestul build-ului; din el luăm numărul build-ului.</summary>
    public static void PreExport(UnityEngine.CloudBuild.BuildManifestObject manifest)
    {
        int buildNumber = 0;
        if (manifest != null && manifest.TryGetValue("buildNumber", out string value)) int.TryParse(value, out buildNumber);
        UnityEngine.Debug.Log("[SugarDrop] Build din cloud nr. " + buildNumber);
        SugarDrop.EditorTools.SugarDropSetup.Run(buildNumber);
    }
#else
    public static void PreExport() => SugarDrop.EditorTools.SugarDropSetup.Run();
#endif
}
