using System;
using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// Bază pentru uneltele utilajelor. Vehicle apelează Tick() cât e condus;
    /// unealta lovește la intervalul din VehicleDefinition.
    /// </summary>
    public abstract class VehicleTool : MonoBehaviour
    {
        [SerializeField] protected VehicleDefinition definition;
        [Tooltip("Layer-ul blocurilor (Block).")]
        [SerializeField] protected LayerMask blockLayer;

        protected readonly Collider[] Results = new Collider[32];

        /// <summary>Anunțat la fiecare lovitură reușită (pentru animații).</summary>
        public event Action Struck;

        public bool IsActive { get; private set; }

        private float _timer;

        public void SetActive(bool active)
        {
            IsActive = active;
            _timer = 0f;
        }

        public void Tick(float dt)
        {
            _timer -= dt;
            if (_timer > 0f) return;
            _timer = definition.HitInterval;
            if (Strike()) Struck?.Invoke();
        }

        /// <summary>Lovește blocurile din fața utilajului. Întoarce true dacă a lovit ceva.</summary>
        protected abstract bool Strike();

        protected Vector3 ToolCenter => transform.position + transform.forward * definition.ToolReach + Vector3.up * 0.5f;
    }
}
