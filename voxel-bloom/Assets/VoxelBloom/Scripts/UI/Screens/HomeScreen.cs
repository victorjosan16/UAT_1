using UnityEngine;
using UnityEngine.UI;
using VoxelBloom.Core;

namespace VoxelBloom
{
    /// <summary>
    /// HOME as in the reference mockup (static): landscape with the waterfall, top bar (avatar + level, coins +, gems +,
    /// settings), Recompense / Evenimente / Clasament on the left, Colecție / Magazin on the right and the big green
    /// "Niveluri" button that opens the level map.
    /// </summary>
    public sealed class HomeScreen : ScreenBase
    {
        GameObject eventsBadge;

        protected override void OnBuild()
        {
            UI.Sky(Root, Theme.Hex("#2F93F0"), Theme.Hex("#A6DCFF"));
            HomeBackdrop.Attach(Root);
            var top = TopBar.Create(Root, false);
            top.OnSettings = () => Router.Show<SettingsScreen>();
            top.OnAddCoins = () => Router.Show<StoreScreen>();
            top.OnAvatar = () => Router.Open<ProfileDialog>();

            Side("Buttons/nav_rewards", null, "Recompense", new Vector2(0.16f, 0.65f), () => Router.Open<RewardsDialog>());
            var ev = Side("Buttons/nav_events", null, "Evenimente", new Vector2(0.16f, 0.49f), () => Router.Show<EventsHubScreen>());
            Side(null, Icon.Trophy, "Clasament", new Vector2(0.16f, 0.33f), () => Router.Show<LeaderboardScreen>());
            Side("Buttons/nav_collection", null, "Colecție", new Vector2(0.84f, 0.64f), () => Router.Show<CollectionScreen>());
            Side("Buttons/nav_shop", null, "Magazin", new Vector2(0.84f, 0.48f), () => Router.Show<StoreScreen>());
            var badge = UI.Image(ev, "Badge", SpriteFactory.Circle(), Theme.Red, false);
            badge.rectTransform.Place(new Vector2(1, 1), new Vector2(-14, -12), new Vector2(60, 60));
            UI.Text(badge.transform, "!", 44, Color.white, TextAnchor.MiddleCenter, 0.2f, Theme.Darker(Theme.Red, 0.6f)).rectTransform.Stretch();
            eventsBadge = badge.gameObject;

            var glow = UI.Image(Root, "Glow", SpriteFactory.Glow(), new Color(1f, 0.95f, 0.45f, 0.45f), false);
            glow.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0, 300), new Vector2(980, 340));
            var lv = UI.Button(Root, "Niveluri", ButtonStyle.Green, new Vector2(800, 240), Icon.Map, 116);
            ((RectTransform)lv.transform).Place(new Vector2(0.5f, 0), new Vector2(0, 300), new Vector2(800, 240));
            lv.Sound = Sfx.Whoosh;
            lv.OnClick = () => Router.Show<MapScreen>();
            foreach (var b in GetComponentsInChildren<UIButton>(true)) b.Squash = false; // static page
        }

        RectTransform Side(string art, Icon? icon, string label, Vector2 anchor, System.Action onClick)
        {
            var sp = art != null ? UIArt.Get(art) : null;
            float w = 290f, h = sp != null ? w * sp.rect.height / sp.rect.width : 290f;
            var b = UI.Button(Root, null, ButtonStyle.Purple, new Vector2(w, h));
            var r = (RectTransform)b.transform;
            r.anchorMin = r.anchorMax = anchor; r.sizeDelta = new Vector2(w, h);
            if (sp != null) UIArt.Skin(b, art);
            else
            {
                UI.Icon(b.Face, icon ?? Icon.Star, 150).rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(150, 150));
                var t = UI.Text(b.Face, label, 44, Color.white, TextAnchor.MiddleCenter, 0.2f, Theme.PurpleDark);
                t.fontStyle = FontStyle.Bold;
                t.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0, 46), new Vector2(286, 60));
            }
            b.OnClick = onClick;
            return r;
        }

        public override bool FadeIn => false;

        public override void OnShow()
        {
            HomeBackdrop.SetActive(true);
            Services.Events.Refresh(Services.Now);
            eventsBadge.SetActive(Services.Events.AnyClaimable(Services.Now));
            App.State.ForceReset(GameState.Home);
        }

        public override void OnHide() { HomeBackdrop.SetActive(false); }
        public override void OnBack() { Router.Show<TitleScreen>(); }
    }
}
