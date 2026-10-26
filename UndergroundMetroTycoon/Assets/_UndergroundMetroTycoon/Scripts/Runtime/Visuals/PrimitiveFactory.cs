using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UndergroundMetroTycoon.Game
{
    /// <summary>Builds collider-free mesh objects from Unity's primitive meshes (procedural placeholder art).</summary>
    public static class PrimitiveFactory
    {
        private static readonly Dictionary<PrimitiveType, Mesh> Meshes = new Dictionary<PrimitiveType, Mesh>();

        public static Mesh GetMesh(PrimitiveType type)
        {
            if (Meshes.TryGetValue(type, out Mesh mesh) && mesh != null) return mesh;
            GameObject temp = GameObject.CreatePrimitive(type);
            mesh = temp.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(temp);
            Meshes[type] = mesh;
            return mesh;
        }

        public static GameObject Create(string name, PrimitiveType type, Transform parent, Vector3 localPosition, Vector3 localScale,
            Material material, bool castShadows = true)
        {
            var go = new GameObject(name);
            Transform t = go.transform;
            t.SetParent(parent, false);
            t.localPosition = localPosition;
            t.localScale = localScale;
            go.AddComponent<MeshFilter>().sharedMesh = GetMesh(type);
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            return go;
        }

        public static GameObject Box(string name, Transform parent, Vector3 localPosition, Vector3 size, Material material, bool castShadows = true) =>
            Create(name, PrimitiveType.Cube, parent, localPosition, size, material, castShadows);

        public static Transform Pivot(string name, Transform parent, Vector3 localPosition)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = localPosition;
            return t;
        }
    }
}
