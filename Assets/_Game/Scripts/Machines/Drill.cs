using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// Burghiul: lovește puternic cel mai apropiat bloc din față și sparge și blocurile foarte dure.
    /// </summary>
    public class Drill : VehicleTool
    {
        [Tooltip("Vârful burghiului, care se rotește cât e condus.")]
        [SerializeField] private Transform bit;
        [SerializeField] private float spinSpeed = 720f;

        protected override bool Strike()
        {
            float radius = Mathf.Max(0.3f, definition.ToolSize.x);
            int count = Physics.OverlapSphereNonAlloc(ToolCenter, radius, Results, blockLayer, QueryTriggerInteraction.Ignore);

            MineBlock best = null;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                if (!Results[i].TryGetComponent(out MineBlock block) || !block.IsAlive) continue;
                if (!block.CanBeMinedBy(definition.MiningTier)) continue;
                float distance = (block.transform.position - ToolCenter).sqrMagnitude;
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                best = block;
            }

            if (best == null) return false;
            best.TakeDamage(definition.HitDamage);
            return true;
        }

        private void Update()
        {
            if (bit != null && IsActive) bit.Rotate(0f, 0f, spinSpeed * Time.deltaTime, Space.Self);
        }

        private void OnDrawGizmosSelected()
        {
            if (definition == null) return;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(ToolCenter, Mathf.Max(0.3f, definition.ToolSize.x));
        }
    }
}
