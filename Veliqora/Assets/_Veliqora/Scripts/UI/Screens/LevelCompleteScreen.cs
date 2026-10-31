using UnityEngine;
using UnityEngine.UI;
using Veliqora.Core.Ads;
using Veliqora.Gameplay;
using Veliqora.Infrastructure;
using Veliqora.Presentation;
using Veliqora.UI.Widgets;

namespace Veliqora.UI.Screens
{
    /// <summary>Stars, earned coins, optional rewarded double, Next / Replay / Map.</summary>
    public sealed class LevelCompleteScreen : ScreenBase
    {
        private LevelResult _result;
        private ButtonHandle _doubleBtn;
        private Text _coinsText;
        private bool _doubled;
        private ButtonHandle _objectiveBtn;
        private bool _objectiveClaimed;
        private int _shownCoins;

        protected override void BuildBackground(RectTransform full)
        {
            var current = S.Themes.Theme;
            if (current.IsDark)
            {
                FullBackground(full, BackgroundArt.Valley(current));
                ReadabilityScrim(full, 0.3f, 0.5f, 0.12f);
                var glow = UIFactory.Image(full, "Rays", SpriteLibrary.RadialGlow, UIStyle.WithAlpha(current.Accent, 0.28f));
                UIFactory.Place(glow.rectTransform, new Vector2(0.5f, 0.72f), new Vector2(1500, 1500), Vector2.zero);
                return;
            }
            var art = BackgroundArt.ScreenArt("complete");
            if (art != null)
            {
                FullBackground(full, art);
                ReadabilityScrim(full, 0.25f, 0.55f, 0.18f);
                return;
            }
            var theme = S.Themes.Theme;
            var raw = UIFactory.Raw(full, "Background", BackgroundArt.GameplayGradient(theme.GameTop, theme.GameBottom, false), Color.white);
            UIFactory.Stretch(raw.rectTransform);
            var rays = UIFactory.Image(full, "Rays", SpriteLibrary.RadialGlow, new Color(1f, 0.9f, 0.55f, 0.35f));
            UIFactory.Place(rays.rectTransform, new Vector2(0.5f, 0.68f), new Vector2(1500, 1500), Vector2.zero);
        }

        protected override void Build(object args)
        {
            AmbientMotes(6f);
            _result = args as LevelResult ?? new LevelResult { Level = S.Save.Progress.CurrentLevel, Stars = 1 };

            // Celebration art behind the stars (authored effects, when present)
            var burst = SpriteLibrary.Art(_result.Stars >= 3 ? "Effects/fx_crown_burst" : "Effects/fx_star_burst");
            if (burst != null)
            {
                var fx = UIFactory.Image(Safe, "Burst", burst, Color.white);
                fx.preserveAspect = true;
                UIFactory.Place(fx.rectTransform, new Vector2(0.5f, 0.82f), _result.Stars >= 3 ? new Vector2(420, 320) : new Vector2(560, 420), new Vector2(0, _result.Stars >= 3 ? 160 : 0));
                if (!MotionPrefs.Reduced) Tween.Pulse(this, fx.rectTransform, 0.03f, 1.6f);
            }

            // Stars
            var starRow = UIFactory.Rect("Stars", Safe);
            UIFactory.Place(starRow, new Vector2(0.5f, 0.82f), new Vector2(760, 260), Vector2.zero);
            for (int i = 0; i < 3; i++)
            {
                bool earned = i < _result.Stars;
                var st = UIFactory.Image(starRow, "Star" + i, SpriteLibrary.Get(earned ? Icon.Star : Icon.StarEmpty), Color.white);
                float size = i == 1 ? 250 : 200;
                UIFactory.Place(st.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(size, size), new Vector2((i - 1) * 230, i == 1 ? 30 : -10));
                if (!MotionPrefs.Reduced)
                {
                    var rt = st.rectTransform;
                    rt.localScale = Vector3.zero;
                    int idx = i;
                    Tween.Delay(this, 0.2f + idx * 0.18f, () =>
                    {
                        Tween.Scale(this, rt, Vector3.zero, Vector3.one, 0.35f, Ease.OutBack);
                        if (idx < _result.Stars) S.Audio.PlayCoin();
                    });
                }
            }

            Ribbon(Safe, Loc.T("Level Complete!"), -560, 760, UIStyle.Hex("#D64FA0"));
            var lvl = UIFactory.Text(Safe, Loc.F("Level {0}", _result.Level) + "  ·  " + Loc.F("{0} moves (benchmark {1})", _result.Moves, _result.Par), UIStyle.FontLabel, Color.white, TextAnchor.MiddleCenter, FontStyle.Normal);
            UIFactory.Band(lvl.rectTransform, 1f, 70, -720, 40, 1f);

            // Decorative completed bottles in the level's colors
            var deco = UIFactory.Rect("Bottles", Safe);
            UIFactory.Place(deco, new Vector2(0.5f, 0.5f), new Vector2(900, 360), Vector2.zero);
            var shape = S.Themes.Bottle(4);
            var level = S.Levels.Get(Mathf.Clamp(_result.Level, 1, S.Levels.Count));
            int colors = Mathf.Min(5, Mathf.Max(1, level.ColorCount));
            for (int i = 0; i < colors; i++)
            {
                var go = new GameObject("Deco" + i, typeof(RectTransform));
                go.transform.SetParent(deco, false);
                var v = go.AddComponent<BottleView>();
                v.Init(i, shape, 4, 100, false);
                v.ShowState(new Core.Puzzle.BottleState(4, i + 1, i + 1, i + 1, i + 1));
                v.HomePosition = new Vector2((i - (colors - 1) * 0.5f) * 160f, -150f);
                v.Rect.anchoredPosition = v.HomePosition;
                foreach (var g in go.GetComponentsInChildren<Graphic>()) g.raycastTarget = false;
                v.Hop(0.4f + i * 0.08f);
            }

            // Coins
            var coinRow = UIFactory.Rect("Coins", Safe);
            UIFactory.Place(coinRow, new Vector2(0.5f, 0.34f), new Vector2(600, 140), Vector2.zero);
            var coin = UIFactory.Image(coinRow, "Coin", SpriteLibrary.Get(Icon.Coin), Color.white);
            UIFactory.Place(coin.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(120, 120), new Vector2(-130, 0));
            int total = _result.Reward != null ? _result.Reward.Total : 0;
            _coinsText = UIFactory.Text(coinRow, "+0", UIStyle.FontTitle, Color.white, TextAnchor.MiddleLeft);
            UIFactory.Place(_coinsText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(360, 140), new Vector2(110, 0));
            UIFactory.AddShadow(_coinsText, new Color(0, 0, 0, 0.3f), new Vector2(0, -4));
            _shownCoins = total + _result.ObjectiveCoins;
            Tween.Run(this, MotionPrefs.Dur(0.8f), k => _coinsText.text = "+" + Mathf.RoundToInt(_shownCoins * k), null, Ease.OutCubic);
            var extras = new System.Collections.Generic.List<string>();
            if (_result.Reward != null && _result.Reward.DifficultyBonus > 0)
                extras.Add(Loc.F(_result.Kind == Core.Levels.LevelKind.Boss ? "Boss level bonus +{0}" : "Hard level bonus +{0}", _result.Reward.DifficultyBonus));
            if (_result.ObjectiveCoins > 0) extras.Add(Loc.F("Objectives +{0}", _result.ObjectiveCoins));
            bool milestone = _result.Reward != null && _result.Reward.MilestoneCoins > 0;
            if (extras.Count > 0)
            {
                var hb = UIFactory.Text(Safe, string.Join("  ·  ", extras), UIStyle.FontLabel, UIStyle.Coral);
                UIFactory.Place(hb.rectTransform, new Vector2(0.5f, 0.34f), new Vector2(980, 60), new Vector2(0, milestone ? 150 : 95));
                UIFactory.AddShadow(hb, new Color(0, 0, 0, 0.4f), new Vector2(0, -3));
            }
            if (milestone)
            {
                var ms = UIFactory.Text(Safe, Loc.T("Milestone chest opened! Bonus coins and boosters added."), UIStyle.FontCaption, UIStyle.Sunshine);
                UIFactory.Place(ms.rectTransform, new Vector2(0.5f, 0.34f), new Vector2(980, 60), new Vector2(0, 95));
            }

            // Rewarded: double coins and the optional ad-only objective bonus (only when an ad can actually be shown)
            bool adReady = total > 0 && S.AdGate.CanRequest(AdPlacements.DoubleCoins);
            int objectiveBonus = 0;
            foreach (var o in _result.RewardedObjectives) objectiveBonus += o.RewardCoins;
            bool objReady = objectiveBonus > 0 && S.AdGate.CanRequest(AdPlacements.ObjectiveBonus);
            bool both = adReady && objReady;
            _doubleBtn = UIFactory.Button(Safe, Loc.T(both ? "Double coins" : (S.Ads.IsMock ? "Double coins (mock ad)" : "Double coins (watch ad)")), UIStyle.ButtonKind.Accent, OnDouble, new Vector2(both ? 470 : 700, 130), Icon.Ad, UIStyle.FontLabel);
            UIFactory.Place(_doubleBtn.Rect, new Vector2(0.5f, 0.26f), _doubleBtn.Rect.sizeDelta, new Vector2(both ? -250 : 0, 0));
            _doubleBtn.Rect.gameObject.SetActive(adReady);
            _objectiveBtn = UIFactory.Button(Safe, Loc.F("Bonus +{0}", objectiveBonus), UIStyle.ButtonKind.Gold, () => OnObjectiveBonus(objectiveBonus), new Vector2(both ? 470 : 700, 130), Icon.Ad, UIStyle.FontLabel);
            UIFactory.Place(_objectiveBtn.Rect, new Vector2(0.5f, 0.26f), _objectiveBtn.Rect.sizeDelta, new Vector2(both ? 250 : 0, 0));
            _objectiveBtn.Rect.gameObject.SetActive(objReady);
            ProgressionOverlays.CheckAchievementNotices(S);

            // Primary CTA
            var next = UIFactory.Button(Safe, Loc.T(_result.IsLastLevel ? "Back to map" : "Next Level"), UIStyle.ButtonKind.Primary, OnNext, new Vector2(700, 170), null, UIStyle.FontHeading);
            UIFactory.Place(next.Rect, new Vector2(0.5f, 0f), next.Rect.sizeDelta, new Vector2(0, 240), new Vector2(0.5f, 0f));
            Tween.Pulse(this, next.Rect, 0.03f, 1.2f);

            var replay = UIFactory.IconButton(Safe, Icon.Restart, UIStyle.Sunshine, 130, () => S.Navigator.Go(ScreenId.Gameplay, _result.Level));
            UIFactory.Place(replay.Rect, new Vector2(0.5f, 0f), replay.Rect.sizeDelta, new Vector2(-150, 70), new Vector2(0.5f, 0f));
            var map = UIFactory.IconButton(Safe, Icon.Map, UIStyle.Mint, 130, () => S.Navigator.Go(ScreenId.LevelMap));
            UIFactory.Place(map.Rect, new Vector2(0.5f, 0f), map.Rect.sizeDelta, new Vector2(150, 70), new Vector2(0.5f, 0f));

            if (_result.IsLastLevel)
            {
                var fin = UIFactory.Text(Safe, Loc.T("You've completed every level in the valley!"), UIStyle.FontLabel, UIStyle.Sunshine);
                UIFactory.Band(fin.rectTransform, 1f, 70, -800, 40, 1f);
            }

            Confetti.Burst(Full, new Vector2(0, 400), 40);
        }

        private void OnDouble()
        {
            if (_doubled || _result.Reward == null) return;
            _doubleBtn.SetInteractable(false);
            string tx = "ad-double:" + _result.AttemptId;
            int amount = _result.Reward.Total;
            S.AdGate.Request(AdPlacements.DoubleCoins, () =>
            {
                if (S.Economy.GrantCoins(amount, tx).Success)
                {
                    _doubled = true;
                    S.Audio.PlayCoin();
                    _shownCoins += amount;
                    _coinsText.text = "+" + _shownCoins;
                    Overlays.Toast(Loc.T("Coins doubled!"));
                }
            }, outcome =>
            {
                if (outcome != AdOutcome.Completed)
                {
                    _doubleBtn.SetInteractable(S.AdGate.CanRequest(AdPlacements.DoubleCoins));
                    Overlays.Toast(Loc.T(outcome == AdOutcome.Cancelled ? "Ad closed early — no bonus this time." : "Ad unavailable right now."));
                }
                else _doubleBtn.Rect.gameObject.SetActive(false);
            }, S.EconomyConfig.RewardedCooldownSeconds);
        }

        /// <summary>Optional ad-only objective reward: granted once per attempt after a completed ad ("objad:attempt:id").</summary>
        private void OnObjectiveBonus(int amount)
        {
            if (_objectiveClaimed) return;
            _objectiveBtn.SetInteractable(false);
            int granted = 0;
            S.AdGate.Request(AdPlacements.ObjectiveBonus, () =>
            {
                foreach (var o in _result.RewardedObjectives)
                    if (S.Economy.GrantCoins(o.RewardCoins, "objad:" + _result.AttemptId + ":" + o.Id).Success) granted += o.RewardCoins;
                if (granted > 0)
                {
                    _objectiveClaimed = true;
                    S.Save.Stats.ObjectivesCompleted += _result.RewardedObjectives.Count;
                    S.MarkDirty();
                    S.Audio.PlayCoin();
                    _shownCoins += granted;
                    _coinsText.text = "+" + _shownCoins;
                    Overlays.Toast(Loc.F("Objective bonus +{0}!", granted));
                }
            }, outcome =>
            {
                if (outcome != AdOutcome.Completed)
                {
                    _objectiveBtn.SetInteractable(S.AdGate.CanRequest(AdPlacements.ObjectiveBonus));
                    Overlays.Toast(Loc.T(outcome == AdOutcome.Cancelled ? "Ad closed early — no bonus this time." : "Ad unavailable right now."));
                }
                else _objectiveBtn.Rect.gameObject.SetActive(false);
            }, S.EconomyConfig.RewardedCooldownSeconds);
        }

        private void OnNext()
        {
            if (_result.IsLastLevel) { S.Navigator.Go(ScreenId.LevelMap); return; }
            S.Navigator.Go(ScreenId.Gameplay, Mathf.Min(S.Levels.Count, _result.Level + 1));
        }
    }
}
