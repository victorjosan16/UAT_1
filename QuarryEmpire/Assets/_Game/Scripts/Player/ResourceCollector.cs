using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// Atrage resursele de pe jos care sunt în raza jucătorului.
    /// În Etapa 2 va verifica și dacă mai e loc în stivă.
    /// </summary>
    public class ResourceCollector : MonoBehaviour
    {
        [SerializeField] private PlayerStats stats;
        [Tooltip("Layer-ul resurselor de pe jos (Resource).")]
        [SerializeField] private LayerMask resourceLayer;
        [Tooltip("Punctul spre care zboară resursele (ex. spatele jucătorului).")]
        [SerializeField] private Transform collectPoint;
        [SerializeField] private float scanInterval = 0.1f;

        private readonly Collider[] _results = new Collider[32];
        private float _scanTimer;

        private void Update()
        {
            _scanTimer -= Time.deltaTime;
            if (_scanTimer > 0f) return;
            _scanTimer = scanInterval;

            int count = Physics.OverlapSphereNonAlloc(
                transform.position, stats.CollectRadius, _results, resourceLayer, QueryTriggerInteraction.Collide);

            Transform target = collectPoint != null ? collectPoint : transform;
            for (int i = 0; i < count; i++)
            {
                if (_results[i].TryGetComponent(out ResourcePickup pickup) && pickup.CanBeCollected)
                    pickup.StartCollect(target);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (stats == null) return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, stats.CollectRadius);
        }
    }
}
