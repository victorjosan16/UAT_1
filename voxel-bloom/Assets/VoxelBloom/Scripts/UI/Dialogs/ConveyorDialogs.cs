using System;
using UnityEngine;
using UnityEngine.UI;
using VoxelBloom.Core;

namespace VoxelBloom
{
    /// <summary>Victory: stars, "Nivel N Finalizat!", glossy cubes, reward (coins / gems), Următorul and Reia.</summary>
    public sealed class ConveyorWinDialog : DialogBase
    {
        [NonSerialized] public CompletionReceipt Receipt;
        [NonSerialized] public ConveyorLevel Level;
        protected override string Title => UIArt.Has("Win/cubes_podium") ? "" : "Nivel " + (Level != null ? Level.Id : 0) + " Finalizat!";
        protected override Vector2 Size => UIArt.Has("Win/cubes_podium") ? new Vector2(1000, 1760) : new Vector2(900, 1300);
        protected override bool ShowClose => false;

        protected override void OnBuild()
        {
            if (UIArt.Has("Win/cubes_podium")) { BuildArt(); return; }
            VB.VioletPanel(Panel);
            var stars = UI.Rect(Body, "Stars").Place(new Vector2(0.5f, 1), new Vector2(0, -130), new Vector2(700, 220));
            for (int i = 0; i < 3; i++)
            {
                bool on = i < Receipt.Stars;
                var s = UI.Image(stars, "Star", UIArt.Get(on ? "Icons/star_gold" : "Icons/star_empty") ?? SpriteFactory.Get(on ? Icon.Star : Icon.StarEmpty), Color.white, false);
                s.preserveAspect = true;
                float size = i == 1 ? 210 : 170;
                s.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 215, i == 1 ? 30 : -10), new Vector2(size, size));
                s.rectTransform.localRotation = Quaternion.Euler(0, 0, (1 - i) * 12);
                s.transform.localScale = Vector3.zero;
                int idx = i;
                Tween.Delay(0.2f + i * 0.25f, () =>
                {
                    if (s == null) return;
                    Tween.Scale(s.transform, Vector3.zero, Vector3.one, 0.45f, Ease.OutBack);
                    if (idx < Receipt.Stars) Services.Audio.Play(Sfx.Star, 1f + idx * 0.12f);
                });
            }
            // glossy cube trio
            var glow = UI.Image(Body, "Glow", SpriteFactory.Glow(), new Color(1f, 0.9f, 0.5f, 0.55f), false);
            glow.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -470), new Vector2(520, 420));
            Color[] cols = { Theme.Hex("#3ED6F0"), Theme.Hex("#D9259C"), Theme.Hex("#FFD93B") };
            Vector2[] pos = { new Vector2(0, -400), new Vector2(-85, -520), new Vector2(85, -520) };
            for (int i = 0; i < 3; i++)
            {
                var c = UI.Image(Body, "Cube", SpriteFactory.Glossy(28), cols[i], true);
                c.rectTransform.Place(new Vector2(0.5f, 1), pos[i], new Vector2(160, 160));
            }

            var reward = UI.Panel(Body, "Reward", Theme.PanelSoft, 40, false);
            var rr = (RectTransform)reward.transform.parent;
            rr.Place(new Vector2(0.5f, 1), new Vector2(0, -760), new Vector2(680, 210));
            UI.Text(rr, "Recompensă", 44, Theme.Ink).rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(600, 56));
            bool gems = Receipt.Gems > 0;
            var coin = UI.Icon(rr, Icon.Coin, 80); coin.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(gems ? -230 : -90, 64), new Vector2(80, 80));
            UI.Text(rr, "+" + Receipt.Coins, 58, Theme.Ink, TextAnchor.MiddleLeft).rectTransform.Place(new Vector2(0.5f, 0), new Vector2(gems ? -175 : -35, 64), new Vector2(220, 80), new Vector2(0, 0.5f));
            if (gems)
            {
                var gem = UI.Icon(rr, Icon.Gem, 80); gem.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(90, 64), new Vector2(80, 80));
                UI.Text(rr, "+" + Receipt.Gems, 58, Theme.Ink, TextAnchor.MiddleLeft).rectTransform.Place(new Vector2(0.5f, 0), new Vector2(145, 64), new Vector2(200, 80), new Vector2(0, 0.5f));
            }

            var next = UI.Button(Body, "Următorul", ButtonStyle.Green, new Vector2(640, 150), Icon.Play, 64);
            ((RectTransform)next.transform).Place(new Vector2(0.5f, 0), new Vector2(0, 230), new Vector2(640, 150));
            next.OnClick = () => { Close(); ConveyorController.Active?.NextLevel(); };
            var replay = UI.Button(Body, "Reia", ButtonStyle.Purple, new Vector2(460, 120), Icon.Restart, 52);
            ((RectTransform)replay.transform).Place(new Vector2(0.5f, 0), new Vector2(0, 80), new Vector2(460, 120));
            replay.OnClick = () => { Close(); ConveyorController.StartLevel(Level.Id); };
        }

        /// <summary>Victory from the supplied art: stars + ribbon with live "Nivel N Finalizat!", cubes on the podium, reward card with live amounts, Următorul / Reia.</summary>
        void BuildArt()
        {
            foreach (var n in new[] { "Face", "Shadow" }) { var t = Panel.Find(n); if (t != null) t.gameObject.SetActive(false); }
            var rays = UI.Image(Panel, "Rays", SpriteFactory.Rays(), new Color(1f, 0.95f, 0.6f, 0.5f), false);
            rays.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -780), new Vector2(1500, 1500));
            Image Art(string path, float y, float w)
            {
                var img = UIArt.Picture(Panel, path, w);
                img.rectTransform.anchorMin = img.rectTransform.anchorMax = new Vector2(0.5f, 1);
                img.rectTransform.anchoredPosition = new Vector2(0, y);
                return img;
            }
            var banner = Art("Collection/win_banner", -250, 860).rectTransform;
            // fewer than 3 stars: grey out the missing ones over the painted gold stars
            Vector2[] starPos = { new Vector2(0.2f, 0.66f), new Vector2(0.5f, 0.8f), new Vector2(0.8f, 0.66f) };
            for (int i = Receipt.Stars; i < 3; i++)
            {
                var e = UI.Image(banner, "StarEmpty", UIArt.Get("Icons/star_empty") ?? SpriteFactory.Get(Icon.StarEmpty), Color.white, false);
                e.preserveAspect = true;
                e.rectTransform.anchorMin = e.rectTransform.anchorMax = starPos[i];
                e.rectTransform.sizeDelta = Vector2.one * (i == 1 ? 270 : 240);
            }
            var title = UI.Text(banner, "Nivel <color=#FFD84A>" + Level.Id + "</color>\nFinalizat!", 80, Color.white, TextAnchor.MiddleCenter, 0.22f, Theme.Hex("#3A1582"));
            title.supportRichText = true; title.fontStyle = FontStyle.Bold; title.lineSpacing = 0.85f;
            title.rectTransform.anchorMin = new Vector2(0.18f, 0.08f); title.rectTransform.anchorMax = new Vector2(0.82f, 0.4f);
            title.rectTransform.offsetMin = title.rectTransform.offsetMax = Vector2.zero;
            title.rectTransform.localRotation = Quaternion.Euler(0, 0, 0);
            Art("Win/cubes_podium", -760, 640);
            var card = Art("Win/reward_card_blank", -1140, 780).rectTransform;
            VText Amount(float x, string v)
            {
                var t = UI.Text(card, v, 64, Theme.Hex("#2B1D6B"), TextAnchor.MiddleLeft); t.fontStyle = FontStyle.Bold;
                t.rectTransform.anchorMin = new Vector2(x, 0.42f); t.rectTransform.anchorMax = new Vector2(x + 0.27f, 0.9f);
                t.rectTransform.offsetMin = t.rectTransform.offsetMax = Vector2.zero; return t;
            }
            Amount(0.25f, "+" + Receipt.Coins); Amount(0.75f, "+" + Receipt.Gems);
            UIButton Btn(string path, float y, float w)
            {
                var sp = UIArt.Get(path); float h = w * sp.rect.height / sp.rect.width;
                var b = UI.Button(Panel, null, ButtonStyle.Green, new Vector2(w, h));
                ((RectTransform)b.transform).Place(new Vector2(0.5f, 1), new Vector2(0, y), new Vector2(w, h));
                UIArt.Skin(b, path); return b;
            }
            var next = Btn("Win/next", -1390, 640);
            next.OnClick = () => { Close(); ConveyorController.Active?.NextLevel(); };
            var replay = Btn("Win/replay", -1590, 540);
            replay.OnClick = () => { Close(); ConveyorController.StartLevel(Level.Id); };
            for (int i = 0; i < Receipt.Stars; i++) { int k = i; Tween.Delay(0.2f + i * 0.25f, () => Services.Audio.Play(Sfx.Star, 1f + k * 0.12f)); }
            Tween.Pop(banner, 0.08f, 0.5f);
        }

        public override void OnBack() { Close(); ConveyorController.Active?.NextLevel(); }
    }

    /// <summary>Deadlock: explains it and offers Undo / Extra station (paid if needed), Retry (one life) or Quit.</summary>
    public sealed class ConveyorFailDialog : DialogBase
    {
        public ConveyorController Controller;
        protected override string Title => "Bandă blocată!";
        protected override Vector2 Size => new Vector2(880, 1180);
        protected override bool ShowClose => false;

        protected override void OnBuild()
        {
            VB.VioletPanel(Panel);
            var t = UI.Text(Body, "Banda e plină cu cuburi\nde care nu are nevoie nicio tavă.", 46, Color.white);
            t.Wrap = true;
            t.rectTransform.TopBand(70, 170, 10);
            float y = 600;
            AddBooster(BoosterType.ColorShuffle, Icon.ColorSwap, ref y);
            if (Controller.HasPlusSlot) AddBooster(BoosterType.ExtraStation, Icon.CubesPlus, ref y);
            var retry = UI.Button(Body, "Reîncearcă", ButtonStyle.Green, new Vector2(640, 140), Icon.Restart, 58);
            ((RectTransform)retry.transform).Place(new Vector2(0.5f, 0), new Vector2(0, 230), new Vector2(640, 140));
            retry.OnClick = () => { Controller.AcceptFailure(); Close(); Controller.Restart(); };
            var quit = UI.Button(Body, "Ieșire", ButtonStyle.White, new Vector2(460, 110), Icon.Home, 48);
            ((RectTransform)quit.transform).Place(new Vector2(0.5f, 0), new Vector2(0, 80), new Vector2(460, 110));
            quit.OnClick = () => { Controller.AcceptFailure(); Close(); Controller.QuitToMap(); };
            UI.Text(Body, "Reîncercarea costă o viață", 34, new Color(1, 1, 1, 0.8f)).rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0, 330), new Vector2(700, 44));
        }

        void AddBooster(BoosterType type, Icon icon, ref float y)
        {
            int have = Services.Boosters.Count(type);
            int price = Services.Config.Booster(type).Price;
            string label = ConveyorController.BoosterName(type) + (have > 0 ? " (" + have + ")" : "  " + price);
            var b = UI.Button(Body, label, ButtonStyle.Gold, new Vector2(680, 130), have > 0 ? icon : Icon.Coin, 44);
            ((RectTransform)b.transform).Place(new Vector2(0.5f, 0), new Vector2(0, y), new Vector2(680, 130));
            b.OnClick = () => { if (Controller.ContinueWith(type)) { Services.Commit(); Close(); } };
            y -= 150;
        }

        public override void OnBack() { }
    }

    public sealed class ConveyorPauseDialog : DialogBase
    {
        public ConveyorController Controller;
        protected override string Title => "Pauză";
        protected override Vector2 Size => new Vector2(860, 1080);

        protected override void OnBuild()
        {
            VB.VioletPanel(Panel);
            Add("Continuă", ButtonStyle.Green, Icon.Play, 0, () => { Close(); Controller.Resume(); });
            Add("Reia nivelul", ButtonStyle.Blue, Icon.Restart, 1, () => Router.Open<ConfirmDialog>(d =>
            {
                d.Message = Controller.Session != null && Controller.Session.Placed > 0 ? "Reiei nivelul?\nCostă o viață." : "Reiei nivelul?";
                d.YesLabel = "Da";
                d.OnYes = () => { Router.CloseAllDialogs(); Controller.Resume(); Controller.Restart(); };
            }));
            Add("Setări", ButtonStyle.Purple, Icon.Gear, 2, () => Router.Open<SettingsVioletDialog>());
            Add("Cum se joacă?", ButtonStyle.Gold, Icon.Info, 3, () => Router.Open<HowToPlayDialog>());
            Add("Ieși din nivel", ButtonStyle.White, Icon.Home, 4, () => { Router.CloseAllDialogs(); Controller.QuitToMap(); });
        }

        void Add(string label, ButtonStyle style, Icon icon, int row, Action a)
        {
            var b = UI.Button(Body, label, style, new Vector2(620, 140), icon, 56);
            ((RectTransform)b.transform).Place(new Vector2(0.5f, 1), new Vector2(0, -90 - row * 175), new Vector2(620, 140));
            b.OnClick = a;
        }

        public override void OnBack() { Close(); Controller.Resume(); }
        protected override bool ShowClose => true;
        void OnDestroy() { if (Controller != null && !Router.HasDialog) Controller.Resume(); }
    }
}
