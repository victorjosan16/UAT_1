using System.Collections.Generic;
using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// Rafinăria: jucătorul lasă minereul brut în zona de intrare, mașina îl transformă
    /// (durează timp) în lingouri mai valoroase, care se adună într-o grămadă la ieșire.
    /// Jucătorul le ia stând în zona de ieșire.
    /// </summary>
    public class RefineZone : MonoBehaviour
    {
        [SerializeField] private ZoneArea inputArea;
        [SerializeField] private ZoneArea outputArea;
        [Tooltip("Unde intră minereul (gura mașinii).")]
        [SerializeField] private Transform machinePoint;
        [Tooltip("Baza grămezii de lingouri.")]
        [SerializeField] private Transform outputPile;
        [SerializeField] private WorldLabel label;
        [SerializeField, Min(1)] private int maxQueue = 40;
        [SerializeField, Min(1)] private int maxOutput = 60;
        [SerializeField] private float transferInterval = 0.07f;
        [SerializeField] private int pileColumns = 4;
        [SerializeField] private int pileRows = 3;
        [SerializeField] private float pileSpacing = 0.32f;
        [Tooltip("Cheie în salvare pentru minereul în lucru și lingourile gata.")]
        [SerializeField] private string saveKey = "refinery";

        private readonly Queue<ResourceData> _queue = new();
        private readonly List<(ResourceData data, GameObject visual)> _output = new();
        private ResourceData _current;
        private float _progress;
        private int _incoming;
        private float _inputTimer;
        private float _outputTimer;
        private float _labelTimer;
        private float _lastFullWarning = -100f;

        private int Pending => _queue.Count + _incoming + (_current != null ? 1 : 0);
        private readonly List<string> _saveBuffer = new();
        private bool _dirty;

        private void Start()
        {
            if (SaveSystem.Instance == null) return;
            List<string> queued = SaveData.GetList(SaveSystem.Instance.Data.stacks, saveKey + "_queue");
            if (queued != null)
                foreach (string id in queued)
                {
                    ResourceData resource = ResourceCatalog.Find(id);
                    if (resource != null && resource.CanBeRefined) _queue.Enqueue(resource);
                }
            List<string> ready = SaveData.GetList(SaveSystem.Instance.Data.stacks, saveKey + "_output");
            if (ready != null)
                foreach (string id in new List<string>(ready))
                {
                    ResourceData resource = ResourceCatalog.Find(id);
                    if (resource != null) AddOutput(resource);
                }
        }

        /// <summary>Scrie în salvare minereul în lucru (inclusiv bucata curentă) și lingourile gata.</summary>
        private void Persist()
        {
            if (!_dirty || SaveSystem.Instance == null) return;
            _dirty = false;
            _saveBuffer.Clear();
            if (_current != null) _saveBuffer.Add(_current.Id);
            foreach (ResourceData resource in _queue) _saveBuffer.Add(resource.Id);
            SaveData.SetList(SaveSystem.Instance.Data.stacks, saveKey + "_queue", _saveBuffer);
            _saveBuffer.Clear();
            foreach (var item in _output) _saveBuffer.Add(item.data.Id);
            SaveData.SetList(SaveSystem.Instance.Data.stacks, saveKey + "_output", _saveBuffer);
            SaveSystem.Instance.MarkDirty();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            PlayerRoot player = PlayerRoot.Instance;
            ResourceStack stack = player != null ? player.Stack : null;

            if (stack != null) HandleInput(player, stack, dt);
            HandleProcessing(dt);
            if (stack != null) HandleOutput(player, stack, dt);

            _labelTimer -= dt;
            if (_labelTimer <= 0f)
            {
                _labelTimer = 0.2f;
                RefreshLabel();
                Persist();
            }
        }

        private void HandleInput(PlayerRoot player, ResourceStack stack, float dt)
        {
            if (!inputArea.Contains(player.Position) || Pending >= maxQueue) return;
            _inputTimer -= dt;
            if (_inputTimer > 0f) return;
            _inputTimer = transferInterval;

            if (!stack.TryTakeLast(d => d.CanBeRefined, out ResourceData data, out Vector3 from)) return;
            _incoming++;
            EffectsLibrary.FlyItemTo(data.PickupMaterial, from, machinePoint.position, () =>
            {
                _incoming--;
                _queue.Enqueue(data);
                _dirty = true;
            });
        }

        private void HandleProcessing(float dt)
        {
            if (_current == null)
            {
                if (_queue.Count == 0 || _output.Count >= maxOutput) return;
                _current = _queue.Dequeue();
                _progress = 0f;
            }

            _progress += dt / _current.RefineSeconds;
            if (_progress < 1f) return;

            ResourceData result = _current.RefinedInto;
            _current = null;
            _progress = 0f;
            AddOutput(result);
            _dirty = true;
            GameEvents.RaiseItemRefined(result);
        }

        private void HandleOutput(PlayerRoot player, ResourceStack stack, float dt)
        {
            if (_output.Count == 0 || !outputArea.Contains(player.Position)) return;
            _outputTimer -= dt;
            if (_outputTimer > 0f) return;
            _outputTimer = transferInterval;

            if (!stack.TryReserve())
            {
                if (Time.time - _lastFullWarning > 3f)
                {
                    _lastFullWarning = Time.time;
                    GameEvents.RaiseStackFull(stack);
                }
                return;
            }

            int last = _output.Count - 1;
            (ResourceData data, GameObject visual) = _output[last];
            _output.RemoveAt(last);
            _dirty = true;
            Vector3 from = visual != null ? visual.transform.position : outputPile.position;
            if (visual != null) ObjectPool.Instance.Despawn(visual);

            EffectsLibrary.FlyItem(data.PickupMaterial, from, stack.Anchor, Vector3.zero, () => stack.AddReserved(data));
        }

        private void AddOutput(ResourceData data)
        {
            if (data == null) return;
            GameObject visual = null;
            EffectsLibrary library = EffectsLibrary.Instance;
            if (library != null && library.StackItemPrefab != null && ObjectPool.Instance != null)
            {
                int index = _output.Count;
                int perLayer = pileColumns * pileRows;
                int layer = index / perLayer;
                int inLayer = index % perLayer;
                Vector3 local = new(
                    (inLayer % pileColumns - (pileColumns - 1) * 0.5f) * pileSpacing,
                    layer * pileSpacing,
                    (inLayer / pileColumns - (pileRows - 1) * 0.5f) * pileSpacing);
                visual = ObjectPool.Instance.Spawn(library.StackItemPrefab, outputPile.TransformPoint(local), outputPile.rotation);
                visual.transform.SetParent(outputPile, true);
                if (data.PickupMaterial != null && visual.TryGetComponent(out Renderer itemRenderer))
                    itemRenderer.sharedMaterial = data.PickupMaterial;
            }
            _output.Add((data, visual));
        }

        private void RefreshLabel()
        {
            if (label == null) return;
            label.SetText(Loc.F("RAFINĂRIE\nÎn lucru: {0} · Gata: {1}", Pending, _output.Count));
            label.SetFill(_current != null ? _progress : 0f);
        }
    }
}
