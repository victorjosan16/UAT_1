using System;
using System.Collections.Generic;
using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// Un strat al carierei: ce blocuri apar (cu ponderi), cât de mare e grila și cât de rezistente sunt.
    /// </summary>
    [CreateAssetMenu(fileName = "Layer_", menuName = "Quarry Empire/Layer Definition")]
    public class LayerDefinition : ScriptableObject
    {
        [Serializable]
        public class WeightedBlock
        {
            public BlockData block;
            [Min(0f)] public float weight = 1f;
        }

        [field: SerializeField] public string DisplayName { get; private set; } = "Suprafață";
        [field: SerializeField, Min(0)] public int DepthMeters { get; private set; } = 10;
        [field: SerializeField, Range(2, 16)] public int Width { get; private set; } = 8;
        [field: SerializeField, Range(2, 16)] public int Depth { get; private set; } = 8;
        [field: Tooltip("Înmulțește viața tuturor blocurilor din strat.")]
        [field: SerializeField, Min(0.1f)] public float HealthMultiplier { get; private set; } = 1f;
        [field: SerializeField] public Color FloorColor { get; private set; } = new(0.55f, 0.45f, 0.35f);
        [SerializeField] private List<WeightedBlock> blocks = new();

        public IReadOnlyList<WeightedBlock> Blocks => blocks;

        /// <summary>Alege aleator un tip de bloc, după ponderi.</summary>
        public BlockData PickBlock(System.Random random)
        {
            float total = 0f;
            foreach (var entry in blocks) total += entry.weight;
            float roll = (float)random.NextDouble() * total;
            foreach (var entry in blocks)
            {
                roll -= entry.weight;
                if (roll <= 0f) return entry.block;
            }
            return blocks.Count > 0 ? blocks[^1].block : null;
        }
    }
}
