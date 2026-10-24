using System;
using System.Collections.Generic;
using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// Pool generic de obiecte: în loc să creăm (Instantiate) și să distrugem (Destroy)
    /// obiecte des, le reciclăm. Pe mobil asta evită sacadările cauzate de Garbage Collector.
    /// </summary>
    [DefaultExecutionOrder(-100)] // rulează înaintea celorlalte scripturi
    public class ObjectPool : MonoBehaviour
    {
        [Serializable]
        private class PrewarmEntry
        {
            public GameObject prefab;
            [Min(0)] public int count = 20;
        }

        public static ObjectPool Instance { get; private set; }

        [Tooltip("Prefab-uri create din start (ca să nu apară sacadări la prima utilizare).")]
        [SerializeField] private List<PrewarmEntry> prewarm = new();

        private readonly Dictionary<GameObject, Stack<PooledObject>> _pools = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            foreach (var entry in prewarm)
            {
                if (entry.prefab == null) continue;
                var stack = GetStack(entry.prefab);
                for (int i = 0; i < entry.count; i++)
                {
                    var pooled = CreateInstance(entry.prefab);
                    pooled.gameObject.SetActive(false);
                    pooled.IsInPool = true;
                    stack.Push(pooled);
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Scoate un obiect din pool (sau creează unul nou dacă pool-ul e gol).</summary>
        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            var stack = GetStack(prefab);

            PooledObject pooled = null;
            // Sărim peste obiectele distruse între timp (ex. la schimbarea scenei).
            while (pooled == null && stack.Count > 0) pooled = stack.Pop();
            if (pooled == null) pooled = CreateInstance(prefab);

            pooled.transform.SetPositionAndRotation(position, rotation);
            pooled.IsInPool = false;
            pooled.gameObject.SetActive(true);
            pooled.NotifySpawned();
            return pooled.gameObject;
        }

        /// <summary>Variantă comodă: primești direct componenta de care ai nevoie.</summary>
        public T Spawn<T>(T prefab, Vector3 position, Quaternion rotation) where T : Component
        {
            return Spawn(prefab.gameObject, position, rotation).GetComponent<T>();
        }

        /// <summary>Pune obiectul înapoi în pool (îl dezactivează, nu îl distruge).</summary>
        public void Despawn(GameObject instance)
        {
            if (instance == null) return;

            if (!instance.TryGetComponent(out PooledObject pooled))
            {
                // Obiectul nu a venit din pool, deci îl distrugem normal.
                Destroy(instance);
                return;
            }
            if (pooled.IsInPool) return; // deja returnat

            pooled.NotifyDespawned();
            pooled.IsInPool = true;
            instance.SetActive(false);
            // Revine sub pool, ca să nu fie distrus odată cu fostul părinte.
            instance.transform.SetParent(transform, false);
            GetStack(pooled.OriginPrefab).Push(pooled);
        }

        private Stack<PooledObject> GetStack(GameObject prefab)
        {
            if (!_pools.TryGetValue(prefab, out var stack))
            {
                stack = new Stack<PooledObject>();
                _pools.Add(prefab, stack);
            }
            return stack;
        }

        private PooledObject CreateInstance(GameObject prefab)
        {
            var go = Instantiate(prefab, transform);
            if (!go.TryGetComponent(out PooledObject pooled)) pooled = go.AddComponent<PooledObject>();
            pooled.Initialize(prefab);
            return pooled;
        }
    }
}
