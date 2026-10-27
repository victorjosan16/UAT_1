using System;
using System.Collections.Generic;
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
        private LevelCompleteAnimator _animator;
        /// <summary>Ce i-a mai lipsit jucătorului (iconița obiectivului și câte au rămas), pe ecranul de pierdere.</summary>
        private readonly Image[] _missingIcons = new Image[3];
        private readonly UiLabel[] _missingCounts = new UiLabel[3];
        private float _lockedUntil;

        private void Awake()
        {
            UiTheme theme = UiRoot.Theme;
            _root = SugarUi.Dim(transform).gameObject;
            RectTransform box = SugarUi.Box(_root.transform, new Vector2(820f, 1000f));
            var holder = (RectTransform)box.parent;

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
                _missingIcons[i] = UiFactory.CreateImage("Missing" + i, box, ProceduralSprites.Star, Color.white);
                UiFactory.Place(_missingIcons[i].rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -440f), new Vector2(96f, 96f));
                _missingIcons[i].preserveAspect = true;
                _missingCounts[i] = SugarUi.Outlined("Count", _missingIcons[i].transform, "", 40, Color.white, 3f);
                UiFactory.Place(_missingCounts[i].RectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, -8f), new Vector2(170f, 56f));
                _missingIcons[i].gameObject.SetActive(false);
            }

            for (int i = 0; i < 3; i++)
            {
                int index = i;
                _buttons[i] = SugarUi.Button("Action" + i, box, "", theme.PositiveColor, () =>
                {
                    // Un singur buton contează pe fereastră: apăsările rapide repetate sunt ignorate.
                    if (Time.unscaledTime < _lockedUntil) return;
                    _lockedUntil = Time.unscaledTime + 0.6f;
                    SugarUi.Click();
                    _actions[index]?.Invoke();
                }, i == 0 ? 52 : 42);
                UiFactory.Place((RectTransform)_buttons[i].transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(0f, 60f + (2 - i) * 150f), new Vector2(600f, i == 0 ? 140f : 120f));
                _buttonFaces[i] = _buttons[i].transform.Find("Face").GetComponent<Image>();
                _buttonLabels[i] = UiFactory.GetButtonLabel(_buttons[i]);
            }

            UIParticleController particles = UiFactory.CreateRect("Sparkles", box).gameObject.AddComponent<UIParticleController>();
            UiFactory.Stretch((RectTransform)particles.transform);
            particles.Initialize(40, SugarSprites.Sparkle, null, 0f);
            _animator = holder.gameObject.AddComponent<LevelCompleteAnimator>();
            _animator.Bind(holder, _stars, _coins, particles, theme.AccentColor, new Color(0.86f, 0.82f, 0.82f));

            Hide();
        }

        public void ShowWin(int level, int stars, int coins, Action onNext)
        {
            UiTheme theme = UiRoot.Theme;
            foreach (Image star in _stars) star.gameObject.SetActive(true);
            _title.Text = SugarLoc.T("BRAVO!");
            _title.Color = theme.AccentColor;
            _subtitle.Text = SugarLoc.F("Nivelul {0} terminat", level);
            _coins.gameObject.SetActive(true);
            ShowMissing(null);
            SetButtons(new DialogButton(SugarLoc.T("CONTINUĂ"), theme.PositiveColor, onNext));
            _lockedUntil = Time.unscaledTime + 0.35f; // nu „prinde” atingerea care a încheiat nivelul
            _root.SetActive(true);
            _animator.PlayWin(stars, coins, SugarLoc.T("+{0} monede"));
        }

        /// <param name="onAdMoves">Null dacă nu se mai poate continua cu reclamă.</param>
        /// <param name="missing">Obiectivele neterminate: iconița, culoarea și câte mai trebuiau.</param>
        public void ShowLose(string message, Action onRetry, Action onAdMoves, Action onMap,
            IReadOnlyList<(Sprite icon, Color color, int left)> missing = null)
        {
            ShowMissing(missing);
            UiTheme theme = UiRoot.Theme;
            foreach (Image star in _stars) star.gameObject.SetActive(false);
            _title.Text = SugarLoc.T("AULEU!");
            _title.Color = theme.NegativeColor;
            _subtitle.Text = message;
            _coins.gameObject.SetActive(false);
            if (onAdMoves != null)
                SetButtons(new DialogButton(SugarLoc.T("+5 MUTĂRI (reclamă)"), theme.AdColor, onAdMoves),
                    new DialogButton(SugarLoc.T("REÎNCEARCĂ"), theme.AccentColor, onRetry),
                    new DialogButton(SugarLoc.T("HARTA"), theme.PanelColor, onMap));
            else
                SetButtons(new DialogButton(SugarLoc.T("REÎNCEARCĂ"), theme.AccentColor, onRetry),
                    new DialogButton(SugarLoc.T("HARTA"), theme.PanelColor, onMap));
            _lockedUntil = Time.unscaledTime + 0.35f; // nu „prinde” atingerea care a încheiat nivelul
            _root.SetActive(true);
            _animator.PlayLose();
        }

        private void ShowMissing(IReadOnlyList<(Sprite icon, Color color, int left)> missing)
        {
            int count = missing == null ? 0 : Mathf.Min(3, missing.Count);
            for (int i = 0; i < 3; i++)
            {
                bool on = i < count;
                _missingIcons[i].gameObject.SetActive(on);
                if (!on) continue;
                _missingIcons[i].sprite = missing[i].icon;
                _missingIcons[i].color = missing[i].color;
                _missingIcons[i].rectTransform.anchoredPosition = new Vector2((i - (count - 1) * 0.5f) * 190f, -440f);
                _missingCounts[i].Text = missing[i].left.ToString();
            }
        }

        public void Hide()
        {
            _root.SetActive(false);
            if (_animator != null) _animator.Stop();
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
    }
}
