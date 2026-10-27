using System;
using System.Collections.Generic;
using QuarryEmpire;
using UnityEngine;
using UnityEngine.UI;

namespace SugarDrop
{
    /// <summary>
    /// Fereastra de final de nivel, în stilul Drop Quest: trei stele deasupra cutiei, o panglică de lemn cu titlul,
    /// scorul, recompensa într-o plăcuță crem, un buton mare și un rând de butoane rotunde (acasă / reia / harta).
    /// Victorie: stelele apar pe rând (cu sunet), monedele se numără, „Nivelul următor”.
    /// Înfrângere: ce a mai lipsit, „+5 mutări” cu reclamă, „Reîncearcă”, acasă și harta.
    /// </summary>
    public class ResultPanel : MonoBehaviour
    {
        private const float Width = 820f;
        private const float WinHeight = 1000f;
        private const float LoseHeight = 960f;

        private GameObject _root;
        private RectTransform _box;
        private Image _ribbon;
        private UiLabel _title;
        private UiLabel _subtitle;
        private RectTransform _scoreArea;
        private UiLabel _scoreCaption;
        private UiLabel _score;
        private RectTransform _reward;
        private UiLabel _coins;
        private readonly Image[] _stars = new Image[3];
        private readonly Button[] _buttons = new Button[2];
        private readonly UiLabel[] _buttonLabels = new UiLabel[2];
        private readonly Image[] _buttonFaces = new Image[2];
        private readonly Action[] _actions = new Action[2];
        /// <summary>Butoanele rotunde: acasă, reia, harta.</summary>
        private readonly Button[] _round = new Button[3];
        private readonly Action[] _roundActions = new Action[3];
        private LevelCompleteAnimator _animator;
        /// <summary>Ce i-a mai lipsit jucătorului (iconița obiectivului și câte au rămas), pe ecranul de pierdere.</summary>
        private readonly Image[] _missingIcons = new Image[3];
        private readonly UiLabel[] _missingCounts = new UiLabel[3];
        private float _lockedUntil;

        private void Awake()
        {
            UiTheme theme = UiRoot.Theme;
            _root = SugarUi.Dim(transform).gameObject;
            _box = SugarUi.Box(_root.transform, new Vector2(Width, WinHeight));
            var holder = (RectTransform)_box.parent;

            // Stelele stau pe marginea de sus a cutiei, cea din mijloc mai sus și mai mare.
            for (int i = 0; i < 3; i++)
            {
                _stars[i] = UiFactory.CreateImage("Star" + i, _box, ProceduralSprites.Star, Color.white);
                float x = (i - 1) * 190f;
                float y = i == 1 ? 70f : 20f;
                float size = i == 1 ? 210f : 170f;
                UiFactory.Place(_stars[i].rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(x, y), new Vector2(size, size));
            }

            // Panglica de lemn, mai lată decât cutia, cu muchia închisă dedesubt.
            Image ribbonEdge = UiFactory.CreatePanel("RibbonEdge", _box, QuestStyle.WoodDeep);
            UiFactory.Place((RectTransform)ribbonEdge.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -112f), new Vector2(Width + 70f, 124f));
            _ribbon = UiFactory.CreatePanel("Ribbon", ribbonEdge.transform, QuestStyle.Wood);
            UiFactory.Stretch((RectTransform)_ribbon.transform);
            ((RectTransform)_ribbon.transform).offsetMin = new Vector2(0f, 10f);
            _title = SugarUi.Outlined("Title", _ribbon.transform, "", 64, Color.white, 4f);
            UiFactory.Stretch(_title.RectTransform);

            _subtitle = UiFactory.CreateText("Subtitle", _box, "", 38, TextAnchor.MiddleCenter, QuestStyle.TextSoft);
            UiFactory.Place(_subtitle.RectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -250f), new Vector2(740f, 110f));

            // Scorul: cuvântul mic deasupra, cifra mare dedesubt.
            _scoreArea = UiFactory.CreateRect("Score", _box);
            UiFactory.Place(_scoreArea, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -340f), new Vector2(600f, 140f));
            _scoreCaption = UiFactory.CreateText("Caption", _scoreArea, "", 34, TextAnchor.MiddleCenter, QuestStyle.TextSoft);
            UiFactory.Place(_scoreCaption.RectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(600f, 46f));
            _score = UiFactory.CreateText("Value", _scoreArea, "", 76, TextAnchor.MiddleCenter, QuestStyle.Text);
            UiFactory.Place(_score.RectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -44f), new Vector2(600f, 92f));

            // Recompensa: plăcuța crem cu moneda și câte s-au câștigat.
            _reward = SugarUi.Card("Reward", _box, out Image rewardFace, QuestStyle.SurfaceTint);
            UiFactory.Place(_reward, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -506f), new Vector2(300f, 130f));
            Image coin = UiFactory.CreateImage("CoinIcon", rewardFace.transform, ProceduralSprites.Coin, Color.white);
            UiFactory.Place(coin.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(26f, 0f), new Vector2(82f, 82f));
            _coins = UiFactory.CreateText("Coins", rewardFace.transform, "", 50, TextAnchor.MiddleCenter, QuestStyle.Text);
            UiFactory.Place(_coins.RectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-16f, 0f), new Vector2(170f, 80f));

            for (int i = 0; i < 3; i++)
            {
                _missingIcons[i] = UiFactory.CreateImage("Missing" + i, _box, ProceduralSprites.Star, Color.white);
                UiFactory.Place(_missingIcons[i].rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -420f), new Vector2(96f, 96f));
                _missingIcons[i].preserveAspect = true;
                _missingCounts[i] = SugarUi.Outlined("Count", _missingIcons[i].transform, "", 40, Color.white, 3f);
                UiFactory.Place(_missingCounts[i].RectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, -8f), new Vector2(170f, 56f));
                _missingIcons[i].gameObject.SetActive(false);
            }

            for (int i = 0; i < 2; i++)
            {
                int index = i;
                _buttons[i] = SugarUi.Button("Action" + i, _box, "", theme.PositiveColor, () => Press(_actions[index]), 46);
                _buttonFaces[i] = _buttons[i].transform.Find("Face").GetComponent<Image>();
                _buttonLabels[i] = UiFactory.GetButtonLabel(_buttons[i]);
            }

            (Sprite icon, Color color)[] round =
            {
                (SugarSprites.Home, QuestStyle.Wood),
                (ProceduralSprites.Restart, QuestStyle.Blue),
                (SugarSprites.MapPin, QuestStyle.Wood),
            };
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                _round[i] = SugarUi.IconButton("Round" + i, _box, round[i].icon, round[i].color, () => Press(_roundActions[index]), 0.5f);
            }

            UIParticleController particles = UiFactory.CreateRect("Sparkles", _box).gameObject.AddComponent<UIParticleController>();
            UiFactory.Stretch((RectTransform)particles.transform);
            particles.Initialize(40, SugarSprites.Sparkle, null, 0f);
            _animator = holder.gameObject.AddComponent<LevelCompleteAnimator>();
            _animator.Bind(holder, _stars, _coins, particles, QuestStyle.Gold, new Color(0.86f, 0.8f, 0.7f));

            Hide();
        }

        /// <summary>Un singur buton contează pe fereastră: apăsările rapide repetate sunt ignorate.</summary>
        private void Press(Action action)
        {
            if (Time.unscaledTime < _lockedUntil) return;
            _lockedUntil = Time.unscaledTime + 0.6f;
            SugarUi.Click();
            action?.Invoke();
        }

        /// <param name="onHome">Null = fără butonul „acasă” (la fel pentru reia și harta).</param>
        public void ShowWin(int level, int stars, int score, int coins, Action onNext, Action onHome, Action onReplay, Action onMap)
        {
            UiTheme theme = UiRoot.Theme;
            SugarUi.ResizeBox(_box, new Vector2(Width, WinHeight));
            foreach (Image star in _stars) star.gameObject.SetActive(true);
            _title.Text = SugarLoc.T("NIVEL COMPLET!");
            _subtitle.Text = SugarLoc.F("NIVELUL {0}", level);
            _scoreArea.gameObject.SetActive(true);
            _scoreCaption.Text = SugarLoc.T("Scor");
            _score.Text = score.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
            _reward.gameObject.SetActive(true);
            ShowMissing(null);
            SetButtons(new DialogButton(SugarLoc.T("NIVELUL URMĂTOR"), theme.PositiveColor, onNext));
            SetRound(onHome, onReplay, onMap);
            _lockedUntil = Time.unscaledTime + 0.35f; // nu „prinde” atingerea care a încheiat nivelul
            _root.SetActive(true);
            _animator.PlayWin(stars, coins, "+{0}");
        }

        /// <param name="onAdMoves">Null dacă nu se mai poate continua cu reclamă.</param>
        /// <param name="missing">Obiectivele neterminate: iconița, culoarea și câte mai trebuiau.</param>
        public void ShowLose(string message, Action onRetry, Action onAdMoves, Action onMap, Action onHome,
            IReadOnlyList<(Sprite icon, Color color, int left)> missing = null)
        {
            UiTheme theme = UiRoot.Theme;
            SugarUi.ResizeBox(_box, new Vector2(Width, LoseHeight));
            ShowMissing(missing);
            foreach (Image star in _stars) star.gameObject.SetActive(false);
            _title.Text = SugarLoc.T("NIVEL EȘUAT");
            _subtitle.Text = message;
            _scoreArea.gameObject.SetActive(false);
            _reward.gameObject.SetActive(false);
            if (onAdMoves != null)
                SetButtons(new DialogButton(SugarLoc.T("+5 MUTĂRI (reclamă)"), theme.AdColor, onAdMoves),
                    new DialogButton(SugarLoc.T("REÎNCEARCĂ"), theme.PositiveColor, onRetry));
            else
                SetButtons(new DialogButton(SugarLoc.T("REÎNCEARCĂ"), theme.PositiveColor, onRetry));
            SetRound(onHome, null, onMap);
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
                _missingIcons[i].rectTransform.anchoredPosition = new Vector2((i - (count - 1) * 0.5f) * 190f, -420f);
                _missingCounts[i].Text = missing[i].left.ToString();
            }
        }

        public void Hide()
        {
            _root.SetActive(false);
            if (_animator != null) _animator.Stop();
        }

        public bool IsOpen => _root.activeSelf;

        /// <summary>Butoanele mari, de jos în sus: primul (cel principal) stă cel mai aproape de rândul rotund.</summary>
        private void SetButtons(params DialogButton[] buttons)
        {
            for (int i = 0; i < _buttons.Length; i++)
            {
                bool used = i < buttons.Length;
                _buttons[i].gameObject.SetActive(used);
                _actions[i] = used ? buttons[i].Action : null;
                if (!used) continue;
                _buttonLabels[i].Text = buttons[i].Label;
                _buttonFaces[i].color = buttons[i].Color;
                _buttons[i].transform.Find("Shadow").GetComponent<Image>().color = UiFactory.Darken(buttons[i].Color, 0.35f);
                int fromBottom = buttons.Length - 1 - i;
                UiFactory.Place((RectTransform)_buttons[i].transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(0f, 200f + fromBottom * 150f), new Vector2(640f, i == buttons.Length - 1 ? 136f : 124f));
            }
        }

        /// <summary>Rândul de butoane rotunde de jos; cele fără acțiune se ascund, restul se centrează.</summary>
        private void SetRound(params Action[] actions)
        {
            int visible = 0;
            foreach (Action action in actions)
                if (action != null) visible++;
            int index = 0;
            for (int i = 0; i < _round.Length; i++)
            {
                _roundActions[i] = actions[i];
                bool used = actions[i] != null;
                _round[i].gameObject.SetActive(used);
                if (!used) continue;
                UiFactory.Place((RectTransform)_round[i].transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2((index - (visible - 1) * 0.5f) * 170f, 50f), new Vector2(124f, 124f));
                index++;
            }
        }
    }
}
