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
        private const string EnvironmentRoot = VisualPrefix + "Environment";

        // Pozițiile vin din scena Etapei 1: grila 8x8 centrată în (0, 0, 6), jucătorul pornește în (0, 0, 0).
        private static readonly Vector3 SellZonePosition = new(-6f, 0f, -3f);
        private static readonly Vector3 UpgradeZonePosition = new(6f, 0f, -3f);

        /// <summary>
        /// Mediu doar decorativ: niciun obiect nou nu are collider, deci nu schimbă unde poate merge jucătorul
        /// și nu interferează cu OverlapSphere-urile de minat/colectare. Decorul e marcat static (batching).
        /// </summary>
        private static void BuildEnvironment(SceneRefs refs, Palette p)
        {
            DestroyRoot(EnvironmentRoot);
            Transform root = new GameObject(EnvironmentRoot).transform;

            // ---- Sol: doar materialul se schimbă pe Ground (colliderul rămâne). Un plan mare fără collider
            // ascunde marginea hărții.
            if (refs.Ground != null && refs.Ground.TryGetComponent(out MeshRenderer groundRenderer))
                groundRenderer.sharedMaterial = p.Grass;
            PrimitivePart(PrimitiveType.Plane, "OuterGround", root, p.GrassOuter, new Vector3(0f, -0.03f, 0f), new Vector3(14f, 1f, 14f), castShadows: false);

            Vector3 gridCenter = refs.Grid != null ? refs.Grid.transform.position : new Vector3(0f, 0f, 6f);
            BuildQuarry(root, p, gridCenter);
            BuildRoads(root, p, gridCenter);
            BuildZone(root, p, refs, ZoneVisual.ZoneKind.Sell, SellZonePosition);
            BuildZone(root, p, refs, ZoneVisual.ZoneKind.Upgrade, UpgradeZonePosition);
            BuildDecor(root, p, gridCenter);

            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                // Zonele au animații, deci nu sunt statice.
                if (child.GetComponentInParent<ZoneVisual>() != null) continue;
                // Textul TMP își generează mesh-ul la rulare: static batching l-ar face invizibil.
                if (child.GetComponent<TMP_Text>() != null) continue;
                GameObjectUtility.SetStaticEditorFlags(child.gameObject, StaticEditorFlags.BatchingStatic);
            }

            ApplyLighting(refs);
        }

        private static void BuildQuarry(Transform root, Palette p, Vector3 center)
        {
            Transform quarry = new GameObject("Quarry").transform;
            quarry.SetParent(root, false);

            // Podeaua carierei: 2 cm peste sol (sub nivelul la care se văd picioarele), fără collider.
            MeshPart("PitFloor", quarry, p.Box(9.8f, 0.04f, 9.8f, 0.02f), p.PitFloor, center + new Vector3(0f, 0f, 0f), castShadows: false);

            // Margine din pietre neregulate, cu deschidere spre jucător (sud).
            var random = new System.Random(42);
            const float half = 5.1f;
            for (float t = -half; t <= half; t += 0.85f)
            {
                PlaceRimRock(quarry, p, random, center + new Vector3(t, 0f, half));        // nord
                PlaceRimRock(quarry, p, random, center + new Vector3(-half, 0f, t));       // vest
                PlaceRimRock(quarry, p, random, center + new Vector3(half, 0f, t));        // est
                if (Mathf.Abs(t) > 2f) PlaceRimRock(quarry, p, random, center + new Vector3(t, 0f, -half)); // sud, cu intrare
            }

            // Felinare la intrare (doar emisie, fără lumini reale — ieftin pe mobil).
            foreach (float x in new[] { -2.3f, 2.3f }) BuildLantern(quarry, p, center + new Vector3(x, 0f, -half - 0.2f));

            // Vagonet pe șine lângă latura de est.
            Transform cart = new GameObject("MineCart").transform;
            cart.SetParent(quarry, false);
            cart.position = center + new Vector3(half + 1.3f, 0f, -1f);
            for (int i = -4; i <= 4; i++)
                MeshPart($"Sleeper{i}", cart, p.Box(1.1f, 0.06f, 0.18f, 0.02f), p.Wood, new Vector3(0f, 0.03f, i * 0.55f), castShadows: false);
            MeshPart("RailL", cart, p.Box(0.07f, 0.07f, 5f, 0.02f), p.Rail, new Vector3(-0.35f, 0.09f, 0f), castShadows: false);
            MeshPart("RailR", cart, p.Box(0.07f, 0.07f, 5f, 0.02f), p.Rail, new Vector3(0.35f, 0.09f, 0f), castShadows: false);
            MeshPart("CartBody", cart, p.Box(0.95f, 0.55f, 1.3f, 0.12f), p.CartBody, new Vector3(0f, 0.5f, 0.8f));
            MeshPart("CartRim", cart, p.Box(1.02f, 0.08f, 1.38f, 0.04f), p.Rail, new Vector3(0f, 0.78f, 0.8f), castShadows: false);
            for (int i = 0; i < 3; i++)
                MeshPart($"CartOre{i}", cart, p.Nugget, i == 1 ? p.RockB : p.RockA,
                    new Vector3(-0.2f + i * 0.2f, 0.8f, 0.7f + i * 0.12f), new Vector3(0f, i * 50f, 0f), Vector3.one * 0.35f, false);
            foreach (float x in new[] { -0.4f, 0.4f })
            foreach (float z in new[] { 0.4f, 1.2f })
                PrimitivePart(PrimitiveType.Cylinder, "Wheel", cart, p.Dark, new Vector3(x, 0.2f, z), new Vector3(0.3f, 0.04f, 0.3f), new Vector3(0f, 0f, 90f), false);
        }

        private static void PlaceRimRock(Transform parent, Palette p, System.Random random, Vector3 position)
        {
            float jitterX = (float)(random.NextDouble() - 0.5) * 0.4f;
            float jitterZ = (float)(random.NextDouble() - 0.5) * 0.4f;
            float scale = 0.55f + (float)random.NextDouble() * 0.45f;
            Mesh mesh = p.Rocks[random.Next(p.Rocks.Length)];
            MeshPart("RimRock", parent, mesh, random.Next(2) == 0 ? p.RockA : p.RockB,
                position + new Vector3(jitterX, 0f, jitterZ),
                new Vector3(0f, (float)random.NextDouble() * 360f, 0f),
                new Vector3(scale * 1.2f, scale, scale * 1.1f),
                castShadows: scale > 0.8f);
        }

        private static void BuildRoads(Transform root, Palette p, Vector3 quarryCenter)
        {
            Transform roads = new GameObject("Roads").transform;
            roads.SetParent(root, false);
            float entranceZ = quarryCenter.z - 5f;
            // Drum principal: de la intersecție până la intrarea în carieră.
            float mainStart = SellZonePosition.z;
            float mainLength = entranceZ - mainStart;
            MeshPart("MainRoad", roads, p.Box(2.2f, 0.03f, mainLength, 0.015f), p.Road,
                new Vector3(quarryCenter.x, 0f, mainStart + mainLength * 0.5f), castShadows: false);
            // Drum transversal între zona de vânzare și cea de upgrade.
            float crossLength = UpgradeZonePosition.x - SellZonePosition.x;
            MeshPart("CrossRoad", roads, p.Box(crossLength, 0.028f, 1.8f, 0.015f), p.Road,
                new Vector3((SellZonePosition.x + UpgradeZonePosition.x) * 0.5f, 0f, SellZonePosition.z), castShadows: false);

            // Pietricele pe margini, ca drumul să nu pară un dreptunghi perfect.
            var random = new System.Random(7);
            for (float x = SellZonePosition.x + 1.5f; x < UpgradeZonePosition.x - 1.5f; x += 1.3f)
            {
                if (Mathf.Abs(x - quarryCenter.x) < 1.4f) continue;
                foreach (float side in new[] { -1.05f, 1.05f })
                    MeshPart("Pebble", roads, p.Rocks[random.Next(p.Rocks.Length)], p.RockA,
                        new Vector3(x + (float)random.NextDouble() * 0.4f, 0f, SellZonePosition.z + side),
                        new Vector3(0f, random.Next(360), 0f), Vector3.one * (0.18f + (float)random.NextDouble() * 0.1f), false);
            }
        }

        private static void BuildZone(Transform root, Palette p, SceneRefs refs, ZoneVisual.ZoneKind kind, Vector3 position)
        {
            bool sell = kind == ZoneVisual.ZoneKind.Sell;
            Material pad = sell ? p.SellPad : p.UpgradePad;
            Material accent = sell ? p.SellAccent : p.UpgradeAccent;

            Transform zone = new GameObject(sell ? "SellZone" : "UpgradeZone").transform;
            zone.SetParent(root, false);
            zone.position = position;

            PrimitivePart(PrimitiveType.Cylinder, "Pad", zone, pad, new Vector3(0f, 0.02f, 0f), new Vector3(2.3f, 0.02f, 2.3f), castShadows: false);
            GameObject ring = MeshPart("PulseRing", zone, p.ZoneRing, accent, new Vector3(0f, 0.05f, 0f), castShadows: false);

            // Semn: stâlp + panou cu text, orientat spre camera izometrică (sud-vest).
            Transform sign = new GameObject("Sign").transform;
            sign.SetParent(zone, false);
            sign.localPosition = new Vector3(sell ? -1.3f : 1.3f, 0f, 1.1f);
            sign.localRotation = Quaternion.Euler(0f, 45f, 0f);
            PrimitivePart(PrimitiveType.Cylinder, "Post", sign, p.Wood, new Vector3(0f, 0.75f, 0f), new Vector3(0.12f, 0.75f, 0.12f));
            MeshPart("Board", sign, p.Box(1.5f, 0.55f, 0.1f, 0.08f), p.SignBoard, new Vector3(0f, 1.55f, 0f));
            MeshPart("BoardTrim", sign, p.Box(1.6f, 0.65f, 0.06f, 0.1f), pad, new Vector3(0f, 1.55f, 0.03f));
            CreateWorldLabel(sign, sell ? "VINDE" : "UPGRADE", new Vector3(0f, 1.55f, -0.07f), sell ? "#1F6B3A" : "#24418A");

            // Iconul care plutește: monedă (vânzare) sau săgeată (upgrade).
            Transform icon = new GameObject("FloatingIcon").transform;
            icon.SetParent(zone, false);
            icon.localPosition = new Vector3(0f, 1.6f, 0f);
            if (sell)
            {
                PrimitivePart(PrimitiveType.Cylinder, "Coin", icon, p.Gold, Vector3.zero, new Vector3(0.75f, 0.06f, 0.75f), new Vector3(90f, 0f, 0f));
                PrimitivePart(PrimitiveType.Cylinder, "CoinInner", icon, accent, Vector3.zero, new Vector3(0.5f, 0.07f, 0.5f), new Vector3(90f, 0f, 0f), false);
            }
            else
            {
                MeshPart("Shaft", icon, p.Box(0.24f, 0.55f, 0.2f, 0.08f), accent, new Vector3(0f, -0.12f, 0f));
                MeshPart("HeadL", icon, p.Box(0.24f, 0.46f, 0.2f, 0.08f), accent, new Vector3(-0.14f, 0.18f, 0f), new Vector3(0f, 0f, -45f));
                MeshPart("HeadR", icon, p.Box(0.24f, 0.46f, 0.2f, 0.08f), accent, new Vector3(0.14f, 0.18f, 0f), new Vector3(0f, 0f, 45f));
            }

            var visual = zone.gameObject.AddComponent<ZoneVisual>();
            var so = new SerializedObject(visual);
            so.FindProperty("kind").enumValueIndex = (int)kind;
            so.FindProperty("pulseRing").objectReferenceValue = ring.transform;
            so.FindProperty("floatingIcon").objectReferenceValue = icon;
            so.FindProperty("player").objectReferenceValue = refs.Player != null ? refs.Player.transform : null;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateWorldLabel(Transform parent, string text, Vector3 localPosition, string colorHex)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            // Fața textului TMP 3D privește spre -Z local, adică spre cameră.
            var label = go.AddComponent<TextMeshPro>();
            label.text = text;
            label.fontSize = 4f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = VisualAssetFactory.Hex(colorHex);
            label.rectTransform.sizeDelta = new Vector2(1.4f, 0.5f);
            label.enableAutoSizing = true;
            label.fontSizeMin = 1f;
            label.fontSizeMax = 4f;
        }

        private static void BuildLantern(Transform parent, Palette p, Vector3 position)
        {
            Transform lantern = new GameObject("Lantern").transform;
            lantern.SetParent(parent, false);
            lantern.position = position;
            PrimitivePart(PrimitiveType.Cylinder, "Post", lantern, p.Wood, new Vector3(0f, 0.7f, 0f), new Vector3(0.1f, 0.7f, 0.1f));
            MeshPart("Cap", lantern, p.Box(0.3f, 0.08f, 0.3f, 0.03f), p.Dark, new Vector3(0f, 1.62f, 0f), castShadows: false);
            PrimitivePart(PrimitiveType.Sphere, "Glow", lantern, p.LanternGlow, new Vector3(0f, 1.48f, 0f), Vector3.one * 0.24f, castShadows: false);
        }

        private static void BuildDecor(Transform root, Palette p, Vector3 quarryCenter)
        {
            Transform decor = new GameObject("Decor").transform;
            decor.SetParent(root, false);
            var random = new System.Random(2024);

            // Tufișuri (2-3 sfere), în afara drumurilor și a zonelor de joc.
            Vector3[] bushes =
            {
                new(-11f, 0f, 0f), new(-7.5f, 0f, -8.5f), new(9.5f, 0f, -8.5f), new(12f, 0f, 4f), new(-12f, 0f, 9f),
                new(1.5f, 0f, -9.5f), new(7f, 0f, 13f), new(-6.5f, 0f, 13.5f), new(-10f, 0f, -4.5f), new(11f, 0f, 10.5f),
            };
            foreach (Vector3 position in bushes)
            {
                Transform bush = new GameObject("Bush").transform;
                bush.SetParent(decor, false);
                bush.position = position;
                int parts = 2 + random.Next(2);
                for (int i = 0; i < parts; i++)
                {
                    float size = 0.7f + (float)random.NextDouble() * 0.5f;
                    var offset = new Vector3((float)(random.NextDouble() - 0.5) * 0.9f, size * 0.38f, (float)(random.NextDouble() - 0.5) * 0.9f);
                    PrimitivePart(PrimitiveType.Sphere, "Leaves", bush, i % 2 == 0 ? p.Bush : p.BushDark, offset,
                        new Vector3(size, size * 0.85f, size), castShadows: i == 0);
                }
            }

            // Grupuri de pietre mari.
            Vector3[] clusters = { new(-9f, 0f, 4.5f), new(9.5f, 0f, -1f), new(-8.5f, 0f, -1f), new(3f, 0f, 13.5f), new(-13f, 0f, -9f), new(13f, 0f, -6f) };
            foreach (Vector3 c in clusters)
            {
                for (int i = 0; i < 3; i++)
                {
                    float scale = 0.6f + (float)random.NextDouble() * 0.9f;
                    var offset = new Vector3((float)(random.NextDouble() - 0.5) * 1.4f, 0f, (float)(random.NextDouble() - 0.5) * 1.4f);
                    MeshPart("Boulder", decor, p.Rocks[random.Next(p.Rocks.Length)], i == 0 ? p.RockB : p.RockA, c + offset,
                        new Vector3(0f, random.Next(360), 0f), new Vector3(scale * 1.3f, scale, scale * 1.15f), castShadows: i == 0);
                }
            }

            // Gard în spatele carierei.
            float fenceZ = quarryCenter.z + 6.6f;
            for (float x = -6f; x <= 6.01f; x += 1.5f)
                MeshPart("FencePost", decor, p.Box(0.16f, 0.9f, 0.16f, 0.05f), p.Fence, new Vector3(x, 0.45f, fenceZ), castShadows: false);
            foreach (float y in new[] { 0.35f, 0.7f })
                MeshPart("FenceRail", decor, p.Box(12.2f, 0.09f, 0.07f, 0.03f), p.Fence, new Vector3(0f, y, fenceZ), castShadows: false);

            // Lăzi lângă zona de vânzare.
            MeshPart("Crate", decor, p.Box(0.7f, 0.7f, 0.7f, 0.08f), p.Crate, SellZonePosition + new Vector3(-2f, 0.35f, -1.2f), new Vector3(0f, 12f, 0f));
            MeshPart("Crate", decor, p.Box(0.6f, 0.6f, 0.6f, 0.07f), p.Crate, SellZonePosition + new Vector3(-1.9f, 1f, -1.15f), new Vector3(0f, -8f, 0f));
            MeshPart("Crate", decor, p.Box(0.6f, 0.6f, 0.6f, 0.07f), p.Crate, SellZonePosition + new Vector3(-1.2f, 0.3f, -1.7f), new Vector3(0f, 30f, 0f));
        }

        /// <summary>Lumină caldă, ambient în 3 tonuri, ceață ușoară (dă adâncime și ascunde marginea hărții).</summary>
        private static void ApplyLighting(SceneRefs refs)
        {
            Color sky = VisualAssetFactory.Hex("#A9D4F2");
            if (refs.Sun != null)
            {
                refs.Sun.color = VisualAssetFactory.Hex("#FFE6C2");
                refs.Sun.intensity = 1.35f;
                refs.Sun.shadows = LightShadows.Soft;
                refs.Sun.shadowStrength = 0.72f;
                refs.Sun.shadowBias = 0.04f;
                refs.Sun.shadowNormalBias = 0.3f;
                refs.Sun.transform.rotation = Quaternion.Euler(48f, -38f, 0f);
            }
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = VisualAssetFactory.Hex("#C9DDF2");
            RenderSettings.ambientEquatorColor = VisualAssetFactory.Hex("#E9DCC6");
            RenderSettings.ambientGroundColor = VisualAssetFactory.Hex("#8E7B66");
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = sky;
            RenderSettings.fogStartDistance = 26f;
            RenderSettings.fogEndDistance = 60f;
            if (refs.Camera != null) refs.Camera.backgroundColor = sky;
        }
    }
}
