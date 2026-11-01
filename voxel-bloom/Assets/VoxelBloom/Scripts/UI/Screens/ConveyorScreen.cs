using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VoxelBloom.Core;

namespace VoxelBloom
{
    /// <summary>
    /// GAMEPLAY view of the conveyor sort puzzle (premium violet/navy style of the reference mockup): HUD, the closed loop
    /// with its luminous cyan edges and direction chevrons, cubes, stations with trays / numbered locks / green plus
    /// slots, the tray queue and the boosters. All rules live in ConveyorSession; ConveyorController drives this view.
    /// </summary>
    public sealed class ConveyorScreen : ScreenBase
    {
        // palette of the reference: violet/navy panels, cyan conveyor edge
        public static readonly Color Bg1 = Theme.Hex("#2A2366"), Bg2 = Theme.Hex("#3C2F8F"), Frame = Theme.Hex("#221C52"),
            Track = Theme.Hex("#5A5396"), TrackDark = Theme.Hex("#4A4384"), Edge = Theme.Hex("#5BE7FF"), Floor = Theme.Hex("#2B2466"),
            TrayRim = Theme.Hex("#DAD2F7"), TrayInner = Theme.Hex("#383070"), Stone = Theme.Hex("#9396B8"), PlusTray = Theme.Hex("#8C7A52");

        public const float BoardWidth = 1020f;
        public RectTransform BoardRoot, Space, FlyLayer, QueueRoot, FxLayer;
        public float K; // UI px per board unit
        public ConveyorBoard Board;
        public VText LevelText, ProgressText, QueueMore, HintText, FeederText;
        public ProgressBar Progress;
        public UIButton PauseButton, GearButton;
        public readonly UIButton[] BoosterButtons = new UIButton[3];
        public readonly VText[] BoosterCounts = new VText[3];
        public static readonly BoosterType[] BoosterOrder = { BoosterType.Undo, BoosterType.Shuffle, BoosterType.ExtraStation };
        public StationView[] Stations;
        public readonly List<QueueCard> Cards = new List<QueueCard>();
        public RectTransform HintArrow;
        ConveyorController controller;
        float boardHeight, topY = 150f;

        public sealed class StationView
        {
            /// <summary>UI anchor at the projected tray centre (tray flights, sparkles).</summary>
            public RectTransform Root, Tray;
            public RectTransform LockTile, PlusTile;
            public VText LockText;
            public float HiddenUntil;
        }

        public sealed class QueueCard
        {
            public RectTransform Root;
            public UIButton Button;
            public Image[] Cells;
            public CanvasGroup Group;
        }

        protected override void OnBuild()
        {
            controller = gameObject.AddComponent<ConveyorController>();
            controller.Bind(this);
            UI.Sky(Root, Bg1, Bg2);
            BuildDecor();
            BuildHud();
            BuildBoosters();
        }

        // ------------------------------------------------------------------ static decor (airport interior hints)
        void BuildDecor()
        {
            foreach (float side in new[] { 0f, 1f })
            {
                var wall = UI.Image(Root, "Wall", SpriteFactory.Rounded(40), Theme.Hex("#45389A"));
                wall.rectTransform.anchorMin = new Vector2(side, 0.1f); wall.rectTransform.anchorMax = new Vector2(side, 0.92f);
                wall.rectTransform.sizeDelta = new Vector2(70, 0);
                wall.rectTransform.anchoredPosition = Vector2.zero;
                for (int i = 0; i < 4; i++)
                {
                    var win = UI.Image(wall.transform, "Window", SpriteFactory.Rounded(20), new Color(0.45f, 0.75f, 1f, 0.35f));
                    win.rectTransform.anchorMin = new Vector2(0.5f, 0.12f + i * 0.22f); win.rectTransform.anchorMax = win.rectTransform.anchorMin;
                    win.rectTransform.sizeDelta = new Vector2(34, 150);
                }
            }
            var floorGlow = UI.Image(Root, "FloorGlow", SpriteFactory.Glow(), new Color(0.55f, 0.45f, 1f, 0.25f), false);
            floorGlow.rectTransform.anchorMin = floorGlow.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            floorGlow.rectTransform.sizeDelta = new Vector2(1400, 600);
        }

        // ------------------------------------------------------------------ HUD: pause, level, progress c/t, settings
        void BuildHud()
        {
            var hud = UI.Rect(Root, "Hud").TopBand(20, 110, 30);
            PauseButton = UI.IconButton(hud, Icon.Pause, 100, ButtonStyle.Purple);
            ((RectTransform)PauseButton.transform).Place(new Vector2(0, 0.5f), new Vector2(50, 0), new Vector2(100, 100));
            PauseButton.OnClick = () => controller.Pause();
            GearButton = UI.IconButton(hud, Icon.Gear, 100, ButtonStyle.Purple);
            ((RectTransform)GearButton.transform).Place(new Vector2(1, 0.5f), new Vector2(-50, 0), new Vector2(100, 100));
            GearButton.OnClick = () => Router.Open<SettingsDialog>();

            var lv = UI.Panel(hud, "LevelPill", Theme.Hex("#4B3BB0"), 40, true, 4f);
            var lr = (RectTransform)lv.transform.parent;
            lr.Place(new Vector2(0.5f, 0.5f), new Vector2(-170, 0), new Vector2(290, 88));
            var rim = UI.Image(lr, "Rim", SpriteFactory.Rounded(44), Theme.Hex("#8E7BFF"));
            rim.rectTransform.Stretch(-4, -4, -4, -4); rim.transform.SetAsFirstSibling();
            LevelText = UI.Text(lr, "Nivel 1", 50, Color.white, TextAnchor.MiddleCenter, 0.15f, Theme.Hex("#22185E"));
            LevelText.supportRichText = true;
            LevelText.rectTransform.Stretch();

            var pg = UI.Panel(hud, "ProgressPill", Theme.Hex("#1E1848"), 40, true, 4f);
            var pr = (RectTransform)pg.transform.parent;
            pr.Place(new Vector2(0.5f, 0.5f), new Vector2(165, 0), new Vector2(330, 88));
            Progress = UI.Progress(pr, Theme.Hex("#45D85A"), 60);
            ((RectTransform)Progress.transform).Stretch(12, 12, 14, 14);
            UIArt.SkinProgress(Progress);
            ProgressText = UI.Text(pr, "0/0", 46, Color.white, TextAnchor.MiddleCenter, 0.2f, Theme.Hex("#0E1430"));
            ProgressText.rectTransform.Stretch();
        }

        void BuildBoosters()
        {
            var row = UI.Rect(Root, "Boosters").BottomBand(24, 170, 150);
            Icon[] icons = { Icon.Restart, Icon.Shuffle, Icon.CubePink };
            for (int i = 0; i < 3; i++)
            {
                var b = UI.IconButton(row, icons[i], 150, ButtonStyle.Purple);
                ((RectTransform)b.transform).Place(new Vector2((i + 0.5f) / 3f, 0.5f), Vector2.zero, new Vector2(150, 150));
                if (i == 2)
                {
                    var plus = UI.Icon(b.Face, Icon.Plus, 64);
                    plus.rectTransform.Place(new Vector2(1, 0), new Vector2(-34, 34), new Vector2(64, 64));
                }
                var t = BoosterOrder[i];
                b.Sound = Sfx.Booster;
                b.OnClick = () => controller.UseBooster(t);
                var ring = UI.Image(b.transform, "CountRing", SpriteFactory.Circle(), Color.white, false);
                ring.rectTransform.Place(new Vector2(1, 1), new Vector2(-12, -12), new Vector2(62, 62));
                var badge = UI.Image(ring.transform, "Count", SpriteFactory.Circle(), Theme.Hex("#E8202E"), false);
                badge.rectTransform.Stretch(5, 5, 5, 5);
                BoosterCounts[i] = UI.Text(badge.transform, "0", 34, Color.white, TextAnchor.MiddleCenter, 0.15f, Theme.Hex("#7A0A14"));
                BoosterCounts[i].fontStyle = FontStyle.Bold;
                BoosterCounts[i].rectTransform.Stretch();
                BoosterButtons[i] = b;
            }
        }

        // ------------------------------------------------------------------ per-level board (3D, rendered behind the UI)
        public VoxelStage Stage { get; private set; }
        public ConveyorBoard3D Board3D { get; private set; }
        RawImage boardImage;

        /// <summary>UI position (Space coordinates) of a board point at height y, projected through the 3D camera.</summary>
        public Vector2 ToUi(Vector2 g, float y = ConveyorBoard3D.TrackTop)
        {
            var vp = Board3D.Viewport(g, y);
            return new Vector2((vp.x - 0.5f) * BoardWidth, (vp.y - 0.5f) * boardHeight);
        }

        public Vector2 ToUi(Vector3 local)
        {
            var vp = Board3D.ViewportOfWorld(local);
            return new Vector2((vp.x - 0.5f) * BoardWidth, (vp.y - 0.5f) * boardHeight);
        }

        public void BuildBoard(ConveyorLevel level, Color[] colors)
        {
            if (BoardRoot != null) Destroy(BoardRoot.gameObject);
            if (QueueRoot != null) Destroy(QueueRoot.gameObject);
            if (Board3D != null) Destroy(Board3D.gameObject);
            Cards.Clear();
            Board = new ConveyorBoard(level);
            K = BoardWidth / (Board.W + 2f);
            boardHeight = (Board.H + 2f) * K;

            BoardRoot = UI.Rect(Root, "Board");
            BoardRoot.anchorMin = BoardRoot.anchorMax = new Vector2(0.5f, 1f);
            BoardRoot.pivot = new Vector2(0.5f, 1f);
            // centre the board + queue block between the HUD and the boosters (tall phones get even margins)
            float avail = Root.rect.height > 0 ? Root.rect.height - 150f - 220f : 0f;
            topY = 150f + Mathf.Max(0f, (avail - (boardHeight + 22f + 230f)) * 0.5f);
            BoardRoot.anchoredPosition = new Vector2(0, -topY);
            BoardRoot.sizeDelta = new Vector2(BoardWidth, boardHeight);

            if (Stage == null)
            {
                Stage = VoxelStage.Create("Conveyor", 64, false);
                Stage.Floating = true;            // no contact shadow / pedestal
                Stage.Model.gameObject.SetActive(false);
            }
            Stage.SetActive(true);
            Stage.UseTexture(1024, Mathf.RoundToInt(1024f * boardHeight / BoardWidth));
            Board3D = ConveyorBoard3D.Create(Stage, level, colors);
            var glow = UI.Image(BoardRoot, "EdgeGlow", SpriteFactory.Glow(), new Color(Edge.r, Edge.g, Edge.b, 0.10f), false);
            glow.rectTransform.Stretch(-80, -80, -80, -80);
            boardImage = UI.Raw(BoardRoot, "Board3D", Stage.Texture);
            boardImage.rectTransform.Stretch();
            Space = UI.Rect(BoardRoot, "Space");
            Space.anchorMin = Space.anchorMax = new Vector2(0.5f, 0.5f); Space.sizeDelta = Vector2.zero;

            // direction chevrons on the belt (projected overlay)
            int pathUnits = level.Slots * ConveyorSession.P;
            int chevrons = Mathf.Max(8, level.Slots / 6);
            for (int i = 0; i < chevrons; i++)
            {
                float sp = (i + 0.5f) * pathUnits / chevrons + ConveyorSession.P * 0.5f;
                var a = ToUi(Board.At(sp), ConveyorBoard3D.TrackTop + 0.01f);
                var b = ToUi(Board.At(sp + 20f), ConveyorBoard3D.TrackTop + 0.01f);
                var ch = UI.Icon(Space, Icon.ArrowRight, 0.42f * K);
                ch.color = new Color(Edge.r, Edge.g, Edge.b, 0.55f);
                ch.rectTransform.anchorMin = ch.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                ch.rectTransform.anchoredPosition = a;
                ch.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg);
                ch.transform.SetAsFirstSibling();
            }

            var fp = Board.At(level.Feeder);
            FeederText = UI.Text(Space, "", 0.6f * K, Color.white, TextAnchor.MiddleCenter, 0.2f, Theme.Hex("#22185E"));
            FeederText.rectTransform.anchorMin = FeederText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            FeederText.rectTransform.anchoredPosition = ToUi(fp + new Vector2(-0.75f, -0.75f), 0.95f);
            FeederText.rectTransform.sizeDelta = new Vector2(2.2f, 1f) * K;

            Stations = new StationView[level.Stations.Length];
            for (int i = 0; i < level.Stations.Length; i++) Stations[i] = BuildStation(level.Stations[i]);

            FxLayer = UI.Rect(Space, "Fx"); FxLayer.anchorMin = FxLayer.anchorMax = new Vector2(0.5f, 0.5f);
            FlyLayer = UI.Rect(Space, "Flying"); FlyLayer.anchorMin = FlyLayer.anchorMax = new Vector2(0.5f, 0.5f);

            HintText = UI.Text(Space, "", 40, Color.white, TextAnchor.MiddleCenter, 0.2f, Theme.Hex("#1A1450"));
            HintText.Wrap = true;
            HintText.rectTransform.anchorMin = HintText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            HintText.rectTransform.anchoredPosition = ToUi(new Vector2(Board.W * 0.62f, Board.H * 0.5f), ConveyorBoard3D.FloorTop);
            HintText.rectTransform.sizeDelta = new Vector2((Board.W - 7f) * K, 4f * K);

            BuildQueue(level);
        }

        StationView BuildStation(ConveyorStationDef st)
        {
            var v = new StationView();
            Board3D.Footprint(st, out var centre, out var size);
            var root = UI.Rect(Space, "Station"); root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
            root.anchoredPosition = ToUi(centre + new Vector3(0, ConveyorBoard3D.FloorTop + 0.3f, 0));
            v.Root = root; v.Tray = root;
            if (st.Lock == StationLock.Completed)
            {
                v.LockTile = UI.Rect(root, "LockNumber");
                v.LockTile.anchorMin = v.LockTile.anchorMax = new Vector2(0.5f, 0.5f);
                v.LockTile.anchoredPosition = ToUi(centre + new Vector3(0, ConveyorBoard3D.FloorTop + 0.53f, 0)) - root.anchoredPosition;
                v.LockTile.sizeDelta = Vector2.one * 1.1f * K;
                v.LockText = UI.Text(v.LockTile, st.LockN.ToString(), 0.8f * K, Color.white, TextAnchor.MiddleCenter, 0.1f, Theme.Hex("#14122A"));
                v.LockText.fontStyle = FontStyle.Bold;
                v.LockText.rectTransform.Stretch();
            }
            else if (st.Lock == StationLock.Booster)
            {
                // invisible tap target over the 3D plus slot
                var a = ToUi(centre - size * 0.5f); var b = ToUi(centre + size * 0.5f);
                var hit = UI.Image(root, "PlusTap", null, new Color(1, 1, 1, 0), false, true);
                hit.rectTransform.anchorMin = hit.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                hit.rectTransform.anchoredPosition = (a + b) * 0.5f - root.anchoredPosition;
                hit.rectTransform.sizeDelta = new Vector2(Mathf.Abs(b.x - a.x), Mathf.Abs(b.y - a.y));
                var btn = hit.gameObject.AddComponent<UIButton>(); btn.Face = hit.rectTransform;
                btn.OnClick = () => controller.UseBooster(BoosterType.ExtraStation);
                v.PlusTile = hit.rectTransform;
            }
            return v;
        }

        void BuildQueue(ConveyorLevel level)
        {
            QueueRoot = UI.Rect(Root, "Queue");
            QueueRoot.anchorMin = QueueRoot.anchorMax = new Vector2(0.5f, 1f);
            QueueRoot.pivot = new Vector2(0.5f, 1f);
            QueueRoot.anchoredPosition = new Vector2(0, -topY - boardHeight - 22);
            QueueRoot.sizeDelta = new Vector2(BoardWidth, 230);
            int n = Mathf.Max(1, level.Visible);
            float slotW = BoardWidth / 7f;
            float cw = Mathf.Min(132f, slotW - 14f), chh = 214f, cell = cw - 34f;
            float x0 = -(n - 1) * slotW / 2f;
            for (int i = 0; i < n; i++)
            {
                var card = new QueueCard();
                var b = UI.Button(QueueRoot, null, ButtonStyle.Lavender, new Vector2(cw, chh));
                card.Button = b; card.Root = (RectTransform)b.transform;
                card.Root.Place(new Vector2(0.5f, 0.5f), new Vector2(x0 + i * slotW, 0), new Vector2(cw, chh));
                var face = b.Face.GetComponent<Image>();
                face.color = TrayRim;
                var inner = UI.Image(b.Face, "Inner", SpriteFactory.Rounded(24), TrayInner);
                inner.rectTransform.Stretch(9, 9, 9, 9);
                card.Cells = new Image[4];
                for (int r = 0; r < 4; r++)
                {
                    var c = UI.Image(inner.transform, "Cell", SpriteFactory.Glossy(18), Color.clear, true);
                    c.rectTransform.anchorMin = c.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                    c.rectTransform.sizeDelta = new Vector2(cell, (chh - 40f) / 4f - 6f);
                    c.rectTransform.anchoredPosition = new Vector2(0, -12 - ((chh - 40f) / 4f) * (r + 0.5f));
                    card.Cells[r] = c;
                }
                card.Group = card.Root.gameObject.AddComponent<CanvasGroup>();
                int idx = i;
                b.Sound = Sfx.Select;
                b.OnClick = () => controller.OnTrayTapped(idx);
                Cards.Add(card);
            }
            QueueMore = UI.Text(QueueRoot, "", 34, new Color(1, 1, 1, 0.75f), TextAnchor.MiddleRight);
            QueueMore.rectTransform.Place(new Vector2(1, 1), new Vector2(0, 26), new Vector2(400, 40), new Vector2(1, 0.5f));
            var arrow = UI.Icon(QueueRoot, Icon.ArrowRight, 80);
            HintArrow = arrow.rectTransform;
            HintArrow.localRotation = Quaternion.Euler(0, 0, -90);
            arrow.color = Theme.Gold;
            arrow.gameObject.SetActive(false);
        }

        /// <summary>Centre of a queue card in Space coordinates (for the tray flight to its station).</summary>
        public Vector2 CardInSpace(int i) => Space.InverseTransformPoint(Cards[Mathf.Clamp(i, 0, Cards.Count - 1)].Root.position);

        public override void OnShow() { if (Stage != null) Stage.SetActive(true); }
        public override void OnHide() { if (Stage != null) Stage.SetActive(false); controller.OnScreenHidden(); }
        public override void OnBack() { controller.Pause(); }
    }
}
