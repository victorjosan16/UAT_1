using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace QuarryEmpire.EditorTools
{
    /// <summary>
    /// Etapa Vizuală: aplică peste scena generată de Stage1Setup personajul stilizat, mediul,
    /// HUD-ul, efectele și profilurile de performanță.
    ///
    /// Reguli de siguranță:
    /// - NU modifică scripturile de gameplay și nici valorile lor (viteză, minat, colectare).
    /// - Verifică întâi toate referințele de care are nevoie; ce lipsește e raportat și sărit.
    /// - Obiectele vechi sunt doar dezactivate (randerele capsulei, contorul de debug), nu șterse.
    /// - Fiecare etapă rulează separat: dacă una eșuează, celelalte continuă, iar scena rămâne jucabilă.
    /// - Poate fi rulată de câte ori vrei: își șterge doar propriile obiecte (prefix „V_” / rădăcini proprii).
    /// </summary>
    public static partial class VisualSetup
    {
        private const string PrefabsDir = "Assets/_Game/Prefabs";
        private const string StageMaterialsDir = "Assets/_Game/Art/Materials";
        private const string PerformanceDir = "Assets/_Game/Settings/Performance";
        private const string VisualPrefix = "V_";

        /// <summary>Obiectele din scena Etapei 1 de care depinde etapa vizuală.</summary>
        private sealed class SceneRefs
        {
            public GameObject Player;
            public PlayerMovement Movement;
            public PlayerMiner Miner;
            public PickaxeSwing Swing;
            public PlayerStats Stats;
            public Transform PickaxePivot;
            public GameObject Ground;
            public Canvas Canvas;
            public RectTransform JoystickBackground;
            public RectTransform JoystickHandle;
            public GameObject DebugCounter;
            public GameObject Systems;
            public ObjectPool Pool;
            public Light Sun;
            public Camera Camera;
            public DebugBlockGrid Grid;
        }

        [MenuItem("Quarry Empire/Setup Vizual (după Etapa 1)", priority = 20)]
        public static void RunFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Run();
        }

        [MenuItem("Quarry Empire/Setup Complet (Etapa 1 + Vizual)", priority = 21)]
        public static void RunAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Stage1Setup.Run();
            Run();
        }

        /// <summary>Punctul de intrare (meniu și build în cloud). Returnează false dacă nu s-a putut aplica nimic.</summary>
        public static bool Run()
        {
            if (!File.Exists(Stage1Setup.ScenePath))
            {
                Debug.LogError($"[QuarryEmpire/Vizual] Lipsește {Stage1Setup.ScenePath}. Rulează întâi „Quarry Empire → Setup Etapa 1”.");
                return false;
            }
            if (!EnsureTextMeshPro())
            {
                Debug.LogError("[QuarryEmpire/Vizual] TextMesh Pro nu e disponibil. Scena Etapei 1 rămâne neschimbată.");
                return false;
            }

            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != Stage1Setup.ScenePath)
                scene = EditorSceneManager.OpenScene(Stage1Setup.ScenePath, OpenSceneMode.Single);

            SceneRefs refs = CollectReferences(out string report);
            Debug.Log(report);

            VisualAssetFactory.EnsureFolders();
            VisualAssetFactory.EnsureFolder(PerformanceDir);

            var palette = Step("Materiale și mesh-uri", () => BuildPalette()) ?? new Palette();
            Step("Prefab bloc + resursă", () => UpgradeMiningPrefabs(palette));
            Step("Personaj", () => BuildCharacter(refs, palette));
            Step("Mediu și lumină", () => BuildEnvironment(refs, palette));
            Step("Efecte", () => BuildEffects(refs, palette));
            Step("HUD", () => BuildHud(refs, palette));
            Step("Performanță", () => BuildPerformance(refs));

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[QuarryEmpire/Vizual] Gata. Scena salvată: " + scene.path);
            return true;
        }

        private static void Step(string name, Action action) => Step<object>(name, () => { action(); return null; });

        private static T Step<T>(string name, Func<T> action) where T : class
        {
            try
            {
                T result = action();
                Debug.Log($"[QuarryEmpire/Vizual] ✓ {name}");
                return result;
            }
            catch (Exception exception)
            {
                // O etapă ratată nu oprește restul (și nu pică build-ul din cloud).
                Debug.LogError($"[QuarryEmpire/Vizual] ✗ {name}: {exception}");
                return null;
            }
        }

        // ================================================================ TextMesh Pro

        /// <summary>
        /// HUD-ul folosește TextMeshPro, care are nevoie de „TMP Essential Resources” (font + setări).
        /// Le importăm automat din pachetul uGUI (merge și în build-ul din cloud, în batch mode).
        /// </summary>
        private static bool EnsureTextMeshPro()
        {
            if (TmpReady()) return true;
            string[] packages =
            {
                "Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage",
                "Packages/com.unity.textmeshpro/Package Resources/TMP Essential Resources.unitypackage",
            };
            foreach (string package in packages)
            {
                if (!File.Exists(Path.GetFullPath(package))) continue;
                Debug.Log("[QuarryEmpire/Vizual] Import TMP Essential Resources din " + package);
                AssetDatabase.ImportPackage(package, false);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                if (TmpReady()) return true;
            }

            if (!Application.isBatchMode)
            {
                EditorApplication.ExecuteMenuItem("Window/TextMeshPro/Import TMP Essential Resources");
                EditorUtility.DisplayDialog("Quarry Empire",
                    "Apasă „Import TMP Essentials” în fereastra TextMesh Pro, apoi rulează din nou „Setup Vizual”.", "OK");
            }
            return false;
        }

        private static bool TmpReady()
        {
            var settings = Resources.Load<TMP_Settings>("TMP Settings");
            return settings != null && TMP_Settings.defaultFontAsset != null;
        }

        // ================================================================ Verificare referințe

        private static SceneRefs CollectReferences(out string report)
        {
            var refs = new SceneRefs();
            var log = new StringBuilder("[QuarryEmpire/Vizual] Verificare referințe scenă:\n");

            refs.Player = GameObject.FindWithTag("Player");
            if (refs.Player != null)
            {
                refs.Movement = refs.Player.GetComponent<PlayerMovement>();
                refs.Miner = refs.Player.GetComponent<PlayerMiner>();
                refs.Swing = refs.Player.GetComponent<PickaxeSwing>();
                refs.Stats = refs.Movement != null ? GetRef<PlayerStats>(refs.Movement, "stats") : null;
                refs.PickaxePivot = refs.Swing != null ? GetRef<Transform>(refs.Swing, "pivot") : null;
            }
            refs.Ground = GameObject.Find("Ground");
            refs.Canvas = Object.FindFirstObjectByType<Canvas>();
            GameObject joystick = GameObject.Find("JoystickBG");
            if (joystick != null)
            {
                refs.JoystickBackground = joystick.transform as RectTransform;
                Transform handle = joystick.transform.Find("JoystickHandle");
                refs.JoystickHandle = handle as RectTransform;
            }
            refs.DebugCounter = GameObject.Find("DebugCounter");
            refs.Systems = GameObject.Find("_Systems");
            refs.Pool = Object.FindFirstObjectByType<ObjectPool>();
            refs.Camera = Camera.main;
            refs.Grid = Object.FindFirstObjectByType<DebugBlockGrid>();
            foreach (Light light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (light.type == LightType.Directional) refs.Sun = light;

            Line(log, "Player (tag Player)", refs.Player);
            Line(log, "PlayerMovement", refs.Movement);
            Line(log, "PlayerMiner", refs.Miner);
            Line(log, "PickaxeSwing", refs.Swing);
            Line(log, "PickaxeSwing.pivot", refs.PickaxePivot);
            Line(log, "PlayerStats", refs.Stats);
            Line(log, "Ground", refs.Ground);
            Line(log, "Canvas", refs.Canvas);
            Line(log, "JoystickBG / JoystickHandle", refs.JoystickHandle);
            Line(log, "DebugCounter", refs.DebugCounter);
            Line(log, "_Systems", refs.Systems);
            Line(log, "ObjectPool", refs.Pool);
            Line(log, "Main Camera", refs.Camera);
            Line(log, "Directional Light", refs.Sun);
            Line(log, "BlockGrid", refs.Grid);
            report = log.ToString();
            return refs;
        }

        private static void Line(StringBuilder log, string name, Object value) =>
            log.Append(value != null ? "  ✓ " : "  ✗ LIPSEȘTE ").AppendLine(name);

        private static T GetRef<T>(Object target, string field) where T : Object
        {
            var so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(field);
            return property != null ? property.objectReferenceValue as T : null;
        }

        // ================================================================ Utilitare comune

        private static void SetRef(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(field)
                ?? throw new Exception($"Nu găsesc câmpul „{field}” pe {target.GetType().Name}");
            property.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetRefs(Object target, string field, IReadOnlyList<Object> values)
        {
            var so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(field)
                ?? throw new Exception($"Nu găsesc câmpul „{field}” pe {target.GetType().Name}");
            property.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(Object target, string field, float value)
        {
            var so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(field)
                ?? throw new Exception($"Nu găsesc câmpul „{field}” pe {target.GetType().Name}");
            property.floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Proprietate [field: SerializeField] → câmpul ei ascuns.</summary>
        private static SerializedProperty Auto(SerializedObject so, string propertyName) =>
            so.FindProperty($"<{propertyName}>k__BackingField")
            ?? throw new Exception($"Nu găsesc „{propertyName}” pe {so.targetObject.GetType().Name}");

        private static void DestroyChild(Transform parent, string name)
        {
            if (parent == null) return;
            Transform child = parent.Find(name);
            while (child != null)
            {
                Object.DestroyImmediate(child.gameObject);
                child = parent.Find(name);
            }
        }

        private static void DestroyRoot(string name)
        {
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
                if (root.name == name) Object.DestroyImmediate(root);
        }

        /// <summary>Obiect vizual din mesh + material, fără collider (decorul nu blochează gameplay-ul).</summary>
        private static GameObject MeshPart(string name, Transform parent, Mesh mesh, Material material,
            Vector3 localPosition, Vector3? localEuler = null, Vector3? localScale = null, bool castShadows = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.Euler(localEuler ?? Vector3.zero);
            go.transform.localScale = localScale ?? Vector3.one;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = castShadows
                ? UnityEngine.Rendering.ShadowCastingMode.On
                : UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            return go;
        }

        /// <summary>Primitivă Unity fără collider.</summary>
        private static GameObject PrimitivePart(PrimitiveType type, string name, Transform parent, Material material,
            Vector3 localPosition, Vector3 localScale, Vector3? localEuler = null, bool castShadows = true)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            Mesh mesh = go.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(go);
            return MeshPart(name, parent, mesh, material, localPosition, localEuler, localScale, castShadows);
        }

        private static GameObject Group(string name, Transform parent, Vector3 localPosition, Vector3? localEuler = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.Euler(localEuler ?? Vector3.zero);
            return go;
        }
    }
}
