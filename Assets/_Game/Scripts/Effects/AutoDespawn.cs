using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>Întoarce obiectul în pool după un timp (ex. particule).</summary>
    public class AutoDespawn : MonoBehaviour, IPoolable
    {
        [SerializeField, Min(0.05f)] private float lifetime = 1f;

        private float _timer;

        public float Lifetime
        {
            get => lifetime;
            set => lifetime = Mathf.Max(0.05f, value);
        }

        private void OnEnable() => _timer = lifetime;

        private void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            if (ObjectPool.Instance != null) ObjectPool.Instance.Despawn(gameObject);
            else gameObject.SetActive(false);
        }

        public void OnSpawned() => _timer = lifetime;
        public void OnDespawned() { }
    }
}
