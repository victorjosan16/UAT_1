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

        /// <summary>Blocks whose colour has no reachable voxel right now are dimmed.</summary>
        public void SetPlayable(bool playable)
        {
            var cg = Root.GetComponent<CanvasGroup>();
            if (cg == null) cg = Root.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = playable ? 1f : 0.5f;
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
            Label.color = lum > 0.8f ? Theme.Ink : Color.white;
            Label.outlineColor = lum > 0.8f ? new Color32(255, 255, 255, 255) : (Color32)Theme.Darker(c, 0.45f);
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
        public readonly BlockView[] Board = new BlockView[GameSession.BoardSize];
        public readonly BlockView[] QueueBlocks = new BlockView[5];
        public readonly RectTransform[] QueueSlots = new RectTransform[5];
        public readonly VText[] BoosterCounts = new VText[4];
        public readonly UIButton[] BoosterButtons = new UIButton[4];
        public VText LevelText, CoinsText, MovesText, ProgressText, RemainingText, HintText;
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
            UI.Sky(Root, Theme.GameSkyTop, Theme.SkyBottom);
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
            MovesText = UI.Pill(hud, Icon.Lightning, "0", out var mr, 200, 76);
            mr.Place(new Vector2(1, 0.5f), new Vector2(-300 - 110, 0), new Vector2(200, 76));
            CoinsText = UI.Pill(hud, Icon.Coin, "0", out var cr, 260, 76);
            cr.Place(new Vector2(1, 0.5f), new Vector2(-130, 0), new Vector2(260, 76));
        }

        void BuildModelArea()
        {
            var panel = UI.Panel(Root, "ModelPanel", Color.white, 56);
            ModelPanel = (RectTransform)panel.transform.parent;
            ModelPanel.anchorMin = Vector2.zero; ModelPanel.anchorMax = Vector2.one;
            ModelPanel.offsetMin = new Vector2(40, 1080); ModelPanel.offsetMax = new Vector2(-40, -150);
            panel.sprite = SpriteFactory.Rounded(56);
            var sky = UI.Image(panel.transform, "Sky", SpriteFactory.VerticalGradient(Theme.Hex("#6FC2FF"), Theme.Hex("#CFEFFF")), Color.white, false);
            sky.rectTransform.Stretch(10, 10, 10, 10);
            var mask = UI.Image(panel.transform, "Mask", SpriteFactory.Rounded(48), Color.white);
            mask.rectTransform.Stretch(10, 10, 10, 10);
            mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            sky.transform.SetParent(mask.transform, true);

            Stage = VoxelStage.Create("Gameplay", 1024, true);
            var raw = UI.Raw(mask.transform, "Model", Stage.Texture);
            raw.rectTransform.Stretch();
            var arf = raw.gameObject.AddComponent<AspectRatioFitter>();
            arf.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            arf.aspectRatio = 1f;
            raw.raycastTarget = true;
            ModelInput = raw.gameObject.AddComponent<ModelInput>();
            ModelInput.Stage = Stage;

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

            var prog = UI.Rect(Root, "ProgressRow").BottomBand(1000, 70, 60);
            var cube = UI.Icon(prog, Icon.Cube, 70);
            cube.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(35, 0), new Vector2(70, 70));
            Progress = UI.Progress(prog, Theme.Green, 44);
            ((RectTransform)Progress.transform).Stretch(90, 150, 13, 13);
            ProgressText = UI.Text(prog, "0%", 42, Theme.Ink, TextAnchor.MiddleRight);
            ProgressText.rectTransform.Place(new Vector2(1, 0.5f), new Vector2(0, 0), new Vector2(140, 60), new Vector2(1, 0.5f));
        }

        void BuildQueue()
        {
            var row = UI.Rect(Root, "Queue").BottomBand(845, 150, 70);
            float slot = 150f, gap = (940f - 5 * slot) / 4f;
            for (int i = 0; i < 5; i++)
            {
                var s = UI.Image(row, "Slot" + i, SpriteFactory.Rounded(32), Theme.SlotEmpty);
                QueueSlots[i] = s.rectTransform;
                QueueSlots[i].Place(new Vector2(0, 0.5f), new Vector2(slot / 2 + i * (slot + gap), 0), new Vector2(slot, slot));
                var inner = UI.Image(s.transform, "Inner", SpriteFactory.Rounded(26), new Color(1, 1, 1, 0.35f));
                inner.rectTransform.Stretch(8, 8, 8, 14);
                var qb = BlockView.Create(s.transform, new Vector2(slot - 10, slot - 10), "Queued" + i);
                qb.Root.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(slot - 10, slot - 10));
                qb.Button.Interactable = true;
                int qi = i;
                qb.Button.OnClick = () => controller.OnQueueTapped(qi);
                qb.Button.Sound = Sfx.Button;
                QueueBlocks[i] = qb;
            }
            RemainingText = UI.Text(Root, "", 38, Theme.InkSoft);
            RemainingText.rectTransform.BottomBand(790, 50, 60);
        }

        void BuildBoard()
        {
            var panel = UI.Panel(Root, "BoardPanel", new Color(1, 1, 1, 0.55f), 48, false);
            BoardRoot = (RectTransform)panel.transform.parent;
            BoardRoot.BottomBand(190, 4 * 130 + 3 * 12 + 40, 40);
            float w = (1000f - 40f - 4 * 16f) / 5f, h = 130f;
            for (int s = 0; s < GameSession.BoardSize; s++)
            {
                int col = s % GameSession.BoardCols, row = s / GameSession.BoardCols;
                var bv = BlockView.Create(BoardRoot, new Vector2(w, h), "Block" + s);
                bv.Root.Place(new Vector2(0, 1), new Vector2(20 + w / 2 + col * (w + 16), -20 - h / 2 - row * (h + 12)), new Vector2(w, h));
                int slot = s;
                bv.Button.OnClick = () => controller.OnBlockTapped(slot);
                bv.Button.OnPress = () => controller.PreviewBlock(slot);
                bv.Button.OnRelease = () => controller.ClearPreview();
                Board[s] = bv;
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
