using UnityEngine;
using UnityEngine.UI;
using VoxelBloom.Core;

namespace VoxelBloom
{
    /// <summary>LEVEL SELECT: five chapter cards with thumbnail, progress, stars, lock state and play action.</summary>
    public sealed class LevelSelectScreen : ScreenBase
    {
        RectTransform content;

        protected override void OnBuild()
        {
            UI.Sky(Root, Theme.SkyTop, Theme.SkyBottom);
            var top = TopBar.Create(Root);
            top.OnSettings = () => Router.Open<SettingsDialog>();
            top.OnAddCoins = () => Router.Show<ShopScreen>();
            top.OnAvatar = () => Router.Open<ProfileDialog>();
            var rib = UI.Ribbon(Root, "Level Select", 720, 150);
            rib.Place(new Vector2(0.5f, 1), new Vector2(0, -230), rib.sizeDelta);
            var back = UI.IconButton(Root, Icon.ArrowLeft, 110, ButtonStyle.White);
            ((RectTransform)back.transform).Place(new Vector2(0, 1), new Vector2(90, -230), new Vector2(110, 110));
            back.OnClick = OnBack;
            var scroll = UI.VerticalScroll(Root, out content, 30, new RectOffset(50, 50, 20, 60));
            ((RectTransform)scroll.transform).Stretch(0, 0, 330, 0);
        }

        public override void OnShow()
        {
            for (int i = content.childCount - 1; i >= 0; i--) Destroy(content.GetChild(i).gameObject);
            foreach (var ch in Services.Config.Chapters) BuildCard(ch);
        }

        void BuildCard(ChapterInfo ch)
        {
            bool unlocked = Services.Progress.IsChapterUnlocked(ch);
            var card = UI.Panel(content, "Chapter" + ch.Id, unlocked ? Color.white : Theme.PanelSoft, 48);
            var root = (RectTransform)card.transform.parent;
            UI.Layout(root, 250);
            var thumbBg = UI.Image(root, "ThumbBg", SpriteFactory.Rounded(36), Theme.Hex("#DDF0FF"));
            thumbBg.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(130, 0), new Vector2(210, 210));
            var thumb = UI.Raw(thumbBg.transform, "Thumb", ThumbnailRenderer.Get(ch.ThumbnailModel));
            thumb.rectTransform.Stretch(6, 6, 6, 6);
            if (!unlocked) thumb.color = new Color(1, 1, 1, 0.55f);

            var t1 = UI.Text(root, "Chapter " + ch.Id, 38, Theme.InkSoft, TextAnchor.MiddleLeft);
            t1.rectTransform.Place(new Vector2(0, 1), new Vector2(270, -54), new Vector2(480, 50), new Vector2(0, 0.5f));
            var t2 = UI.Text(root, ch.Name, 54, Theme.Ink, TextAnchor.MiddleLeft);
            t2.rectTransform.Place(new Vector2(0, 1), new Vector2(270, -112), new Vector2(520, 64), new Vector2(0, 0.5f));
            int stars = Services.Progress.ChapterStars(ch);
            var star = UI.Icon(root, Icon.Star, 56);
            star.rectTransform.Place(new Vector2(0, 0), new Vector2(298, 62), new Vector2(56, 56));
            var st = UI.Text(root, unlocked ? stars + "/" + ch.LevelCount * 3 : "Need " + ch.UnlockStars + " stars", 40, unlocked ? Theme.Ink : Theme.InkSoft, TextAnchor.MiddleLeft);
            st.rectTransform.Place(new Vector2(0, 0), new Vector2(340, 62), new Vector2(420, 50), new Vector2(0, 0.5f));

            UIButton action;
            if (unlocked)
            {
                action = UI.IconButton(root, Icon.Play, 120, ButtonStyle.Green);
                action.OnClick = () => Router.Show<LevelGridScreen>().SetChapter(ch);
            }
            else
            {
                action = UI.IconButton(root, Icon.Lock, 120, ButtonStyle.White);
                int need = Mathf.Max(0, ch.UnlockStars - Services.Progress.TotalStars);
                bool reached = Services.Save.HighestUnlockedLevel >= ch.FirstLevel;
                action.OnClick = () => Router.Toast(reached
                    ? "Collect " + need + " more stars to unlock " + ch.Name
                    : "Finish level " + (ch.FirstLevel - 1) + " and collect " + ch.UnlockStars + " stars", Icon.Lock);
            }
            ((RectTransform)action.transform).Place(new Vector2(1, 0.5f), new Vector2(-100, 0), new Vector2(120, 120));
            var hit = root.gameObject.AddComponent<UIButton>();
            root.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0);
            hit.OnClick = action.OnClick;
        }

        public override void OnBack() => Router.Show<HomeScreen>();
    }

    /// <summary>Level grid of one chapter: number, stars, completed/current/locked and HARD markers.</summary>
    public sealed class LevelGridScreen : ScreenBase
    {
        ChapterInfo chapter;
        RectTransform grid;
        RectTransform ribbon;

        protected override void OnBuild()
        {
            UI.Sky(Root, Theme.SkyTop, Theme.SkyBottom);
            var back = UI.IconButton(Root, Icon.ArrowLeft, 110, ButtonStyle.White);
            ((RectTransform)back.transform).Place(new Vector2(0, 1), new Vector2(90, -90), new Vector2(110, 110));
            back.OnClick = OnBack;
            var scroll = UI.VerticalScroll(Root, out var content, 0, new RectOffset(40, 40, 20, 40));
            ((RectTransform)scroll.transform).Stretch(0, 0, 260, 0);
            grid = UI.Rect(content, "Grid");
            var gl = grid.gameObject.AddComponent<GridLayoutGroup>();
            gl.cellSize = new Vector2(220, 250);
            gl.spacing = new Vector2(30, 34);
            gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gl.constraintCount = 4;
            gl.childAlignment = TextAnchor.UpperCenter;
            var fit = grid.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        public void SetChapter(ChapterInfo c)
        {
            chapter = c;
            if (ribbon != null) Destroy(ribbon.gameObject);
            ribbon = UI.Ribbon(Root, c.Name, 760, 150);
            ribbon.Place(new Vector2(0.5f, 1), new Vector2(40, -40), ribbon.sizeDelta);
            Rebuild();
        }

        public override void OnShow() { if (chapter != null) Rebuild(); }

        void Rebuild()
        {
            for (int i = grid.childCount - 1; i >= 0; i--) Destroy(grid.GetChild(i).gameObject);
            int current = Services.Progress.LatestPlayableLevel();
            for (int l = chapter.FirstLevel; l <= chapter.LastLevel; l++) BuildTile(l, current);
            UI.Layout(grid, Mathf.Ceil(chapter.LevelCount / 4f) * 284f);
        }

        void BuildTile(int level, int current)
        {
            bool unlocked = Services.Progress.IsLevelUnlocked(level);
            bool done = Services.Progress.IsCompleted(level);
            bool hard = Services.Progress.IsHard(level);
            bool isCurrent = level == current && !done;
            var style = !unlocked ? ButtonStyle.White : isCurrent ? ButtonStyle.Green : hard ? ButtonStyle.Gold : ButtonStyle.Purple;
            var b = UI.Button(grid, unlocked ? level.ToString() : null, style, new Vector2(220, 220), unlocked ? (Icon?)null : Icon.Lock, 92);
            var face = b.Face;
            if (unlocked)
            {
                var label = face.Find("Label") as RectTransform;
                if (label != null) label.Stretch(0, 0, 0, 50);
                int s = Services.Progress.StarsFor(level);
                for (int i = 0; i < 3; i++)
                {
                    var star = UI.Icon(face, i < s ? Icon.Star : Icon.StarEmpty, 54);
                    star.rectTransform.Place(new Vector2(0.5f, 0), new Vector2((i - 1) * 56, 42 + (i == 1 ? 8 : 0)), new Vector2(54, 54));
                }
            }
            if (hard)
            {
                var tag = UI.Image(b.transform, "Hard", SpriteFactory.Rounded(20), Theme.Red);
                tag.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, 6), new Vector2(150, 50));
                var ic = UI.Icon(tag.transform, Icon.Lightning, 44);
                ic.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(22, 0), new Vector2(44, 44));
                UI.Text(tag.transform, "HARD", 30, Color.white).rectTransform.Stretch(36, 4, 0, 0);
            }
            b.OnClick = () =>
            {
                if (!unlocked) { Router.Toast("Complete level " + (level - 1) + " first", Icon.Lock); return; }
                ConveyorController.StartLevel(level);
            };
        }

        public override void OnBack() => Router.Show<LevelSelectScreen>();
    }
}
