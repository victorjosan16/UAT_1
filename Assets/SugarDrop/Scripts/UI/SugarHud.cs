using System;
using QuarryEmpire;
using UnityEngine;
using UnityEngine.UI;

namespace SugarDrop
{
    /// <summary>
    /// HUD-ul de sus: nivelul, progresul bomboanelor în borcan (x/obiectiv), mutările rămase și pauza.
    /// Tot aici apar cuvintele de combo („Delicios!”), bannerele și textul tutorialului.
    /// </summary>
    public class SugarHud : MonoBehaviour
    {
        public event Action PauseClicked;

        private UiLabel _level;
        private Image _levelPill;
        private UiLabel _moves;
        private UiLabel _movesCaption;
        private UiLabel _progressText;
        private UiProgressBar _progress;
        private PunchScale _movesPunch;
        private PunchScale _progressPunch;
        private UiLabel _tutorial;

        private UiLabel _word;
        private float _wordT = -1f;
        private UiLabel _banner;
        private float _bannerT = -1f;

        // Scorul (bară cu praguri de stele) și punctele care apar unde au dispărut capacele.
        private UiLabel _score;
        private UiProgressBar _scoreBar;
        private readonly Image[] _scoreStars = new Image[2];
        private RectTransform _scoreBarRect;
        private int _twoStars, _threeStars;
        private PunchScale _scorePunch;
        private readonly UiLabel[] _popups = new UiLabel[10];
        private readonly float[] _popupT = new float[10];
        private readonly Vector2[] _popupStart = new Vector2[10];
        private int _nextPopup;
        private bool _anyPopup;
        private Camera _camera;

        private void Awake()
        {
            UiTheme theme = UiRoot.Theme;

            // Nivelul (stânga sus).
            _levelPill = UiFactory.CreatePanel("LevelPill", transform, theme.PanelColor);
            UiFactory.Place((RectTransform)_levelPill.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(270f, 96f));
            _level = UiFactory.CreateText("Level", _levelPill.transform, "", 40);
            UiFactory.Stretch(_level.RectTransform, 6f);

            // Progresul borcanului (centru sus).
            Image card = UiFactory.CreatePanel("GoalCard", transform, theme.PanelLightColor);
            var cardRect = UiFactory.Place((RectTransform)card.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -136f), new Vector2(620f, 96f));
            _progressPunch = card.gameObject.AddComponent<PunchScale>();
            Image candy = UiFactory.CreateImage("CandyIcon", cardRect, SugarSprites.CandySwirl, Palette.WallStripe);
            UiFactory.Place(candy.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(54f, 0f), new Vector2(70f, 70f));
            _progress = UiFactory.CreateProgressBar("Progress", cardRect, theme.PositiveColor);
            var barRect = (RectTransform)_progress.transform;
            barRect.anchorMin = new Vector2(0f, 0.5f);
            barRect.anchorMax = new Vector2(1f, 0.5f);
            barRect.pivot = new Vector2(0.5f, 0.5f);
            barRect.offsetMin = new Vector2(100f, -24f);
            barRect.offsetMax = new Vector2(-24f, 24f);
            _progressText = UiFactory.CreateText("Text", barRect, "", 32);
            UiFactory.Stretch(_progressText.RectTransform);

            // Mutările rămase (dreapta sus) și butonul de pauză.
            Image movesDisc = UiFactory.CreateImage("MovesDisc", transform, theme.Circle, theme.AccentColor);
            UiFactory.Place(movesDisc.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-148f, -14f), new Vector2(124f, 124f));
            _movesPunch = movesDisc.gameObject.AddComponent<PunchScale>();
            _moves = UiFactory.CreateText("Moves", movesDisc.transform, "", 54, TextAnchor.MiddleCenter, theme.TextDarkColor);
            UiFactory.Place(_moves.RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 8f), new Vector2(110f, 66f));
            _movesCaption = UiFactory.CreateText("Caption", movesDisc.transform, "", 22, TextAnchor.MiddleCenter, theme.TextDarkColor);
            UiFactory.Place(_movesCaption.RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -32f), new Vector2(110f, 30f));

            Button pause = SugarUi.IconButton("Pause", transform, ProceduralSprites.Pause, theme.PanelColor,
                () =>
                {
                    SugarUi.Click();
                    PauseClicked?.Invoke();
                }, 0.45f);
            UiFactory.Place((RectTransform)pause.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -24f), new Vector2(104f, 104f));

            BuildScore(theme);

            // Textul tutorialului (sub HUD).
            _tutorial = SugarUi.Outlined("Tutorial", transform, "", 40, Color.white);
            UiFactory.Place(_tutorial.RectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -300f), new Vector2(980f, 100f));
            _tutorial.gameObject.SetActive(false);

            // Cuvântul de combo (centru) și bannerul („Ultimele mutări!”).
            _word = SugarUi.Outlined("Word", transform, "", 110, Color.white, 5f);
            UiFactory.Place(_word.RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 200f), new Vector2(1000f, 160f));
            _word.gameObject.SetActive(false);
            _banner = SugarUi.Outlined("Banner", transform, "", 64, theme.AccentColor, 4f);
            UiFactory.Place(_banner.RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 420f), new Vector2(1000f, 110f));
            _banner.gameObject.SetActive(false);
        }

        private void BuildScore(UiTheme theme)
        {
            Image pill = UiFactory.CreatePanel("ScorePill", transform, theme.PanelColor);
            var pillRect = UiFactory.Place((RectTransform)pill.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -240f), new Vector2(620f, 56f));
            _scorePunch = pill.gameObject.AddComponent<PunchScale>();
            _score = UiFactory.CreateText("Score", pillRect, "0", 34);
            UiFactory.Place(_score.RectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(16f, 0f), new Vector2(150f, 50f));
            _scoreBar = UiFactory.CreateProgressBar("ScoreBar", pillRect, theme.AccentColor);
            _scoreBarRect = (RectTransform)_scoreBar.transform;
            _scoreBarRect.anchorMin = new Vector2(0f, 0.5f);
            _scoreBarRect.anchorMax = new Vector2(1f, 0.5f);
            _scoreBarRect.pivot = new Vector2(0.5f, 0.5f);
            _scoreBarRect.offsetMin = new Vector2(176f, -11f);
            _scoreBarRect.offsetMax = new Vector2(-30f, 11f);
            for (int i = 0; i < 2; i++)
            {
                _scoreStars[i] = UiFactory.CreateImage("Star" + (i + 2), _scoreBarRect, ProceduralSprites.Star, Color.white);
                UiFactory.Place(_scoreStars[i].rectTransform, new Vector2(i == 0 ? 0.62f : 1f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(46f, 46f));
            }

            for (int i = 0; i < _popups.Length; i++)
            {
                _popups[i] = SugarUi.Outlined("Points", transform, "", 46, Color.white, 3f);
                UiFactory.Place(_popups[i].RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300f, 70f));
                _popups[i].gameObject.SetActive(false);
                _popupT[i] = -1f;
            }
        }

        /// <summary>Pragurile de 2 și 3 stele ale nivelului (bara se umple până la 3 stele).</summary>
        public void SetScoreTargets(int twoStars, int threeStars)
        {
            _twoStars = Mathf.Max(1, twoStars);
            _threeStars = Mathf.Max(_twoStars + 1, threeStars);
            float marker = (float)_twoStars / _threeStars;
            RectTransform first = _scoreStars[0].rectTransform;
            first.anchorMin = first.anchorMax = new Vector2(marker, 0.5f);
            SetScore(0, false);
        }

        public void SetScore(int score, bool punch)
        {
            _score.Text = score.ToString();
            _scoreBar.SetValue(_threeStars > 0 ? Mathf.Clamp01((float)score / _threeStars) : 0f, !punch);
            Color off = new(1f, 1f, 1f, 0.55f);
            _scoreStars[0].color = score >= _twoStars ? UiRoot.Theme.AccentColor : off;
            _scoreStars[1].color = score >= _threeStars ? UiRoot.Theme.AccentColor : off;
            if (punch) _scorePunch.Punch(0.15f);
        }

        /// <summary>Puncte care sar din locul unde au dispărut capacele (poziție din lume).</summary>
        public void ShowPoints(Vector3 world, int points, Color color)
        {
            if (_camera == null) _camera = Camera.main;
            if (_camera == null) return;
            Vector3 screen = _camera.WorldToScreenPoint(world);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, screen, null, out Vector2 local)) return;
            int i = _nextPopup;
            _nextPopup = (_nextPopup + 1) % _popups.Length;
            UiLabel label = _popups[i];
            label.Text = points.ToString();
            label.Color = color;
            label.gameObject.SetActive(true);
            label.RectTransform.SetAsLastSibling();
            _popupStart[i] = local;
            _popupT[i] = 0f;
            _anyPopup = true;
        }

        public void SetLevel(int level, bool boss)
        {
            _level.Text = boss ? SugarLoc.T("BOSS") + " " + level : SugarLoc.F("NIVELUL {0}", level);
            _levelPill.color = boss ? Palette.Tile(4) : UiRoot.Theme.PanelColor;
            _movesCaption.Text = SugarLoc.T("mutări");
        }

        public void SetMoves(int moves, bool punch)
        {
            _moves.Text = moves.ToString();
            _moves.Color = moves <= 3 ? UiRoot.Theme.NegativeColor : UiRoot.Theme.TextDarkColor;
            if (punch) _movesPunch.Punch(moves <= 3 ? 2f : 1f);
        }

        public void SetProgress(int count, int goal, bool punch)
        {
            _progressText.Text = $"{Mathf.Min(count, goal)}/{goal}";
            _progress.SetValue(goal > 0 ? (float)count / goal : 0f, !punch);
            if (punch) _progressPunch.Punch(0.4f);
        }

        public void SetTutorial(string text)
        {
            _tutorial.gameObject.SetActive(!string.IsNullOrEmpty(text));
            if (!string.IsNullOrEmpty(text)) _tutorial.Text = text;
        }

        /// <summary>Cuvânt mare care sare în mijlocul ecranului (combo).</summary>
        public void ShowWord(string text, Color color)
        {
            _word.Text = text;
            _word.Color = color;
            _word.gameObject.SetActive(true);
            _wordT = 0f;
        }

        public void ShowBanner(string text)
        {
            _banner.Text = text;
            _banner.gameObject.SetActive(true);
            _bannerT = 0f;
        }

        public void HideTransient()
        {
            _word.gameObject.SetActive(false);
            _banner.gameObject.SetActive(false);
            _wordT = _bannerT = -1f;
            for (int i = 0; i < _popups.Length; i++)
            {
                _popupT[i] = -1f;
                _popups[i].gameObject.SetActive(false);
            }
            _anyPopup = false;
        }

        private void Update()
        {
            if (_wordT >= 0f)
            {
                _wordT += Time.unscaledDeltaTime / 1.1f;
                float t = Mathf.Clamp01(_wordT);
                // Apare cu „boing”, stă puțin, apoi urcă și se stinge.
                float scale = t < 0.2f ? Mathf.LerpUnclamped(0.3f, 1f, Overshoot(t / 0.2f)) : 1f;
                _word.RectTransform.localScale = Vector3.one * scale;
                _word.RectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 12f) * 4f * (1f - t));
                _word.RectTransform.anchoredPosition = new Vector2(0f, 200f + Mathf.Max(0f, t - 0.6f) * 300f);
                Color c = _word.Color;
                c.a = t < 0.7f ? 1f : 1f - (t - 0.7f) / 0.3f;
                _word.Color = c;
                if (t >= 1f)
                {
                    _wordT = -1f;
                    _word.gameObject.SetActive(false);
                }
            }

            if (_bannerT >= 0f)
            {
                _bannerT += Time.unscaledDeltaTime / 1.8f;
                float t = Mathf.Clamp01(_bannerT);
                _banner.RectTransform.localScale = Vector3.one * (1f + Mathf.Sin(t * Mathf.PI * 6f) * 0.06f);
                Color c = _banner.Color;
                c.a = t < 0.8f ? 1f : 1f - (t - 0.8f) / 0.2f;
                _banner.Color = c;
                if (t >= 1f)
                {
                    _bannerT = -1f;
                    _banner.gameObject.SetActive(false);
                }
            }

            if (_anyPopup) UpdatePopups();

            if (_tutorial.gameObject.activeSelf)
                _tutorial.RectTransform.localScale = Vector3.one * (1f + Mathf.Sin(Time.unscaledTime * 4f) * 0.03f);
        }

        private void UpdatePopups()
        {
            _anyPopup = false;
            for (int i = 0; i < _popups.Length; i++)
            {
                if (_popupT[i] < 0f) continue;
                _popupT[i] += Time.unscaledDeltaTime / 0.9f;
                float t = Mathf.Clamp01(_popupT[i]);
                UiLabel label = _popups[i];
                label.RectTransform.anchoredPosition = _popupStart[i] + new Vector2(0f, UiEase.OutCubic(t) * 130f);
                label.RectTransform.localScale = Vector3.one * (t < 0.2f ? UiEase.OutBack(t / 0.2f, 2.5f) : 1f);
                Color c = label.Color;
                c.a = t < 0.65f ? 1f : 1f - (t - 0.65f) / 0.35f;
                label.Color = c;
                if (t >= 1f)
                {
                    _popupT[i] = -1f;
                    label.gameObject.SetActive(false);
                }
                else _anyPopup = true;
            }
        }

        private static float Overshoot(float t)
        {
            const float s = 2.2f;
            t -= 1f;
            return t * t * ((s + 1f) * t + s) + 1f;
        }
    }
}
