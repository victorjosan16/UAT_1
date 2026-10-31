using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Veliqora.Core.Ads;
using Veliqora.Core.Economy;
using Veliqora.Core.Progression;
using Veliqora.Infrastructure;

namespace Veliqora.UI
{
    /// <summary>
    /// Daily reward calendar, achievements list and the "unlocked!" notification banner queue.
    /// All rewards go through the services (idempotent ledger tx ids); this class only presents them.
    /// </summary>
    public static class ProgressionOverlays
    {
        private const string NotifiedPrefix = "notified:ach:";

        public static int Today => DayClock.DayNumber(DateTime.Now);

        public static string BoosterName(BoosterType t)
        {
            switch (t)
            {
                case BoosterType.Undo: return Loc.T("Undo");
                case BoosterType.ExtraBottle: return Loc.T("Add bottle");
                case BoosterType.Shuffle: return Loc.T("Shuffle");
                case BoosterType.Hint: return Loc.T("Hint");
                case BoosterType.RemoveColor: return Loc.T("Remove color");
                default: return t.ToString();
            }
        }

        public static string Describe(DailyRewardDef r)
        {
            var parts = new List<string>();
            if (r.Coins > 0) parts.Add("+" + r.Coins);
            if (r.Booster.HasValue) parts.Add(BoosterName(r.Booster.Value) + " ×" + Math.Max(1, r.BoosterQuantity));
            return string.Join("\n", parts);
        }

        // ---------------- daily reward ----------------

        public static OverlayHandle ShowDaily(AppServices s, Action closed = null)
        {
            int today = Today;
            bool canClaim = s.Daily.CanClaim(today);
            int streak = canClaim ? s.Daily.NextStreak(today) : s.Daily.State.Streak;
            int days = s.ProgressionConfig.Daily.Count;
            int cycleStart = ((Mathf.Max(1, streak) - 1) / days) * days;   // which 7-day cycle we are in
            var h = Overlays.Open("Daily", new Vector2(960, 1260), o =>
            {
                var sub = UIFactory.Text(o.Content, Loc.F("Day {0} of your streak. Come back tomorrow to keep it going!", streak), UIStyle.FontCaption, UIStyle.InkSoft, TextAnchor.UpperCenter, FontStyle.Normal);
                UIFactory.Band(sub.rectTransform, 1f, 100, 0, 20, 1f);
                for (int i = 0; i < days; i++)
                {
                    int day = cycleStart + i + 1;
                    var def = s.Daily.RewardFor(day);
                    bool past = day < streak || (day == streak && !canClaim);
                    bool current = day == streak && canClaim;
                    int row = i < 4 ? 0 : 1;
                    int col = row == 0 ? i : i - 4;
                    int perRow = row == 0 ? 4 : days - 4;
                    float x = (col - (perRow - 1) * 0.5f) * 212f;
                    var tile = UIFactory.Panel(o.Content, "Day" + day, current ? UIStyle.Sunshine : (past ? UIStyle.Disabled : UIStyle.Card), UIStyle.RadiusSmall, false);
                    var tr = (RectTransform)tile.transform.parent;
                    UIFactory.Place(tr, new Vector2(0.5f, 1f), new Vector2(196, 300), new Vector2(x, -120 - row * 320), new Vector2(0.5f, 1f));
                    Color ink = current ? UIStyle.Brown : UIStyle.Ink;
                    var dt = UIFactory.Text(tr, Loc.F("Day {0}", day), UIStyle.FontCaption, ink);
                    UIFactory.Band(dt.rectTransform, 1f, 56, -10, 6, 1f);
                    var icon = UIFactory.Image(tr, "Icon", SpriteLibrary.Get(past ? Icon.Check : (def.Booster.HasValue ? Icon.Chest : Icon.Coin)), Color.white);
                    UIFactory.Place(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(96, 96), new Vector2(0, 14));
                    var rt = UIFactory.Text(tr, Describe(def), UIStyle.FontCaption - 8, ink);
                    UIFactory.Band(rt.rectTransform, 0f, 80, 8, 6, 0f);
                }
                if (canClaim)
                {
                    ButtonHandle claim = null;
                    claim = UIFactory.Button(o.Content, Loc.T("Claim"), UIStyle.ButtonKind.Gold, () =>
                    {
                        var r = s.Daily.Claim(today);
                        if (r == null) return;
                        s.Save.Stats.DailyClaims++;
                        s.MarkDirty();
                        s.Audio.PlayCoin();
                        Overlays.Close(o);
                        if (r.Coins > 0 && s.AdGate.CanRequest(AdPlacements.DailyDouble)) OfferDouble(s, today, r.Coins, closed);
                        else closed?.Invoke();
                        CheckAchievementNotices(s);
                    }, new Vector2(600, UIStyle.ButtonHeight), Icon.Coin);
                    UIFactory.Place(claim.Rect, new Vector2(0.5f, 0f), claim.Rect.sizeDelta, new Vector2(0, 30), new Vector2(0.5f, 0f));
                }
                else
                {
                    var done = UIFactory.Text(o.Content, Loc.T("Already claimed today. See you tomorrow!"), UIStyle.FontBody, UIStyle.Ink);
                    UIFactory.Band(done.rectTransform, 0f, 140, 30, 20, 0f);
                }
            }, true, Loc.T("Daily reward"));
            if (!canClaim) h.Closed += () => closed?.Invoke();
            return h;
        }

        private static void OfferDouble(AppServices s, int today, int coins, Action closed)
        {
            Overlays.Confirm(Loc.T("Double it?"), Loc.F("Watch a short ad to get +{0} more coins.", coins), Loc.T(s.Ads.IsMock ? "Double (mock ad)" : "Watch ad"), () =>
            {
                bool ok = false;
                s.AdGate.Request(AdPlacements.DailyDouble, () => { ok = s.Daily.ClaimAdDouble(today); if (ok) s.MarkDirty(); }, outcome =>
                {
                    Overlays.Toast(Loc.T(ok ? "Coins doubled!" : "No reward — the ad wasn't completed."));
                    closed?.Invoke();
                });
            }, Loc.T("No thanks"), UIStyle.ButtonKind.Accent, () => closed?.Invoke());
        }

        // ---------------- achievements ----------------

        public static OverlayHandle ShowAchievements(AppServices s, Action changed = null)
        {
            return Overlays.Open("Achievements", new Vector2(960, 1500), o => BuildAchievementList(s, o, changed), true, Loc.T("Achievements"));
        }

        private static void BuildAchievementList(AppServices s, OverlayHandle o, Action changed)
        {
            for (int i = o.Content.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(o.Content.GetChild(i).gameObject);
            var viewport = UIFactory.Rect("Viewport", o.Content);
            UIFactory.Stretch(viewport);
            viewport.gameObject.AddComponent<Image>().color = new Color(1, 1, 1, 0.003f);
            viewport.gameObject.AddComponent<RectMask2D>();
            var list = UIFactory.Rect("List", viewport);
            list.anchorMin = new Vector2(0, 1);
            list.anchorMax = new Vector2(1, 1);
            list.pivot = new Vector2(0.5f, 1);
            list.offsetMin = Vector2.zero;
            list.offsetMax = Vector2.zero;
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = list;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;

            var all = s.Achievements.All();
            // Claimable first, then in progress, then claimed.
            all.Sort((a, b) => Rank(a).CompareTo(Rank(b)));
            const float rowH = 170f;
            for (int i = 0; i < all.Count; i++)
            {
                var st = all[i];
                var bg = UIFactory.Panel(list, "Ach_" + st.Def.Id, st.Claimed ? UIStyle.WithAlpha(UIStyle.Card, 0.6f) : UIStyle.Card, UIStyle.RadiusSmall, false);
                var row = (RectTransform)bg.transform.parent;
                row.anchorMin = new Vector2(0, 1); row.anchorMax = new Vector2(1, 1); row.pivot = new Vector2(0.5f, 1);
                row.offsetMin = new Vector2(0, -(i + 1) * rowH + 12); row.offsetMax = new Vector2(0, -i * rowH);
                var icon = UIFactory.Image(row, "Icon", SpriteLibrary.Get(st.Claimed ? Icon.Check : (st.Complete ? Icon.Star : Icon.StarEmpty)), st.Complete ? UIStyle.Sunshine : UIStyle.InkSoft);
                UIFactory.Place(icon.rectTransform, new Vector2(0, 0.5f), new Vector2(90, 90), new Vector2(24, 0), new Vector2(0, 0.5f));
                var title = UIFactory.Text(row, Loc.T(st.Def.Title), UIStyle.FontLabel, UIStyle.Ink, TextAnchor.MiddleLeft);
                title.rectTransform.offsetMin = new Vector2(132, 60); title.rectTransform.offsetMax = new Vector2(-300, -10);
                var prog = UIFactory.Text(row, st.Progress + " / " + st.Def.Target + "    +" + st.Def.RewardCoins, UIStyle.FontCaption, UIStyle.InkSoft, TextAnchor.MiddleLeft, FontStyle.Normal);
                prog.rectTransform.offsetMin = new Vector2(132, 10); prog.rectTransform.offsetMax = new Vector2(-300, -80);
                // progress bar
                var barBg = UIFactory.Image(row, "Bar", SpriteLibrary.Rounded(10), new Color(0, 0, 0, 0.35f));
                UIFactory.Place(barBg.rectTransform, new Vector2(0, 0f), new Vector2(460, 16), new Vector2(132, 14), new Vector2(0, 0f));
                var fill = UIFactory.Image(barBg.transform, "Fill", SpriteLibrary.Rounded(10), UIStyle.Mint);
                fill.rectTransform.anchorMin = Vector2.zero;
                fill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(st.Progress / (float)Mathf.Max(1, st.Def.Target)), 1f);
                fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;

                if (st.Complete && !st.Claimed)
                {
                    string id = st.Def.Id;
                    var claim = UIFactory.Button(row, Loc.T("Claim"), UIStyle.ButtonKind.Gold, () =>
                    {
                        if (s.Achievements.Claim(id)) { s.Audio.PlayCoin(); s.MarkDirty(); changed?.Invoke(); }
                        BuildAchievementList(s, o, changed);
                    }, new Vector2(250, 110), Icon.Coin, UIStyle.FontCaption);
                    UIFactory.Place(claim.Rect, new Vector2(1, 0.5f), claim.Rect.sizeDelta, new Vector2(-20, 0), new Vector2(1, 0.5f));
                }
                else
                {
                    var state = UIFactory.Text(row, Loc.T(st.Claimed ? "Claimed" : "In progress"), UIStyle.FontCaption, st.Claimed ? UIStyle.Mint : UIStyle.InkSoft);
                    UIFactory.Place(state.rectTransform, new Vector2(1, 0.5f), new Vector2(260, 80), new Vector2(-20, 0), new Vector2(1, 0.5f));
                }
            }
            list.sizeDelta = new Vector2(0, all.Count * rowH);
        }

        private static int Rank(AchievementStatus a) => a.Complete && !a.Claimed ? 0 : (a.Claimed ? 2 : 1);

        /// <summary>
        /// Queues an "Achievement unlocked" banner for each newly completed achievement (once per achievement;
        /// remembered in the save so it never repeats).
        /// </summary>
        public static void CheckAchievementNotices(AppServices s)
        {
            if (s?.Achievements == null) return;
            bool any = false;
            foreach (var st in s.Achievements.All())
            {
                if (!st.Complete || st.Claimed) continue;
                string key = NotifiedPrefix + st.Def.Id;
                if (s.Save.TutorialsSeen.Contains(key)) continue;
                s.Save.TutorialsSeen.Add(key);
                any = true;
                UnlockNotifications.Show(Loc.T("Achievement unlocked!"), Loc.T(st.Def.Title) + "  ·  +" + st.Def.RewardCoins, Icon.Star);
            }
            if (any) s.MarkDirty();
        }
    }

    /// <summary>Non-blocking banner that slides in from the top; queued so several unlocks show one after another.</summary>
    public static class UnlockNotifications
    {
        private struct Item { public string Title, Text; public Icon Icon; }
        private static readonly Queue<Item> Pending = new Queue<Item>();
        private static GameObject _current;

        public static int QueuedCount => Pending.Count + (_current != null ? 1 : 0);

        public static void Show(string title, string text, Icon icon = Icon.Star)
        {
            Pending.Enqueue(new Item { Title = title, Text = text, Icon = icon });
            if (_current == null) Next();
        }

        private static void Next()
        {
            if (Pending.Count == 0) { _current = null; return; }
            var item = Pending.Dequeue();
            var layer = Overlays.Layer;
            var host = layer.GetComponent<MonoBehaviour>();
            var safe = UIFactory.SafeArea(layer);
            safe.name = "UnlockNotice";
            _current = safe.gameObject;
            var bg = UIFactory.Panel(safe, "Banner", new Color(0.16f, 0.1f, 0.34f, 0.97f), UIStyle.RadiusMedium);
            var holder = (RectTransform)bg.transform.parent;
            UIFactory.AddOutline(bg, UIStyle.Sunshine, 3f);
            Vector2 shown = new Vector2(0, -40), hidden = new Vector2(0, 260);
            UIFactory.Place(holder, new Vector2(0.5f, 1f), new Vector2(960, 190), shown, new Vector2(0.5f, 1f));
            var icon = UIFactory.Image(holder, "Icon", SpriteLibrary.Get(item.Icon), UIStyle.Sunshine);
            UIFactory.Place(icon.rectTransform, new Vector2(0, 0.5f), new Vector2(110, 110), new Vector2(30, 0), new Vector2(0, 0.5f));
            var t = UIFactory.Text(holder, item.Title, UIStyle.FontLabel, UIStyle.Sunshine, TextAnchor.MiddleLeft);
            t.rectTransform.offsetMin = new Vector2(160, 90); t.rectTransform.offsetMax = new Vector2(-24, -14);
            var b = UIFactory.Text(holder, item.Text, UIStyle.FontCaption, Color.white, TextAnchor.MiddleLeft, FontStyle.Normal);
            b.rectTransform.offsetMin = new Vector2(160, 16); b.rectTransform.offsetMax = new Vector2(-24, -96);
            foreach (var g in holder.GetComponentsInChildren<Graphic>()) g.raycastTarget = false;
            var go = _current;
            if (host == null) { UnityEngine.Object.Destroy(go); _current = null; return; }
            if (!MotionPrefs.Reduced) Tween.Move(host, holder, hidden, shown, 0.35f, Ease.OutBack);
            Tween.Delay(host, 2.6f, () =>
            {
                if (go == null) { Next(); return; }
                if (MotionPrefs.Reduced) { UnityEngine.Object.Destroy(go); Next(); return; }
                Tween.Move(host, holder, shown, hidden, 0.3f, Ease.InCubic, () => { if (go != null) UnityEngine.Object.Destroy(go); Next(); });
            });
        }
    }
}
