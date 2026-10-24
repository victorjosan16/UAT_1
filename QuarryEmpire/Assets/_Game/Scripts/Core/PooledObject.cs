using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// Marcaj adăugat automat de ObjectPool pe fiecare instanță creată.
    /// Ține minte din ce prefab provine obiectul, ca să știm în ce pool îl punem înapoi.
    /// </summary>
    [DisallowMultipleComponent]
    public class PooledObject : MonoBehaviour
    {
        public GameObject OriginPrefab { get; private set; }
        public bool IsInPool { get; internal set; }

        private IPoolable[] _poolables;

        internal void Initialize(GameObject originPrefab)
        {
            OriginPrefab = originPrefab;
            // Căutăm o singură dată toate componentele care vor să fie anunțate.
            _poolables = GetComponentsInChildren<IPoolable>(true);
        }

        internal void NotifySpawned()
        {
            for (int i = 0; i < _poolables.Length; i++) _poolables[i].OnSpawned();
        }

        internal void NotifyDespawned()
        {
            for (int i = 0; i < _poolables.Length; i++) _poolables[i].OnDespawned();
        }
    }
}
