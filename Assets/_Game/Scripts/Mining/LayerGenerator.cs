using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// Generează procedural grila de blocuri a stratului curent. Când toate blocurile sunt sparte,
    /// cariera „coboară”: după o pauză apare stratul următor, cu minereuri mai rare și blocuri mai dure.
    /// După ultimul strat definit, ultimul se repetă cu viață din ce în ce mai mare.
    /// </summary>
    public class LayerGenerator : MonoBehaviour
    {
        public static LayerGenerator Instance { get; private set; }

        [SerializeField] private MineBlock blockPrefab;
        [SerializeField] private List<LayerDefinition> layers = new();
        [Tooltip("Distanța dintre centrele blocurilor (1 = lipite).")]
        [SerializeField, Min(0.5f)] private float spacing = 1f;
        [Tooltip("Podeaua carierei; își schimbă culoarea la fiecare strat.")]
        [SerializeField] private Renderer quarryFloor;
        [SerializeField] private float nextLayerDelay = 2.5f;
        [Tooltip("Cât crește viața la fiecare strat după ultimul definit.")]
        [SerializeField] private float extraLayerHealthGrowth = 1.35f;

        public int CurrentLayerIndex { get; private set; }
        public LayerDefinition CurrentLayer { get; private set; }
        public int RemainingBlocks { get; private set; }
        public int TotalBlocks { get; private set; }
        public int DepthMeters { get; private set; }

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private MaterialPropertyBlock _floorBlock;
        private bool _advancing;

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnEnable() => GameEvents.BlockDestroyed += OnBlockDestroyed;
        private void OnDisable() => GameEvents.BlockDestroyed -= OnBlockDestroyed;

        private void Start()
        {
            int saved = SaveSystem.Instance != null ? SaveSystem.Instance.Data.layerIndex : 0;
            SpawnLayer(Mathf.Max(0, saved));
        }

        private void OnBlockDestroyed(MineBlock block)
        {
            if (RemainingBlocks <= 0) return;
            RemainingBlocks--;
            if (RemainingBlocks == 0 && !_advancing) StartCoroutine(AdvanceRoutine());
        }

        private IEnumerator AdvanceRoutine()
        {
            _advancing = true;
            GameEvents.RaiseLayerCleared(CurrentLayerIndex);
            int nextDepth = GetDepthMeters(CurrentLayerIndex + 1);
            GameEvents.RaiseToast($"Strat curățat! Coborâm la {nextDepth} m");
            yield return new WaitForSeconds(nextLayerDelay);

            int next = CurrentLayerIndex + 1;
            if (SaveSystem.Instance != null)
            {
                SaveSystem.Instance.Data.layerIndex = next;
                SaveSystem.Instance.MarkDirty();
            }
            SpawnLayer(next);
            _advancing = false;
        }

        private void SpawnLayer(int index)
        {
            if (layers.Count == 0 || blockPrefab == null || ObjectPool.Instance == null)
            {
                Debug.LogWarning("LayerGenerator: lipsesc straturile, prefab-ul de bloc sau ObjectPool.", this);
                return;
            }

            CurrentLayerIndex = index;
            int definitionIndex = Mathf.Min(index, layers.Count - 1);
            CurrentLayer = layers[definitionIndex];
            DepthMeters = GetDepthMeters(index);

            float healthMultiplier = CurrentLayer.HealthMultiplier;
            int extraLayers = index - (layers.Count - 1);
            if (extraLayers > 0) healthMultiplier *= Mathf.Pow(extraLayerHealthGrowth, extraLayers);

            var random = new System.Random(index * 7919 + System.Environment.TickCount);
            int width = CurrentLayer.Width;
            int depth = CurrentLayer.Depth;
            Vector3 origin = transform.position - new Vector3((width - 1) * spacing * 0.5f, 0f, (depth - 1) * spacing * 0.5f);

            TotalBlocks = 0;
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    BlockData data = CurrentLayer.PickBlock(random);
                    if (data == null) continue;
                    Vector3 position = origin + new Vector3(x * spacing, 0.5f, z * spacing);
                    MineBlock block = ObjectPool.Instance.Spawn(blockPrefab, position, Quaternion.identity);
                    block.transform.SetParent(transform, true);
                    block.Init(data, healthMultiplier);
                    TotalBlocks++;
                }
            }
            RemainingBlocks = TotalBlocks;

            TintFloor(CurrentLayer.FloorColor);
            GameEvents.RaiseLayerStarted(index, CurrentLayer);
        }

        /// <summary>Adâncimea în metri: cea definită, apoi +10 m la fiecare strat în plus.</summary>
        private int GetDepthMeters(int index)
        {
            if (layers.Count == 0) return 0;
            if (index < layers.Count) return layers[index].DepthMeters;
            return layers[^1].DepthMeters + (index - layers.Count + 1) * 10;
        }

        private void TintFloor(Color color)
        {
            if (quarryFloor == null) return;
            _floorBlock ??= new MaterialPropertyBlock();
            _floorBlock.SetColor(BaseColorId, color);
            quarryFloor.SetPropertyBlock(_floorBlock);
        }
    }
}
