using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// Animații procedurale pentru personajele din primitive (jucător, muncitori):
    /// Idle (respirație), Walk (picioare și brațe), Mine (lovitură cu târnăcopul), Carry (aplecat, brațe strânse)
    /// și poziția de șezut în utilaje. Nu atinge colliderul sau mișcarea: doar rotește „oasele” modelului.
    /// Se pune pe obiectul „Model” (copilul personajului) și se leagă oasele în Inspector.
    /// </summary>
    public class CharacterAnimator : MonoBehaviour
    {
        [Header("Oase (pivoți)")]
        [SerializeField] private Transform hips;
        [SerializeField] private Transform torso;
        [SerializeField] private Transform head;
        [SerializeField] private Transform legLeft;
        [SerializeField] private Transform legRight;
        [SerializeField] private Transform armLeft;
        [SerializeField] private Transform armRight;

        [Header("Surse (opționale)")]
        [Tooltip("PlayerMiner sau WorkerAI: anunță loviturile.")]
        [SerializeField] private MonoBehaviour swingSource;
        [Tooltip("Stiva de pe spate: dacă are resurse, personajul „cară”.")]
        [SerializeField] private ResourceStack stack;
        [Tooltip("Doar la jucător: dacă e în utilaj, stă jos.")]
        [SerializeField] private PlayerVehicleDriver driver;

        [Header("Mers")]
        [SerializeField] private float fullSpeed = 4.5f;
        [SerializeField] private float stepsPerSecond = 2.2f;
        [SerializeField] private float legSwing = 38f;
        [SerializeField] private float armSwing = 32f;
        [SerializeField] private float bobHeight = 0.06f;
        [SerializeField] private float leanAngle = 7f;

        [Header("Minat")]
        [SerializeField] private float swingDuration = 0.28f;
        [SerializeField] private Vector3 armRaised = new(-165f, 0f, 8f);
        [SerializeField] private Vector3 armStrike = new(-30f, 0f, 0f);

        [Header("Cărat")]
        [SerializeField] private float carryLean = 6f;

        private ISwingSource _source;
        private Quaternion _hipsRot, _torsoRot, _headRot, _legLRot, _legRRot, _armLRot, _armRRot;
        private Vector3 _hipsPos;
        private Vector3 _lastPosition;
        private float _speed;
        private float _phase;
        private float _swing = -1f;
        private float _time;

        private void Awake()
        {
            _source = swingSource as ISwingSource;
            _hipsPos = hips.localPosition;
            _hipsRot = hips.localRotation;
            _torsoRot = torso.localRotation;
            _headRot = head.localRotation;
            _legLRot = legLeft.localRotation;
            _legRRot = legRight.localRotation;
            _armLRot = armLeft.localRotation;
            _armRRot = armRight.localRotation;
            _lastPosition = transform.position;
        }

        private void OnEnable()
        {
            if (_source != null) _source.Swung += OnSwung;
            _lastPosition = transform.position;
        }

        private void OnDisable()
        {
            if (_source != null) _source.Swung -= OnSwung;
        }

        private void OnSwung(MineBlock _) => _swing = 0f;

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            _time += dt;

            // Viteza reală, din cât s-a deplasat modelul (funcționează pentru orice tip de mișcare).
            Vector3 delta = transform.position - _lastPosition;
            delta.y = 0f;
            _lastPosition = transform.position;
            _speed = Mathf.Lerp(_speed, delta.magnitude / dt, 1f - Mathf.Exp(-10f * dt));

            bool seated = driver != null && driver.IsDriving;
            bool carrying = stack != null && stack.Count > 0;
            float walk = seated ? 0f : Mathf.Clamp01(_speed / fullSpeed);
            if (walk > 0.05f) _phase += dt * stepsPerSecond * Mathf.Lerp(0.7f, 1.25f, walk) * Mathf.PI * 2f;

            float step = Mathf.Sin(_phase);
            float breath = Mathf.Sin(_time * 2.2f);

            // Șold: se leagănă în sus la fiecare pas; respirație ușoară în repaus.
            float bob = Mathf.Abs(step) * bobHeight * walk + breath * 0.012f * (1f - walk);
            hips.localPosition = _hipsPos + Vector3.up * bob;
            hips.localRotation = _hipsRot * Quaternion.Euler(0f, step * 6f * walk, 0f);

            // Picioare.
            float legAngle = seated ? -80f : step * legSwing * walk;
            legLeft.localRotation = _legLRot * Quaternion.Euler(seated ? legAngle : legAngle, 0f, 0f);
            legRight.localRotation = _legRRot * Quaternion.Euler(seated ? legAngle : -legAngle, 0f, 0f);

            // Trunchi: aplecat la mers și când cară; la lovitură se apleacă scurt înainte.
            float swingT = _swing >= 0f ? Mathf.Clamp01(_swing / swingDuration) : -1f;
            float strikeLean = swingT >= 0f ? Mathf.Sin(swingT * Mathf.PI) * 10f : 0f;
            float lean = leanAngle * walk + (carrying ? carryLean : 0f) + strikeLean + breath * 1.2f * (1f - walk);
            torso.localRotation = _torsoRot * Quaternion.Euler(lean, -step * 4f * walk, 0f);
            head.localRotation = _headRot * Quaternion.Euler(-lean * 0.5f + breath * 1.5f, 0f, 0f);

            // Brațe: opuse picioarelor; când cară, brațele stau strânse și puțin în față.
            float armAmplitude = armSwing * walk * (carrying ? 0.45f : 1f);
            float armBase = carrying ? -22f : 0f;
            float left = seated ? -55f : armBase - step * armAmplitude;
            float right = seated ? -55f : armBase + step * armAmplitude;
            armLeft.localRotation = _armLRot * Quaternion.Euler(left, 0f, -6f);
            armRight.localRotation = _armRRot * Quaternion.Euler(right, 0f, 6f);

            if (swingT >= 0f && !seated)
            {
                armRight.localRotation = _armRRot * SwingPose(swingT, right);
                _swing += dt;
                if (_swing >= swingDuration) _swing = -1f;
            }
        }

        /// <summary>Lovitura: ridică brațul peste cap (35%), lovește (20%), revine (45%).</summary>
        private Quaternion SwingPose(float t, float restAngle)
        {
            Vector3 rest = new(restAngle, 0f, 6f);
            if (t < 0.35f) return Quaternion.Euler(Vector3.Lerp(rest, armRaised, EaseOut(t / 0.35f)));
            if (t < 0.55f) return Quaternion.Euler(Vector3.Lerp(armRaised, armStrike, EaseIn((t - 0.35f) / 0.2f)));
            return Quaternion.Euler(Vector3.Lerp(armStrike, rest, EaseOut((t - 0.55f) / 0.45f)));
        }

        private static float EaseOut(float t) => 1f - (1f - t) * (1f - t);
        private static float EaseIn(float t) => t * t;
    }
}
