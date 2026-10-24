using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace QuarryEmpire.EditorTools
{
    public static partial class VisualSetup
    {
        // ================================================================ Prefab-uri de minat

        /// <summary>
        /// Bloc: mesh cu muchii rotunjite pe copilul „Visual” + BlockVfxRelay. Resursa: pepită low-poly.
        /// Colliderele, scripturile și valorile lor rămân exact cum le-a creat Stage1Setup.
        /// </summary>
        private static void UpgradeMiningPrefabs(Palette p)
        {
            EditPrefab($"{PrefabsDir}/Block.prefab", root =>
            {
                Transform visual = root.transform.Find("Visual")
                                   ?? throw new Exception("Block.prefab nu are copilul „Visual”.");
                visual.GetComponent<MeshFilter>().sharedMesh = p.BlockRounded;
                if (!root.TryGetComponent(out BlockVfxRelay _)) root.AddComponent<BlockVfxRelay>();
            });

            EditPrefab($"{PrefabsDir}/Pickup_Resource.prefab", root =>
            {
                root.GetComponent<MeshFilter>().sharedMesh = p.Nugget;
            });

            // Materialele de minereu: aspect mat; aurul și diamantul primesc puțin luciu/strălucire.
            TuneOre("M_Stone", 0.08f, 0f, null);
            TuneOre("M_Copper", 0.25f, 0.25f, null);
            TuneOre("M_Iron", 0.25f, 0.35f, null);
            TuneOre("M_Gold", 0.5f, 0.55f, VisualAssetFactory.Hex("#3A2A00"));
            TuneOre("M_Diamond", 0.65f, 0f, VisualAssetFactory.Hex("#1E6C78"));
            // Solul vechi rămâne neatins (îl înlocuim doar pe Ground în scenă).
        }

        private static void TuneOre(string name, float smoothness, float metallic, Color? emission)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>($"{StageMaterialsDir}/{name}.mat");
            if (material == null)
            {
                Debug.LogWarning($"[QuarryEmpire/Vizual] Lipsește {name}.mat (sărit).");
                return;
            }
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", metallic);
            if (emission.HasValue)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission.Value);
            }
            EditorUtility.SetDirty(material);
        }

        private static void EditPrefab(string path, Action<GameObject> edit)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                throw new Exception($"Lipsește {path} (rulează Setup Etapa 1).");
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                edit(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // ================================================================ Personaj

        /// <summary>
        /// Construiește minerul din primitive și cutii rotunjite sub „V_MinerVisual” (copil al Player).
        /// - Capsula și „nasul” vechi: doar MeshRenderer dezactivat (obiectele și referințele rămân).
        /// - PickaxePivot (referit de PickaxeSwing) e mutat în mâna dreaptă; PickaxeSwing continuă să-l rotească,
        ///   iar târnăcopul nou e copilul lui (cel vechi are doar randerele dezactivate).
        /// - CharacterController, scripturile și CollectPoint nu sunt atinse.
        /// </summary>
        private static void BuildCharacter(SceneRefs refs, Palette p)
        {
            if (refs.Player == null || refs.Movement == null || refs.Miner == null)
                throw new Exception("Player / PlayerMovement / PlayerMiner lipsesc — personajul nu poate fi construit.");

            Transform player = refs.Player.transform;
            Transform pivot = refs.PickaxePivot;

            // Re-rulare: scoatem întâi pivotul din vizualul vechi, apoi ștergem doar ce am creat noi.
            if (pivot != null)
            {
                pivot.SetParent(player, true);
                DestroyChild(pivot, VisualPrefix + "Pickaxe");
            }
            DestroyChild(player, VisualPrefix + "MinerVisual");

            foreach (string legacy in new[] { "Body", "Nose" }) HideRenderers(player.Find(legacy));
            if (pivot != null) HideRenderers(pivot.Find("Pickaxe"));

            Transform visual = Group(VisualPrefix + "MinerVisual", player, Vector3.zero).transform;
            visual.localScale = Vector3.one * 0.95f;
            Transform rig = Group("Rig", visual, Vector3.zero).transform;

            // ---- Șold și picioare
            MeshPart("Hips", rig, p.Box(0.56f, 0.22f, 0.4f, 0.1f), p.Overalls, new Vector3(0f, 0.84f, 0f));
            Transform legL = BuildLeg(rig, p, -0.14f);
            Transform legR = BuildLeg(rig, p, 0.14f);

            // ---- Trunchi
            Transform torso = Group("Torso", rig, new Vector3(0f, 0.86f, 0f)).transform;
            MeshPart("Shirt", torso, p.Box(0.66f, 0.62f, 0.44f, 0.16f), p.Shirt, new Vector3(0f, 0.38f, 0f));
            MeshPart("OverallsLower", torso, p.Box(0.7f, 0.4f, 0.48f, 0.14f), p.Overalls, new Vector3(0f, 0.1f, 0f));
            MeshPart("Bib", torso, p.Box(0.4f, 0.3f, 0.06f, 0.03f), p.Overalls, new Vector3(0f, 0.4f, 0.215f));
            MeshPart("StrapL", torso, p.Box(0.08f, 0.06f, 0.48f, 0.025f), p.OverallsDark, new Vector3(-0.15f, 0.64f, 0f));
            MeshPart("StrapR", torso, p.Box(0.08f, 0.06f, 0.48f, 0.025f), p.OverallsDark, new Vector3(0.15f, 0.64f, 0f));
            MeshPart("Belt", torso, p.Box(0.72f, 0.08f, 0.5f, 0.035f), p.Leather, new Vector3(0f, 0.27f, 0f));
            MeshPart("Buckle", torso, p.Box(0.12f, 0.09f, 0.04f, 0.015f), p.Gold, new Vector3(0f, 0.27f, 0.25f), castShadows: false);

            // ---- Cap și cască cu lanternă
            Transform head = Group("Head", torso, new Vector3(0f, 0.7f, 0f)).transform;
            PrimitivePart(PrimitiveType.Sphere, "Face", head, p.Skin, new Vector3(0f, 0.24f, 0f), Vector3.one * 0.5f);
            PrimitivePart(PrimitiveType.Sphere, "Nose", head, p.SkinShade, new Vector3(0f, 0.2f, 0.25f), Vector3.one * 0.12f, castShadows: false);
            PrimitivePart(PrimitiveType.Sphere, "EyeL", head, p.Dark, new Vector3(-0.09f, 0.29f, 0.225f), Vector3.one * 0.065f, castShadows: false);
            PrimitivePart(PrimitiveType.Sphere, "EyeR", head, p.Dark, new Vector3(0.09f, 0.29f, 0.225f), Vector3.one * 0.065f, castShadows: false);
            MeshPart("Mustache", head, p.Box(0.24f, 0.06f, 0.07f, 0.03f), p.Leather, new Vector3(0f, 0.14f, 0.23f), castShadows: false);
            PrimitivePart(PrimitiveType.Sphere, "HelmetDome", head, p.Helmet, new Vector3(0f, 0.4f, 0f), new Vector3(0.56f, 0.42f, 0.56f));
            PrimitivePart(PrimitiveType.Cylinder, "HelmetBrim", head, p.Helmet, new Vector3(0f, 0.33f, 0.02f), new Vector3(0.64f, 0.02f, 0.66f));
            PrimitivePart(PrimitiveType.Cylinder, "LampBase", head, p.Metal, new Vector3(0f, 0.43f, 0.27f), new Vector3(0.14f, 0.04f, 0.14f), new Vector3(90f, 0f, 0f), false);
            PrimitivePart(PrimitiveType.Sphere, "LampGlass", head, p.Lamp, new Vector3(0f, 0.43f, 0.305f), Vector3.one * 0.1f, castShadows: false);

            // ---- Rucsac + încărcătură vizibilă (Carry)
            Transform pack = Group("Backpack", torso, new Vector3(0f, 0.4f, -0.33f)).transform;
            MeshPart("Bag", pack, p.Box(0.5f, 0.56f, 0.26f, 0.1f), p.Backpack, Vector3.zero);
            MeshPart("Flap", pack, p.Box(0.52f, 0.16f, 0.28f, 0.06f), p.BackpackDark, new Vector3(0f, 0.2f, 0f));
            MeshPart("Pocket", pack, p.Box(0.3f, 0.18f, 0.08f, 0.04f), p.BackpackDark, new Vector3(0f, -0.1f, -0.15f), castShadows: false);
            var load = new List<Renderer>();
            Vector3[] loadPositions =
            {
                new(-0.12f, 0.32f, 0f), new(0.11f, 0.33f, -0.02f), new(0f, 0.4f, 0.01f), new(-0.02f, 0.31f, 0.08f),
            };
            for (int i = 0; i < loadPositions.Length; i++)
            {
                GameObject piece = MeshPart($"Load{i}", pack, p.Nugget, p.RockA, loadPositions[i],
                    new Vector3(0f, i * 70f, 0f), Vector3.one * 0.17f, false);
                load.Add(piece.GetComponent<Renderer>());
            }

            // ---- Brațul stâng (cel drept ține târnăcopul)
            Transform armL = Group("ArmL", torso, new Vector3(-0.4f, 0.56f, 0f)).transform;
            PrimitivePart(PrimitiveType.Capsule, "Sleeve", armL, p.Shirt, new Vector3(0f, -0.2f, 0f), new Vector3(0.19f, 0.24f, 0.19f));
            PrimitivePart(PrimitiveType.Sphere, "Glove", armL, p.Leather, new Vector3(0f, -0.46f, 0f), Vector3.one * 0.22f);

            Transform armR = null;
            if (pivot != null) armR = BuildPickaxeArm(pivot, torso, refs.Swing, p);
            else Debug.LogWarning("[QuarryEmpire/Vizual] PickaxeSwing.pivot lipsește: brațul drept nu e atașat.");

            // ---- Animator procedural
            var animator = visual.gameObject.AddComponent<MinerAnimator>();
            SetRef(animator, "movement", refs.Movement);
            SetRef(animator, "miner", refs.Miner);
            SetRef(animator, "stats", refs.Stats);
            SetRef(animator, "rig", rig);
            SetRef(animator, "torso", torso);
            SetRef(animator, "head", head);
            SetRef(animator, "leftArm", armL);
            SetRef(animator, "rightArm", armR);
            SetRef(animator, "leftLeg", legL);
            SetRef(animator, "rightLeg", legR);
            SetRefs(animator, "loadPieces", load.ToArray());
        }

        private static Transform BuildLeg(Transform rig, Palette p, float x)
        {
            Transform leg = Group(x < 0f ? "LegL" : "LegR", rig, new Vector3(x, 0.8f, 0f)).transform;
            PrimitivePart(PrimitiveType.Capsule, "Pants", leg, p.OverallsDark, new Vector3(0f, -0.32f, 0f), new Vector3(0.22f, 0.3f, 0.22f));
            MeshPart("Boot", leg, p.Box(0.24f, 0.18f, 0.36f, 0.07f), p.Boots, new Vector3(0f, -0.64f, 0.05f));
            MeshPart("Sole", leg, p.Box(0.25f, 0.05f, 0.37f, 0.02f), p.Dark, new Vector3(0f, -0.73f, 0.05f), castShadows: false);
            return leg;
        }

        /// <summary>
        /// Brațul drept (os animat de MinerAnimator) cu PickaxePivot mutat în mână. PickaxeSwing rotește în
        /// continuare pivotul („încheietura”), iar MinerAnimator ridică brațul în același timp, cu aceeași curbă:
        /// împreună dau o lovitură înainte-jos. Doar cele două unghiuri de poză ale PickaxeSwing se reglează
        /// (rest 45°, lovitură 180°); durata, curba și evenimentul Swung rămân neschimbate.
        /// </summary>
        private static Transform BuildPickaxeArm(Transform pivot, Transform torso, PickaxeSwing swing, Palette p)
        {
            Transform arm = Group("ArmR", torso, new Vector3(0.4f, 0.56f, 0f)).transform;
            PrimitivePart(PrimitiveType.Capsule, "Sleeve", arm, p.Shirt, new Vector3(0f, -0.2f, 0f), new Vector3(0.19f, 0.24f, 0.19f));
            PrimitivePart(PrimitiveType.Sphere, "Glove", arm, p.Leather, new Vector3(0f, -0.44f, 0f), Vector3.one * 0.22f);

            pivot.SetParent(arm, false);
            pivot.localPosition = new Vector3(0f, -0.44f, 0f);

            // În spațiul pivotului: +Y = mânerul (pornește puțin sub mână), capul sus.
            Transform pickaxe = Group(VisualPrefix + "Pickaxe", pivot, Vector3.zero).transform;
            PrimitivePart(PrimitiveType.Cylinder, "Handle", pickaxe, p.Wood, new Vector3(0f, 0.33f, 0f), new Vector3(0.075f, 0.475f, 0.075f));
            PrimitivePart(PrimitiveType.Cylinder, "Collar", pickaxe, p.Metal, new Vector3(0f, 0.78f, 0f), new Vector3(0.11f, 0.05f, 0.11f), castShadows: false);
            // Capul curbat: două jumătăți ușor înclinate în jos, cu vârfuri.
            Mesh half = p.Box(0.09f, 0.1f, 0.4f, 0.04f);
            MeshPart("HeadFront", pickaxe, half, p.Metal, new Vector3(0f, 0.78f, 0.18f), new Vector3(14f, 0f, 0f));
            MeshPart("HeadBack", pickaxe, half, p.Metal, new Vector3(0f, 0.78f, -0.18f), new Vector3(-14f, 0f, 0f));

            if (swing != null)
            {
                var so = new SerializedObject(swing);
                so.FindProperty("restEuler").vector3Value = new Vector3(45f, 0f, 0f);
                so.FindProperty("strikeEuler").vector3Value = new Vector3(180f, 0f, 0f);
                so.ApplyModifiedPropertiesWithoutUndo();
                pivot.localRotation = Quaternion.Euler(45f, 0f, 0f);
            }
            return arm;
        }

        private static void HideRenderers(Transform target)
        {
            if (target == null) return;
            foreach (Renderer renderer in target.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
        }
    }
}
