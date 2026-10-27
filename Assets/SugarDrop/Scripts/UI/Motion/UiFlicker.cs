using UnityEngine;

namespace SugarDrop
{
    /// <summary>Pâlpâire de flacără pentru un element de interfață: crește și scade puțin și se leagănă.</summary>
    public class UiFlicker : MonoBehaviour
    {
        private float _phase;

        private void Awake() => _phase = Random.value * 10f;

        private void Update()
        {
            float t = Time.unscaledTime * 7f + _phase;
            transform.localScale = new Vector3(1f + Mathf.Sin(t * 1.3f) * 0.06f, 1f + Mathf.Sin(t) * 0.1f, 1f);
            transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 0.7f) * 6f);
        }
    }
}
