using System.Collections;
using UnityEngine;

namespace LastBlock.Gameplay
{
    public enum BlockState
    {
        Idle,
        Dragging,
        Extracted,
        OutOfBounds
    }

    /// <summary>
    /// One tower block: physics setup, selection highlight, impact reporting and the extraction animation.
    /// The block's long axis is its local X axis.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
    public class BlockController : MonoBehaviour
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");

        [SerializeField] Color highlightColor = new Color(1f, 0.95f, 0.6f);
        [SerializeField] Color deniedColor = new Color(1f, 0.25f, 0.25f);
        [SerializeField] float highlightFadeSpeed = 8f;
        [SerializeField] float deniedFlashDuration = 0.45f;
        [SerializeField] float minImpactSpeed = 0.8f;
        [SerializeField] float outOfBoundsHeight = -8f;
        [SerializeField] float extractAnimationDuration = 0.35f;

        public Rigidbody Body { get; private set; }
        public BoxCollider BoxCollider { get; private set; }
        public BlockState State { get; private set; } = BlockState.Idle;
        public int LayerIndex { get; private set; }
        public int IndexInLayer { get; private set; }
        /// <summary>Top-layer blocks are locked, like in the real game.</summary>
        public bool IsLocked { get; private set; }
        public Color BaseColor { get; private set; } = Color.white;

        // Stability bookkeeping, owned by TowerStabilityChecker.
        public Vector3 BaselinePosition { get; set; }
        public float FallenTimer { get; set; }

        public bool CanBeSelected => State == BlockState.Idle && !IsLocked && isActiveAndEnabled;

        /// <summary>World axis (X or Z, always positive) that the block slides along.</summary>
        public Vector3 PullAxis
        {
            get
            {
                Vector3 right = transform.right;
                return Mathf.Abs(right.x) >= Mathf.Abs(right.z) ? Vector3.right : Vector3.forward;
            }
        }

        TowerGenerator owner;
        Renderer cachedRenderer;
        MaterialPropertyBlock propertyBlock;
        bool selected;
        float highlightAmount;
        float deniedTimer;
        bool propertyBlockApplied;

        void Awake()
        {
            CacheComponents();
        }

        void CacheComponents()
        {
            if (Body == null) Body = GetComponent<Rigidbody>();
            if (BoxCollider == null) BoxCollider = GetComponent<BoxCollider>();
            if (cachedRenderer == null) cachedRenderer = GetComponent<Renderer>();
            if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();
        }

        /// <summary>Configures the block. Called by TowerGenerator right after instantiation.</summary>
        public void Initialize(TowerGenerator generator, int layer, int indexInLayer, bool locked, Vector3 size,
            float mass, PhysicsMaterial physicsMaterial, Material renderMaterial)
        {
            CacheComponents();
            owner = generator;
            LayerIndex = layer;
            IndexInLayer = indexInLayer;
            IsLocked = locked;
            State = BlockState.Idle;
            FallenTimer = 0f;

            transform.localScale = size;

            BoxCollider.size = Vector3.one;
            BoxCollider.center = Vector3.zero;
            if (physicsMaterial != null) BoxCollider.sharedMaterial = physicsMaterial;

            Body.mass = Mathf.Max(0.01f, mass);
            Body.useGravity = true;
            Body.isKinematic = false;
            Body.linearDamping = 0.05f;
            Body.angularDamping = 0.2f;
            Body.interpolation = RigidbodyInterpolation.Interpolate;
            Body.collisionDetectionMode = CollisionDetectionMode.Discrete;
            Body.constraints = RigidbodyConstraints.None;

            if (cachedRenderer != null && renderMaterial != null)
            {
                cachedRenderer.sharedMaterial = renderMaterial;
                BaseColor = ReadMaterialColor(renderMaterial);
            }
        }

        static Color ReadMaterialColor(Material material)
        {
            if (material.HasProperty(BaseColorId)) return material.GetColor(BaseColorId);
            if (material.HasProperty(ColorId)) return material.GetColor(ColorId);
            return Color.white;
        }

        public void SetSelected(bool value)
        {
            selected = value;
        }

        /// <summary>Short red flash, used when the player taps a locked block.</summary>
        public void FlashDenied()
        {
            deniedTimer = deniedFlashDuration;
        }

        /// <summary>
        /// Starts a physical drag: rotation and the two non-pull axes are frozen so the block can only slide
        /// along its long axis. Neighbours in the same layer are parallel, so it never passes through them.
        /// </summary>
        public void BeginDrag(Vector3 pullAxis)
        {
            if (State != BlockState.Idle) return;
            State = BlockState.Dragging;
            FallenTimer = 0f;
            RigidbodyConstraints constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
            constraints |= pullAxis.x > 0.5f ? RigidbodyConstraints.FreezePositionZ : RigidbodyConstraints.FreezePositionX;
            Body.constraints = constraints;
            Body.WakeUp();
            SetSelected(true);
        }

        /// <summary>Releases the block back to free physics.</summary>
        public void EndDrag()
        {
            if (State != BlockState.Dragging) return;
            State = BlockState.Idle;
            Body.constraints = RigidbodyConstraints.None;
            Body.WakeUp();
            SetSelected(false);
        }

        /// <summary>
        /// The block has fully left the tower: remove it from the simulation and play a short fly-out animation.
        /// </summary>
        public void MarkExtracted(Vector3 exitDirection)
        {
            if (State == BlockState.Extracted) return;
            State = BlockState.Extracted;
            SetSelected(false);

            Body.linearVelocity = Vector3.zero;
            Body.angularVelocity = Vector3.zero;
            Body.constraints = RigidbodyConstraints.None;
            Body.interpolation = RigidbodyInterpolation.None;
            Body.isKinematic = true;
            BoxCollider.enabled = false;
            if (owner != null) owner.WakeAll();

            if (isActiveAndEnabled) StartCoroutine(ExtractRoutine(exitDirection.normalized));
            else Destroy(gameObject);
        }

        IEnumerator ExtractRoutine(Vector3 direction)
        {
            Vector3 startPosition = transform.position;
            Vector3 startScale = transform.localScale;
            float time = 0f;
            while (time < extractAnimationDuration)
            {
                time += Time.deltaTime;
                float k = Mathf.Clamp01(time / extractAnimationDuration);
                transform.position = startPosition + direction * (k * 0.9f) + Vector3.up * (Mathf.Sin(k * Mathf.PI) * 0.25f);
                transform.localScale = startScale * (1f - k * k);
                yield return null;
            }
            Destroy(gameObject);
        }

        void FixedUpdate()
        {
            // Blocks that fall far below the platform are removed from the simulation and counted as fallen.
            if (State != BlockState.Extracted && State != BlockState.OutOfBounds && transform.position.y < outOfBoundsHeight)
            {
                State = BlockState.OutOfBounds;
                Body.linearVelocity = Vector3.zero;
                Body.angularVelocity = Vector3.zero;
                Body.isKinematic = true;
                gameObject.SetActive(false);
            }
        }

        void Update()
        {
            float target = selected ? 1f : 0f;
            highlightAmount = Mathf.MoveTowards(highlightAmount, target, highlightFadeSpeed * Time.deltaTime);
            if (deniedTimer > 0f) deniedTimer -= Time.deltaTime;
            ApplyHighlight();
        }

        void ApplyHighlight()
        {
            if (cachedRenderer == null) return;

            if (highlightAmount <= 0.001f && deniedTimer <= 0f)
            {
                if (propertyBlockApplied)
                {
                    propertyBlock.Clear();
                    cachedRenderer.SetPropertyBlock(propertyBlock);
                    propertyBlockApplied = false;
                }
                return;
            }

            Color color = BaseColor;
            if (highlightAmount > 0f)
            {
                float pulse = 0.55f + 0.25f * Mathf.Sin(Time.time * 8f);
                color = Color.Lerp(BaseColor, highlightColor, highlightAmount * pulse);
            }
            if (deniedTimer > 0f)
            {
                color = Color.Lerp(color, deniedColor, deniedTimer / deniedFlashDuration);
            }

            propertyBlock.SetColor(BaseColorId, color);
            propertyBlock.SetColor(ColorId, color);
            cachedRenderer.SetPropertyBlock(propertyBlock);
            propertyBlockApplied = true;
        }

        void OnCollisionEnter(Collision collision)
        {
            if (State == BlockState.Extracted || owner == null) return;
            float speed = collision.relativeVelocity.magnitude;
            if (speed >= minImpactSpeed) owner.ReportImpact(this, speed);
        }
    }
}
