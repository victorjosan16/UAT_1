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
        private ObjectivePanel _objectives;
        private bool _removeMode;

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
            _levelText = UIFactory.Text(bar, Loc.F("Level {0}", number), UIStyle.FontHeading, hudText);
            var stageText = UIFactory.Text(bar, Loc.F("Stage {0}", _level.Stage), UIStyle.FontCaption, UIStyle.WithAlpha(hudText, 0.7f), TextAnchor.MiddleCenter, FontStyle.Normal);
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
                var bt = UIFactory.Text(badge.transform, Loc.T(boss ? "BOSS LEVEL" : "HARD LEVEL"), UIStyle.FontCaption, Color.white);
                if (boss)
                {
                    var crown = UIFactory.Image(badge.transform, "Crown", SpriteLibrary.Get(Icon.Crown), Color.white);
                    UIFactory.Place(crown.rectTransform, new Vector2(0, 0.5f), new Vector2(56, 56), new Vector2(14, 2), new Vector2(0, 0.5f));
                }
                if (!MotionPrefs.Reduced) Tween.Pulse(this, badge.rectTransform, 0.05f, 0.9f);
            }
            _goalText = UIFactory.Text(Safe, "", UIStyle.FontCaption, UIStyle.WithAlpha(hudText, 0.8f), TextAnchor.MiddleCenter, FontStyle.Normal);
            UIFactory.Band(_goalText.rectTransform, 1f, 60, -UIStyle.TopBarHeight - (_level.IsHard ? 82 : 30), 40, 1f);

            float objectiveSpace = 0f;
            if (_level.Objectives.Count > 0)
            {
                _objectives = ObjectivePanel.Create(Safe, _level.Objectives.Count, hudText);
                UIFactory.Place(_objectives.Rect, new Vector2(0.5f, 1f), new Vector2(1000, 90), new Vector2(0, -UIStyle.TopBarHeight - (_level.IsHard ? 150 : 98)), new Vector2(0.5f, 1f));
                objectiveSpace = 96f;
            }

            AmbientMotes(3f);
            BottleView.NeonAura = !light;

            // Board (between HUD and booster bar)
            var boardRt = UIFactory.Rect("Board", Safe);
            boardRt.offsetMin = new Vector2(30, 360);
            boardRt.offsetMax = new Vector2(-30, -UIStyle.TopBarHeight - 110 - objectiveSpace);
            _board = boardRt.gameObject.AddComponent<BoardView>();
            _board.Init(cap => S.Themes.Bottle(cap), _level.Capacity, S.Settings.Data.ColorblindSymbols);
            if (light) _board.SetGlassColor(UIStyle.Hex("#7F8DB0"));

            // Booster bar
            var boosterBar = UIFactory.Rect("Boosters", Safe);
            UIFactory.Band(boosterBar, 0f, 300, 40, 40, 0f);
            AddBooster(boosterBar, BoosterType.Undo, Icon.Undo, Loc.T("Undo"), UIStyle.Hex("#3D8BFF"), 0.1f);
            AddBooster(boosterBar, BoosterType.ExtraBottle, Icon.BottlePlus, Loc.T("Add bottle"), UIStyle.Hex("#36B0C9"), 0.3f);
            AddBooster(boosterBar, BoosterType.Shuffle, Icon.Shuffle, Loc.T("Shuffle"), UIStyle.Hex("#8E5CF7"), 0.5f);
            AddBooster(boosterBar, BoosterType.RemoveColor, Icon.Droplet, Loc.T("Remove color"), UIStyle.Hex("#E2557A"), 0.7f);
            AddBooster(boosterBar, BoosterType.Hint, Icon.Bulb, Loc.T("Hint"), UIStyle.Hex("#F5A623"), 0.9f);

            _coach = CoachMark.Create(Safe);

            Canvas.ForceUpdateCanvases();
            _ctl = new GameplayController(S, _level, _board, this);
            _ctl.StateChanged += Refresh;
            _ctl.StateChanged += RefreshObjectives;
            _ctl.Won += OnWon;
            _ctl.Stuck += ShowStuck;
            _ctl.Notice += msg => Overlays.Toast(msg, 2.6f);
            _ctl.Tapped += OnTapped;
            _ctl.TapInterceptor = InterceptTap;
            _ctl.ObjectivesChanged += RefreshObjectives;
            _ctl.BottleUnlocked += i => Overlays.Toast(Loc.T("Cork removed!"), 1.6f);
            _board.Build(_ctl.Session.Current, true);
            S.Economy.Changed += Refresh;
            S.Settings.Changed += OnSettingsChanged;
            Refresh();
            RefreshObjectives();
            StartTutorial();
            if (_level.IsHard && _ctl.Session.MoveCount == 0)
            {
                int bonus = _level.Kind == LevelKind.Boss ? S.EconomyConfig.BossLevelBonus : S.EconomyConfig.HardLevelBonus;
                bool firstClear = !S.Save.Progress.IsCompleted(_level.Number);
                Tween.Delay(this, 0.5f, () => Overlays.Toast((_level.Kind == LevelKind.Boss ? Loc.F("Boss level — end of stage {0}!", _level.Stage) : Loc.T("Hard level!"))
                    + (firstClear ? "  " + Loc.F("Bonus +{0} coins", bonus) : ""), 2.4f));
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
            UIFactory.Place(holder, new Vector2(x, 0.5f), new Vector2(196, 280), Vector2.zero);
            var b = UIFactory.IconButton(holder, icon, color, 150, () => OnBooster(type));
            UIFactory.Place(b.Rect, new Vector2(0.5f, 1f), b.Rect.sizeDelta, new Vector2(0, -6), new Vector2(0.5f, 1f));
            var badge = UIFactory.Image(b.Rect, "Badge", SpriteLibrary.Circle, UIStyle.Coral);
            UIFactory.Place(badge.rectTransform, new Vector2(1, 1), new Vector2(66, 66), new Vector2(6, 6), new Vector2(1, 1));
            UIFactory.AddOutline(badge, Color.white, 3);
            var count = UIFactory.Text(badge.transform, "0", UIStyle.FontCaption, Color.white);
            var t = UIFactory.Text(holder, label, UIStyle.FontCaption - 4, S.Settings.Data.LightGameplayBackground ? UIStyle.InkOnLight : Color.white);
            UIFactory.Band(t.rectTransform, 0f, 60, 0, 0, 0f);
            UIFactory.AddShadow(t, new Color(0, 0, 0, 0.3f), new Vector2(0, -2));
            _boosters[type] = new BoosterButton { Button = b, Badge = badge, Count = count };
        }

        private void Refresh()
        {
            if (_ctl == null) return;
            var sess = _ctl.Session;
            _movesText.text = Loc.F("Moves: {0}", sess.MoveCount) + (sess.ShuffleCount > 0 ? "  ·  " + Loc.T("shuffled") : "");
            int three = StarRating.ThreeStarLimit(sess.ParMoves);
            _goalText.text = Loc.T("No move limit") + "  ·  " + Loc.F("★★★ in {0} moves or fewer", three)
                + (_level.EmptyBottleCount == 1 ? "  ·  " + Loc.T("one spare bottle") : "")
                + (_level.HasMixedCapacities ? "  ·  " + Loc.T("mixed sizes") : (_level.Capacity >= 5 ? "  ·  " + Loc.T("tall bottles") : ""));
            foreach (var kv in _boosters)
            {
                int c = S.Economy.Count(kv.Key);
                bool disabled = _ctl.IsDisabledHere(kv.Key);
                kv.Value.Count.text = disabled ? "×" : (c > 0 ? c.ToString() : "+");
                kv.Value.Badge.color = disabled ? UIStyle.Disabled : (c > 0 ? UIStyle.Coral : UIStyle.ButtonFill(UIStyle.ButtonKind.Primary));
                bool usable = !disabled && (_ctl.CanUse(kv.Key) || _ctl.InputLocked);
                float alpha = disabled ? 0.3f : (usable ? 1f : 0.55f);
                if (kv.Key == BoosterType.RemoveColor && _removeMode) alpha = 1f;
                kv.Value.Button.Background.color = UIStyle.WithAlpha(kv.Value.Button.Background.color, alpha);
            }
        }

        // ---------------- boosters ----------------

        private void OnBooster(BoosterType type)
        {
            if (_ctl.InputLocked) return;
            if (_removeMode) { SetRemoveMode(false); if (type == BoosterType.RemoveColor) return; }
            if (_ctl.IsDisabledHere(type)) { Overlays.Toast(Loc.T("Booster not available on this level.")); return; }
            HideTutorialIf("undo_intro", type == BoosterType.Undo);
            HideTutorialIf("boosters_intro", type == BoosterType.ExtraBottle || type == BoosterType.Shuffle);
            HideTutorialIf("hint_intro", type == BoosterType.Hint);
            if (!_ctl.CanUse(type))
            {
                Overlays.Toast(Loc.T(type == BoosterType.Undo ? "Nothing to undo yet." :
                               type == BoosterType.ExtraBottle ? "No more extra bottles on this level." : "Not available right now."));
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
                    Overlays.Confirm(Loc.T("Shuffle bottles?"), Loc.T("Deals a NEW layout that is verified solvable, and restarts this level from the beginning."), Loc.T("Shuffle"),
                        () => _ctl.UseShuffle(ok => { if (ok) { Overlays.Toast(Loc.T("New layout dealt — good luck!")); _objectives?.ResetPops(); RefreshObjectives(); } }), Loc.T("Cancel"));
                    break;
                case BoosterType.RemoveColor:
                    SetRemoveMode(true);
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
                var body = UIFactory.Text(h.Content, Loc.T(offer.Description) + "\n\n" + Loc.T("You have none left."), UIStyle.FontBody, UIStyle.Ink, TextAnchor.UpperCenter, FontStyle.Normal);
                body.rectTransform.offsetMin = new Vector2(10, 380);
                var buy = UIFactory.Button(h.Content, Loc.F("Buy ×{0}  for  {1}", offer.Quantity, offer.Price), UIStyle.ButtonKind.Gold, () =>
                {
                    var r = S.Economy.BuyOffer(offer.Id, "buy:" + System.Guid.NewGuid().ToString("N"));
                    h.Close();
                    if (r.Success) { S.Audio.PlayCoin(); Use(type); }
                    else Overlays.Toast(Loc.T("Not enough coins. Complete levels to earn more!"));
                }, new Vector2(640, UIStyle.ButtonHeight), Icon.Coin);
                UIFactory.Place(buy.Rect, new Vector2(0.5f, 0f), buy.Rect.sizeDelta, new Vector2(0, 200), new Vector2(0.5f, 0f));
                buy.SetInteractable(canAfford);
                var ad = UIFactory.Button(h.Content, Loc.T(S.Ads.IsMock ? "Free (mock ad)" : "Free (watch ad)"), UIStyle.ButtonKind.Accent, () =>
                {
                    h.Close();
                    RequestBonusBooster(type);
                }, new Vector2(640, UIStyle.ButtonHeight), Icon.Ad);
                UIFactory.Place(ad.Rect, new Vector2(0.5f, 0f), ad.Rect.sizeDelta, new Vector2(0, 20), new Vector2(0.5f, 0f));
                ad.SetInteractable(adReady);
                if (!adReady) ad.SetLabel(Loc.T("Ad unavailable"));
            }, true, Loc.T(offer.Title));
        }

        private void RequestBonusBooster(BoosterType type)
        {
            if (type == BoosterType.ExtraBottle)
            {
                // Rewarded ad gives the empty bottle directly on the board.
                _ctl.AddBottleFromAd(ok => { Overlays.Toast(Loc.T(ok ? "Empty bottle added!" : "No bottle this time: the ad wasn't completed.")); Refresh(); });
                return;
            }
            _ctl.RequestRewardedBooster(type, ok =>
            {
                Overlays.Toast(Loc.T(ok ? "Booster added!" : "No reward — the ad wasn't completed."));
                Refresh();
            });
        }

        // ---------------- special taps: Remove Color mode, ad bottles, corks ----------------

        private void SetRemoveMode(bool on)
        {
            _removeMode = on;
            if (on) Overlays.Toast(Loc.T("Tap a bottle: its top color is removed from the board. Tap the booster again to cancel."), 3f);
            Refresh();
        }

        private bool InterceptTap(int index)
        {
            if (_removeMode)
            {
                SetRemoveMode(false);
                if (!_ctl.Session.CanRemoveColorAt(index))
                {
                    _board.Shake(index);
                    Overlays.Toast(Loc.T("Pick a bottle with a visible color on top."));
                    return true;
                }
                _ctl.UseRemoveColor(index, ok => { if (ok) { S.Audio.PlayComplete(); Overlays.Toast(Loc.T("Color removed!")); } Refresh(); });
                return true;
            }
            if (_ctl.Session.IsRewardBottle(index))
            {
                if (!_ctl.CanUnlockRewardBottle(index)) { _board.Shake(index); Overlays.Toast(Loc.T("Ad unavailable right now. Try again later.")); return true; }
                HideTutorialIf("reward_bottle_intro", true);
                Overlays.Confirm(Loc.T("Bonus bottle"), Loc.T("Unlock this bottle with a short ad?"), Loc.T(S.Ads.IsMock ? "Unlock (mock ad)" : "Watch ad"),
                    () => _ctl.UnlockRewardBottle(index, ok => Overlays.Toast(Loc.T(ok ? "Bottle unlocked!" : "No reward — the ad wasn't completed."))), Loc.T("Cancel"));
                return true;
            }
            return false;
        }

        private void RefreshObjectives()
        {
            if (_objectives == null || _ctl == null) return;
            var newly = _objectives.Refresh(_ctl.Objectives());
            foreach (var st in newly)
            {
                Overlays.Toast(Loc.F("Objective complete: {0}", ObjectivePanel.Describe(st.Def)), 1.8f);
                S.Audio.PlayComplete();
            }
        }

        // ---------------- pause / back ----------------

        private void OpenPause()
        {
            if (_pause != null || _ctl.Session.IsComplete) return;
            _ctl.Paused = true;
            _pause = CommonOverlays.ShowPause(S,
                onResume: () => { _ctl.Paused = false; _pause = null; },
                onRestart: () => { HideStuck(); _ctl.Restart(); _objectives?.ResetPops(); RefreshObjectives(); },
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

            var title = UIFactory.Text(holder, Loc.T("Colors are blocked!"), UIStyle.FontHeading, UIStyle.Ink);
            UIFactory.Band(title.rectTransform, 1f, 80, -22, 30, 1f);
            var sub = UIFactory.Text(holder, Loc.T("Choose one of 3 helps (no move limit, take your time):"), UIStyle.FontCaption, UIStyle.InkSoft, TextAnchor.MiddleCenter, FontStyle.Normal);
            UIFactory.Band(sub.rectTransform, 1f, 50, -96, 30, 1f);

            HelpTile(holder, BoosterType.Undo, Icon.Undo, Loc.T("Undo"), UIStyle.Hex("#3D8BFF"), 0.2f);
            HelpTile(holder, BoosterType.Shuffle, Icon.Shuffle, Loc.T("Shuffle"), UIStyle.Hex("#8E5CF7"), 0.5f);
            HelpTile(holder, BoosterType.ExtraBottle, Icon.BottlePlus, Loc.T("Empty bottle"), UIStyle.Hex("#36B0C9"), 0.8f);

            if (adReady)
            {
                var ad = UIFactory.Button(holder, Loc.T(S.Ads.IsMock ? "Free empty bottle (mock ad)" : "Free empty bottle (watch ad)"), UIStyle.ButtonKind.Primary, () =>
                {
                    HideStuck();
                    _ctl.AddBottleFromAd(ok => Overlays.Toast(Loc.T(ok ? "Empty bottle added!" : "No bottle this time: the ad wasn't completed.")));
                }, new Vector2(820, 120), Icon.Ad, UIStyle.FontLabel);
                UIFactory.Place(ad.Rect, new Vector2(0.5f, 0f), ad.Rect.sizeDelta, new Vector2(0, 110), new Vector2(0.5f, 0f));
            }
            var restart = UIFactory.Button(holder, Loc.T("Restart level"), UIStyle.ButtonKind.Secondary, () => { HideStuck(); _ctl.Restart(); _objectives?.ResetPops(); RefreshObjectives(); }, new Vector2(400, 84), Icon.Restart, UIStyle.FontCaption);
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
            bool usable = !_ctl.IsDisabledHere(type) && (_ctl.CanUse(type) || (type == BoosterType.Undo && _ctl.Session.CanUndo));
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
                case "tap_to_pour": return Loc.T("Tap a bottle to pick up its top color.");
                case "same_color_stacks": return Loc.T("You can pour only onto the same color — or into an empty bottle.");
                case "empty_accepts_any": return Loc.T("Empty bottles accept any color. Use them to untangle layers!");
                case "plan_ahead": return Loc.T("Goal: make every bottle a single color.");
                case "undo_intro": return Loc.T("Made a mistake? Undo takes back your last pour.");
                case "partial_pour": return Loc.T("If there isn't room for all of it, only what fits is poured.");
                case "boosters_intro": return Loc.T("Stuck? Add bottle or Shuffle can help.");
                case "hint_intro": return Loc.T("Hint shows a recommended move from the solver.");
                case "five_colors": return Loc.T("Five colors now — take your time.");
                case "stars_intro": return Loc.F("Fewer moves = more stars. ★★★ in {0} moves!", threeStars);
                case "one_spare_bottle": return Loc.T("Only ONE empty bottle here — plan every pour!");
                case "tall_bottles": return Loc.T("Tall bottles hold 5 layers. Same rules, bigger puzzles!");
                case "mystery_intro": return Loc.T("Mystery layers (?) stay hidden until they reach the top of the bottle.");
                case "sizes_intro": return Loc.T("Bottles now come in different sizes. A color is done when one bottle holds all of it.");
                case "cork_intro": return Loc.T("Corked bottles can't be used until their condition is met. Tap one to see it.");
                case "reward_bottle_intro": return Loc.T("This bonus bottle is optional: unlock it with a short ad if you want extra room.");
                case "objectives_intro": return Loc.T("Complete the objectives at the top for bonus coins.");
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
            if (key == "objectives_intro" && _objectives != null) target = _objectives.Rect;
            if (key == "cork_intro" || key == "reward_bottle_intro" || key == "mystery_intro")
            {
                var st = _ctl.Session.Current;
                for (int i = 0; i < st.BottleCount && i < _board.Views.Count; i++)
                {
                    bool match = key == "cork_intro" ? st[i].IsCorked : key == "reward_bottle_intro" ? st[i].RewardLocked : st[i].Hidden > 0;
                    if (match) { target = _board.Views[i].Rect; break; }
                }
            }
            _coach.Show(text, target, 0.86f);
        }

        private void OnTapped(TapResult r)
        {
            if (r.Kind == TapKind.Invalid && r.Move != null && r.Move.Rejection == Core.Puzzle.MoveRejection.Locked)
            {
                Color c;
                int idx = _ctl.Session.Current[r.Index].IsLocked ? r.Index : r.PreviousSelection;
                string cond = idx >= 0 ? BoardView.LockLabel(_ctl.Session.Current, idx, out c) : null;
                Overlays.Toast(cond != null ? Loc.F("Corked bottle — opens when: {0}", cond) : Loc.T("This bottle is locked."), 2.2f);
                HideTutorialIf("cork_intro", true);
            }
            if (r.Kind == TapKind.Moved) RefreshObjectives();
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
            if (_activeTutorial == "tap_to_pour" || _activeTutorial == null) return;
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
