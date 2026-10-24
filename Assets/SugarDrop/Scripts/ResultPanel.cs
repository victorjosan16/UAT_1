using System;
using QuarryEmpire;
using UnityEngine;
using UnityEngine.UI;

namespace SugarDrop
{
    /// <summary>Fereastra de final de nivel: „Bravo!” cu stele, sau „Mai încearcă”.</summary>
    public class ResultPanel : MonoBehaviour
    {
        private GameObject _root;
        private UiLabel _title;
        private UiLabel _subtitle;
        private UiLabel _buttonLabel;
        private Image _button;
        private readonly Image[] _stars = new Image[3];
        private Action _onClick;

        private void Awake()
        {
            UiTheme theme = UiRoot.Theme;
            Image dim = UiFactory.CreateImage("Dim", transform, UiFactory.WhiteSprite, new Color(0.15f, 0.05f, 0.12f, 0.55f));
            dim.preserveAspect = false;
            dim.raycastTarget = true;
            UiFactory.Stretch(dim.rectTransform, -200f);
            _root = dim.gameObject;

            Image box = UiFactory.CreatePanel("Box", dim.transform, theme.PanelLightColor);
            var boxRect = UiFactory.Place((RectTransform)box.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(800f, 760f));
            box.gameObject.AddComponent<CanvasGroup>();
            box.gameObject.AddComponent<PopIn>();

            for (int i = 0; i < 3; i++)
            {
                _stars[i] = UiFactory.CreateImage("Star" + i, boxRect, ProceduralSprites.Star, Color.white);
                float x = (i - 1) * 170f;
                float y = i == 1 ? 40f : 0f;
                float size = i == 1 ? 190f : 150f;
                UiFactory.Place(_stars[i].rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(x, y), new Vector2(size, size));
            }

            _title = UiFactory.CreateText("Title", boxRect, "", 84, TextAnchor.MiddleCenter, theme.TextDarkColor);
            UiFactory.Place(_title.RectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(720f, 120f));
            _subtitle = UiFactory.CreateText("Subtitle", boxRect, "", 40, TextAnchor.MiddleCenter, Color.Lerp(theme.TextDarkColor, Color.white, 0.35f));
            UiFactory.Place(_subtitle.RectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -280f), new Vector2(720f, 140f));

            Button button = UiFactory.CreateButton("Action", boxRect, "", theme.PositiveColor, () => _onClick?.Invoke(), 52);
            UiFactory.Place((RectTransform)button.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(560f, 150f));
            _button = button.transform.Find("Face").GetComponent<Image>();
            _buttonLabel = UiFactory.GetButtonLabel(button);

            Hide();
        }

        public void ShowWin(int level, int stars, Action onNext)
        {
            UiTheme theme = UiRoot.Theme;
            for (int i = 0; i < 3; i++)
            {
                _stars[i].gameObject.SetActive(true);
                _stars[i].color = i < stars ? theme.AccentColor : new Color(0.85f, 0.82f, 0.8f);
            }
            _title.Text = "BRAVO!";
            _subtitle.Text = $"Nivelul {level} terminat";
            _buttonLabel.Text = "CONTINUĂ";
            _button.color = theme.PositiveColor;
            _onClick = onNext;
            _root.SetActive(true);
        }

        public void ShowLose(int collected, int goal, Action onRetry)
        {
            foreach (Image star in _stars) star.gameObject.SetActive(false);
            _title.Text = "AULEU!";
            _subtitle.Text = $"Ai strâns {collected} din {goal} bomboane.\nMai încearcă!";
            _buttonLabel.Text = "REÎNCEARCĂ";
            _button.color = UiRoot.Theme.AccentColor;
            _onClick = onRetry;
            _root.SetActive(true);
        }

        public void Hide() => _root.SetActive(false);
    }
}
