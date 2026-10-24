using UnityEngine;
using UnityEngine.UI;

namespace QuarryEmpire
{
    /// <summary>
    /// Nivelul (stratul) în insigna din stânga sus și progresul stratului în bara de sub bani.
    /// </summary>
    public class LayerHud : MonoBehaviour
    {
        private UiLabel _level;
        private UiLabel _depth;
        private UiLabel _progressText;
        private UiProgressBar _progress;
        private PunchScale _badgePunch;
        private int _shownLayer = -1;
        private float _timer;

        private void Awake()
        {
            UiTheme theme = UiRoot.Theme;

            // Insigna rotundă: inel auriu + număr.
            RectTransform badge = UiFactory.CreateRect("LevelBadge", transform);
            UiFactory.Place(badge, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -16f), new Vector2(124f, 124f));
            Image disc = UiFactory.CreateImage("Disc", badge, theme.Circle, theme.PanelColor);
            UiFactory.Stretch((RectTransform)disc.transform);
            Image ring = UiFactory.CreateImage("Ring", badge, ProceduralSprites.Ring, theme.AccentColor);
            UiFactory.Stretch((RectTransform)ring.transform);
            UiLabel caption = UiFactory.CreateText("Caption", badge, "STRAT", 22, TextAnchor.MiddleCenter, theme.AccentColor);
            UiFactory.Place(caption.RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 26f), new Vector2(110f, 30f));
            _level = UiFactory.CreateText("Level", badge, "1", 56);
            UiFactory.Place(_level.RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(110f, 70f));
            _badgePunch = badge.gameObject.AddComponent<PunchScale>();

            _depth = UiFactory.CreateText("Depth", transform, "", 26, TextAnchor.MiddleCenter, theme.TextColor);
            UiFactory.Place(_depth.RectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(86f, -144f), new Vector2(160f, 36f));

            // Bara de progres a stratului, sub bani.
            _progress = UiFactory.CreateProgressBar("LayerProgress", transform, theme.AccentColor);
            UiFactory.Place((RectTransform)_progress.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -142f), new Vector2(560f, 40f));
            _progressText = UiFactory.CreateText("Text", _progress.transform, "", 24, TextAnchor.MiddleCenter, theme.TextColor);
            UiFactory.Stretch(_progressText.RectTransform);
        }

        private void Update()
        {
            _timer -= Time.unscaledDeltaTime;
            if (_timer > 0f) return;
            _timer = 0.2f;

            LayerGenerator layers = LayerGenerator.Instance;
            if (layers == null || layers.CurrentLayer == null) return;

            if (layers.CurrentLayerIndex != _shownLayer)
            {
                if (_shownLayer >= 0) _badgePunch.Punch(2f);
                _shownLayer = layers.CurrentLayerIndex;
                _level.Text = (_shownLayer + 1).ToString();
            }
            _depth.Text = $"{layers.DepthMeters} m";
            int mined = layers.TotalBlocks - layers.RemainingBlocks;
            _progressText.Text = $"{mined}/{layers.TotalBlocks} blocuri";
            _progress.SetValue(layers.TotalBlocks > 0 ? (float)mined / layers.TotalBlocks : 0f);
        }
    }
}
