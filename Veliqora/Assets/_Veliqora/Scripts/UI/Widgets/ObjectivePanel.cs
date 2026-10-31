using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Veliqora.Core.Objectives;
using Veliqora.Presentation;

namespace Veliqora.UI.Widgets
{
    /// <summary>
    /// Row of objective chips shown under the gameplay HUD. Progress is derived from the board state by
    /// ObjectiveTracker, so it stays correct across undo / shuffle / restart. Newly completed chips pop.
    /// </summary>
    public sealed class ObjectivePanel : MonoBehaviour
    {
        private sealed class Chip
        {
            public RectTransform Rect;
            public Image Bg;
            public Image Icon;
            public Text Label;
            public Text Reward;
        }

        private readonly List<Chip> _chips = new List<Chip>();
        private readonly HashSet<string> _complete = new HashSet<string>();
        private bool _initialised;
        private Color _ink = Color.white;

        public RectTransform Rect => (RectTransform)transform;

        public static ObjectivePanel Create(RectTransform parent, int count, Color ink)
        {
            var rt = UIFactory.Rect("Objectives", parent);
            var p = rt.gameObject.AddComponent<ObjectivePanel>();
            p._ink = ink;
            float w = Mathf.Min(320f, 980f / Mathf.Max(1, count));
            for (int i = 0; i < count; i++)
            {
                var chip = new Chip();
                chip.Bg = UIFactory.Image(rt, "Objective" + i, SpriteLibrary.Rounded(26), new Color(0.07f, 0.06f, 0.18f, 0.72f));
                chip.Rect = chip.Bg.rectTransform;
                float x = (i - (count - 1) * 0.5f) * (w + 12f);
                UIFactory.Place(chip.Rect, new Vector2(0.5f, 0.5f), new Vector2(w, 84), new Vector2(x, 0));
                UIFactory.AddOutline(chip.Bg, UIStyle.WithAlpha(UIStyle.ThemeAccent, 0.55f), 2f);
                chip.Icon = UIFactory.Image(chip.Rect, "Icon", SpriteLibrary.Circle, Color.white);
                UIFactory.Place(chip.Icon.rectTransform, new Vector2(0, 0.5f), new Vector2(44, 44), new Vector2(14, 0), new Vector2(0, 0.5f));
                chip.Label = UIFactory.Text(chip.Rect, "", UIStyle.FontCaption - 6, ink, TextAnchor.MiddleLeft, FontStyle.Bold);
                chip.Label.rectTransform.offsetMin = new Vector2(66, 0);
                chip.Label.rectTransform.offsetMax = new Vector2(-6, 0);
                chip.Label.resizeTextForBestFit = true;
                chip.Label.resizeTextMinSize = 16;
                chip.Label.resizeTextMaxSize = UIStyle.FontCaption - 6;
                chip.Reward = UIFactory.Text(chip.Rect, "", UIStyle.FontCaption - 10, UIStyle.Sunshine, TextAnchor.MiddleRight, FontStyle.Bold);
                UIFactory.Place(chip.Reward.rectTransform, new Vector2(1, 1), new Vector2(150, 30), new Vector2(-10, 12), new Vector2(1, 1));
                p._chips.Add(chip);
            }
            return p;
        }

        public static string Describe(ObjectiveDef d)
        {
            switch (d.Type)
            {
                case ObjectiveType.CollectColor:
                    return Loc.F("Fill {0}", Loc.T(LiquidPalette.Names[(Mathf.Max(1, d.Color) - 1) % LiquidPalette.Names.Length]));
                case ObjectiveType.CompleteBottles: return Loc.T("Complete bottles");
                case ObjectiveType.UnlockBottles: return Loc.T("Open corks");
                case ObjectiveType.RevealMystery: return Loc.T("Reveal mystery");
                default: return Loc.T("Finish the level");
            }
        }

        /// <summary>Updates every chip. Returns the objectives that became complete since the last call.</summary>
        public List<ObjectiveStatus> Refresh(List<ObjectiveStatus> list)
        {
            var newly = new List<ObjectiveStatus>();
            for (int i = 0; i < _chips.Count && i < list.Count; i++)
            {
                var st = list[i];
                var chip = _chips[i];
                var d = st.Def;
                string progress = d.Type == ObjectiveType.FinishLevel ? "" : "  " + st.Progress + "/" + d.Target;
                chip.Label.text = Describe(d) + progress;
                chip.Label.color = st.Unlocked ? _ink : UIStyle.WithAlpha(_ink, 0.45f);
                chip.Reward.text = d.Rewarded ? Loc.T("Ad bonus") : (d.RewardCoins > 0 ? "+" + d.RewardCoins : "");

                if (st.Complete)
                {
                    chip.Icon.sprite = SpriteLibrary.Get(Icon.Check);
                    chip.Icon.color = UIStyle.Mint;
                }
                else if (!st.Unlocked)
                {
                    chip.Icon.sprite = SpriteLibrary.Get(Icon.Lock);
                    chip.Icon.color = UIStyle.InkSoft;
                }
                else if (d.Type == ObjectiveType.CollectColor)
                {
                    chip.Icon.sprite = SpriteLibrary.Circle;
                    chip.Icon.color = LiquidPalette.Get(d.Color);
                }
                else
                {
                    chip.Icon.sprite = SpriteLibrary.Get(d.Type == ObjectiveType.UnlockBottles ? Icon.Cork : d.Type == ObjectiveType.CompleteBottles ? Icon.Check : d.Type == ObjectiveType.RevealMystery ? Icon.Bulb : Icon.Star);
                    chip.Icon.color = d.Type == ObjectiveType.CompleteBottles ? UIStyle.InkSoft : Color.white;
                }
                chip.Bg.color = st.Complete ? new Color(0.12f, 0.32f, 0.24f, 0.8f) : new Color(0.07f, 0.06f, 0.18f, 0.72f);

                bool was = _complete.Contains(d.Id);
                if (st.Complete && !was)
                {
                    _complete.Add(d.Id);
                    if (_initialised)
                    {
                        newly.Add(st);
                        if (!MotionPrefs.Reduced) Tween.Scale(this, chip.Rect, new Vector3(1.25f, 1.25f, 1), Vector3.one, 0.35f, Ease.OutBack);
                    }
                }
                else if (!st.Complete && was) _complete.Remove(d.Id); // undo / restart
            }
            _initialised = true;
            return newly;
        }

        /// <summary>Forget completion state (used after restart so the first refresh doesn't pop).</summary>
        public void ResetPops()
        {
            _complete.Clear();
            _initialised = false;
        }
    }
}
