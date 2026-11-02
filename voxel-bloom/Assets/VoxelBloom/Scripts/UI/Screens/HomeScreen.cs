using UnityEngine;
using UnityEngine.UI;
using VoxelBloom.Core;

namespace VoxelBloom
{
    /// <summary>
    /// HOME (second page) built from the supplied art: painted background (Backgrounds/home_background, envelope-fitted),
    /// avatar with the live player level, coin and gem pills with live values (+ opens the shop), settings, the logo,
    /// Recompense / Evenimente / Clasament on the left, Colecție / Magazin on the right, "Joacă" (latest level) and
    /// "Niveluri" (level map). Positions are fractions measured on the reference picture (x of its width, y of its height);
    /// sizes follow the screen width, so a 2:3 screen matches the reference exactly and taller phones get more space.
    /// </summary>
    public sealed class HomeScreen : ScreenBase
    {
        // path, centre x, centre y (fractions of the reference, y from the top), width (fraction of the screen width)
        static readonly (string path, float x, float y, float w)[] Layout =
        {
            ("HUD/home_avatar", 0.151f, 0.068f, 0.193f), ("HUD/home_pill_coins_blank", 0.405f, 0.053f, 0.314f),
            ("HUD/home_pill_gems_blank", 0.705f, 0.056f, 0.27f), ("Buttons/home_settings", 0.921f, 0.055f, 0.111f),
            ("Logo/logo_home", 0.495f, 0.24f, 0.54f),
            ("Buttons/home_rewards", 0.14f, 0.346f, 0.222f), ("Buttons/home_events", 0.132f, 0.507f, 0.21f), ("Buttons/home_leaderboard", 0.13f, 0.665f, 0.206f),
            ("Buttons/home_collection", 0.881f, 0.355f, 0.194f), ("Buttons/home_shop", 0.869f, 0.512f, 0.22f),
            ("Buttons/home_play", 0.506f, 0.689f, 0.525f), ("Buttons/home_levels", 0.501f, 0.824f, 0.555f),
        };

        VText coins, gems, level;
        GameObject eventsBadge;
        int shownCoins = -1, shownGems = -1, shownLevel = -1;
        readonly System.Collections.Generic.List<(RectTransform rt, Vector2 size, bool hud)> pieces = new System.Collections.Generic.List<(RectTransform, Vector2, bool)>();
        float lastAspect;

        /// <summary>Taller phones than the 2:3 reference: the side buttons grow up to 8% (top bar, logo, Joacă and Niveluri keep the reference size, they already fill their rows).</summary>
        void LateUpdate()
        {
            var rr = Root.rect;
            if (rr.width <= 0) return;
            float aspect = rr.height / rr.width;
            if (Mathf.Abs(aspect - lastAspect) < 0.001f) return;
            lastAspect = aspect;
            float k = Mathf.Clamp(Mathf.Sqrt(aspect / 1.5f), 1f, 1.08f);
            foreach (var (rt, size, hud) in pieces) rt.sizeDelta = hud ? size : size * k;
        }

        protected override void OnBuild()
        {
            UI.Sky(Root, Theme.Hex("#2F93F0"), Theme.Hex("#A6DCFF"));
            var painted = UIArt.Get("Backgrounds/home_background", default, false);
            if (painted != null)
            {
                var bg = UI.Image(Root, "HomeBackground", painted, Color.white, false);
                bg.rectTransform.Stretch();
                var fit = bg.gameObject.AddComponent<AspectRatioFitter>();
                fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fit.aspectRatio = painted.rect.width / painted.rect.height;
            }
            else HomeBackdrop.Attach(Root);

            foreach (var (path, x, y, w) in Layout)
            {
                var r = Piece(path, x, y, w, out var button);
                switch (path)
                {
                    case "HUD/home_avatar":
                        button.OnClick = () => Router.Open<ProfileDialog>();
                        // live player level over the painted badge
                        var ring = UI.Image(r, "Level", SpriteFactory.Circle(), Theme.Gold, false);
                        ring.rectTransform.anchorMin = ring.rectTransform.anchorMax = new Vector2(0.775f, 0.225f);
                        ring.rectTransform.sizeDelta = Vector2.one * 0.34f * WidthOf(w);
                        var disc = UI.Image(ring.transform, "Disc", SpriteFactory.Circle(), Theme.Hex("#141428"), false);
                        disc.rectTransform.Stretch(5, 5, 5, 5);
                        level = UI.Text(disc.transform, "1", 40, Color.white); level.fontStyle = FontStyle.Bold;
                        level.resizeTextForBestFit = true; level.resizeTextMinSize = 14; level.resizeTextMaxSize = 40;
                        level.rectTransform.Stretch(4, 4, 4, 4);
                        break;
                    case "HUD/home_pill_coins_blank": coins = Value(r, 0.52f, 0.42f); button.OnClick = () => Router.Show<StoreScreen>(); break;
                    case "HUD/home_pill_gems_blank": gems = Value(r, 0.565f, 0.34f); button.OnClick = () => Router.Show<StoreScreen>(); break;
                    case "Buttons/home_settings": button.OnClick = () => Router.Show<SettingsScreen>(); break;
                    case "Logo/logo_home": button.enabled = false; break;
                    case "Buttons/home_rewards": button.OnClick = () => Router.Open<RewardsDialog>(); break;
                    case "Buttons/home_events":
                        button.OnClick = () => Router.Show<EventsHubScreen>();
                        var badge = UI.Image(r, "Badge", SpriteFactory.Circle(), Theme.Red, false);
                        badge.rectTransform.anchorMin = badge.rectTransform.anchorMax = new Vector2(0.92f, 0.92f);
                        badge.rectTransform.sizeDelta = Vector2.one * 64;
                        UI.Text(badge.transform, "!", 46, Color.white, TextAnchor.MiddleCenter, 0.2f, Theme.Darker(Theme.Red, 0.6f)).rectTransform.Stretch();
                        eventsBadge = badge.gameObject;
                        break;
                    case "Buttons/home_leaderboard": button.OnClick = () => Router.Show<LeaderboardScreen>(); break;
                    case "Buttons/home_collection": button.OnClick = () => Router.Show<CollectionScreen>(); break;
                    case "Buttons/home_shop": button.OnClick = () => Router.Show<StoreScreen>(); break;
                    case "Buttons/home_play":
                        button.Sound = Sfx.Whoosh;
                        button.OnClick = () => ConveyorController.StartLevel(Services.Progress.LatestConveyorLevel(ConveyorController.TotalLevels));
                        break;
                    case "Buttons/home_levels": button.OnClick = () => Router.Show<MapScreen>(); break;
                }
            }
        }

        float WidthOf(float w) => w * 1080f;

        /// <summary>An artwork piece as a button (static: no squash animation), placed by reference fractions.</summary>
        RectTransform Piece(string path, float x, float y, float w, out UIButton button)
        {
            var sp = UIArt.Get(path);
            float pw = WidthOf(w), ph = sp != null ? pw * sp.rect.height / sp.rect.width : pw * 0.4f;
            button = UI.Button(Root, null, ButtonStyle.Purple, new Vector2(pw, ph));
            var r = (RectTransform)button.transform;
            r.anchorMin = r.anchorMax = new Vector2(x, 1f - y);
            r.anchoredPosition = Vector2.zero;
            r.sizeDelta = new Vector2(pw, ph);
            pieces.Add((r, r.sizeDelta, path.StartsWith("HUD/") || path == "Buttons/home_settings" || path.StartsWith("Logo/") || path == "Buttons/home_play" || path == "Buttons/home_levels"));
            UIArt.Skin(button, path);
            button.Squash = false;
            return r;
        }

        static VText Value(RectTransform pill, float cx, float w)
        {
            var t = UI.Text(pill, "0", 52, Color.white, TextAnchor.MiddleCenter, 0.18f, Theme.Hex("#0E1430"));
            t.fontStyle = FontStyle.Bold;
            t.resizeTextForBestFit = true; t.resizeTextMinSize = 18; t.resizeTextMaxSize = 52;
            t.rectTransform.anchorMin = new Vector2(cx - w / 2, 0.16f);
            t.rectTransform.anchorMax = new Vector2(cx + w / 2, 0.84f);
            t.rectTransform.offsetMin = t.rectTransform.offsetMax = Vector2.zero;
            return t;
        }

        void Update()
        {
            if (!Services.Initialized) return;
            int c = Services.Wallet.Coins; if (c != shownCoins) { shownCoins = c; coins.text = c.ToString("N0", System.Globalization.CultureInfo.InvariantCulture).Replace(",", " "); }
            int g = Services.Save.Gems; if (g != shownGems) { shownGems = g; gems.text = g.ToString(); }
            int l = 1 + Services.Progress.ConveyorTotalStars / 10; if (l != shownLevel) { shownLevel = l; level.text = l.ToString(); }
        }

        public override bool FadeIn => false;

        public override void OnShow()
        {
            HomeBackdrop.SetActive(UIArt.Get("Backgrounds/home_background", default, false) == null);
            Services.Events.Refresh(Services.Now);
            if (eventsBadge != null) eventsBadge.SetActive(Services.Events.AnyClaimable(Services.Now));
            App.State.ForceReset(GameState.Home);
        }

        public override void OnHide() { HomeBackdrop.SetActive(false); }
        public override void OnBack() { Router.Show<TitleScreen>(); }
    }
}
