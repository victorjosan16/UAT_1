using System;
using System.Collections.Generic;
using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// TEMPORAR (Etapa 1): așază o grilă de blocuri cu tipuri alese aleator după pondere,
    /// ca să nu plasăm zeci de cuburi de mână. În Etapa 6 îl înlocuim cu LayerGenerator.
    /// </summary>
    public class DebugBlockGrid : MonoBehaviour
    {
        [Serializable]
        private class WeightedBlock
        {
            public BlockData data;
            [Min(0f)] public float weight = 1f;
        }

        [SerializeField] private MineBlock blockPrefab;
        [SerializeField] private List<WeightedBlock> blocks = new();
        [SerializeField, Min(1)] private int width = 8;
        [SerializeField, Min(1)] private int depth = 8;
        [Tooltip("Distanța dintre centrele blocurilor (1 = lipite).")]
        [SerializeField, Min(0.1f)] private float spacing = 1f;
        [Tooltip("0 = altă grilă la fiecare Play; alt număr = mereu aceeași grilă.")]
        [SerializeField] private int seed;

        private void Start() => Build();

        private void Build()
        {
            if (blockPrefab == null || blocks.Count == 0)
            {
                Debug.LogWarning("DebugBlockGrid: setează Block Prefab și lista Blocks.", this);
                return;
            }

            var random = seed == 0 ? new System.Random() : new System.Random(seed);
            float totalWeight = 0f;
            foreach (var b in blocks) totalWeight += b.weight;

            // Grila e centrată pe acest GameObject; blocurile stau cu baza pe sol (y + 0.5).
            Vector3 origin = transform.position - new Vector3((width - 1) * spacing * 0.5f, 0f, (depth - 1) * spacing * 0.5f);
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    Vector3 position = origin + new Vector3(x * spacing, 0.5f, z * spacing);
                    MineBlock block = Instantiate(blockPrefab, position, Quaternion.identity, transform);
                    block.Init(PickRandom(random, totalWeight));
                }
            }
        }

        private BlockData PickRandom(System.Random random, float totalWeight)
        {
            float roll = (float)random.NextDouble() * totalWeight;
            foreach (var b in blocks)
            {
                roll -= b.weight;
                if (roll <= 0f) return b.data;
            }
            return blocks[^1].data;
        }
    }
}
