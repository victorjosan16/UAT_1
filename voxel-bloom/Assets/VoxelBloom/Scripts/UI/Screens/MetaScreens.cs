using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VoxelBloom.Core;

namespace VoxelBloom
{
    /// <summary>COLLECTION: category tabs, 3-column model grid, lock/check states, progress bar.</summary>
    public sealed class CollectionScreen : ScreenBase
    {
        static readonly string[] Categories = { "Animals", "Food", "Vehicles", "Buildings" };
        string category = "Animals";
        RectTransform grid;
        readonly List<UIButton> tabs = new List<UIButton>();
        TextMeshProUGUI progressText;
        ProgressBar progress;

        protected override void OnBuild()
        {
            UI.Sky(Root, Theme.SkyTop, Theme.SkyBottom);
            var top = TopBar.Create(Root);
            top.OnSettings = () => Router.Open<SettingsDialog>();
            top.OnAddCoins = () => Router.Show<ShopScreen>();
            top.OnAvatar = () => Router.Open<ProfileDialog>();
            var rib = UI.Ribbon(Root, "Collection", 700, 150);
            rib.Place(new Vector2(0.5f, 1), new Vector2(0, -230), rib.sizeDelta);
            var back = UI.IconButton(Root, Icon.ArrowLeft, 110, ButtonStyle.White);
            ((RectTransform)back.transform).Place(new Vector2(0, 1), new Vector2(90, -230), new Vector2(110, 110));
            back.OnClick = OnBack;

            var tabRow = UI.Rect(Root, "Tabs").TopBand(330, 100, 40);
            for (int i = 0; i < Categories.Length; i++)
            {
                string c = Categories[i];
                var b = UI.Button(tabRow, c, ButtonStyle.White, new Vector2(232, 90), null, 38);
                ((RectTransform)b.transform).Place(new Vector2((i + 0.5f) / Categories.Length, 0.5f), Vector2.zero, new Vector2(232, 90));
                b.OnClick = () => { category = c; Rebuild(); };
                tabs.Add(b);
            }

            var panel = UI.Panel(Root, "GridPanel", new Color(1, 1, 1, 0.9f), 48);
            var pr = (RectTransform)panel.transform.parent;
            pr.Stretch(40, 40, 460, 230);
            var scroll = UI.VerticalScroll(pr, out var content, 0, new RectOffset(30, 30, 30, 30));
            ((RectTransform)scroll.transform).Stretch();
            grid = UI.Rect(content, "Grid");
            var gl = grid.gameObject.AddComponent<GridLayoutGroup>();
            gl.cellSize = new Vector2(280, 320);
            gl.spacing = new Vector2(24, 24);
            gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gl.constraintCount = 3;
            gl.childAlignment = TextAnchor.UpperCenter;

            var foot = UI.Panel(Root, "Footer", Color.white, 40);
            var fr = (RectTransform)foot.transform.parent;
            fr.BottomBand(40, 160, 40);
            progressText = UI.Text(fr, "", 42, Theme.Ink);
            progressText.rectTransform.TopBand(18, 56, 20);
            progress = UI.Progress(fr, Theme.Green, 40);
            ((RectTransform)progress.transform).BottomBand(28, 40, 60);
        }

        public override void OnShow() => Rebuild();

        void Rebuild()
        {
            for (int i = 0; i < tabs.Count; i++)
            {
                UI.StyleColors(Categories[i] == category ? ButtonStyle.Green : ButtonStyle.White, out var face, out var lip, out var tc);
                tabs[i].Face.GetComponent<Image>().color = face;
                tabs[i].transform.Find("Lip").GetComponent<Image>().color = lip;
                tabs[i].Face.Find("Label").GetComponent<TextMeshProUGUI>().color = tc;
            }
            for (int i = grid.childCount - 1; i >= 0; i--) Destroy(grid.GetChild(i).gameObject);
            int n = 0;
            foreach (var e in Services.Config.Collection)
            {
                if (e.Category != category) continue;
                n++;
                Tile(e);
            }
            UI.Layout(grid, Mathf.Ceil(n / 3f) * 344f);
            int got = Services.Progress.CollectedIn(category, out int total);
            progressText.text = category + " collected: " + got + "/" + total;
            progress.Set(total == 0 ? 0 : (float)got / total);
        }

        void Tile(CollectionEntry e)
        {
            bool unlocked = Services.Progress.IsModelUnlocked(e.Id);
            var b = UI.Button(grid, null, ButtonStyle.White, new Vector2(280, 320));
            var face = b.Face;
            var bg = UI.Image(face, "Bg", SpriteFactory.Rounded(32), unlocked ? Theme.Hex("#E3F4FF") : Theme.Hex("#ECEAF4"));
            bg.rectTransform.Stretch(14, 14, 14, 70);
            var raw = UI.Raw(bg.transform, "Thumb", ThumbnailRenderer.Get(e.Id));
            raw.rectTransform.Stretch(6, 6, 6, 6);
            raw.color = unlocked ? Color.white : new Color(0.25f, 0.22f, 0.35f, 0.55f);
            UI.Text(face, unlocked ? e.Name : "???", 32, Theme.Ink).rectTransform.BottomBand(14, 50, 8);
            var badge = UI.Icon(face, unlocked ? Icon.Check : Icon.Lock, 72);
            badge.rectTransform.Place(new Vector2(1, 0), new Vector2(-30, 90), new Vector2(72, 72));
            b.OnClick = () =>
            {
                if (unlocked) Router.Open<ModelPreviewDialog>(d => d.Entry = e);
                else Router.Toast(e.UnlockLevel > 0 ? "Complete level " + e.UnlockLevel + " to unlock" : "Coming soon", Icon.Lock);
            };
        }

        public override void OnBack() => Router.Show<HomeScreen>();
    }

    /// <summary>Rotatable / zoomable 3D preview of an unlocked model.</summary>
    public sealed class ModelPreviewDialog : DialogBase
    {
        [System.NonSerialized] public CollectionEntry Entry;
        static VoxelStage stage;
        protected override string Title => Entry != null ? Entry.Name : "Model";
        protected override Vector2 Size => new Vector2(940, 1240);

        protected override void OnBuild()
        {
            if (stage == null) stage = VoxelStage.Create("Preview", 1024, true);
            stage.SetActive(true);
            var g = Services.Data.GetModel(Entry.Id);
            if (g != null) stage.ShowModel(g, 2f);
            stage.Pivot.localScale = Vector3.one;
            stage.AutoRotateSpeed = 20f;
            var bg = UI.Image(Body, "Bg", SpriteFactory.VerticalGradient(Theme.Hex("#7CC8FF"), Theme.Hex("#E2F4FF")), Color.white, false);
            bg.rectTransform.TopBand(60, 820, 0);
            var raw = UI.Raw(bg.transform, "Model", stage.Texture);
            raw.rectTransform.Stretch();
            raw.raycastTarget = true;
            var arf = raw.gameObject.AddComponent<AspectRatioFitter>();
            arf.aspectMode = AspectRatioFitter.AspectMode.FitInParent; arf.aspectRatio = 1;
            var input = raw.gameObject.AddComponent<ModelInput>();
            input.Stage = stage; input.AllowZoom = true;
            int vox = g != null ? g.CountSolid() : 0;
            var info = UI.Text(Body, Entry.Category + "  ·  " + vox + " voxels\nDrag to rotate · pinch or scroll to zoom", 36, Theme.InkSoft);
            info.rectTransform.BottomBand(30, 120, 0);
        }

        void OnDestroy() { if (stage != null) { stage.AutoRotateSpeed = 0; stage.SetActive(false); } }
    }

    /// <summary>SHOP: buy boosters with coins. Real-money packs are intentionally absent (no store SDK configured).</summary>
    public sealed class ShopScreen : ScreenBase
    {
        protected override void OnBuild()
        {
            UI.Sky(Root, Theme.SkyTop, Theme.SkyBottom);
            var top = TopBar.Create(Root);
            top.OnSettings = () => Router.Open<SettingsDialog>();
            top.OnAvatar = () => Router.Open<ProfileDialog>();
            top.OnAddCoins = () => Router.Toast("Earn coins by completing levels - HARD levels pay x2.5!", Icon.Coin);
            var rib = UI.Ribbon(Root, "Shop", 600, 150);
            rib.Place(new Vector2(0.5f, 1), new Vector2(0, -230), rib.sizeDelta);
            var back = UI.IconButton(Root, Icon.ArrowLeft, 110, ButtonStyle.White);
            ((RectTransform)back.transform).Place(new Vector2(0, 1), new Vector2(90, -230), new Vector2(110, 110));
            back.OnClick = OnBack;

            var open = UI.Button(Root, "Boosters", ButtonStyle.Purple, new Vector2(700, 170), Icon.Hammer, 64);
            ((RectTransform)open.transform).Place(new Vector2(0.5f, 1), new Vector2(0, -470), new Vector2(700, 170));
            open.OnClick = () => Router.Open<BoostersDialog>();

            bool ads = Services.Ads.IsReady(AdPlacement.ExtraBooster);
            var watch = UI.Button(Root, ads ? "Free booster (video)" : "Free booster (video unavailable)", ButtonStyle.Blue, new Vector2(700, 140), Icon.Video, ads ? 46 : 36);
            ((RectTransform)watch.transform).Place(new Vector2(0.5f, 1), new Vector2(0, -680), new Vector2(700, 140));
            watch.SetInteractable(ads);
            watch.OnClick = () => Services.Ads.Show(AdPlacement.ExtraBooster, o =>
            {
                if (RewardGate.MayGrant(Services.Ads, o)) { Services.Boosters.Add(BoosterType.Shuffle, 1); Services.Commit(); Router.Toast("+1 Shuffle", Icon.Shuffle); }
                else Router.Toast(Services.Ads.IsTestProvider ? "Test ad provider: no reward granted" : "No reward", Icon.Video);
            });

            var tip = UI.Panel(Root, "Tip", Color.white, 40);
            var tr = (RectTransform)tip.transform.parent;
            tr.Place(new Vector2(0.5f, 1), new Vector2(0, -980), new Vector2(900, 300));
            UI.Icon(tr, Icon.Lightning, 150).rectTransform.Place(new Vector2(0, 0.5f), new Vector2(110, 0), new Vector2(150, 150));
            var t = UI.Text(tr, "Every 5th level is a HARD level.\nWin it for x2.5 coins and spend them\non Eliminare to clear a jammed queue!", 38, Theme.Ink, TextAlignmentOptions.Left);
            t.textWrappingMode = TextWrappingModes.Normal;
            t.rectTransform.Stretch(210, 30, 20, 20);
        }

        public override void OnBack() => Router.Show<HomeScreen>();
    }

    /// <summary>EVENTS: the HARD challenge track (every 5th level, bigger rewards).</summary>
    public sealed class EventsScreen : ScreenBase
    {
        RectTransform content;

        protected override void OnBuild()
        {
            UI.Sky(Root, Theme.SkyTop, Theme.SkyBottom);
            var rib = UI.Ribbon(Root, "Hard Challenges", 760, 150);
            rib.Place(new Vector2(0.5f, 1), new Vector2(40, -40), rib.sizeDelta);
            var back = UI.IconButton(Root, Icon.ArrowLeft, 110, ButtonStyle.White);
            ((RectTransform)back.transform).Place(new Vector2(0, 1), new Vector2(90, -90), new Vector2(110, 110));
            back.OnClick = OnBack;
            var scroll = UI.VerticalScroll(Root, out content, 22, new RectOffset(50, 50, 20, 60));
            ((RectTransform)scroll.transform).Stretch(0, 0, 220, 0);
        }

        public override void OnShow()
        {
            for (int i = content.childCount - 1; i >= 0; i--) Destroy(content.GetChild(i).gameObject);
            int every = Mathf.Max(1, Services.Config.HardEvery);
            for (int l = every; l <= Services.Config.TotalLevels; l += every)
            {
                var def = Services.Data.GetLevel(l);
                if (def == null) continue;
                bool unlocked = Services.Progress.IsLevelUnlocked(l);
                int stars = Services.Progress.StarsFor(l);
                var card = UI.Panel(content, "Hard" + l, unlocked ? Color.white : Theme.PanelSoft, 40);
                var rt = (RectTransform)card.transform.parent;
                UI.Layout(rt, 190);
                UI.Icon(rt, Icon.Lightning, 110).rectTransform.Place(new Vector2(0, 0.5f), new Vector2(90, 0), new Vector2(110, 110));
                UI.Text(rt, "Level " + l + " · " + def.Name, 44, Theme.Ink, TextAlignmentOptions.Left).rectTransform.Place(new Vector2(0, 1), new Vector2(170, -60), new Vector2(560, 56), new Vector2(0, 0.5f));
                var sub = UI.Text(rt, stars > 0 ? "Completed · " + stars + " stars" : "Reward " + def.RewardCoins + " coins", 34, stars > 0 ? Theme.Green : Theme.InkSoft, TextAlignmentOptions.Left);
                sub.rectTransform.Place(new Vector2(0, 0), new Vector2(170, 56), new Vector2(560, 50), new Vector2(0, 0.5f));
                var b = UI.IconButton(rt, unlocked ? Icon.Play : Icon.Lock, 110, unlocked ? ButtonStyle.Gold : ButtonStyle.White);
                ((RectTransform)b.transform).Place(new Vector2(1, 0.5f), new Vector2(-90, 0), new Vector2(110, 110));
                int level = l;
                b.OnClick = () => { if (unlocked) GameplayController.StartLevel(level); else Router.Toast("Reach level " + level + " first", Icon.Lock); };
            }
        }

        public override void OnBack() => Router.Show<HomeScreen>();
    }
}
