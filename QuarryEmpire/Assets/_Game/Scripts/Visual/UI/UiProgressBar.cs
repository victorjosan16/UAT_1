using UnityEngine;
using UnityEngine.UI;

namespace QuarryEmpire
{
    /// <summary>
    /// Bară de progres cu umplere animată.
    /// Image de tip Filled → folosește fillAmount; altfel (Sliced, cu colțuri rotunjite) întinde
    /// RectTransform-ul umplerii, ca să nu se deformeze capetele rotunde.
    /// </summary>
    public class UiProgressBar : MonoBehaviour
    {
        [SerializeField] private Image fill;
        [SerializeField] private float fillSpeed = 2.5f;

        private float _target;
        private float _shown = -1f;

        public void SetValue(float value01, bool instant = false)
        {
            _target = Mathf.Clamp01(value01);
            if (instant || _shown < 0f) Apply(_shown = _target);
        }

        private void Update()
        {
            if (Mathf.Approximately(_shown, _target)) return;
            _shown = Mathf.MoveTowards(_shown, _target, fillSpeed * Time.unscaledDeltaTime);
            Apply(_shown);
        }

        private void Apply(float value)
        {
            if (fill == null) return;
            if (fill.type == Image.Type.Filled)
            {
                fill.fillAmount = value;
                return;
            }
            RectTransform rect = fill.rectTransform;
            rect.anchorMin = new Vector2(0f, rect.anchorMin.y);
            rect.anchorMax = new Vector2(value, rect.anchorMax.y);
            // Sub grosimea barei capetele rotunde s-ar suprapune urât: ascundem umplerea.
            fill.enabled = value > 0.001f;
        }
    }
}
