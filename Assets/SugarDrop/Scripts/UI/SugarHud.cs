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

            // Textul tutorialului (sub HUD).
            _tutorial = SugarUi.Outlined("Tutorial", transform, "", 40, Color.white);
            UiFactory.Place(_tutorial.RectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -250f), new Vector2(980f, 80f));
            _tutorial.gameObject.SetActive(false);

            // Cuvântul de combo (centru) și bannerul („Ultimele mutări!”).
            _word = SugarUi.Outlined("Word", transform, "", 110, Color.white, 5f);
            UiFactory.Place(_word.RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 200f), new Vector2(1000f, 160f));
            _word.gameObject.SetActive(false);
            _banner = SugarUi.Outlined("Banner", transform, "", 64, theme.AccentColor, 4f);
            UiFactory.Place(_banner.RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 420f), new Vector2(1000f, 110f));
            _banner.gameObject.SetActive(false);
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

            if (_tutorial.gameObject.activeSelf)
                _tutorial.RectTransform.localScale = Vector3.one * (1f + Mathf.Sin(Time.unscaledTime * 4f) * 0.03f);
        }

        private static float Overshoot(float t)
        {
            const float s = 2.2f;
            t -= 1f;
            return t * t * ((s + 1f) * t + s) + 1f;
        }
    }
}
