using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// Atrage resursele de pe jos aflate în rază și le trimite în stivă, cât timp mai e loc.
    /// Folosit de jucător și de muncitori.
    /// </summary>
    public class ResourceCollector : MonoBehaviour
    {
        [SerializeField] private ResourceStack stack;
        [Tooltip("Layer-ul resurselor de pe jos (Resource).")]
        [SerializeField] private LayerMask resourceLayer;
        [SerializeField, Min(0.1f)] private float radius = 2.5f;
        [SerializeField] private float scanInterval = 0.1f;

        public float Radius
        {
            get => radius;
            set => radius = Mathf.Max(0.1f, value);
        }

        private readonly Collider[] _results = new Collider[32];
        private float _scanTimer;
        private float _lastFullWarning = -100f;

        private void Update()
        {
            _scanTimer -= Time.deltaTime;
            if (_scanTimer > 0f) return;
            _scanTimer = scanInterval;

            int count = Physics.OverlapSphereNonAlloc(
                transform.position, radius, _results, resourceLayer, QueryTriggerInteraction.Collide);

            for (int i = 0; i < count; i++)
            {
                if (!_results[i].TryGetComponent(out ResourcePickup pickup) || !pickup.CanBeCollected) continue;

                if (stack.IsFull)
                {
                    WarnFull();
                    return;
                }
                pickup.StartCollect(stack);
            }
        }

        private void WarnFull()
        {
            if (Time.time - _lastFullWarning < 3f) return;
            _lastFullWarning = Time.time;
            GameEvents.RaiseStackFull(stack);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
