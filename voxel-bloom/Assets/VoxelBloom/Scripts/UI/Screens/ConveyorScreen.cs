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

        public const float MaxBoardWidth = 1020f;
        /// <summary>Board width in UI units: 1020, or less when a tall (large) board must fit between the HUD and the queue.</summary>
        public float BoardWidth = MaxBoardWidth;
        public static readonly Color[] TierColors = { Theme.Hex("#3FCB4C"), Theme.Hex("#6C5CE7"), Theme.Hex("#FF9A2E"), Theme.Hex("#FF4D5E") };
        public RectTransform BoardRoot, Space, CubeLayer, FlyLayer, QueueRoot, FxLayer;
        public float K; // UI px per board unit
        public ConveyorBoard Board;
        public VText LevelText, ProgressText, QueueMore, HintText, FeederText;
        public ProgressBar Progress;
        public UIButton PauseButton, GearButton;
        public readonly UIButton[] BoosterButtons = new UIButton[3];
        public readonly VText[] BoosterCounts = new VText[3];
        public static readonly BoosterType[] BoosterOrder = { BoosterType.Shuffle, BoosterType.ColorShuffle, BoosterType.ExtraStation };
        public Image[] SlotCubes;
        public StationView[] Stations;
        public readonly List<QueueCard> Cards = new List<QueueCard>();
        public RectTransform HintArrow;
        ConveyorController controller;
        float boardHeight, topY = 150f;

        public sealed class StationView
        {
            public RectTransform Root, Tray;
            public Image Rim, Inner;
            public Image[] Cells;
            public RectTransform LockTile, PlusTile;
            public VText LockText;
            public CanvasGroup Group;
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
            GearButton.OnClick = () => Router.Open<SettingsVioletDialog>();

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
            Icon[] icons = { Icon.Refresh, Icon.ColorSwap, Icon.CubesPlus };
            for (int i = 0; i < 3; i++)
            {
                string art = new[] { "Boosters/refresh", "Boosters/color_swap", "Boosters/add_tray" }[i];
                bool hasArt = UIArt.Has(art);
                var b = hasArt ? UI.Button(row, null, ButtonStyle.Purple, new Vector2(170, 165)) : UI.IconButton(row, icons[i], 150, ButtonStyle.Purple);
                ((RectTransform)b.transform).Place(new Vector2((i + 0.5f) / 3f, 0.5f), Vector2.zero, hasArt ? new Vector2(170, 165) : new Vector2(150, 150));
                if (hasArt) UIArt.Skin(b, art); // the painted "3" badge is covered by the live count below
                var t = BoosterOrder[i];
                b.Sound = Sfx.Booster;
                b.OnClick = () => controller.UseBooster(t);
                var ring = UI.Image(b.transform, "CountRing", SpriteFactory.Circle(), Color.white, false);
                ring.rectTransform.Place(new Vector2(1, 1), new Vector2(-33, -30), new Vector2(66, 66)); // exactly over the painted badge
                var badge = UI.Image(ring.transform, "Count", SpriteFactory.Circle(), Theme.Hex("#E8202E"), false);
                badge.rectTransform.Stretch(5, 5, 5, 5);
                BoosterCounts[i] = UI.Text(badge.transform, "0", 34, Color.white, TextAnchor.MiddleCenter, 0.15f, Theme.Hex("#7A0A14"));
                BoosterCounts[i].fontStyle = FontStyle.Bold;
                BoosterCounts[i].rectTransform.Stretch();
                BoosterButtons[i] = b;
            }
        }

        // ------------------------------------------------------------------ per-level board
        public Vector2 ToUi(Vector2 g) => new Vector2((g.x + 1f) * K - BoardWidth / 2f, boardHeight / 2f - (g.y + 1f) * K);

        Image RoundRect(Transform parent, string name, Vector2 centreG, Vector2 sizeG, float radiusG, Color color)
        {
            var img = UI.Image(parent, name, SpriteFactory.Rounded(64), color, true);
            img.pixelsPerUnitMultiplier = 64f / Mathf.Max(1f, radiusG * K);
            img.rectTransform.anchorMin = img.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            img.rectTransform.anchoredPosition = ToUi(centreG);
            img.rectTransform.sizeDelta = sizeG * K;
            return img;
        }

        public void BuildBoard(ConveyorLevel level)
        {
            if (BoardRoot != null) Destroy(BoardRoot.gameObject);
            if (QueueRoot != null) Destroy(QueueRoot.gameObject);
            Cards.Clear();
            Board = new ConveyorBoard(level);
            float maxH = Root.rect.height > 0 ? Root.rect.height - 150f - 220f - 252f : 1300f;
            BoardWidth = Mathf.Min(MaxBoardWidth, maxH * (Board.W + 2f) / (Board.H + 2f));
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
            Space = UI.Rect(BoardRoot, "Space");
            Space.anchorMin = Space.anchorMax = new Vector2(0.5f, 0.5f); Space.sizeDelta = Vector2.zero;

            float W = Board.W, H = Board.H, R = Board.R;
            var mid = new Vector2(W / 2f, H / 2f);
            var sh = UI.Image(Space, "Shadow", SpriteFactory.Shadow(48), new Color(0.05f, 0.02f, 0.2f, 0.45f));
            sh.rectTransform.anchoredPosition = ToUi(mid) + new Vector2(0, -16); sh.rectTransform.sizeDelta = new Vector2(W + 3f, H + 3f) * K;
            RoundRect(Space, "Frame", mid, new Vector2(W + 2f, H + 2f), R + 1f, Frame);
            RoundRect(Space, "OuterEdge", mid, new Vector2(W + 1.32f, H + 1.32f), R + 0.66f, Edge);
            RoundRect(Space, "Track", mid, new Vector2(W + 1.16f, H + 1.16f), R + 0.58f, Track);
            RoundRect(Space, "TrackShade", mid, new Vector2(W + 0.2f, H + 0.2f), R + 0.1f, TrackDark).color = new Color(TrackDark.r, TrackDark.g, TrackDark.b, 0.35f);
            RoundRect(Space, "InnerEdge", mid, new Vector2(W - 0.84f, H - 0.84f), R - 0.42f, Edge);
            RoundRect(Space, "Floor", mid, new Vector2(W - 1.0f, H - 1.0f), Mathf.Max(0.3f, R - 0.5f), Floor);
            var glow = UI.Image(Space, "EdgeGlow", SpriteFactory.Glow(), new Color(Edge.r, Edge.g, Edge.b, 0.10f), false);
            glow.rectTransform.anchoredPosition = ToUi(mid); glow.rectTransform.sizeDelta = new Vector2(W + 4f, H + 4f) * K;
            glow.transform.SetAsFirstSibling();

            // direction chevrons along the track
            int pathUnits = level.Slots * ConveyorSession.P;
            int chevrons = Mathf.Max(8, level.Slots / 6);
            for (int i = 0; i < chevrons; i++)
            {
                float s = (i + 0.5f) * pathUnits / chevrons;
                var t = Board.Tangent(s);
                var ch = UI.Icon(Space, Icon.ArrowRight, 0.55f * K);
                ch.color = new Color(Edge.r, Edge.g, Edge.b, 0.75f);
                ch.rectTransform.anchorMin = ch.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                ch.rectTransform.anchoredPosition = ToUi(Board.At(s));
                ch.rectTransform.localRotation = Quaternion.Euler(0, 0, -Mathf.Atan2(t.y, t.x) * Mathf.Rad2Deg);
            }

            // feeder chute (top-left corner, outside the loop): where new cubes enter
            var fp = Board.At(level.Feeder);
            var chute = RoundRect(Space, "Feeder", fp + new Vector2(-0.55f, -0.55f), new Vector2(1.5f, 1.5f), 0.4f, Theme.Hex("#6B5FC2"));
            chute.transform.localRotation = Quaternion.Euler(0, 0, 45);
            FeederText = UI.Text(Space, "", 0.6f * K, Color.white, TextAnchor.MiddleCenter, 0.2f, Theme.Hex("#22185E"));
            FeederText.rectTransform.anchorMin = FeederText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            FeederText.rectTransform.anchoredPosition = ToUi(fp + new Vector2(-0.55f, -0.55f));
            FeederText.rectTransform.sizeDelta = new Vector2(2.2f, 1f) * K;

            // stations
            Stations = new StationView[level.Stations.Length];
            for (int i = 0; i < level.Stations.Length; i++) Stations[i] = BuildStation(level.Stations[i]);

            // cubes on the belt (one image per slot), flights and effects above
            CubeLayer = UI.Rect(Space, "Cubes"); CubeLayer.anchorMin = CubeLayer.anchorMax = new Vector2(0.5f, 0.5f);
            Isolate(CubeLayer);
            SlotCubes = new Image[level.Slots];
            for (int k = 0; k < level.Slots; k++)
            {
                var c = UI.Image(CubeLayer, "Cube", SpriteFactory.Glossy(18), Color.white, true);
                c.pixelsPerUnitMultiplier = 18f / (0.2f * K);
                c.rectTransform.anchorMin = c.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                c.rectTransform.sizeDelta = Vector2.one * 0.8f * K;
                SlotCubes[k] = c;
            }
            FlyLayer = UI.Rect(Space, "Flying"); FlyLayer.anchorMin = FlyLayer.anchorMax = new Vector2(0.5f, 0.5f);
            Isolate(FlyLayer);
            FxLayer = UI.Rect(Space, "Fx"); FxLayer.anchorMin = FxLayer.anchorMax = new Vector2(0.5f, 0.5f);
            Isolate(FxLayer);

            HintText = UI.Text(Space, "", 40, Color.white, TextAnchor.MiddleCenter, 0.2f, Theme.Hex("#1A1450"));
            HintText.Wrap = true;
            HintText.rectTransform.anchorMin = HintText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            HintText.rectTransform.anchoredPosition = ToUi(new Vector2(W * 0.62f, H * 0.5f));
            HintText.rectTransform.sizeDelta = new Vector2((W - 7f) * K, 4f * K);

            BuildQueue(level);

            // difficulty badge on the top edge of the board (not shown for Normal)
            if (level.Tier != 1)
            {
                var badge = UI.Image(BoardRoot, "Tier", SpriteFactory.Rounded(30), TierColors[level.Tier]);
                badge.rectTransform.anchorMin = badge.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                badge.rectTransform.anchoredPosition = new Vector2(0, 6);
                badge.rectTransform.sizeDelta = new Vector2(level.Tier == 3 ? 300 : 200, 62);
                var t = UI.Text(badge.transform, level.TierName.ToUpperInvariant(), 36, Color.white, TextAnchor.MiddleCenter, 0.2f, Theme.Darker(TierColors[level.Tier], 0.5f));
                t.fontStyle = FontStyle.Bold;
                t.rectTransform.Stretch();
            }
        }

        /// <summary>
        /// Performance: a nested Canvas so elements that change every frame (moving cubes, flights, effects, tray cells)
        /// only rebuild their own small batch instead of the whole screen canvas.
        /// </summary>
        static void Isolate(RectTransform rt)
        {
            var c = rt.gameObject.AddComponent<Canvas>();
            c.pixelPerfect = false;
        }

        StationView BuildStation(ConveyorStationDef st)
        {
            var v = new StationView();
            var c0 = Board.Compartment(st, 0); var cN = Board.Compartment(st, st.Capacity - 1);
            var centre = (c0 + cN) / 2f;
            bool vertical = ConveyorBoard.IsVertical(st);
            float len = st.Capacity * ConveyorBoard.Pitch + 0.24f;
            var size = vertical ? new Vector2(ConveyorBoard.TrayWidth, len) : new Vector2(len, ConveyorBoard.TrayWidth);
            var root = UI.Rect(Space, "Station"); root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
            v.Root = root;
            v.Group = root.gameObject.AddComponent<CanvasGroup>();
            Isolate(root); // compartment colours change often: rebuild only this tray
            v.Rim = RoundRect(root, "Rim", centre, size, 0.3f, TrayRim);
            v.Inner = RoundRect(root, "Inner", centre, size - new Vector2(0.2f, 0.2f), 0.24f, TrayInner);
            v.Tray = v.Rim.rectTransform;
            v.Cells = new Image[st.Capacity];
            for (int i = 0; i < st.Capacity; i++)
            {
                var cell = RoundRect(root, "Cell", Board.Compartment(st, i), new Vector2(ConveyorBoard.Cell, ConveyorBoard.Cell), 0.3f, Color.clear);
                cell.sprite = SpriteFactory.Glossy(18); cell.pixelsPerUnitMultiplier = 18f / (0.3f * K);
                v.Cells[i] = cell;
            }
            if (st.Lock == StationLock.Completed)
            {
                var tile = RoundRect(root, "Lock", centre, size, 0.3f, Stone);
                RoundRect(tile.transform, "Top", centre + new Vector2(0, -0.04f), size - new Vector2(0.18f, 0.22f), 0.24f, Theme.Lighter(Stone, 0.25f));
                v.LockTile = tile.rectTransform;
                var badge = RoundRect(tile.transform, "Badge", centre, new Vector2(1.2f, 1.2f), 0.3f, Theme.Hex("#2C2A4A"));
                v.LockText = UI.Text(badge.transform, st.LockN.ToString(), 0.85f * K, Color.white);
                v.LockText.fontStyle = FontStyle.Bold;
                v.LockText.rectTransform.Stretch();
            }
            else if (st.Lock == StationLock.Booster)
            {
                var tile = RoundRect(root, "Plus", centre, size, 0.3f, Theme.Lighter(PlusTray, 0.3f));
                RoundRect(tile.transform, "Inner", centre, size - new Vector2(0.16f, 0.16f), 0.24f, PlusTray);
                v.PlusTile = tile.rectTransform;
                var plus = UI.Icon(tile.transform, Icon.Plus, 1.2f * K);
                plus.rectTransform.anchorMin = plus.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                plus.rectTransform.anchoredPosition = Vector2.zero;
                tile.raycastTarget = true;
                var btn = tile.gameObject.AddComponent<UIButton>(); btn.Face = tile.rectTransform;
                btn.OnClick = () => controller.UseBooster(BoosterType.ExtraStation);
            }
            return v;
        }

        void BuildQueue(ConveyorLevel level)
        {
            QueueRoot = UI.Rect(Root, "Queue");
            QueueRoot.anchorMin = QueueRoot.anchorMax = new Vector2(0.5f, 1f);
            QueueRoot.pivot = new Vector2(0.5f, 1f);
            QueueRoot.anchoredPosition = new Vector2(0, -topY - boardHeight - 22);
            QueueRoot.sizeDelta = new Vector2(MaxBoardWidth, 230);
            int n = Mathf.Max(1, level.Visible);
            float slotW = MaxBoardWidth / 7f;
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

        public override void OnShow() { }
        public override void OnHide() { controller.OnScreenHidden(); }
        public override void OnBack() { controller.Pause(); }
    }
}
