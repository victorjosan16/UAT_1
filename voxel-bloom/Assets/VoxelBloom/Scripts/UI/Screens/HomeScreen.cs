using UnityEngine;
using UnityEngine.UI;
using VoxelBloom.Core;

namespace VoxelBloom
{
    /// <summary>HOME: top bar, logo, animated voxel fox diorama, PLAY, bottom navigation.</summary>
    public sealed class HomeScreen : ScreenBase
    {
        VoxelStage stage;
        VText levelLabel;
        GameObject rewardsBadge;
        RectTransform[] clouds;
        RectTransform logo;
        Transform playButton;

        protected override void OnBuild()
        {
            UI.Sky(Root, Theme.SkyTop, Theme.SkyBottom);
            clouds = new RectTransform[4];
            for (int i = 0; i < clouds.Length; i++)
            {
                var c = UI.Image(Root, "Cloud", SpriteFactory.Rounded(48), new Color(1, 1, 1, 0.75f));
                clouds[i] = c.rectTransform;
                clouds[i].Place(new Vector2(0, 1), new Vector2(150 + i * 300, -380 - (i % 2) * 140), new Vector2(260 + (i % 3) * 60, 90));
            }
            // soft hills
            var hill = UI.Image(Root, "Hill", SpriteFactory.Circle(), Theme.Hex("#9EE27F"), false);
            hill.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0, -900), new Vector2(2600, 1900));
            var hill2 = UI.Image(Root, "Hill2", SpriteFactory.Circle(), Theme.Hex("#B8EE96"), false);
            hill2.rectTransform.Place(new Vector2(0.15f, 0), new Vector2(0, -1050), new Vector2(1800, 1900));

            var top = TopBar.Create(Root);
            top.OnSettings = () => Router.Open<SettingsDialog>();
            top.OnAddCoins = () => Router.Show<ShopScreen>();
            top.OnAvatar = () => Router.Open<ProfileDialog>();

            logo = BuildLogo(Root);

            stage = VoxelStage.Create("Home", 1024, false, true);
            var fox = Services.Data.GetModel("fox");
            if (fox != null) stage.ShowModel(fox, 1.9f);
            stage.ScalePedestal(1.9f);
            Props.AddTree(stage.PedestalRoot, new Vector3(-6.2f, 3f, 4.5f), 0.9f, 3);
            Props.AddTree(stage.PedestalRoot, new Vector3(6.5f, 3f, 3.5f), 0.75f, 9);
            Props.AddFlower(stage.PedestalRoot, new Vector3(-4.8f, 3f, -5.2f), 0.6f, new Color32(255, 120, 190, 255));
            Props.AddFlower(stage.PedestalRoot, new Vector3(5.2f, 3f, -4.6f), 0.6f, new Color32(255, 255, 255, 255));
            Props.AddFlower(stage.PedestalRoot, new Vector3(-2f, 3f, -7.4f), 0.5f, new Color32(186, 140, 255, 255));
            stage.Bob = 0.03f;
            stage.TargetYaw = stage.Yaw = -24f;
            var raw = UI.Raw(Root, "Diorama", stage.Texture);
            raw.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(1000, 1000));

            var play = UI.Button(Root, "PLAY", ButtonStyle.Green, new Vector2(620, 190), Icon.Play, 96);
            ((RectTransform)play.transform).Place(new Vector2(0.5f, 0), new Vector2(0, 420), new Vector2(620, 190));
            play.OnClick = Play;
            var levels = UI.Button(Root, "Level 1", ButtonStyle.Purple, new Vector2(360, 96), Icon.Star, 44);
            ((RectTransform)levels.transform).Place(new Vector2(0.5f, 0), new Vector2(0, 590), new Vector2(360, 96));
            levels.OnClick = () => Router.Show<LevelSelectScreen>();
            levelLabel = levels.Face.Find("Label").GetComponent<VText>();
            playButton = play.transform;

            BuildNav();
        }

        RectTransform BuildLogo(Transform parent)
        {
            var root = UI.Rect(parent, "Logo").Place(new Vector2(0.5f, 1), new Vector2(0, -330), new Vector2(900, 330));
            var l1 = UI.Text(root, "Voxel", 170, Color.white, TextAnchor.MiddleCenter, 0.3f, Theme.PurpleDark);
            l1.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(-30, -80), new Vector2(800, 170));
            l1.fontStyle = FontStyle.Bold;
            l1.SetGradient(Color.white, Theme.Lavender);
            var l2 = UI.Text(root, "Bloom", 170, Color.white, TextAnchor.MiddleCenter, 0.3f, Theme.PurpleDark);
            l2.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(-60, -230), new Vector2(800, 170));
            l2.fontStyle = FontStyle.Bold;
            l2.SetGradient(Color.white, Theme.Lavender);
            var l3 = UI.Text(root, "3D", 150, Theme.Gold, TextAnchor.MiddleCenter, 0.3f, Theme.Darker(Theme.GoldDark, 0.7f));
            l3.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(300, -240), new Vector2(260, 160));
            l3.fontStyle = FontStyle.Bold;
            l3.SetGradient(Theme.Lighter(Theme.Gold, 0.5f), Theme.GoldDark);
            l3.rectTransform.localRotation = Quaternion.Euler(0, 0, 6);
            var cube = UI.Icon(root, Icon.Cube, 110);
            cube.rectTransform.Place(new Vector2(0, 1), new Vector2(70, -40), new Vector2(110, 110));
            cube.rectTransform.localRotation = Quaternion.Euler(0, 0, -12);
            return root;
        }

        void BuildNav()
        {
            var bar = UI.Rect(Root, "Nav").BottomBand(30, 230, 40);
            var hl = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 24; hl.childForceExpandWidth = true; hl.childControlWidth = true; hl.childControlHeight = true;
            AddNav(bar, Icon.Paw, "Collection", () => Router.Show<CollectionScreen>());
            var rewards = AddNav(bar, Icon.Gift, "Rewards", () => Router.Open<RewardsDialog>());
            AddNav(bar, Icon.Shop, "Shop", () => Router.Show<ShopScreen>());
            AddNav(bar, Icon.Calendar, "Events", () => Router.Show<EventsScreen>());
            var badge = UI.Image(rewards, "Badge", SpriteFactory.Circle(), Theme.Red, false);
            badge.rectTransform.Place(new Vector2(1, 1), new Vector2(-18, -18), new Vector2(56, 56));
            UI.Text(badge.transform, "!", 40, Color.white).rectTransform.Stretch();
            rewardsBadge = badge.gameObject;
        }

        RectTransform AddNav(Transform parent, Icon icon, string label, System.Action onClick)
        {
            var b = UI.Button(parent, null, ButtonStyle.White, new Vector2(220, 220));
            var face = b.Face;
            UI.Icon(face, icon, 120).rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0, 22), new Vector2(120, 120));
            var t = UI.Text(face, label, 34, Theme.Ink);
            t.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0, 34), new Vector2(210, 44));
            b.OnClick = onClick;
            return (RectTransform)b.transform;
        }

        void Play()
        {
            int level = Services.Progress.LatestPlayableLevel();
            GameplayController.StartLevel(level);
        }

        public override void OnShow()
        {
            stage.SetActive(true);
            levelLabel.text = "Level " + Services.Progress.LatestPlayableLevel();
            rewardsBadge.SetActive(Services.Daily.CanClaim(Services.Now));
            App.State.ForceReset(GameState.Home);
            Tween.Scale(logo, Vector3.one * 0.85f, Vector3.one, 0.5f, Ease.OutElastic);
        }

        public override void OnHide() { stage.SetActive(false); }

        public override void OnBack() { Application.Quit(); }

        void Update()
        {
            // parallax clouds + gentle logo float
            for (int i = 0; i < clouds.Length; i++)
            {
                var p = clouds[i].anchoredPosition;
                p.x += (12f + i * 5f) * Time.unscaledDeltaTime;
                if (p.x > 1300) p.x = -250;
                clouds[i].anchoredPosition = p;
            }
            logo.anchoredPosition = new Vector2(0, -330 + Mathf.Sin(Time.unscaledTime * 1.4f) * 8f);
            if (playButton != null && !Router.HasDialog) playButton.localScale = Vector3.one * (1f + Mathf.Sin(Time.unscaledTime * 3f) * 0.025f);
            if (stage != null) stage.TargetYaw = -24f + Mathf.Sin(Time.unscaledTime * 0.5f) * 14f;
        }
    }
}
