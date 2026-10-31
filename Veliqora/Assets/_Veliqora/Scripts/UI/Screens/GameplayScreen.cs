using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Veliqora.Core.Ads;
using Veliqora.Core.Economy;
using Veliqora.Core.Levels;
using Veliqora.Core.Session;
using Veliqora.Gameplay;
using Veliqora.Infrastructure;
using Veliqora.Presentation;
using Veliqora.UI.Widgets;

namespace Veliqora.UI.Screens
{
    /// <summary>Gameplay: HUD, responsive board, booster bar, contextual tutorial, stuck + win flow.</summary>
    public sealed class GameplayScreen : ScreenBase
    {
        private GameplayController _ctl;
        private BoardView _board;
        private Text _levelText, _movesText, _goalText;
        private readonly Dictionary<BoosterType, BoosterButton> _boosters = new Dictionary<BoosterType, BoosterButton>();
        private CoachMark _coach;
        private string _activeTutorial;
        private int _tutorialStep;
        private OverlayHandle _pause;
        private GameObject _stuckPanel;
        private LevelDefinition _level;

        /// <summary>Exposed for PlayMode tests and tooling.</summary>
        public GameplayController Controller => _ctl;
        public BoardView Board => _board;

        private sealed class BoosterButton
        {
            public ButtonHandle Button;
            public Image Badge;
            public Text Count;
        }

        protected override void BuildBackground(RectTransform full)
        {
            var theme = S.Themes.Theme;
            bool light = S.Settings.Data.LightGameplayBackground;
            if (!light && theme.IsDark)
            {
                // Dark scenes are designed low-contrast already; a light veil keeps the neon liquids dominant.
                FullBackground(full, BackgroundArt.Valley(theme));
                ReadabilityScrim(full, 0.45f, 0.5f, 0.22f);
                return;
            }
            var art = light ? null : BackgroundArt.ThemeArt(S.Themes.ThemeId);
            if (art != null)
            {
                // Themed scene behind a navy veil: atmosphere without hurting liquid readability.
                FullBackground(full, art);
                ReadabilityScrim(full, 0.55f, 0.6f, 0.38f);
                return;
            }
            var raw = UIFactory.Raw(full, "Background", BackgroundArt.GameplayGradient(theme.GameTop, theme.GameBottom, light), Color.white);
            UIFactory.Stretch(raw.rectTransform);
        }

        protected override void Build(object args)
        {
            int number = args is int n ? n : S.Save.Progress.CurrentLevel;
            number = Mathf.Clamp(number, 1, S.Levels.Count);
            if (number > S.Save.Progress.HighestUnlocked) number = S.Save.Progress.HighestUnlocked;
            _level = S.Levels.Get(number);
            bool light = S.Settings.Data.LightGameplayBackground;
            Color hudText = light ? UIStyle.InkOnLight : Color.white;

            // HUD
            var bar = TopBar();
            var pause = UIFactory.IconButton(bar, Icon.Pause, UIStyle.Violet, 116, OpenPause);
            UIFactory.Place(pause.Rect, new Vector2(0, 0.5f), pause.Rect.sizeDelta, Vector2.zero, new Vector2(0, 0.5f));
            AddCoins(bar);
            _levelText = UIFactory.Text(bar, "Level " + number, UIStyle.FontHeading, hudText);
            var stageText = UIFactory.Text(bar, "Stage " + _level.Stage, UIStyle.FontCaption, UIStyle.WithAlpha(hudText, 0.7f), TextAnchor.MiddleCenter, FontStyle.Normal);
            UIFactory.Place(stageText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(300, 44), new Vector2(-40, 66));
            UIFactory.Place(_levelText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(420, 90), new Vector2(-40, 18));
            UIFactory.AddShadow(_levelText, new Color(0, 0, 0, light ? 0.08f : 0.3f), new Vector2(0, -3));
            _movesText = UIFactory.Text(bar, "", UIStyle.FontCaption, UIStyle.WithAlpha(hudText, 0.85f), TextAnchor.MiddleCenter, FontStyle.Normal);
            UIFactory.Place(_movesText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(460, 50), new Vector2(-40, -40));

            if (_level.IsHard)
            {
                bool boss = _level.Kind == LevelKind.Boss;
                var badge = UIFactory.Image(Safe, "HardBadge", SpriteLibrary.Rounded(30), boss ? UIStyle.Violet : UIStyle.Danger);
                UIFactory.Place(badge.rectTransform, new Vector2(0.5f, 1f), new Vector2(boss ? 380 : 340, 64), new Vector2(-40, -UIStyle.TopBarHeight - 4), new Vector2(0.5f, 1f));
                UIFactory.AddOutline(badge, new Color(1, 1, 1, 0.6f), 2f);
                var bt = UIFactory.Text(badge.transform, boss ? "BOSS LEVEL" : "HARD LEVEL", UIStyle.FontCaption, Color.white);
                if (boss)
                {
                    var crown = UIFactory.Image(badge.transform, "Crown", SpriteLibrary.Get(Icon.Crown), Color.white);
                    UIFactory.Place(crown.rectTransform, new Vector2(0, 0.5f), new Vector2(56, 56), new Vector2(14, 2), new Vector2(0, 0.5f));
                }
                if (!MotionPrefs.Reduced) Tween.Pulse(this, badge.rectTransform, 0.05f, 0.9f);
            }
            _goalText = UIFactory.Text(Safe, "", UIStyle.FontCaption, UIStyle.WithAlpha(hudText, 0.8f), TextAnchor.MiddleCenter, FontStyle.Normal);
            UIFactory.Band(_goalText.rectTransform, 1f, 60, -UIStyle.TopBarHeight - (_level.IsHard ? 82 : 30), 40, 1f);

            AmbientMotes(3f);
            BottleView.NeonAura = !light;

            // Board (between HUD and booster bar)
            var boardRt = UIFactory.Rect("Board", Safe);
            boardRt.offsetMin = new Vector2(30, 360);
            boardRt.offsetMax = new Vector2(-30, -UIStyle.TopBarHeight - 110);
            _board = boardRt.gameObject.AddComponent<BoardView>();
            _board.Init(S.Themes.Bottle(_level.Capacity), _level.Capacity, S.Settings.Data.ColorblindSymbols);
            if (light) _board.SetGlassColor(UIStyle.Hex("#7F8DB0"));

            // Booster bar
            var boosterBar = UIFactory.Rect("Boosters", Safe);
            UIFactory.Band(boosterBar, 0f, 300, 40, 40, 0f);
            AddBooster(boosterBar, BoosterType.Undo, Icon.Undo, "Undo", UIStyle.Hex("#3D8BFF"), 0.125f);
            AddBooster(boosterBar, BoosterType.ExtraBottle, Icon.BottlePlus, "Add bottle", UIStyle.Hex("#36B0C9"), 0.375f);
            AddBooster(boosterBar, BoosterType.Shuffle, Icon.Shuffle, "Shuffle", UIStyle.Hex("#8E5CF7"), 0.625f);
            AddBooster(boosterBar, BoosterType.Hint, Icon.Bulb, "Hint", UIStyle.Hex("#F5A623"), 0.875f);

            _coach = CoachMark.Create(Safe);

            Canvas.ForceUpdateCanvases();
            _ctl = new GameplayController(S, _level, _board, this);
            _ctl.StateChanged += Refresh;
            _ctl.Won += OnWon;
            _ctl.Stuck += ShowStuck;
            _ctl.Notice += msg => Overlays.Toast(msg, 2.6f);
            _ctl.Tapped += OnTapped;
            _board.Build(_ctl.Session.Current, true);
            S.Economy.Changed += Refresh;
            S.Settings.Changed += OnSettingsChanged;
            Refresh();
            StartTutorial();
            if (_level.IsHard && _ctl.Session.MoveCount == 0)
            {
                int bonus = _level.Kind == LevelKind.Boss ? S.EconomyConfig.BossLevelBonus : S.EconomyConfig.HardLevelBonus;
                bool firstClear = !S.Save.Progress.IsCompleted(_level.Number);
                Tween.Delay(this, 0.5f, () => Overlays.Toast((_level.Kind == LevelKind.Boss ? "Boss level — end of stage " + _level.Stage + "!" : "Hard level!")
                    + (firstClear ? "  Bonus +" + bonus + " coins" : ""), 2.4f));
            }
        }

        private void OnDestroy()
        {
            if (S == null) return;
            S.Economy.Changed -= Refresh;
            S.Settings.Changed -= OnSettingsChanged;
            _ctl?.Dispose();
        }

        private void OnSettingsChanged()
        {
            if (_board != null) _board.SetSymbols(S.Settings.Data.ColorblindSymbols);
        }

        private void AddBooster(RectTransform parent, BoosterType type, Icon icon, string label, Color color, float x)
        {
            var holder = UIFactory.Rect("Booster_" + type, parent);
            UIFactory.Place(holder, new Vector2(x, 0.5f), new Vector2(220, 280), Vector2.zero);
            var b = UIFactory.IconButton(holder, icon, color, 168, () => OnBooster(type));
            UIFactory.Place(b.Rect, new Vector2(0.5f, 1f), b.Rect.sizeDelta, new Vector2(0, -6), new Vector2(0.5f, 1f));
            var badge = UIFactory.Image(b.Rect, "Badge", SpriteLibrary.Circle, UIStyle.Coral);
            UIFactory.Place(badge.rectTransform, new Vector2(1, 1), new Vector2(66, 66), new Vector2(6, 6), new Vector2(1, 1));
            UIFactory.AddOutline(badge, Color.white, 3);
            var count = UIFactory.Text(badge.transform, "0", UIStyle.FontCaption, Color.white);
            var t = UIFactory.Text(holder, label, UIStyle.FontCaption, S.Settings.Data.LightGameplayBackground ? UIStyle.InkOnLight : Color.white);
            UIFactory.Band(t.rectTransform, 0f, 60, 0, 0, 0f);
            UIFactory.AddShadow(t, new Color(0, 0, 0, 0.3f), new Vector2(0, -2));
            _boosters[type] = new BoosterButton { Button = b, Badge = badge, Count = count };
        }

        private void Refresh()
        {
            if (_ctl == null) return;
            var sess = _ctl.Session;
            _movesText.text = "Moves: " + sess.MoveCount + (sess.ShuffleCount > 0 ? "  ·  shuffled" : "");
            int three = StarRating.ThreeStarLimit(sess.ParMoves);
            _goalText.text = "No move limit  ·  ★★★ in " + three + " moves or fewer" + (_level.EmptyBottleCount == 1 ? "  ·  one spare bottle" : "") + (_level.Capacity == 5 ? "  ·  tall bottles" : "");
            foreach (var kv in _boosters)
            {
                int c = S.Economy.Count(kv.Key);
                kv.Value.Count.text = c > 0 ? c.ToString() : "+";
                kv.Value.Badge.color = c > 0 ? UIStyle.Coral : UIStyle.ButtonFill(UIStyle.ButtonKind.Primary);
                bool usable = _ctl.CanUse(kv.Key) || _ctl.InputLocked;
                kv.Value.Button.Background.color = UIStyle.WithAlpha(kv.Value.Button.Background.color, usable ? 1f : 0.55f);
            }
        }

        // ---------------- boosters ----------------

        private void OnBooster(BoosterType type)
        {
            if (_ctl.InputLocked) return;
            HideTutorialIf("undo_intro", type == BoosterType.Undo);
            HideTutorialIf("boosters_intro", type == BoosterType.ExtraBottle || type == BoosterType.Shuffle);
            HideTutorialIf("hint_intro", type == BoosterType.Hint);
            if (!_ctl.CanUse(type))
            {
                Overlays.Toast(type == BoosterType.Undo ? "Nothing to undo yet." :
                               type == BoosterType.ExtraBottle ? "Only one extra bottle per level." : "Not available right now.");
                return;
            }
            if (S.Economy.Count(type) <= 0)
            {
                OfferBooster(type);
                return;
            }
            Use(type);
        }

        private void Use(BoosterType type)
        {
            switch (type)
            {
                case BoosterType.Undo: _ctl.UseUndo(); break;
                case BoosterType.ExtraBottle: _ctl.UseExtraBottle(); break;
                case BoosterType.Hint: _ctl.UseHint(); break;
                case BoosterType.Shuffle:
                    Overlays.Confirm("Shuffle bottles?", "Deals a NEW layout that is verified solvable, and restarts this level from the beginning.", "Shuffle",
                        () => _ctl.UseShuffle(ok => { if (ok) Overlays.Toast("New layout dealt — good luck!"); }));
                    break;
            }
            Refresh();
        }

        private void OfferBooster(BoosterType type)
        {
            var offer = S.EconomyConfig.FindOffer(type);
            if (offer == null) return;
            bool canAfford = S.Economy.Coins >= offer.Price;
            bool adReady = type == BoosterType.ExtraBottle ? _ctl.CanAddBottleFromAd : S.AdGate.CanRequest(AdPlacements.BonusBooster);
            Overlays.Open("BuyBooster", new Vector2(900, 900), h =>
            {
                var body = UIFactory.Text(h.Content, offer.Description + "\n\nYou have none left.", UIStyle.FontBody, UIStyle.Ink, TextAnchor.UpperCenter, FontStyle.Normal);
                body.rectTransform.offsetMin = new Vector2(10, 380);
                var buy = UIFactory.Button(h.Content, "Buy ×" + offer.Quantity + "  for  " + offer.Price, UIStyle.ButtonKind.Gold, () =>
                {
                    var r = S.Economy.BuyOffer(offer.Id, "buy:" + System.Guid.NewGuid().ToString("N"));
                    h.Close();
                    if (r.Success) { S.Audio.PlayCoin(); Use(type); }
                    else Overlays.Toast("Not enough coins. Complete levels to earn more!");
                }, new Vector2(640, UIStyle.ButtonHeight), Icon.Coin);
                UIFactory.Place(buy.Rect, new Vector2(0.5f, 0f), buy.Rect.sizeDelta, new Vector2(0, 200), new Vector2(0.5f, 0f));
                buy.SetInteractable(canAfford);
                var ad = UIFactory.Button(h.Content, S.Ads.IsMock ? "Free (mock ad)" : "Free (watch ad)", UIStyle.ButtonKind.Accent, () =>
                {
                    h.Close();
                    RequestBonusBooster(type);
                }, new Vector2(640, UIStyle.ButtonHeight), Icon.Ad);
                UIFactory.Place(ad.Rect, new Vector2(0.5f, 0f), ad.Rect.sizeDelta, new Vector2(0, 20), new Vector2(0.5f, 0f));
                ad.SetInteractable(adReady);
                if (!adReady) ad.SetLabel("Ad unavailable");
            }, true, offer.Title);
        }

        private void RequestBonusBooster(BoosterType type)
        {
            if (type == BoosterType.ExtraBottle)
            {
                // Rewarded ad gives the empty bottle directly on the board.
                _ctl.AddBottleFromAd(ok => { Overlays.Toast(ok ? "Empty bottle added!" : "No bottle this time: the ad wasn't completed."); Refresh(); });
                return;
            }
            string tx = "ad-booster:" + _ctl.Session.AttemptId + ":" + type + ":" + System.Guid.NewGuid().ToString("N");
            _ctl.Paused = true;
            S.AdGate.Request(AdPlacements.BonusBooster, () =>
            {
                if (S.Economy.GrantBooster(type, S.EconomyConfig.RewardedBonusBoosterQuantity, tx).Success)
                    Overlays.Toast("Booster added!");
            }, outcome =>
            {
                _ctl.Paused = false;
                if (outcome != AdOutcome.Completed) Overlays.Toast("No reward — the ad wasn't completed.");
                Refresh();
            }, S.EconomyConfig.RewardedCooldownSeconds);
        }

        // ---------------- pause / back ----------------

        private void OpenPause()
        {
            if (_pause != null || _ctl.Session.IsComplete) return;
            _ctl.Paused = true;
            _pause = CommonOverlays.ShowPause(S,
                onResume: () => { _ctl.Paused = false; _pause = null; },
                onRestart: () => { HideStuck(); _ctl.Restart(); },
                onMap: () => S.Navigator.Go(ScreenId.LevelMap));
        }

        public override bool HandleBack()
        {
            OpenPause();
            return true;
        }

        public override void OnAppPause(bool paused)
        {
            if (paused)
            {
                _ctl?.PersistSession();
                S.SaveNow();
            }
            else if (_ctl != null && !_ctl.Session.IsComplete && !Overlays.AnyOpen)
            {
                OpenPause(); // resume into a paused state, never mid-move surprise
            }
        }

        // ---------------- stuck ----------------

        /// <summary>
        /// "Colors are blocked": no useful move remains. Offers exactly three helps (Undo, Shuffle, Empty bottle),
        /// plus a free empty bottle from a rewarded ad when ads are available, and Restart as a last resort.
        /// There is never a move limit or a fail state; the player can also close the panel and keep looking.
        /// </summary>
        private void ShowStuck()
        {
            if (_stuckPanel != null) return;
            var bg = UIFactory.Panel(Safe, "Stuck", UIStyle.Ivory, UIStyle.RadiusLarge);
            var holder = (RectTransform)bg.transform.parent;
            _stuckPanel = holder.gameObject;
            bool adReady = _ctl.CanAddBottleFromAd;
            float height = adReady ? 640 : 500;
            UIFactory.Place(holder, new Vector2(0.5f, 0f), new Vector2(980, height), new Vector2(0, 330), new Vector2(0.5f, 0f));

            var title = UIFactory.Text(holder, "Colors are blocked!", UIStyle.FontHeading, UIStyle.Ink);
            UIFactory.Band(title.rectTransform, 1f, 80, -22, 30, 1f);
            var sub = UIFactory.Text(holder, "Choose one of 3 helps (no move limit, take your time):", UIStyle.FontCaption, UIStyle.InkSoft, TextAnchor.MiddleCenter, FontStyle.Normal);
            UIFactory.Band(sub.rectTransform, 1f, 50, -96, 30, 1f);

            HelpTile(holder, BoosterType.Undo, Icon.Undo, "Undo", UIStyle.Hex("#3D8BFF"), 0.2f);
            HelpTile(holder, BoosterType.Shuffle, Icon.Shuffle, "Shuffle", UIStyle.Hex("#8E5CF7"), 0.5f);
            HelpTile(holder, BoosterType.ExtraBottle, Icon.BottlePlus, "Empty bottle", UIStyle.Hex("#36B0C9"), 0.8f);

            if (adReady)
            {
                var ad = UIFactory.Button(holder, S.Ads.IsMock ? "Free empty bottle (mock ad)" : "Free empty bottle (watch ad)", UIStyle.ButtonKind.Primary, () =>
                {
                    HideStuck();
                    _ctl.AddBottleFromAd(ok => Overlays.Toast(ok ? "Empty bottle added!" : "No bottle this time: the ad wasn't completed."));
                }, new Vector2(820, 120), Icon.Ad, UIStyle.FontLabel);
                UIFactory.Place(ad.Rect, new Vector2(0.5f, 0f), ad.Rect.sizeDelta, new Vector2(0, 110), new Vector2(0.5f, 0f));
            }
            var restart = UIFactory.Button(holder, "Restart level", UIStyle.ButtonKind.Secondary, () => { HideStuck(); _ctl.Restart(); }, new Vector2(400, 84), Icon.Restart, UIStyle.FontCaption);
            UIFactory.Place(restart.Rect, new Vector2(0.5f, 0f), restart.Rect.sizeDelta, new Vector2(0, 18), new Vector2(0.5f, 0f));
            var close = UIFactory.IconButton(holder, Icon.Close, UIStyle.Coral, 84, HideStuck);
            UIFactory.Place(close.Rect, new Vector2(1, 1), close.Rect.sizeDelta, new Vector2(-12, -12), new Vector2(1, 1));
            S.Audio.PlayInvalid();
            if (!MotionPrefs.Reduced) Tween.Scale(this, holder, new Vector3(0.9f, 0.9f, 1), Vector3.one, 0.25f, Ease.OutBack);
        }

        private void HelpTile(RectTransform parent, BoosterType type, Icon icon, string label, Color color, float x)
        {
            var tile = UIFactory.Rect("Help_" + type, parent);
            UIFactory.Place(tile, new Vector2(x, 1f), new Vector2(260, 220), new Vector2(0, -160), new Vector2(0.5f, 1f));
            bool usable = _ctl.CanUse(type) || (type == BoosterType.Undo && _ctl.Session.CanUndo);
            var b = UIFactory.IconButton(tile, icon, color, 140, () => { HideStuck(); OnBooster(type); });
            UIFactory.Place(b.Rect, new Vector2(0.5f, 1f), b.Rect.sizeDelta, Vector2.zero, new Vector2(0.5f, 1f));
            if (!usable) b.SetInteractable(false);
            int count = S.Economy.Count(type);
            var badge = UIFactory.Image(b.Rect, "Badge", SpriteLibrary.Circle, count > 0 ? UIStyle.Coral : UIStyle.ButtonFill(UIStyle.ButtonKind.Primary));
            UIFactory.Place(badge.rectTransform, new Vector2(1, 1), new Vector2(56, 56), new Vector2(6, 6), new Vector2(1, 1));
            UIFactory.AddOutline(badge, Color.white, 3);
            UIFactory.Text(badge.transform, count > 0 ? count.ToString() : "+", UIStyle.FontCaption - 4, Color.white);
            var t = UIFactory.Text(tile, label, UIStyle.FontCaption, UIStyle.Ink);
            UIFactory.Band(t.rectTransform, 0f, 56, 0, 0, 0f);
        }

        private void HideStuck()
        {
            if (_stuckPanel != null) Destroy(_stuckPanel);
            _stuckPanel = null;
        }

        // ---------------- tutorial ----------------

        private static string TutorialText(string key, int threeStars)
        {
            switch (key)
            {
                case "tap_to_pour": return "Tap a bottle to pick up its top color.";
                case "same_color_stacks": return "You can pour only onto the same color — or into an empty bottle.";
                case "empty_accepts_any": return "Empty bottles accept any color. Use them to untangle layers!";
                case "plan_ahead": return "Goal: make every bottle a single color.";
                case "undo_intro": return "Made a mistake? Undo takes back your last pour.";
                case "partial_pour": return "If there isn't room for all of it, only what fits is poured.";
                case "boosters_intro": return "Stuck? Add bottle or Shuffle can help.";
                case "hint_intro": return "Hint shows a recommended move from the solver.";
                case "five_colors": return "Five colors now — take your time.";
                case "stars_intro": return "Fewer moves = more stars. ★★★ in " + threeStars + " moves!";
                case "one_spare_bottle": return "Only ONE empty bottle here — plan every pour!";
                case "tall_bottles": return "Tall bottles hold 5 layers. Same rules, bigger puzzles!";
                default: return null;
            }
        }

        private void StartTutorial()
        {
            var key = _level.TutorialKey;
            if (string.IsNullOrEmpty(key) || S.Save.TutorialsSeen.Contains(key)) return;
            string text = TutorialText(key, StarRating.ThreeStarLimit(_ctl.Session.ParMoves));
            if (text == null) return;
            _activeTutorial = key;
            _tutorialStep = 0;
            RectTransform target = null;
            if (key == "tap_to_pour" && _board.Views.Count > 0) target = _board.Views[0].Rect;
            if (key == "undo_intro") target = _boosters[BoosterType.Undo].Button.Rect;
            if (key == "boosters_intro") target = _boosters[BoosterType.ExtraBottle].Button.Rect;
            if (key == "hint_intro") target = _boosters[BoosterType.Hint].Button.Rect;
            _coach.Show(text, target, 0.86f);
        }

        private void OnTapped(TapResult r)
        {
            if (_activeTutorial == null) return;
            if (_activeTutorial == "tap_to_pour")
            {
                if (r.Kind == TapKind.Selected && _tutorialStep == 0)
                {
                    _tutorialStep = 1;
                    int other = r.Index == 0 ? 1 : 0;
                    _coach.Show("Now tap the other bottle to pour.", other < _board.Views.Count ? _board.Views[other].Rect : null, 0.86f);
                    return;
                }
                if (r.Kind == TapKind.Moved) CompleteTutorial();
                return;
            }
            bool boosterLesson = _activeTutorial == "undo_intro" || _activeTutorial == "hint_intro" || _activeTutorial == "boosters_intro";
            // Booster lessons stay up a little longer (until used, or after a few pours) so the player can try them.
            if (r.Kind == TapKind.Moved && (!boosterLesson || _ctl.Session.MoveCount >= 3))
                CompleteTutorial();
        }

        private void HideTutorialIf(string key, bool condition)
        {
            if (condition && _activeTutorial == key) CompleteTutorial();
        }

        private void CompleteTutorial()
        {
            if (_activeTutorial == null) return;
            S.Save.TutorialsSeen.Add(_activeTutorial);
            S.MarkDirty();
            _activeTutorial = null;
            _coach.Hide();
        }

        // ---------------- win ----------------

        private void OnWon(LevelResult result)
        {
            CompleteTutorial();
            HideStuck();
            Confetti.Burst(Full, new Vector2(0, -200), 36);
            Tween.Delay(this, MotionPrefs.Reduced ? 0.3f : 1.0f, () => S.Navigator.Go(ScreenId.LevelComplete, result));
        }
    }
}
