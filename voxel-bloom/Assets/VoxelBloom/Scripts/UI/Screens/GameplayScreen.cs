using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VoxelBloom.Core;

namespace VoxelBloom
{
    /// <summary>One numbered attack block (board tile or queue slot content).</summary>
    public sealed class BlockView
    {
        public RectTransform Root;
        public Image Face, Lip;
        public VText Label;
        public UIButton Button;
        public AttackBlock? Block;

        public static BlockView Create(Transform parent, Vector2 size, string name)
        {
            var b = UI.Button(parent, "0", ButtonStyle.Purple, size, null, size.y * 0.46f);
            b.name = name;
            b.Sound = Sfx.Select;
            b.Cooldown = 0.05f;
            var v = new BlockView { Root = (RectTransform)b.transform, Button = b };
            v.Face = b.Face.GetComponent<Image>();
            v.Lip = b.transform.Find("Lip").GetComponent<Image>();
            v.Label = b.Face.Find("Label").GetComponent<VText>();
            v.Label.fontStyle = FontStyle.Bold;
            return v;
        }

        /// <summary>Row 0 is active (dimmed slightly when its colour is not visible yet); deeper rows are previews.</summary>
        public void SetPlayable(bool visibleColour, int row = 0)
        {
            var cg = Root.GetComponent<CanvasGroup>();
            if (cg == null) cg = Root.gameObject.AddComponent<CanvasGroup>();
            // inactive rows are only slightly faded (colours stay true, never grey)
            cg.alpha = row > 0 ? 0.72f : (visibleColour ? 1f : 0.88f);
            Root.localScale = Vector3.one * (row > 0 ? 0.96f : 1f);
        }

        public void Set(AttackBlock? block, Color32[] palette)
        {
            Block = block;
            Root.gameObject.SetActive(block.HasValue);
            if (!block.HasValue) return;
            Color c = palette[block.Value.Color];
            Face.color = c;
            Lip.color = Theme.Darker(c, 0.72f);
            Label.text = block.Value.Power.ToString();
            float lum = 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;
            bool light = lum > 0.62f;
            Label.color = light ? Theme.Navy : Color.white;
            Label.outlineColor = light ? new Color(1, 1, 1, 0.6f) : (Color)Theme.Darker(c, 0.5f);
        }
    }

    /// <summary>Drag to rotate the model; tap to pick a voxel (boosters). Drag never selects board tiles.</summary>
    public sealed class ModelInput : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, IScrollHandler
    {
        public VoxelStage Stage;
        public Action<Vector2> OnTapUv;
        public float ZoomMin = 0.7f, ZoomMax = 1.6f;
        public bool AllowZoom;
        bool dragging;

        public void OnBeginDrag(PointerEventData e) { dragging = true; if (Stage != null) Stage.NotifyInteraction(); }
        public void OnDrag(PointerEventData e)
        {
            if (Stage == null) return;
            if (AllowZoom && Input.touchCount >= 2)
            {
                var t0 = Input.GetTouch(0); var t1 = Input.GetTouch(1);
                float prev = ((t0.position - t0.deltaPosition) - (t1.position - t1.deltaPosition)).magnitude;
                float cur = (t0.position - t1.position).magnitude;
                Zoom(cur / Mathf.Max(1f, prev));
                return;
            }
            Stage.TargetYaw -= e.delta.x * 0.35f;
            Stage.NotifyInteraction();
        }
        public void OnEndDrag(PointerEventData e) { dragging = false; }
        public void OnScroll(PointerEventData e) { if (AllowZoom) Zoom(1f + e.scrollDelta.y * 0.08f); }

        void Zoom(float k)
        {
            var p = Stage.Pivot;
            float s = Mathf.Clamp(p.localScale.x * k, ZoomMin, ZoomMax);
            p.localScale = Vector3.one * s;
        }

        public void OnPointerClick(PointerEventData e)
        {
            if (dragging || e.dragging) return;
            var rt = (RectTransform)transform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, e.position, e.pressEventCamera, out var local)) return;
            var r = rt.rect;
            OnTapUv?.Invoke(new Vector2((local.x - r.xMin) / r.width, (local.y - r.yMin) / r.height));
        }
    }

    /// <summary>GAMEPLAY view: builds and refreshes widgets; all decisions live in GameplayController.</summary>
    public sealed class GameplayScreen : ScreenBase
    {
        public VoxelStage Stage { get; private set; }
        public const int VisibleRows = 4;
        /// <summary>Board[col * VisibleRows + row]; row 0 is the active top block of the column.</summary>
        public readonly BlockView[] Board = new BlockView[GameSession.Columns * VisibleRows];
        public readonly BlockView[] LauncherBlocks = new BlockView[GameSession.Launchers];
        public readonly RectTransform[] LauncherSlots = new RectTransform[GameSession.Launchers];
        public readonly Image[] LauncherGlow = new Image[GameSession.Launchers];
        public readonly Image[] LauncherOrb = new Image[GameSession.Launchers];
        public readonly VText[] BoosterCounts = new VText[4];
        public readonly UIButton[] BoosterButtons = new UIButton[4];
        public VText LevelText, CoinsText, ProgressText, RemainingText, HintText;
        public Image[] Stars = new Image[3];
        public ProgressBar Progress;
        public RectTransform ModelPanel, ComboLayer, HardBadge, BoardRoot;
        public ModelInput ModelInput;
        public UIButton PauseButton;
        GameplayController controller;

        public static readonly BoosterType[] BoosterOrder = { BoosterType.Hammer, BoosterType.Shuffle, BoosterType.ColorBomb, BoosterType.Eliminate };

        protected override void OnBuild()
        {
            controller = gameObject.AddComponent<GameplayController>();
            controller.Bind(this);
            UI.Sky(Root, Theme.GameBackground, Theme.GameBackground);
            BuildHud();
            BuildModelArea();
            BuildQueue();
            BuildBoard();
            BuildBoosters();
        }

        void BuildHud()
        {
            var hud = UI.Rect(Root, "Hud").TopBand(20, 110, 30);
            PauseButton = UI.IconButton(hud, Icon.Pause, 104, ButtonStyle.White);
            ((RectTransform)PauseButton.transform).Place(new Vector2(0, 0.5f), new Vector2(52, 0), new Vector2(104, 104));
            PauseButton.OnClick = () => controller.Pause();

            var lv = UI.Panel(hud, "LevelPill", Theme.Purple, 40, true, 4f);
            var lvRoot = (RectTransform)lv.transform.parent;
            lvRoot.Place(new Vector2(0, 0.5f), new Vector2(240, 0), new Vector2(170, 76));
            LevelText = UI.Text(lvRoot, "Lv. 1", 40, Color.white);
            LevelText.rectTransform.Stretch();

            var starBg = UI.Panel(hud, "Stars", new Color(1, 1, 1, 0.92f), 40, true, 4f);
            var sr = (RectTransform)starBg.transform.parent;
            sr.Place(new Vector2(0.5f, 0.5f), new Vector2(-10, 0), new Vector2(230, 76));
            for (int i = 0; i < 3; i++)
            {
                Stars[i] = UI.Icon(sr, Icon.Star, 64);
                Stars[i].rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 66, i == 1 ? 6 : 0), new Vector2(64, 64));
            }
            CoinsText = UI.Pill(hud, Icon.Coin, "0", out var cr, 260, 76);
            cr.Place(new Vector2(1, 0.5f), new Vector2(-130, 0), new Vector2(260, 76));
            var addCoins = UI.IconButton(cr, Icon.Plus, 64, ButtonStyle.Green);
            ((RectTransform)addCoins.transform).Place(new Vector2(1, 0.5f), new Vector2(-8, 0), new Vector2(64, 64));
            addCoins.OnClick = () => Router.Open<BoostersDialog>();
        }

        void BuildModelArea()
        {
            var panel = UI.Panel(Root, "ModelPanel", Theme.GamePanel, 56);
            ModelPanel = (RectTransform)panel.transform.parent;
            ModelPanel.anchorMin = Vector2.zero; ModelPanel.anchorMax = Vector2.one;
            ModelPanel.offsetMin = new Vector2(40, 1095); ModelPanel.offsetMax = new Vector2(-40, -150);
            panel.sprite = SpriteFactory.Rounded(56);
            var sky = UI.Image(panel.transform, "Sky", SpriteFactory.VerticalGradient(Theme.Hex("#6FC2FF"), Theme.Hex("#CFEFFF")), Color.white, false);
            sky.rectTransform.Stretch(10, 10, 10, 10);
            for (int c = 0; c < 4; c++)
            {
                var cloud = UI.Image(sky.transform, "Cloud", SpriteFactory.Glow(), new Color(1, 1, 1, 0.55f), false);
                cloud.rectTransform.Place(new Vector2(c % 2 == 0 ? 0.2f : 0.8f, c < 2 ? 0.82f : 0.62f), new Vector2(c * 40 - 60, 0), new Vector2(360 - c * 40, 150 - c * 15));
            }
            var mask = UI.Image(panel.transform, "Mask", SpriteFactory.Rounded(48), Color.white);
            mask.rectTransform.Stretch(10, 10, 10, 10);
            mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            sky.transform.SetParent(mask.transform, true);

            Stage = VoxelStage.Create("Gameplay", 1024, false); // no platform: the object floats in the air
            Stage.Floating = true;
            var raw = UI.Raw(mask.transform, "Model", Stage.Texture);
            raw.rectTransform.Stretch();
            var arf = raw.gameObject.AddComponent<AspectRatioFitter>();
            arf.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            arf.aspectRatio = 1f;
            raw.raycastTarget = true;
            ModelInput = raw.gameObject.AddComponent<ModelInput>();
            ModelInput.Stage = Stage;

            // soft vignette framing the model (premium depth)
            var vig = UI.Image(mask.transform, "Vignette", SpriteFactory.Shadow(48), new Color(0.15f, 0.25f, 0.45f, 0.18f));
            vig.rectTransform.Stretch(-40, -40, -40, -40);
            var left = UI.IconButton(ModelPanel, Icon.ArrowLeft, 100, ButtonStyle.White);
            ((RectTransform)left.transform).Place(new Vector2(0, 0.5f), new Vector2(80, 0), new Vector2(100, 100));
            left.OnClick = () => { Stage.TargetYaw += 45f; Stage.NotifyInteraction(); };
            var right = UI.IconButton(ModelPanel, Icon.ArrowRight, 100, ButtonStyle.White);
            ((RectTransform)right.transform).Place(new Vector2(1, 0.5f), new Vector2(-80, 0), new Vector2(100, 100));
            right.OnClick = () => { Stage.TargetYaw -= 45f; Stage.NotifyInteraction(); };

            var hb = UI.Image(ModelPanel, "HardBadge", SpriteFactory.Rounded(28), Theme.Red);
            HardBadge = hb.rectTransform;
            HardBadge.Place(new Vector2(0, 1), new Vector2(150, -50), new Vector2(230, 64));
            UI.Icon(HardBadge, Icon.Lightning, 56).rectTransform.Place(new Vector2(0, 0.5f), new Vector2(34, 0), new Vector2(56, 56));
            UI.Text(HardBadge, "HARD x2.5", 32, Color.white).rectTransform.Stretch(60, 8, 0, 0);

            HintText = UI.Text(ModelPanel, "", 40, Color.white, TextAnchor.MiddleCenter, 0.25f, Theme.PurpleDark);
            HintText.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -60), new Vector2(760, 60));

            ComboLayer = UI.Rect(ModelPanel, "Combo").Stretch();

            var prog = UI.Rect(Root, "ProgressRow").BottomBand(1015, 70, 60);
            var cube = UI.Icon(prog, Icon.Cube, 70);
            cube.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(35, 0), new Vector2(70, 70));
            Progress = UI.Progress(prog, Theme.Green, 44);
            ((RectTransform)Progress.transform).Stretch(90, 150, 13, 13);
            ProgressText = UI.Text(prog, "0%", 42, Theme.Ink, TextAnchor.MiddleRight);
            ProgressText.rectTransform.Place(new Vector2(1, 0.5f), new Vector2(0, 0), new Vector2(140, 60), new Vector2(1, 0.5f));
        }

        void BuildQueue()
        {
            // 4 launchers: a block moved here fires coloured projectiles at its colour until its count reaches 0
            var row = UI.Rect(Root, "Launchers").BottomBand(790, 170, 60);
            float w = 200f, gap = (960f - 4 * w) / 3f;
            for (int i = 0; i < GameSession.Launchers; i++)
            {
                var basePanel = UI.Panel(row, "Launcher" + i, Theme.SlotEmpty, 36, true, 6f);
                var plus = UI.Text(basePanel.transform, "+", 72, new Color(0.55f, 0.65f, 0.82f, 1f));
                plus.rectTransform.Stretch(0, 0, 0, 10);
                var slot = (RectTransform)basePanel.transform.parent;
                slot.Place(new Vector2(0, 0.5f), new Vector2(w / 2 + i * (w + gap), -10), new Vector2(w, 150));
                LauncherSlots[i] = slot;
                var glow = UI.Image(slot, "Glow", SpriteFactory.Glow(), new Color(1, 1, 1, 0f), false);
                glow.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0, -10), new Vector2(170, 110)); // subtle, not neon
                LauncherGlow[i] = glow;
                var lb = BlockView.Create(slot, new Vector2(w - 24, 108), "Loaded" + i);
                lb.Root.Place(new Vector2(0.5f, 0.5f), new Vector2(0, -6), new Vector2(w - 24, 108));
                // glossy ball on top of the launcher (tinted with the loaded colour)
                var orb = UI.Image(slot, "Orb", SpriteFactory.Orb(), new Color(0.75f, 0.75f, 0.82f, 1f), false);
                orb.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0, 16), new Vector2(74, 74));
                LauncherOrb[i] = orb;
                int li = i;
                lb.Button.Sound = Sfx.Button;
                lb.Button.OnClick = () => controller.OnLauncherTapped(li);
                LauncherBlocks[i] = lb;
            }
            RemainingText = UI.Text(Root, "", 36, Theme.InkSoft);
            RemainingText.rectTransform.BottomBand(965, 45, 60);
        }

        void BuildBoard()
        {
            var panel = UI.Panel(Root, "BoardPanel", Theme.GamePanel, 48, true);
            BoardRoot = (RectTransform)panel.transform.parent;
            BoardRoot.BottomBand(190, VisibleRows * 125 + (VisibleRows - 1) * 12 + 40, 40);
            float w = (1000f - 40f - 3 * 16f) / 4f, h = 125f;
            for (int col = 0; col < GameSession.Columns; col++)
                for (int row = 0; row < VisibleRows; row++)
                {
                    var bv = BlockView.Create(BoardRoot, new Vector2(w, h), "Block" + col + "_" + row);
                    bv.Root.Place(new Vector2(0, 1), new Vector2(20 + w / 2 + col * (w + 16), -20 - h / 2 - row * (h + 12)), new Vector2(w, h));
                    int c = col;
                    if (row == 0)
                    {
                        bv.Button.OnClick = () => controller.OnColumnTapped(c);
                        bv.Button.OnPress = () => controller.PreviewColumn(c);
                        bv.Button.OnRelease = () => controller.ClearPreview();
                    }
                    else bv.Button.Interactable = false;
                    Board[col * VisibleRows + row] = bv;
                }
        }

        void BuildBoosters()
        {
            var row = UI.Rect(Root, "Boosters").BottomBand(20, 150, 120);
            Icon[] icons = { Icon.Hammer, Icon.Shuffle, Icon.ColorBomb, Icon.Eliminate };
            for (int i = 0; i < 4; i++)
            {
                var b = UI.IconButton(row, icons[i], 140, ButtonStyle.White);
                ((RectTransform)b.transform).Place(new Vector2((i + 0.5f) / 4f, 0.5f), Vector2.zero, new Vector2(140, 140));
                b.Sound = Sfx.Booster;
                var t = BoosterOrder[i];
                b.OnClick = () => controller.UseBooster(t);
                var badge = UI.Image(b.transform, "Count", SpriteFactory.Circle(), Theme.Red, false);
                badge.rectTransform.Place(new Vector2(1, 0), new Vector2(-14, 18), new Vector2(60, 60));
                BoosterCounts[i] = UI.Text(badge.transform, "0", 36, Color.white);
                BoosterCounts[i].rectTransform.Stretch();
                BoosterButtons[i] = b;
            }
        }

        public void ShowCombo(int combo)
        {
            var t = UI.Text(ComboLayer, "COMBO\nx" + Mathf.Min(combo, 5), 110, Theme.Gold, TextAnchor.MiddleCenter, 0.3f, Theme.Darker(Theme.Red, 0.8f));
            t.fontStyle = FontStyle.Bold;
            t.lineSpacing = 0.8f;
            t.rectTransform.Place(new Vector2(1, 1), new Vector2(-190, -170), new Vector2(360, 260));
            t.rectTransform.localRotation = Quaternion.Euler(0, 0, -10);
            t.SetGradient(Theme.Lighter(Theme.Gold, 0.4f), Theme.Hex("#FF7A5C"));
            var cg = t.gameObject.AddComponent<CanvasGroup>();
            Tween.Scale(t.transform, Vector3.one * 0.3f, Vector3.one, 0.35f, Ease.OutBack);
            Tween.Delay(0.8f, () => { if (cg != null) Tween.Fade(cg, 1, 0, 0.3f, () => { if (t != null) Destroy(t.gameObject); }); });
        }

        public void FloatText(string text, Color color, Vector2 pos)
        {
            var t = UI.Text(ComboLayer, text, 54, color, TextAnchor.MiddleCenter, 0.25f, Theme.Ink);
            t.rectTransform.Place(new Vector2(0.5f, 0.5f), pos, new Vector2(500, 80));
            var cg = t.gameObject.AddComponent<CanvasGroup>();
            Tween.Move(t.rectTransform, pos, pos + new Vector2(0, 120), 0.9f);
            Tween.Delay(0.5f, () => { if (cg != null) Tween.Fade(cg, 1, 0, 0.4f, () => { if (t != null) Destroy(t.gameObject); }); });
        }

        public override void OnShow() { Stage.SetActive(true); }
        public override void OnHide() { Stage.SetActive(false); controller.OnScreenHidden(); }
        public override void OnBack() { controller.Pause(); }
    }
}
