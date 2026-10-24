using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// Lama buldozerului: lovește toate blocurile dintr-o zonă dreptunghiulară din față.
    /// </summary>
    public class Bulldozer : VehicleTool
    {
        [Tooltip("Lama, care „împinge” vizual la fiecare lovitură.")]
        [SerializeField] private Transform blade;
        [SerializeField] private float bladePunch = 0.25f;

        private Vector3 _bladeRest;
        private float _punch;

        private void Awake()
        {
            if (blade != null) _bladeRest = blade.localPosition;
            Struck += () => _punch = 1f;
        }

        protected override bool Strike()
        {
            int count = Physics.OverlapBoxNonAlloc(ToolCenter, definition.ToolSize * 0.5f, Results,
                transform.rotation, blockLayer, QueryTriggerInteraction.Ignore);

            bool hitSomething = false;
            for (int i = 0; i < count; i++)
            {
                if (!Results[i].TryGetComponent(out MineBlock block) || !block.IsAlive) continue;
                if (!block.CanBeMinedBy(definition.MiningTier)) continue;
                block.TakeDamage(definition.HitDamage);
                hitSomething = true;
            }
            return hitSomething;
        }

        private void Update()
        {
            if (blade == null) return;
            _punch = Mathf.MoveTowards(_punch, 0f, Time.deltaTime * 5f);
            blade.localPosition = _bladeRest + Vector3.forward * (bladePunch * Mathf.Sin(_punch * Mathf.PI));
        }

        private void OnDrawGizmosSelected()
        {
            if (definition == null) return;
            Gizmos.color = Color.red;
            Gizmos.matrix = Matrix4x4.TRS(ToolCenter, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, definition.ToolSize);
        }
    }
}
