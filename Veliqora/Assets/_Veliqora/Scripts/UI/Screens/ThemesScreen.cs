using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Veliqora.Core.Economy;
using Veliqora.Core.Puzzle;
using Veliqora.Infrastructure;
using Veliqora.Presentation;

namespace Veliqora.UI.Screens
{
    /// <summary>Themes and bottle skins: tabs, live preview, locked/owned/applied states, purchase + apply.</summary>
    public sealed class ThemesScreen : ScreenBase
    {
        private CosmeticKind _tab = CosmeticKind.Theme;
        private string _previewId;
        private RectTransform _grid;       // scroll content
        private ScrollRect _gridScroll;
        private RectTransform _preview;
        private ButtonHandle _tabThemes, _tabBottles, _action;
        private RawImage _bg;
        private bool _bgFollowsTheme;

        protected override void BuildBackground(RectTransform full)
        {
            var screenArt = S.Themes.Theme.IsDark ? null : BackgroundArt.ScreenArt("themes");
            _bg = FullBackground(full, screenArt != null ? (Texture)screenArt : BackgroundArt.Valley(S.Themes.Theme));
            _bgFollowsTheme = screenArt == null;
            ReadabilityScrim(full, 0.4f, 0.3f, 0.15f);
        }

        protected override void Build(object args)
        {
            var bar = TopBar();
            AddBack(bar, () => S.Navigator.GoBackToPrevious());
            AddCoins(bar);

            var bg = UIFactory.Panel(Safe, "Panel", UIStyle.Ivory, UIStyle.RadiusLarge);
            var panel = (RectTransform)bg.transform.parent;
            panel.offsetMin = new Vector2(40, 60);
            panel.offsetMax = new Vector2(-40, -UIStyle.TopBarHeight - 40);

            // Tabs
            _tabThemes = UIFactory.Button(panel, "Themes", UIStyle.ButtonKind.Accent, () => SetTab(CosmeticKind.Theme), new Vector2(420, 120), null, UIStyle.FontBody);
            UIFactory.Place(_tabThemes.Rect, new Vector2(0.5f, 1f), _tabThemes.Rect.sizeDelta, new Vector2(-220, -30), new Vector2(0.5f, 1f));
            _tabBottles = UIFactory.Button(panel, "Bottles", UIStyle.ButtonKind.Secondary, () => SetTab(CosmeticKind.BottleSkin), new Vector2(420, 120), null, UIStyle.FontBody);
            UIFactory.Place(_tabBottles.Rect, new Vector2(0.5f, 1f), _tabBottles.Rect.sizeDelta, new Vector2(220, -30), new Vector2(0.5f, 1f));

            // Preview
            _preview = UIFactory.Rect("Preview", panel);
            UIFactory.Band(_preview, 1f, 470, -175, 40, 1f);

            _action = UIFactory.Button(panel, "Apply", UIStyle.ButtonKind.Primary, OnAction, new Vector2(560, 130), null, UIStyle.FontBody);
            UIFactory.Place(_action.Rect, new Vector2(0.5f, 1f), _action.Rect.sizeDelta, new Vector2(0, -670), new Vector2(0.5f, 1f));

            var viewport = UIFactory.Rect("GridViewport", panel);
            viewport.offsetMin = new Vector2(40, 40);
            viewport.offsetMax = new Vector2(-40, -830);
            viewport.gameObject.AddComponent<Image>().color = new Color(1, 1, 1, 0.003f); // receives drags
            viewport.gameObject.AddComponent<RectMask2D>();
            _grid = UIFactory.Rect("Grid", viewport);
            _grid.anchorMin = new Vector2(0, 1);
            _grid.anchorMax = new Vector2(1, 1);
            _grid.pivot = new Vector2(0.5f, 1);
            _grid.offsetMin = Vector2.zero;
            _grid.offsetMax = Vector2.zero;
            _gridScroll = viewport.gameObject.AddComponent<ScrollRect>();
            _gridScroll.content = _grid;
            _gridScroll.viewport = viewport;
            _gridScroll.horizontal = false;
            _gridScroll.movementType = ScrollRect.MovementType.Clamped;
            _gridScroll.scrollSensitivity = 40f;

            SetTab(CosmeticKind.Theme);
        }

        private void SetTab(CosmeticKind tab)
        {
            _tab = tab;
            Restyle(_tabThemes, tab == CosmeticKind.Theme);
            Restyle(_tabBottles, tab == CosmeticKind.BottleSkin);
            _previewId = tab == CosmeticKind.Theme ? S.Themes.ThemeId : S.Themes.BottleId;
            RebuildGrid();
            RebuildPreview();
        }

        private static void Restyle(ButtonHandle b, bool active)
        {
            b.Kind = active ? UIStyle.ButtonKind.Accent : UIStyle.ButtonKind.Secondary;
            b.SetInteractable(true);
        }

        private List<CosmeticItem> Items()
        {
            var list = new List<CosmeticItem>();
            foreach (var c in S.EconomyConfig.Cosmetics) if (c.Kind == _tab) list.Add(c);
            return list;
        }

        private void RebuildGrid()
        {
            float keepScroll = _gridScroll != null ? _gridScroll.verticalNormalizedPosition : 1f;
            for (int i = _grid.childCount - 1; i >= 0; i--) Destroy(_grid.GetChild(i).gameObject);
            var items = Items();
            Canvas.ForceUpdateCanvases();
            float w = _grid.rect.width;
            const int cols = 4;
            const float gap = 18f;
            float cellW = (w - gap * (cols - 1)) / cols;
            int rows = (items.Count + cols - 1) / cols;
            float cellH = cellW * 1.15f;
            _grid.sizeDelta = new Vector2(0, rows * cellH + (rows - 1) * gap);
            for (int i = 0; i < items.Count; i++)
            {
                int col = i % cols, row = i / cols;
                var item = items[i];
                var cell = UIFactory.Rect("Cell_" + item.Id, _grid);
                cell.anchorMin = cell.anchorMax = new Vector2(0, 1);
                cell.pivot = new Vector2(0, 1);
                cell.sizeDelta = new Vector2(cellW, cellH);
                cell.anchoredPosition = new Vector2(col * (cellW + gap), -row * (cellH + gap));
                BuildCell(cell, item);
            }
            Canvas.ForceUpdateCanvases();
            _gridScroll.verticalNormalizedPosition = keepScroll;
        }

        private void BuildCell(RectTransform cell, CosmeticItem item)
        {
            bool owned = S.Economy.Owns(item.Id);
            bool applied = item.Id == (item.Kind == CosmeticKind.Theme ? S.Themes.ThemeId : S.Themes.BottleId);
            bool previewing = item.Id == _previewId;

            var frame = UIFactory.Image(cell, "Frame", SpriteLibrary.Rounded(UIStyle.RadiusMedium), previewing ? UIStyle.Mint : UIStyle.Card, true);
            var inner = UIFactory.Rect("Inner", frame.transform);
            UIFactory.Stretch(inner, 10);
            inner.offsetMin = new Vector2(10, 64);
            if (item.Kind == CosmeticKind.Theme)
            {
                var thumb = UIFactory.Raw(inner, "Thumb", BackgroundArt.Valley(S.Themes.ThemeFor(item.Id), 180, 240), Color.white);
                UIFactory.Stretch(thumb.rectTransform);
                thumb.uvRect = new Rect(0, 0.1f, 1, 0.75f);
            }
            else
            {
                var b = new GameObject("Bottle", typeof(RectTransform));
                b.transform.SetParent(inner, false);
                var v = b.AddComponent<BottleView>();
                var shape = S.Themes.BottleFor(item.Id, 4);
                Canvas.ForceUpdateCanvases();
                float innerH = Mathf.Max(60f, inner.rect.height);
                float bw = Mathf.Min(inner.rect.width * 0.45f, innerH * 0.9f / shape.Aspect);
                v.Init(0, shape, 4, bw, false);
                v.ShowState(new BottleState(4, 1, 5, 2, 4));
                v.Rect.anchoredPosition = new Vector2(0, -bw * shape.Aspect * 0.5f);
                foreach (var g in b.GetComponentsInChildren<Graphic>()) g.raycastTarget = false;
            }
            var label = UIFactory.Text(frame.transform, item.Title, UIStyle.FontCaption - 4, UIStyle.Ink);
            UIFactory.Band(label.rectTransform, 0f, 60, 4, 8, 0f);

            if (applied)
            {
                var check = UIFactory.IconButton(frame.transform, Icon.Check, UIStyle.ButtonFill(UIStyle.ButtonKind.Primary), 70, null);
                UIFactory.Place(check.Rect, new Vector2(1, 1), check.Rect.sizeDelta, new Vector2(10, 10), new Vector2(1, 1));
                check.Button.interactable = false;
            }
            else if (!owned)
            {
                var lockBg = UIFactory.Image(inner, "Locked", SpriteLibrary.Rounded(24), new Color(0.05f, 0.05f, 0.15f, 0.45f));
                UIFactory.Stretch(lockBg.rectTransform);
                var lk = UIFactory.Image(lockBg.transform, "Lock", SpriteLibrary.Get(Icon.Lock), Color.white);
                UIFactory.Place(lk.rectTransform, new Vector2(0.5f, 0.6f), new Vector2(70, 70), Vector2.zero);
                var price = UIFactory.Text(lockBg.transform, item.Price.ToString(), UIStyle.FontLabel, UIStyle.Sunshine);
                UIFactory.Place(price.rectTransform, new Vector2(0.5f, 0.25f), new Vector2(200, 60), Vector2.zero);
            }
            var btn = frame.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            frame.gameObject.AddComponent<ButtonFx>().Target = cell;
            btn.onClick.AddListener(() =>
            {
                _previewId = item.Id;
                RebuildGrid();
                RebuildPreview();
            });
        }

        private void RebuildPreview()
        {
            for (int i = _preview.childCount - 1; i >= 0; i--) Destroy(_preview.GetChild(i).gameObject);
            var item = S.EconomyConfig.FindCosmetic(_previewId);
            if (item == null) return;
            var theme = item.Kind == CosmeticKind.Theme ? S.Themes.ThemeFor(item.Id) : S.Themes.Theme;
            string bottleId = item.Kind == CosmeticKind.BottleSkin ? item.Id : S.Themes.BottleId;

            var frame = UIFactory.Image(_preview, "Frame", SpriteLibrary.Rounded(UIStyle.RadiusMedium), Color.white);
            var mask = frame.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = true;
            var sky = UIFactory.Raw(frame.transform, "Valley", BackgroundArt.Valley(theme, 270, 480), Color.white);
            UIFactory.Stretch(sky.rectTransform);
            sky.uvRect = new Rect(0, 0.25f, 1, 0.5f);
            var game = UIFactory.Raw(frame.transform, "Game", BackgroundArt.GameplayGradient(theme.GameTop, theme.GameBottom, false), Color.white);
            game.rectTransform.anchorMin = new Vector2(0.52f, 0.06f);
            game.rectTransform.anchorMax = new Vector2(0.97f, 0.94f);
            game.rectTransform.offsetMin = game.rectTransform.offsetMax = Vector2.zero;
            var shape = S.Themes.BottleFor(bottleId, 4);
            int[][] demo = { new[] { 1, 2, 1, 3 }, new[] { 3, 3, 2 }, new[] { 2, 1 } };
            for (int i = 0; i < demo.Length; i++)
            {
                var b = new GameObject("Demo" + i, typeof(RectTransform));
                b.transform.SetParent(game.transform, false);
                var v = b.AddComponent<BottleView>();
                v.Init(i, shape, 4, 80, S.Settings.Data.ColorblindSymbols);
                v.ShowState(new BottleState(4, demo[i]));
                v.Rect.anchoredPosition = new Vector2((i - 1) * 125, -150);
                foreach (var g in b.GetComponentsInChildren<Graphic>()) g.raycastTarget = false;
            }
            var name = UIFactory.Text(frame.transform, item.Title, UIStyle.FontBody, Color.white, TextAnchor.UpperLeft);
            name.rectTransform.offsetMin = new Vector2(30, 0);
            name.rectTransform.offsetMax = new Vector2(0, -24);
            UIFactory.AddShadow(name, new Color(0, 0, 0, 0.45f), new Vector2(0, -3));

            bool owned = S.Economy.Owns(item.Id);
            bool applied = item.Id == (item.Kind == CosmeticKind.Theme ? S.Themes.ThemeId : S.Themes.BottleId);
            if (applied) { _action.SetLabel("Applied"); _action.Kind = UIStyle.ButtonKind.Secondary; _action.SetInteractable(false); }
            else if (owned) { _action.SetLabel("Apply"); _action.Kind = UIStyle.ButtonKind.Primary; _action.SetInteractable(true); }
            else
            {
                _action.SetLabel("Unlock for " + item.Price + " coins");
                _action.Kind = UIStyle.ButtonKind.Gold;
                _action.SetInteractable(S.Economy.Coins >= item.Price);
            }
        }

        private void OnAction()
        {
            var item = S.EconomyConfig.FindCosmetic(_previewId);
            if (item == null) return;
            if (!S.Economy.Owns(item.Id))
            {
                Overlays.Confirm("Unlock " + item.Title + "?", "Spend " + item.Price + " coins to unlock and apply it.", "Unlock", () =>
                {
                    var r = S.Economy.BuyCosmetic(item.Id, "cosmetic:" + item.Id + ":" + System.Guid.NewGuid().ToString("N"));
                    if (r.Success) { S.Audio.PlayCoin(); Apply(item); }
                    else Overlays.Toast(r.Status == TxStatus.InsufficientFunds ? "Not enough coins yet." : "Couldn't unlock (" + r.Status + ").");
                }, "Cancel", UIStyle.ButtonKind.Gold);
                return;
            }
            Apply(item);
        }

        private void Apply(CosmeticItem item)
        {
            if (!S.Themes.Apply(item.Id)) return;
            S.SaveNow();
            if (item.Kind == CosmeticKind.Theme && _bg != null && (_bgFollowsTheme || S.Themes.Theme.IsDark))
            {
                var tex = BackgroundArt.Valley(S.Themes.Theme);
                _bg.texture = tex;
            }
            Overlays.Toast(item.Title + " applied!");
            RebuildGrid();
            RebuildPreview();
        }
    }
}
