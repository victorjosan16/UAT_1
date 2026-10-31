using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Veliqora.Infrastructure;
using Veliqora.Presentation;

namespace Veliqora.UI.Screens
{
    /// <summary>
    /// Vertical scrolling valley route. Nodes show locked / unlocked / completed (stars) / current states, chest
    /// milestones every 10 levels, and are culled outside the viewport so 200+ levels stay cheap.
    /// </summary>
    public sealed class LevelMapScreen : ScreenBase
    {
        public override bool AllowsBanner => true;

        private const float Spacing = 230f;
        private const float BottomMargin = 360f;
        private const float TopMargin = 420f;

        private ScrollRect _scroll;
        private RectTransform _content;
        private readonly List<RectTransform> _nodes = new List<RectTransform>();
        private readonly List<float> _nodeY = new List<float>();
        private float _width;

        protected override void Build(object args)
        {
            var theme = S.Themes.Theme;
            int count = S.Levels.Count;
            var prog = S.Save.Progress;

            // Scroll view (full screen, under the safe-area header)
            var viewport = UIFactory.Rect("Viewport", Full);
            viewport.SetAsFirstSibling();
            var vpImg = viewport.gameObject.AddComponent<Image>();
            vpImg.color = new Color(1, 1, 1, 0.003f); // receives drags
            viewport.gameObject.AddComponent<RectMask2D>();
            _content = UIFactory.Rect("Content", viewport);
            _content.anchorMin = new Vector2(0, 0);
            _content.anchorMax = new Vector2(1, 0);
            _content.pivot = new Vector2(0.5f, 0);
            float height = BottomMargin + (count - 1) * Spacing + TopMargin;
            _content.sizeDelta = new Vector2(0, height);
            _content.anchoredPosition = Vector2.zero;

            _scroll = viewport.gameObject.AddComponent<ScrollRect>();
            _scroll.content = _content;
            _scroll.viewport = viewport;
            _scroll.horizontal = false;
            _scroll.vertical = true;
            _scroll.movementType = ScrollRect.MovementType.Clamped;
            _scroll.inertia = true;
            _scroll.decelerationRate = 0.12f;
            _scroll.scrollSensitivity = 40f;

            Canvas.ForceUpdateCanvases();
            _width = Full.rect.width;

            // Repeating valley tile background (one draw call)
            var tex = BackgroundArt.MapTile(theme);
            var bg = UIFactory.Raw(_content, "Tiles", tex, Color.white);
            float tileH = _width * (BackgroundArt.MapTileHeight / (float)BackgroundArt.MapTileWidth);
            bg.uvRect = new Rect(0, 0, 1, height / tileH);

            for (int n = 1; n <= count; n++)
            {
                float y = BottomMargin + (n - 1) * Spacing;
                float x = NodeX(y, tileH);
                _nodeY.Add(y);
                var node = BuildNode(n, new Vector2(x, y), prog);
                _nodes.Add(node);
                if (n > 1) Dots(new Vector2(NodeX(y - Spacing, tileH), y - Spacing), new Vector2(x, y), node);
            }

            // Header
            var bar = TopBar();
            AddBack(bar, () => S.Navigator.Go(ScreenId.Home));
            AddCoins(bar);
            Ribbon(Safe, "Valley Map", -UIStyle.TopBarHeight - UIStyle.Space3, 520, UIStyle.Mint);
            var gear = UIFactory.IconButton(Safe, Icon.Gear, UIStyle.Violet, 110, () => CommonOverlays.ShowSettings(S));
            UIFactory.Place(gear.Rect, new Vector2(1, 0), gear.Rect.sizeDelta, new Vector2(-UIStyle.Space3, UIStyle.Space3), new Vector2(1, 0));

            _scroll.onValueChanged.AddListener(_ => Cull());
            ScrollTo(prog.CurrentLevel);
            Cull();
        }

        private float NodeX(float y, float tileH)
        {
            float yNorm = Mathf.Repeat(y / tileH, 1f);
            float river = BackgroundArt.MapRiverX(yNorm);
            // The route mirrors the river, crossing it like stepping stones near the center.
            float x = 0.5f - (river - 0.5f) * 1.25f;
            return (Mathf.Clamp(x, 0.16f, 0.84f) - 0.5f) * _width;
        }

        private void Dots(Vector2 from, Vector2 to, RectTransform owner)
        {
            for (int i = 1; i <= 3; i++)
            {
                var p = Vector2.Lerp(from, to, i / 4f);
                var d = UIFactory.Image(owner, "Dot", SpriteLibrary.Circle, new Color(1, 1, 1, 0.85f));
                UIFactory.Place(d.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(22, 22), p - to);
                d.transform.SetAsFirstSibling();
            }
        }

        private RectTransform BuildNode(int n, Vector2 pos, Core.Save.ProgressData prog)
        {
            bool completed = prog.IsCompleted(n);
            bool unlocked = n <= prog.HighestUnlocked;
            bool current = n == prog.CurrentLevel && unlocked;
            var node = UIFactory.Rect("Node_" + n, _content);
            node.anchorMin = node.anchorMax = new Vector2(0.5f, 0f);
            node.pivot = new Vector2(0.5f, 0.5f);
            node.sizeDelta = new Vector2(150, 150);
            node.anchoredPosition = pos;

            float size = current ? 170 : 140;
            Color fill = completed ? UIStyle.Sunshine : (unlocked ? UIStyle.Violet : UIStyle.Hex("#9AA3B5"));
            var btn = UIFactory.IconButton(node, unlocked ? Icon.Play : Icon.Lock, fill, size, () => OnNode(n));
            UIFactory.Place(btn.Rect, new Vector2(0.5f, 0.5f), new Vector2(size, size), Vector2.zero);
            if (unlocked)
            {
                btn.Icon.enabled = false;
                var num = UIFactory.Text(btn.Icon.transform.parent, n.ToString(), current ? UIStyle.FontHeading : UIStyle.FontBody, completed ? UIStyle.Brown : Color.white);
                UIFactory.AddShadow(num, new Color(0, 0, 0, 0.2f), new Vector2(0, -3));
            }
            else
            {
                btn.Icon.rectTransform.offsetMin = btn.Icon.rectTransform.offsetMax = Vector2.zero;
                UIFactory.Stretch(btn.Icon.rectTransform, size * 0.3f);
            }

            if (completed)
            {
                int stars = prog.Stars(n);
                for (int i = 0; i < 3; i++)
                {
                    var st = UIFactory.Image(node, "Star", SpriteLibrary.Get(i < stars ? Icon.Star : Icon.StarEmpty), Color.white);
                    float sx = (i - 1) * 46f;
                    UIFactory.Place(st.rectTransform, new Vector2(0.5f, 0f), new Vector2(i == 1 ? 58 : 48, i == 1 ? 58 : 48), new Vector2(sx, i == 1 ? -36 : -26));
                }
            }

            if (current)
            {
                var ring = UIFactory.Image(node, "Current", SpriteLibrary.RadialGlow, new Color(1f, 0.95f, 0.55f, 0.9f));
                UIFactory.Stretch(ring.rectTransform, -60);
                ring.transform.SetAsFirstSibling();
                Tween.Pulse(this, btn.Rect, 0.06f, 1.0f);
            }

            if (n % 10 == 0)
            {
                bool open = completed;
                float side = pos.x > 0 ? -1 : 1;
                var chest = UIFactory.Image(node, "Chest", SpriteLibrary.Get(open ? Icon.ChestOpen : Icon.Chest), Color.white, true);
                UIFactory.Place(chest.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(130, 130), new Vector2(side * 175, 20));
                if (open)
                {
                    var check = UIFactory.Image(chest.transform, "Opened", SpriteLibrary.Get(Icon.Check), UIStyle.ButtonFill(UIStyle.ButtonKind.Primary));
                    UIFactory.Place(check.rectTransform, new Vector2(1, 1), new Vector2(54, 54), Vector2.zero, new Vector2(1, 1));
                    UIFactory.AddOutline(check, Color.white, 3f);
                }
                var cb = chest.gameObject.AddComponent<Button>();
                cb.transition = Selectable.Transition.None;
                int level = n;
                cb.onClick.AddListener(() => Overlays.Toast(open
                    ? "Milestone chest from level " + level + " already collected."
                    : "Milestone chest: finish level " + level + " for +" + S.EconomyConfig.MilestoneCoins + " coins and bonus boosters!"));
            }
            return node;
        }

        private void OnNode(int n)
        {
            if (n > S.Save.Progress.HighestUnlocked)
            {
                Overlays.Toast("Complete level " + S.Save.Progress.HighestUnlocked + " to unlock this one.");
                return;
            }
            S.Navigator.Go(ScreenId.Gameplay, n);
        }

        private void ScrollTo(int level)
        {
            Canvas.ForceUpdateCanvases();
            float viewH = _scroll.viewport.rect.height;
            float contentH = _content.rect.height;
            if (contentH <= viewH) return;
            float y = _nodeY[Mathf.Clamp(level - 1, 0, _nodeY.Count - 1)];
            float bottom = Mathf.Clamp(y - viewH * 0.42f, 0f, contentH - viewH);
            _scroll.verticalNormalizedPosition = bottom / (contentH - viewH);
        }

        private void Cull()
        {
            float viewH = _scroll.viewport.rect.height;
            float bottom = -_content.anchoredPosition.y;
            float lo = bottom - Spacing * 2f, hi = bottom + viewH + Spacing * 2f;
            for (int i = 0; i < _nodes.Count; i++)
            {
                bool vis = _nodeY[i] >= lo && _nodeY[i] <= hi;
                if (_nodes[i].gameObject.activeSelf != vis) _nodes[i].gameObject.SetActive(vis);
            }
        }
    }
}
