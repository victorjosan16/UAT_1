using System;
using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// Ține prefab-urile pentru efecte comune și oferă scurtături statice
    /// (FlyItem, FlyCoin), ca sistemele să nu aibă nevoie fiecare de referințe la prefab-uri.
    /// </summary>
    [DefaultExecutionOrder(-250)]
    public class EffectsLibrary : MonoBehaviour
    {
        public static EffectsLibrary Instance { get; private set; }

        [SerializeField] private FlyingItem flyingItemPrefab;
        [SerializeField] private FlyingItem coinPrefab;
        [Tooltip("Cubul mic folosit în stive și în grămezile de la rafinărie.")]
        [SerializeField] private GameObject stackItemPrefab;

        public GameObject StackItemPrefab => stackItemPrefab;

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>O resursă zboară spre un obiect (care se poate mișca).</summary>
        public static void FlyItem(Material material, Vector3 from, Transform target, Vector3 targetOffset,
            Action onArrive, float duration = 0.35f, float arcHeight = 1f)
        {
            if (!TryGetPrefab(Instance?.flyingItemPrefab, out var prefab))
            {
                onArrive?.Invoke();
                return;
            }
            ObjectPool.Instance.Spawn(prefab, from, Quaternion.identity)
                .Launch(from, target, targetOffset, material, duration, arcHeight, onArrive);
        }

        /// <summary>O resursă zboară spre un punct fix.</summary>
        public static void FlyItemTo(Material material, Vector3 from, Vector3 to, Action onArrive,
            float duration = 0.35f, float arcHeight = 1f)
        {
            if (!TryGetPrefab(Instance?.flyingItemPrefab, out var prefab))
            {
                onArrive?.Invoke();
                return;
            }
            ObjectPool.Instance.Spawn(prefab, from, Quaternion.identity)
                .Launch(from, to, material, duration, arcHeight, onArrive);
        }

        /// <summary>O monedă zboară de la A la B (plăți pe zone).</summary>
        public static void FlyCoin(Vector3 from, Vector3 to, float duration = 0.3f)
        {
            if (!TryGetPrefab(Instance?.coinPrefab, out var prefab)) return;
            ObjectPool.Instance.Spawn(prefab, from, Quaternion.identity)
                .Launch(from, to, null, duration, 0.8f, null);
        }

        private static bool TryGetPrefab(FlyingItem candidate, out FlyingItem prefab)
        {
            prefab = candidate;
            return prefab != null && ObjectPool.Instance != null;
        }
    }
}
