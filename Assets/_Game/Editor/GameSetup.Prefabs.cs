using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace QuarryEmpire.EditorTools
{
    /// <summary>Prefab-urile reciclate prin ObjectPool: bloc, resursă căzută, cub de stivă, obiecte zburătoare.</summary>
    public static partial class GameSetup
    {
        private class Prefabs
        {
            public ResourcePickup Pickup;
            public MineBlock Block;
            public GameObject StackItem;
            public FlyingItem FlyingItem;
            public FlyingItem Coin;
        }

        private static Prefabs CreatePrefabs(Materials m)
        {
            var prefabs = new Prefabs
            {
                Pickup = CreatePickupPrefab(m["Stone"]),
                StackItem = CreateStackItemPrefab(m["Stone"]),
                FlyingItem = CreateFlyingPrefab("FlyingItem", PrimitiveType.Cube, new Vector3(0.28f, 0.28f, 0.28f), m["Stone"]),
                Coin = CreateFlyingPrefab("FlyingCoin", PrimitiveType.Cylinder, new Vector3(0.35f, 0.04f, 0.35f), m["Coin"]),
            };
            prefabs.Block = CreateBlockPrefab(m["Stone"], prefabs.Pickup, m["Dark"], m["PadSell"], m["Lock"], m["Metal"]);
            return prefabs;
        }

        private static GameObject SavePrefab(GameObject instance, string name)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, $"{PrefabsDir}/{name}.prefab");
            Object.DestroyImmediate(instance);
            return prefab;
        }

        private static ResourcePickup CreatePickupPrefab(Material material)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Pickup_Resource";
            go.layer = ResourceLayer;
            go.transform.localScale = Vector3.one * 0.3f;
            go.GetComponent<BoxCollider>().isTrigger = true;
            go.GetComponent<MeshFilter>().sharedMesh = RoundedMesh(Vector3.one, 0.22f);
            var meshRenderer = go.GetComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;

            var pickup = go.AddComponent<ResourcePickup>();
            Set(pickup, "pickupRenderer", meshRenderer);
            return SavePrefab(go, "Pickup_Resource").GetComponent<ResourcePickup>();
        }

        private static GameObject CreateStackItemPrefab(Material material)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "StackItem";
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.GetComponent<MeshFilter>().sharedMesh = RoundedMesh(Vector3.one, 0.22f);
            go.transform.localScale = Vector3.one * 0.26f;
            var meshRenderer = go.GetComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            return SavePrefab(go, "StackItem");
        }

        private static FlyingItem CreateFlyingPrefab(string name, PrimitiveType type, Vector3 scale, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            if (type == PrimitiveType.Cube) go.GetComponent<MeshFilter>().sharedMesh = RoundedMesh(Vector3.one, 0.22f);
            go.transform.localScale = scale;
            var meshRenderer = go.GetComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            var flying = go.AddComponent<FlyingItem>();
            Set(flying, "itemRenderer", meshRenderer);
            return SavePrefab(go, name).GetComponent<FlyingItem>();
        }

        private static MineBlock CreateBlockPrefab(Material material, ResourcePickup pickupPrefab, Material healthBack,
            Material healthFill, Material lockMaterial, Material lockShackle)
        {
            var root = new GameObject("Block") { layer = BlockLayer };
            root.AddComponent<BoxCollider>();

            // Cub cu muchii rotunjite: arată „premium”, iar mesh-ul e comun pentru toate blocurile (un singur asset).
            GameObject visual = Box("Visual", root.transform, Vector3.zero, Vector3.one * 0.97f, material, 0.14f);
            var visualRenderer = visual.GetComponent<MeshRenderer>();

            // Pepite de minereu pe fețe (culoarea se setează la rulare, după resursă).
            GameObject ore = Empty("Ore", visual.transform, Vector3.zero);
            var nuggetSpots = new (Vector3 position, float size)[]
            {
                (new Vector3(0.28f, 0.47f, 0.12f), 0.24f), (new Vector3(-0.2f, 0.47f, -0.24f), 0.2f),
                (new Vector3(0.47f, 0.08f, -0.2f), 0.22f), (new Vector3(-0.47f, -0.16f, 0.22f), 0.2f),
                (new Vector3(0.12f, -0.06f, 0.47f), 0.24f), (new Vector3(-0.24f, 0.2f, -0.47f), 0.2f),
            };
            var nuggets = new Renderer[nuggetSpots.Length];
            for (int i = 0; i < nuggetSpots.Length; i++)
            {
                GameObject nugget = Box("Nugget", ore.transform, nuggetSpots[i].position, Vector3.one * nuggetSpots[i].size,
                    material, 0.07f, new Vector3(i * 23f, i * 41f, i * 17f), castShadows: false);
                nuggets[i] = nugget.GetComponent<MeshRenderer>();
            }

            // Bară de viață pe fața de sus (apare după prima lovitură).
            GameObject healthBar = Empty("HealthBar", root.transform, new Vector3(0f, 0.53f, -0.3f));
            Box("Back", healthBar.transform, Vector3.zero, new Vector3(0.84f, 0.05f, 0.16f), healthBack, 0.02f, castShadows: false);
            GameObject fillPivot = Empty("FillPivot", healthBar.transform, new Vector3(-0.4f, 0.015f, 0f));
            Box("Fill", fillPivot.transform, new Vector3(0.4f, 0f, 0f), new Vector3(0.8f, 0.05f, 0.11f), healthFill, 0.02f, castShadows: false);
            healthBar.SetActive(false);

            // Lacăt deasupra blocurilor prea dure, întors spre cameră.
            GameObject lockIcon = Empty("Lock", root.transform, new Vector3(0f, 0.78f, 0f), 225f);
            Box("Body", lockIcon.transform, Vector3.zero, new Vector3(0.36f, 0.28f, 0.12f), lockMaterial, 0.05f, castShadows: false);
            Box("ShackleL", lockIcon.transform, new Vector3(-0.11f, 0.2f, 0f), new Vector3(0.06f, 0.18f, 0.06f), lockShackle, 0.025f, castShadows: false);
            Box("ShackleR", lockIcon.transform, new Vector3(0.11f, 0.2f, 0f), new Vector3(0.06f, 0.18f, 0.06f), lockShackle, 0.025f, castShadows: false);
            Box("ShackleTop", lockIcon.transform, new Vector3(0f, 0.3f, 0f), new Vector3(0.28f, 0.06f, 0.06f), lockShackle, 0.025f, castShadows: false);
            lockIcon.SetActive(false);

            var block = root.AddComponent<MineBlock>();
            Set(block, "blockRenderer", visualRenderer);

            var status = root.AddComponent<BlockStatusVisual>();
            Set(status, "block", block);
            Set(status, "healthBar", healthBar);
            Set(status, "healthFill", fillPivot.transform);
            Set(status, "lockIcon", lockIcon);

            var oreVisual = root.AddComponent<BlockOreVisual>();
            Set(oreVisual, "block", block);
            Set(oreVisual, "visual", visual.transform);
            Set(oreVisual, "oreGroup", ore);
            SetArray(oreVisual, "nuggets", nuggets);

            var pop = root.AddComponent<BlockSpawnPop>();
            Set(pop, "block", block);
            Set(pop, "visual", visual.transform);

            var feedback = root.AddComponent<BlockHitFeedback>();
            Set(feedback, "block", block);
            Set(feedback, "visual", visual.transform);
            Set(feedback, "targetRenderer", visualRenderer);

            var dropSpawner = root.AddComponent<BlockDropSpawner>();
            Set(dropSpawner, "pickupPrefab", pickupPrefab);

            return SavePrefab(root, "Block").GetComponent<MineBlock>();
        }
    }
}
