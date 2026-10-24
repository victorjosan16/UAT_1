using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// Cariera e înconjurată de roci și are o singură intrare (spre sud). Muncitorii trec prin ea:
    /// dacă pornesc dinăuntru spre afară (sau invers), merg întâi la poarta interioară / exterioară.
    /// </summary>
    public class QuarryNavigation : MonoBehaviour
    {
        public static QuarryNavigation Instance { get; private set; }

        [Tooltip("Jumătate din mărimea interiorului carierei (m), măsurată de la centrul acestui obiect.")]
        [SerializeField] private Vector2 halfSize = new(6.6f, 6.6f);
        [SerializeField] private Transform gateInside;
        [SerializeField] private Transform gateOutside;
        [SerializeField] private float reachDistance = 0.8f;

        public Vector3 GateOutside => gateOutside.position;

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public bool IsInside(Vector3 position)
        {
            Vector3 local = position - transform.position;
            return Mathf.Abs(local.x) < halfSize.x && Mathf.Abs(local.z) < halfSize.y;
        }

        /// <summary>Următorul punct spre care trebuie mers ca să ajungi de la „from” la „to”.</summary>
        public Vector3 NextWaypoint(Vector3 from, Vector3 to)
        {
            bool fromInside = IsInside(from);
            if (fromInside == IsInside(to)) return to;

            Vector3 first = fromInside ? gateInside.position : gateOutside.position;
            Vector3 second = fromInside ? gateOutside.position : gateInside.position;
            Vector3 offset = first - from;
            offset.y = 0f;
            return offset.magnitude > reachDistance ? first : second;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position, new Vector3(halfSize.x * 2f, 1f, halfSize.y * 2f));
        }
    }
}
