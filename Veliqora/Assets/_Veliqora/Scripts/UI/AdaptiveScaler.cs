using UnityEngine;
using UnityEngine.UI;

namespace Veliqora.UI
{
    /// <summary>Matches width on tall phones and height on wide tablets so layouts never overflow.</summary>
    [RequireComponent(typeof(CanvasScaler))]
    public sealed class AdaptiveScaler : MonoBehaviour
    {
        private CanvasScaler _scaler;
        private Vector2 _last;

        private void Awake() { _scaler = GetComponent<CanvasScaler>(); Apply(); }
        private void Update() { if (_last.x != Screen.width || _last.y != Screen.height) Apply(); }

        private void Apply()
        {
            _last = new Vector2(Screen.width, Screen.height);
            float aspect = Screen.height / Mathf.Max(1f, (float)Screen.width);
            _scaler.matchWidthOrHeight = aspect >= 1920f / 1080f ? 0f : 1f;
        }
    }
}
