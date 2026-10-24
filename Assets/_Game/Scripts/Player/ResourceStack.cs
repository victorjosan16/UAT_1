using System;
using System.Collections.Generic;
using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// O stivă de resurse cu capacitate limitată, vizibilă ca un teanc de cuburi (pe spatele
    /// jucătorului, al muncitorilor sau în camion). Zonele (Sell, Rafinărie, Comenzi) scot
    /// resurse din stivele aflate în interiorul lor.
    /// </summary>
    public class ResourceStack : MonoBehaviour
    {
        private struct Entry
        {
            public ResourceData Data;
            public GameObject Visual;
        }

        private static readonly List<ResourceStack> ActiveStacks = new();

        /// <summary>Toate stivele active (pentru zone), fără Find().</summary>
        public static IReadOnlyList<ResourceStack> Active => ActiveStacks;

        [Tooltip("Punctul de la baza teancului (ex. spatele jucătorului).")]
        [SerializeField] private Transform anchor;
        [SerializeField, Min(0)] private int capacity = 10;
        [Tooltip("Câte cuburi pe verticală într-o coloană.")]
        [SerializeField, Min(1)] private int itemsPerColumn = 10;
        [Tooltip("Câte coloane pe lățime, înainte de a începe un rând nou în spate.")]
        [SerializeField, Min(1)] private int columnsPerRow = 2;
        [SerializeField] private Vector3 itemSpacing = new(0.3f, 0.3f, 0.3f);
        [Tooltip("Banii din această stivă se animă spre UI când e vândută (doar pentru jucător).")]
        [SerializeField] private bool animateMoneyOnSell;

        private readonly List<Entry> _items = new();
        private int _reserved;

        public event Action<ResourceStack> Changed;

        public int Count => _items.Count;
        public int Capacity
        {
            get => capacity;
            set
            {
                capacity = Mathf.Max(0, value);
                Changed?.Invoke(this);
            }
        }
        public bool IsFull => _items.Count + _reserved >= capacity;
        public bool IsEmpty => _items.Count == 0;
        public Transform Anchor => anchor != null ? anchor : transform;
        public bool AnimateMoneyOnSell => animateMoneyOnSell;

        private void OnEnable() => ActiveStacks.Add(this);

        private void OnDisable()
        {
            ActiveStacks.Remove(this);
            _reserved = 0;
        }

        // ------------------------------------------------------------ Adăugare

        /// <summary>Rezervă un loc pentru o resursă care e încă în zbor spre stivă.</summary>
        public bool TryReserve()
        {
            if (IsFull) return false;
            _reserved++;
            return true;
        }

        public void CancelReservation()
        {
            if (_reserved > 0) _reserved--;
        }

        /// <summary>Adaugă o resursă pentru care s-a rezervat deja loc.</summary>
        public void AddReserved(ResourceData data)
        {
            CancelReservation();
            AddInternal(data);
        }

        public bool TryAdd(ResourceData data)
        {
            if (data == null || IsFull) return false;
            AddInternal(data);
            return true;
        }

        private void AddInternal(ResourceData data)
        {
            if (data == null) return;
            _items.Add(new Entry { Data = data, Visual = SpawnVisual(data, _items.Count) });
            Changed?.Invoke(this);
        }

        // ------------------------------------------------------------ Scoatere

        /// <summary>
        /// Scoate ultima resursă care respectă filtrul (null = oricare).
        /// Întoarce și poziția din lume a cubului, pentru animații.
        /// </summary>
        public bool TryTakeLast(Func<ResourceData, bool> filter, out ResourceData data, out Vector3 worldPosition)
        {
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                Entry entry = _items[i];
                if (filter != null && !filter(entry.Data)) continue;

                data = entry.Data;
                worldPosition = entry.Visual != null ? entry.Visual.transform.position : Anchor.position;
                DespawnVisual(entry.Visual);
                _items.RemoveAt(i);
                if (i < _items.Count) RelayoutFrom(i);
                Changed?.Invoke(this);
                return true;
            }
            data = null;
            worldPosition = default;
            return false;
        }

        public bool Contains(Func<ResourceData, bool> filter)
        {
            foreach (var entry in _items)
                if (filter == null || filter(entry.Data)) return true;
            return false;
        }

        /// <summary>Copiază resursele din stivă (de jos în sus) în lista dată (pentru salvare).</summary>
        public void GetContents(List<ResourceData> output)
        {
            output.Clear();
            foreach (var entry in _items) output.Add(entry.Data);
        }

        public int CountOf(ResourceData data)
        {
            int count = 0;
            foreach (var entry in _items)
                if (entry.Data == data) count++;
            return count;
        }

        // ------------------------------------------------------------ Vizual

        private Vector3 SlotLocalPosition(int index)
        {
            int column = index / itemsPerColumn;
            int row = index % itemsPerColumn;
            int columnInRow = column % columnsPerRow;
            int rowBack = column / columnsPerRow;
            float xOffset = (columnInRow - (columnsPerRow - 1) * 0.5f) * itemSpacing.x;
            return new Vector3(xOffset, row * itemSpacing.y, -rowBack * itemSpacing.z);
        }

        private GameObject SpawnVisual(ResourceData data, int index)
        {
            EffectsLibrary library = EffectsLibrary.Instance;
            if (library == null || library.StackItemPrefab == null || ObjectPool.Instance == null) return null;

            GameObject visual = ObjectPool.Instance.Spawn(library.StackItemPrefab, Anchor.position, Anchor.rotation);
            visual.transform.SetParent(Anchor, false);
            visual.transform.localPosition = SlotLocalPosition(index);
            visual.transform.localRotation = Quaternion.identity;
            if (data.PickupMaterial != null && visual.TryGetComponent(out Renderer itemRenderer))
                itemRenderer.sharedMaterial = data.PickupMaterial;
            return visual;
        }

        private static void DespawnVisual(GameObject visual)
        {
            if (visual == null) return;
            if (ObjectPool.Instance != null) ObjectPool.Instance.Despawn(visual);
            else Destroy(visual);
        }

        private void RelayoutFrom(int startIndex)
        {
            for (int i = startIndex; i < _items.Count; i++)
                if (_items[i].Visual != null) _items[i].Visual.transform.localPosition = SlotLocalPosition(i);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => ActiveStacks.Clear();
    }
}
