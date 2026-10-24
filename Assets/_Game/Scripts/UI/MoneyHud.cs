using UnityEngine;
using UnityEngine.UI;

namespace QuarryEmpire
{
    /// <summary>Banii (sus, centru): „pastilă” cu monedă, puls la câștig și indicatorul x2.</summary>
    public class MoneyHud : MonoBehaviour
    {
        public static MoneyHud Instance { get; private set; }

        private UiLabel _money;
        private UiLabel _boost;
        private GameObject _boostChip;
        private RectTransform _panel;
        private RectTransform _coin;
        private PunchScale _punch;
        private long _shown = -1;

        /// <summary>Ținta spre care zboară monedele din UI.</summary>
        public RectTransform CoinTarget => _coin != null ? _coin : _panel;

        private void Awake()
        {
            Instance = this;
            UiTheme theme = UiRoot.Theme;

            Image panel = UiFactory.CreatePanel("MoneyPill", transform, theme.PanelColor);
            _panel = UiFactory.Place((RectTransform)panel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -24f), new Vector2(400f, 104f));
            _punch = panel.gameObject.AddComponent<PunchScale>();

            Image coin = UiFactory.CreateImage("Coin", _panel, ProceduralSprites.Coin, Color.white);
            _coin = UiFactory.Place((RectTransform)coin.transform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(52f, 0f), new Vector2(84f, 84f));

            _money = UiFactory.CreateText("Amount", _panel, "0", 64, TextAnchor.MiddleCenter, theme.TextColor);
            RectTransform moneyRect = UiFactory.Stretch(_money.RectTransform);
            moneyRect.offsetMin = new Vector2(96f, 0f);
            moneyRect.offsetMax = new Vector2(-20f, 0f);

            Image chip = UiFactory.CreatePanel("BoostChip", transform, theme.PositiveColor);
            UiFactory.Place((RectTransform)chip.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -284f), new Vector2(300f, 60f));
            _boost = UiFactory.CreateText("Text", chip.transform, "", 34);
            UiFactory.Stretch(_boost.RectTransform);
            _boostChip = chip.gameObject;
            _boostChip.SetActive(false);
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
                if (_shown >= 0 && money.Money > _shown) _punch.Punch();
                _shown = money.Money;
                _money.Text = NumberFormat.Short(_shown);
            }

            bool boosted = money.BoostTimeLeft > 0f;
            if (_boostChip.activeSelf != boosted) _boostChip.SetActive(boosted);
            if (boosted) _boost.Text = Loc.F("BANI x2 · {0}s", Mathf.CeilToInt(money.BoostTimeLeft));
        }
    }
}
