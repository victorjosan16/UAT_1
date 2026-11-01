using UnityEngine;
using UnityEngine.UI;
using VoxelBloom.Core;

namespace VoxelBloom
{
    /// <summary>"Niveluri": the conveyor levels with stars and locks (a winding map like the mockup comes in the visual phase).</summary>
    public sealed class ConveyorLevelsScreen : ScreenBase
    {
        RectTransform grid;

        protected override void OnBuild()
        {
            UI.Sky(Root, ConveyorScreen.Bg1, ConveyorScreen.Bg2);
            var back = UI.IconButton(Root, Icon.ArrowLeft, 110, ButtonStyle.Purple);
            ((RectTransform)back.transform).Place(new Vector2(0, 1), new Vector2(90, -90), new Vector2(110, 110));
            back.OnClick = OnBack;
            var rib = UI.Ribbon(Root, "Niveluri", 640, 140);
            rib.Place(new Vector2(0.5f, 1), new Vector2(40, -90), rib.sizeDelta);
            var scroll = UI.VerticalScroll(Root, out var content, 0, new RectOffset(40, 40, 30, 60));
            ((RectTransform)scroll.transform).Stretch(0, 0, 210, 0);
            grid = UI.Rect(content, "Grid");
            var gl = grid.gameObject.AddComponent<GridLayoutGroup>();
            gl.cellSize = new Vector2(220, 250);
            gl.spacing = new Vector2(30, 34);
            gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gl.constraintCount = 4;
            gl.childAlignment = TextAnchor.UpperCenter;
        }

        public override void OnShow()
        {
            for (int i = grid.childCount - 1; i >= 0; i--) Destroy(grid.GetChild(i).gameObject);
            int total = ConveyorController.TotalLevels;
            int current = Services.Progress.LatestConveyorLevel(total);
            for (int l = 1; l <= total; l++) BuildTile(l, current, total);
            UI.Layout(grid, Mathf.Ceil(total / 4f) * 284f);
        }

        void BuildTile(int level, int current, int total)
        {
            bool unlocked = Services.Progress.IsConveyorUnlocked(level, total);
            int stars = Services.Progress.ConveyorStarsFor(level);
            bool isCurrent = level == current && stars == 0;
            var def = Services.Data.GetConveyorLevel(level);
            int tier = def != null ? def.Tier : 1;
            var style = !unlocked ? ButtonStyle.White : tier == 0 ? ButtonStyle.Green : tier == 1 ? ButtonStyle.Purple : tier == 2 ? ButtonStyle.Gold : ButtonStyle.Red;
            var b = UI.Button(grid, unlocked ? level.ToString() : null, style, new Vector2(220, 220), unlocked ? (Icon?)null : Icon.Lock, 92);
            if (isCurrent) { var ring = UI.Image(b.transform, "Current", SpriteFactory.Rounded(56), Color.white); ring.rectTransform.Stretch(-10, -10, -10, -10); ring.transform.SetAsFirstSibling(); }
            if (tier >= 2 && def != null)
            {
                var tag = UI.Image(b.transform, "Tier", SpriteFactory.Rounded(20), ConveyorScreen.TierColors[tier]);
                tag.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, 6), new Vector2(tier == 3 ? 190 : 130, 46));
                UI.Text(tag.transform, tier == 3 ? "F. GREU" : "GREU", 28, Color.white, TextAnchor.MiddleCenter, 0.15f, Theme.Darker(ConveyorScreen.TierColors[tier], 0.5f)).rectTransform.Stretch();
            }
            if (unlocked)
            {
                var label = b.Face.Find("Label") as RectTransform;
                if (label != null) label.Stretch(0, 0, 0, 50);
                for (int i = 0; i < 3; i++)
                {
                    var sp = UIArt.Get(i < stars ? "Icons/star_gold" : "Icons/star_empty") ?? SpriteFactory.Get(i < stars ? Icon.Star : Icon.StarEmpty);
                    var star = UI.Image(b.Face, "Star", sp, Color.white, false);
                    star.preserveAspect = true;
                    star.rectTransform.Place(new Vector2(0.5f, 0), new Vector2((i - 1) * 56, 42 + (i == 1 ? 8 : 0)), new Vector2(54, 54));
                }
            }
            b.OnClick = () =>
            {
                if (!unlocked) { Router.Toast("Termină întâi nivelul " + (level - 1), Icon.Lock); return; }
                ConveyorController.StartLevel(level);
            };
        }

        public override void OnBack() => Router.Show<HomeScreen>();
    }
}
