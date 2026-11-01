using System;
using UnityEngine;
using UnityEngine.UI;
using VoxelBloom.Core;

namespace VoxelBloom
{
    public sealed class PauseDialog : DialogBase
    {
        public GameplayController Controller;
        protected override string Title => "Pause";
        protected override Vector2 Size => new Vector2(860, 900);

        protected override void OnBuild()
        {
            Add("Resume", ButtonStyle.Green, Icon.Play, 0, () => { Close(); Controller.Resume(); });
            Add("Restart", ButtonStyle.Blue, Icon.Restart, 1, () => Router.Open<ConfirmDialog>(d =>
            {
                d.Message = Controller.Session != null && Controller.Session.Moves > 0 ? "Restart this level?\nIt costs one life." : "Restart this level?";
                d.OnYes = () => { Router.CloseAllDialogs(); Controller.Restart(); };
            }));
            Add("Settings", ButtonStyle.Purple, Icon.Gear, 2, () => Router.Open<SettingsDialog>());
            Add("Quit level", ButtonStyle.White, Icon.Home, 3, () => { Router.CloseAllDialogs(); Controller.QuitToMap(); });
        }

        void Add(string label, ButtonStyle style, Icon icon, int row, Action a)
        {
            var b = UI.Button(Body, label, style, new Vector2(620, 140), icon, 60);
            ((RectTransform)b.transform).Place(new Vector2(0.5f, 1), new Vector2(0, -90 - row * 175), new Vector2(620, 140));
            b.OnClick = a;
        }

        public override void OnBack() { Close(); Controller.Resume(); }
        protected override bool ShowClose => true;
        void OnDestroy() { if (Controller != null && App.State.Current == GameState.Paused && !Router.HasDialog) Controller.Resume(); }
    }

    public sealed class ConfirmDialog : DialogBase
    {
        public string Message = "Are you sure?";
        public string YesLabel = "Yes";
        public Action OnYes;
        protected override string Title => "Confirm";
        protected override Vector2 Size => new Vector2(820, 640);

        protected override void OnBuild()
        {
            var t = UI.Text(Body, Message, 50, Theme.Ink);
            t.Wrap = true;
            t.rectTransform.Stretch(10, 10, 40, 220);
            var yes = UI.Button(Body, YesLabel, ButtonStyle.Green, new Vector2(320, 130), null, 56);
            ((RectTransform)yes.transform).Place(new Vector2(0.5f, 0), new Vector2(175, 90), new Vector2(320, 130));
            yes.OnClick = () => { Close(); OnYes?.Invoke(); };
            var no = UI.Button(Body, "No", ButtonStyle.White, new Vector2(320, 130), null, 56);
            ((RectTransform)no.transform).Place(new Vector2(0.5f, 0), new Vector2(-175, 90), new Vector2(320, 130));
            no.OnClick = Close;
        }
    }

    public sealed class LevelCompleteDialog : DialogBase
    {
        [NonSerialized] public CompletionReceipt Receipt;
        [NonSerialized] public LevelDefinition Level;
        VText coinsText;
        UIButton doubleBtn;
        protected override string Title => "Level Complete!";
        protected override Vector2 Size => new Vector2(920, 1360);
        protected override bool ShowClose => false;

        protected override void OnBuild()
        {
            // rays + stars
            var stars = UI.Rect(Body, "Stars").Place(new Vector2(0.5f, 1), new Vector2(0, -150), new Vector2(700, 220));
            for (int i = 0; i < 3; i++)
            {
                bool on = i < Receipt.Stars;
                var s = UI.Icon(stars, on ? Icon.Star : Icon.StarEmpty, i == 1 ? 210 : 170);
                s.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 215, i == 1 ? 30 : -10), s.rectTransform.sizeDelta);
                s.rectTransform.localRotation = Quaternion.Euler(0, 0, (1 - i) * 12);
                s.transform.localScale = Vector3.zero;
                int idx = i;
                Tween.Delay(0.25f + i * 0.28f, () =>
                {
                    if (s == null) return;
                    Tween.Scale(s.transform, Vector3.zero, Vector3.one, 0.45f, Ease.OutBack);
                    if (idx < Receipt.Stars) Services.Audio.Play(Sfx.Star, 1f + idx * 0.12f);
                });
            }
            var thumbBg = UI.Image(Body, "Model", SpriteFactory.Rounded(40), Theme.Hex("#E3F4FF"));
            thumbBg.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -470), new Vector2(380, 380));
            var raw = UI.Raw(thumbBg.transform, "Thumb", ThumbnailRenderer.Get(Level.ModelId, Level.Grid, 384));
            raw.rectTransform.Stretch(10, 10, 10, 10);
            var name = UI.Text(Body, Level.Name + (Receipt.Hard ? "  ·  HARD" : ""), 46, Theme.InkSoft);
            name.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -700), new Vector2(780, 60));

            var reward = UI.Panel(Body, "Reward", Theme.PanelSoft, 40, false);
            var rr = (RectTransform)reward.transform.parent;
            rr.Place(new Vector2(0.5f, 1), new Vector2(0, -850), new Vector2(640, 190));
            var rt = UI.Text(rr, Receipt.Hard ? "Hard level reward" : "Reward", 40, Theme.InkSoft);
            rt.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -36), new Vector2(600, 50));
            UI.Icon(rr, Icon.Coin, 100).rectTransform.Place(new Vector2(0.5f, 0), new Vector2(-120, 60), new Vector2(100, 100));
            coinsText = UI.Text(rr, "+0", 80, Theme.Ink, TextAnchor.MiddleLeft);
            coinsText.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(-50, 60), new Vector2(320, 100), new Vector2(0, 0.5f));
            Tween.Value(0.9f, k => { if (coinsText != null) coinsText.text = "+" + Mathf.RoundToInt(Receipt.Coins * k); });
            Tween.Delay(0.9f, () => Services.Audio.Play(Sfx.Coin));

            var cont = UI.Button(Body, "Continue", ButtonStyle.Green, new Vector2(600, 150), Icon.Play, 64);
            ((RectTransform)cont.transform).Place(new Vector2(0.5f, 0), new Vector2(0, 230), new Vector2(600, 150));
            cont.OnClick = Continue;

            bool adsReady = Services.Ads.IsReady(AdPlacement.DoubleReward);
            doubleBtn = UI.Button(Body, adsReady ? "Double Reward" : "Double Reward (unavailable)", ButtonStyle.Purple, new Vector2(600, 110), Icon.Video, adsReady ? 44 : 34);
            ((RectTransform)doubleBtn.transform).Place(new Vector2(0.5f, 0), new Vector2(0, 70), new Vector2(600, 110));
            doubleBtn.SetInteractable(adsReady);
            doubleBtn.OnClick = DoubleReward;
        }

        void DoubleReward()
        {
            doubleBtn.SetInteractable(false);
            var ads = Services.Ads;
            ads.Show(AdPlacement.DoubleReward, outcome =>
            {
                if (RewardGate.MayGrant(ads, outcome) && Services.Progress.TryDoubleReward(Receipt))
                {
                    Services.Commit();
                    if (coinsText != null) coinsText.text = "+" + Receipt.Coins * 2;
                    Services.Audio.Play(Sfx.Coin);
                    Router.Toast("Reward doubled!", Icon.Coin);
                }
                else if (ads.IsTestProvider) Router.Toast("Test ad provider: no reward granted", Icon.Video);
                else Router.Toast("Ad not completed - no reward", Icon.Video);
            });
        }

        void Continue()
        {
            Close();
            int next = Level.Id + 1;
            if (next <= Services.Config.TotalLevels && Services.Progress.IsLevelUnlocked(next)) GameplayController.StartLevel(next);
            else
            {
                var ch = Services.Config.ChapterOf(Math.Min(next, Services.Config.TotalLevels));
                if (ch != null && !Services.Progress.IsChapterUnlocked(ch))
                    Router.Toast("Collect " + ch.UnlockStars + " stars to open " + ch.Name, Icon.Star);
                Router.Show<LevelSelectScreen>();
            }
        }
    }

    public sealed class LevelFailedDialog : DialogBase
    {
        public FailReason Reason;
        public GameplayController Controller;
        protected override string Title => "Out of luck!";
        protected override Vector2 Size => new Vector2(880, 1040);
        protected override bool ShowClose => false;

        protected override void OnBuild()
        {
            string msg = Reason == FailReason.QueueFull ? "The attack queue is jammed:\nno queued colour can reach the model."
                : Reason == FailReason.OutOfMoves ? "You ran out of moves." : "No blocks left on the board.";
            var t = UI.Text(Body, msg, 46, Theme.Ink);
            t.Wrap = true;
            t.rectTransform.TopBand(80, 200, 10);
            UI.Icon(Body, Icon.Heart, 120).rectTransform.Place(new Vector2(0.5f, 1), new Vector2(-70, -350), new Vector2(120, 120));
            UI.Text(Body, "-1", 70, Theme.Red).rectTransform.Place(new Vector2(0.5f, 1), new Vector2(60, -350), new Vector2(160, 100));

            float y = 420;
            if (Reason == FailReason.QueueFull)
            {
                int have = Services.Boosters.Count(BoosterType.Eliminate);
                int price = Services.Config.Booster(BoosterType.Eliminate).Price;
                var b = UI.Button(Body, have > 0 ? "Use Eliminare (" + have + ")" : "Eliminare  " + price, ButtonStyle.Gold, new Vector2(640, 140), have > 0 ? Icon.Eliminate : Icon.Coin, 50);
                ((RectTransform)b.transform).Place(new Vector2(0.5f, 0), new Vector2(0, y), new Vector2(640, 140));
                b.OnClick = () =>
                {
                    if (Services.Boosters.Count(BoosterType.Eliminate) <= 0)
                    {
                        if (Services.Boosters.Buy(BoosterType.Eliminate, Services.Config, Services.Wallet) != PurchaseResult.Success)
                        { Router.Toast("Not enough coins - win HARD levels for big rewards!", Icon.Coin); return; }
                    }
                    if (Controller.ContinueWithEliminate())
                    {
                        Services.Commit();
                        Close();
                    }
                };
                y -= 170;
            }
            var retry = UI.Button(Body, "Retry", ButtonStyle.Green, new Vector2(640, 140), Icon.Restart, 60);
            ((RectTransform)retry.transform).Place(new Vector2(0.5f, 0), new Vector2(0, y), new Vector2(640, 140));
            retry.OnClick = () =>
            {
                Controller.AcceptFailure();
                Close();
                Services.Lives.Update(Services.Now);
                if (Services.Lives.Count <= 0) { Controller.QuitToMap(); Router.Open<NoLivesDialog>(); }
                else Controller.Begin(Controller.Level, null);
            };
            var home = UI.Button(Body, "Give up", ButtonStyle.White, new Vector2(640, 120), Icon.Home, 50);
            ((RectTransform)home.transform).Place(new Vector2(0.5f, 0), new Vector2(0, y - 160), new Vector2(640, 120));
            home.OnClick = () => { Controller.AcceptFailure(); Close(); Controller.QuitToMap(); };
        }

        public override void OnBack() { }
    }

    /// <summary>BOOSTERS: icon, description, price, confirmation, purchase into persistent inventory.</summary>
    public sealed class BoostersDialog : DialogBase
    {
        [NonSerialized] public BoosterType? Focus;
        protected override string Title => "Boosters";
        protected override Vector2 Size => new Vector2(940, 1420);

        static readonly (BoosterType t, Icon i, string name, string desc)[] Items =
        {
            (BoosterType.Hammer, Icon.Hammer, "Hammer", "Destroy selected cubes (any colour)."),
            (BoosterType.Shuffle, Icon.Shuffle, "Shuffle", "Mix and refresh the blocks."),
            (BoosterType.ColorBomb, Icon.ColorBomb, "Color Bomb", "Remove a large group of the same colour."),
            (BoosterType.Eliminate, Icon.Eliminate, "Eliminare", "Remove a stuck attack from the queue."),
        };

        protected override void OnBuild()
        {
            for (int i = 0; i < Items.Length; i++) Row(Items[i], i);
            var coins = UI.Pill(Body, Icon.Coin, Services.Wallet.Coins.ToString("N0"), out var cr, 320, 84);
            cr.Place(new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(320, 84));
            gameObject.AddComponent<CoinsLabel>().Label = coins;
        }

        void Row((BoosterType t, Icon i, string name, string desc) it, int row)
        {
            var bg = UI.Panel(Body, "Row" + row, Focus == it.t ? Theme.Hex("#FFF6D6") : Theme.PanelSoft, 40, false);
            var rt = (RectTransform)bg.transform.parent;
            rt.TopBand(70 + row * 270, 240, 0);
            UI.Icon(rt, it.i, 150).rectTransform.Place(new Vector2(0, 0.5f), new Vector2(100, 0), new Vector2(150, 150));
            var n = UI.Text(rt, it.name, 50, Theme.Ink, TextAnchor.MiddleLeft);
            n.rectTransform.Place(new Vector2(0, 1), new Vector2(200, -60), new Vector2(400, 60), new Vector2(0, 0.5f));
            var d = UI.Text(rt, it.desc, 32, Theme.InkSoft, TextAnchor.UpperLeft);
            d.Wrap = true;
            d.rectTransform.Place(new Vector2(0, 1), new Vector2(200, -100), new Vector2(380, 110), new Vector2(0, 1));
            var owned = UI.Text(rt, "Owned: " + Services.Boosters.Count(it.t), 32, Theme.Purple, TextAnchor.MiddleLeft);
            owned.rectTransform.Place(new Vector2(0, 0), new Vector2(200, 34), new Vector2(300, 44), new Vector2(0, 0.5f));
            int price = Services.Config.Booster(it.t).Price;
            var buy = UI.Button(rt, price.ToString(), ButtonStyle.Green, new Vector2(230, 110), Icon.Coin, 46);
            ((RectTransform)buy.transform).Place(new Vector2(1, 0.5f), new Vector2(-140, 0), new Vector2(230, 110));
            buy.OnClick = () => Router.Open<ConfirmDialog>(c =>
            {
                c.Message = "Buy 1 " + it.name + " for " + price + " coins?";
                c.YesLabel = "Buy";
                c.OnYes = () =>
                {
                    var r = Services.Boosters.Buy(it.t, Services.Config, Services.Wallet);
                    if (r == PurchaseResult.Success)
                    {
                        Services.Commit();
                        Services.Audio.Play(Sfx.Coin);
                        owned.text = "Owned: " + Services.Boosters.Count(it.t);
                        Tween.Pop(owned.transform, 0.3f);
                    }
                    else Router.Toast("Not enough coins. HARD levels (every 5th) pay x2.5!", Icon.Coin);
                };
            });
        }
    }

    public sealed class CoinsLabel : MonoBehaviour
    {
        public VText Label;
        int shown = -1;
        void Update()
        {
            int c = Services.Wallet.Coins;
            if (c == shown || Label == null) return;
            shown = c;
            Label.text = c.ToString("N0");
        }
    }

    public sealed class SettingsDialog : DialogBase
    {
        protected override string Title => "Settings";
        protected override Vector2 Size => new Vector2(900, 1420);

        protected override void OnBuild()
        {
            var st = Services.Save.Settings;
            Toggle(0, Icon.Music, "Music", () => st.Music, v => st.Music = v);
            Toggle(1, Icon.Sound, "Sound", () => st.Sound, v => st.Sound = v);
            Toggle(2, Icon.Vibrate, "Vibration", () => st.Vibration, v => st.Vibration = v);
            Toggle(3, Icon.Sparkle, "Reduced effects", () => st.ReducedEffects, v => st.ReducedEffects = v);
            Toggle(4, Icon.Restart, "Rotate model 360°", () => st.AutoRotate, v => st.AutoRotate = v);
            var q = UI.Text(Body, "Graphics quality", 44, Theme.Ink, TextAnchor.MiddleLeft);
            q.rectTransform.TopBand(5 * 170 + 70, 60, 20);
            string[] names = { "Low", "Medium", "High" };
            var buttons = new UIButton[3];
            for (int i = 0; i < 3; i++)
            {
                int preset = i;
                buttons[i] = UI.Button(Body, names[i], st.Quality == i ? ButtonStyle.Purple : ButtonStyle.White, new Vector2(240, 110), null, 44);
                ((RectTransform)buttons[i].transform).Place(new Vector2(0.5f, 1), new Vector2((i - 1) * 260, -(5 * 170 + 200)), new Vector2(240, 110));
                buttons[i].OnClick = () => { st.Quality = preset; Services.NotifySettingsChanged(); Close(); Router.Open<SettingsDialog>(); };
            }
            var ver = UI.Text(Body, "Voxel Bloom 3D · " + Application.version + " · Ads: " + Services.Ads.ProviderName, 28, Theme.InkSoft);
            ver.rectTransform.BottomBand(10, 40, 0);
        }

        void Toggle(int row, Icon icon, string label, Func<bool> get, Action<bool> set)
        {
            var bg = UI.Panel(Body, label, Theme.PanelSoft, 36, false);
            var rt = (RectTransform)bg.transform.parent;
            rt.TopBand(60 + row * 170, 140, 0);
            UI.Icon(rt, icon, 100).rectTransform.Place(new Vector2(0, 0.5f), new Vector2(80, 0), new Vector2(100, 100));
            UI.Text(rt, label, 46, Theme.Ink, TextAnchor.MiddleLeft).rectTransform.Place(new Vector2(0, 0.5f), new Vector2(160, 0), new Vector2(400, 60), new Vector2(0, 0.5f));
            UIButton b = null;
            b = UI.Button(rt, get() ? "ON" : "OFF", get() ? ButtonStyle.Green : ButtonStyle.White, new Vector2(190, 100), null, 44);
            ((RectTransform)b.transform).Place(new Vector2(1, 0.5f), new Vector2(-120, 0), new Vector2(190, 100));
            b.OnClick = () =>
            {
                set(!get());
                Services.NotifySettingsChanged();
                var lbl = b.Face.Find("Label").GetComponent<VText>();
                lbl.text = get() ? "ON" : "OFF";
                UI.StyleColors(get() ? ButtonStyle.Green : ButtonStyle.White, out var face, out var lip, out var tc);
                b.Face.GetComponent<Image>().color = face;
                b.transform.Find("Lip").GetComponent<Image>().color = lip;
                lbl.color = tc;
            };
        }
    }

    /// <summary>Daily reward: one claim per UTC day, 7-day streak.</summary>
    public sealed class RewardsDialog : DialogBase
    {
        protected override string Title => "Daily Rewards";
        protected override Vector2 Size => new Vector2(940, 1100);

        protected override void OnBuild()
        {
            long now = Services.Now;
            var d = Services.Daily;
            int[] rewards = Services.Config.Economy.DailyRewards;
            bool can = d.CanClaim(now);
            int nextDay = (d.StreakIfClaimed(now) - 1) % rewards.Length;
            int doneUpTo = can ? nextDay - 1 : (d.Streak - 1) % rewards.Length;
            for (int i = 0; i < rewards.Length; i++)
            {
                int col = i % 4, row = i / 4;
                bool claimed = i <= doneUpTo;
                bool today = can && i == nextDay;
                var tile = UI.Panel(Body, "Day" + i, today ? Theme.Hex("#FFF2C2") : claimed ? Theme.Hex("#E6F8E3") : Theme.PanelSoft, 30, false);
                var rt = (RectTransform)tile.transform.parent;
                rt.Place(new Vector2(0, 1), new Vector2(105 + col * 205, -150 - row * 260), new Vector2(190, 240));
                UI.Text(rt, "Day " + (i + 1), 34, Theme.InkSoft).rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(180, 44));
                UI.Icon(rt, claimed ? Icon.Check : (i == rewards.Length - 1 ? Icon.Gift : Icon.Coin), 100).rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(100, 100));
                UI.Text(rt, rewards[i].ToString(), 40, Theme.Ink).rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0, 36), new Vector2(180, 50));
            }
            var claim = UI.Button(Body, can ? "Claim " + d.PendingReward(now) : "Come back tomorrow", can ? ButtonStyle.Green : ButtonStyle.White, new Vector2(640, 150), can ? Icon.Coin : Icon.Calendar, 56);
            ((RectTransform)claim.transform).Place(new Vector2(0.5f, 0), new Vector2(0, 120), new Vector2(640, 150));
            claim.SetInteractable(can);
            claim.OnClick = () =>
            {
                int got = Services.Daily.Claim(Services.Now, Services.Wallet);
                if (got > 0)
                {
                    Services.Commit();
                    Services.Audio.Play(Sfx.Coin);
                    Router.Toast("+" + got + " coins", Icon.Coin);
                }
                Close();
            };
        }
    }

    public sealed class NoLivesDialog : DialogBase
    {
        const int RefillPrice = 400;
        VText timer;
        protected override string Title => "No lives";
        protected override Vector2 Size => new Vector2(860, 900);

        protected override void OnBuild()
        {
            UI.Icon(Body, Icon.Heart, 200).rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -170), new Vector2(200, 200));
            timer = UI.Text(Body, "", 50, Theme.Ink);
            timer.rectTransform.TopBand(300, 80, 0);
            var refill = UI.Button(Body, "Refill  " + RefillPrice, ButtonStyle.Green, new Vector2(600, 140), Icon.Coin, 56);
            ((RectTransform)refill.transform).Place(new Vector2(0.5f, 0), new Vector2(0, 240), new Vector2(600, 140));
            refill.OnClick = () =>
            {
                if (Services.Wallet.TrySpend(RefillPrice)) { Services.Lives.Refill(); Services.Commit(); Services.Audio.Play(Sfx.Coin); Close(); }
                else Router.Toast("Not enough coins", Icon.Coin);
            };
            var ok = UI.Button(Body, "OK", ButtonStyle.White, new Vector2(600, 120), null, 52);
            ((RectTransform)ok.transform).Place(new Vector2(0.5f, 0), new Vector2(0, 80), new Vector2(600, 120));
            ok.OnClick = Close;
        }

        void Update()
        {
            long s = Services.Lives.SecondsToNext(Services.Now);
            timer.text = Services.Lives.Count > 0 ? "You have a life!" : "Next life in " + (s / 60).ToString("00") + ":" + (s % 60).ToString("00");
        }
    }

    public sealed class ProfileDialog : DialogBase
    {
        protected override string Title => "Profile";
        protected override Vector2 Size => new Vector2(860, 940);

        protected override void OnBuild()
        {
            UI.Icon(Body, Icon.Avatar, 220).rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -170), new Vector2(220, 220));
            var s = Services.Save;
            int completed = s.Stars.Count;
            string text = "Levels completed: " + completed + "/" + Services.Config.TotalLevels +
                          "\nStars: " + s.TotalStars + "\nModels collected: " + s.CollectionUnlocked.Count + "/" + Services.Config.Collection.Count +
                          "\nTotal score: " + s.TotalScore.ToString("N0");
            var t = UI.Text(Body, text, 46, Theme.Ink);
            t.rectTransform.TopBand(320, 360, 0);
        }
    }
}
