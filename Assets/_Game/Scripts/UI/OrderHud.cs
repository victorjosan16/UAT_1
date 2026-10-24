using UnityEngine;
using UnityEngine.UI;

namespace QuarryEmpire
{
    /// <summary>Comanda activă (sub bani): ce trebuie livrat, cât timp a rămas și recompensa.</summary>
    public class OrderHud : MonoBehaviour
    {
        private GameObject _panel;
        private Text _text;
        private Image _fill;
        private float _timer;

        private void Awake()
        {
            UiTheme theme = UiRoot.Theme;
            Image panel = UiFactory.CreatePanel("OrderPanel", transform, theme.PanelColor);
            panel.raycastTarget = false;
            _panel = panel.gameObject;
            var rect = UiFactory.Place((RectTransform)panel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -215f), new Vector2(640f, 120f));

            _text = UiFactory.CreateText("Text", rect, "", 32);
            UiFactory.Stretch((RectTransform)_text.transform, 6f).offsetMin = new Vector2(6f, 24f);

            _fill = UiFactory.CreateFill("Time", rect, theme.PositiveColor);
            var fillRect = (RectTransform)_fill.transform;
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(1f, 0f);
            fillRect.pivot = new Vector2(0.5f, 0f);
            fillRect.offsetMin = new Vector2(14f, 10f);
            fillRect.offsetMax = new Vector2(-14f, 24f);
            _panel.SetActive(false);
        }

        private void Update()
        {
            _timer -= Time.unscaledDeltaTime;
            if (_timer > 0f) return;
            _timer = 0.2f;

            OrderSystem orders = OrderSystem.Instance;
            bool active = orders != null && orders.HasOrder;
            if (_panel.activeSelf != active) _panel.SetActive(active);
            if (!active) return;

            OrderSystem.ActiveOrder order = orders.Current;
            _text.text = $"COMANDĂ: {order.Delivered}/{order.Required} × {order.Resource.DisplayName}\n" +
                         $"{Mathf.CeilToInt(order.TimeLeft)}s · Bonus {NumberFormat.Money(order.Reward)} · du-le la camionul albastru";
            _fill.fillAmount = order.TimeLimit > 0f ? order.TimeLeft / order.TimeLimit : 0f;
        }
    }
}
