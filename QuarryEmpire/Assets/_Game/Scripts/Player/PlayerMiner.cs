using System;
using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// Găsește automat cel mai apropiat bloc minabil și îl lovește la intervale regulate.
    /// Nu se ocupă de animație: anunță doar evenimentul Swung.
    /// </summary>
    public class PlayerMiner : MonoBehaviour
    {
        [SerializeField] private PlayerStats stats;
        [SerializeField] private PlayerMovement movement;
        [Tooltip("Layer-ul pe care stau blocurile (Block).")]
        [SerializeField] private LayerMask blockLayer;
        [Tooltip("Cât de des (secunde) căutăm ținte noi. Nu e nevoie în fiecare frame.")]
        [SerializeField] private float scanInterval = 0.1f;
        [Tooltip("Înălțimea (m) de la care se caută blocurile, față de picioarele jucătorului.")]
        [SerializeField] private float scanHeight = 0.5f;

        /// <summary>Anunțat la fiecare lovitură (pentru animație, sunet, efecte).</summary>
        public event Action<MineBlock> Swung;

        public MineBlock CurrentTarget => _target;

        // Buffer refolosit: OverlapSphereNonAlloc nu alocă memorie nouă la fiecare apel.
        private readonly Collider[] _results = new Collider[16];
        private MineBlock _target;
        private float _hitCooldown;
        private float _scanTimer;

        private void Update()
        {
            float dt = Time.deltaTime;
            if (_hitCooldown > 0f) _hitCooldown -= dt;

            _scanTimer -= dt;
            if (_scanTimer <= 0f)
            {
                _scanTimer = scanInterval;
                _target = FindBestTarget();
            }

            if (_target != null && !_target.IsAlive) _target = null;

            if (_target == null)
            {
                movement.ClearAimTarget();
                return;
            }

            movement.SetAimTarget(_target.transform.position);

            if (_hitCooldown <= 0f)
            {
                _hitCooldown = stats.HitInterval;
                Swung?.Invoke(_target);
                _target.TakeDamage(stats.HitDamage);
            }
        }

        private MineBlock FindBestTarget()
        {
            Vector3 origin = transform.position + Vector3.up * scanHeight;
            int count = Physics.OverlapSphereNonAlloc(
                origin, stats.HitRange, _results, blockLayer, QueryTriggerInteraction.Ignore);

            MineBlock best = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                if (!_results[i].TryGetComponent(out MineBlock block)) continue;
                if (!block.IsAlive || block.Data == null) continue;
                if (block.Data.Hardness > stats.MiningTier) continue; // prea dur pentru târnăcop

                // Păstrăm ținta curentă cât timp e în rază, ca să nu sărim între blocuri.
                if (block == _target) return block;

                Vector3 offset = block.transform.position - transform.position;
                offset.y = 0f;
                float distance = offset.sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = block;
                }
            }
            return best;
        }

        // Desenează raza de minat în Scene view când jucătorul e selectat.
        private void OnDrawGizmosSelected()
        {
            if (stats == null) return;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * scanHeight, stats.HitRange);
        }
    }
}
