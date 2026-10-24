using System;
using System.Collections.Generic;
using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// Muncitor angajat: caută cel mai apropiat bloc liber, îl sparge, adună resursele în stiva lui,
    /// iar când stiva e plină le duce la zona SELL. Apoi o ia de la capăt.
    /// Mișcare simplă în linie dreaptă (fără NavMesh), cu ocolire când se blochează.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class WorkerAI : MonoBehaviour, ISwingSource
    {
        private enum State { FindBlock, MoveToBlock, Mine, WaitForDrops, MoveToSell, Selling, Idle }

        private static readonly List<WorkerAI> ActiveWorkers = new();
        public static IReadOnlyList<WorkerAI> Active => ActiveWorkers;

        [SerializeField] private WorkerDefinition definition;
        [SerializeField] private ResourceStack stack;
        [SerializeField] private ResourceCollector collector;
        [SerializeField] private float hitRange = 1.25f;
        [SerializeField] private float gravity = -25f;
        [SerializeField] private float rotationSpeed = 540f;

        public event Action<MineBlock> Swung;

        public float OfflineIncomePerMinute => definition != null ? definition.OfflineIncomePerMinute : 0f;

        private CharacterController _controller;
        private State _state;
        private MineBlock _target;
        private float _timer;
        private float _hitCooldown;
        private float _verticalVelocity;
        private Vector3 _lastPosition;
        private float _stuckTimer;
        private Vector3 _detour;
        private float _detourTimer;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            if (stack != null) stack.Capacity = definition.Capacity;
            if (collector != null) collector.Radius = definition.CollectRadius;
        }

        private void OnEnable()
        {
            ActiveWorkers.Add(this);
            _state = State.FindBlock;
        }

        private void OnDisable()
        {
            ActiveWorkers.Remove(this);
            ReleaseTarget();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (_hitCooldown > 0f) _hitCooldown -= dt;
            Vector3 move = Vector3.zero;

            switch (_state)
            {
                case State.FindBlock:
                    if (stack.IsFull) { GoSell(); break; }
                    _target = FindNearestFreeBlock();
                    if (_target != null)
                    {
                        _target.ClaimedBy = this;
                        _state = State.MoveToBlock;
                    }
                    else if (!stack.IsEmpty) GoSell();
                    else { _state = State.Idle; _timer = 1f; }
                    break;

                case State.MoveToBlock:
                    if (!IsTargetValid()) { ReleaseTarget(); _state = State.FindBlock; break; }
                    if (HorizontalDistance(_target.transform.position) <= hitRange) _state = State.Mine;
                    else move = Steer(_target.transform.position, dt);
                    break;

                case State.Mine:
                    if (!IsTargetValid())
                    {
                        ReleaseTarget();
                        _state = State.WaitForDrops;
                        _timer = 0.7f; // lăsăm resursele să cadă și să fie strânse
                        break;
                    }
                    Face(_target.transform.position, dt);
                    if (_hitCooldown <= 0f)
                    {
                        _hitCooldown = definition.HitInterval;
                        Swung?.Invoke(_target);
                        _target.TakeDamage(definition.HitDamage);
                    }
                    break;

                case State.WaitForDrops:
                    _timer -= dt;
                    if (_timer <= 0f) _state = stack.IsFull ? State.MoveToSell : State.FindBlock;
                    break;

                case State.MoveToSell:
                    SellZone zone = SellZone.Main;
                    if (zone == null) { _state = State.Idle; _timer = 2f; break; }
                    if (zone.Area.Contains(transform.position)) { _state = State.Selling; _timer = 15f; }
                    else move = Steer(zone.Position, dt);
                    break;

                case State.Selling:
                    _timer -= dt;
                    if (stack.IsEmpty || _timer <= 0f) _state = State.FindBlock;
                    break;

                case State.Idle:
                    _timer -= dt;
                    if (_timer <= 0f) _state = State.FindBlock;
                    break;
            }

            if (_controller.isGrounded && _verticalVelocity < 0f) _verticalVelocity = -2f;
            _verticalVelocity += gravity * dt;
            _controller.Move((move * definition.MoveSpeed + Vector3.up * _verticalVelocity) * dt);
        }

        private void GoSell()
        {
            ReleaseTarget();
            _state = State.MoveToSell;
        }

        private bool IsTargetValid() => _target != null && _target.IsAlive && _target.ClaimedBy == this;

        private void ReleaseTarget()
        {
            if (_target != null && _target.ClaimedBy == this) _target.ClaimedBy = null;
            _target = null;
        }

        private MineBlock FindNearestFreeBlock()
        {
            MineBlock best = null;
            float bestDistance = float.MaxValue;
            IReadOnlyList<MineBlock> blocks = MineBlock.Active;
            for (int i = 0; i < blocks.Count; i++)
            {
                MineBlock block = blocks[i];
                if (!block.IsAlive || block.ClaimedBy != null || !block.CanBeMinedBy(definition.MiningTier)) continue;
                float distance = (block.transform.position - transform.position).sqrMagnitude;
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                best = block;
            }
            return best;
        }

        /// <summary>Direcția de mers spre țintă; dacă stă pe loc prea mult, încearcă un ocol lateral.</summary>
        private Vector3 Steer(Vector3 target, float dt)
        {
            Vector3 direction = target - transform.position;
            direction.y = 0f;
            direction.Normalize();

            if (_detourTimer > 0f)
            {
                _detourTimer -= dt;
                direction = (direction + _detour * 1.5f).normalized;
            }
            else
            {
                float moved = (transform.position - _lastPosition).magnitude;
                _stuckTimer = moved < definition.MoveSpeed * dt * 0.2f ? _stuckTimer + dt : 0f;
                if (_stuckTimer > 0.6f)
                {
                    _stuckTimer = 0f;
                    _detourTimer = 0.8f;
                    _detour = Vector3.Cross(Vector3.up, direction) * (UnityEngine.Random.value < 0.5f ? 1f : -1f);
                    // Dacă se blochează spre un bloc, renunță la el și alege altul.
                    if (_state == State.MoveToBlock)
                    {
                        ReleaseTarget();
                        _state = State.FindBlock;
                    }
                }
            }
            _lastPosition = transform.position;
            Face(transform.position + direction, dt);
            return direction;
        }

        private void Face(Vector3 point, float dt)
        {
            Vector3 look = point - transform.position;
            look.y = 0f;
            if (look.sqrMagnitude < 0.0001f) return;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(look), rotationSpeed * dt);
        }

        private float HorizontalDistance(Vector3 point)
        {
            Vector3 offset = point - transform.position;
            offset.y = 0f;
            return offset.magnitude;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => ActiveWorkers.Clear();
    }
}
