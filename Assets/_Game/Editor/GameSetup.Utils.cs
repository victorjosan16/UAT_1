using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace QuarryEmpire.EditorTools
{
    /// <summary>Funcții ajutătoare pentru setup: creare asset-uri, setare câmpuri private, primitive.</summary>
    public static partial class GameSetup
    {
        private static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;

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

        /// <summary>Editează câmpurile serializate (inclusiv private) ale unui obiect.</summary>
        private static void Edit(Object target, Action<SerializedObject> edit)
        {
            var so = new SerializedObject(target);
            edit(so);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        /// <summary>Găsește un câmp (sau câmpul din spatele unei proprietăți [field: SerializeField]).</summary>
        private static SerializedProperty P(SerializedObject so, string name)
        {
            return so.FindProperty(name)
                   ?? so.FindProperty($"<{name}>k__BackingField")
                   ?? throw new Exception($"Nu găsesc câmpul „{name}” pe {so.targetObject.GetType().Name}");
        }

        private static void Set(Object target, string field, Object value) => Edit(target, so => P(so, field).objectReferenceValue = value);
        private static void Set(Object target, string field, int value) => Edit(target, so => P(so, field).intValue = value);
        private static void Set(Object target, string field, long value) => Edit(target, so => P(so, field).longValue = value);
        private static void Set(Object target, string field, float value) => Edit(target, so => P(so, field).floatValue = value);
        private static void Set(Object target, string field, bool value) => Edit(target, so => P(so, field).boolValue = value);
        private static void Set(Object target, string field, string value) => Edit(target, so => P(so, field).stringValue = value);
        private static void Set(Object target, string field, Vector2 value) => Edit(target, so => P(so, field).vector2Value = value);
        private static void Set(Object target, string field, Vector3 value) => Edit(target, so => P(so, field).vector3Value = value);
        private static void Set(Object target, string field, Color value) => Edit(target, so => P(so, field).colorValue = value);

        private static void SetArray(Object target, string field, Object[] values) => Edit(target, so =>
        {
            SerializedProperty array = P(so, field);
            array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        });

        private static GameObject Empty(string name, Transform parent, Vector3 localPosition, float yRotation = 0f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);
            return go;
        }

        private static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 localPosition,
            Vector3 localScale, Material material, bool keepCollider = false, Vector3 localEuler = default)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            if (!keepCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.Euler(localEuler);
            go.transform.localScale = localScale;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }

        private static ZoneArea AddZone(GameObject go, Vector3 size)
        {
            var zone = go.AddComponent<ZoneArea>();
            Set(zone, "size", size);
            return zone;
        }

        private static WorldLabel AddLabel(GameObject go, string text, float height, bool showBar,
            Vector2? size = null, int fontSize = 40)
        {
            var label = go.AddComponent<WorldLabel>();
            Edit(label, so =>
            {
                P(so, "initialText").stringValue = text;
                P(so, "height").floatValue = height;
                P(so, "showBar").boolValue = showBar;
                P(so, "size").vector2Value = size ?? new Vector2(360f, 140f);
                P(so, "fontSize").intValue = fontSize;
            });
            return label;
        }

        // ------------------------------------------------------------ Mesh-uri rotunjite

        private static readonly Dictionary<string, Mesh> RoundedMeshes = new();

        /// <summary>Mesh rotunjit pentru o anumită mărime, salvat ca asset (refolosit de toate obiectele de aceeași mărime).</summary>
        private static Mesh RoundedMesh(Vector3 size, float radius)
        {
            string key = $"{size.x:0.###}x{size.y:0.###}x{size.z:0.###}_r{radius:0.###}";
            if (RoundedMeshes.TryGetValue(key, out Mesh cached) && cached != null) return cached;

            string path = $"{MeshesDir}/RB_{key}.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null)
            {
                mesh = RoundedBoxMesh.Create(size, radius, 3, "RB_" + key);
                AssetDatabase.CreateAsset(mesh, path);
            }
            RoundedMeshes[key] = mesh;
            return mesh;
        }

        /// <summary>Cutie cu muchii rotunjite, de mărimea exactă dată (fără scară, deci rotunjirea nu se deformează).</summary>
        private static GameObject Box(string name, Transform parent, Vector3 localPosition, Vector3 size, Material material,
            float radius = 0.08f, Vector3 localEuler = default, bool collider = false, bool castShadows = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.Euler(localEuler);
            go.AddComponent<MeshFilter>().sharedMesh = RoundedMesh(size, radius);
            var meshRenderer = go.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            if (collider) go.AddComponent<BoxCollider>().size = size;
            return go;
        }

        /// <summary>Cutie rotunjită de mărime 1, scalată (pentru roci neregulate unde deformarea e chiar dorită).</summary>
        private static GameObject ScaledRock(string name, Transform parent, Vector3 localPosition, Vector3 scale, Material material, float yRotation)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);
            go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = RoundedMesh(Vector3.one, 0.22f);
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }

        /// <summary>Marchează decorul ca static: Unity îl unește la build (static batching), deci mai puține draw calls.</summary>
        private static void MarkStatic(GameObject root)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.SetStaticEditorFlags(child.gameObject, StaticEditorFlags.BatchingStatic);
        }
    }
}
