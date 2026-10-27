using System;
using QuarryEmpire;
using UnityEngine;
using UnityEngine.UI;

namespace SugarDrop
{
    public enum Booster { Bomb, ExtraMoves, Shuffle }

    /// <summary>
    /// Ajutoarele, într-o tavă crem jos (stilul Drop Quest): bomba (explozie 3×3), +5 mutări și amestecarea.
    /// Fiecare are o bulină cu numărul rămas; la 0 apare „+” (se cumpără cu monede sau cu o reclamă).
    /// </summary>
    public class BoosterBar : MonoBehaviour
    {
        public event Action<Booster> Clicked;

        private readonly UiLabel[] _counts = new UiLabel[3];
        private readonly Image[] _badges = new Image[3];
        private readonly RectTransform[] _buttons = new RectTransform[3];
        private UiLabel _hint;
        private bool _targeting;

        private void Awake()
        {
            UiTheme theme = UiRoot.Theme;
            Sprite[] icons = { SugarSprites.Bomb, null, SugarSprites.ShuffleIcon };
            Color[] colors = { QuestStyle.Blue, QuestStyle.Green, QuestStyle.Wood };
            RectTransform tray = SugarUi.Card("Tray", transform, out _);
            UiFactory.Place(tray, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-60f, 18f), new Vector2(560f, 176f));
            for (int i = 0; i < 3; i++)
            {
                var booster = (Booster)i;
                Button button = SugarUi.IconButton(booster.ToString(), transform, icons[i] != null ? icons[i] : UiFactory.WhiteSprite, colors[i],
                    () => Clicked?.Invoke(booster), i == 0 ? 0.7f : 0.55f);
                var rect = (RectTransform)button.transform;
                UiFactory.Place(rect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(-60f + (i - 1) * 170f, 108f), new Vector2(136f, 136f));
                _buttons[i] = rect;
                if (i == 0) button.transform.Find("Face/Icon").GetComponent<Image>().color = Color.white;
                if (i == 1)
                {
                    // „+5” scris în loc de iconiță.
                    button.transform.Find("Face/Icon").gameObject.SetActive(false);
                    UiLabel plus = SugarUi.Outlined("Plus5", button.transform.Find("Face"), "+5", 56, Color.white);
                    UiFactory.Stretch(plus.RectTransform);
                }

                _badges[i] = UiFactory.CreateImage("Badge", rect, theme.Circle, theme.NegativeColor);
                UiFactory.Place(_badges[i].rectTransform, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-14f, -14f), new Vector2(56f, 56f));
                _counts[i] = UiFactory.CreateText("Count", _badges[i].transform, "0", 32);
                UiFactory.Stretch(_counts[i].RectTransform);
            }

            _hint = SugarUi.Outlined("BombHint", transform, "", 34, Color.white);
            UiFactory.Place(_hint.RectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 210f), new Vector2(760f, 90f));
            _hint.gameObject.SetActive(false);
        }

        /// <summary>Butonul din mijloc (+5 mutări), spre care arată tutorialul.</summary>
        public RectTransform MiddleButton => _buttons[1];

        public void SetCounts(int bombs, int moves, int shuffles)
        {
            int[] values = { bombs, moves, shuffles };
            for (int i = 0; i < 3; i++)
            {
                _counts[i].Text = values[i] > 0 ? values[i].ToString() : "+";
                _badges[i].color = values[i] > 0 ? UiRoot.Theme.NegativeColor : UiRoot.Theme.PositiveColor;
            }
        }

        /// <summary>Modul „alege unde cade bomba”: butonul pulsează și apare un text de ajutor.</summary>
        public void SetTargeting(bool targeting)
        {
            _targeting = targeting;
            _hint.gameObject.SetActive(targeting);
            if (targeting) _hint.Text = SugarLoc.T("Atinge o piesă pentru bombă");
            if (!targeting) _buttons[0].localScale = Vector3.one;
        }

        private void Update()
        {
            if (!_targeting) return;
            _buttons[0].localScale = Vector3.one * (1f + Mathf.Sin(Time.unscaledTime * 8f) * 0.08f);
            _hint.RectTransform.localScale = Vector3.one * (1f + Mathf.Sin(Time.unscaledTime * 4f) * 0.03f);
        }
    }
}
