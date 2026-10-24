using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// Animații procedurale pentru minerul stilizat: Idle, Walk, Mine și Carry.
    /// Doar vizual: citește starea din PlayerMovement / PlayerMiner și rotește oasele „rig”-ului.
    /// Nu atinge CharacterController-ul, rotația jucătorului sau pivotul târnăcopului
    /// (pe acela îl animă în continuare PickaxeSwing).
    /// </summary>
    [DisallowMultipleComponent]
    public class MinerAnimator : MonoBehaviour
    {
        public enum MinerState { Idle, Walk, Mine, Carry }

        [Header("Surse (gameplay existent)")]
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private PlayerMiner miner;
        [SerializeField] private PlayerStats stats;

        [Header("Oase (create de VisualSetup)")]
        [Tooltip("Rădăcina vizuală: primește bob-ul și turtirea, nu și colliderul.")]
        [SerializeField] private Transform rig;
        [SerializeField] private Transform torso;
        [SerializeField] private Transform head;
        [SerializeField] private Transform leftArm;
        [Tooltip("Brațul drept: în mână (copil) stă PickaxePivot, rotit de PickaxeSwing.")]
        [SerializeField] private Transform rightArm;
        [SerializeField] private Transform leftLeg;
        [SerializeField] private Transform rightLeg;

        [Header("Încărcătură (Carry)")]
        [Tooltip("Bucățile de minereu din rucsac, afișate pe rând pe măsură ce crește încărcătura.")]
        [SerializeField] private Renderer[] loadPieces = new Renderer[0];
        [Tooltip("Până la Etapa 2 (stiva reală), încărcătura crește la fiecare resursă colectată.")]
        [SerializeField] private bool autoLoadFromCollected = true;
        [Tooltip("Doar pentru încărcătura automată: o bucată „dispare” după atâtea secunde fără colectare " +
                 "(altfel, fără vânzare, minerul ar rămâne pentru totdeauna în poza Carry). 0 = niciodată.")]
        [SerializeField, Min(0f)] private float autoLoadDecaySeconds = 6f;

        [Header("Walk")]
        [SerializeField, Min(0.1f)] private float stepsPerMeter = 1.1f;
        [SerializeField] private float legSwing = 32f;
        [SerializeField] private float armSwing = 28f;
        [SerializeField] private float walkBob = 0.06f;

        [Header("Idle")]
        [SerializeField] private float breathSpeed = 1.6f;
        [SerializeField] private float breathAmount = 0.018f;

        [Header("Mine (sincron cu PickaxeSwing: aceeași durată și curbă)")]
        [Tooltip("Cât se apleacă trunchiul la lovitură (grade).")]
        [SerializeField] private float mineLean = 14f;
        [Tooltip("Ține-o egală cu Duration din PickaxeSwing.")]
        [SerializeField, Min(0.05f)] private float swingDuration = 0.25f;
        [Tooltip("Ține-o egală cu Curve din PickaxeSwing (0 = repaus, 1 = lovitură).")]
        [SerializeField] private AnimationCurve swingCurve = new(
            new Keyframe(0f, 0f), new Keyframe(0.35f, 1f), new Keyframe(1f, 0f));
        [Tooltip("Unghiul brațului drept în repaus și la lovitură (negativ = înainte).")]
        [SerializeField] private float rightArmRest = -15f;
        [SerializeField] private float rightArmStrike = -60f;

        [Header("Carry")]
        [SerializeField] private float carryLean = 7f;
        [SerializeField] private float carryArmForward = -45f;

        [Header("Feedback colectare")]
        [SerializeField] private float collectPunch = 0.08f;
        [SerializeField, Min(0.01f)] private float collectPunchDuration = 0.14f;

        [Tooltip("Viteza cu care se amestecă stările (mai mare = tranziții mai bruște).")]
        [SerializeField] private float blendSpeed = 10f;

        public MinerState State { get; private set; }
        public int CarryLoad { get; private set; }

        private Quaternion _torsoBase, _headBase, _leftArmBase, _rightArmBase, _leftLegBase, _rightLegBase;
        private Vector3 _rigBasePosition, _rigBaseScale;
        private Vector3 _lastPosition;
        private float _walkPhase;
        private float _walkWeight;
        private float _carryWeight;
        private float _mineTimer = -1f;
        private float _collectTimer = -1f;
        private float _speed;
        private float _decayTimer;

        private void Awake()
        {
            if (rig == null) rig = transform;
            _rigBasePosition = rig.localPosition;
            _rigBaseScale = rig.localScale;
            _torsoBase = Base(torso);
            _headBase = Base(head);
            _leftArmBase = Base(leftArm);
            _rightArmBase = Base(rightArm);
            _leftLegBase = Base(leftLeg);
            _rightLegBase = Base(rightLeg);
            _lastPosition = transform.position;
            RefreshLoadPieces();
        }

        private static Quaternion Base(Transform t) => t != null ? t.localRotation : Quaternion.identity;

        private void OnEnable()
        {
            if (miner != null) miner.Swung += HandleSwung;
            GameEvents.ResourceCollected += HandleCollected;
        }

        private void OnDisable()
        {
            if (miner != null) miner.Swung -= HandleSwung;
            GameEvents.ResourceCollected -= HandleCollected;
        }

        /// <summary>
        /// Setează câte bucăți „cară” minerul (0 = mâini libere). Pentru Etapa 2:
        /// stiva reală apelează asta și dezactivează autoLoadFromCollected.
        /// </summary>
        public void SetCarryLoad(int amount)
        {
            CarryLoad = Mathf.Max(0, amount);
            RefreshLoadPieces();
        }

        private void HandleSwung(MineBlock _) => _mineTimer = 0f;

        private void HandleCollected(ResourceData resource, Vector3 _)
        {
            _collectTimer = 0f;
            if (!autoLoadFromCollected) return;
            _decayTimer = 0f;

            // Bucata nou apărută primește culoarea resursei colectate (material partajat, fără copii).
            int index = Mathf.Min(CarryLoad, loadPieces.Length - 1);
            if (index >= 0 && resource != null && resource.PickupMaterial != null && loadPieces[index] != null)
                loadPieces[index].sharedMaterial = resource.PickupMaterial;
            SetCarryLoad(CarryLoad + 1);
        }

        private void RefreshLoadPieces()
        {
            for (int i = 0; i < loadPieces.Length; i++)
                if (loadPieces[i] != null) loadPieces[i].enabled = i < CarryLoad;
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            if (autoLoadFromCollected && autoLoadDecaySeconds > 0f && CarryLoad > 0)
            {
                _decayTimer += dt;
                if (_decayTimer >= autoLoadDecaySeconds)
                {
                    _decayTimer = 0f;
                    SetCarryLoad(Mathf.Min(CarryLoad, loadPieces.Length) - 1);
                }
            }

            // Viteza reală (m/s) din deplasare: funcționează indiferent cum mută PlayerMovement jucătorul.
            Vector3 delta = transform.position - _lastPosition;
            delta.y = 0f;
            _lastPosition = transform.position;
            _speed = Mathf.Lerp(_speed, delta.magnitude / dt, 1f - Mathf.Exp(-12f * dt));

            bool moving = movement != null ? movement.IsMoving : _speed > 0.2f;
            bool carrying = CarryLoad > 0;
            float blend = 1f - Mathf.Exp(-blendSpeed * dt);
            _walkWeight = Mathf.Lerp(_walkWeight, moving ? 1f : 0f, blend);
            _carryWeight = Mathf.Lerp(_carryWeight, carrying ? 1f : 0f, blend);

            float maxSpeed = stats != null ? Mathf.Max(0.1f, stats.MoveSpeed) : 5f;
            _walkPhase += _speed * stepsPerMeter * Mathf.PI * dt * (carrying ? 0.85f : 1f);
            if (_walkWeight < 0.01f) _walkPhase = 0f;

            // Aceeași formulă ca PickaxeSwing, pornită de același eveniment Swung: brațul și târnăcopul
            // (rotit de PickaxeSwing în mână) ajung la lovitură în același frame.
            float mineCurve = 0f;
            if (_mineTimer >= 0f)
            {
                _mineTimer += dt / swingDuration;
                mineCurve = _mineTimer >= 1f ? 0f : swingCurve.Evaluate(_mineTimer);
                if (_mineTimer >= 1f) _mineTimer = -1f;
            }

            State = moving ? (carrying ? MinerState.Carry : MinerState.Walk)
                : mineCurve > 0f ? MinerState.Mine
                : carrying ? MinerState.Carry
                : MinerState.Idle;

            float sin = Mathf.Sin(_walkPhase);
            float speedFactor = Mathf.Clamp01(_speed / maxSpeed);
            float swing = _walkWeight * Mathf.Lerp(0.5f, 1f, speedFactor);

            // Picioare și brațul stâng (dreptul ține târnăcopul și urmează PickaxeSwing).
            Rotate(leftLeg, _leftLegBase, sin * legSwing * swing);
            Rotate(rightLeg, _rightLegBase, -sin * legSwing * swing);
            float leftArmAngle = -sin * armSwing * swing * (1f - _carryWeight * 0.7f) + carryArmForward * _carryWeight;
            Rotate(leftArm, _leftArmBase, leftArmAngle);
            Rotate(rightArm, _rightArmBase, Mathf.LerpUnclamped(rightArmRest, rightArmStrike, mineCurve));

            // Trunchi: aplecare la minat, ușor înainte când cară.
            float lean = mineCurve * mineLean + _carryWeight * carryLean + _walkWeight * 3f;
            if (torso != null)
                torso.localRotation = _torsoBase * Quaternion.Euler(lean, sin * 4f * swing, 0f);
            if (head != null)
                head.localRotation = _headBase * Quaternion.Euler(-lean * 0.5f, Mathf.Sin(Time.time * 0.7f) * 6f * (1f - _walkWeight), 0f);

            // Bob la mers + respirație în repaus + turtire la colectare/lovitură.
            float bob = Mathf.Abs(Mathf.Cos(_walkPhase)) * walkBob * swing;
            float breath = Mathf.Sin(Time.time * breathSpeed * Mathf.PI) * breathAmount * (1f - _walkWeight);
            float punch = 0f;
            if (_collectTimer >= 0f)
            {
                _collectTimer += dt;
                float k = Mathf.Clamp01(_collectTimer / collectPunchDuration);
                punch = Mathf.Sin(k * Mathf.PI) * collectPunch;
                if (k >= 1f) _collectTimer = -1f;
            }
            float squash = mineCurve * 0.04f;

            rig.localPosition = _rigBasePosition + Vector3.up * bob;
            rig.localScale = new Vector3(
                _rigBaseScale.x * (1f + punch + squash * 0.5f),
                _rigBaseScale.y * (1f + breath + punch - squash),
                _rigBaseScale.z * (1f + punch + squash * 0.5f));
        }

        private static void Rotate(Transform bone, Quaternion baseRotation, float xDegrees)
        {
            if (bone != null) bone.localRotation = baseRotation * Quaternion.Euler(xDegrees, 0f, 0f);
        }
    }
}
