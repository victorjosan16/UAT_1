using System;
using System.Collections;
using System.Collections.Generic;
using QuarryEmpire;
using UnityEngine;
using UnityEngine.UI;

namespace SugarDrop
{
    /// <summary>
    /// Harta nivelurilor (2.5D): un drum curbat, luminos, care urcă printre nori de glazură și bomboane, cu noduri
    /// lucioase. Nivelurile trecute au culoarea aromei și stele, cel curent e auriu, plutește și are un halou,
    /// cele viitoare sunt estompate și încuiate. Harta se derulează cu degetul, se centrează singură pe nivelul
    /// curent, iar după o victorie drumul se aprinde până la nivelul nou, care se deblochează animat.
    /// Straturile de fundal se mișcă mai încet decât drumul (paralaxă). Sus: vieți, monede, setări; jos: „Joacă”.
    /// </summary>
    public class MapScreen : MonoBehaviour
    {
        private const float DotSpacing = 46f;
        private const float NodeSize = 170f;
        private const float BossNodeSize = 210f;

        public event Action PlayClicked;
        public event Action SettingsClicked;
        public event Action LeaderboardClicked;
        public event Action GiftClicked;
        public event Action MissionsClicked;
        /// <summary>Un nivel deblocat a fost atins (pentru rejucare sau pentru nivelul curent).</summary>
        public event Action<int> LevelClicked;

        private class NodeView
        {
            public int Level;
            public RectTransform Root;
            public RectTransform Visual;
            public Image Face;
            public Image Glow;
            public Image Ring;
            public UiLabel Number;
            public RectTransform Lock;
            public UiLabel Boss;
            public Image Portrait;
            public Image[] Stars;
            public Image Sparkle;
            public LevelNodeAnimator Animator;
        }

        private static readonly Color LockedColor = new(0.8f, 0.76f, 0.87f);
        private static readonly Color DotOff = new(1f, 1f, 1f, 0.6f);
        private static readonly Color DotOn = new(1f, 0.84f, 0.36f);
        private static readonly Color[] UnlockColors = { Color.white, new(1f, 0.84f, 0.36f), new(1f, 0.56f, 0.69f), new(0.71f, 0.48f, 1f) };

        private GameObject _root;
        private Image _background;
        private int _backgroundChapter = -1;
        private readonly List<(int Chapter, UiLabel Label)> _chapterLabels = new();
        private RectTransform _content;
        private RectTransform _view;
        private RectTransform _scrollContent;
        private LevelMapScrollController _scroller;
        private RectTransform _farLayer, _midLayer;
        private RectTransform _roadLayer, _dotLayer, _nodeLayer;
        private UIParticleController _bursts;
        private RectTransform _marker;
        private RectTransform _playGlow;
        private UiLabel _lives, _lifeTimer, _coins, _playLabel;
        private GameObject _giftBadge, _missionsBadge;

        private readonly List<NodeView> _nodes = new();
        private readonly List<Image> _dots = new();
        private readonly List<int> _dotSegment = new();
        private int _levels;
        private int _shownLevel;
        private int _litLevel;
        private float _dotsPopUntil;
        private float _refresh;
        private Vector2 _markerTarget;
        private Coroutine _unlock;

        public bool IsOpen => _root.activeSelf;

        // ------------------------------------------------------------ Construire

        private void Awake()
        {
            UiTheme theme = UiRoot.Theme;
            Image background = UiFactory.CreateImage("Background", transform, SugarSprites.BackgroundGradient, Color.white);
            background.preserveAspect = false;
            background.raycastTarget = true;
            UiFactory.Stretch(background.rectTransform, -300f);
            _root = background.gameObject;
            _background = background;
            // Canvas propriu: animațiile hărții nu reconstruiesc restul interfeței (și invers).
            _root.AddComponent<Canvas>();
            _root.AddComponent<GraphicRaycaster>();

            // Fundalul depășește ecranul cu 300 px pe fiecare parte (acoperă marginile);
            // conținutul stă într-un dreptunghi exact cât ecranul.
            _content = UiFactory.CreateRect("Content", _root.transform);
            UiFactory.Stretch(_content, 300f);

            // Straturi de adâncime: departe (lumini moi), mijloc (nori și bomboane), sclipiri ambientale.
            _farLayer = Layer("FarLayer", _content);
            _midLayer = Layer("MidLayer", _content);
            UIParticleController ambient = Layer("Ambient", _content).gameObject.AddComponent<UIParticleController>();
            ambient.Initialize(36, SugarSprites.Sparkle, new[] { new Color(1f, 1f, 1f, 0.9f), new Color(1f, 0.9f, 0.6f, 0.9f), new Color(1f, 0.75f, 0.85f, 0.9f) }, 3f);
            ambient.SetAmbientSize(14f, 30f);

            BuildScroll();

            _bursts = Layer("Bursts", _content).gameObject.AddComponent<UIParticleController>();
            _bursts.Initialize(40, SugarSprites.Sparkle, null, 0f);

            BuildTopBar(theme);
            BuildPlayButton(theme);

            _scroller.SetParallaxLayers(new[] { _farLayer, _midLayer }, new[] { 0.25f, 0.6f });
            _root.SetActive(false);
        }

        private static RectTransform Layer(string name, Transform parent)
        {
            RectTransform rect = UiFactory.CreateRect(name, parent);
            UiFactory.Stretch(rect);
            return rect;
        }

        private void BuildScroll()
        {
            _view = Layer("Scroll", _content);
            // Suprafață invizibilă care primește tragerea cu degetul (nu se desenează).
            var hit = _view.gameObject.AddComponent<Image>();
            hit.color = new Color(1f, 1f, 1f, 0f);
            hit.raycastTarget = true;
            hit.canvasRenderer.cullTransparentMesh = true;

            var scroll = _view.gameObject.AddComponent<ScrollRect>();
            _scrollContent = UiFactory.CreateRect("Path", _view);
            _scrollContent.anchorMin = new Vector2(0f, 0f);
            _scrollContent.anchorMax = new Vector2(1f, 0f);
            _scrollContent.pivot = new Vector2(0.5f, 0f);
            _scrollContent.anchoredPosition = Vector2.zero;
            _scrollContent.sizeDelta = new Vector2(0f, MapLayout.ContentHeight(1));
            scroll.content = _scrollContent;
            scroll.viewport = _view;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.elasticity = 0.12f;
            scroll.inertia = true;
            scroll.decelerationRate = 0.12f;
            scroll.scrollSensitivity = 30f;
            _scroller = _view.gameObject.AddComponent<LevelMapScrollController>();

            _roadLayer = Layer("Road", _scrollContent);
            _dotLayer = Layer("Dots", _scrollContent);
            _nodeLayer = Layer("Nodes", _scrollContent);
            BuildMarker();
        }

        /// <summary>Bobo în miniatură, „ești aici”, deasupra nivelului curent.</summary>
        private void BuildMarker()
        {
            _marker = UiFactory.CreateRect("Marker", _scrollContent);
            UiFactory.Place(_marker, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(110f, 120f));
            Image shadow = UiFactory.CreateImage("Shadow", _marker, SugarSprites.SoftDot, new Color(0.35f, 0.1f, 0.3f, 0.3f));
            UiFactory.Place(shadow.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(90f, 26f));
            shadow.preserveAspect = false;
            Image cup = UiFactory.CreateImage("Cup", _marker, SugarSprites.CupWrapper, Color.white);
            UiFactory.Place(cup.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(96f, 96f));
            Image frosting = UiFactory.CreateImage("Frosting", _marker, SugarSprites.Frosting, Color.white);
            UiFactory.Place(frosting.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 46f), new Vector2(104f, 104f));
            Image cherry = UiFactory.CreateImage("Cherry", _marker, SugarSprites.Circle, new Color(0.93f, 0.2f, 0.32f));
            UiFactory.Place(cherry.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(4f, 136f), new Vector2(26f, 26f));
            for (int e = -1; e <= 1; e += 2)
            {
                Image eye = UiFactory.CreateImage("Eye", _marker, SugarSprites.Circle, new Color(0.25f, 0.12f, 0.2f));
                UiFactory.Place(eye.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(e * 17f, 40f), new Vector2(14f, 14f));
            }
        }

        private void BuildTopBar(UiTheme theme)
        {
            RectTransform bar = UiFactory.CreateRect("TopBar", _content);
            UiFactory.Place(bar, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(1080f, 160f));

            RectTransform livesPill = Pill("LivesPill", bar, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(84f, -30f), new Vector2(290f, 100f), theme);
            Image heart = UiFactory.CreateImage("Heart", livesPill, SugarSprites.LifeHeart, Color.white);
            UiFactory.Place(heart.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(120f, 120f));
            _lives = SugarUi.Outlined("Count", heart.transform, "5", 46, Color.white);
            UiFactory.Stretch(_lives.RectTransform);
            _lifeTimer = UiFactory.CreateText("Timer", livesPill, "", 38);
            UiFactory.Place(_lifeTimer.RectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(70f, 0f), new Vector2(210f, 80f));

            RectTransform coinsPill = Pill("CoinsPill", bar, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-150f, -30f), new Vector2(270f, 100f), theme);
            Image coin = UiFactory.CreateImage("Coin", coinsPill, ProceduralSprites.Coin, Color.white);
            UiFactory.Place(coin.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(96f, 96f));
            _coins = UiFactory.CreateText("Count", coinsPill, "0", 42);
            UiFactory.Place(_coins.RectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(60f, 0f), new Vector2(200f, 80f));

            Button settings = SugarUi.IconButton("Settings", bar, SugarSprites.Gear, theme.PanelColor, () =>
            {
                SugarUi.Click();
                SettingsClicked?.Invoke();
            }, 0.6f);
            UiFactory.Place((RectTransform)settings.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -28f), new Vector2(108f, 108f));

            Button leaderboard = SugarUi.IconButton("Leaderboard", bar, SugarSprites.Trophy, theme.AccentColor, () =>
            {
                SugarUi.Click();
                LeaderboardClicked?.Invoke();
            }, 0.62f);
            UiFactory.Place((RectTransform)leaderboard.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -160f), new Vector2(108f, 108f));

            // Stânga, sub vieți: cadoul zilnic și misiunile (cu un punct roșu când e ceva de revendicat).
            _giftBadge = SideButton("Gift", bar, SugarSprites.Gift, Palette.Tile(0), new Vector2(24f, -160f), () => GiftClicked?.Invoke());
            _missionsBadge = SideButton("Missions", bar, SugarSprites.Checklist, Palette.Tile(1), new Vector2(24f, -292f), () => MissionsClicked?.Invoke());
        }

        private static GameObject SideButton(string name, RectTransform bar, Sprite icon, Color color, Vector2 position, Action onClick)
        {
            Button button = SugarUi.IconButton(name, bar, icon, color, () =>
            {
                SugarUi.Click();
                onClick();
            }, 0.6f);
            UiFactory.Place((RectTransform)button.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), position, new Vector2(108f, 108f));
            Image badge = UiFactory.CreateImage("Badge", button.transform, SugarSprites.Circle, UiRoot.Theme.NegativeColor);
            UiFactory.Place(badge.rectTransform, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-14f, -14f), new Vector2(34f, 34f));
            badge.gameObject.SetActive(false);
            return badge.gameObject;
        }

        private static RectTransform Pill(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size, UiTheme theme)
        {
            Image shadow = UiFactory.CreatePanel(name + "Shadow", parent, new Color(0.3f, 0.08f, 0.25f, 0.3f));
            UiFactory.Place((RectTransform)shadow.transform, anchor, pivot, position + new Vector2(0f, -8f), size);
            Image pill = UiFactory.CreatePanel(name, parent, theme.PanelColor);
            return UiFactory.Place((RectTransform)pill.transform, anchor, pivot, position, size);
        }

        private void BuildPlayButton(UiTheme theme)
        {
            Image glow = UiFactory.CreateImage("PlayGlow", _content, SugarSprites.SoftDot, new Color(1f, 1f, 0.85f, 0.55f));
            glow.preserveAspect = false;
            _playGlow = UiFactory.Place(glow.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 165f), new Vector2(860f, 320f));
            Button play = SugarUi.Button("Play", _content, "", theme.PositiveColor, () =>
            {
                SugarUi.Click();
                PlayClicked?.Invoke();
            }, 54);
            UiFactory.Place((RectTransform)play.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 80f), new Vector2(660f, 170f));
            _playLabel = UiFactory.GetButtonLabel(play);
        }

        // ------------------------------------------------------------ Niveluri, drum, decor

        /// <summary>Creează nodurile lipsă (harta crește odată cu progresul) și reface drumul.</summary>
        private void EnsureLevels()
        {
            int needed = MapLayout.VisibleLevels(SugarSave.Data.level);
            if (needed <= _levels) return;
            int first = _levels + 1;
            for (int level = first; level <= needed; level++)
            {
                if ((level - 1) % LevelConfig.ChapterSize == 0) BuildChapterBanner(ChapterTheme.ChapterOf(level));
                _nodes.Add(BuildNode(level));
            }
            BuildDecor(first, needed);
            _levels = needed;
            _scrollContent.sizeDelta = new Vector2(0f, MapLayout.ContentHeight(_levels));
            BuildPath();
            _marker.SetAsLastSibling();
        }

        /// <summary>Eticheta de la începutul unui capitol: „CAPITOLUL 2 · Pădurea de Mentă”, în culoarea temei.</summary>
        private void BuildChapterBanner(int chapter)
        {
            int firstLevel = chapter * LevelConfig.ChapterSize + 1;
            Vector2 position = MapLayout.NodePosition(firstLevel);
            ChapterTheme theme = ChapterTheme.For(chapter);
            Image pill = UiFactory.CreatePanel("Chapter" + (chapter + 1), _dotLayer, theme.Stripe);
            float x = position.x > 0f ? position.x - 300f : position.x + 300f;
            UiFactory.Place((RectTransform)pill.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(Mathf.Clamp(x, -280f, 280f), position.y - 110f), new Vector2(420f, 96f));
            UiLabel label = SugarUi.Outlined("Label", pill.transform, "", 30, Color.white, 3f);
            UiFactory.Stretch(label.RectTransform, 8f);
            _chapterLabels.Add((chapter, label));
            SetChapterText(chapter, label);
        }

        private static void SetChapterText(int chapter, UiLabel label) =>
            label.Text = SugarLoc.F("CAPITOLUL {0}", chapter + 1) + "\n" + SugarLoc.T(ChapterTheme.For(chapter).Name);

        private NodeView BuildNode(int level)
        {
            UiTheme theme = UiRoot.Theme;
            bool boss = LevelConfig.IsBossLevel(level);
            float size = boss ? BossNodeSize : NodeSize;
            var view = new NodeView { Level = level };

            view.Root = UiFactory.CreateRect("Level" + level, _nodeLayer);
            UiFactory.Place(view.Root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), MapLayout.NodePosition(level), new Vector2(size, size));
            view.Visual = UiFactory.CreateRect("Visual", view.Root);
            UiFactory.Stretch(view.Visual);

            view.Glow = UiFactory.CreateImage("Glow", view.Visual, SugarSprites.SoftDot, new Color(1f, 0.86f, 0.35f, 0.6f));
            UiFactory.Place(view.Glow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * size * 2.1f);
            Image shadow = UiFactory.CreateImage("Shadow", view.Visual, SugarSprites.CapShadow, new Color(0.35f, 0.08f, 0.3f, 0.32f));
            UiFactory.Place(shadow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -14f), Vector2.one * size);
            view.Ring = UiFactory.CreateImage("Ring", view.Visual, SugarSprites.Ring, Palette.Tile(4));
            UiFactory.Place(view.Ring.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * size * 1.22f);
            view.Ring.gameObject.SetActive(boss);
            view.Face = UiFactory.CreateImage("Face", view.Visual, SugarSprites.Cap, Color.white);
            UiFactory.Stretch(view.Face.rectTransform);
            view.Face.raycastTarget = true;
            Image gloss = UiFactory.CreateImage("Gloss", view.Visual, SugarSprites.SoftDot, new Color(1f, 1f, 1f, 0.45f));
            UiFactory.Place(gloss.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-size * 0.2f, size * 0.22f), Vector2.one * size * 0.36f);

            view.Number = SugarUi.Outlined("Number", view.Visual, level.ToString(), boss ? 70 : 62, Color.white, 4f);
            UiFactory.Stretch(view.Number.RectTransform);
            Image lockImage = UiFactory.CreateImage("Lock", view.Visual, SugarSprites.Lock, Color.white);
            view.Lock = UiFactory.Place(lockImage.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * size * 0.42f);
            view.Sparkle = UiFactory.CreateImage("Sparkle", view.Visual, SugarSprites.Sparkle, Color.white);
            UiFactory.Place(view.Sparkle.rectTransform, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-12f, -12f), Vector2.one * 46f);

            view.Boss = SugarUi.Outlined("Boss", view.Visual, SugarLoc.T("BOSS"), 34, theme.AccentColor);
            UiFactory.Place(view.Boss.RectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(220f, 48f));
            view.Boss.gameObject.SetActive(boss);
            if (boss)
            {
                // Portretul Boss-ului „pândește” din colțul nodului.
                view.Portrait = UiFactory.CreateImage("BossFace", view.Visual, SugarSprites.BossFace(BossDefinition.For(level, 20).Look), Color.white);
                UiFactory.Place(view.Portrait.rectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(size * 0.02f, -size * 0.04f), Vector2.one * size * 0.55f);
            }

            view.Stars = new Image[3];
            for (int s = 0; s < 3; s++)
            {
                Image star = UiFactory.CreateImage("Star" + s, view.Visual, ProceduralSprites.Star, theme.AccentColor);
                UiFactory.Place(star.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2((s - 1) * 52f, s == 1 ? -18f : -8f), Vector2.one * 50f);
                view.Stars[s] = star;
            }

            view.Animator = view.Root.gameObject.AddComponent<LevelNodeAnimator>();
            view.Animator.Bind(view.Visual, view.Glow, view.Lock);
            var button = view.Root.gameObject.AddComponent<Button>();
            button.targetGraphic = view.Face;
            button.transition = Selectable.Transition.None;
            NodeView captured = view;
            button.onClick.AddListener(() => OnNodeClicked(captured));
            view.Root.gameObject.AddComponent<UIButtonPressAnimator>();
            return view;
        }

        private void OnNodeClicked(NodeView view)
        {
            if (view.Animator.State == LevelNodeState.Locked)
            {
                view.Animator.Wiggle();
                if (SugarAudio.Instance != null) SugarAudio.Instance.Play(SugarSound.Invalid, 0.6f);
                return;
            }
            SugarUi.Click();
            LevelClicked?.Invoke(view.Level);
        }

        /// <summary>Drumul: o bandă moale de lumină și puncte pe o curbă netedă între noduri.</summary>
        private void BuildPath()
        {
            foreach (Transform child in _roadLayer) Destroy(child.gameObject);
            foreach (Transform child in _dotLayer) Destroy(child.gameObject);
            _dots.Clear();
            _dotSegment.Clear();

            for (int level = 1; level < _levels; level++)
            {
                float length = Vector2.Distance(MapLayout.NodePosition(level), MapLayout.NodePosition(level + 1)) * 1.1f;
                int count = Mathf.Max(3, Mathf.RoundToInt(length / DotSpacing));
                for (int k = 0; k < count; k++)
                {
                    float t = (k + 0.5f) / count;
                    Vector2 p = MapLayout.PathPoint(level, t);
                    Image road = UiFactory.CreateImage("Road", _roadLayer, SugarSprites.SoftDot, new Color(1f, 1f, 1f, 0.16f));
                    UiFactory.Place(road.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), p, new Vector2(110f, 110f));
                    Image dot = UiFactory.CreateImage("Dot", _dotLayer, SugarSprites.Circle, DotOff);
                    UiFactory.Place(dot.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), p, new Vector2(22f, 22f));
                    _dots.Add(dot);
                    _dotSegment.Add(level);
                }
            }
            _litLevel = -1;
        }

        /// <summary>Nori de glazură, acadele și bomboane pe marginile drumului (strat de mijloc) și lumini moi (strat îndepărtat).</summary>
        private void BuildDecor(int fromLevel, int toLevel)
        {
            var random = new System.Random(fromLevel * 7919 + toLevel);
            for (int level = fromLevel; level <= toLevel; level++)
            {
                Vector2 node = MapLayout.NodePosition(level);
                float side = node.x > 0f ? -1f : 1f;
                float y = node.y * 0.6f + 260f;

                if (level % 2 == 0)
                {
                    float x = side * (330f + (float)random.NextDouble() * 130f);
                    int kind = random.Next(3);
                    if (kind == 0)
                    {
                        Image cloud = UiFactory.CreateImage("Cloud", _midLayer, SugarSprites.Frosting, new Color(1f, 1f, 1f, 0.7f));
                        float size = 190f + (float)random.NextDouble() * 110f;
                        UiFactory.Place(cloud.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(x, y), new Vector2(size, size * 0.8f));
                        cloud.preserveAspect = false;
                    }
                    else if (kind == 1) Lollipop(new Vector2(x, y), Palette.Tile(random.Next(Palette.TileColorCount)), 80f + (float)random.NextDouble() * 40f);
                    else
                    {
                        Color c = Palette.TileLight(random.Next(Palette.TileColorCount));
                        Image candy = UiFactory.CreateImage("Candy", _midLayer, SugarSprites.CandyVariant(random.Next(3)), new Color(c.r, c.g, c.b, 0.85f));
                        float size = 80f + (float)random.NextDouble() * 50f;
                        UiFactory.Place(candy.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(x, y), new Vector2(size, size));
                        candy.rectTransform.localRotation = Quaternion.Euler(0f, 0f, (float)random.NextDouble() * 360f);
                    }
                }

                // Lumini moi, foarte mari, pe stratul îndepărtat.
                if (level % 3 == 0)
                {
                    Color glow = Color.HSVToRGB(Mathf.Repeat(0.85f + (float)random.NextDouble() * 0.2f, 1f), 0.25f, 1f);
                    glow.a = 0.35f;
                    Image blob = UiFactory.CreateImage("Glow", _farLayer, SugarSprites.SoftDot, glow);
                    float size = 380f + (float)random.NextDouble() * 320f;
                    float bx = ((float)random.NextDouble() - 0.5f) * 1000f;
                    UiFactory.Place(blob.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(bx, node.y * 0.25f + 300f), new Vector2(size, size));
                }
            }
        }

        private void Lollipop(Vector2 position, Color color, float size)
        {
            Image stick = UiFactory.CreateImage("Stick", _midLayer, UiFactory.WhiteSprite, new Color(1f, 1f, 1f, 0.9f));
            stick.preserveAspect = false;
            UiFactory.Place(stick.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), position, new Vector2(12f, size * 1.2f));
            Image head = UiFactory.CreateImage("Lollipop", _midLayer, SugarSprites.CandySwirl, color);
            UiFactory.Place(head.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), position, new Vector2(size, size));
        }

        // ------------------------------------------------------------ Afișare

        public void Show()
        {
            StopUnlock();
            _root.SetActive(true);
            EnsureLevels();
            int current = Mathf.Max(1, SugarSave.Data.level);
            bool unlock = _shownLevel > 0 && current > _shownLevel && current <= _levels;

            if (unlock)
            {
                // Arătăm întâi starea veche, apoi deblocăm animat nivelul nou.
                Refresh(_shownLevel);
                _scroller.FocusNormalized(Focus(_shownLevel), false);
                _unlock = StartCoroutine(UnlockSequence(_shownLevel, current));
            }
            else
            {
                Refresh(current);
                _scroller.FocusNormalized(Focus(current), false);
                for (int i = 0; i < _nodes.Count; i++)
                    if (Mathf.Abs(_nodes[i].Level - current) <= 4) _nodes[i].Animator.PopIn(Mathf.Abs(_nodes[i].Level - current) * 0.05f);
            }
            _shownLevel = current;
        }

        public void Hide()
        {
            StopUnlock();
            _root.SetActive(false);
        }

        /// <summary>Reîmprospătează harta (după schimbarea limbii, a vieților sau a monedelor).</summary>
        public void Refresh()
        {
            if (_unlock != null) return;
            Refresh(Mathf.Max(1, SugarSave.Data.level));
        }

        private float Focus(int level) => MapLayout.FocusNormalized(level, _levels, _view.rect.height);

        private void Refresh(int current)
        {
            UiTheme theme = UiRoot.Theme;
            foreach (NodeView node in _nodes) ApplyNode(node, current, theme);
            LightPath(current, false);
            _markerTarget = MarkerPosition(current);
            _marker.anchoredPosition = _markerTarget;

            SugarSaveData data = SugarSave.Data;
            // Cerul hărții are culorile capitolului în care e jucătorul.
            int chapter = ChapterTheme.ChapterOf(current);
            if (chapter != _backgroundChapter)
            {
                _backgroundChapter = chapter;
                _background.sprite = SugarSprites.Sky(chapter);
            }
            foreach ((int labelChapter, UiLabel label) in _chapterLabels) SetChapterText(labelChapter, label);
            _coins.Text = data.coins.ToString();
            _giftBadge.SetActive(SugarDaily.GiftAvailable);
            _missionsBadge.SetActive(SugarDaily.AnyClaimable);
            _playLabel.Text = SugarLoc.T("JOACĂ") + "  ·  " + SugarLoc.F("NIVELUL {0}", Mathf.Max(1, data.level));
            RefreshLives();
        }

        private void ApplyNode(NodeView node, int current, UiTheme theme)
        {
            int level = node.Level;
            bool boss = LevelConfig.IsBossLevel(level);
            LevelNodeState state = level < current ? LevelNodeState.Completed : level == current ? LevelNodeState.Current : LevelNodeState.Locked;
            node.Animator.SetState(state);

            Color face = state switch
            {
                LevelNodeState.Current => theme.AccentColor,
                LevelNodeState.Completed => boss ? Palette.Tile(4) : Palette.Tile((level - 1) % Palette.TileColorCount),
                _ => boss ? Color.Lerp(LockedColor, Palette.Tile(4), 0.3f) : LockedColor,
            };
            node.Face.color = face;
            node.Ring.color = state == LevelNodeState.Locked ? new Color(0.71f, 0.48f, 1f, 0.45f) : Palette.Tile(4);
            node.Number.gameObject.SetActive(state != LevelNodeState.Locked);
            node.Lock.gameObject.SetActive(state == LevelNodeState.Locked);
            // Mega-Boss (la fiecare 50 de niveluri): etichetă proprie, aurie, și inel auriu.
            bool mega = LevelConfig.IsMegaBossLevel(level);
            node.Boss.Text = SugarLoc.T(mega ? "MEGA BOSS" : "BOSS");
            node.Boss.Color = mega ? new Color(1f, 0.84f, 0.3f) : theme.AccentColor;
            if (mega) node.Ring.color = new Color(1f, 0.84f, 0.3f, state == LevelNodeState.Locked ? 0.5f : 1f);
            if (node.Portrait != null) node.Portrait.color = state == LevelNodeState.Locked ? new Color(0.75f, 0.72f, 0.8f, 0.9f) : Color.white;

            int stars = SugarSave.GetStars(level);
            for (int s = 0; s < 3; s++)
            {
                node.Stars[s].gameObject.SetActive(state == LevelNodeState.Completed);
                node.Stars[s].color = s < stars ? theme.AccentColor : new Color(1f, 1f, 1f, 0.55f);
            }
            node.Sparkle.gameObject.SetActive(state == LevelNodeState.Completed && stars >= 3);
        }

        /// <summary>Aprinde punctele drumului până la nivelul dat (opțional, cu un mic „pop”).</summary>
        private void LightPath(int level, bool pop)
        {
            if (level == _litLevel) return;
            for (int i = 0; i < _dots.Count; i++)
            {
                bool on = _dotSegment[i] < level;
                Image dot = _dots[i];
                Color target = on ? DotOn : DotOff;
                if (dot.color == target) continue;
                dot.color = target;
                dot.rectTransform.sizeDelta = on ? new Vector2(26f, 26f) : new Vector2(22f, 22f);
                if (pop && on)
                {
                    dot.rectTransform.localScale = Vector3.one * 1.8f;
                    _dotsPopUntil = Time.unscaledTime + 0.5f;
                }
            }
            _litLevel = level;
        }

        private static Vector2 MarkerPosition(int level)
        {
            Vector2 node = MapLayout.NodePosition(level);
            float size = LevelConfig.IsBossLevel(level) ? BossNodeSize : NodeSize;
            return new Vector2(node.x, node.y + size * 0.5f + 18f);
        }

        private IEnumerator UnlockSequence(int from, int to)
        {
            yield return new WaitForSecondsRealtime(0.35f);
            _scroller.FocusNormalized(Focus(to), true);

            // Drumul se aprinde punct cu punct, iar Bobo merge spre nivelul nou.
            Vector2 markerFrom = MarkerPosition(from);
            Vector2 markerTo = MarkerPosition(to);
            const float walk = 0.75f;
            for (float t = 0f; t < walk; t += Time.unscaledDeltaTime)
            {
                float k = t / walk;
                LightPartial(from, to, k);
                _marker.anchoredPosition = Vector2.Lerp(markerFrom, markerTo, UiEase.InOutCubic(k)) + new Vector2(0f, Mathf.Abs(Mathf.Sin(k * Mathf.PI * 3f)) * 40f);
                yield return null;
            }
            _marker.anchoredPosition = markerTo;
            _markerTarget = markerTo;

            // Deblocarea: lacătul sare, nodul devine auriu, explozie de sclipiri.
            Refresh(to);
            NodeView node = _nodes.Find(n => n.Level == to);
            if (node != null)
            {
                node.Lock.gameObject.SetActive(true);
                node.Animator.PlayUnlock();
                _bursts.BurstAt(node.Root, 18, SugarSprites.Sparkle, UnlockColors, 700f, 46f);
            }
            if (SugarAudio.Instance != null) SugarAudio.Instance.Play(SugarSound.SpecialCreated);
            SugarAudio.Buzz(1);
            _unlock = null;
        }

        private void LightPartial(int from, int to, float progress)
        {
            // Punctele dintre „from” și „to” se aprind treptat, în ordine.
            int first = -1, last = -1;
            for (int i = 0; i < _dots.Count; i++)
            {
                if (_dotSegment[i] < from || _dotSegment[i] >= to) continue;
                if (first < 0) first = i;
                last = i;
            }
            if (first < 0) return;
            int lit = first + Mathf.FloorToInt((last - first + 1) * progress);
            for (int i = first; i <= last; i++)
            {
                bool on = i < lit;
                Image dot = _dots[i];
                if (!on || dot.color == DotOn) continue;
                dot.color = DotOn;
                dot.rectTransform.sizeDelta = new Vector2(26f, 26f);
                dot.rectTransform.localScale = Vector3.one * 1.8f;
                _dotsPopUntil = Time.unscaledTime + 0.5f;
            }
            _litLevel = -1;
        }

        private void StopUnlock()
        {
            if (_unlock == null) return;
            StopCoroutine(_unlock);
            _unlock = null;
        }

        private void RefreshLives()
        {
            SugarSave.UpdateLives();
            SugarSaveData data = SugarSave.Data;
            _lives.Text = data.lives.ToString();
            _lifeTimer.Text = data.lives >= SugarSave.MaxLives ? "MAX" : SugarUi.Time(SugarSave.SecondsToNextLife);
        }

        private void Update()
        {
            if (!_root.activeSelf) return;
            float time = Time.unscaledTime;
            float dt = Time.unscaledDeltaTime;

            // Bobo sare ușor deasupra nivelului curent; lumina din spatele butonului „Joacă” respiră.
            if (_unlock == null)
                _marker.anchoredPosition = _markerTarget + new Vector2(0f, Mathf.Abs(Mathf.Sin(time * 2.6f)) * 16f);
            _playGlow.localScale = Vector3.one * (1f + Mathf.Sin(time * 3f) * 0.08f);

            // Punctele aprinse recent revin la mărimea normală (doar cât timp există așa ceva).
            if (time < _dotsPopUntil)
            for (int i = 0; i < _dots.Count; i++)
            {
                RectTransform rect = _dots[i].rectTransform;
                float s = rect.localScale.x;
                if (s > 1.001f) rect.localScale = Vector3.one * Mathf.MoveTowards(s, 1f, dt * 3f);
            }

            _refresh -= dt;
            if (_refresh > 0f) return;
            _refresh = 0.5f;
            RefreshLives();
        }
    }
}
