using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace QuarryEmpire.EditorTools
{
    public static partial class VisualSetup
    {
        private const string PerformanceObjectName = VisualPrefix + "PerformanceManager";

        /// <summary>
        /// Două profiluri (60 FPS „Calitate” și 30 FPS „Economic”), fiecare cu o copie a asset-ului URP
        /// existent (nu migrăm nimic: proiectul e deja pe URP). Asset-urile se creează o singură dată;
        /// după aceea le poți regla liber în Inspector — rularea din nou nu le suprascrie.
        /// </summary>
        private static void BuildPerformance(SceneRefs refs)
        {
            var basePipeline = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
            if (basePipeline == null) throw new Exception("Proiectul nu are un asset URP implicit (rulează Setup Etapa 1).");
            string basePath = AssetDatabase.GetAssetPath(basePipeline);

            UniversalRenderPipelineAsset high = ClonePipeline(basePath, "URP_Mobile_High60", pipeline =>
            {
                pipeline.renderScale = 0.9f;
                pipeline.msaaSampleCount = 2;
                pipeline.shadowDistance = 22f;
                ConfigureCommon(pipeline, softShadows: true, shadowResolution: 2048);
            });
            UniversalRenderPipelineAsset low = ClonePipeline(basePath, "URP_Mobile_Low30", pipeline =>
            {
                pipeline.renderScale = 0.75f;
                pipeline.msaaSampleCount = 1;
                pipeline.shadowDistance = 14f;
                ConfigureCommon(pipeline, softShadows: false, shadowResolution: 1024);
            });

            PerformanceProfile highProfile = CreateProfile("Profile_High60", "Calitate (60 FPS)", 60, high, 1f);
            PerformanceProfile lowProfile = CreateProfile("Profile_Low30", "Economic (30 FPS)", 30, low, 0.6f);

            Transform parent = refs.Systems != null ? refs.Systems.transform : null;
            if (parent != null) DestroyChild(parent, PerformanceObjectName);
            else DestroyRoot(PerformanceObjectName);
            var go = new GameObject(PerformanceObjectName);
            go.transform.SetParent(parent, false);
            var manager = go.AddComponent<PerformanceManager>();
            SetRefs(manager, "profiles", new Object[] { highProfile, lowProfile });
        }

        private static UniversalRenderPipelineAsset ClonePipeline(string basePath, string name,
            Action<UniversalRenderPipelineAsset> configure)
        {
            string path = $"{PerformanceDir}/{name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
            if (existing != null) return existing;

            // Copie a asset-ului de bază: păstrează renderer-ul (și orice reglaj existent).
            if (!AssetDatabase.CopyAsset(basePath, path)) throw new Exception("Nu pot copia " + basePath);
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
            configure(pipeline);
            EditorUtility.SetDirty(pipeline);
            return pipeline;
        }

        private static void ConfigureCommon(UniversalRenderPipelineAsset pipeline, bool softShadows, int shadowResolution)
        {
            pipeline.shadowCascadeCount = 1;      // o singură cascadă: scena e mică și camera e fixă
            pipeline.supportsHDR = false;          // mai puțină lățime de bandă pe mobil
            pipeline.supportsCameraDepthTexture = false;
            pipeline.supportsCameraOpaqueTexture = false;
            pipeline.useSRPBatcher = true;

            // Câmpuri fără setter public: le setăm prin SerializedObject (ignorate dacă versiunea URP le-a redenumit).
            var so = new SerializedObject(pipeline);
            SetIfExists(so, "m_SoftShadowsSupported", p => p.boolValue = softShadows);
            SetIfExists(so, "m_MainLightShadowmapResolution", p => p.intValue = shadowResolution);
            SetIfExists(so, "m_AdditionalLightsRenderingMode", p => p.intValue = 0); // fără lumini adiționale
            SetIfExists(so, "m_SupportsDynamicBatching", p => p.boolValue = false);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetIfExists(SerializedObject so, string name, Action<SerializedProperty> set)
        {
            SerializedProperty property = so.FindProperty(name);
            if (property != null) set(property);
            else Debug.LogWarning($"[QuarryEmpire/Vizual] URP: câmpul {name} nu există în această versiune (sărit).");
        }

        private static PerformanceProfile CreateProfile(string fileName, string displayName, int fps,
            RenderPipelineAsset pipeline, float particleDensity)
        {
            string path = $"{PerformanceDir}/{fileName}.asset";
            var profile = AssetDatabase.LoadAssetAtPath<PerformanceProfile>(path);
            if (profile != null) return profile;

            profile = ScriptableObject.CreateInstance<PerformanceProfile>();
            AssetDatabase.CreateAsset(profile, path);
            var so = new SerializedObject(profile);
            Auto(so, "DisplayName").stringValue = displayName;
            Auto(so, "TargetFrameRate").intValue = fps;
            Auto(so, "PipelineAsset").objectReferenceValue = pipeline;
            Auto(so, "ParticleDensity").floatValue = particleDensity;
            Auto(so, "CameraShadows").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            return profile;
        }
    }
}
