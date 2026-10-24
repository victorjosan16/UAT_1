using System;
using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// Un obiect care zboară în arc de la A la B (resurse vândute, monede plătite, lingouri).
    /// Pur vizual: la sosire apelează un callback și se întoarce în pool.
    /// </summary>
    public class FlyingItem : MonoBehaviour, IPoolable
    {
        [SerializeField] private Renderer itemRenderer;
        [SerializeField] private float spinSpeed = 540f;

        private Vector3 _from;
        private Vector3 _fixedTarget;
        private Transform _target;
        private Vector3 _targetOffset;
        private float _progress;
        private float _duration;
        private float _arcHeight;
        private Action _onArrive;
        private Vector3 _baseScale;
        private bool _flying;

        private void Awake() => _baseScale = transform.localScale;

        /// <summary>Zbor spre un obiect care se poate mișca (ex. spatele jucătorului).</summary>
        public void Launch(Vector3 from, Transform target, Vector3 targetOffset, Material material,
            float duration, float arcHeight, Action onArrive)
        {
            _target = target;
            _targetOffset = targetOffset;
            Begin(from, material, duration, arcHeight, onArrive);
        }

        /// <summary>Zbor spre un punct fix.</summary>
        public void Launch(Vector3 from, Vector3 to, Material material, float duration, float arcHeight, Action onArrive)
        {
            _target = null;
            _fixedTarget = to;
            Begin(from, material, duration, arcHeight, onArrive);
        }

        private void Begin(Vector3 from, Material material, float duration, float arcHeight, Action onArrive)
        {
            _from = from;
            _duration = Mathf.Max(0.05f, duration);
            _arcHeight = arcHeight;
            _onArrive = onArrive;
            _progress = 0f;
            _flying = true;
            transform.position = from;
            if (material != null && itemRenderer != null) itemRenderer.sharedMaterial = material;
        }

        private void Update()
        {
            if (!_flying) return;

            _progress += Time.deltaTime / _duration;
            float t = Mathf.Clamp01(_progress);
            Vector3 end = _target != null ? _target.position + _targetOffset : _fixedTarget;
            transform.position = Vector3.Lerp(_from, end, t) + Vector3.up * (4f * _arcHeight * t * (1f - t));
            transform.Rotate(spinSpeed * Time.deltaTime, spinSpeed * 0.7f * Time.deltaTime, 0f, Space.Self);
            transform.localScale = _baseScale * Mathf.Lerp(1f, 0.7f, t);

            if (t < 1f) return;

            _flying = false;
            Action callback = _onArrive;
            _onArrive = null;
            if (ObjectPool.Instance != null) ObjectPool.Instance.Despawn(gameObject);
            else gameObject.SetActive(false);
            callback?.Invoke();
        }

        public void OnSpawned() => transform.localScale = _baseScale;

        public void OnDespawned()
        {
            _flying = false;
            _onArrive = null;
            _target = null;
        }
    }
}
