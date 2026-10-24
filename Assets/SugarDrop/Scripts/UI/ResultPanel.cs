using System;
using QuarryEmpire;
using UnityEngine;
using UnityEngine.UI;

namespace SugarDrop
{
    /// <summary>
    /// Fereastra de final de nivel. Victorie: stelele apar pe rând (cu sunet), monedele câștigate și „Continuă”.
    /// Înfrângere: câte bomboane ai strâns, „+5 mutări” cu reclamă, „Reîncearcă” și „Harta”.
    /// </summary>
    public class ResultPanel : MonoBehaviour
    {
        private GameObject _root;
        private UiLabel _title;
        private UiLabel _subtitle;
        private UiLabel _coins;
        private readonly Image[] _stars = new Image[3];
        private readonly Button[] _buttons = new Button[3];
        private readonly UiLabel[] _buttonLabels = new UiLabel[3];
        private readonly Image[] _buttonFaces = new Image[3];
        private readonly Action[] _actions = new Action[3];
        private int _starCount;
        private float _starTime = -1f;

        private void Awake()
        {
            UiTheme theme = UiRoot.Theme;
            _root = SugarUi.Dim(transform).gameObject;
            RectTransform box = SugarUi.Box(_root.transform, new Vector2(820f, 1000f));

            for (int i = 0; i < 3; i++)
            {
                _stars[i] = UiFactory.CreateImage("Star" + i, box, ProceduralSprites.Star, Color.white);
                float x = (i - 1) * 180f;
                float y = i == 1 ? 50f : 0f;
                float size = i == 1 ? 200f : 160f;
                UiFactory.Place(_stars[i].rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(x, y), new Vector2(size, size));
            }

            _title = SugarUi.Outlined("Title", box, "", 92, theme.AccentColor, 4f);
            UiFactory.Place(_title.RectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(760f, 130f));
            _subtitle = UiFactory.CreateText("Subtitle", box, "", 42, TextAnchor.MiddleCenter, Color.Lerp(theme.TextDarkColor, Color.white, 0.25f));
            UiFactory.Place(_subtitle.RectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -280f), new Vector2(740f, 130f));

            // Monedele câștigate.
            _coins = UiFactory.CreateText("Coins", box, "", 50, TextAnchor.MiddleCenter, theme.TextDarkColor);
            UiFactory.Place(_coins.RectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(40f, -420f), new Vector2(500f, 80f));
            Image coin = UiFactory.CreateImage("CoinIcon", _coins.transform, ProceduralSprites.Coin, Color.white);
            UiFactory.Place(coin.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(10f, 0f), new Vector2(70f, 70f));

            for (int i = 0; i < 3; i++)
            {
                int index = i;
                _buttons[i] = UiFactory.CreateButton("Action" + i, box, "", theme.PositiveColor, () =>
                {
                    SugarUi.Click();
                    _actions[index]?.Invoke();
                }, i == 0 ? 52 : 42);
                UiFactory.Place((RectTransform)_buttons[i].transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(0f, 60f + (2 - i) * 150f), new Vector2(600f, i == 0 ? 140f : 120f));
                _buttonFaces[i] = _buttons[i].transform.Find("Face").GetComponent<Image>();
                _buttonLabels[i] = UiFactory.GetButtonLabel(_buttons[i]);
            }

            Hide();
        }

        public void ShowWin(int level, int stars, int coins, Action onNext)
        {
            UiTheme theme = UiRoot.Theme;
            for (int i = 0; i < 3; i++)
            {
                _stars[i].gameObject.SetActive(true);
                _stars[i].color = new Color(0.86f, 0.82f, 0.82f);
                _stars[i].rectTransform.localScale = Vector3.one;
            }
            _starCount = stars;
            _starTime = 0f;
            _title.Text = SugarLoc.T("BRAVO!");
            _title.Color = theme.AccentColor;
            _subtitle.Text = SugarLoc.F("Nivelul {0} terminat", level);
            _coins.gameObject.SetActive(true);
            _coins.Text = SugarLoc.F("+{0} monede", coins);
            SetButtons(new DialogButton(SugarLoc.T("CONTINUĂ"), theme.PositiveColor, onNext));
            _root.SetActive(true);
        }

        /// <param name="onAdMoves">Null dacă nu se mai poate continua cu reclamă.</param>
        public void ShowLose(int collected, int goal, Action onRetry, Action onAdMoves, Action onMap)
        {
            UiTheme theme = UiRoot.Theme;
            foreach (Image star in _stars) star.gameObject.SetActive(false);
            _starTime = -1f;
            _title.Text = SugarLoc.T("AULEU!");
            _title.Color = theme.NegativeColor;
            _subtitle.Text = SugarLoc.F("Ai strâns {0} din {1} bomboane.", collected, goal);
            _coins.gameObject.SetActive(false);
            if (onAdMoves != null)
                SetButtons(new DialogButton(SugarLoc.T("+5 MUTĂRI (reclamă)"), theme.AdColor, onAdMoves),
                    new DialogButton(SugarLoc.T("REÎNCEARCĂ"), theme.AccentColor, onRetry),
                    new DialogButton(SugarLoc.T("HARTA"), theme.PanelColor, onMap));
            else
                SetButtons(new DialogButton(SugarLoc.T("REÎNCEARCĂ"), theme.AccentColor, onRetry),
                    new DialogButton(SugarLoc.T("HARTA"), theme.PanelColor, onMap));
            _root.SetActive(true);
        }

        public void Hide()
        {
            _root.SetActive(false);
            _starTime = -1f;
        }

        public bool IsOpen => _root.activeSelf;

        private void SetButtons(params DialogButton[] buttons)
        {
            for (int i = 0; i < 3; i++)
            {
                bool used = i < buttons.Length;
                _buttons[i].gameObject.SetActive(used);
                _actions[i] = used ? buttons[i].Action : null;
                if (!used) continue;
                _buttonLabels[i].Text = buttons[i].Label;
                _buttonFaces[i].color = buttons[i].Color;
                _buttons[i].transform.Find("Shadow").GetComponent<Image>().color = UiFactory.Darken(buttons[i].Color, 0.35f);
            }
        }

        private void Update()
        {
            if (_starTime < 0f) return;
            // Stelele câștigate „pocnesc” pe rând.
            float previous = _starTime;
            _starTime += Time.unscaledDeltaTime;
            for (int i = 0; i < _starCount; i++)
            {
                float start = 0.35f + i * 0.3f;
                if (previous < start && _starTime >= start)
                {
                    _stars[i].color = UiRoot.Theme.AccentColor;
                    if (SugarAudio.Instance != null) SugarAudio.Instance.Play(SugarSound.Star, 1f, 1f + i * 0.12f);
                    SugarAudio.Buzz(0);
                }
                float t = Mathf.Clamp01((_starTime - start) / 0.3f);
                if (_starTime >= start) _stars[i].rectTransform.localScale = Vector3.one * (1f + Mathf.Sin(t * Mathf.PI) * 0.35f);
            }
            if (_starTime > 0.35f + _starCount * 0.3f + 0.3f) _starTime = -1f;
        }
    }
}
