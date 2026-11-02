using System;
using UnityEngine;
using UnityEngine.UI;
using VoxelBloom.Core;

namespace VoxelBloom
{
    /// <summary>
    /// A supplied full-page picture (Menus/*.png) fitted to the screen width, with live overlays and tap areas placed in
    /// picture pixels. Used by the art versions of Boostere, Evenimente, Setări and Cum se joacă.
    /// </summary>
    public sealed class ArtPage
    {
        public readonly RectTransform Root;
        readonly float pw, ph;

        public static ArtPage TryCreate(Transform parent, string path)
        {
            var sp = UIArt.Get(path, default, false);
            return sp == null ? null : new ArtPage(parent, sp);
        }

        ArtPage(Transform parent, Sprite sp)
        {
            pw = sp.rect.width; ph = sp.rect.height;
            var img = UI.Image(parent, "Page", sp, Color.white, false, true);
            Root = img.rectTransform;
            Root.anchorMin = new Vector2(0, 0.5f); Root.anchorMax = new Vector2(1, 0.5f); Root.pivot = new Vector2(0.5f, 0.5f);
            Root.offsetMin = Root.offsetMax = Vector2.zero;
            var fit = img.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight; fit.aspectRatio = pw / ph;
        }

        /// <summary>A child stretched over a picture rectangle (x0, y0, x1, y1 in picture pixels, y down).</summary>
        public RectTransform Area(string name, float x0, float y0, float x1, float y1)
        {
            var r = UI.Rect(Root, name);
            r.anchorMin = new Vector2(x0 / pw, 1f - y1 / ph); r.anchorMax = new Vector2(x1 / pw, 1f - y0 / ph);
            r.offsetMin = r.offsetMax = Vector2.zero;
            return r;
        }

        public UIButton Hit(float x0, float y0, float x1, float y1, Action onClick, Sfx sound = Sfx.Button)
        {
            var r = Area("Hit", x0, y0, x1, y1);
            var img = r.gameObject.AddComponent<Image>(); img.color = new Color(1, 1, 1, 0);
            var b = r.gameObject.AddComponent<UIButton>(); b.Face = r; b.OnClick = onClick; b.Sound = sound;
            return b;
        }

        public VText Text(float x0, float y0, float x1, float y1, string text, Color color, TextAnchor align = TextAnchor.MiddleCenter, float outline = 0f, Color? outlineColor = null)
        {
            var t = UI.Text(Area("Text", x0, y0, x1, y1), text, 120, color, align, outline, outlineColor);
            t.fontStyle = FontStyle.Bold; t.resizeTextForBestFit = true; t.resizeTextMinSize = 10; t.resizeTextMaxSize = 120;
            t.rectTransform.Stretch();
            return t;
        }

        public Image Picture(string path, float x0, float y0, float x1, float y1)
        {
            var img = UI.Image(Area(System.IO.Path.GetFileName(path), x0, y0, x1, y1), "Img", UIArt.Get(path), Color.white, false);
            img.preserveAspect = true; img.rectTransform.Stretch();
            return img;
        }

        /// <summary>Live green fill over a painted (emptied) progress bar, with its text.</summary>
        public void Bar(float x0, float y0, float x1, float y1, float value, string label)
        {
            var area = Area("Bar", x0, y0, x1, y1);
            var fill = UI.Image(area, "Fill", UIArt.Get("HUD/progress_fill", new Vector4(26, 20, 26, 20)) ?? SpriteFactory.Rounded(20), UIArt.Has("HUD/progress_fill") ? Color.white : Theme.Hex("#45D85A"), true);
            fill.rectTransform.anchorMin = new Vector2(0, 0.08f); fill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(value), 0.92f);
            fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
            fill.gameObject.SetActive(value > 0.001f);
            var t = UI.Text(area, label, 60, Color.white, TextAnchor.MiddleCenter, 0.2f, Theme.Hex("#1A1450"));
            t.fontStyle = FontStyle.Bold; t.resizeTextForBestFit = true; t.resizeTextMinSize = 10; t.resizeTextMaxSize = 60; t.rectTransform.Stretch();
        }
    }

    // ====================================================================== Boostere (art)
    public static class BoostersArt
    {
        static readonly int[] Rows = { 366, 588, 804 };
        static readonly string[] Names = { "Reîmprospătează", "Amestecă culorile", "Adaugă o tavă" };
        static readonly string[] Desc = { "Schimbă ordinea tăvilor din coadă.", "Rearanjează culorile cuburilor de pe bandă.", "Deschide o stație în plus (slotul +)." };
        static readonly string[] Art = { null, "Boosters/color_swap", "Boosters/add_tray" };

        /// <summary>Builds the supplied Boostere page; returns false if the art is missing.</summary>
        public static bool Build(Transform parent, Action onBack, Action changed)
        {
            var page = ArtPage.TryCreate(parent, "Menus/boosters");
            if (page == null) return false;
            page.Hit(28, 45, 190, 210, onBack);
            var coins = Wallet(page, "HUD/home_pill_coins_blank", 668, 40, 988, 135, 0.52f, 0.42f);
            var gems = Wallet(page, "HUD/home_pill_gems_blank", 690, 140, 988, 225, 0.565f, 0.34f);
            void RefreshWallet() { coins.text = Services.Wallet.Coins.ToString("N0", System.Globalization.CultureInfo.InvariantCulture).Replace(",", " "); gems.text = Services.Save.Gems.ToString(); }
            RefreshWallet();
            for (int i = 0; i < 3; i++)
            {
                int c = Rows[i]; var t = BoostersInfoDialog.All[i].t;
                if (Art[i] != null) page.Picture(Art[i], 78, c - 96, 272, c + 96);
                var count = page.Text(216, c - 98, 272, c - 50, Services.Boosters.Count(t).ToString(), Color.white, TextAnchor.MiddleCenter, 0.2f, Theme.Hex("#7A0A14"));
                if (Art[i] == null) count.transform.parent.gameObject.SetActive(false);
                page.Text(296, c - 86, 664, c - 26, Names[i], Theme.Hex("#2B1D6B"), TextAnchor.MiddleLeft);
                var d = page.Text(296, c - 20, 664, c + 70, Desc[i], Theme.Hex("#3F2E86"), TextAnchor.UpperLeft); d.Wrap = true; d.fontStyle = FontStyle.Normal; d.resizeTextMaxSize = 34;
                int price = Services.Config.Booster(t).Price;
                page.Text(796, c - 26, 916, c + 42, price.ToString(), Color.white, TextAnchor.MiddleCenter, 0.2f, Theme.Hex("#1F6B1F"));
                page.Hit(668, c - 52, 946, c + 52, () =>
                {
                    if (Services.Boosters.Buy(t, Services.Config, Services.Wallet) != PurchaseResult.Success) { VB.Toast("Nu ai destule monede", Icon.Coin); return; }
                    Services.Commit(); Services.Audio.Play(Sfx.Coin);
                    count.text = Services.Boosters.Count(t).ToString(); RefreshWallet(); changed?.Invoke();
                }, Sfx.Coin);
            }
            return true;
        }

        static VText Wallet(ArtPage page, string path, float x0, float y0, float x1, float y1, float cx, float w)
        {
            page.Picture(path, x0, y0, x1, y1);
            float tx0 = x0 + (x1 - x0) * (cx - w / 2), tx1 = x0 + (x1 - x0) * (cx + w / 2);
            return page.Text(tx0, y0 + (y1 - y0) * 0.18f, tx1, y1 - (y1 - y0) * 0.18f, "0", Color.white, TextAnchor.MiddleCenter, 0.18f, Theme.Hex("#0E1430"));
        }
    }

    // ====================================================================== Evenimente (art)
    public static class EventsArt
    {
        static readonly int[] Rows = { 325, 545, 780, 1010, 1235 };

        public static bool Build(Transform parent, Action onBack, Action rebuild)
        {
            var page = ArtPage.TryCreate(parent, "Menus/events");
            if (page == null) return false;
            page.Hit(20, 30, 180, 185, onBack);
            var ev = Services.Events; long now = Services.Now; var s = Services.Save; ev.Refresh(now);
            page.Bar(322, 350, 758, 402, s.DailyChallengeCount / (float)ConveyorEvents.DailyGoal, s.DailyChallengeClaimed ? "Revino mâine" : Mathf.Min(s.DailyChallengeCount, ConveyorEvents.DailyGoal) + "/" + ConveyorEvents.DailyGoal);
            page.Bar(322, 577, 758, 629, s.MissionCubes / (float)ConveyorEvents.MissionGoal, s.MissionCubes + "/" + ConveyorEvents.MissionGoal);
            long season = Math.Max(0, ConveyorEvents.SeasonEndsAt(now) - now);
            page.Text(530, 806, 700, 864, (season / 86400) + "z " + (season % 86400 / 3600) + "h", Color.white, TextAnchor.MiddleCenter, 0.15f, Theme.Hex("#2A1466"));
            page.Bar(322, 1034, 758, 1086, s.SeasonStars / (float)ConveyorEvents.WeeklyGoal, Mathf.Min(s.SeasonStars, ConveyorEvents.WeeklyGoal) + "/" + ConveyorEvents.WeeklyGoal);
            long day = Math.Max(0, ConveyorEvents.DayEndsAt(now) - now);
            page.Text(530, 1258, 700, 1314, s.BonusClaimed ? "Luat!" : (day / 3600) + "h " + (day % 3600 / 60) + "m", Color.white, TextAnchor.MiddleCenter, 0.15f, Theme.Hex("#2A1466"));
            Gift(page, Rows[0], ev.CanClaimDaily(now), s.DailyChallengeClaimed, () => Show(ev.ClaimDaily(now), rebuild));
            Gift(page, Rows[1], ev.CanClaimMission, false, () => Show(ev.ClaimMission(), rebuild));
            Gift(page, Rows[2], ev.CanClaimSeason(now), s.SeasonClaimed, () => Show(ev.ClaimSeason(now), rebuild));
            Gift(page, Rows[3], ev.CanClaimWeekly(now), s.WeeklyClaimed, () => Show(ev.ClaimWeekly(now), rebuild));
            Gift(page, Rows[4], ev.CanClaimBonus(now), s.BonusClaimed, () => Show(ev.ClaimBonus(now), rebuild));
            return true;
        }

        static void Gift(ArtPage page, int c, bool claimable, bool claimed, Action claim)
        {
            var b = page.Hit(785, c - 100, 962, c + 100, () =>
            {
                if (claimable) claim();
                else VB.Toast(claimed ? "Recompensa a fost deja luată" : "Termină întâi provocarea", Icon.Gift);
            }, claimable ? Sfx.Coin : Sfx.Button);
            if (claimable) { var glow = UI.Image(b.transform, "Glow", SpriteFactory.Glow(), new Color(1f, 0.9f, 0.4f, 0.55f), false); glow.rectTransform.Stretch(-40, -40, -40, -40); glow.transform.SetAsFirstSibling(); Tween.Pop(b.transform, 0.1f, 0.6f); }
            if (claimed) { var ok = UI.Icon(b.transform, Icon.Check2, 80); ok.rectTransform.anchorMin = ok.rectTransform.anchorMax = new Vector2(0.8f, 0.2f); }
        }

        static void Show(EventReward r, Action rebuild)
        {
            Services.Commit();
            if (r.Coins + r.Gems + r.Boosters > 0)
            {
                Services.Audio.Play(Sfx.Win);
                var parts = new System.Collections.Generic.List<string>();
                if (r.Coins > 0) parts.Add("+" + r.Coins + " monede"); if (r.Gems > 0) parts.Add("+" + r.Gems + " gems"); if (r.Boosters > 0) parts.Add("+" + r.Boosters + " din fiecare booster");
                VB.Toast(string.Join(", ", parts), Icon.Gift);
            }
            rebuild?.Invoke();
        }
    }

    // ====================================================================== Setări (art)
    public static class SettingsArt
    {
        public static bool Build(Transform parent, Action onBack)
        {
            var page = ArtPage.TryCreate(parent, "Menus/settings");
            if (page == null) return false;
            if (onBack != null) page.Hit(25, 40, 195, 205, onBack);
            var st = Services.Save.Settings;
            void Apply() { Services.NotifySettingsChanged(); Services.Commit(); }
            Toggle(page, 300, st.Sound, v => { st.Sound = v; Apply(); });
            Toggle(page, 412, st.Music, v => { st.Music = v; Apply(); });
            Toggle(page, 527, st.Vibration, v => { st.Vibration = v; Apply(); if (v) Haptics.Light(); });
            page.Hit(78, 600, 945, 690, () => VB.Toast("Momentan jocul este doar în limba română", Icon.Globe));
            var cloud = page.Text(612, 732, 870, 792, "", Color.white, TextAnchor.MiddleRight);
            void Cloud() { cloud.text = Services.Save.CloudSave ? "Pornit" : "Oprit"; cloud.color = Services.Save.CloudSave ? Theme.Hex("#2E9E3A") : Theme.Hex("#6A5E9A"); }
            Cloud();
            page.Hit(78, 715, 945, 805, () =>
            {
                Services.Save.CloudSave = !Services.Save.CloudSave; Services.Commit(); Cloud();
                if (Services.Save.CloudSave) VB.Toast("Sincronizarea are nevoie de Google Play Games (neconfigurat încă). Progresul rămâne salvat pe telefon.", Icon.Cloud);
            });
            page.Hit(78, 810, 945, 905, () => Router.Instance.Open<AboutDialog>());
            page.Hit(78, 910, 945, 1005, () => Router.Instance.Open<PrivacyDialog>());
            page.Hit(78, 1010, 945, 1100, () => Application.OpenURL("market://details?id=com.animatix.voxelbloom3d"));
            page.Hit(110, 1125, 290, 1305, () => Router.Instance.Open<HowToPlayDialog>());
            page.Hit(318, 1125, 498, 1305, () => Router.Instance.Show<LeaderboardScreen>());
            page.Hit(523, 1125, 703, 1305, () => Router.Instance.Show<LeaderboardScreen>());
            page.Hit(730, 1125, 910, 1305, () => Router.Instance.Open<RewardsDialog>());
            return true;
        }

        static void Toggle(ArtPage page, float y, bool value, Action<bool> changed)
        {
            var area = page.Area("Toggle", 742, y - 44, 912, y + 44);
            var t = VBToggle.Create(area, value, changed);
            var r = (RectTransform)t.transform; r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
        }
    }

    public sealed class PrivacyDialog : DialogBase
    {
        protected override string Title => "Confidențialitate";
        protected override Vector2 Size => new Vector2(860, 820);
        protected override void OnBuild()
        {
            VB.VioletPanel(Panel);
            var t = UI.Text(Body, "Voxel Bloom 3D nu colectează date personale.\n\nProgresul, monedele și setările sunt salvate doar pe acest telefon.\nNu există reclame sau urmărire.", 38, Color.white);
            t.Wrap = true; t.rectTransform.Stretch(20, 20, 40, 60);
        }
    }
}
