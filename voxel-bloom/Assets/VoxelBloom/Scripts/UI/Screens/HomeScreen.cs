using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VoxelBloom.Core;

namespace VoxelBloom
{
    /// <summary>
    /// HOME, built to match the premium reference (static: nothing moves on its own; the model turns only when dragged).
    /// HUD (avatar + level, lives, coins +, gems +, settings), logo with crown/leaves/cubes and the "Puzzle cu Obiecte 3D"
    /// ribbon, voxel landscape, the selected model on a purple/gold 360° pedestal with round purple arrows, a 6-tile
    /// carousel strip (locked models as grey silhouettes with a gold lock) and "N. Name · page/pages", the big green
    /// "Joacă" button, the "Nivel N" badge with laurels and the purple bottom navigation.
    /// Layout: 1080x1920 reference; top block anchored to the top, controls to the bottom.
    /// </summary>
    public sealed class HomeScreen : ScreenBase
    {
        VoxelStage stage;
        CharacterPreview hero;
        CharacterCatalog catalog;
        int selected;
        VText levelLabel, caption;
        GameObject rewardsBadge;
        readonly List<CarouselTile> tiles = new List<CarouselTile>();

        sealed class CarouselTile { public RectTransform Root; public Image Frame, Glow; public RawImage Thumb; public Image Lock; public int Index = -1; }

        protected override void OnBuild()
        {
            catalog = new CharacterCatalog(Services.Config);
            UI.Sky(Root, Theme.Hex("#2F93F0"), Theme.Hex("#A6DCFF"));
            var rays = UI.Image(Root, "SunRays", SpriteFactory.Rays(), new Color(1f, 1f, 1f, 0.35f), false);
            rays.rectTransform.anchorMin = rays.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            rays.rectTransform.anchoredPosition = new Vector2(0, -470);
            rays.rectTransform.sizeDelta = new Vector2(1500, 1500);
            painted = UIArt.Get("Backgrounds/home_background", default, false); // optional drop-in
            if (painted != null)
            {
                // full-screen painted landscape (drop-in: Art/Backgrounds/Resources/VBUI/Backgrounds/home_background.png)
                var bg = UI.Image(Root, "HomeBackground", painted, Color.white, false);
                bg.rectTransform.Stretch();
                var fit = bg.gameObject.AddComponent<AspectRatioFitter>();
                fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fit.aspectRatio = painted.rect.width / painted.rect.height;
            }

            BuildDiorama();
            var top = TopBar.Create(Root);
            top.OnSettings = () => Router.Open<SettingsDialog>();
            top.OnAddCoins = () => Router.Show<ShopScreen>();
            top.OnAvatar = () => Router.Open<ProfileDialog>();
            BuildLogo();
            BuildCarousel();
            BuildPlay();
            BuildNav();
            foreach (var b in GetComponentsInChildren<UIButton>(true)) b.Squash = false; // static page
        }

        public override bool FadeIn => false;
        Sprite painted;
        Vector3 camPos, camLook;
        float camFov;

        // ------------------------------------------------------------------ full-screen 3D landscape + pedestal (static)
        void BuildDiorama()
        {
            // static clouds in the UI sky (the 3D render has a transparent background)
            if (painted == null) foreach (var (x, y, w) in new[] { (0.12f, 0.93f, 300f), (0.86f, 0.9f, 340f), (0.6f, 0.97f, 260f), (0.3f, 0.82f, 220f), (0.9f, 0.78f, 240f) })
            {
                var c = UI.Image(Root, "Cloud", SpriteFactory.Glow(), new Color(1, 1, 1, 0.9f), false);
                c.rectTransform.anchorMin = c.rectTransform.anchorMax = new Vector2(x, y);
                c.rectTransform.sizeDelta = new Vector2(w, w * 0.5f);
            }

            stage = VoxelStage.Create("Home", 64, false);
            stage.UseTexture(900, 1950); // portrait, covers the whole screen
            stage.Model.gameObject.SetActive(false);
            var scene = new GameObject("Scenery").transform;
            scene.SetParent(stage.transform, false);
            const float v = 0.085f;
            camPos = new Vector3(0, 1.85f, -8.6f); camLook = new Vector3(0, -0.1f, 0); camFov = 40f;
            if (painted == null && Props.AddDiorama(scene, out camPos, out camLook, out camFov))
            {
                // data-driven landscape (tools/diorama.mjs): forest, waterfall cliffs, river, daisies, clouds, pedestal
            }
            else
            {
                if (painted == null) BuildScenery(scene, v);
                Props.AddPedestal(scene, Vector3.zero, 13, v); // purple/gold display pedestal
            }
            BuildStageView();
        }

        void BuildScenery(Transform scene, float v)
        {
            // ground: wide grass island, raised ledges at the sides
            Props.AddIsland(scene, new Vector3(0, -0.36f, 2.5f), 40, 0.13f);
            Props.AddIsland(scene, new Vector3(-3.4f, 0.4f, 4.5f), 12, 0.13f);
            Props.AddIsland(scene, new Vector3(3.6f, 0.6f, 5.5f), 12, 0.13f);
            // background: cliffs with a waterfall (right), trees on both sides
            Props.AddCliffWaterfall(scene, new Vector3(3.1f, 0.2f, 7.5f), 0.2f);
            Props.AddTree(scene, new Vector3(-2.9f, 0.4f, 3.4f), 0.24f, 3);
            Props.AddTree(scene, new Vector3(-4.2f, 0.4f, 6.8f), 0.2f, 5);
            Props.AddTree(scene, new Vector3(-1.9f, 0.4f, 7.6f), 0.16f, 7);
            Props.AddTree(scene, new Vector3(3.0f, 0.6f, 3.8f), 0.2f, 11);
            Props.AddTree(scene, new Vector3(4.6f, 2.3f, 9.5f), 0.18f, 13);
            // flowers and bushes around the pedestal and in the foreground corners
            Props.AddFlower(scene, new Vector3(-1.6f, -0.36f, -0.4f), v * 1.4f, new Color32(190, 120, 255, 255), new Color32(255, 214, 64, 255));
            Props.AddFlower(scene, new Vector3(1.65f, -0.36f, -0.2f), v * 1.4f, new Color32(255, 120, 190, 255), new Color32(255, 214, 64, 255));
            Props.AddFlower(scene, new Vector3(-1.3f, -0.36f, -2.6f), v * 1.8f, new Color32(255, 214, 64, 255), new Color32(255, 150, 40, 255));
            Props.AddFlower(scene, new Vector3(1.35f, -0.36f, -2.7f), v * 1.9f, new Color32(255, 150, 210, 255), new Color32(255, 214, 64, 255));
            Props.AddFlower(scene, new Vector3(-1.9f, 0.4f, 2.6f), v * 1.3f, new Color32(190, 120, 255, 255), new Color32(255, 214, 64, 255));
            Props.AddBush(scene, new Vector3(-1.5f, -0.36f, 1.0f), v * 1.3f);
            Props.AddBush(scene, new Vector3(1.6f, -0.36f, 1.2f), v * 1.3f);
        }

        void BuildStageView()
        {
            hero = CharacterPreview.Create(stage.Pivot);
            hero.Static = true; // home page is static
            // camera: model in the upper middle of the screen (like the reference), looking slightly down
            stage.SetCamera(camPos, camLook, camFov); // pedestal sits just above the carousel strip
            stage.TargetYaw = stage.Yaw = -22f;

            var raw = UI.Raw(Root, "Landscape", stage.Texture);
            raw.rectTransform.Stretch();
            var arf = raw.gameObject.AddComponent<AspectRatioFitter>();
            arf.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            arf.aspectRatio = 900f / 1950f;
            raw.raycastTarget = true;
            var input = raw.gameObject.AddComponent<ModelInput>();
            input.Stage = stage; // drag to rotate 360° (only moves when the player drags)

            // "360°" on the pedestal front and round purple arrows beside the model (anchored to screen fractions,
            // matching the full-screen render)
            var badge = UI.Rect(Root, "Label360");
            badge.anchorMin = badge.anchorMax = new Vector2(0.5f, 0.474f); badge.sizeDelta = new Vector2(420, 120);
            if (UIArt.Has("Icons/label_360"))
            {
                // reference artwork: swoosh arrows around "360°" (sheet → canvas scale 1.39)
                const float s = 1.39f;
                ArtAt(badge, "Icons/rotate_left", new Vector2(-137, 0), 76 * s);
                ArtAt(badge, "Icons/label_360", new Vector2(2, 0), 116 * s);
                ArtAt(badge, "Icons/rotate_right", new Vector2(132, 0), 82 * s);
            }
            else
            {
                var bl = UI.Icon(badge, Icon.ArrowLeft, 60); bl.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(40, 0), new Vector2(60, 60));
                var br = UI.Icon(badge, Icon.ArrowRight, 60); br.rectTransform.Place(new Vector2(1, 0.5f), new Vector2(-40, 0), new Vector2(60, 60));
                var t360 = UI.Text(badge, "360°", 66, Color.white, TextAnchor.MiddleCenter, 0.3f, Theme.PurpleDark);
                t360.rectTransform.Stretch();
            }

            var left = UI.IconButton(Root, Icon.ArrowLeft, 118, ButtonStyle.Purple);
            var lr = (RectTransform)left.transform; lr.anchorMin = lr.anchorMax = new Vector2(0.17f, 0.568f); lr.sizeDelta = new Vector2(118, 118);
            UIArt.Skin(left, "Buttons/back");
            left.OnClick = () => Select(catalog.Step(selected, -1));
            var right = UI.IconButton(Root, Icon.ArrowRight, 118, ButtonStyle.Purple);
            var rr = (RectTransform)right.transform; rr.anchorMin = rr.anchorMax = new Vector2(0.83f, 0.568f); rr.sizeDelta = new Vector2(118, 118);
            if (UIArt.Skin(right, "Buttons/back") != null)
            {
                right.Face.localScale = new Vector3(-1, 1, 1); // same artwork, mirrored
                var ic = right.Face.Find("Icon_" + Icon.ArrowRight);
                if (ic != null) ic.gameObject.SetActive(false);
            }
            var icl = left.Face.Find("Icon_" + Icon.ArrowLeft);
            if (icl != null && UIArt.Has("Buttons/back")) icl.gameObject.SetActive(false);
            right.OnClick = () => Select(catalog.Step(selected, +1));
        }

        // ------------------------------------------------------------------ logo
        void BuildLogo()
        {
            if (UIArt.Has("Logo/logo"))
            {
                // the exact reference logo (crown, leaves, cubes, ribbon "Puzzle cu Obiecte 3D"), mockup ×1.148
                var logo = UIArt.Picture(Root, "Logo/logo", 790);
                logo.rectTransform.anchorMin = logo.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                logo.rectTransform.anchoredPosition = new Vector2(-6, -312);
                return;
            }
            var root = UI.Rect(Root, "Logo").Place(new Vector2(0.5f, 1), new Vector2(0, -335), new Vector2(920, 320));
            AddIcon(root, Icon.Leaf, new Vector2(-370, 40), 150, 200);
            AddIcon(root, Icon.Leaf, new Vector2(370, 70), 150, -20);
            AddIcon(root, Icon.Leaf, new Vector2(400, -20), 110, 20);
            Word(root, "Voxel", 170, new Vector2(30, 80), Color.white, Theme.Lavender, Theme.PurpleDark);
            Word(root, "Bloom", 170, new Vector2(-60, -50), Color.white, Theme.Lavender, Theme.PurpleDark);
            var three = Word(root, "3D", 150, new Vector2(300, -60), Theme.Lighter(Theme.Gold, 0.55f), Theme.GoldDark, Theme.Darker(Theme.Orange, 0.8f));
            three.localRotation = Quaternion.Euler(0, 0, 5);
            AddIcon(root, Icon.Crown, new Vector2(-235, 108), 118, -14);
            AddIcon(root, Icon.CubeBlue, new Vector2(-400, -90), 96, -10);
            AddIcon(root, Icon.CubePink, new Vector2(400, 110), 96, 14);
            var rib = UI.Ribbon(Root, "Puzzle cu Obiecte 3D", 680, 120);
            rib.Place(new Vector2(0.5f, 1), new Vector2(0, -540), rib.sizeDelta);
            rib.localRotation = Quaternion.Euler(0, 0, -2);
        }

        RectTransform Word(Transform parent, string text, float size, Vector2 pos, Color top, Color bottom, Color edge)
        {
            var holder = UI.Rect(parent, "Word_" + text).Place(new Vector2(0.5f, 0.5f), pos, new Vector2(800, size + 20));
            for (int d = 7; d >= 1; d--)
            {
                var ext = UI.Text(holder, text, size, Theme.Darker(edge, 0.85f), TextAnchor.MiddleCenter, 0.55f, Theme.Darker(edge, 0.7f));
                ext.fontStyle = FontStyle.Bold;
                ext.rectTransform.Stretch(0, 0, d * 3, -d * 3);
            }
            var face = UI.Text(holder, text, size, Color.white, TextAnchor.MiddleCenter, 0.55f, edge);
            face.fontStyle = FontStyle.Bold;
            face.rectTransform.Stretch();
            face.SetGradient(top, bottom);
            return holder;
        }

        static Image ArtAt(Transform parent, string path, Vector2 pos, float width)
        {
            var img = UIArt.Picture(parent, path, width);
            img.rectTransform.anchorMin = img.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            img.rectTransform.anchoredPosition = pos;
            return img;
        }

        static RectTransform AddIcon(Transform parent, Icon icon, Vector2 pos, float size, float rot)
        {
            var img = UI.Icon(parent, icon, size);
            img.rectTransform.Place(new Vector2(0.5f, 0.5f), pos, new Vector2(size, size));
            img.rectTransform.localRotation = Quaternion.Euler(0, 0, rot);
            return img.rectTransform;
        }

        // ------------------------------------------------------------------ carousel strip
        void BuildCarousel()
        {
            var strip = UI.Panel(Root, "CarouselStrip", new Color(0.93f, 0.95f, 1f, 0.55f), 44, true, 6f);
            var row = (RectTransform)strip.transform.parent;
            row.BottomBand(700, 176, 24);
            for (int i = 0; i < CharacterCatalog.PerPage; i++)
            {
                var t = new CarouselTile();
                var b = UI.Button(row, null, ButtonStyle.White, new Vector2(150, 140));
                t.Root = (RectTransform)b.transform;
                t.Root.Place(new Vector2((i + 0.5f) / CharacterCatalog.PerPage, 0.5f), new Vector2(0, 4), new Vector2(150, 140));
                var glow = UI.Image(t.Root, "Selected", SpriteFactory.Rounded(44), Theme.Gold);
                glow.rectTransform.Stretch(-9, -9, -9, -13);
                glow.transform.SetAsFirstSibling();
                t.Glow = glow;
                var bg = UI.Image(b.Face, "Bg", SpriteFactory.Rounded(34), Theme.Hex("#E7E4FF"));
                bg.rectTransform.Stretch(7, 7, 7, 7);
                t.Frame = bg;
                t.Thumb = UI.Raw(bg.transform, "Thumb", null);
                t.Thumb.rectTransform.Stretch(2, 2, 2, 2);
                t.Lock = UI.Icon(b.Face, Icon.Lock, 54);
                t.Lock.rectTransform.Place(new Vector2(1, 0), new Vector2(-24, 26), new Vector2(54, 54));
                int slot = i;
                b.Sound = Sfx.Select;
                b.OnClick = () => { if (tiles[slot].Index >= 0) Select(tiles[slot].Index); };
                tiles.Add(t);
            }
            if (UIArt.Has("HUD/caption_pill_blank"))
            {
                var pill = UIArt.Picture(Root, "HUD/caption_pill_blank", 360);
                pill.rectTransform.anchorMin = pill.rectTransform.anchorMax = new Vector2(0.5f, 0f);
                pill.rectTransform.anchoredPosition = new Vector2(0, 672);
                caption = UI.Text(pill.transform, "", 34, Theme.Hex("#2A1466"), TextAnchor.MiddleCenter, 0.12f, Color.white);
                caption.resizeTextForBestFit = true; caption.resizeTextMinSize = 16; caption.resizeTextMaxSize = 34;
                caption.rectTransform.Stretch(18, 18, 6, 10);
            }
            else
            {
                caption = UI.Text(Root, "", 42, Theme.Navy, TextAnchor.MiddleCenter, 0.2f, Color.white);
                caption.rectTransform.BottomBand(645, 54, 0);
            }
            foreach (var x in new[] { -250f, 250f })
            {
                var sp = UI.Icon(Root, Icon.Sparkle, 40); sp.color = Theme.Gold;
                sp.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(x, 672), new Vector2(40, 40));
            }
        }

        void Select(int index)
        {
            selected = Mathf.Clamp(index, 0, catalog.Count - 1);
            var e = catalog.Characters[selected];
            bool unlocked = catalog.IsUnlocked(e.Id, Services.Save);
            var grid = Services.Data.GetModel(e.Id);
            if (grid != null)
            {
                float vs = 1.6f / Mathf.Max(grid.X, Mathf.Max(grid.Y, grid.Z));
                hero.Show(grid, Services.Data.GetAnim(e.Id), vs, !unlocked, selected);
                stage.SetShadowSize(Mathf.Max(grid.X, grid.Z) * vs * 1.2f);
            }
            if (unlocked && Services.Save.HomeCharacter != e.Id) { Services.Save.HomeCharacter = e.Id; Services.Commit(); }
            int page = CharacterCatalog.PageOf(selected);
            caption.text = (selected + 1) + ". " + e.Name + "  ·  " + (page + 1) + " / " + catalog.PageCount;
            if (!unlocked) caption.text = (selected + 1) + ". " + e.Name + "  ·  " + (e.UnlockLevel > 0 ? "se deblochează la nivelul " + e.UnlockLevel : "în curând");
            RefreshCarousel(page);
        }

        void RefreshCarousel(int page)
        {
            for (int i = 0; i < tiles.Count; i++)
            {
                int idx = page * CharacterCatalog.PerPage + i;
                var t = tiles[i];
                bool exists = idx < catalog.Count;
                t.Root.gameObject.SetActive(exists);
                if (!exists) { t.Index = -1; continue; }
                t.Index = idx;
                var e = catalog.Characters[idx];
                bool unlocked = catalog.IsUnlocked(e.Id, Services.Save);
                t.Thumb.texture = ThumbnailRenderer.Get(e.Id);
                t.Thumb.color = unlocked ? Color.white : new Color(0.42f, 0.47f, 0.6f, 0.95f); // grey silhouette
                t.Frame.color = unlocked ? Theme.Hex("#FFF4DE") : Theme.Hex("#E7E4FF");
                t.Lock.gameObject.SetActive(!unlocked);
                bool sel = idx == selected;
                t.Glow.gameObject.SetActive(sel);
                t.Root.localScale = Vector3.one * (sel ? 1.06f : 1f);
            }
        }

        // ------------------------------------------------------------------ play / level / nav
        void BuildPlay()
        {
            var glow = UI.Image(Root, "PlayGlow", SpriteFactory.Glow(), new Color(1f, 0.95f, 0.45f, 0.5f), false);
            glow.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0, 540), new Vector2(880, 320));
            bool art = UIArt.Has("Buttons/play");
            var play = art ? UI.Button(Root, null, ButtonStyle.Green, new Vector2(680, 244))
                           : UI.Button(Root, "Joacă", ButtonStyle.Green, new Vector2(700, 176), Icon.Play, 110);
            ((RectTransform)play.transform).Place(new Vector2(0.5f, 0), new Vector2(0, art ? 540 : 508), art ? new Vector2(680, 244) : new Vector2(700, 176));
            if (art) UIArt.Skin(play, "Buttons/play"); // the reference button (label "Joacă" is part of the artwork)
            play.OnClick = () => GameplayController.StartLevel(Services.Progress.LatestPlayableLevel());
            play.Sound = Sfx.Whoosh;

            if (UIArt.Has("Buttons/level_badge_blank"))
            {
                var lv = UI.Button(Root, null, ButtonStyle.Purple, new Vector2(460, 162));
                ((RectTransform)lv.transform).Place(new Vector2(0.5f, 0), new Vector2(0, 407), new Vector2(460, 162));
                UIArt.Skin(lv, "Buttons/level_badge_blank"); // laurels + star from the artwork, live level number
                levelLabel = UI.Text(lv.Face, "Nivel 1", 64, Color.white, TextAnchor.MiddleCenter, 0.2f, Theme.Hex("#2A0C66"));
                levelLabel.fontStyle = FontStyle.Bold;
                levelLabel.resizeTextForBestFit = true; levelLabel.resizeTextMinSize = 24; levelLabel.resizeTextMaxSize = 64;
                var lr = levelLabel.rectTransform;
                lr.anchorMin = new Vector2(0.35f, 0.22f); lr.anchorMax = new Vector2(0.84f, 0.8f);
                lr.offsetMin = lr.offsetMax = Vector2.zero;
                lv.OnClick = () => Router.Show<LevelSelectScreen>();
                return;
            }
            var lvb = UI.Button(Root, "Nivel 1", ButtonStyle.Purple, new Vector2(380, 92), Icon.Star, 50);
            ((RectTransform)lvb.transform).Place(new Vector2(0.5f, 0), new Vector2(0, 372), new Vector2(380, 92));
            lvb.OnClick = () => Router.Show<LevelSelectScreen>();
            levelLabel = lvb.Face.Find("Label").GetComponent<VText>();
            // golden laurels
            var l1 = UI.Icon(Root, Icon.Leaf, 84); l1.color = Theme.Gold; l1.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(-232, 372), new Vector2(84, 84)); l1.rectTransform.localRotation = Quaternion.Euler(0, 0, 230);
            var l2 = UI.Icon(Root, Icon.Leaf, 84); l2.color = Theme.Gold; l2.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(232, 372), new Vector2(84, 84)); l2.rectTransform.localRotation = Quaternion.Euler(0, 0, -50);
        }

        void BuildNav()
        {
            if (UIArt.Has("Buttons/nav_collection"))
            {
                // reference nav buttons (labels are part of the artwork); mockup centres ×1.148
                ArtNav("Buttons/nav_collection", -393, () => Router.Show<CollectionScreen>());
                ArtNav("Buttons/nav_rewards", -131, () => Router.Open<RewardsDialog>());
                ArtNav("Buttons/nav_shop", 131, () => Router.Show<ShopScreen>());
                ArtNav("Buttons/nav_events", 393, () => Router.Show<EventsScreen>());
                rewardsBadge = null; // the "!" badge is painted into the artwork
                return;
            }
            var bar = UI.Rect(Root, "Nav").BottomBand(60, 230, 28);
            var hl = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 20; hl.childForceExpandWidth = true; hl.childControlWidth = true; hl.childControlHeight = true;
            AddNav(bar, Icon.PawGold, "Colecție", () => Router.Show<CollectionScreen>());
            var rewards = AddNav(bar, Icon.Gift, "Recompense", () => Router.Open<RewardsDialog>());
            AddNav(bar, Icon.Shop, "Magazin", () => Router.Show<ShopScreen>());
            AddNav(bar, Icon.CalendarStar, "Evenimente", () => Router.Show<EventsScreen>());
            var badge = UI.Image(rewards, "Badge", SpriteFactory.Circle(), Theme.Red, false);
            badge.rectTransform.Place(new Vector2(1, 1), new Vector2(-16, -10), new Vector2(64, 64));
            UI.Text(badge.transform, "!", 46, Color.white, TextAnchor.MiddleCenter, 0.2f, Theme.Darker(Theme.Red, 0.6f)).rectTransform.Stretch();
            rewardsBadge = badge.gameObject;
        }

        void ArtNav(string path, float x, System.Action onClick)
        {
            var sp = UIArt.Get(path);
            float w = 240f * sp.rect.width / 180f, h = w * sp.rect.height / sp.rect.width;
            var b = UI.Button(Root, null, ButtonStyle.Purple, new Vector2(w, h));
            var r = (RectTransform)b.transform;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0f);
            r.pivot = new Vector2(0.5f, 0f);
            r.anchoredPosition = new Vector2(x, 88);
            r.sizeDelta = new Vector2(w, h);
            UIArt.Skin(b, path);
            b.OnClick = onClick;
        }

        RectTransform AddNav(Transform parent, Icon icon, string label, System.Action onClick)
        {
            var b = UI.Button(parent, null, ButtonStyle.Purple, new Vector2(240, 230));
            var face = b.Face;
            UI.Icon(face, icon, 120).rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0, 26), new Vector2(120, 120));
            var t = UI.Text(face, label, 34, Color.white, TextAnchor.MiddleCenter, 0.2f, Theme.PurpleDark);
            t.fontStyle = FontStyle.Bold;
            t.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0, 36), new Vector2(236, 48));
            b.OnClick = onClick;
            return (RectTransform)b.transform;
        }

        public override void OnShow()
        {
            stage.SetActive(true);
            levelLabel.text = "Nivel " + Services.Progress.LatestPlayableLevel();
            if (rewardsBadge != null) rewardsBadge.SetActive(Services.Daily.CanClaim(Services.Now));
            App.State.ForceReset(GameState.Home);
            Select(catalog.IndexOf(catalog.ResolveSaved(Services.Save.HomeCharacter, Services.Save)));
        }

        public override void OnHide() { stage.SetActive(false); }

        public override void OnBack() { Application.Quit(); }
    }
}
