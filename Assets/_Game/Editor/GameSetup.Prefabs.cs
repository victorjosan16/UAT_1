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
            prefabs.Block = CreateBlockPrefab(m["Stone"], prefabs.Pickup);
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
            go.transform.localScale = scale;
            var meshRenderer = go.GetComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            var flying = go.AddComponent<FlyingItem>();
            Set(flying, "itemRenderer", meshRenderer);
            return SavePrefab(go, name).GetComponent<FlyingItem>();
        }

        private static MineBlock CreateBlockPrefab(Material material, ResourcePickup pickupPrefab)
        {
            var root = new GameObject("Block") { layer = BlockLayer };
            root.AddComponent<BoxCollider>();

            GameObject visual = Primitive(PrimitiveType.Cube, "Visual", root.transform, Vector3.zero, Vector3.one * 0.98f, material);
            var visualRenderer = visual.GetComponent<MeshRenderer>();

            var block = root.AddComponent<MineBlock>();
            Set(block, "blockRenderer", visualRenderer);

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
