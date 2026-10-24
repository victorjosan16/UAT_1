using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// O bucată de resursă căzută din bloc. Ciclu de viață:
    /// 1) sare în arc până pe sol, 2) stă pe jos, 3) zboară spre jucător și e colectată.
    /// Obiectul vine din ObjectPool și se întoarce acolo după colectare.
    /// </summary>
    public class ResourcePickup : MonoBehaviour, IPoolable
    {
        private enum State { Popping, Resting, Flying }

        [SerializeField] private Renderer pickupRenderer;

        [Header("Săritura din bloc")]
        [SerializeField, Min(0.05f)] private float popDuration = 0.45f;
        [SerializeField, Min(0f)] private float popHeight = 1.2f;
        [Tooltip("Jumătate din înălțimea bucății, ca să stea pe sol, nu în el.")]
        [SerializeField] private float groundOffset = 0.15f;

        [Header("Pe jos")]
        [Tooltip("Cât stă pe jos înainte să poată fi colectată (să se vadă că a căzut).")]
        [SerializeField, Min(0f)] private float restDelay = 0.2f;
        [SerializeField] private float spinSpeed = 90f;

        [Header("Zbor spre jucător")]
        [SerializeField] private float flyStartSpeed = 6f;
        [SerializeField] private float flyAcceleration = 30f;
        [SerializeField] private float arriveDistance = 0.25f;

        public ResourceData Data { get; private set; }
        public bool CanBeCollected => _state == State.Resting && _timer >= restDelay && Data != null;

        private State _state;
        private float _timer;
        private Vector3 _from;
        private Vector3 _to;
        private Transform _target;
        private float _flySpeed;

        /// <summary>Setează ce resursă reprezintă bucata (și materialul ei).</summary>
        public void Setup(ResourceData data)
        {
            Data = data;
            if (pickupRenderer != null && data.PickupMaterial != null)
                pickupRenderer.sharedMaterial = data.PickupMaterial;
        }

        /// <summary>Pornește săritura de la poziția blocului la punctul de aterizare.</summary>
        public void Launch(Vector3 from, Vector3 landing)
        {
            _from = from;
            _to = landing + Vector3.up * groundOffset;
            _timer = 0f;
            _state = State.Popping;
            transform.position = from;
        }

        /// <summary>Începe zborul spre ținta dată (de obicei spatele jucătorului).</summary>
        public void StartCollect(Transform target)
        {
            if (!CanBeCollected) return;
            _target = target;
            _flySpeed = flyStartSpeed;
            _state = State.Flying;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            switch (_state)
            {
                case State.Popping:
                    _timer += dt;
                    float t = Mathf.Clamp01(_timer / popDuration);
                    // Parabolă: 4*t*(1-t) e 0 la capete și 1 la mijloc.
                    transform.position = Vector3.Lerp(_from, _to, t) + Vector3.up * (4f * popHeight * t * (1f - t));
                    if (t >= 1f)
                    {
                        _state = State.Resting;
                        _timer = 0f;
                    }
                    break;

                case State.Resting:
                    _timer += dt;
                    transform.Rotate(0f, spinSpeed * dt, 0f, Space.World);
                    break;

                case State.Flying:
                    if (_target == null)
                    {
                        _state = State.Resting;
                        break;
                    }
                    _flySpeed += flyAcceleration * dt;
                    transform.position = Vector3.MoveTowards(transform.position, _target.position, _flySpeed * dt);
                    if ((transform.position - _target.position).sqrMagnitude <= arriveDistance * arriveDistance)
                        Collect();
                    break;
            }
        }

        private void Collect()
        {
            GameEvents.RaiseResourceCollected(Data, transform.position);
            if (ObjectPool.Instance != null) ObjectPool.Instance.Despawn(gameObject);
            else gameObject.SetActive(false);
        }

        public void OnSpawned()
        {
            _state = State.Popping;
            _timer = 0f;
            _target = null;
        }

        public void OnDespawned()
        {
            _target = null;
            Data = null;
        }
    }
}
