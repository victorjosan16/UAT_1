using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// Semnalizare 3D pentru o zonă (Vânzare, Upgrade): inel care pulsează, icon care plutește
    /// și se rotește, evidențiere când jucătorul e aproape. Doar vizual — nu are trigger și nu
    /// face nicio tranzacție. Sistemele din Etapa 2/3 pot apela SetHighlighted / PlayActivated.
    /// </summary>
    public class ZoneVisual : MonoBehaviour
    {
        public enum ZoneKind { Sell, Upgrade }

        [SerializeField] private ZoneKind kind = ZoneKind.Sell;
        [SerializeField] private Transform pulseRing;
        [SerializeField] private Transform floatingIcon;
        [Tooltip("Dacă e setat, zona se evidențiază singură când jucătorul intră în rază.")]
        [SerializeField] private Transform player;
        [SerializeField, Min(0.1f)] private float highlightRadius = 2.2f;

        [Header("Animație")]
        [SerializeField] private float pulseSpeed = 1.6f;
        [SerializeField] private float pulseAmount = 0.08f;
        [SerializeField] private float bobHeight = 0.18f;
        [SerializeField] private float bobSpeed = 2f;
        [SerializeField] private float spinSpeed = 70f;
        [SerializeField] private float highlightScale = 1.18f;

        public ZoneKind Kind => kind;
        public bool IsHighlighted { get; private set; }

        private Vector3 _ringBaseScale = Vector3.one;
        private Vector3 _iconBasePosition;
        private Vector3 _iconBaseScale = Vector3.one;
        private float _highlight;
        private float _punchTimer = -1f;
        private bool _manualHighlight;

        private void Awake()
        {
            if (pulseRing != null) _ringBaseScale = pulseRing.localScale;
            if (floatingIcon != null)
            {
                _iconBasePosition = floatingIcon.localPosition;
                _iconBaseScale = floatingIcon.localScale;
            }
        }

        /// <summary>Evidențiere forțată (ex. „ai ceva de vândut”). false = revine la detecția automată.</summary>
        public void SetHighlighted(bool highlighted) => _manualHighlight = highlighted;

        /// <summary>Feedback când zona chiar a făcut ceva (vânzare / upgrade reușit).</summary>
        public void PlayActivated()
        {
            _punchTimer = 0f;
            VfxManager vfx = VfxManager.Instance;
            if (vfx == null) return;
            if (kind == ZoneKind.Upgrade) vfx.PlayUpgrade(transform.position);
            else vfx.PlaySell(transform.position + Vector3.up * 0.5f, 0);
        }

        private void Update()
        {
            float time = Time.time;
            float dt = Time.deltaTime;

            bool near = false;
            if (player != null)
            {
                Vector3 offset = player.position - transform.position;
                offset.y = 0f;
                near = offset.sqrMagnitude <= highlightRadius * highlightRadius;
            }
            IsHighlighted = near || _manualHighlight;
            _highlight = Mathf.MoveTowards(_highlight, IsHighlighted ? 1f : 0f, dt * 4f);

            float punch = 0f;
            if (_punchTimer >= 0f)
            {
                _punchTimer += dt;
                float k = Mathf.Clamp01(_punchTimer / 0.35f);
                punch = Mathf.Sin(k * Mathf.PI) * 0.25f;
                if (k >= 1f) _punchTimer = -1f;
            }

            if (pulseRing != null)
            {
                float pulse = 1f + Mathf.Sin(time * pulseSpeed * Mathf.PI) * pulseAmount * (1f + _highlight);
                float s = pulse * Mathf.Lerp(1f, highlightScale, _highlight) + punch;
                pulseRing.localScale = new Vector3(_ringBaseScale.x * s, _ringBaseScale.y, _ringBaseScale.z * s);
            }

            if (floatingIcon != null)
            {
                float bob = Mathf.Sin(time * bobSpeed) * bobHeight;
                floatingIcon.localPosition = _iconBasePosition + Vector3.up * (bob + _highlight * 0.15f);
                floatingIcon.Rotate(0f, spinSpeed * (1f + _highlight) * dt, 0f, Space.World);
                floatingIcon.localScale = _iconBaseScale * (Mathf.Lerp(1f, highlightScale, _highlight) + punch);
            }
        }
    }
}
