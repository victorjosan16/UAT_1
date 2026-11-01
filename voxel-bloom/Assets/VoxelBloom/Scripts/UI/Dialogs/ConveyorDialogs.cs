using System;
using UnityEngine;
using VoxelBloom.Core;

namespace VoxelBloom
{
    /// <summary>Victory: stars, "Nivel N Finalizat!", glossy cubes, reward (coins / gems), Următorul and Reia.</summary>
    public sealed class ConveyorWinDialog : DialogBase
    {
        [NonSerialized] public CompletionReceipt Receipt;
        [NonSerialized] public ConveyorLevel Level;
        protected override string Title => "Nivel " + (Level != null ? Level.Id : 0) + " Finalizat!";
        protected override Vector2 Size => new Vector2(900, 1300);
        protected override bool ShowClose => false;

        protected override void OnBuild()
        {
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
            var t = UI.Text(Body, "Banda e plină cu cuburi\nde care nu are nevoie nicio tavă.", 46, Theme.Ink);
            t.Wrap = true;
            t.rectTransform.TopBand(70, 170, 10);
            float y = 600;
            if (Controller.CanUndo)
            {
                AddBooster(BoosterType.Undo, Icon.Restart, ref y);
            }
            if (Controller.HasPlusSlot)
            {
                AddBooster(BoosterType.ExtraStation, Icon.Plus, ref y);
            }
            var retry = UI.Button(Body, "Reîncearcă", ButtonStyle.Green, new Vector2(640, 140), Icon.Restart, 58);
            ((RectTransform)retry.transform).Place(new Vector2(0.5f, 0), new Vector2(0, 230), new Vector2(640, 140));
            retry.OnClick = () => { Controller.AcceptFailure(); Close(); Controller.Restart(); };
            var quit = UI.Button(Body, "Ieșire", ButtonStyle.White, new Vector2(460, 110), Icon.Home, 48);
            ((RectTransform)quit.transform).Place(new Vector2(0.5f, 0), new Vector2(0, 80), new Vector2(460, 110));
            quit.OnClick = () => { Controller.AcceptFailure(); Close(); Controller.QuitToMap(); };
            UI.Text(Body, "Reîncercarea costă o viață", 34, Theme.InkSoft).rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0, 330), new Vector2(700, 44));
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
        protected override Vector2 Size => new Vector2(860, 900);

        protected override void OnBuild()
        {
            Add("Continuă", ButtonStyle.Green, Icon.Play, 0, () => { Close(); Controller.Resume(); });
            Add("Reia nivelul", ButtonStyle.Blue, Icon.Restart, 1, () => Router.Open<ConfirmDialog>(d =>
            {
                d.Message = Controller.Session != null && Controller.Session.Placed > 0 ? "Reiei nivelul?\nCostă o viață." : "Reiei nivelul?";
                d.YesLabel = "Da";
                d.OnYes = () => { Router.CloseAllDialogs(); Controller.Resume(); Controller.Restart(); };
            }));
            Add("Setări", ButtonStyle.Purple, Icon.Gear, 2, () => Router.Open<SettingsDialog>());
            Add("Ieși din nivel", ButtonStyle.White, Icon.Home, 3, () => { Router.CloseAllDialogs(); Controller.QuitToMap(); });
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
