using System;
using System.Collections.Generic;
using LastBlock.Core;
using UnityEngine;

namespace LastBlock.Gameplay
{
    /// <summary>
    /// Builds a tower of 3 blocks per layer, each layer rotated 90 degrees from the one below.
    /// The tower base sits at this transform's position (platform top surface).
    /// </summary>
    public class TowerGenerator : MonoBehaviour
    {
        const int BlocksPerLayer = 3;

        [Header("Prefab & Visuals")]
        [SerializeField] GameObject blockPrefab;
        [SerializeField] Material[] blockMaterials;

        [Header("Dimensions")]
        [SerializeField] float blockWidth = 0.5f;
        [SerializeField] float blockHeight = 0.3f;
        [Tooltip("Horizontal gap between blocks in the same layer.")]
        [SerializeField] float gap = 0.02f;

        [Header("Physics")]
        [SerializeField] float blockMass = 1f;
        [SerializeField] float bounciness = 0f;

        readonly List<BlockController> blocks = new List<BlockController>();
        PhysicsMaterial levelPhysicsMaterial;
        Material[] runtimeFallbackMaterials;

        public float BlockWidth => blockWidth;
        public float BlockHeight => blockHeight;
        /// <summary>Length equals the width of a full layer, so the tower footprint is square.</summary>
        public float BlockLength => blockWidth * BlocksPerLayer + gap * (BlocksPerLayer - 1);
        public Vector3 TowerCenter => transform.position;
        public IReadOnlyList<BlockController> Blocks => blocks;

        /// <summary>Raised when a block hits something hard enough (block, speed).</summary>
        public event Action<BlockController, float> BlockImpact;

        public float GetTowerHeight(int layers) => layers * blockHeight;

        internal void ReportImpact(BlockController block, float speed)
        {
            BlockImpact?.Invoke(block, speed);
        }

        /// <summary>Wakes every block so removed support is noticed even by sleeping bodies.</summary>
        public void WakeAll()
        {
            foreach (BlockController block in blocks)
            {
                if (block != null && block.State != BlockState.Extracted && block.State != BlockState.OutOfBounds)
                    block.Body.WakeUp();
            }
        }

        /// <summary>Destroys any existing tower and builds a fresh one for the given level.</summary>
        public List<BlockController> Generate(LevelConfig config)
        {
            Clear();
            int layers = Mathf.Max(1, config.layers);
            PhysicsMaterial physicsMaterial = Application.isPlaying ? CreateLevelPhysicsMaterial(config) : null;
            Material[] materials = GetMaterials();
            float length = BlockLength;

            for (int layer = 0; layer < layers; layer++)
            {
                bool alongX = layer % 2 == 0;
                Quaternion rotation = alongX ? Quaternion.identity : Quaternion.Euler(0f, 90f, 0f);
                float y = blockHeight * 0.5f + layer * blockHeight;

                for (int i = 0; i < BlocksPerLayer; i++)
                {
                    if (i == 1 && config.IsMiddlePreRemoved(layer)) continue;

                    float offset = (i - 1) * (blockWidth + gap);
                    Vector3 localPosition = alongX ? new Vector3(0f, y, offset) : new Vector3(offset, y, 0f);
                    Material material = materials.Length > 0 ? materials[(layer + i * 2) % materials.Length] : null;

                    BlockController block = SpawnBlock(transform.TransformPoint(localPosition), transform.rotation * rotation);
                    block.name = $"Block_L{layer:00}_{i}";
                    block.Initialize(this, layer, i, layer == layers - 1,
                        new Vector3(length, blockHeight, blockWidth), blockMass, physicsMaterial, material);
                    blocks.Add(block);
                }
            }

            // Start asleep so the tower is perfectly still until something touches it.
            if (Application.isPlaying)
            {
                foreach (BlockController block in blocks) block.Body.Sleep();
            }
            return blocks;
        }

        /// <summary>Removes every block under this transform (including edit-mode preview blocks).</summary>
        public void Clear()
        {
            blocks.Clear();
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = transform.GetChild(i).gameObject;
                // Deactivate first: Destroy is deferred and old colliders must not touch the new tower.
                child.SetActive(false);
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }
        }

        BlockController SpawnBlock(Vector3 position, Quaternion rotation)
        {
            GameObject instance;
            GameObject prefab = blockPrefab != null ? blockPrefab : Resources.Load<GameObject>("Block");
            if (prefab != null)
            {
                instance = Instantiate(prefab, position, rotation, transform);
            }
            else
            {
                // Fallback so the game (and tests) still work without the generated prefab.
                instance = GameObject.CreatePrimitive(PrimitiveType.Cube);
                instance.transform.SetParent(transform, false);
                instance.transform.SetPositionAndRotation(position, rotation);
            }

            if (instance.GetComponent<Rigidbody>() == null) instance.AddComponent<Rigidbody>();
            if (instance.GetComponent<BoxCollider>() == null) instance.AddComponent<BoxCollider>();
            BlockController controller = instance.GetComponent<BlockController>();
            if (controller == null) controller = instance.AddComponent<BlockController>();
            return controller;
        }

        PhysicsMaterial CreateLevelPhysicsMaterial(LevelConfig config)
        {
            if (levelPhysicsMaterial != null) Destroy(levelPhysicsMaterial);
            levelPhysicsMaterial = new PhysicsMaterial("BlockPhysics_Level" + config.levelNumber)
            {
                staticFriction = config.staticFriction > 0f ? config.staticFriction : 0.6f,
                dynamicFriction = config.dynamicFriction > 0f ? config.dynamicFriction : 0.45f,
                bounciness = bounciness,
                frictionCombine = PhysicsMaterialCombine.Average,
                bounceCombine = PhysicsMaterialCombine.Minimum
            };
            return levelPhysicsMaterial;
        }

        Material[] GetMaterials()
        {
            if (blockMaterials != null && blockMaterials.Length > 0) return blockMaterials;
            if (runtimeFallbackMaterials != null) return runtimeFallbackMaterials;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) return Array.Empty<Material>();

            Color[] palette =
            {
                new Color(0.95f, 0.45f, 0.40f), new Color(0.98f, 0.68f, 0.32f), new Color(0.98f, 0.85f, 0.40f),
                new Color(0.45f, 0.82f, 0.60f), new Color(0.42f, 0.70f, 0.95f), new Color(0.70f, 0.55f, 0.90f)
            };
            runtimeFallbackMaterials = new Material[palette.Length];
            for (int i = 0; i < palette.Length; i++)
            {
                runtimeFallbackMaterials[i] = new Material(shader) { color = palette[i] };
            }
            return runtimeFallbackMaterials;
        }

        void OnDestroy()
        {
            if (levelPhysicsMaterial != null) Destroy(levelPhysicsMaterial);
        }
    }
}
