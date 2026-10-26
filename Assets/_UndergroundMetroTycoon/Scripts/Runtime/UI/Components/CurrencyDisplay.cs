using UnityEngine;
using UnityEngine.UI;

namespace UndergroundMetroTycoon.Game
{
    /// <summary>Coin pill: icon plus a number that rolls toward the real wallet balance and punches on gains.</summary>
    public sealed class CurrencyDisplay : MonoBehaviour
    {
        private Text _amount;
        private double _shown;
        private long _target;
        private float _punch;

        public RectTransform IconTarget { get; private set; }

        public static CurrencyDisplay Create(Transform parent, Sprite icon, long initial)
        {
            Image pill = UIFactory.Image("Currency", parent, UITheme.PanelGlass, UISprites.Rounded, UITheme.CornerPill);
            UIFactory.AddShadow(pill, 5f, 0.35f);
            CurrencyDisplay view = pill.gameObject.AddComponent<CurrencyDisplay>();

            Image iconImage = UIFactory.Image("Icon", pill.transform, Color.white, icon);
            iconImage.preserveAspect = true;
            UIFactory.Place(iconImage.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(8f, 2f), new Vector2(84f, 84f));
            view.IconTarget = iconImage.rectTransform;

            view._amount = UIFactory.Text("Amount", pill.transform, UITheme.Number(initial), 46, UITheme.TextOnDark, TextAnchor.MiddleRight, true);
            UIFactory.Stretch(view._amount.rectTransform, 96f, 0f, 28f, 4f);
            view._shown = initial;
            view._target = initial;
            return view;
        }

        public void SetTarget(long value) => _target = value;

        public void Punch() => _punch = UITheme.MotionNormal;

        private void Update()
        {
            if (System.Math.Abs(_shown - _target) > 0.01)
            {
                double step = System.Math.Max(1.0, System.Math.Abs(_target - _shown) * Time.unscaledDeltaTime * 6.0);
                _shown = _shown < _target ? System.Math.Min(_target, _shown + step) : System.Math.Max(_target, _shown - step);
                _amount.text = UITheme.Number((long)System.Math.Round(_shown));
            }

            float s = 1f;
            if (_punch > 0f)
            {
                _punch -= Time.unscaledDeltaTime;
                s = 1f + Mathf.Sin(Mathf.Clamp01(1f - _punch / UITheme.MotionNormal) * Mathf.PI) * 0.1f;
            }
            transform.localScale = new Vector3(s, s, 1f);
        }
    }
}
