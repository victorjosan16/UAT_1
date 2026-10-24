using UnityEngine;
using UnityEngine.UI;

namespace QuarryEmpire
{
    /// <summary>
    /// Misiunea compactă de sub bara de sus: comanda activă (cu timp) sau, dacă nu există,
    /// obiectivul curent („curăță stratul”).
    /// </summary>
    public class OrderHud : MonoBehaviour
    {
        private UiLabel _title;
        private UiLabel _detail;
        private Image _icon;
        private UiProgressBar _time;
        private PunchScale _punch;
        private bool _hadOrder;
        private float _timer;

        private void Awake()
        {
            UiTheme theme = UiRoot.Theme;
            Image card = UiFactory.CreatePanel("MissionCard", transform, theme.PanelLightColor);
            var rect = UiFactory.Place((RectTransform)card.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -196f), new Vector2(760f, 128f));
            _punch = card.gameObject.AddComponent<PunchScale>();

            Image iconBack = UiFactory.CreateImage("IconBack", rect, theme.Circle, theme.AccentColor);
            UiFactory.Place((RectTransform)iconBack.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(16f, 4f), new Vector2(92f, 92f));
            _icon = UiFactory.CreateImage("Icon", iconBack.transform, ProceduralSprites.Crate, Color.white);
            UiFactory.Stretch((RectTransform)_icon.transform, 20f);

            _title = UiFactory.CreateText("Title", rect, "", 34, TextAnchor.MiddleLeft, theme.TextDarkColor);
            UiFactory.Place(_title.RectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(124f, -10f), new Vector2(620f, 48f));
            _detail = UiFactory.CreateText("Detail", rect, "", 28, TextAnchor.MiddleLeft, Color.Lerp(theme.TextDarkColor, Color.white, 0.35f));
            UiFactory.Place(_detail.RectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(124f, -56f), new Vector2(620f, 40f));

            _time = UiFactory.CreateProgressBar("Time", rect, theme.PositiveColor);
            var timeRect = (RectTransform)_time.transform;
            timeRect.anchorMin = new Vector2(0f, 0f);
            timeRect.anchorMax = new Vector2(1f, 0f);
            timeRect.pivot = new Vector2(0.5f, 0f);
            timeRect.offsetMin = new Vector2(124f, 12f);
            timeRect.offsetMax = new Vector2(-24f, 34f);
        }

        private void Update()
        {
            _timer -= Time.unscaledDeltaTime;
            if (_timer > 0f) return;
            _timer = 0.2f;

            OrderSystem orders = OrderSystem.Instance;
            bool hasOrder = orders != null && orders.HasOrder;
            if (hasOrder != _hadOrder)
            {
                _hadOrder = hasOrder;
                _punch.Punch(1.5f);
            }
            _time.gameObject.SetActive(hasOrder);

            if (hasOrder)
            {
                OrderSystem.ActiveOrder order = orders.Current;
                _icon.sprite = ProceduralSprites.Crate;
                _title.Text = $"Livrează {order.Required} × {order.Resource.DisplayName}";
                _detail.Text = $"{order.Delivered}/{order.Required} · {Mathf.CeilToInt(order.TimeLeft)}s · bonus {NumberFormat.Money(order.Reward)}";
                _time.SetValue(order.TimeLimit > 0f ? order.TimeLeft / order.TimeLimit : 0f);
                return;
            }

            LayerGenerator layers = LayerGenerator.Instance;
            _icon.sprite = ProceduralSprites.Gem;
            _title.Text = "Curăță stratul";
            _detail.Text = layers != null ? $"Mai sunt {layers.RemainingBlocks} blocuri până coborâm" : "";
        }
    }
}
