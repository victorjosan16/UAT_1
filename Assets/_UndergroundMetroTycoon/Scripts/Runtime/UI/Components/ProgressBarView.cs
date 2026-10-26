using UnityEngine;
using UnityEngine.UI;

namespace UndergroundMetroTycoon.Game
{
    /// <summary>Rounded progress bar whose fill eases toward its target value, with an optional centred label.</summary>
    public sealed class ProgressBarView : MonoBehaviour
    {
        private RectTransform _fill;
        private Image _fillImage;
        private Text _label;
        private float _current;
        private float _target;

        public Text Label => _label;

        public static ProgressBarView Create(Transform parent, Color fill, Color background, bool withLabel = false, int labelSize = UITheme.SizeCaption)
        {
            Image bg = UIFactory.Image("ProgressBar", parent, background, UISprites.Rounded, 0.4f);
            ProgressBarView view = bg.gameObject.AddComponent<ProgressBarView>();

            view._fillImage = UIFactory.Image("Fill", bg.transform, fill, UISprites.Rounded, 0.4f);
            view._fill = view._fillImage.rectTransform;
            view._fill.anchorMin = Vector2.zero;
            view._fill.anchorMax = new Vector2(0f, 1f);
            view._fill.offsetMin = new Vector2(4f, 4f);
            view._fill.offsetMax = new Vector2(-4f, -4f);

            Image gloss = UIFactory.Image("Gloss", view._fill, new Color(1f, 1f, 1f, 0.25f), UISprites.Rounded, 0.3f);
            gloss.rectTransform.anchorMin = new Vector2(0f, 0.55f);
            gloss.rectTransform.anchorMax = Vector2.one;
            gloss.rectTransform.offsetMin = new Vector2(4f, 0f);
            gloss.rectTransform.offsetMax = new Vector2(-4f, -2f);

            if (withLabel)
            {
                view._label = UIFactory.Text("Label", bg.transform, "", labelSize, Color.white, TextAnchor.MiddleCenter, true);
                UIFactory.Stretch(view._label.rectTransform);
                UIFactory.AddShadow(view._label, 2f, 0.6f);
            }
            view.Apply(0f);
            return view;
        }

        public RectTransform Rect => (RectTransform)transform;

        public void SetColor(Color fill) => _fillImage.color = fill;

        public void SetValue(float value, bool instant = false)
        {
            _target = Mathf.Clamp01(value);
            if (!instant) return;
            _current = _target;
            Apply(_current);
        }

        private void Update()
        {
            if (Mathf.Approximately(_current, _target)) return;
            _current = Mathf.MoveTowards(_current, _target, Time.unscaledDeltaTime * Mathf.Max(0.6f, Mathf.Abs(_target - _current) * 6f));
            Apply(_current);
        }

        private void Apply(float value)
        {
            _fill.anchorMax = new Vector2(value, 1f);
            _fill.gameObject.SetActive(value > 0.005f);
        }
    }
}
