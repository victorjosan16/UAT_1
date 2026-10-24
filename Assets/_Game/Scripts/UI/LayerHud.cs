using UnityEngine;
using UnityEngine.UI;

namespace QuarryEmpire
{
    /// <summary>
    /// Insigna stratului (stânga sus): numărul stratului, adâncimea și un inel care se umple
    /// pe măsură ce spargi blocurile din strat.
    /// </summary>
    public class LayerHud : MonoBehaviour
    {
        private UiLabel _level;
        private UiLabel _depth;
        private Image _progressRing;
        private PunchScale _badgePunch;
        private int _shownLayer = -1;
        private float _shownProgress;
        private float _timer;

        private void Awake()
        {
            UiTheme theme = UiRoot.Theme;

            RectTransform badge = UiFactory.CreateRect("LevelBadge", transform);
            UiFactory.Place(badge, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -14f), new Vector2(140f, 140f));
            Image disc = UiFactory.CreateImage("Disc", badge, theme.Circle, theme.PanelColor);
            UiFactory.Stretch((RectTransform)disc.transform, 8f);

            // Inel de fundal + inel de progres (umplere radială).
            Image track = UiFactory.CreateImage("RingTrack", badge, ProceduralSprites.Ring, new Color(1f, 1f, 1f, 0.18f));
            UiFactory.Stretch((RectTransform)track.transform);
            _progressRing = UiFactory.CreateImage("RingProgress", badge, ProceduralSprites.Ring, theme.AccentColor);
            UiFactory.Stretch((RectTransform)_progressRing.transform);
            _progressRing.type = Image.Type.Filled;
            _progressRing.fillMethod = Image.FillMethod.Radial360;
            _progressRing.fillOrigin = 2; // sus
            _progressRing.fillClockwise = true;
            _progressRing.fillAmount = 0f;

            UiLabel caption = UiFactory.CreateText("Caption", badge, Loc.T("STRAT"), 20, TextAnchor.MiddleCenter, theme.AccentColor);
            UiFactory.Place(caption.RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 34f), new Vector2(110f, 28f));
            _level = UiFactory.CreateText("Level", badge, "1", 54);
            UiFactory.Place(_level.RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 2f), new Vector2(110f, 64f));
            _depth = UiFactory.CreateText("Depth", badge, "", 22, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.8f));
            UiFactory.Place(_depth.RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -36f), new Vector2(110f, 28f));
            _badgePunch = badge.gameObject.AddComponent<PunchScale>();
        }

        private void Update()
        {
            LayerGenerator layers = LayerGenerator.Instance;
            if (layers == null || layers.CurrentLayer == null) return;

            float progress = layers.TotalBlocks > 0 ? 1f - (float)layers.RemainingBlocks / layers.TotalBlocks : 0f;
            _shownProgress = Mathf.MoveTowards(_shownProgress, progress, Time.unscaledDeltaTime * 0.8f);
            _progressRing.fillAmount = _shownProgress;

            _timer -= Time.unscaledDeltaTime;
            if (_timer > 0f) return;
            _timer = 0.2f;

            if (layers.CurrentLayerIndex != _shownLayer)
            {
                if (_shownLayer >= 0)
                {
                    _badgePunch.Punch(2f);
                    _shownProgress = 0f;
                }
                _shownLayer = layers.CurrentLayerIndex;
                _level.Text = (_shownLayer + 1).ToString();
            }
            _depth.Text = $"{layers.DepthMeters} m";
        }
    }
}
