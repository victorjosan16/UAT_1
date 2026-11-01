using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VoxelBloom.Core;

namespace VoxelBloom
{
    /// <summary>
    /// Premium HOME: real-state HUD, dimensional logo with rays/leaves/floating cubes, a 3D voxel diorama
    /// (floating island, trees, flowers, cliffs + waterfall) with the selected animated character, a character
    /// carousel (6 per page, arrows wrap, locked states), level badge, PLAY and bottom navigation.
    /// Layout follows the 1080x1920 reference: top block anchored to the top, controls anchored to the bottom,
    /// the diorama stretches in between (taller phones get a bigger diorama).
    /// </summary>
    public sealed class HomeScreen : ScreenBase
    {
        VoxelStage stage;
        CharacterPreview hero;
        CharacterCatalog catalog;
        int selected;
        VText levelLabel, lockLabel;
        RectTransform lockPill;
        GameObject rewardsBadge;
        RectTransform[] clouds;
        RectTransform logo, rays;
        readonly List<RectTransform> floaters = new List<RectTransform>();
        readonly List<CarouselTile> tiles = new List<CarouselTile>();
        readonly List<Image> dots = new List<Image>();
        Transform playButton;
        int page = -1;
        VText pageLabel;

        sealed class CarouselTile
        {
            public RectTransform Root; public Image Frame, Glow; public RawImage Thumb; public Image Lock; public int Index = -1;
        }

        static readonly Color[] TileBg =
        {
            Theme.Hex("#FFE7C7"), Theme.Hex("#DDF5D2"), Theme.Hex("#FFF4C2"), Theme.Hex("#F1E3FF"), Theme.Hex("#D7F0FF"),
            Theme.Hex("#E6E9F5"), Theme.Hex("#F3E1CC"), Theme.Hex("#FFE3E3"), Theme.Hex("#DDE8FF"), Theme.Hex("#E3F2FA"),
        };

        protected override void OnBuild()
        {
            catalog = new CharacterCatalog(Services.Config);
            BuildBackground();

            var top = TopBar.Create(Root);
            top.OnSettings = () => Router.Open<SettingsDialog>();
            top.OnAddCoins = () => Router.Show<ShopScreen>();
            top.OnAvatar = () => Router.Open<ProfileDialog>();

            BuildDiorama();
            logo = BuildLogo(Root);
            BuildCarousel();
            BuildPlay();
            BuildNav();
            BuildCornerDecor();
        }

        // ------------------------------------------------------------------ background
        void BuildBackground()
        {
            UI.Sky(Root, Theme.Hex("#5DB8FF"), Theme.Hex("#E9F6FF"));
            var far = UI.Image(Root, "FarHills", SpriteFactory.Circle(), Theme.Hex("#BFE6B0"), false);
            far.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(-260, -40), new Vector2(1500, 900));
            var far2 = UI.Image(Root, "FarHills2", SpriteFactory.Circle(), Theme.Hex("#CDEFC0"), false);
            far2.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(420, -80), new Vector2(1300, 800));
            clouds = new RectTransform[6];
            for (int i = 0; i < clouds.Length; i++)
            {
                var c = UI.Image(Root, "Cloud", SpriteFactory.Glow(), new Color(1, 1, 1, i < 3 ? 0.95f : 0.75f), false);
                clouds[i] = c.rectTransform;
                float w = 300 + (i % 3) * 110;
                clouds[i].Place(new Vector2(0, i < 3 ? 1 : 0), new Vector2(120 + i * 260, i < 3 ? -260 - (i % 2) * 120 : 420 + (i % 2) * 160), new Vector2(w, w * 0.55f));
            }
            // soft bottom haze behind the controls
            var haze = UI.Image(Root, "Haze", SpriteFactory.VerticalGradient(new Color(1, 1, 1, 0f), new Color(0.93f, 0.95f, 1f, 0.95f)), Color.white, false);
            haze.rectTransform.BottomBand(0, 900, 0);
        }

        // ------------------------------------------------------------------ diorama
        void BuildDiorama()
        {
            stage = VoxelStage.Create("Home", 1024, false);
            stage.Model.gameObject.SetActive(false);
            var scene = new GameObject("Scenery").transform;
            scene.SetParent(stage.transform, false);
            var anim = scene.gameObject.AddComponent<DioramaAnimator>();
            const float v = 0.085f;
            Props.AddCliffWaterfall(scene, new Vector3(2.6f, -1.25f, 3.6f), 0.13f);
            Props.AddTree(scene, new Vector3(-2.9f, -0.95f, 2.6f), 0.15f, 3);
            Props.AddTree(scene, new Vector3(-1.9f, -0.75f, 4.4f), 0.11f, 7);
            Props.AddTree(scene, new Vector3(3.3f, -0.9f, 1.4f), 0.13f, 11);
            Props.AddIsland(scene, Vector3.zero, 19, v);
            var f1 = Props.AddFlower(scene, new Vector3(-1.25f, 0f, -0.8f), v * 1.25f, new Color32(255, 110, 190, 255), new Color32(255, 214, 64, 255));
            var f2 = Props.AddFlower(scene, new Vector3(1.25f, 0f, -0.75f), v * 1.25f, new Color32(255, 255, 255, 255), new Color32(255, 200, 40, 255));
            var f3 = Props.AddFlower(scene, new Vector3(-1.55f, 0f, 0.4f), v, new Color32(190, 140, 255, 255), new Color32(255, 230, 120, 255));
            var f4 = Props.AddFlower(scene, new Vector3(1.5f, 0f, 0.5f), v, new Color32(255, 214, 64, 255), new Color32(255, 150, 40, 255));
            Props.AddBush(scene, new Vector3(-0.9f, 0f, 1.25f), v);
            Props.AddBush(scene, new Vector3(0.95f, 0f, 1.3f), v);
            anim.Add(f1, 0f); anim.Add(f2, 1.3f); anim.Add(f3, 2.1f); anim.Add(f4, 3.4f);

            hero = CharacterPreview.Create(stage.Pivot);
            hero.Sparkles = stage.Fragments;
            stage.FrameScene(4.4f, 3.1f, 0.85f);
            stage.TargetYaw = stage.Yaw = -18f;

            var raw = UI.Raw(Root, "Diorama", stage.Texture);
            raw.rectTransform.anchorMin = Vector2.zero; raw.rectTransform.anchorMax = Vector2.one;
            raw.rectTransform.offsetMin = new Vector2(0, 700); raw.rectTransform.offsetMax = new Vector2(0, -330);
            var arf = raw.gameObject.AddComponent<AspectRatioFitter>();
            arf.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            arf.aspectRatio = 1f;
            raw.raycastTarget = true;
            var input = raw.gameObject.AddComponent<ModelInput>();
            input.Stage = stage;
            input.OnTapUv = _ => { hero.React(); Services.Audio.Play(Sfx.Star, 1.2f); };

            // arrows beside the character
            var left = UI.IconButton(Root, Icon.ArrowLeft, 112, ButtonStyle.White);
            ((RectTransform)left.transform).Place(new Vector2(0, 1), new Vector2(120, -760), new Vector2(112, 112));
            left.OnClick = () => Select(catalog.Step(selected, -1), true);
            var right = UI.IconButton(Root, Icon.ArrowRight, 112, ButtonStyle.White);
            ((RectTransform)right.transform).Place(new Vector2(1, 1), new Vector2(-120, -760), new Vector2(112, 112));
            right.OnClick = () => Select(catalog.Step(selected, +1), true);
            foreach (var b in new[] { left, right }) { var cg = b.gameObject.AddComponent<CanvasGroup>(); cg.alpha = 0.9f; }
        }

        // ------------------------------------------------------------------ logo
        RectTransform BuildLogo(Transform parent)
        {
            var root = UI.Rect(parent, "Logo").Place(new Vector2(0.5f, 1), new Vector2(0, -330), new Vector2(900, 330));
            var r = UI.Image(root, "Rays", SpriteFactory.Rays(), new Color(1, 1, 0.9f, 0.85f), false);
            rays = r.rectTransform;
            rays.Place(new Vector2(0.5f, 0.5f), new Vector2(0, -10), new Vector2(900, 900));
            var glow = UI.Image(root, "Glow", SpriteFactory.Glow(), new Color(1f, 0.85f, 1f, 0.7f), false);
            glow.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820, 420));
            // dimensional letters: a darker extruded copy below the face
            MakeWord(root, "Voxel", 176, new Vector2(-10, 70), Color.white, Theme.Lavender, Theme.PurpleDark);
            MakeWord(root, "Bloom", 176, new Vector2(-70, -90), Color.white, Theme.Lavender, Theme.PurpleDark);
            var three = MakeWord(root, "3D", 160, new Vector2(290, -100), Theme.Lighter(Theme.Gold, 0.55f), Theme.GoldDark, Theme.Darker(Theme.Orange, 0.8f));
            three.localRotation = Quaternion.Euler(0, 0, 6);
            AddDecor(root, Icon.Leaf, new Vector2(330, 120), 120, -30);
            AddDecor(root, Icon.Leaf, new Vector2(-370, -60), 110, 200);
            AddDecor(root, Icon.Leaf, new Vector2(380, 40), 80, 20);
            floaters.Add(AddDecor(root, Icon.Cube, new Vector2(-330, 140), 120, -12));
            floaters.Add(AddDecor(root, Icon.CubePink, new Vector2(350, -230), 100, 18));
            floaters.Add(AddDecor(root, Icon.CubeBlue, new Vector2(-300, -250), 60, 8));
            return root;
        }

        RectTransform MakeWord(Transform parent, string text, float size, Vector2 pos, Color top, Color bottom, Color edge)
        {
            var holder = UI.Rect(parent, "Word_" + text).Place(new Vector2(0.5f, 0.5f), pos, new Vector2(800, size + 20));
            for (int d = 4; d >= 1; d--)
            {
                var ext = UI.Text(holder, text, size, Theme.Darker(edge, 0.85f), TextAnchor.MiddleCenter, 0.3f, Theme.Darker(edge, 0.7f));
                ext.fontStyle = FontStyle.Bold;
                ext.rectTransform.Stretch(0, 0, d * 3, -d * 3);
            }
            var face = UI.Text(holder, text, size, Color.white, TextAnchor.MiddleCenter, 0.3f, edge);
            face.fontStyle = FontStyle.Bold;
            face.rectTransform.Stretch();
            face.SetGradient(top, bottom);
            return holder;
        }

        RectTransform AddDecor(Transform parent, Icon icon, Vector2 pos, float size, float rot)
        {
            var img = UI.Icon(parent, icon, size);
            img.rectTransform.Place(new Vector2(0.5f, 0.5f), pos, new Vector2(size, size));
            img.rectTransform.localRotation = Quaternion.Euler(0, 0, rot);
            return img.rectTransform;
        }

        // ------------------------------------------------------------------ carousel
        void BuildCarousel()
        {
            var row = UI.Rect(Root, "Carousel").BottomBand(760, 150, 40);
            for (int i = 0; i < CharacterCatalog.PerPage; i++)
            {
                var t = new CarouselTile();
                var b = UI.Button(row, null, ButtonStyle.White, new Vector2(146, 146));
                t.Root = (RectTransform)b.transform;
                t.Root.Place(new Vector2((i + 0.5f) / CharacterCatalog.PerPage, 0.5f), Vector2.zero, new Vector2(146, 146));
                var glow = UI.Image(t.Root, "Selected", SpriteFactory.Rounded(44), Theme.Gold);
                glow.rectTransform.Stretch(-10, -10, -10, -14);
                glow.transform.SetAsFirstSibling();
                t.Glow = glow;
                var bg = UI.Image(b.Face, "Bg", SpriteFactory.Rounded(36), Color.white);
                bg.rectTransform.Stretch(8, 8, 8, 8);
                t.Frame = bg;
                t.Thumb = UI.Raw(bg.transform, "Thumb", null);
                t.Thumb.rectTransform.Stretch(2, 2, 2, 2);
                t.Lock = UI.Icon(b.Face, Icon.Lock, 56);
                t.Lock.rectTransform.Place(new Vector2(1, 0), new Vector2(-22, 26), new Vector2(56, 56));
                int slot = i;
                b.Sound = Sfx.Select;
                b.OnClick = () => { if (tiles[slot].Index >= 0) Select(tiles[slot].Index, true); };
                tiles.Add(t);
            }
            var dotRow = UI.Rect(Root, "Dots").BottomBand(706, 40, 0);
            if (catalog.PageCount > 5)
            {
                // many mascots: compact "page / pages" indicator with mini dots for neighbouring pages
                pageLabel = UI.Text(dotRow, "", 32, Theme.Purple, TextAnchor.MiddleCenter, 0.15f, Color.white);
                pageLabel.fontStyle = FontStyle.Bold;
                pageLabel.rectTransform.Stretch();
            }
            for (int p = 0; p < (catalog.PageCount > 5 ? 0 : catalog.PageCount); p++)
            {
                var d = UI.Image(dotRow, "Dot", SpriteFactory.Circle(), Theme.Lavender, false);
                d.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2((p - (catalog.PageCount - 1) * 0.5f) * 48, 0), new Vector2(26, 26));
                int pg = p;
                var btn = d.gameObject.AddComponent<UIButton>();
                d.raycastTarget = true;
                btn.OnClick = () => Select(Mathf.Min(pg * CharacterCatalog.PerPage, catalog.Count - 1), true);
                dots.Add(d);
            }
            var lp = UI.Panel(Root, "LockPill", new Color(0.23f, 0.18f, 0.36f, 0.85f), 30, false);
            lockPill = (RectTransform)lp.transform.parent;
            lockPill.Place(new Vector2(0.5f, 0), new Vector2(0, 930), new Vector2(620, 64));
            lockLabel = UI.Text(lockPill, "", 34, Color.white);
            lockLabel.rectTransform.Stretch(20, 20, 0, 0);
        }

        void Select(int index, bool animate)
        {
            selected = Mathf.Clamp(index, 0, catalog.Count - 1);
            var e = catalog.Characters[selected];
            bool unlocked = catalog.IsUnlocked(e.Id, Services.Save);
            var grid = Services.Data.GetModel(e.Id);
            if (grid != null)
            {
                float vs = 2.0f / Mathf.Max(grid.X, Mathf.Max(grid.Y, grid.Z));
                hero.Show(grid, Services.Data.GetAnim(e.Id), vs, !unlocked, selected);
                stage.SetShadowSize(Mathf.Max(grid.X, grid.Z) * vs * 1.3f);
            }
            if (unlocked && Services.Save.HomeCharacter != e.Id) { Services.Save.HomeCharacter = e.Id; Services.Commit(); }
            lockPill.gameObject.SetActive(!unlocked);
            lockLabel.text = e.Name + " - " + catalog.UnlockText(e);
            if (animate) Services.Audio.Play(Sfx.Select, 1f + selected * 0.03f);
            RefreshCarousel();
        }

        void RefreshCarousel()
        {
            int p = CharacterCatalog.PageOf(selected);
            bool pageChanged = p != page;
            page = p;
            for (int i = 0; i < tiles.Count; i++)
            {
                int idx = p * CharacterCatalog.PerPage + i;
                var t = tiles[i];
                bool exists = idx < catalog.Count;
                t.Root.gameObject.SetActive(exists);
                if (!exists) { t.Index = -1; continue; }
                t.Index = idx;
                var e = catalog.Characters[idx];
                bool unlocked = catalog.IsUnlocked(e.Id, Services.Save);
                t.Thumb.texture = ThumbnailRenderer.Get(e.Id);
                t.Thumb.color = unlocked ? Color.white : new Color(0.3f, 0.27f, 0.42f, 0.8f);
                t.Frame.color = TileBg[idx % TileBg.Length];
                t.Lock.gameObject.SetActive(!unlocked);
                bool sel = idx == selected;
                t.Glow.gameObject.SetActive(sel);
                t.Root.localScale = Vector3.one * (sel ? 1.08f : 1f);
                if (sel) Tween.Pop(t.Root, 0.12f, 0.25f);
                if (pageChanged) Tween.Scale(t.Root, Vector3.one * 0.6f, Vector3.one * (sel ? 1.08f : 1f), 0.25f + i * 0.03f, Ease.OutBack);
            }
            for (int d = 0; d < dots.Count; d++) dots[d].color = d == p ? Theme.Purple : Theme.Lavender;
            if (pageLabel != null) pageLabel.text = (selected + 1) + ". " + catalog.Characters[selected].Name + "   ·   " + (p + 1) + " / " + catalog.PageCount;
        }

        // ------------------------------------------------------------------ play / nav
        void BuildPlay()
        {
            var levels = UI.Button(Root, "Level 1", ButtonStyle.Purple, new Vector2(390, 96), Icon.Star, 46);
            ((RectTransform)levels.transform).Place(new Vector2(0.5f, 0), new Vector2(0, 640), new Vector2(390, 96));
            levels.OnClick = () => Router.Show<LevelSelectScreen>();
            levelLabel = levels.Face.Find("Label").GetComponent<VText>();

            var play = UI.Button(Root, "PLAY", ButtonStyle.Green, new Vector2(680, 200), Icon.Play, 110);
            ((RectTransform)play.transform).Place(new Vector2(0.5f, 0), new Vector2(0, 462), new Vector2(680, 200));
            play.OnClick = Play;
            play.Sound = Sfx.Whoosh;
            playButton = play.transform;
            var shine = UI.Image(play.Face, "Shine", SpriteFactory.Glow(), new Color(1, 1, 1, 0.35f), false);
            shine.rectTransform.Place(new Vector2(0.3f, 0.75f), Vector2.zero, new Vector2(320, 70));
        }

        void BuildNav()
        {
            var bar = UI.Rect(Root, "Nav").BottomBand(50, 240, 36);
            var hl = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 22; hl.childForceExpandWidth = true; hl.childControlWidth = true; hl.childControlHeight = true;
            AddNav(bar, Icon.Paw, "Collection", () => Router.Show<CollectionScreen>());
            var rewards = AddNav(bar, Icon.Gift, "Rewards", () => Router.Open<RewardsDialog>());
            AddNav(bar, Icon.Shop, "Shop", () => Router.Show<ShopScreen>());
            AddNav(bar, Icon.Calendar, "Events", () => Router.Show<EventsScreen>());
            var badge = UI.Image(rewards, "Badge", SpriteFactory.Circle(), Theme.Red, false);
            badge.rectTransform.Place(new Vector2(1, 1), new Vector2(-16, -10), new Vector2(64, 64));
            UI.Text(badge.transform, "!", 46, Color.white, TextAnchor.MiddleCenter, 0.2f, Theme.Darker(Theme.Red, 0.6f)).rectTransform.Stretch();
            rewardsBadge = badge.gameObject;
        }

        RectTransform AddNav(Transform parent, Icon icon, string label, System.Action onClick)
        {
            var b = UI.Button(parent, null, ButtonStyle.White, new Vector2(230, 230));
            var face = b.Face;
            var tint = UI.Image(face, "Tint", SpriteFactory.VerticalGradient(Color.white, Theme.Hex("#EEE8FF")), Color.white, false);
            tint.rectTransform.Stretch(6, 6, 6, 6);
            UI.Icon(face, icon, 128).rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0, 24), new Vector2(128, 128));
            var t = UI.Text(face, label, 38, Theme.Ink);
            t.fontStyle = FontStyle.Bold;
            t.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0, 36), new Vector2(220, 48));
            b.OnClick = onClick;
            return (RectTransform)b.transform;
        }

        void BuildCornerDecor()
        {
            foreach (var (pos, size, icon, rot) in new[] { (new Vector2(70, 560), 120f, Icon.Flower, 10f), (new Vector2(1010, 380), 110f, Icon.Flower, -12f), (new Vector2(40, 680), 110f, Icon.Leaf, 60f), (new Vector2(1040, 520), 120f, Icon.Leaf, 120f) })
            {
                var img = UI.Icon(Root, icon, size);
                img.rectTransform.Place(new Vector2(0, 0), pos, new Vector2(size, size));
                img.rectTransform.localRotation = Quaternion.Euler(0, 0, rot);
                img.transform.SetSiblingIndex(3);
            }
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
            Select(catalog.IndexOf(catalog.ResolveSaved(Services.Save.HomeCharacter, Services.Save)), false);
            Tween.Scale(logo, Vector3.one * 0.85f, Vector3.one, 0.6f, Ease.OutElastic);
        }

        public override void OnHide() { stage.SetActive(false); }

        public override void OnBack() { Application.Quit(); }

        void Update()
        {
            float t = Time.unscaledTime, dt = Time.unscaledDeltaTime;
            bool calm = Services.ReducedEffects;
            for (int i = 0; i < clouds.Length; i++)
            {
                var p = clouds[i].anchoredPosition;
                p.x += (calm ? 4f : 10f + i * 4f) * dt;
                if (p.x > 1350) p.x = -300;
                clouds[i].anchoredPosition = p;
            }
            logo.anchoredPosition = new Vector2(0, -330 + Mathf.Sin(t * 1.4f) * (calm ? 2f : 7f));
            if (rays != null && !calm) rays.localRotation = Quaternion.Euler(0, 0, t * 6f);
            for (int i = 0; i < floaters.Count; i++)
                if (floaters[i] != null) floaters[i].localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 1.1f + i) * 12f);
            if (playButton != null && !Router.HasDialog && !calm) playButton.localScale = Vector3.one * (1f + Mathf.Sin(t * 3f) * 0.022f);
        }
    }
}
