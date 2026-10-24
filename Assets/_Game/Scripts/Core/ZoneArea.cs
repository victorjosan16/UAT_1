using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// O zonă dreptunghiulară pe sol. Verifică matematic dacă un punct e în interior,
    /// fără fizică (mai ieftin decât trigger-ele și funcționează și pentru obiecte fără Rigidbody).
    /// </summary>
    public class ZoneArea : MonoBehaviour
    {
        [Tooltip("Mărimea zonei în metri (X = lățime, Z = adâncime). Y = toleranța pe înălțime.")]
        [SerializeField] private Vector3 size = new(3f, 4f, 3f);

        public Vector3 Size => size;
        public Vector3 Center => transform.position;

        public bool Contains(Vector3 worldPoint)
        {
            Vector3 local = transform.InverseTransformPoint(worldPoint);
            Vector3 scale = transform.lossyScale;
            // InverseTransformPoint ține cont de scară; o anulăm ca „size” să fie în metri.
            local = Vector3.Scale(local, scale);
            return Mathf.Abs(local.x) <= size.x * 0.5f
                   && Mathf.Abs(local.z) <= size.z * 0.5f
                   && Mathf.Abs(local.y) <= size.y * 0.5f;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.35f);
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, size);
        }
    }
}
