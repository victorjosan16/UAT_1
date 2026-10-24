using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace QuarryEmpire.EditorTools
{
    public static partial class VisualSetup
    {
        private const string VfxRootName = VisualPrefix + "VFX";
        private const string FloatingTextPrefabPath = PrefabsDir + "/FX_FloatingText.prefab";
        private const string FloatingTextMaterialPath = VisualAssetFactory.MaterialsDir + "/M_V_FloatingTextOutline.mat";

        /// <summary>
        /// Un ParticleSystem per efect, sub _Systems/V_VFX (reglabile din Inspector), plus VfxManager,
        /// CollectFeedbackRelay și prefab-ul de text plutitor adăugat în prewarm-ul ObjectPool-ului existent.
        /// Toate sistemele: fără emisie continuă, fără coliziuni, fără umbre, maxParticles mic.
        /// </summary>
        private static void BuildEffects(SceneRefs refs, Palette p)
        {
            Transform parent = refs.Systems != null ? refs.Systems.transform : null;
            if (parent != null) DestroyChild(parent, VfxRootName);
            else DestroyRoot(VfxRootName);

            var root = new GameObject(VfxRootName);
            root.transform.SetParent(parent, false);
            var vfx = root.AddComponent<VfxManager>();
            root.AddComponent<CollectFeedbackRelay>();

            ParticleSystem impactChips = Burst(root.transform, "FX_ImpactChips", p.FxChunk, 60, s =>
            {
                Main(s, 0.35f, 0.6f, 2.5f, 4.5f, 0.06f, 0.12f, 1.6f);
                Shape(s, ParticleSystemShapeType.Hemisphere, 0.15f, new Vector3(-90f, 0f, 0f)); // cupola în sus
                MeshRender(s, p.Nugget);
            });
            ParticleSystem impactDust = Burst(root.transform, "FX_ImpactDust", p.FxDust, 30, s =>
            {
                Main(s, 0.35f, 0.55f, 0.6f, 1.2f, 0.35f, 0.6f, -0.05f);
                SetStartColor(s, new Color(0.93f, 0.88f, 0.8f, 0.55f));
                Shape(s, ParticleSystemShapeType.Sphere, 0.2f);
                FadeAndGrow(s, 1.6f);
            });
            ParticleSystem breakChunks = Burst(root.transform, "FX_BreakChunks", p.FxChunk, 120, s =>
            {
                Main(s, 0.6f, 0.9f, 3f, 5.5f, 0.12f, 0.24f, 2.2f);
                Shape(s, ParticleSystemShapeType.Box, 0.4f);
                MeshRender(s, p.Nugget);
                ShrinkAtEnd(s);
            });
            ParticleSystem breakDust = Burst(root.transform, "FX_BreakDust", p.FxDust, 60, s =>
            {
                Main(s, 0.5f, 0.8f, 1.2f, 2.2f, 0.6f, 1f, -0.1f);
                SetStartColor(s, new Color(0.9f, 0.85f, 0.76f, 0.6f));
                Shape(s, ParticleSystemShapeType.Circle, 0.4f, new Vector3(-90f, 0f, 0f));
                FadeAndGrow(s, 1.8f);
            });
            ParticleSystem collect = Burst(root.transform, "FX_CollectSparkle", p.FxSparkle, 60, s =>
            {
                Main(s, 0.3f, 0.5f, 1f, 2.2f, 0.18f, 0.32f, -0.2f);
                Shape(s, ParticleSystemShapeType.Sphere, 0.15f);
                FadeAndGrow(s, 0.4f);
            });
            ParticleSystem sell = Burst(root.transform, "FX_SellCoins", p.FxCoin, 80, s =>
            {
                Main(s, 0.7f, 1f, 4f, 6.5f, 0.28f, 0.4f, 2f);
                Shape(s, ParticleSystemShapeType.Cone, 0.3f, new Vector3(-90f, 0f, 0f), 25f);
                ShrinkAtEnd(s);
            });
            ParticleSystem upgrade = Burst(root.transform, "FX_UpgradeSparkle", p.FxSparkle, 80, s =>
            {
                Main(s, 0.8f, 1.2f, 1.5f, 3f, 0.2f, 0.38f, -0.4f);
                SetStartColor(s, new Color(0.6f, 0.85f, 1f, 1f));
                Shape(s, ParticleSystemShapeType.Circle, 1.1f, new Vector3(-90f, 0f, 0f));
                FadeAndGrow(s, 0.6f);
            });

            var so = new SerializedObject(vfx);
            so.FindProperty("impactChips").objectReferenceValue = impactChips;
            so.FindProperty("impactDust").objectReferenceValue = impactDust;
            so.FindProperty("breakChunks").objectReferenceValue = breakChunks;
            so.FindProperty("breakDust").objectReferenceValue = breakDust;
            so.FindProperty("collectSparkle").objectReferenceValue = collect;
            so.FindProperty("sellCoins").objectReferenceValue = sell;
            so.FindProperty("upgradeSparkle").objectReferenceValue = upgrade;
            so.FindProperty("floatingTextPrefab").objectReferenceValue = CreateFloatingTextPrefab();
            so.ApplyModifiedPropertiesWithoutUndo();

            AddPoolPrewarm(refs.Pool, AssetDatabase.LoadAssetAtPath<GameObject>(FloatingTextPrefabPath), 10);
        }

        private static ParticleSystem Burst(Transform parent, string name, Material material, int maxParticles,
            Action<ParticleSystem> configure)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = system.main;
            main.playOnAwake = true;
            main.loop = true; // „pornit” mereu, fără emisie proprie: Emit() din VfxManager simulează normal
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = maxParticles;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.enabled = false;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.sortMode = ParticleSystemSortMode.None;

            configure(system);
            return system;
        }

        private static void Main(ParticleSystem s, float lifeMin, float lifeMax, float speedMin, float speedMax,
            float sizeMin, float sizeMax, float gravity)
        {
            ParticleSystem.MainModule main = s.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifeMin, lifeMax);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
            main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = gravity;
        }

        private static void SetStartColor(ParticleSystem s, Color color)
        {
            ParticleSystem.MainModule main = s.main;
            main.startColor = color;
        }

        private static void Shape(ParticleSystem s, ParticleSystemShapeType type, float radius,
            Vector3? rotation = null, float angle = 25f)
        {
            ParticleSystem.ShapeModule shape = s.shape;
            shape.enabled = true;
            shape.shapeType = type;
            shape.radius = radius;
            shape.scale = type == ParticleSystemShapeType.Box ? Vector3.one * radius * 2f : Vector3.one;
            shape.angle = angle;
            if (rotation.HasValue) shape.rotation = rotation.Value;
        }

        private static void MeshRender(ParticleSystem s, Mesh mesh)
        {
            var renderer = s.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = mesh;
            ParticleSystem.MainModule main = s.main;
            main.startRotation3D = true;
            main.startRotationX = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startRotationY = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            ParticleSystem.RotationOverLifetimeModule rotation = s.rotationOverLifetime;
            rotation.enabled = true;
            rotation.separateAxes = true;
            rotation.x = new ParticleSystem.MinMaxCurve(-8f, 8f);
            rotation.y = new ParticleSystem.MinMaxCurve(-8f, 8f);
            rotation.z = new ParticleSystem.MinMaxCurve(-8f, 8f);
        }

        private static void FadeAndGrow(ParticleSystem s, float growTo)
        {
            ParticleSystem.ColorOverLifetimeModule color = s.colorOverLifetime;
            color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.4f), new GradientAlphaKey(0f, 1f) });
            color.color = new ParticleSystem.MinMaxGradient(gradient);

            ParticleSystem.SizeOverLifetimeModule size = s.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, growTo));
        }

        private static void ShrinkAtEnd(ParticleSystem s)
        {
            ParticleSystem.SizeOverLifetimeModule size = s.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 1f), new Keyframe(0.7f, 1f), new Keyframe(1f, 0f)));
        }

        private static GameObject CreateFloatingTextPrefab()
        {
            var go = new GameObject("FX_FloatingText");
            var label = go.AddComponent<TextMeshPro>();
            label.text = "+1$";
            label.fontSize = 5f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.rectTransform.sizeDelta = new Vector2(4f, 1.2f);
            label.raycastTarget = false;
            Material outline = FloatingTextMaterial();
            if (outline != null) label.fontSharedMaterial = outline;
            if (go.TryGetComponent(out MeshRenderer renderer))
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            var floating = go.AddComponent<FloatingText>();
            SetRef(floating, "label", label);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, FloatingTextPrefabPath);
            Object.DestroyImmediate(go);
            return prefab;
        }

        /// <summary>Copie a materialului fontului implicit, cu contur închis (lizibil pe orice fundal), partajată.</summary>
        private static Material FloatingTextMaterial()
        {
            TMP_FontAsset font = TMP_Settings.defaultFontAsset;
            if (font == null || font.material == null) return null;
            var material = AssetDatabase.LoadAssetAtPath<Material>(FloatingTextMaterialPath);
            if (material == null)
            {
                material = new Material(font.material);
                AssetDatabase.CreateAsset(material, FloatingTextMaterialPath);
            }
            else
            {
                material.CopyPropertiesFromMaterial(font.material);
                material.shader = font.material.shader;
            }
            material.EnableKeyword("OUTLINE_ON");
            material.SetFloat("_OutlineWidth", 0.22f);
            material.SetColor("_OutlineColor", VisualAssetFactory.Hex("#2A1E2E"));
            material.SetFloat("_FaceDilate", 0.15f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void AddPoolPrewarm(ObjectPool pool, GameObject prefab, int count)
        {
            if (pool == null || prefab == null)
            {
                Debug.LogWarning("[QuarryEmpire/Vizual] ObjectPool sau prefab-ul de text lipsește: textul plutitor se creează la nevoie.");
                return;
            }
            var so = new SerializedObject(pool);
            SerializedProperty list = so.FindProperty("prewarm");
            for (int i = 0; i < list.arraySize; i++)
                if (list.GetArrayElementAtIndex(i).FindPropertyRelative("prefab").objectReferenceValue == prefab) return;
            int index = list.arraySize;
            list.arraySize++;
            SerializedProperty entry = list.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("prefab").objectReferenceValue = prefab;
            entry.FindPropertyRelative("count").intValue = count;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
