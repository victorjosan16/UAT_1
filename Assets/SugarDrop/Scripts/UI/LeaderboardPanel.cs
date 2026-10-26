using System.Collections.Generic;
using System.Globalization;
using QuarryEmpire;
using UnityEngine;
using UnityEngine.UI;

namespace SugarDrop
{
    /// <summary>
    /// Clasamentul: primii jucători (cu medalii pentru primele 3 locuri), rândul tău evidențiat și, jos,
    /// locul tău și punctele (suma celor mai bune scoruri de pe fiecare nivel).
    /// </summary>
    public class LeaderboardPanel : MonoBehaviour
    {
        private const float Width = 900f;
        private const float Height = 1560f;
        private const float RowHeight = 104f;
        private const float RowGap = 10f;

        private static readonly Color[] MedalColors = { new(1f, 0.8f, 0.2f), new(0.78f, 0.8f, 0.86f), new(0.85f, 0.55f, 0.3f) };
        private static readonly Color RowColor = new(1f, 1f, 1f, 0.75f);
        private static readonly Color MyRowColor = new(1f, 0.86f, 0.45f);

        private class RowView
        {
            public GameObject Root;
            public Image Background;
            public Image Medal;
            public UiLabel Rank;
            public UiLabel Name;
            public UiLabel Score;
        }

        private GameObject _root;
        private UiLabel _title;
        private UiLabel _subtitle;
        private UiLabel _status;
        private UiLabel _mine;
        private UiLabel _closeLabel;
        private RectTransform _content;
        private ScrollRect _scroll;
        private readonly List<RowView> _rows = new();
        private int _request;

        public bool IsOpen => _root != null && _root.activeSelf;

        private void Awake()
        {
            UiTheme theme = UiRoot.Theme;
            _root = SugarUi.Dim(transform).gameObject;
            RectTransform box = SugarUi.Box(_root.transform, new Vector2(Width, Height));

            Image trophy = UiFactory.CreateImage("Trophy", box, SugarSprites.Trophy, theme.AccentColor);
            UiFactory.Place(trophy.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(170f, 170f));
            _title = SugarUi.Outlined("Title", box, "", 80, theme.AccentColor, 4f);
            UiFactory.Place(_title.RectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -80f), new Vector2(Width - 60f, 110f));
            _subtitle = UiFactory.CreateText("Subtitle", box, "", 34, TextAnchor.MiddleCenter, Color.Lerp(theme.TextDarkColor, Color.white, 0.3f));
            UiFactory.Place(_subtitle.RectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -185f), new Vector2(Width - 80f, 60f));

            // Lista care se derulează.
            Image viewport = UiFactory.CreatePanel("List", box, new Color(0.45f, 0.2f, 0.38f, 0.12f));
            var viewRect = UiFactory.Place((RectTransform)viewport.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -260f), new Vector2(Width - 70f, 980f));
            viewport.raycastTarget = true;
            viewport.gameObject.AddComponent<RectMask2D>();
            _content = UiFactory.CreateRect("Content", viewRect);
            _content.anchorMin = new Vector2(0f, 1f);
            _content.anchorMax = new Vector2(1f, 1f);
            _content.pivot = new Vector2(0.5f, 1f);
            _content.anchoredPosition = Vector2.zero;
            _content.sizeDelta = Vector2.zero;
            _scroll = viewport.gameObject.AddComponent<ScrollRect>();
            _scroll.viewport = viewRect;
            _scroll.content = _content;
            _scroll.horizontal = false;
            _scroll.movementType = ScrollRect.MovementType.Elastic;
            _scroll.scrollSensitivity = 30f;

            _status = UiFactory.CreateText("Status", viewRect, "", 40, TextAnchor.MiddleCenter, theme.TextDarkColor);
            UiFactory.Stretch(_status.RectTransform, 40f);

            _mine = SugarUi.Outlined("Mine", box, "", 44, Color.white, 3f);
            Image mineBack = UiFactory.CreatePanel("MineBack", box, theme.PanelColor);
            UiFactory.Place((RectTransform)mineBack.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 190f), new Vector2(Width - 70f, 100f));
            _mine.transform.SetParent(mineBack.transform, false);
            UiFactory.Stretch(_mine.RectTransform, 10f);

            Button close = SugarUi.Button("Close", box, "", theme.PositiveColor, () =>
            {
                SugarUi.Click();
                Hide();
            }, 46);
            UiFactory.Place((RectTransform)close.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 45f), new Vector2(520f, 120f));
            _closeLabel = UiFactory.GetButtonLabel(close);

            _root.SetActive(false);
        }

        public void Show()
        {
            _title.Text = SugarLoc.T("CLASAMENT");
            _subtitle.Text = SugarLoc.T("Punctele = suma celor mai bune scoruri de pe fiecare nivel");
            _closeLabel.Text = SugarLoc.T("ÎNCHIDE");
            SetRows(null);
            _status.Text = SugarLoc.T("Se încarcă…");
            _mine.Text = MineText(new LeaderboardRow(0, SugarSave.Data.nickname, SugarSave.TotalScore, true));
            _root.SetActive(true);

            int request = ++_request;
            SugarLeaderboard leaderboard = SugarLeaderboard.Ensure();
            leaderboard.Submit();
            leaderboard.Load(result =>
            {
                if (request != _request || !IsOpen) return;
                SetRows(result.Top);
                _mine.Text = MineText(result.Me);
                _status.Text = result.Online ? (result.Top.Count == 0 ? SugarLoc.T("Fii primul în clasament!") : "")
                    : SugarLoc.T("Clasamentul online nu e disponibil acum. Verifică internetul.");
                if (!result.Online) _status.RectTransform.anchoredPosition = new Vector2(0f, -RowHeight);
            });
        }

        public void Hide()
        {
            _request++;
            if (_root.activeSelf) _root.SetActive(false);
        }

        private static string MineText(LeaderboardRow me) => me.Rank > 0
            ? SugarLoc.F("Locul tău: {0} · {1} puncte", me.Rank, Format(me.Score))
            : SugarLoc.F("Punctele tale: {0}", Format(me.Score));

        private static string Format(long score) => score.ToString("#,0", CultureInfo.InvariantCulture).Replace(',', '.');

        private void SetRows(List<LeaderboardRow> rows)
        {
            int count = rows?.Count ?? 0;
            while (_rows.Count < count) _rows.Add(CreateRow(_rows.Count));
            for (int i = 0; i < _rows.Count; i++)
            {
                RowView view = _rows[i];
                view.Root.SetActive(i < count);
                if (i >= count) continue;
                LeaderboardRow row = rows[i];
                view.Background.color = row.IsMe ? MyRowColor : RowColor;
                bool medal = row.Rank >= 1 && row.Rank <= 3;
                view.Medal.gameObject.SetActive(medal);
                if (medal) view.Medal.color = MedalColors[row.Rank - 1];
                view.Rank.Text = row.Rank.ToString();
                view.Name.Text = row.IsMe ? row.Name + SugarLoc.T(" (tu)") : row.Name;
                view.Score.Text = Format(row.Score);
            }
            _content.sizeDelta = new Vector2(0f, count * (RowHeight + RowGap) + RowGap);
            _content.anchoredPosition = Vector2.zero;
            _status.RectTransform.anchoredPosition = Vector2.zero;
        }

        private RowView CreateRow(int index)
        {
            UiTheme theme = UiRoot.Theme;
            var view = new RowView();
            view.Background = UiFactory.CreatePanel("Row" + index, _content, RowColor);
            var rect = (RectTransform)view.Background.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(-28f, RowHeight);
            rect.anchoredPosition = new Vector2(0f, -RowGap - index * (RowHeight + RowGap));
            view.Root = rect.gameObject;

            view.Medal = UiFactory.CreateImage("Medal", rect, SugarSprites.Circle, Color.white);
            UiFactory.Place(view.Medal.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(66f, 0f), new Vector2(80f, 80f));
            view.Rank = UiFactory.CreateText("Rank", rect, "", 44, TextAnchor.MiddleCenter, theme.TextDarkColor);
            UiFactory.Place(view.Rank.RectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(66f, 0f), new Vector2(120f, 80f));
            view.Name = UiFactory.CreateText("Name", rect, "", 38, TextAnchor.MiddleLeft, theme.TextDarkColor);
            UiFactory.Place(view.Name.RectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(130f, 0f), new Vector2(390f, 80f));
            view.Score = UiFactory.CreateText("Score", rect, "", 42, TextAnchor.MiddleRight, theme.TextDarkColor);
            UiFactory.Place(view.Score.RectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-30f, 0f), new Vector2(240f, 80f));
            return view;
        }
    }
}
