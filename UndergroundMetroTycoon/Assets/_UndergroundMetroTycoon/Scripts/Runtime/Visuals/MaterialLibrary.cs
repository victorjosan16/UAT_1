using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UndergroundMetroTycoon.Game
{
    /// <summary>
    /// Creates and caches one lit material per colour from a base material, so the same art direction works
    /// with URP (Simple Lit) or the Built-in pipeline (Standard). The base material asset is created by the
    /// editor setup and referenced from the scene, which keeps its shader in Android builds.
    /// </summary>
    public sealed class MaterialLibrary
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");
        private static readonly int GlossinessId = Shader.PropertyToID("_Glossiness");

        private readonly Material _base;
        private readonly Dictionary<Color32, Material> _cache = new Dictionary<Color32, Material>();
        private readonly List<Material> _created = new List<Material>();

        public MaterialLibrary(Material baseMaterial)
        {
            _base = baseMaterial != null ? baseMaterial : CreateFallbackBase();
        }

        public Material Get(Color color)
        {
            Color32 key = color;
            if (_cache.TryGetValue(key, out Material material)) return material;
            material = new Material(_base) { name = "UMT_" + ColorUtility.ToHtmlStringRGB(color), enableInstancing = true };
            SetColor(material, color);
            if (material.HasProperty(SmoothnessId)) material.SetFloat(SmoothnessId, 0.12f);
            if (material.HasProperty(GlossinessId)) material.SetFloat(GlossinessId, 0.12f);
            _cache.Add(key, material);
            _created.Add(material);
            return material;
        }

        public static void SetColor(Material material, Color color)
        {
            if (material.HasProperty(BaseColorId)) material.SetColor(BaseColorId, color);
            if (material.HasProperty(ColorId)) material.SetColor(ColorId, color);
        }

        public static void SetColor(MaterialPropertyBlock block, Color color)
        {
            block.SetColor(BaseColorId, color);
            block.SetColor(ColorId, color);
        }

        public void DestroyAll()
        {
            foreach (Material m in _created)
            {
                if (m != null) Object.Destroy(m);
            }
            _created.Clear();
            _cache.Clear();
        }

        private static Material CreateFallbackBase()
        {
            bool srp = GraphicsSettings.currentRenderPipeline != null;
            Shader shader = srp
                ? Shader.Find("Universal Render Pipeline/Simple Lit") ?? Shader.Find("Universal Render Pipeline/Lit")
                : Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Diffuse") ?? Shader.Find("Unlit/Color");
            Debug.LogWarning("[UMT] No base material assigned on GameBootstrap; using runtime fallback shader " +
                             (shader != null ? shader.name : "<none>") + ". Run 'Underground Metro Tycoon > Setup Project' for builds.");
            return new Material(shader);
        }
    }
}
