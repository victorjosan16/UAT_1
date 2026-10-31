using UnityEngine;
using UnityEngine.UI;
using Veliqora.Infrastructure;
using Veliqora.Presentation;
using Veliqora.UI.Widgets;

namespace Veliqora.UI.Screens
{
    /// <summary>Home: logo, Play/Continue, progress, currency, Settings, Shop, Themes, Map; first-time coach mark.</summary>
    public sealed class MainMenuScreen : ScreenBase
    {
        public override bool AllowsBanner => true;

        private RectTransform _logo;
        private RectTransform _play;

        protected override void BuildBackground(RectTransform full)
        {
            // Classic theme uses the dedicated home illustration; other themes show their own artwork.
            Texture tex = S.Themes.ThemeId == "theme_valley" ? BackgroundArt.ScreenArt("home") : null;
            if (tex == null) tex = BackgroundArt.Valley(S.Themes.Theme);
            FullBackground(full, tex);
            ReadabilityScrim(full, 0.35f, 0.5f);
        }

        protected override void Build(object args)
        {
            var bar = TopBar();
            AddCoins(bar, right: false);
            var gear = UIFactory.IconButton(bar, Icon.Gear, UIStyle.Violet, 116, () => CommonOverlays.ShowSettings(S));
            UIFactory.Place(gear.Rect, new Vector2(1, 0.5f), gear.Rect.sizeDelta, Vector2.zero, new Vector2(1, 0.5f));

            AmbientMotes(5f);
            BuildLogo();

            int level = S.Save.Progress.CurrentLevel;
            bool resume = S.Save.Session != null && S.Save.Session.LevelNumber == level;
            bool allDone = S.Save.Progress.IsCompleted(S.Levels.Count) && level >= S.Levels.Count;

            // Big play button
            _play = UIFactory.Rect("Play", Safe);
            UIFactory.Place(_play, new Vector2(0.5f, 0.36f), new Vector2(330, 330), Vector2.zero);
            var glow = UIFactory.Image(_play, "Glow", SpriteLibrary.RadialGlow, new Color(1f, 0.92f, 0.5f, 0.75f));
            UIFactory.Stretch(glow.rectTransform, -120);
            var play = UIFactory.IconButton(_play, Icon.Play, UIStyle.Sunshine, 330, OnPlay);
            UIFactory.Stretch(play.Rect);
            Tween.Pulse(this, play.Rect, 0.04f, 1.4f);

            var pill = UIFactory.Image(Safe, "LevelPill", SpriteLibrary.Rounded(50), new Color(0.1f, 0.12f, 0.3f, 0.7f));
            UIFactory.Place(pill.rectTransform, new Vector2(0.5f, 0.36f), new Vector2(820, 110), new Vector2(0, -250));
            var levelDef = S.Levels.Get(Mathf.Clamp(level, 1, S.Levels.Count));
            string kindTag = levelDef.Kind == Core.Levels.LevelKind.Boss ? "  ·  BOSS" : (levelDef.Kind == Core.Levels.LevelKind.Hard ? "  ·  HARD" : "");
            string label = allDone ? "All levels complete!" : (resume ? "Continue  ·  " : "") + "Stage " + levelDef.Stage + "  ·  Level " + level + kindTag;
            var lt = UIFactory.Text(pill.transform, label, UIStyle.FontBody, Color.white);
            UIFactory.AddShadow(lt, new Color(0, 0, 0, 0.3f), new Vector2(0, -3));

            var stars = UIFactory.Text(Safe, "★ " + S.Save.Progress.TotalStars + " / " + (S.Levels.Count * 3), UIStyle.FontLabel, Color.white);
            UIFactory.Place(stars.rectTransform, new Vector2(0.5f, 0.36f), new Vector2(600, 70), new Vector2(0, -345));
            UIFactory.AddShadow(stars, new Color(0, 0, 0, 0.35f), new Vector2(0, -3));

            // Bottom navigation
            var nav = UIFactory.Rect("Nav", Safe);
            UIFactory.Band(nav, 0f, 230, 60, 60, 0f);
            NavButton(nav, Icon.Map, "Map", UIStyle.Mint, 0.17f, () => S.Navigator.Go(ScreenId.LevelMap));
            NavButton(nav, Icon.Bag, "Shop", UIStyle.Coral, 0.5f, () => S.Navigator.Go(ScreenId.Shop));
            NavButton(nav, Icon.Palette, "Themes", UIStyle.Violet, 0.83f, () => S.Navigator.Go(ScreenId.Themes));

            // First-time onboarding
            if (!S.Save.TutorialsSeen.Contains("home_play"))
            {
                var coach = CoachMark.Create(Safe);
                coach.Show("Welcome to Veliqora! Tap Play to begin your color-sorting adventure.", _play, 0.6f);
            }
        }

        private void BuildLogo()
        {
            _logo = UIFactory.Rect("Logo", Safe);
            UIFactory.Place(_logo, new Vector2(0.5f, 0.76f), new Vector2(1000, 520), Vector2.zero);
            var drop = UIFactory.Image(_logo, "Droplet", SpriteLibrary.Get(Icon.Droplet), Color.white);
            UIFactory.Place(drop.rectTransform, new Vector2(0.5f, 1f), new Vector2(170, 170), new Vector2(0, 10), new Vector2(0.5f, 1f));
            var dropGlow = UIFactory.Image(drop.transform, "Glow", SpriteLibrary.RadialGlow, new Color(1, 1, 1, 0.55f));
            UIFactory.Stretch(dropGlow.rectTransform, -50);
            dropGlow.transform.SetAsFirstSibling();

            var word = UIFactory.Text(_logo, "VELIQORA", UIStyle.FontDisplay, Color.white);
            UIFactory.Place(word.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(1000, 190), new Vector2(0, 10));
            word.horizontalOverflow = HorizontalWrapMode.Overflow;
            var grad = word.gameObject.AddComponent<VerticalGradientEffect>();
            grad.Top = UIStyle.Hex("#FFE58A");
            grad.Bottom = UIStyle.Hex("#FF9A1F");
            UIFactory.AddOutline(word, UIStyle.Brown, 6f);
            UIFactory.AddShadow(word, new Color(0.25f, 0.1f, 0.02f, 0.55f), new Vector2(0, -12));

            var ribbon = UIFactory.Image(_logo, "Subtitle", SpriteLibrary.Rounded(36), UIStyle.Hex("#9A4F1E"));
            UIFactory.Place(ribbon.rectTransform, new Vector2(0.5f, 0f), new Vector2(760, 96), new Vector2(0, 40), new Vector2(0.5f, 0f));
            UIFactory.AddShadow(ribbon, UIStyle.Shadow, new Vector2(0, -6));
            var sub = UIFactory.Text(ribbon.transform, "COLOR SORTING ADVENTURE", UIStyle.FontLabel, UIStyle.Hex("#FFF1C9"));
            UIFactory.AddShadow(sub, new Color(0, 0, 0, 0.25f), new Vector2(0, -3));

            if (!MotionPrefs.Reduced)
            {
                _logo.localScale = new Vector3(0.85f, 0.85f, 1);
                Tween.Scale(this, _logo, new Vector3(0.85f, 0.85f, 1), Vector3.one, 0.4f, Ease.OutBack);
            }
        }

        private void NavButton(RectTransform nav, Icon icon, string label, Color color, float x, System.Action onClick)
        {
            var holder = UIFactory.Rect("Nav_" + label, nav);
            UIFactory.Place(holder, new Vector2(x, 0.5f), new Vector2(220, 230), Vector2.zero);
            var b = UIFactory.IconButton(holder, icon, color, 150, onClick);
            UIFactory.Place(b.Rect, new Vector2(0.5f, 1f), b.Rect.sizeDelta, Vector2.zero, new Vector2(0.5f, 1f));
            var t = UIFactory.Text(holder, label, UIStyle.FontLabel, Color.white);
            UIFactory.Band(t.rectTransform, 0f, 64, 0, 0, 0f);
            UIFactory.AddShadow(t, new Color(0, 0, 0, 0.45f), new Vector2(0, -3));
        }

        private void OnPlay()
        {
            S.Save.TutorialsSeen.Add("home_play");
            S.MarkDirty();
            int level = Mathf.Clamp(S.Save.Progress.CurrentLevel, 1, S.Levels.Count);
            S.Navigator.Go(ScreenId.Gameplay, level);
        }
    }
}
