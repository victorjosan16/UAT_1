using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// Când blocul se sparge, aruncă în jur bucățile de resursă (luate din ObjectPool).
    /// </summary>
    [RequireComponent(typeof(MineBlock))]
    public class BlockDropSpawner : MonoBehaviour
    {
        [SerializeField] private ResourcePickup pickupPrefab;
        [Tooltip("Distanța minimă/maximă (m) de la centrul blocului unde aterizează bucățile.")]
        [SerializeField, Min(0f)] private float minScatter = 0.5f;
        [SerializeField, Min(0f)] private float maxScatter = 1.2f;

        private MineBlock _block;
        private Collider _collider;

        private void Awake()
        {
            _block = GetComponent<MineBlock>();
            _collider = GetComponent<Collider>();
        }

        private void OnEnable() => _block.Broken += HandleBroken;
        private void OnDisable() => _block.Broken -= HandleBroken;

        private void HandleBroken(MineBlock block)
        {
            BlockData data = block.Data;
            if (data == null || data.DroppedResource == null || pickupPrefab == null) return;
            if (ObjectPool.Instance == null)
            {
                Debug.LogWarning("Lipsește ObjectPool din scenă.", this);
                return;
            }

            Bounds bounds = _collider != null ? _collider.bounds : new Bounds(transform.position, Vector3.one);
            Vector3 center = bounds.center;
            float groundY = bounds.min.y;

            int count = Random.Range(data.MinDrops, data.MaxDrops + 1); // max e exclusiv la int
            for (int i = 0; i < count; i++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float distance = Random.Range(minScatter, maxScatter);
                Vector3 landing = new(
                    center.x + Mathf.Cos(angle) * distance,
                    groundY,
                    center.z + Mathf.Sin(angle) * distance);

                ResourcePickup pickup = ObjectPool.Instance.Spawn(
                    pickupPrefab, center, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
                pickup.Setup(data.DroppedResource);
                pickup.Launch(center, landing);
            }
        }
    }
}
