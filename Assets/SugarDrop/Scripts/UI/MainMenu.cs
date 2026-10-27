using System;
using System.Collections.Generic;
using QuarryEmpire;
using UnityEngine;
using UnityEngine.UI;

namespace SugarDrop
{
    /// <summary>
    /// Dashboard-ul (ecranul principal): vieți (cu cronometru), monede și stele sus; Bobo cu costumul ales, care
    /// clipește, sare când îl atingi și spune ce te așteaptă; progresul capitolului; butonul „Joacă” (spre harta
    /// nivelurilor) și 8 plăci: Roata norocului, Cadou, Misiuni, Eveniment, Garderoba lui Bobo, Clasament, Profil,
    /// Setări. Fundalul are cerul capitolului și o ploaie de bomboane.
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        public event Action PlayClicked;
        public event Action WheelClicked;
        public event Action GiftClicked;
        public event Action MissionsClicked;
        public event Action EventClicked;
        public event Action WardrobeClicked;
        public event Action LeaderboardClicked;
        public event Action ProfileClicked;
        public event Action SettingsClicked;

        private const int FloaterCount = 16;
        private const float RainTop = 1150f;
        private const float RainBottom = -1150f;
        private const float BoboY = 300f;
        private const float PlayY = -150f;

        private static readonly string[] TapLines = { "Hihi, gâdilă!", "Hai să jucăm!", "Ești cel mai tare!", "Bomboane, bomboane!", "Încă un nivel?" };

        private class DashTile
        {
            public RectTransform Rect;
            public UiLabel Caption;
            public UiLabel Sub;
            public GameObject Badge;
            public string Key;
        }

        private GameObject _root;
        private Image _background;
        private int _backgroundChapter = -1;
        private UiLabel _logo;
        private UiLabel _lives, _lifeTimer, _coins, _stars;
        private UiLabel _chapter;
        private UiProgressBar _chapterBar;
        private UiLabel _chapterStars;
        private UiLabel _playLabel;
        private RectTransform _playRect;
        private RectTransform _shine;
        private BoboView _bobo;
        private RectTransform _bubble;
        private UiLabel _bubbleText;
        private DashTile _wheel, _gift, _missions, _event, _wardrobe, _ranking, _profile, _settings;
        private readonly List<DashTile> _tiles = new();
        private UIParticleController _bursts;

        private RectTransform[] _floaters;
        private float[] _floaterSpeed, _floaterSpin, _floaterPhase;

        private float _enterT = -1f;
        private float _jump, _jumpVelocity;
        private float _nextBlink, _blinkT = -1f;
        private float _nextLine;
        private int _line;
        private float _bubblePop;
        private int _tapLine;
        private float _refresh;

        public bool IsOpen => _root.activeSelf;

        /// <summary>Numele jocului, pe ecranul principal.</summary>
        public const string GameName = "Drop Quest";

        private void Awake()
        {
            UiTheme theme = UiRoot.Theme;
            _background = UiFactory.CreateImage("Background", transform, SugarSprites.BackgroundGradient, Color.white);
            _background.preserveAspect = false;
            _background.raycastTarget = true;
            UiFactory.Stretch(_background.rectTransform, -300f);
            _root = _background.gameObject;

            BuildCandyRain();
            RectTransform sparkleLayer = UiFactory.CreateRect("Sparkles", _root.transform);
            UiFactory.Stretch(sparkleLayer, 300f);
            _bursts = sparkleLayer.gameObject.AddComponent<UIParticleController>();
            _bursts.Initialize(60, SugarSprites.Sparkle, new[] { Color.white, new Color(1f, 0.9f, 0.6f), new Color(1f, 0.75f, 0.85f) }, 4f);
            _bursts.SetAmbientSize(16f, 36f);

            BuildTopBar(theme);
            _logo = SugarUi.Outlined("Logo", _root.transform, SugarUi.Logo(GameName), 128, Color.white, 6f);
            UiFactory.Place(_logo.RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 680f), new Vector2(1060f, 320f));
            // Logo-ul pictat (Resources/Art/logo.png), dacă există, înlocuiește textul.
            Sprite logoArt = ArtLibrary.Find("logo");
            if (logoArt != null)
            {
                _logo.Text = "";
                Image logoImage = UiFactory.CreateImage("LogoArt", _logo.RectTransform, logoArt, Color.white);
                UiFactory.Place(logoImage.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(760f, 420f));
            }
            BuildBobo();
            BuildBubble(theme);
            BuildChapterCard(theme);
            BuildPlayButton(theme);
            BuildTiles(theme);

            _root.SetActive(false);
        }

        // ------------------------------------------------------------ Construire

        private void BuildTopBar(UiTheme theme)
        {
            RectTransform lives = Pill("LivesPill", SugarSprites.LifeHeart, Color.white, new Vector2(-330f, 850f), theme, out _lives);
            _lifeTimer = UiFactory.CreateText("Timer", lives, "", 28, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.85f));
            UiFactory.Place(_lifeTimer.RectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-14f, 0f), new Vector2(120f, 40f));
            Pill("CoinsPill", ProceduralSprites.Coin, Color.white, new Vector2(0f, 850f), theme, out _coins);
            Pill("StarsPill", ProceduralSprites.Star, theme.AccentColor, new Vector2(330f, 850f), theme, out _stars);
        }

        private RectTransform Pill(string name, Sprite icon, Color iconColor, Vector2 position, UiTheme theme, out UiLabel label)
        {
            Image pill = UiFactory.CreatePanel(name, _root.transform, theme.PanelColor);
            var rect = UiFactory.Place((RectTransform)pill.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(290f, 88f));
            Image image = UiFactory.CreateImage("Icon", rect, icon, iconColor);
            UiFactory.Place(image.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(10f, 0f), new Vector2(92f, 92f));
            label = UiFactory.CreateText("Count", rect, "0", 42);
            UiFactory.Place(label.RectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(64f, 0f), new Vector2(120f, 76f));
            return rect;
        }

        /// <summary>Bomboane și capace care cad încet și se rotesc (cele mici cad mai încet: adâncime).</summary>
        private void BuildCandyRain()
        {
            _floaters = new RectTransform[FloaterCount];
            _floaterSpeed = new float[FloaterCount];
            _floaterSpin = new float[FloaterCount];
            _floaterPhase = new float[FloaterCount];
            for (int i = 0; i < FloaterCount; i++)
            {
                bool cap = i % 3 == 2;
                Sprite sprite = cap ? SugarSprites.Cap : SugarSprites.CandyVariant(i);
                Color color = cap ? Palette.Tile(i % Palette.TileColorCount) : Palette.RandomCandy();
                float size = UnityEngine.Random.Range(60f, 140f);
                Image image = UiFactory.CreateImage("Candy", _root.transform, sprite, new Color(color.r, color.g, color.b, Mathf.Lerp(0.4f, 0.8f, size / 140f)));
                UiFactory.Place(image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(UnityEngine.Random.Range(-500f, 500f), UnityEngine.Random.Range(RainBottom, RainTop)), new Vector2(size, size));
                if (cap)
                {
                    int flavour = i % Palette.TileColorCount;
                    Image icon = UiFactory.CreateImage("Icon", image.transform, SugarSprites.Icon(flavour), Palette.TileLight(flavour));
                    UiFactory.Place(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size * 0.5f, size * 0.5f));
                }
                _floaters[i] = image.rectTransform;
                _floaterSpeed[i] = size * 0.9f + UnityEngine.Random.Range(0f, 40f);
                _floaterSpin[i] = UnityEngine.Random.Range(-40f, 40f);
                _floaterPhase[i] = UnityEngine.Random.value * 10f;
            }
        }

        private void BuildBobo()
        {
            _bobo = BoboView.Build(_root.transform, 0.7f);
            UiFactory.Place(_bobo.Root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, BoboY), _bobo.Root.sizeDelta);
            // Toată silueta lui Bobo se poate atinge.
            var hit = _bobo.Root.gameObject.AddComponent<Image>();
            hit.color = new Color(1f, 1f, 1f, 0f);
            hit.raycastTarget = true;
            var button = _bobo.Root.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(PokeBobo);
        }

        private void BuildBubble(UiTheme theme)
        {
            Image bubble = UiFactory.CreatePanel("Bubble", _root.transform, Color.white);
            _bubble = UiFactory.Place((RectTransform)bubble.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(130f, 330f), new Vector2(380f, 140f));
            Image tail = UiFactory.CreateImage("Tail", _bubble, UiFactory.WhiteSprite, Color.white);
            UiFactory.Place(tail.rectTransform, new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), new Vector2(46f, 6f), new Vector2(36f, 36f));
            tail.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            _bubbleText = UiFactory.CreateText("Text", _bubble, "", 32, TextAnchor.MiddleCenter, theme.TextDarkColor);
            UiFactory.Stretch(_bubbleText.RectTransform, 14f);
        }

        /// <summary>Capitolul curent: numele ținutului și câte stele ai strâns în el (din 30).</summary>
        private void BuildChapterCard(UiTheme theme)
        {
            Image card = UiFactory.CreatePanel("ChapterCard", _root.transform, new Color(1f, 1f, 1f, 0.7f));
            var rect = UiFactory.Place((RectTransform)card.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(780f, 120f));
            _chapter = UiFactory.CreateText("Chapter", rect, "", 32, TextAnchor.MiddleCenter, theme.TextDarkColor);
            UiFactory.Place(_chapter.RectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(760f, 50f));
            _chapterBar = UiFactory.CreateProgressBar("Progress", rect, theme.AccentColor);
            UiFactory.Place((RectTransform)_chapterBar.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-50f, 18f), new Vector2(560f, 34f));
            Image star = UiFactory.CreateImage("Star", rect, ProceduralSprites.Star, theme.AccentColor);
            UiFactory.Place(star.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(262f, 35f), new Vector2(56f, 56f));
            _chapterStars = UiFactory.CreateText("Stars", rect, "", 28, TextAnchor.MiddleLeft, theme.TextDarkColor);
            UiFactory.Place(_chapterStars.RectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 0.5f), new Vector2(294f, 35f), new Vector2(100f, 40f));
        }

        private void BuildPlayButton(UiTheme theme)
        {
            Image glow = UiFactory.CreateImage("PlayGlow", _root.transform, SugarSprites.SoftDot, new Color(1f, 0.95f, 0.55f, 0.55f));
            UiFactory.Place(glow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, PlayY), new Vector2(900f, 320f));
            glow.preserveAspect = false;
            Button play = SugarUi.Button("Play", _root.transform, "", theme.PositiveColor, () =>
            {
                SugarUi.Click();
                PlayClicked?.Invoke();
            }, 60);
            _playRect = UiFactory.Place((RectTransform)play.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, PlayY), new Vector2(640f, 175f));
            _playLabel = UiFactory.GetButtonLabel(play);
            // O dungă de lumină care trece peste buton, din când în când.
            Transform face = play.transform.Find("Face");
            face.gameObject.AddComponent<RectMask2D>();
            Image shine = UiFactory.CreateImage("Shine", face, UiFactory.WhiteSprite, new Color(1f, 1f, 1f, 0.35f));
            shine.preserveAspect = false;
            _shine = UiFactory.Place(shine.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-600f, 0f), new Vector2(70f, 320f));
            _shine.localRotation = Quaternion.Euler(0f, 0f, -20f);
            _playLabel.transform.SetAsLastSibling();
        }

        private void BuildTiles(UiTheme theme)
        {
            _wheel = AddTile("Wheel", SugarSprites.Wheel(8), Color.white, Palette.Tile(3), 0, "Roata", () => WheelClicked?.Invoke(), true);
            _gift = AddTile("Gift", SugarSprites.Gift, Color.white, Palette.Tile(0), 1, "Cadou", () => GiftClicked?.Invoke(), true);
            _missions = AddTile("Missions", SugarSprites.Checklist, Color.white, Palette.Tile(1), 2, "Misiuni", () => MissionsClicked?.Invoke(), true);
            _event = AddTile("Event", ProceduralSprites.Star, Color.white, Palette.Tile(4), 3, "Eveniment", () => EventClicked?.Invoke(), false);
            _wardrobe = AddTile("Wardrobe", SugarSprites.CostumeSprite(Costume.PartyHat), Color.white, Palette.Tile(2), 4, "Bobo", () => WardrobeClicked?.Invoke(), false);
            _ranking = AddTile("Leaderboard", SugarSprites.Trophy, theme.AccentColor, theme.PanelColor, 5, "Clasament", () => LeaderboardClicked?.Invoke(), false);
            _profile = AddTile("Profile", SugarSprites.Person, Color.white, Color.Lerp(Palette.Tile(1), Palette.Tile(4), 0.5f), 6, "Profil", () => ProfileClicked?.Invoke(), false);
            _settings = AddTile("Settings", SugarSprites.Gear, Color.white, theme.PanelColor, 7, "Setări", () => SettingsClicked?.Invoke(), false);
        }

        private DashTile AddTile(string name, Sprite icon, Color iconColor, Color color, int index, string captionKey, Action onClick, bool badge)
        {
            int row = index / 4, column = index % 4;
            Vector2 position = new((column - 1.5f) * 240f, -390f - row * 230f);
            Button button = SugarUi.Button(name, _root.transform, "", color, () =>
            {
                SugarUi.Click();
                onClick();
            }, 28);
            var rect = UiFactory.Place((RectTransform)button.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(225f, 205f));
            Transform face = button.transform.Find("Face");
            Image image = UiFactory.CreateImage("Icon", face, icon, iconColor);
            UiFactory.Place(image.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(100f, 100f));
            var tile = new DashTile { Rect = rect, Key = captionKey };
            tile.Caption = SugarUi.Outlined("Caption", face, "", 30, Color.white, 3f);
            UiFactory.Place(tile.Caption.RectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 36f), new Vector2(215f, 44f));
            tile.Sub = UiFactory.CreateText("Sub", face, "", 22, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.9f));
            UiFactory.Place(tile.Sub.RectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(215f, 30f));
            if (badge)
            {
                Image dot = UiFactory.CreateImage("Badge", button.transform, SugarSprites.Circle, UiRoot.Theme.NegativeColor);
                UiFactory.Place(dot.rectTransform, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-14f, -14f), new Vector2(40f, 40f));
                dot.gameObject.SetActive(false);
                tile.Badge = dot.gameObject;
            }
            _tiles.Add(tile);
            return tile;
        }

        // ------------------------------------------------------------ Afișare

        public void Show()
        {
            foreach (DashTile tile in _tiles) tile.Caption.Text = SugarLoc.T(tile.Key);
            RefreshCounters();
            _bobo.SetCostume(CostumeRules.Current);
            _line = 0;
            Say(BoboLine(0));
            _nextLine = Time.unscaledTime + 4.5f;
            _nextBlink = Time.unscaledTime + 2f;
            _root.SetActive(true);
            _enterT = 0f;
        }

        public void Hide() => _root.SetActive(false);

        /// <summary>Vieți, monede, stele, capitolul, punctele roșii și textele mici de pe plăci.</summary>
        public void RefreshCounters()
        {
            SugarSaveData data = SugarSave.Data;
            SugarSave.UpdateLives();
            int level = Mathf.Max(1, data.level);
            int chapter = ChapterTheme.ChapterOf(level);
            if (chapter != _backgroundChapter)
            {
                _backgroundChapter = chapter;
                _background.sprite = SugarSprites.Sky(chapter);
            }
            _lives.Text = data.lives.ToString();
            _lifeTimer.Text = data.lives >= SugarSave.MaxLives ? SugarLoc.T("PLIN") : SugarUi.Time(SugarSave.SecondsToNextLife);
            _coins.Text = data.coins.ToString();
            _stars.Text = SugarSave.TotalStars(data).ToString();

            int chapterStars = 0;
            for (int l = chapter * LevelConfig.ChapterSize + 1; l <= (chapter + 1) * LevelConfig.ChapterSize; l++) chapterStars += SugarSave.GetStars(l);
            _chapter.Text = SugarLoc.F("CAPITOLUL {0}", chapter + 1) + "  ·  " + SugarLoc.T(ChapterTheme.For(chapter).Name);
            _chapterBar.SetValue(chapterStars / 30f, true);
            _chapterStars.Text = chapterStars + "/30";
            _playLabel.Text = SugarLoc.T("JOACĂ") + "\n<size=32>" + SugarLoc.F("NIVELUL {0}", level) + "</size>";

            int freeSpins = SugarWheel.FreeSpinsLeft;
            _wheel.Badge.SetActive(freeSpins > 0);
            _wheel.Sub.Text = freeSpins > 0 ? SugarLoc.T("GRATIS") : SugarWheel.AdSpinsLeft > 0 ? SugarLoc.T("cu reclamă") : "";
            _gift.Badge.SetActive(SugarDaily.GiftAvailable);
            _gift.Sub.Text = SugarDaily.GiftAvailable ? SugarLoc.F("Ziua {0} din 7", SugarDaily.GiftStreakDay) : "";
            _missions.Badge.SetActive(SugarDaily.AnyClaimable);
            _missions.Sub.Text = MissionsDone() + "/" + DailyRules.MissionsPerDay;
            bool weekend = SugarEvents.WeekendNow;
            _event.Sub.Text = Countdown(SugarEvents.Countdown(DateTime.Now));
            _event.Caption.Text = SugarLoc.T(weekend ? "Weekend dulce" : "Eveniment");
            _wardrobe.Sub.Text = SugarLoc.T(CostumeRules.NameKey(CostumeRules.Current));
            _ranking.Sub.Text = SugarSave.TotalScore.ToString("#,0", System.Globalization.CultureInfo.InvariantCulture).Replace(',', '.');
            _profile.Sub.Text = data.nickname;
            _settings.Sub.Text = SugarLoc.NativeName(SugarLoc.Current);
        }

        private static int MissionsDone()
        {
            int done = 0;
            for (int i = 0; i < DailyRules.MissionsPerDay; i++)
                if (SugarDaily.Claimed(i) || SugarDaily.CanClaim(i)) done++;
            return done;
        }

        /// <summary>„1z 5h” sau „5h 12m” (cât mai e până la începutul/sfârșitul weekendului).</summary>
        public static string Countdown(TimeSpan time) =>
            time.TotalDays >= 1 ? SugarLoc.F("{0}z {1}h", (int)time.TotalDays, time.Hours) : SugarLoc.F("{0}h {1}m", (int)time.TotalHours, time.Minutes);

        /// <summary>Ce spune Bobo, pe rând: salutul, apoi ce te așteaptă azi.</summary>
        private string BoboLine(int index)
        {
            var lines = new List<string>();
            string nickname = SugarSave.Data.nickname;
            lines.Add(string.IsNullOrEmpty(nickname) ? SugarLoc.T("Bine ai venit!") : SugarLoc.F("Salut, {0}!", nickname));
            if (SugarWheel.FreeSpinsLeft > 0) lines.Add(SugarLoc.T("Roata norocului e gratis azi!"));
            if (SugarDaily.GiftAvailable) lines.Add(SugarLoc.T("Te așteaptă un cadou!"));
            if (SugarDaily.AnyClaimable) lines.Add(SugarLoc.T("Ai misiuni de revendicat!"));
            if (SugarEvents.WeekendNow) lines.Add(SugarLoc.T("E weekend dulce: monede duble!"));
            lines.Add(SugarLoc.F("Nivelul {0} te așteaptă!", Mathf.Max(1, SugarSave.Data.level)));
            lines.Add(SugarLoc.T("Atinge-mă!"));
            return lines[index % lines.Count];
        }

        private void Say(string text)
        {
            _bubbleText.Text = text;
            _bubblePop = 0f;
        }

        private void PokeBobo()
        {
            _jumpVelocity = 1400f;
            _blinkT = 0f;
            Say(SugarLoc.T(TapLines[_tapLine++ % TapLines.Length]));
            _nextLine = Time.unscaledTime + 4f;
            if (SugarAudio.Instance != null) SugarAudio.Instance.Play(SugarSound.SpecialCreated, 0.8f, UnityEngine.Random.Range(1.1f, 1.4f));
            SugarAudio.Buzz(0);
            _bursts.BurstAt(_bobo.Root, 16, SugarSprites.Sparkle, new[] { Color.white, Palette.TileLight(0), Palette.TileLight(3) });
        }

        // ------------------------------------------------------------ Animații

        private void Update()
        {
            if (!_root.activeSelf) return;
            float time = Time.unscaledTime;
            float dt = Time.unscaledDeltaTime;

            _refresh -= dt;
            if (_refresh <= 0f)
            {
                _refresh = 1f;
                RefreshCounters();
            }

            for (int i = 0; i < _floaters.Length; i++)
            {
                Vector2 p = _floaters[i].anchoredPosition;
                p.y -= _floaterSpeed[i] * dt * 0.35f;
                p.x += Mathf.Sin(time * 0.8f + _floaterPhase[i]) * 18f * dt;
                if (p.y < RainBottom)
                {
                    p.y = RainTop;
                    p.x = UnityEngine.Random.Range(-500f, 500f);
                }
                _floaters[i].anchoredPosition = p;
                _floaters[i].localRotation = Quaternion.Euler(0f, 0f, _floaterSpin[i] * time + _floaterPhase[i] * 30f);
            }

            _logo.RectTransform.localScale = Vector3.one * (1f + Mathf.Sin(time * 2f) * 0.03f);
            _logo.RectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(time * 1.3f) * 2f);

            // Bobo: respiră, sare când e atins, clipește din când în când.
            _jumpVelocity -= 4200f * dt;
            _jump = Mathf.Max(0f, _jump + _jumpVelocity * dt);
            if (_jump <= 0f) _jumpVelocity = 0f;
            float breathe = Mathf.Sin(time * 2.4f);
            _bobo.Root.anchoredPosition = new Vector2(0f, BoboY + breathe * 6f + _jump);
            float squash = _jump > 0f ? 0.04f : breathe * 0.025f;
            _bobo.Root.localScale = new Vector3(1f + squash, 1f - squash, 1f);
            _bobo.Root.localRotation = Quaternion.Euler(0f, 0f, _jump > 0f ? Mathf.Sin(_jump * 0.03f) * 8f : Mathf.Sin(time * 1.1f) * 2f);
            if (_blinkT < 0f && time >= _nextBlink) _blinkT = 0f;
            float eyeY = 1f;
            if (_blinkT >= 0f)
            {
                _blinkT += dt;
                eyeY = Mathf.Abs(Mathf.Cos(Mathf.Clamp01(_blinkT / 0.16f) * Mathf.PI)) * 0.9f + 0.1f;
                if (_blinkT >= 0.16f)
                {
                    _blinkT = -1f;
                    _nextBlink = time + UnityEngine.Random.Range(2f, 4.5f);
                }
            }
            foreach (RectTransform eye in _bobo.Eyes) eye.localScale = new Vector3(1f, eyeY, 1f);

            if (time >= _nextLine)
            {
                _nextLine = time + 4.5f;
                Say(BoboLine(++_line));
            }
            _bubblePop = Mathf.Min(1f, _bubblePop + dt / 0.3f);
            _bubble.localScale = Vector3.one * UiEase.OutBack(_bubblePop, 2.2f);

            _shine.anchoredPosition = new Vector2(Mathf.Lerp(-600f, 600f, Mathf.Repeat(time / 2.6f, 1f) * 1.6f - 0.3f), 0f);
            // Roata de pe placă se învârte încet, ca să atragă atenția când e gratis.
            if (_wheel.Badge.activeSelf) _wheel.Rect.Find("Face/Icon").localRotation = Quaternion.Euler(0f, 0f, -time * 40f);

            // Intrarea: logo-ul cade de sus, butonul urcă, plăcile apar pe rând.
            float pulse = 1f + Mathf.Sin(time * 3.2f) * 0.035f;
            if (_enterT >= 0f)
            {
                _enterT += dt;
                float logoT = Mathf.Clamp01(_enterT / 0.6f);
                float playT = Mathf.Clamp01((_enterT - 0.25f) / 0.5f);
                _logo.RectTransform.anchoredPosition = new Vector2(0f, 700f + (1f - UiEase.OutBack(logoT, 2f)) * 600f);
                _playRect.anchoredPosition = new Vector2(0f, PlayY - (1f - UiEase.OutBack(playT, 1.6f)) * 500f);
                for (int i = 0; i < _tiles.Count; i++)
                {
                    float t = Mathf.Clamp01((_enterT - 0.35f - i * 0.05f) / 0.35f);
                    _tiles[i].Rect.localScale = Vector3.one * UiEase.OutBack(t, 2f);
                }
                if (_enterT > 1.2f) _enterT = -1f;
            }
            _playRect.localScale = Vector3.one * pulse;
        }
    }
}
