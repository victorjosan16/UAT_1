using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// Feedback vizual pentru zone: când jucătorul stă pe zonă, bordura „pulsează” și iconița se rotește mai repede.
    /// Doar vizual: nu schimbă nimic din logica zonei.
    /// </summary>
    public class ZoneHighlight : MonoBehaviour
    {
        [SerializeField] private ZoneArea area;
        [Tooltip("Bordura plăcii (se scalează ușor).")]
        [SerializeField] private Transform border;
        [SerializeField] private FloatingBob icon;
        [SerializeField] private float pulseAmount = 0.06f;
        [SerializeField] private float pulseSpeed = 9f;

        private Vector3 _borderScale;
        private float _blend;

        private void Awake()
        {
            if (border != null) _borderScale = border.localScale;
        }

        private void Update()
        {
            PlayerRoot player = PlayerRoot.Instance;
            bool inside = player != null && area != null && area.Contains(player.Position);
            _blend = Mathf.MoveTowards(_blend, inside ? 1f : 0f, Time.deltaTime * 5f);

            if (border != null)
            {
                float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount * _blend + 0.03f * _blend;
                border.localScale = new Vector3(_borderScale.x * pulse, _borderScale.y, _borderScale.z * pulse);
            }
            if (icon != null) icon.SpeedMultiplier = 1f + 4f * _blend;
        }
    }
}
