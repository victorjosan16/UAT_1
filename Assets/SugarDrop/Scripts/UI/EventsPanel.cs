using System;
using QuarryEmpire;
using UnityEngine;
using UnityEngine.UI;

namespace SugarDrop
{
    /// <summary>Ce deschide fiecare rând din fereastra de evenimente (ferestrele existente ale jocului).</summary>
    public sealed class EventsHost
    {
        public Action Wheel;
        public Action Gift;
        public Action Missions;
        public Action Weekend;
    }

    /// <summary>
    /// Evenimentele într-un singur loc, ca listă de carduri crem: roata norocului, cadoul zilnic, misiunile și
    /// weekendul dulce. Fiecare card are iconița, numele, starea (gratis, ziua din 7, misiuni făcute, cât mai e)
    /// și butonul „Deschide”; punctul roșu arată ce se poate lua acum.
    /// </summary>
    public class EventsPanel : MonoBehaviour
    {
        private const float Width = 900f;
        private const float RowHeight = 156f;
        private const float RowStep = 172f;

        private sealed class Row
        {
            public UiLabel Title;
            public UiLabel Sub;
            public GameObject Badge;
            public UiLabel Open;
        }

        private GameObject _root;
        private RectTransform _box;
        private UiLabel _title;
        private Row _wheel, _gift, _missions, _weekend;
        private EventsHost _host;
        private float _refresh;

        public bool IsOpen => _root != null && _root.activeSelf;

        private void Awake()
        {
            UiTheme theme = UiRoot.Theme;
            _root = SugarUi.Dim(transform).gameObject;
            _box = SugarUi.Box(_root.transform, new Vector2(Width, 300f + 4 * RowStep));
            _title = SugarUi.Outlined("Title", _box, "", 72, theme.AccentColor, 4f);
            UiFactory.Place(_title.RectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(640f, 100f));
            Button close = SugarUi.IconButton("Close", _box, SugarSprites.Cross, theme.NegativeColor, () =>
            {
                SugarUi.Click();
                Hide();
            }, 0.45f);
            UiFactory.Place((RectTransform)close.transform, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-30f, -30f), new Vector2(100f, 100f));

            _wheel = AddRow(0, SugarSprites.Wheel(8), Color.white, Palette.Tile(3), () => _host?.Wheel);
            _gift = AddRow(1, SugarSprites.Gift, Palette.Tile(0), QuestStyle.SurfaceTint, () => _host?.Gift);
            _missions = AddRow(2, SugarSprites.Checklist, QuestStyle.Blue, QuestStyle.SurfaceTint, () => _host?.Missions);
            _weekend = AddRow(3, ProceduralSprites.Star, QuestStyle.GoldDeep, QuestStyle.SurfaceTint, () => _host?.Weekend);

            _root.SetActive(false);
        }

        private Row AddRow(int index, Sprite icon, Color iconColor, Color iconBack, Func<Action> action)
        {
            RectTransform card = SugarUi.Card("Event" + index, _box, out Image face);
            UiFactory.Place(card, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -150f - index * RowStep), new Vector2(Width - 100f, RowHeight));
            Image disc = UiFactory.CreateImage("Disc", face.transform, SugarSprites.Circle, iconBack);
            UiFactory.Place(disc.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(116f, 116f));
            Image image = UiFactory.CreateImage("Icon", disc.transform, icon, iconColor);
            UiFactory.Place(image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(86f, 86f));
            var row = new Row
            {
                Title = UiFactory.CreateText("Title", face.transform, "", 38, TextAnchor.MiddleLeft, QuestStyle.Text),
                Sub = UiFactory.CreateText("Sub", face.transform, "", 28, TextAnchor.MiddleLeft, QuestStyle.TextSoft),
            };
            UiFactory.Place(row.Title.RectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(152f, 24f), new Vector2(390f, 52f));
            UiFactory.Place(row.Sub.RectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(152f, -26f), new Vector2(390f, 40f));

            Button open = SugarUi.Button("Open", face.transform, "", UiRoot.Theme.PositiveColor, () =>
            {
                SugarUi.Click();
                Action run = action();
                Hide();
                run?.Invoke();
            }, 30);
            UiFactory.Place((RectTransform)open.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-20f, 2f), new Vector2(210f, 92f));
            row.Open = UiFactory.GetButtonLabel(open);
            Image badge = UiFactory.CreateImage("Badge", open.transform, SugarSprites.Circle, QuestStyle.Red);
            UiFactory.Place(badge.rectTransform, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-8f, -8f), new Vector2(36f, 36f));
            row.Badge = badge.gameObject;
            return row;
        }

        public void Show(EventsHost host)
        {
            _host = host;
            _root.SetActive(true);
            Refresh();
        }

        public void Hide() => _root.SetActive(false);

        /// <summary>Ceva de luat acum (roată gratis, cadou, misiune terminată): punctul roșu de pe „Evenimente”.</summary>
        public static bool AnythingNew => SugarWheel.FreeSpinsLeft > 0 || SugarDaily.GiftAvailable || SugarDaily.AnyClaimable;

        private void Update()
        {
            if (!IsOpen) return;
            _refresh -= Time.unscaledDeltaTime;
            if (_refresh > 0f) return;
            _refresh = 1f;
            Refresh();
        }

        private void Refresh()
        {
            _title.Text = SugarLoc.T("EVENIMENTE");
            int freeSpins = SugarWheel.FreeSpinsLeft;
            Fill(_wheel, "Roata norocului", freeSpins > 0 ? SugarLoc.T("GRATIS") : SugarWheel.AdSpinsLeft > 0 ? SugarLoc.T("cu reclamă") : "", freeSpins > 0);
            bool gift = SugarDaily.GiftAvailable;
            Fill(_gift, "Cadou zilnic", gift ? SugarLoc.F("Ziua {0} din 7", SugarDaily.GiftStreakDay) : "", gift);
            Fill(_missions, "Misiuni zilnice", MissionsDone() + "/" + DailyRules.MissionsPerDay, SugarDaily.AnyClaimable);
            Fill(_weekend, "Weekend norocos", MainMenu.Countdown(SugarEvents.Countdown(DateTime.Now)), false);
        }

        private static void Fill(Row row, string titleKey, string sub, bool badge)
        {
            row.Title.Text = SugarLoc.T(titleKey);
            row.Sub.Text = sub;
            row.Badge.SetActive(badge);
            row.Open.Text = SugarLoc.T("DESCHIDE");
        }

        private static int MissionsDone()
        {
            int done = 0;
            for (int i = 0; i < DailyRules.MissionsPerDay; i++)
                if (SugarDaily.Claimed(i) || SugarDaily.CanClaim(i)) done++;
            return done;
        }
    }
}
