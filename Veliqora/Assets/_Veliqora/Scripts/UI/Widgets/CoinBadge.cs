using System;
using UnityEngine;
using UnityEngine.UI;
using Veliqora.Core.Economy;

namespace Veliqora.UI.Widgets
{
    /// <summary>Coin balance pill with "+" shortcut to the shop; animates when the balance changes.</summary>
    public sealed class CoinBadge : MonoBehaviour
    {
        private EconomyService _economy;
        private Text _label;
        private long _shown;
        private Coroutine _anim;
        private RectTransform _icon;

        public void Init(EconomyService economy, Action onPlus)
        {
            _economy = economy;
            var rt = (RectTransform)transform;
            var bg = UIFactory.Image(rt, "Pill", SpriteLibrary.Rounded(48), new Color(0.08f, 0.1f, 0.25f, 0.55f), true);
            bg.rectTransform.offsetMin = new Vector2(40, 10);
            bg.rectTransform.offsetMax = new Vector2(0, -10);
            var icon = UIFactory.Image(rt, "Coin", SpriteLibrary.Get(Icon.Coin), Color.white);
            _icon = icon.rectTransform;
            UIFactory.Place(_icon, new Vector2(0, 0.5f), new Vector2(104, 104), new Vector2(0, 0), new Vector2(0, 0.5f));
            _label = UIFactory.Text(rt, "0", UIStyle.FontBody, Color.white, TextAnchor.MiddleCenter);
            _label.rectTransform.offsetMin = new Vector2(100, 0);
            _label.rectTransform.offsetMax = new Vector2(-84, 0);
            UIFactory.AddShadow(_label, new Color(0, 0, 0, 0.3f), new Vector2(0, -3));
            var plus = UIFactory.IconButton(rt, Icon.Plus, UIStyle.ButtonFill(UIStyle.ButtonKind.Primary), 80, onPlus);
            UIFactory.Place(plus.Rect, new Vector2(1, 0.5f), plus.Rect.sizeDelta, new Vector2(-6, 0), new Vector2(1, 0.5f));
            var hit = bg.gameObject.AddComponent<Button>();
            hit.transition = Selectable.Transition.None;
            hit.onClick.AddListener(() => onPlus?.Invoke());

            _shown = economy.Coins;
            _label.text = Format(_shown);
            economy.Changed += OnChanged;
        }

        private void OnDestroy()
        {
            if (_economy != null) _economy.Changed -= OnChanged;
        }

        private static string Format(long v) => v >= 100000 ? (v / 1000) + "K" : v.ToString("N0");

        private void OnChanged()
        {
            if (this == null || _label == null) return;
            long target = _economy.Coins;
            long from = _shown;
            if (_anim != null) StopCoroutine(_anim);
            _anim = Tween.Run(this, MotionPrefs.Dur(0.5f), k =>
            {
                _shown = (long)Mathf.Lerp(from, target, k);
                _label.text = Format(_shown);
            }, () => { _shown = target; _label.text = Format(target); });
            if (target > from && !MotionPrefs.Reduced)
                Tween.Run(this, 0.3f, k => { float s = 1f + Mathf.Sin(k * Mathf.PI) * 0.18f; _icon.localScale = new Vector3(s, s, 1); }, () => _icon.localScale = Vector3.one, Ease.Linear);
        }
    }
}
