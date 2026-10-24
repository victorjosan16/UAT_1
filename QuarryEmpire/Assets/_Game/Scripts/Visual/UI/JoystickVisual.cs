using UnityEngine;
using UnityEngine.UI;

namespace QuarryEmpire
{
    /// <summary>
    /// Doar aspect pentru joystick-ul existent (OnScreenStick rămâne neatins): fundalul se
    /// luminează cât e folosit, iar un indicator arată direcția. Citește poziția mânerului.
    /// </summary>
    public class JoystickVisual : MonoBehaviour
    {
        [SerializeField] private RectTransform handle;
        [SerializeField] private Image background;
        [SerializeField] private RectTransform directionIndicator;
        [SerializeField] private Image directionImage;
        [Tooltip("Aceeași valoare ca Movement Range din OnScreenStick.")]
        [SerializeField, Min(1f)] private float movementRange = 100f;
        [SerializeField] private float idleAlpha = 0.45f;
        [SerializeField] private float activeAlpha = 0.85f;
        [SerializeField] private float activeScale = 1.06f;

        private Vector2 _handleRest;
        private Color _backgroundColor;
        private Color _directionColor;
        private float _active;

        private void Awake()
        {
            if (handle != null) _handleRest = handle.anchoredPosition;
            if (background != null) _backgroundColor = background.color;
            if (directionImage != null) _directionColor = directionImage.color;
        }

        private void LateUpdate()
        {
            if (handle == null) return;
            Vector2 offset = handle.anchoredPosition - _handleRest;
            float amount = Mathf.Clamp01(offset.magnitude / movementRange);
            _active = Mathf.MoveTowards(_active, amount > 0.05f ? 1f : 0f, Time.unscaledDeltaTime * 6f);

            if (background != null)
            {
                Color c = _backgroundColor;
                c.a = Mathf.Lerp(idleAlpha, activeAlpha, _active);
                background.color = c;
                background.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, activeScale, _active);
            }

            if (directionIndicator != null && amount > 0.05f)
            {
                float angle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg - 90f;
                directionIndicator.localRotation = Quaternion.Euler(0f, 0f, angle);
            }
            if (directionImage != null)
            {
                Color c = _directionColor;
                c.a *= _active * amount;
                directionImage.color = c;
            }
        }
    }
}
