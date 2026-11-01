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
            UI.Sky(Root, Theme.Hex("#5DB8FF"), Theme.Hex("#BDE6FF"));
            BuildDiorama();
            var top = TopBar.Create(Root);
            top.OnSettings = () => Router.Open<SettingsDialog>();
            top.OnAddCoins = () => Router.Show<ShopScreen>();
            top.OnAvatar = () => Router.Open<ProfileDialog>();
            BuildLogo();
            BuildCarousel();
            BuildPlay();
            BuildNav();
        }

        // ------------------------------------------------------------------ 3D landscape + pedestal (static)
        void BuildDiorama()
        {
            stage = VoxelStage.Create("Home", 1024, false);
            stage.Model.gameObject.SetActive(false);
            var scene = new GameObject("Scenery").transform;
            scene.SetParent(stage.transform, false);
            const float v = 0.085f;
            Props.AddCliffWaterfall(scene, new Vector3(2.7f, -1.3f, 3.8f), 0.14f);
            Props.AddTree(scene, new Vector3(-3.1f, -1.0f, 2.4f), 0.16f, 3);
            Props.AddTree(scene, new Vector3(-2.0f, -0.8f, 4.4f), 0.12f, 7);
            Props.AddTree(scene, new Vector3(3.5f, -0.9f, 1.2f), 0.13f, 11);
            Props.AddIsland(scene, new Vector3(0, -0.36f, 0), 21, v);
            Props.AddPedestal(scene, Vector3.zero, 13, v);
            Props.AddFlower(scene, new Vector3(-1.55f, -0.36f, -0.9f), v * 1.3f, new Color32(190, 120, 255, 255), new Color32(255, 214, 64, 255));
            Props.AddFlower(scene, new Vector3(1.6f, -0.36f, -0.7f), v * 1.3f, new Color32(255, 120, 190, 255), new Color32(255, 214, 64, 255));
            Props.AddFlower(scene, new Vector3(-1.75f, -0.36f, 0.5f), v, new Color32(255, 214, 64, 255), new Color32(255, 150, 40, 255));
            Props.AddBush(scene, new Vector3(1.2f, -0.36f, 1.3f), v);

            hero = CharacterPreview.Create(stage.Pivot);
            hero.Static = true; // home page is static
            stage.FrameScene(3.9f, 3.0f, 0.95f);
            stage.TargetYaw = stage.Yaw = -22f;

            var raw = UI.Raw(Root, "Diorama", stage.Texture);
            raw.rectTransform.anchorMin = Vector2.zero; raw.rectTransform.anchorMax = Vector2.one;
            raw.rectTransform.offsetMin = new Vector2(0, 760); raw.rectTransform.offsetMax = new Vector2(0, -300);
            var arf = raw.gameObject.AddComponent<AspectRatioFitter>();
            arf.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            arf.aspectRatio = 1f;
            raw.raycastTarget = true;
            var input = raw.gameObject.AddComponent<ModelInput>();
            input.Stage = stage; // drag to rotate 360° (only moves when the player drags)

            // "360°" label on the pedestal front
            var badge = UI.Rect(Root, "Label360").Place(new Vector2(0.5f, 0), new Vector2(0, 870), new Vector2(380, 80));
            var bl = UI.Icon(badge, Icon.ArrowLeft, 56); bl.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(40, 0), new Vector2(56, 56));
            var br = UI.Icon(badge, Icon.ArrowRight, 56); br.rectTransform.Place(new Vector2(1, 0.5f), new Vector2(-40, 0), new Vector2(56, 56));
            var t360 = UI.Text(badge, "360°", 62, Color.white, TextAnchor.MiddleCenter, 0.25f, Theme.PurpleDark);
            t360.rectTransform.Stretch();

            // round purple arrows beside the model
            var left = UI.IconButton(Root, Icon.ArrowLeft, 118, ButtonStyle.Purple);
            ((RectTransform)left.transform).Place(new Vector2(0, 0), new Vector2(170, 1090), new Vector2(118, 118));
            left.OnClick = () => Select(catalog.Step(selected, -1));
            var right = UI.IconButton(Root, Icon.ArrowRight, 118, ButtonStyle.Purple);
            ((RectTransform)right.transform).Place(new Vector2(1, 0), new Vector2(-170, 1090), new Vector2(118, 118));
            right.OnClick = () => Select(catalog.Step(selected, +1));
        }

        // ------------------------------------------------------------------ logo
        void BuildLogo()
        {
            var root = UI.Rect(Root, "Logo").Place(new Vector2(0.5f, 1), new Vector2(0, -300), new Vector2(920, 320));
            AddIcon(root, Icon.Leaf, new Vector2(-370, 40), 150, 200);
            AddIcon(root, Icon.Leaf, new Vector2(370, 70), 150, -20);
            AddIcon(root, Icon.Leaf, new Vector2(400, -20), 110, 20);
            Word(root, "Voxel", 170, new Vector2(30, 80), Color.white, Theme.Lavender, Theme.PurpleDark);
            Word(root, "Bloom", 170, new Vector2(-60, -50), Color.white, Theme.Lavender, Theme.PurpleDark);
            var three = Word(root, "3D", 150, new Vector2(300, -60), Theme.Lighter(Theme.Gold, 0.55f), Theme.GoldDark, Theme.Darker(Theme.Orange, 0.8f));
            three.localRotation = Quaternion.Euler(0, 0, 5);
            AddIcon(root, Icon.Crown, new Vector2(-250, 150), 150, -14);
            AddIcon(root, Icon.CubeBlue, new Vector2(-400, -90), 96, -10);
            AddIcon(root, Icon.CubePink, new Vector2(400, 110), 96, 14);
            var rib = UI.Ribbon(Root, "Puzzle cu Obiecte 3D", 680, 120);
            rib.Place(new Vector2(0.5f, 1), new Vector2(0, -510), rib.sizeDelta);
            rib.localRotation = Quaternion.Euler(0, 0, -2);
        }

        RectTransform Word(Transform parent, string text, float size, Vector2 pos, Color top, Color bottom, Color edge)
        {
            var holder = UI.Rect(parent, "Word_" + text).Place(new Vector2(0.5f, 0.5f), pos, new Vector2(800, size + 20));
            for (int d = 5; d >= 1; d--)
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
            var strip = UI.Panel(Root, "CarouselStrip", new Color(1, 1, 1, 0.45f), 44, true, 6f);
            var row = (RectTransform)strip.transform.parent;
            row.BottomBand(650, 176, 24);
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
            caption = UI.Text(Root, "", 40, Theme.Ink, TextAnchor.MiddleCenter, 0.15f, Color.white);
            caption.rectTransform.BottomBand(600, 54, 0);
        }

        void Select(int index)
        {
            selected = Mathf.Clamp(index, 0, catalog.Count - 1);
            var e = catalog.Characters[selected];
            bool unlocked = catalog.IsUnlocked(e.Id, Services.Save);
            var grid = Services.Data.GetModel(e.Id);
            if (grid != null)
            {
                float vs = 2.0f / Mathf.Max(grid.X, Mathf.Max(grid.Y, grid.Z));
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
            var play = UI.Button(Root, "Joacă", ButtonStyle.Green, new Vector2(700, 176), Icon.Play, 110);
            ((RectTransform)play.transform).Place(new Vector2(0.5f, 0), new Vector2(0, 508), new Vector2(700, 176));
            play.OnClick = () => GameplayController.StartLevel(Services.Progress.LatestPlayableLevel());
            play.Sound = Sfx.Whoosh;

            var lv = UI.Button(Root, "Nivel 1", ButtonStyle.Purple, new Vector2(380, 92), Icon.Star, 50);
            ((RectTransform)lv.transform).Place(new Vector2(0.5f, 0), new Vector2(0, 372), new Vector2(380, 92));
            lv.OnClick = () => Router.Show<LevelSelectScreen>();
            levelLabel = lv.Face.Find("Label").GetComponent<VText>();
            // golden laurels
            var l1 = UI.Icon(Root, Icon.Leaf, 84); l1.color = Theme.Gold; l1.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(-232, 372), new Vector2(84, 84)); l1.rectTransform.localRotation = Quaternion.Euler(0, 0, 230);
            var l2 = UI.Icon(Root, Icon.Leaf, 84); l2.color = Theme.Gold; l2.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(232, 372), new Vector2(84, 84)); l2.rectTransform.localRotation = Quaternion.Euler(0, 0, -50);
        }

        void BuildNav()
        {
            var bar = UI.Rect(Root, "Nav").BottomBand(60, 230, 28);
            var hl = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 20; hl.childForceExpandWidth = true; hl.childControlWidth = true; hl.childControlHeight = true;
            AddNav(bar, Icon.Paw, "Colecție", () => Router.Show<CollectionScreen>());
            var rewards = AddNav(bar, Icon.Gift, "Recompense", () => Router.Open<RewardsDialog>());
            AddNav(bar, Icon.Shop, "Magazin", () => Router.Show<ShopScreen>());
            AddNav(bar, Icon.Calendar, "Evenimente", () => Router.Show<EventsScreen>());
            var badge = UI.Image(rewards, "Badge", SpriteFactory.Circle(), Theme.Red, false);
            badge.rectTransform.Place(new Vector2(1, 1), new Vector2(-16, -10), new Vector2(64, 64));
            UI.Text(badge.transform, "!", 46, Color.white, TextAnchor.MiddleCenter, 0.2f, Theme.Darker(Theme.Red, 0.6f)).rectTransform.Stretch();
            rewardsBadge = badge.gameObject;
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
            rewardsBadge.SetActive(Services.Daily.CanClaim(Services.Now));
            App.State.ForceReset(GameState.Home);
            Select(catalog.IndexOf(catalog.ResolveSaved(Services.Save.HomeCharacter, Services.Save)));
        }

        public override void OnHide() { stage.SetActive(false); }

        public override void OnBack() { Application.Quit(); }
    }
}
