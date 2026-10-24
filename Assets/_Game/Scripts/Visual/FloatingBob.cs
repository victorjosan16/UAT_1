using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>Iconiță 3D care plutește și se rotește încet deasupra unei zone (monedă, săgeată, ladă).</summary>
    public class FloatingBob : MonoBehaviour
    {
        [SerializeField] private float amplitude = 0.15f;
        [SerializeField] private float frequency = 1.6f;
        [SerializeField] private float spinSpeed = 60f;

        private Vector3 _basePosition;
        private float _offset;

        /// <summary>Multiplicator pentru viteză (ZoneHighlight îl crește când jucătorul e pe zonă).</summary>
        public float SpeedMultiplier { get; set; } = 1f;

        private void Awake()
        {
            _basePosition = transform.localPosition;
            _offset = Random.value * 10f; // ca iconițele să nu se miște sincron
        }

        private void Update()
        {
            float t = Time.time * frequency + _offset;
            transform.localPosition = _basePosition + Vector3.up * (Mathf.Sin(t) * amplitude);
            transform.Rotate(0f, spinSpeed * SpeedMultiplier * Time.deltaTime, 0f, Space.World);
        }
    }
}
