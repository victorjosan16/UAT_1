using System;
using QuarryEmpire;
using UnityEngine;
using UnityEngine.UI;

namespace SugarDrop
{
    /// <summary>
    /// HUD-ul de sus: nivelul, progresul bomboanelor în borcan (x/obiectiv), mutările rămase și butonul de reluare.
    /// </summary>
    public class SugarHud : MonoBehaviour
    {
        public event Action RestartClicked;

        private UiLabel _level;
        private UiLabel _moves;
        private UiLabel _progressText;
        private UiProgressBar _progress;
        private PunchScale _movesPunch;
        private PunchScale _progressPunch;

        private void Awake()
        {
            UiTheme theme = UiRoot.Theme;

            // Nivelul (stânga sus).
            Image levelPill = UiFactory.CreatePanel("LevelPill", transform, theme.PanelColor);
            UiFactory.Place((RectTransform)levelPill.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(250f, 96f));
            _level = UiFactory.CreateText("Level", levelPill.transform, "", 40);
            UiFactory.Stretch(_level.RectTransform, 6f);

            // Progresul borcanului (centru sus).
            Image card = UiFactory.CreatePanel("GoalCard", transform, theme.PanelLightColor);
            var cardRect = UiFactory.Place((RectTransform)card.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -136f), new Vector2(620f, 96f));
            _progressPunch = card.gameObject.AddComponent<PunchScale>();
            Image candy = UiFactory.CreateImage("CandyIcon", cardRect, ProceduralSprites.Circle, Palette.WallStripe);
            UiFactory.Place(candy.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(54f, 0f), new Vector2(64f, 64f));
            _progress = UiFactory.CreateProgressBar("Progress", cardRect, theme.PositiveColor);
            var barRect = (RectTransform)_progress.transform;
            barRect.anchorMin = new Vector2(0f, 0.5f);
            barRect.anchorMax = new Vector2(1f, 0.5f);
            barRect.pivot = new Vector2(0.5f, 0.5f);
            barRect.offsetMin = new Vector2(100f, -24f);
            barRect.offsetMax = new Vector2(-24f, 24f);
            _progressText = UiFactory.CreateText("Text", barRect, "", 32);
            UiFactory.Stretch(_progressText.RectTransform);

            // Mutările rămase (dreapta sus) și butonul de reluare.
            Image movesDisc = UiFactory.CreateImage("MovesDisc", transform, theme.Circle, theme.AccentColor);
            UiFactory.Place(movesDisc.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-148f, -14f), new Vector2(124f, 124f));
            _movesPunch = movesDisc.gameObject.AddComponent<PunchScale>();
            _moves = UiFactory.CreateText("Moves", movesDisc.transform, "", 54, TextAnchor.MiddleCenter, theme.TextDarkColor);
            UiFactory.Place(_moves.RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 8f), new Vector2(110f, 66f));
            UiLabel caption = UiFactory.CreateText("Caption", movesDisc.transform, "mutări", 22, TextAnchor.MiddleCenter, theme.TextDarkColor);
            UiFactory.Place(caption.RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -32f), new Vector2(110f, 30f));

            Button restart = UiFactory.CreateIconButton("Restart", transform, ProceduralSprites.Restart, theme.PanelColor,
                () => RestartClicked?.Invoke(), 0.55f);
            UiFactory.Place((RectTransform)restart.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -24f), new Vector2(104f, 104f));
        }

        public void SetLevel(int level) => _level.Text = $"NIVELUL {level}";

        public void SetMoves(int moves, bool punch)
        {
            _moves.Text = moves.ToString();
            _moves.Color = moves <= 3 ? UiRoot.Theme.NegativeColor : UiRoot.Theme.TextDarkColor;
            if (punch) _movesPunch.Punch();
        }

        public void SetProgress(int count, int goal, bool punch)
        {
            _progressText.Text = $"{Mathf.Min(count, goal)}/{goal}";
            _progress.SetValue(goal > 0 ? (float)count / goal : 0f, !punch);
            if (punch) _progressPunch.Punch(0.4f);
        }
    }
}
