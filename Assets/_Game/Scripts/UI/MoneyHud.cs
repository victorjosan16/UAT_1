using UnityEngine;
using UnityEngine.UI;

namespace QuarryEmpire
{
    /// <summary>Banii în partea de sus a ecranului, cu un mic „puls” la schimbare și indicatorul x2.</summary>
    public class MoneyHud : MonoBehaviour
    {
        public static MoneyHud Instance { get; private set; }

        private Text _money;
        private Text _boost;
        private RectTransform _panel;
        private float _pulse;
        private long _shown = -1;

        /// <summary>Ținta spre care zboară monedele din UI.</summary>
        public RectTransform CoinTarget => _panel;

        private void Awake()
        {
            Instance = this;
            UiTheme theme = UiRoot.Theme;

            Image panel = UiFactory.CreatePanel("MoneyPanel", transform, theme.PanelColor);
            _panel = UiFactory.Place((RectTransform)panel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -50f), new Vector2(380f, 110f));
            panel.raycastTarget = false;

            _money = UiFactory.CreateText("Money", _panel, "$0", 64, TextAnchor.MiddleCenter, theme.AccentColor);
            UiFactory.Stretch((RectTransform)_money.transform);

            _boost = UiFactory.CreateText("Boost", transform, "", 34, TextAnchor.MiddleCenter, theme.PositiveColor);
            UiFactory.Place((RectTransform)_boost.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -165f), new Vector2(440f, 50f));
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            MoneyManager money = MoneyManager.Instance;
            if (money == null) return;

            if (money.Money != _shown)
            {
                if (_shown >= 0 && money.Money > _shown) _pulse = 1f;
                _shown = money.Money;
                _money.text = NumberFormat.Money(_shown);
            }

            _pulse = Mathf.MoveTowards(_pulse, 0f, Time.unscaledDeltaTime * 4f);
            _panel.localScale = Vector3.one * (1f + 0.12f * _pulse);

            _boost.text = money.BoostTimeLeft > 0f ? $"BANI x2 · {Mathf.CeilToInt(money.BoostTimeLeft)}s" : "";
        }
    }
}
