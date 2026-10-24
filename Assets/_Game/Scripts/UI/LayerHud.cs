using UnityEngine;
using UnityEngine.UI;

namespace QuarryEmpire
{
    /// <summary>Stratul curent, adâncimea și câte blocuri au mai rămas (stânga sus).</summary>
    public class LayerHud : MonoBehaviour
    {
        private Text _text;
        private Image _fill;
        private float _timer;

        private void Awake()
        {
            UiTheme theme = UiRoot.Theme;
            Image panel = UiFactory.CreatePanel("LayerPanel", transform, theme.PanelColor);
            panel.raycastTarget = false;
            var rect = UiFactory.Place((RectTransform)panel.transform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(30f, -50f), new Vector2(300f, 110f));

            _text = UiFactory.CreateText("Text", rect, "", 30);
            var textRect = UiFactory.Stretch((RectTransform)_text.transform, 6f);
            textRect.offsetMin = new Vector2(6f, 22f);

            _fill = UiFactory.CreateFill("Progress", rect, theme.AccentColor);
            var fillRect = (RectTransform)_fill.transform;
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(1f, 0f);
            fillRect.pivot = new Vector2(0.5f, 0f);
            fillRect.offsetMin = new Vector2(14f, 10f);
            fillRect.offsetMax = new Vector2(-14f, 22f);
        }

        private void Update()
        {
            _timer -= Time.unscaledDeltaTime;
            if (_timer > 0f) return;
            _timer = 0.25f;

            LayerGenerator layers = LayerGenerator.Instance;
            if (layers == null || layers.CurrentLayer == null) return;
            _text.text = $"Strat {layers.CurrentLayerIndex + 1} · {layers.DepthMeters} m\nBlocuri: {layers.RemainingBlocks}/{layers.TotalBlocks}";
            _fill.fillAmount = layers.TotalBlocks > 0 ? 1f - (float)layers.RemainingBlocks / layers.TotalBlocks : 0f;
        }
    }
}
