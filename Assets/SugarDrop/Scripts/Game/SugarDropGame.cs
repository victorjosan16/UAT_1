using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SugarDrop
{
    /// <summary>
    /// Fluxul jocului: meniu → hartă → nivel → rezultat. Numără mutările, ține scorul și obiectivele nivelului
    /// (bomboane în borcan, culori de strâns, glazură, scor), verifică victoria și înfrângerea, dă monede și stele,
    /// gestionează viețile, ajutoarele (bombă, +5 mutări, amestecare), tutorialul și indiciile. Leagă evenimentele
    /// tablei de efecte, sunete și de mascotă.
    /// </summary>
    public class SugarDropGame : MonoBehaviour
    {
        private enum State { Menu, Map, Playing, Finale, Result }

        [Header("Joc")]
        [SerializeField] private Board board;
        [SerializeField] private BoardInput input;
        [SerializeField] private CandySpawner candies;
        [SerializeField] private Jar jar;
        [SerializeField] private Playfield playfield;
        [SerializeField] private Mascot mascot;
        [SerializeField] private HintGuide hintGuide;

        [Header("Interfață")]
        [SerializeField] private SugarHud hud;
        [SerializeField] private BoosterBar boosters;
        [SerializeField] private ResultPanel result;
        [SerializeField] private DialogPanel dialog;
        [SerializeField] private MapScreen map;
        [SerializeField] private MainMenu menu;
        [SerializeField] private WelcomePanel welcome;
        [SerializeField] private LeaderboardPanel leaderboard;
        [SerializeField] private TutorialCoach coach;
        [SerializeField] private WheelPanel wheel;
        [SerializeField] private WardrobePanel wardrobe;

        [Header("Așezare (unități din lume)")]
        [SerializeField] private float boardBottomY = -2.6f;
        [SerializeField] private Vector2 mascotOffset = new(3.35f, -0.4f);

        [Header("Final de nivel")]
        [Tooltip("Pauza dintre îndeplinirea obiectivelor (sau ultima mutare) și finalul nivelului.")]
        [SerializeField] private float endDelay = 0.45f;

        [Header("Economie")]
        [SerializeField] private int boosterPrice = 100;
        [SerializeField] private int extraMoves = 5;
        [SerializeField] private float idleHintSeconds = 7f;

        private LevelConfig _config;
        private int _level;
        private int _moves;
        private State _state;
        private float _endTimer;
        private LevelGoals _goals;
        private System.Func<int, int> _colorWeight;
        private float _idleTime;
        private bool _playfieldBuilt;
        private bool _continuedWithAd;
        private bool _warnedLastMoves;
        private enum TutorialStep { None, Match, Rocket, Fire, Goal, Moves, Boosters }

        private TutorialStep _tutorial;
        /// <summary>O mutare din tutorial s-a făcut; pasul următor vine după ce tabla se liniștește.</summary>
        private bool _tutorialWaiting;
        private int _popChain;
        private int _score;
        private int _movePoints;
        private bool _scoreDirty;
        private float _lastBuzz;

        // Lupta cu Boss-ul (null pe nivelurile obișnuite).
        private BossBattle _boss;
        private bool _bossTurnPending;
        private bool _waitingBossIntro;
        private float _lastBossHitSound;

        private static SugarSaveData Data => SugarSave.Data;
        private static SugarVfx Vfx => SugarVfx.Instance;
        private static SugarAudio Audio => SugarAudio.Instance;

        // ------------------------------------------------------------ Pornire

        private void Awake()
        {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            SugarSave.Load();
            SugarQuality.Apply();
            SugarSave.UpdateLives();
            _colorWeight = ColorWeight;
        }

        /// <summary>Pentru indicii: piesele de culorile încă cerute valorează mai mult.</summary>
        private int ColorWeight(int color)
        {
            if (_goals == null) return 0;
            foreach (GoalItem item in _goals.Items)
                if (item.Kind == GoalKind.Collect && item.Color == color && !item.Done) return 3;
            return 0;
        }

        private void OnEnable()
        {
            board.MoveMade += OnMoveMade;
            board.InvalidTap += OnInvalidTap;
            board.GroupBlasted += OnGroupBlasted;
            board.ComboTriggered += OnComboTriggered;
            board.TileCleared += OnTileCleared;
            board.SpecialActivated += OnSpecialActivated;
            board.SpecialCreated += OnSpecialCreated;
            board.IceBroken += OnIceBroken;
            board.BiscuitHit += OnBiscuitHit;
            board.MoveResolved += OnMoveResolved;
            board.Shuffled += OnShuffled;
            board.StepPlayed += OnStepPlayed;
            board.FrostingMelted += OnFrostingMelted;
            board.IngredientExited += OnIngredientExited;
            board.ChocolateGrew += OnChocolateGrew;
            board.PotWatered += OnPotWatered;
            board.PotBloomed += OnPotBloomed;
            board.TimeBombExploded += OnTimeBombExploded;
            board.PortalUsed += OnPortalUsed;
            board.BossAttackLanded += OnBossAttackLanded;
            jar.CandyEntered += OnCandyEntered;
            input.Touched += OnTouched;
            hud.PauseClicked += ShowPause;
            boosters.Clicked += OnBooster;
            map.PlayClicked += TryPlay;
            map.LevelClicked += TryPlayLevel;
            map.SettingsClicked += ShowMapSettings;
            map.LeaderboardClicked += ShowLeaderboard;
            map.GiftClicked += ShowGift;
            map.MissionsClicked += ShowMissions;
            menu.PlayClicked += OpenMapAnimated;
            menu.GiftClicked += ShowGift;
            menu.MissionsClicked += ShowMissions;
            menu.LeaderboardClicked += ShowLeaderboard;
            menu.SettingsClicked += ShowMapSettings;
            menu.WheelClicked += ShowWheel;
            menu.EventClicked += ShowEvent;
            menu.WardrobeClicked += ShowWardrobe;
            menu.ProfileClicked += ShowProfile;
            wheel.PrizeWon += OnWheelPrize;
            wardrobe.CostumeChanged += OnCostumeChanged;
            SugarCloudSave.Restored += OnCloudRestored;
        }

        private void OnDisable()
        {
            board.MoveMade -= OnMoveMade;
            board.InvalidTap -= OnInvalidTap;
            board.GroupBlasted -= OnGroupBlasted;
            board.ComboTriggered -= OnComboTriggered;
            board.TileCleared -= OnTileCleared;
            board.SpecialActivated -= OnSpecialActivated;
            board.SpecialCreated -= OnSpecialCreated;
            board.IceBroken -= OnIceBroken;
            board.BiscuitHit -= OnBiscuitHit;
            board.MoveResolved -= OnMoveResolved;
            board.Shuffled -= OnShuffled;
            board.StepPlayed -= OnStepPlayed;
            board.FrostingMelted -= OnFrostingMelted;
            board.IngredientExited -= OnIngredientExited;
            board.ChocolateGrew -= OnChocolateGrew;
            board.PotWatered -= OnPotWatered;
            board.PotBloomed -= OnPotBloomed;
            board.TimeBombExploded -= OnTimeBombExploded;
            board.PortalUsed -= OnPortalUsed;
            board.BossAttackLanded -= OnBossAttackLanded;
            jar.CandyEntered -= OnCandyEntered;
            input.Touched -= OnTouched;
            hud.PauseClicked -= ShowPause;
            boosters.Clicked -= OnBooster;
            map.PlayClicked -= TryPlay;
            map.LevelClicked -= TryPlayLevel;
            map.SettingsClicked -= ShowMapSettings;
            map.LeaderboardClicked -= ShowLeaderboard;
            map.GiftClicked -= ShowGift;
            map.MissionsClicked -= ShowMissions;
            menu.PlayClicked -= OpenMapAnimated;
            menu.GiftClicked -= ShowGift;
            menu.MissionsClicked -= ShowMissions;
            menu.LeaderboardClicked -= ShowLeaderboard;
            menu.SettingsClicked -= ShowMapSettings;
            menu.WheelClicked -= ShowWheel;
            menu.EventClicked -= ShowEvent;
            menu.WardrobeClicked -= ShowWardrobe;
            menu.ProfileClicked -= ShowProfile;
            wheel.PrizeWon -= OnWheelPrize;
            wardrobe.CostumeChanged -= OnCostumeChanged;
            SugarCloudSave.Restored -= OnCloudRestored;
        }

        private void Start()
        {
            SugarFonts.Apply();
            _state = State.Menu;
            input.enabled = false;
            result.Hide();
            map.Hide();
            menu.Show();
            SetGameplayUi(false);
            RefreshBoosters();
            SugarLeaderboard.Ensure();

            // Prima pornire: Bobo ne salută și ne alegem nickname-ul; abia apoi întrebarea despre reclame.
            if (string.IsNullOrEmpty(Data.nickname))
            {
                welcome.Show(true, nickname =>
                {
                    SetNickname(nickname);
                    SetupAds();
                });
            }
            else SetupAds();
        }

        // ------------------------------------------------------------ Nickname și clasament

        private void SetNickname(string nickname)
        {
            Data.nickname = nickname;
            SugarSave.Save();
            SugarLeaderboard.Ensure().Submit();
            if (menu.IsOpen) menu.Show();
        }

        /// <summary>Cufărul de final de capitol: monede după stelele strânse în capitol, ajutoare pentru capitolele aproape perfecte.</summary>
        private void ShowChapterChest(int chapter)
        {
            int stars = 0;
            for (int level = chapter * LevelConfig.ChapterSize + 1; level <= (chapter + 1) * LevelConfig.ChapterSize; level++)
                stars += SugarSave.GetStars(level);
            int coins = 50 + stars * 5;
            bool boosters = stars >= 24;
            Data.coins += coins;
            StartBoosters.Add(StartBooster.Rocket, 1);
            if (boosters)
            {
                Data.bombs++;
                Data.extraMoves++;
                Data.shuffles++;
                StartBoosters.Add(StartBooster.Bomb, 1);
            }
            Data.chaptersRewarded = chapter + 1;
            SugarSave.Save();
            RefreshBoosters();
            map.Refresh();
            if (Audio != null) Audio.Play(SugarSound.Win);
            if (Vfx != null) Vfx.Confetti(Vector3.zero, 9.2f, 10.5f);

            UiThemeColors(out Color positive, out Color ad, out Color panel);
            string message = SugarLoc.F("Stele: {0}/30", stars) + "  ·  " + SugarLoc.F("+{0} monede", coins)
                + "\n" + SugarLoc.T("+1 rachetă la start") + (boosters ? "  ·  " + SugarLoc.T("+1 la fiecare ajutor") : "")
                + "\n" + SugarLoc.F("Urmează: {0}", SugarLoc.T(ChapterTheme.For(chapter + 1).Name));
            dialog.Show(SugarLoc.F("Capitolul {0} terminat!", chapter + 1), message,
                new DialogButton(SugarLoc.T("SUPER!"), positive, null));
        }

        // ------------------------------------------------------------ Cadoul zilnic și misiunile

        /// <summary>Meniul sau harta (de unde se deschid cadoul, misiunile, clasamentul și setările).</summary>
        private bool OnHomeScreen => _state == State.Map || _state == State.Menu;

        /// <summary>Monedele, stelele și punctele roșii de pe ecranul deschis.</summary>
        private void RefreshHome()
        {
            map.Refresh();
            if (menu.IsOpen) menu.RefreshCounters();
        }

        private bool _giftOffered;

        /// <summary>La prima deschidere a hărții dintr-o zi nouă, cadoul apare singur.</summary>
        private void OfferGift()
        {
            if (_giftOffered || !SugarDaily.GiftAvailable || dialog.IsOpen || welcome.IsOpen) return;
            _giftOffered = true;
            ShowGift();
        }

        private void ShowGift()
        {
            if (!OnHomeScreen || dialog.IsOpen) return;
            UiThemeColors(out Color positive, out Color ad, out Color panel);
            if (!SugarDaily.GiftAvailable)
            {
                dialog.Show(SugarLoc.T("Cadou zilnic"), null,
                    () => SugarLoc.F("Următorul cadou în {0}", Clock(DailyRules.UntilTomorrow)),
                    new DialogButton(SugarLoc.T("ÎNCHIDE"), panel, null));
                return;
            }
            int day = SugarDaily.GiftStreakDay;
            DailyGift gift = DailyRules.GiftFor(day);
            var buttons = new List<DialogButton> { new(SugarLoc.T("REVENDICĂ"), positive, () => ClaimGift(1)) };
            if (gift.Coins > 0) buttons.Add(new DialogButton(SugarLoc.T("x2 MONEDE (reclamă)"), ad, () => SugarUi.RewardedAd("daily_gift", () => ClaimGift(2))));
            dialog.Show(SugarLoc.T("Cadou zilnic"), SugarLoc.F("Ziua {0} din 7", day) + "\n" + GiftText(gift, 1), buttons.ToArray());
        }

        private void ClaimGift(int multiplier)
        {
            if (!SugarDaily.GiftAvailable) return;
            DailyGift gift = SugarDaily.ClaimGift(multiplier);
            RefreshBoosters();
            RefreshHome();
            if (Audio != null) Audio.Play(SugarSound.Win);
            if (Vfx != null) Vfx.Confetti(Vector3.zero, 9.2f, 10.5f);
            UiThemeColors(out Color positive, out Color ad, out Color panel);
            dialog.Show(SugarLoc.T("Cadou zilnic"), GiftText(gift, multiplier), new DialogButton(SugarLoc.T("SUPER!"), positive, null));
        }

        private static string GiftText(DailyGift gift, int multiplier)
        {
            var parts = new List<string>();
            if (gift.Coins > 0) parts.Add(SugarLoc.F("+{0} monede", gift.Coins * multiplier));
            if (gift.Bombs > 0 && gift.ExtraMoves > 0 && gift.Shuffles > 0) parts.Add(SugarLoc.T("+1 la fiecare ajutor"));
            else
            {
                if (gift.Bombs > 0) parts.Add(SugarLoc.T("+1 bombă"));
                if (gift.ExtraMoves > 0) parts.Add(SugarLoc.T("+1 ajutor „+5 mutări”"));
                if (gift.Shuffles > 0) parts.Add(SugarLoc.T("+1 amestecare"));
            }
            return string.Join("  ·  ", parts);
        }

        private void ShowMissions()
        {
            if (!OnHomeScreen) return;
            UiThemeColors(out Color positive, out Color ad, out Color panel);
            Mission[] missions = SugarDaily.Missions;
            var buttons = new List<DialogButton>();
            for (int i = 0; i < missions.Length; i++)
            {
                int index = i;
                Mission mission = missions[i];
                string text = SugarLoc.F(mission.TextKey, mission.Target);
                if (SugarDaily.Claimed(i))
                    buttons.Add(new DialogButton(text + "\n" + SugarLoc.T("GATA"), Color.Lerp(panel, Color.white, 0.35f), null, false));
                else if (SugarDaily.CanClaim(i))
                    buttons.Add(new DialogButton(text + "\n" + SugarLoc.F("REVENDICĂ +{0}", mission.Reward), positive, () => ClaimMission(index), false));
                else
                    buttons.Add(new DialogButton(text + "\n" + SugarDaily.Progress(i) + "/" + mission.Target + "  ·  +" + mission.Reward, panel, null, false));
            }
            buttons.Add(new DialogButton(SugarLoc.T("ÎNCHIDE"), QuarryEmpire.UiRoot.Theme.AccentColor, null));
            dialog.Show(SugarLoc.T("Misiuni zilnice"), null,
                () => SugarLoc.F("Toate trei: +{0} bonus", DailyRules.AllMissionsBonus) + "\n" + SugarLoc.F("Misiuni noi în {0}", Clock(DailyRules.UntilTomorrow)),
                buttons.ToArray());
        }

        private void ClaimMission(int index)
        {
            int coins = SugarDaily.Claim(index);
            if (coins <= 0) return;
            if (Audio != null) Audio.Play(SugarSound.Win);
            SugarAudio.Buzz(1);
            RefreshHome();
            ShowMissions();
        }

        /// <summary>Progresul misiunilor dintr-un pas al tablei (bomboane, piese speciale, combinații, obstacole).</summary>
        private void TrackMissions(BoardStep step)
        {
            int candies = 0, obstacles = step.IceLayersBroken + step.ChainsBroken;
            foreach (PieceInfo piece in step.Cleared)
            {
                if (piece.Kind == TileKind.Normal && piece.Color >= 0) candies++;
                else if (piece.Kind == TileKind.Crate || piece.Kind == TileKind.Chocolate) obstacles++;
            }
            bool done = SugarDaily.AddProgress(MissionKind.ClearCandies, candies) >= 0;
            done |= SugarDaily.AddProgress(MissionKind.UseSpecials, step.Activations.Count) >= 0;
            done |= SugarDaily.AddProgress(MissionKind.MakeCombos, step.Combo ? 1 : 0) >= 0;
            done |= SugarDaily.AddProgress(MissionKind.BreakObstacles, obstacles) >= 0;
            if (done) hud.ShowBanner(SugarLoc.T("Misiune îndeplinită!"));
        }

        private static string Clock(System.TimeSpan time) =>
            $"{(int)time.TotalHours}:{time.Minutes:00}:{time.Seconds:00}";

        /// <summary>Progresul a venit din cloud (ex. după reinstalare): ecranele arată noul progres.</summary>
        private void OnCloudRestored()
        {
            RefreshBoosters();
            if (welcome.IsOpen && !string.IsNullOrEmpty(Data.nickname))
            {
                // Jucătorul își avea deja numele: nu-l mai întrebăm, trecem direct la reclame.
                welcome.Hide();
                SetupAds();
            }
            if (menu.IsOpen) menu.Show();
            if (map.IsOpen) map.Show();
            SugarLeaderboard.Ensure().Submit();
        }

        // ------------------------------------------------------------ Cererea de recenzie

        /// <summary>După nivelul 8 (și încă o dată după 25, dacă a ales „mai târziu”), Bobo întreabă dacă îți place jocul.</summary>
        private bool ShouldAskRating(int level) =>
            Data.rateState == 0 && level == 8 || Data.rateState == 1 && level == 25;

        private void AskRating()
        {
            UiThemeColors(out Color positive, out Color ad, out Color panel);
            dialog.Show(SugarLoc.T("Îți place Sugar Drop?"), SugarLoc.T("Bobo e curios ce părere ai!"),
                new DialogButton(SugarLoc.T("DA, ÎMI PLACE!"), positive, AskReview),
                new DialogButton(SugarLoc.T("NU PREA"), panel, () => SetRateState(2)));
            dialog.DismissAction = () => SetRateState(1);
        }

        private void AskReview()
        {
            UiThemeColors(out Color positive, out Color ad, out Color panel);
            dialog.Show(SugarLoc.T("Mulțumim!"), SugarLoc.T("Ne lași o recenzie pe Google Play? Ne ajută foarte mult."),
                new DialogButton(SugarLoc.T("EVALUEAZĂ"), positive, () =>
                {
                    SetRateState(2);
                    OpenStorePage();
                }),
                new DialogButton(SugarLoc.T("MAI TÂRZIU"), panel, () => SetRateState(1)));
            dialog.DismissAction = () => SetRateState(1);
        }

        private static void SetRateState(int state)
        {
            Data.rateState = System.Math.Max(Data.rateState, state);
            SugarSave.Save();
        }

        private static void OpenStorePage()
        {
            string id = Application.identifier;
            Application.OpenURL(Application.platform == RuntimePlatform.Android
                ? "market://details?id=" + id
                : "https://play.google.com/store/apps/details?id=" + id);
        }

        private void ShowLeaderboard()
        {
            if (!OnHomeScreen || dialog.IsOpen) return;
            leaderboard.Show();
        }

        // ------------------------------------------------------------ Reclame (consimțământ GDPR)

        /// <summary>
        /// La prima pornire (doar dacă reclamele reale sunt active) întrebăm dacă jucătorul acceptă reclame
        /// personalizate; răspunsul se poate schimba din Setări. Apoi pornește LevelPlay.
        /// </summary>
        private void SetupAds()
        {
            SugarAds.Unavailable -= OnAdUnavailable;
            SugarAds.Unavailable += OnAdUnavailable;
            SugarAds.Ensure();
            if (!SugarAds.RealAds) return;
            if (Data.adsConsent >= 0)
            {
                SugarAds.Instance.Initialize(Data.adsConsent == 1);
                return;
            }
            UiThemeColors(out Color positive, out Color ad, out Color panel);
            dialog.Show(SugarLoc.T("Reclame"), SugarLoc.T("Reclamele din joc sunt opționale și îți dau recompense (vieți, mutări, ajutoare). Accepți reclame personalizate? Poți schimba oricând din Setări."),
                new DialogButton(SugarLoc.T("DA, ACCEPT"), positive, () => SetAdsConsent(true)),
                new DialogButton(SugarLoc.T("NU, MULȚUMESC"), panel, () => SetAdsConsent(false)));
            dialog.DismissAction = () => SetAdsConsent(false);
        }

        private void SetAdsConsent(bool consent)
        {
            bool first = Data.adsConsent < 0;
            Data.adsConsent = consent ? 1 : 0;
            SugarSave.Save();
            if (SugarAds.Instance == null) return;
            if (first) SugarAds.Instance.Initialize(consent);
            else SugarAds.Instance.UpdateConsent(consent);
        }

        private void OnAdUnavailable()
        {
            UiThemeColors(out Color positive, out Color ad, out Color panel);
            dialog.Show(SugarLoc.T("Nicio reclamă acum"), SugarLoc.T("Nu e nicio reclamă disponibilă. Verifică internetul și încearcă din nou puțin mai târziu."),
                new DialogButton(SugarLoc.T("ÎNCHIDE"), panel, null));
        }

        /// <summary>HUD-ul și ajutoarele se ascund cât timp meniul sau harta acoperă ecranul (mai puțin de desenat).</summary>
        private void SetGameplayUi(bool visible)
        {
            hud.gameObject.SetActive(visible);
            boosters.gameObject.SetActive(visible);
        }

        /// <summary>Schimbă ecranul sub o tranziție (care blochează și apăsările repetate).</summary>
        private static void Go(System.Action action) => UITransitionController.Run(action);

        private void OpenMapAnimated() => Go(() =>
        {
            OpenMap();
            OfferGift();
        });

        private void OnApplicationPause(bool paused)
        {
            if (paused) SugarSave.Save();
        }

        // ------------------------------------------------------------ Ecrane

        private void OpenMap()
        {
            Time.timeScale = 1f;
            _state = State.Map;
            input.enabled = false;
            CancelBomb();
            menu.Hide();
            result.Hide();
            dialog.Hide();
            hintGuide.Hide();
            StopTutorial();
            SetGameplayUi(false);
            mascot.SetIdle();
            map.Show();
        }

        private void OpenMenu()
        {
            _state = State.Menu;
            map.Hide();
            menu.Show();
        }

        private void TryPlay() => TryPlayLevel(Mathf.Max(1, Data.level));

        /// <summary>Pornește un nivel deblocat (cel curent sau unul trecut, pentru mai multe stele).</summary>
        private void TryPlayLevel(int level)
        {
            if (_state != State.Map || dialog.IsOpen || welcome.IsOpen || leaderboard.IsOpen || wheel.IsOpen || wardrobe.IsOpen) return;
            level = Mathf.Clamp(level, 1, Mathf.Max(1, Data.level));
            SugarSave.UpdateLives();
            if (Data.lives <= 0)
            {
                ShowNoLives();
                return;
            }
            if (StartBoosters.Offered(level) && Data.tutorialDone)
            {
                _useRocket = _useBomb = false;
                ShowLevelStart(level);
            }
            else Go(() => StartLevel(level));
        }

        // ------------------------------------------------------------ Fereastra de start a nivelului (boostere de start)

        private bool _useRocket, _useBomb;

        /// <summary>
        /// Înainte de nivel: ce cere nivelul și boosterele de start (o rachetă sau o bombă puse pe tablă de la început).
        /// Dacă nu mai ai, le poți cumpăra cu monede.
        /// </summary>
        private void ShowLevelStart(int level)
        {
            UiThemeColors(out Color positive, out Color ad, out Color panel);
            LevelConfig config = LevelConfig.For(level);
            string title = config.IsBoss ? SugarLoc.T(config.IsMegaBoss ? "MEGA BOSS!" : "BOSS!") + " " + level : SugarLoc.F("NIVELUL {0}", level);
            string message = config.IsBoss ? SugarLoc.T("Învinge Boss-ul!") : IntroText(config.Type);
            dialog.Show(title, message,
                StartBoosterButton(StartBooster.Rocket, "Rachetă la start", level),
                StartBoosterButton(StartBooster.Bomb, "Bombă la start", level),
                new DialogButton(SugarLoc.T("JOACĂ"), positive, () =>
                {
                    if (_useRocket) StartBoosters.Add(StartBooster.Rocket, -1);
                    if (_useBomb) StartBoosters.Add(StartBooster.Bomb, -1);
                    SugarSave.Save();
                    bool rocket = _useRocket, bomb = _useBomb;
                    Go(() =>
                    {
                        StartLevel(level);
                        if (rocket) board.TryMakeRandomSpecial(out _, TileKind.RocketH);
                        if (bomb) board.TryMakeRandomSpecial(out _, TileKind.Bomb);
                    });
                }),
                new DialogButton(SugarLoc.T("ÎNAPOI"), panel, null));
        }

        private DialogButton StartBoosterButton(StartBooster booster, string nameKey, int level)
        {
            bool selected = booster == StartBooster.Rocket ? _useRocket : _useBomb;
            int count = StartBoosters.Count(booster);
            Color color = selected ? QuarryEmpire.UiRoot.Theme.AccentColor : QuarryEmpire.UiRoot.Theme.PanelColor;
            string label = count > 0 || selected
                ? SugarLoc.T(nameKey) + ": " + SugarLoc.T(selected ? "DA" : "NU") + "  (" + count + ")"
                : SugarLoc.T(nameKey) + "  ·  " + SugarLoc.F("{0} monede", StartBoosters.Price);
            return new DialogButton(label, color, () =>
            {
                if (!selected && StartBoosters.Count(booster) <= 0)
                {
                    if (Data.coins < StartBoosters.Price)
                    {
                        SugarAudio.Buzz(1);
                        if (Audio != null) Audio.Play(SugarSound.Invalid);
                        return;
                    }
                    Data.coins -= StartBoosters.Price;
                    StartBoosters.Add(booster, 1);
                    SugarSave.Save();
                    if (Audio != null) Audio.Play(SugarSound.Coin);
                    RefreshHome();
                }
                if (booster == StartBooster.Rocket) _useRocket = !_useRocket;
                else _useBomb = !_useBomb;
                ShowLevelStart(level);
            }, false);
        }

        // ------------------------------------------------------------ Roata, garderoba, evenimentul, profilul

        private void ShowWheel()
        {
            if (!OnHomeScreen || dialog.IsOpen) return;
            wheel.Show();
        }

        private void OnWheelPrize()
        {
            RefreshBoosters();
            RefreshHome();
        }

        private void ShowWardrobe()
        {
            if (!OnHomeScreen || dialog.IsOpen) return;
            wardrobe.Show();
        }

        private void OnCostumeChanged()
        {
            mascot.ApplyCostume();
            if (menu.IsOpen) menu.Show();
        }

        private void ShowEvent()
        {
            if (!OnHomeScreen || dialog.IsOpen) return;
            UiThemeColors(out Color positive, out Color ad, out Color panel);
            if (SugarEvents.WeekendNow)
            {
                dialog.Show(SugarLoc.T("Weekend dulce"), null,
                    () => SugarLoc.T("Monede duble la fiecare nivel câștigat și o rotire gratuită în plus la roată!") + "\n"
                        + SugarLoc.F("Se termină în {0}", MainMenu.Countdown(SugarEvents.Countdown(System.DateTime.Now))),
                    new DialogButton(SugarLoc.T("LA ROATĂ"), positive, ShowWheel),
                    new DialogButton(SugarLoc.T("ÎNCHIDE"), panel, null));
                return;
            }
            dialog.Show(SugarLoc.T("Eveniment"), null,
                () => SugarLoc.T("În weekend: monede duble și o rotire gratuită în plus la roată.") + "\n"
                    + SugarLoc.F("Începe în {0}", MainMenu.Countdown(SugarEvents.Countdown(System.DateTime.Now))),
                new DialogButton(SugarLoc.T("ÎNCHIDE"), panel, null));
        }

        private void ShowProfile()
        {
            if (!OnHomeScreen || dialog.IsOpen) return;
            UiThemeColors(out Color positive, out Color ad, out Color panel);
            int stars = SugarSave.TotalStars(Data);
            string points = SugarSave.TotalScore.ToString("#,0", System.Globalization.CultureInfo.InvariantCulture).Replace(',', '.');
            string message = SugarLoc.F("Nivelul {0}  ·  {1} stele", Mathf.Max(1, Data.level), stars) + "\n"
                + SugarLoc.F("{0} puncte  ·  {1} capitole", points, Data.chaptersRewarded);
            dialog.Show(string.IsNullOrEmpty(Data.nickname) ? SugarLoc.T("Profil") : Data.nickname, message,
                new DialogButton(SugarLoc.T("SCHIMBĂ NICKNAME-UL"), panel, () => welcome.Show(false, SetNickname)),
                new DialogButton(SugarLoc.T("Garderoba lui Bobo"), Palette.Tile(2), ShowWardrobe),
                new DialogButton(SugarLoc.T("CLASAMENT"), QuarryEmpire.UiRoot.Theme.AccentColor, ShowLeaderboard),
                new DialogButton(SugarLoc.T("ÎNCHIDE"), positive, null));
        }

        private void ShowNoLives()
        {
            UiThemeColors(out Color positive, out Color ad, out Color panel);
            dialog.Show(SugarLoc.T("Fără vieți!"), null, () =>
                {
                    SugarSave.UpdateLives();
                    return SugarLoc.F("Următoarea viață în {0}", SugarUi.Time(SugarSave.SecondsToNextLife));
                },
                new DialogButton(SugarLoc.T("+1 VIAȚĂ (reclamă)"), ad, () => SugarUi.RewardedAd("life", () =>
                {
                    SugarSave.AddLife();
                    map.Refresh();
                })),
                new DialogButton(SugarLoc.T("ÎNCHIDE"), panel, null));
        }

        // ------------------------------------------------------------ Nivel

        private void StartLevel(int level)
        {
            Time.timeScale = 1f;
            _level = level;
            _config = LevelConfig.For(_level);
            int chapter = ChapterTheme.ChapterOf(_level);
            if (Backdrop.Instance != null) Backdrop.Instance.ApplyTheme(chapter);
            // Antrenamentul de la nivelul 1 are tabla pregătită (mutări clare pentru fiecare pas).
            bool tutorial = _level == 1 && !Data.tutorialDone;
            board.Build(_config, new Vector2(0f, boardBottomY), Random.Range(1, int.MaxValue), tutorial ? (System.Func<Vector2Int, int>)TutorialBoard.ColorAt : null);

            if (!_playfieldBuilt)
            {
                playfield.Build(board.Width * 0.5f, board.Bottom, board.Top, board.Top + 0.35f);
                mascot.transform.position = playfield.JarCenter + (Vector3)mascotOffset;
                _playfieldBuilt = true;
            }
            playfield.ApplyTheme(chapter);
            mascot.ApplyCostume();

            candies.Clear();
            jar.ResetJar(_config.Ingredients);
            _boss = _config.Type == LevelType.Boss ? new BossBattle(BossDefinition.For(_level, _config.Moves)) : null;
            _bossTurnPending = false;
            _goals = new LevelGoals(_config, board.FrostingLeft, _boss?.Health ?? 0, board.Obstacles);
            _moves = _config.Moves;
            _state = State.Playing;
            _endTimer = 0f;
            _idleTime = 0f;
            _popChain = 0;
            _continuedWithAd = false;
            _warnedLastMoves = false;
            input.enabled = true;
            CancelBomb();
            RefreshBoosters();

            menu.Hide();
            map.Hide();
            result.Hide();
            dialog.Hide();
            SetGameplayUi(true);
            hud.HideTransient();
            hud.SetLevel(_level, _config.IsBoss);
            // Sugar Rush: pe timp, fără mutări; cronometrul stă în discul mutărilor.
            _bonusTime = _config.IsBonus ? _config.BonusSeconds : 0f;
            _bonusShown = -1;
            if (_config.IsBonus) _moves = 999;
            hud.SetMoves(_config.IsBonus ? _config.BonusSeconds : _moves, false);
            if (_config.IsBonus) hud.SetMovesCaption(SugarLoc.T("sec"));
            hud.SetBoss(_boss);
            hud.SetGoals(_goals);
            _score = 0;
            _movePoints = 0;
            _scoreDirty = false;
            hud.SetScoreTargets(ScoreRules.TwoStars(_config), ScoreRules.ThreeStars(_config));
            mascot.SetIdle();
            // Boss-ul își face intrarea; tabla se poate atinge după ce se termină.
            _waitingBossIntro = _boss != null;
            if (_boss != null)
            {
                input.enabled = false;
                hud.ShowBossIntro(_boss.Definition);
                if (Audio != null) Audio.Play(SugarSound.Bomb, 0.8f, 0.7f);
                if (Vfx != null) Vfx.Shake(0.25f);
                SugarAudio.Buzz(2);
            }
            else hud.ShowBanner(IntroText(_config.Type));

            // Tutorialul: doar la primul nivel, până la prima mutare reușită.
            hud.SetTutorial(null);
            hintGuide.Hide();
            coach.Hide();
            _tutorialWaiting = false;
            _tutorial = tutorial ? TutorialStep.Match : TutorialStep.None;
            if (tutorial) ShowTutorialStep();
        }

        // ------------------------------------------------------------ Antrenamentul de la nivelul 1

        /// <summary>
        /// Pașii: 3 la fel în linie → 4 la rând fac o rachetă → mută racheta ca să explodeze → obiectivul → mutările →
        /// ajutoarele. Pașii de joc au indiciul cu degetul pe tablă; explicațiile opresc tabla până la „Am înțeles”.
        /// </summary>
        private void ShowTutorialStep()
        {
            hintGuide.Hide();
            switch (_tutorial)
            {
                case TutorialStep.Match:
                    coach.ShowAction(SugarLoc.T("Trage o bomboană ca să faci 3 la fel în linie!"));
                    ShowSwapHint(TutorialBoard.MatchFrom, TutorialBoard.MatchTo);
                    break;
                case TutorialStep.Rocket:
                    coach.ShowAction(SugarLoc.T("Bravo! Acum fă 4 la rând: așa se face o rachetă!"));
                    ShowSwapHint(TutorialBoard.RocketFrom, TutorialBoard.RocketTo);
                    break;
                case TutorialStep.Fire:
                    if (!board.TryFindSpecial(out Vector2Int rocket) || !TryNeighbour(rocket, out Vector2Int neighbour))
                    {
                        NextTutorialStep();
                        return;
                    }
                    coach.ShowAction(SugarLoc.T("Ai o rachetă! Mut-o ca să explodeze și să curețe o linie întreagă."));
                    ShowSwapHint(rocket, neighbour);
                    break;
                case TutorialStep.Goal:
                    coach.ShowInfo(SugarLoc.T("Sus vezi obiectivul nivelului. Îndeplinește-l ca să câștigi!"), hud.GoalArea, true, NextTutorialStep);
                    break;
                case TutorialStep.Moves:
                    coach.ShowInfo(SugarLoc.T("Aici vezi câte mutări mai ai. Fiecare schimb costă o mutare."), hud.MovesArea, true, NextTutorialStep);
                    break;
                case TutorialStep.Boosters:
                    coach.ShowInfo(SugarLoc.T("Ajutoarele te scot din încurcătură: bombă, +5 mutări și amestecare."), boosters.MiddleButton, false, NextTutorialStep);
                    break;
            }
        }

        /// <summary>Indiciul pe tablă: mutarea pregătită, sau cea mai bună mutare dacă tabla s-a schimbat între timp.</summary>
        private void ShowSwapHint(Vector2Int from, Vector2Int to)
        {
            if (!board.IsValidSwap(from, to) && !board.TryFindBestHint(null, out from, out to)) return;
            hintGuide.Show(board.transform.TransformPoint(board.CellCenter(from)), board.transform.TransformPoint(board.CellCenter(to)));
        }

        private bool TryNeighbour(Vector2Int cell, out Vector2Int neighbour)
        {
            foreach (Vector2Int d in new[] { Vector2Int.up, Vector2Int.left, Vector2Int.right, Vector2Int.down })
            {
                neighbour = cell + d;
                if (board.IsValidSwap(cell, neighbour)) return true;
            }
            neighbour = cell;
            return false;
        }

        /// <summary>După o mutare din tutorial (tabla liniștită): racheta apare doar dacă s-a făcut; altfel trecem la explicații.</summary>
        private void AfterTutorialMove()
        {
            switch (_tutorial)
            {
                case TutorialStep.Match: _tutorial = TutorialStep.Rocket; break;
                case TutorialStep.Rocket: _tutorial = board.TryFindSpecial(out _) ? TutorialStep.Fire : TutorialStep.Goal; break;
                default: _tutorial = TutorialStep.Goal; break;
            }
            ShowTutorialStep();
        }

        private void NextTutorialStep()
        {
            _tutorial = _tutorial switch
            {
                TutorialStep.Fire => TutorialStep.Goal,
                TutorialStep.Goal => TutorialStep.Moves,
                TutorialStep.Moves => TutorialStep.Boosters,
                _ => TutorialStep.None,
            };
            if (_tutorial != TutorialStep.None)
            {
                ShowTutorialStep();
                return;
            }
            FinishTutorial();
            hud.ShowBanner(SugarLoc.T("Acum e rândul tău! Atinge scorul."));
        }

        private void FinishTutorial()
        {
            _tutorial = TutorialStep.None;
            _tutorialWaiting = false;
            coach.Hide();
            hintGuide.Hide();
            if (Data.tutorialDone) return;
            Data.tutorialDone = true;
            SugarSave.Save();
        }

        /// <summary>Nivelul s-a terminat sau jucătorul a ieșit: antrenamentul nu mai apare.</summary>
        private void StopTutorial()
        {
            if (_tutorial != TutorialStep.None) FinishTutorial();
            coach.Hide();
        }

        /// <summary>Din Setări: antrenamentul apare din nou la nivelul 1.</summary>
        private void ReplayTutorial()
        {
            Data.tutorialDone = false;
            SugarSave.Save();
            if (_state == State.Menu) OpenMap();
            TryPlayLevel(1);
        }

        private static string IntroText(LevelType type) => type switch
        {
            LevelType.Ingredients => SugarLoc.T("Adu bomboanele jos, în borcan!"),
            LevelType.Collect => SugarLoc.T("Strânge piesele cerute!"),
            LevelType.Frosting => SugarLoc.T("Topește toată glazura!"),
            LevelType.Crates => SugarLoc.T("Sparge toate lăzile!"),
            LevelType.Ice => SugarLoc.T("Sparge toată gheața!"),
            LevelType.Chains => SugarLoc.T("Rupe toate lanțurile!"),
            LevelType.Chocolate => SugarLoc.T("Oprește ciocolata!"),
            LevelType.Specials => SugarLoc.T("Folosește piese speciale!"),
            LevelType.Garden => SugarLoc.T("Udă ghivecele până înfloresc!"),
            LevelType.Defuse => SugarLoc.T("Dezamorsează bombele cu ceas!"),
            LevelType.Bonus => SugarLoc.T("Sugar Rush! Cât mai multe puncte până expiră timpul!"),
            _ => SugarLoc.T("Atinge scorul!"),
        };

        private void RetryLevel()
        {
            SugarSave.LoseLife();
            if (Data.lives > 0) StartLevel(_level);
            else
            {
                OpenMap();
                ShowNoLives();
            }
        }

        private void LeaveToMap()
        {
            SugarSave.LoseLife();
            OpenMap();
        }

        private void Update()
        {
            // Butonul „Înapoi” de pe Android (în Input System apare ca tasta Escape).
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) OnBack();

            // Scorul se afișează o dată pe cadru (nu la fiecare capac), ca textul să nu se reconstruiască degeaba.
            if (_scoreDirty)
            {
                _scoreDirty = false;
                hud.SetScore(_score, true);
            }

            if (_state != State.Playing) return;
            float dt = Time.deltaTime;

            if (_waitingBossIntro)
            {
                if (hud.BossIntroPlaying) return;
                _waitingBossIntro = false;
                input.enabled = true;
                hud.ShowBanner(SugarLoc.T("Învinge Boss-ul!"));
            }

            // Fără mutări rămase (sau cu obiectivele îndeplinite) tabla nu mai primește atingeri: altfel, cât timp
            // animațiile țin tabla ocupată, finalul n-ar mai veni și s-ar putea juca la nesfârșit.
            if (_config.IsBonus) TickBonus(dt);
            bool canTap = (_config.IsBonus ? _bonusTime > 0f : _moves > 0 && !_goals.IsComplete) && !coach.Blocking && !board.TimeBombExpired;
            if (input.enabled != canTap) input.enabled = canTap;

            // Rândul Boss-ului: abia după ce mutarea jucătorului s-a rezolvat complet (tabla liniștită).
            if (_bossTurnPending && !board.Busy)
            {
                _bossTurnPending = false;
                BossTurn();
            }

            // Indiciu după o pauză lungă: două capace care se pot schimba tremură.
            // Pasul următor al antrenamentului, după ce mutarea s-a rezolvat complet.
            if (_tutorialWaiting && !board.Busy)
            {
                _tutorialWaiting = false;
                AfterTutorialMove();
            }

            if (!board.Busy && input.TapTarget == null && _tutorial == TutorialStep.None)
            {
                _idleTime += dt;
                if (_idleTime >= (_level <= 3 ? 4f : idleHintSeconds))
                {
                    _idleTime = 0f;
                    board.ShowHint(_colorWeight);
                }
            }

            // Finalul: obiective îndeplinite → victorie; fără mutări (sau tabla blocată) → înfrângere.
            // Așteptăm să se termine animațiile tablei.
            bool complete = _config.IsBonus ? _bonusTime <= 0f || board.Stuck : _goals.IsComplete;
            bool outOfMoves = !_config.IsBonus && (_moves <= 0 || board.Stuck || board.TimeBombExpired);
            if (board.Busy || (!complete && !outOfMoves))
            {
                _endTimer = 0f;
                return;
            }
            _endTimer += dt;
            if (_endTimer < endDelay) return;
            if (complete) Win();
            else Lose();
        }

        private void OnBack()
        {
            if (UITransitionController.Instance != null && UITransitionController.Instance.IsBusy) return;
            if (QuarryEmpire.AdService.Instance != null && QuarryEmpire.AdService.Instance.IsShowing) return;
            if (dialog.IsOpen)
            {
                dialog.Dismiss();
                return;
            }
            if (welcome.IsOpen)
            {
                welcome.Dismiss();
                return;
            }
            if (leaderboard.IsOpen)
            {
                leaderboard.Hide();
                return;
            }
            if (wheel.IsOpen)
            {
                wheel.Hide();
                return;
            }
            if (wardrobe.IsOpen)
            {
                wardrobe.Hide();
                return;
            }
            switch (_state)
            {
                case State.Playing: ShowPause(); break;
                case State.Map: Go(OpenMenu); break;
                case State.Menu: Application.Quit(); break;
                case State.Result: break; // fereastra de final are butoanele ei
            }
        }

        private void Win()
        {
            _state = State.Finale;
            input.enabled = false;
            CancelBomb();
            hintGuide.Hide();
            hud.SetTutorial(null);
            StopTutorial();

            // Progresul se salvează imediat (dacă aplicația se închide în timpul finalului, nivelul rămâne trecut).
            // Cufărul Boss-ului: monede duble și câte un ajutor din fiecare (Mega-Boss: triple și câte două).
            // Sugar Rush: monedele vin din scor; nu rămân mutări de transformat în finalul dulce.
            if (_config.IsBonus) _moves = 0;
            int coins = _config.IsBonus ? 20 + _score / 100 : 20 + _moves * 5;
            if (_config.IsBoss)
            {
                int bonus = _config.IsMegaBoss ? 2 : 1;
                coins *= _config.IsMegaBoss ? 3 : 2;
                Data.bombs += bonus;
                Data.extraMoves += bonus;
                Data.shuffles += bonus;
            }
            // Weekend dulce: monede duble.
            coins *= SugarEvents.CoinMultiplier(System.DateTime.Now);
            Data.coins += coins;
            if (_level >= Data.level) Data.level = _level + 1;
            SugarSave.SetStars(_level, 1);
            SugarSave.Save();
            RefreshBoosters();
            mascot.SetHappy();
            StartCoroutine(FinaleRoutine(coins));
        }

        /// <summary>
        /// Finalul dulce: fiecare mutare rămasă devine un capac dungat (+puncte), apoi toate explodează pe rând.
        /// Stelele se dau după scorul final.
        /// </summary>
        private IEnumerator FinaleRoutine(int coins)
        {
            if (_moves > 0) hud.ShowBanner(SugarLoc.T("Final dulce!"));
            while (board.Busy) yield return null;
            yield return new WaitForSeconds(0.4f);

            int converted = 0;
            while (_moves > 0)
            {
                _moves--;
                hud.SetMoves(_moves, true);
                AddScore(ScoreRules.FinaleMove);
                if (converted < 15 && board.TryMakeRandomSpecial(out Vector3 position))
                {
                    converted++;
                    hud.ShowPoints(position, ScoreRules.FinaleMove, QuarryEmpire.UiRoot.Theme.AccentColor);
                    yield return new WaitForSeconds(0.14f);
                }
            }
            yield return new WaitForSeconds(0.3f);

            for (int guard = 0; guard < 60; guard++)
            {
                while (board.Busy) yield return null;
                if (!board.DetonateNextSpecial()) break;
                yield return new WaitForSeconds(0.12f);
            }
            while (board.Busy) yield return null;
            yield return new WaitForSeconds(0.8f); // bomboanele ieșite mai cad în borcan

            _state = State.Result;
            int stars = ScoreRules.Stars(_score, _config);
            if (stars == 3)
            {
                Data.coins += 20;
                coins += 20;
            }
            SugarSave.SetStars(_level, stars);
            bool record = SugarSave.SetBestScore(_level, _score);
            SugarDaily.AddProgress(MissionKind.WinLevels, 1);
            SugarDaily.AddProgress(MissionKind.EarnStars, stars);
            SugarSave.Save();
            if (record) SugarLeaderboard.Ensure().Submit();

            if (Audio != null) Audio.Play(SugarSound.Win);
            SugarAudio.Buzz(1);
            if (Vfx != null) Vfx.Confetti(Vector3.zero, 9.2f, 10.5f);
            if (_config.IsBoss)
                hud.ShowBanner(SugarLoc.T(_config.IsMegaBoss ? "Mega-Boss învins! +2 la fiecare ajutor" : "Boss învins! +1 la fiecare ajutor"));
            // Capitol terminat (Boss învins prima dată): cufărul capitolului apare pe hartă.
            int chapter = ChapterTheme.ChapterOf(_level);
            bool chest = _config.IsBoss && chapter == Data.chaptersRewarded;
            bool rating = !chest && ShouldAskRating(_level);
            SugarNotifications.AskPermissionOnce();
            if (SugarCloudSave.Instance != null) SugarCloudSave.Instance.RequestUpload();
            result.ShowWin(_level, stars, coins, () => Go(() =>
            {
                OpenMap();
                if (chest) ShowChapterChest(chapter);
                else if (rating) AskRating();
            }));
        }

        /// <param name="inPopup">Punctele intră în cifra care sare la finalul mutării (bomboanele din borcan, nu).</param>
        private void AddScore(int points, bool inPopup = true)
        {
            _score += points;
            if (inPopup) _movePoints += points;
            _scoreDirty = true;
        }

        private void Lose()
        {
            bool exploded = board.TimeBombExpired;
            _state = State.Result;
            input.enabled = false;
            CancelBomb();
            StopTutorial();
            if (Audio != null) Audio.Play(SugarSound.Lose);
            SugarAudio.Buzz(1);
            mascot.SetSad();
            bool canContinue = !_continuedWithAd && !board.Stuck && !exploded;
            System.Action onAd = canContinue ? () => SugarUi.RewardedAd("extra_moves", ContinueWithMoves) : null;
            result.ShowLose(SugarLoc.T(exploded ? "Bomba cu ceas a explodat!" : "Mai aveai puțin!"), () => Go(RetryLevel), onAd, () => Go(LeaveToMap));
        }

        private void ContinueWithMoves()
        {
            _continuedWithAd = true;
            _moves += extraMoves;
            _state = State.Playing;
            _endTimer = 0f;
            _warnedLastMoves = false;
            input.enabled = true;
            result.Hide();
            mascot.SetIdle();
            hud.SetMoves(_moves, true);
        }

        // ------------------------------------------------------------ Pauză și setări

        private void ShowPause()
        {
            if (_state != State.Playing || dialog.IsOpen) return;
            Time.timeScale = 0f;
            CancelBomb();
            ShowSettings(true);
        }

        private void ShowMapSettings() => ShowSettings(false);

        private void ShowSettings(bool inGame)
        {
            UiThemeColors(out Color positive, out Color ad, out Color panel);
            Color accent = QuarryEmpire.UiRoot.Theme.AccentColor;
            var buttons = new List<DialogButton>();
            if (inGame)
            {
                buttons.Add(new DialogButton(SugarLoc.T("CONTINUĂ"), positive, Resume));
                buttons.Add(new DialogButton(SugarLoc.T("RELUARE NIVEL"), accent, () =>
                {
                    Time.timeScale = 1f;
                    Go(RetryLevel);
                }));
                buttons.Add(new DialogButton(SugarLoc.T("ÎNAPOI LA HARTĂ"), accent, () =>
                {
                    Time.timeScale = 1f;
                    Go(LeaveToMap);
                }));
            }
            buttons.Add(Toggle(Data.sound ? "SUNET: PORNIT" : "SUNET: OPRIT", () => Data.sound = !Data.sound, inGame));
            buttons.Add(Toggle(Data.music ? "MUZICĂ: PORNITĂ" : "MUZICĂ: OPRITĂ", () =>
            {
                Data.music = !Data.music;
                if (Audio != null) Audio.ApplySettings();
            }, inGame));
            buttons.Add(Toggle(Data.vibration ? "VIBRAȚII: PORNITE" : "VIBRAȚII: OPRITE", () => Data.vibration = !Data.vibration, inGame));
            buttons.Add(Toggle(Data.reducedMotion ? "MIȘCARE REDUSĂ: DA" : "MIȘCARE REDUSĂ: NU", () => Data.reducedMotion = !Data.reducedMotion, inGame));
            buttons.Add(Toggle(SugarQuality.LabelKey, SugarQuality.Cycle, inGame));
            buttons.Add(Toggle(Data.notifications ? "NOTIFICĂRI: PORNITE" : "NOTIFICĂRI: OPRITE", () => Data.notifications = !Data.notifications, inGame));
            if (SugarAds.RealAds)
                buttons.Add(Toggle(Data.adsConsent == 1 ? "RECLAME PERSONALIZATE: DA" : "RECLAME PERSONALIZATE: NU", () => SetAdsConsent(Data.adsConsent != 1), inGame));
            buttons.Add(new DialogButton(SugarLoc.T("LIMBA") + ": " + SugarLoc.NativeName(SugarLoc.Current).ToUpperInvariant(), panel,
                () => ShowLanguages(inGame), false));
            if (!inGame)
            {
                buttons.Add(new DialogButton(SugarLoc.F("NICKNAME: {0}", Data.nickname), panel, () => welcome.Show(false, SetNickname)));
                buttons.Add(new DialogButton(SugarLoc.T("REVEZI TUTORIALUL"), panel, ReplayTutorial));
                buttons.Add(new DialogButton(SugarLoc.T("ÎNCHIDE"), positive, null));
            }

            string message = (inGame ? SugarLoc.T("Nivelul costă o viață.") + "\n" : "") + SugarLoc.F("Versiunea {0}", ErrorReporter.BuildId);
            dialog.Show(SugarLoc.T(inGame ? "PAUZĂ" : "SETĂRI"), message, buttons.ToArray());
            if (inGame) dialog.DismissAction = Resume;
        }

        /// <summary>Lista limbilor (fiecare scrisă în limba ei); alegerea traduce imediat tot jocul.</summary>
        private void ShowLanguages(bool inGame)
        {
            UiThemeColors(out Color positive, out Color ad, out Color panel);
            var buttons = new List<DialogButton>();
            foreach (SugarLanguage language in SugarLoc.Languages)
            {
                bool current = language == SugarLoc.Current;
                buttons.Add(new DialogButton(SugarLoc.NativeName(language), current ? positive : panel, () =>
                {
                    Data.language = SugarLoc.Code(language);
                    SugarSave.Save();
                    RefreshTexts();
                    ShowSettings(inGame);
                }, false));
            }
            buttons.Add(new DialogButton(SugarLoc.T("ÎNAPOI"), QuarryEmpire.UiRoot.Theme.AccentColor, () => ShowSettings(inGame), false));
            dialog.Show(SugarLoc.T("LIMBA"), null, buttons.ToArray());
            dialog.DismissAction = () => ShowSettings(inGame);
        }

        private DialogButton Toggle(string label, System.Action change, bool inGame) =>
            new(SugarLoc.T(label), QuarryEmpire.UiRoot.Theme.PanelColor, () =>
            {
                change();
                SugarSave.Save();
                ShowSettings(inGame);
            }, false);

        private void Resume()
        {
            Time.timeScale = 1f;
            _idleTime = 0f;
        }

        private void RefreshTexts()
        {
            SugarFonts.Apply();
            if (_state == State.Playing || _state == State.Result) hud.SetLevel(_level, _config.IsBoss);
            if ((_state == State.Playing || _state == State.Result) && _boss != null) hud.SetBoss(_boss);
            if (map.IsOpen) map.Refresh();
            if (menu.IsOpen) menu.Show();
            if (_tutorial != TutorialStep.None) ShowTutorialStep();
        }

        private static void UiThemeColors(out Color positive, out Color ad, out Color panel)
        {
            QuarryEmpire.UiTheme theme = QuarryEmpire.UiRoot.Theme;
            positive = theme.PositiveColor;
            ad = theme.AdColor;
            panel = theme.PanelColor;
        }

        // ------------------------------------------------------------ Ajutoare

        private void RefreshBoosters() => boosters.SetCounts(Data.bombs, Data.extraMoves, Data.shuffles);

        private static int Count(Booster booster) => booster switch
        {
            Booster.Bomb => Data.bombs,
            Booster.ExtraMoves => Data.extraMoves,
            _ => Data.shuffles,
        };

        private static void AddBooster(Booster booster, int amount)
        {
            switch (booster)
            {
                case Booster.Bomb: Data.bombs += amount; break;
                case Booster.ExtraMoves: Data.extraMoves += amount; break;
                default: Data.shuffles += amount; break;
            }
            SugarSave.Save();
        }

        private void OnBooster(Booster booster)
        {
            if (_state != State.Playing || dialog.IsOpen) return;
            SugarUi.Click();

            // A doua apăsare pe bombă anulează țintirea.
            if (booster == Booster.Bomb && input.TapTarget != null)
            {
                CancelBomb();
                return;
            }
            if (board.Busy) return;
            if (Count(booster) <= 0)
            {
                ShowBuy(booster);
                return;
            }

            switch (booster)
            {
                case Booster.Bomb:
                    boosters.SetTargeting(true);
                    input.TapTarget = cell =>
                    {
                        boosters.SetTargeting(false);
                        _popChain = 0;
                        if (!board.Blast(cell)) return;
                        AddBooster(Booster.Bomb, -1);
                        RefreshBoosters();
                    };
                    break;

                case Booster.ExtraMoves:
                    AddBooster(Booster.ExtraMoves, -1);
                    // În Sugar Rush ajutorul dă secunde în loc de mutări.
                    if (_config.IsBonus) _bonusTime += extraMoves;
                    else _moves += extraMoves;
                    _endTimer = 0f;
                    hud.SetMoves(_config.IsBonus ? Mathf.CeilToInt(_bonusTime) : _moves, true);
                    hud.ShowWord("+" + extraMoves, Palette.Tile(2));
                    if (Audio != null) Audio.Play(SugarSound.SpecialCreated);
                    mascot.Cheer();
                    break;

                case Booster.Shuffle:
                    if (!board.ShuffleBooster()) return;
                    AddBooster(Booster.Shuffle, -1);
                    break;
            }
            RefreshBoosters();
        }

        private void CancelBomb()
        {
            input.TapTarget = null;
            boosters.SetTargeting(false);
        }

        private void ShowBuy(Booster booster)
        {
            UiThemeColors(out Color positive, out Color ad, out Color panel);
            string name = booster switch
            {
                Booster.Bomb => SugarLoc.T("Bombă"),
                Booster.ExtraMoves => SugarLoc.T("+5 mutări"),
                _ => SugarLoc.T("Amestecă"),
            };
            dialog.Show(name, SugarLoc.F("Cumpără cu {0} monede?", boosterPrice) + "\n" + SugarLoc.F("Ai {0} monede", Data.coins),
                new DialogButton(SugarLoc.T("CUMPĂRĂ"), positive, () =>
                {
                    if (Data.coins < boosterPrice)
                    {
                        if (Audio != null) Audio.Play(SugarSound.Invalid);
                        dialog.Show(SugarLoc.T("Nu ai destule monede"), null,
                            new DialogButton(SugarLoc.T("GRATIS (reclamă)"), ad, () => SugarUi.RewardedAd("booster", () => Grant(booster))),
                            new DialogButton(SugarLoc.T("ÎNCHIDE"), panel, null));
                        return;
                    }
                    Data.coins -= boosterPrice;
                    if (Audio != null) Audio.Play(SugarSound.Coin);
                    Grant(booster);
                }),
                new DialogButton(SugarLoc.T("GRATIS (reclamă)"), ad, () => SugarUi.RewardedAd("booster", () => Grant(booster))),
                new DialogButton(SugarLoc.T("ÎNCHIDE"), panel, null));
        }

        private void Grant(Booster booster)
        {
            AddBooster(booster, 1);
            RefreshBoosters();
        }

        // ------------------------------------------------------------ Evenimente de pe tablă

        private void OnTouched() => _idleTime = 0f;

        private void OnMoveMade()
        {
            if (_state != State.Playing) return;
            if (!_config.IsBonus) _moves = Mathf.Max(0, _moves - 1);
            _idleTime = 0f;
            _popChain = 0;
            if (!_config.IsBonus) hud.SetMoves(_moves, true);
            if (Audio != null) Audio.Play(SugarSound.Swap);
            if (_boss != null) _bossTurnPending = true;
            if (_moves <= 0) input.enabled = false; // ultima mutare: nicio atingere în plus până la final

            if (_tutorial is TutorialStep.Match or TutorialStep.Rocket or TutorialStep.Fire)
            {
                hintGuide.Hide();
                _tutorialWaiting = true;
            }
            if (_moves == 3 && !_warnedLastMoves && !_config.IsBonus)
            {
                _warnedLastMoves = true;
                hud.ShowBanner(SugarLoc.T("Ultimele mutări!"));
            }
        }

        private void OnInvalidTap()
        {
            if (Audio != null) Audio.Play(SugarSound.Invalid, 0.7f);
            SugarAudio.Buzz(0);
        }

        /// <summary>
        /// Efectele cresc cu mărimea grupului: 3–4 pocnet mic; 5–6 explozie medie + puls de ecran;
        /// 7–9 explozie puternică + undă de șoc; 10+ explozie curcubeu + impuls de cameră.
        /// </summary>
        private void OnGroupBlasted(int size, Vector3 center, int color)
        {
            if (Vfx != null) Vfx.GroupBurst(center, size, color);
            if (Audio != null) Audio.Play(size >= 7 ? SugarSound.Wrapped : SugarSound.Click, size >= 7 ? 0.7f : 0.5f, 1f + Mathf.Min(size, 12) * 0.03f);
            SugarAudio.Buzz(size >= 10 ? 2 : size >= 5 ? 1 : 0);
            if (size >= 7) mascot.Cheer(size >= 10 ? 2f : 1.5f);
        }

        private void OnComboTriggered(Vector3 position)
        {
            if (Vfx != null) Vfx.ComboFlash(position);
            if (Audio != null) Audio.Play(SugarSound.Rainbow, 1f, 0.9f);
            hud.ShowWord(SugarLoc.T("Combo!"), Palette.Tile(3));
            SugarAudio.Buzz(2);
            mascot.Cheer(2f);
        }

        private void OnTileCleared(int color, TileKind kind, Vector3 position)
        {
            if (color >= 0)
            {
                if (Vfx != null) Vfx.TilePopped(color, position);
                // Notele urcă pe parcursul mutării: lanțurile lungi sună ca o melodie.
                if (Audio != null) Audio.PlayPop(color, _popChain / 2);
            }
            _popChain++;
            // Bomboanele din culorile cerute „cad” spre borcan și se adună acolo (doar decor; obiectivul e deja numărat).
            if (color >= 0 && IsCollectColor(color) && _decorCounter++ % 2 == 0) candies.SpawnDecorative(position, Palette.TileLight(color));
            mascot.LookAt(position);
            if (Time.unscaledTime - _lastBuzz > 0.09f)
            {
                _lastBuzz = Time.unscaledTime;
                SugarAudio.Buzz(0);
            }
        }

        private int _decorCounter;

        private bool IsCollectColor(int color)
        {
            if (_goals == null) return false;
            foreach (GoalItem item in _goals.Items)
                if (item.Kind == GoalKind.Collect && item.Color == color) return true;
            return false;
        }

        private void OnSpecialActivated(TileKind kind, Vector3 position, int color, List<Vector3> targets)
        {
            switch (kind)
            {
                case TileKind.RocketH:
                case TileKind.RocketV:
                    if (Vfx != null) Vfx.RocketLaunch(position, kind == TileKind.RocketH, board.Width + 0.6f);
                    if (Audio != null) Audio.Play(SugarSound.Striped);
                    SugarAudio.Buzz(1);
                    break;
                case TileKind.Bomb:
                    if (Vfx != null) Vfx.WrapBlast(position, color, 1.6f);
                    if (Audio != null) Audio.Play(SugarSound.Bomb);
                    SugarAudio.Buzz(2);
                    break;
                case TileKind.Rainbow:
                    if (Vfx != null) Vfx.RainbowBlast(position, targets);
                    if (Audio != null) Audio.Play(SugarSound.Rainbow);
                    SugarAudio.Buzz(1);
                    break;
            }
            mascot.Cheer(1.5f);
        }

        private void OnSpecialCreated(TileKind kind, Vector3 position)
        {
            if (Vfx != null) Vfx.SpecialCreated(position);
            if (Audio != null) Audio.Play(SugarSound.SpecialCreated);
            mascot.Cheer();
        }

        private void OnIceBroken(Vector3 position)
        {
            if (Vfx != null) Vfx.IceShatter(position);
            if (Audio != null) Audio.Play(SugarSound.Ice);
        }

        private void OnBiscuitHit(Vector3 position, bool broken)
        {
            if (Vfx != null) Vfx.BiscuitCrumbs(position, broken);
            if (Audio != null) Audio.Play(SugarSound.Biscuit, 1f, broken ? 0.85f : 1.1f);
        }

        /// <summary>Scorul și obiectivele vin direct din pașii tablei (aceleași reguli ca în simulatorul din teste).</summary>
        private void OnStepPlayed(BoardStep step)
        {
            if (_state != State.Playing && _state != State.Finale) return;
            int points = ScoreRules.PointsFor(step);
            if (points > 0) AddScore(points);
            if (_state == State.Playing) TrackMissions(step);
            bool changed = _goals.Apply(step);
            changed |= _goals.OnScore(_score);
            if (_boss != null && _state == State.Playing) changed |= DamageBoss(BossRules.DamageFor(step));
            if (changed) hud.RefreshGoals(true);
        }

        // ------------------------------------------------------------ Lupta cu Boss-ul

        /// <summary>Daunele unui pas (aceleași reguli ca în simulator). Întoarce true dacă obiectivul s-a schimbat.</summary>
        private bool DamageBoss(int damage)
        {
            if (damage <= 0 || _boss.Defeated) return false;
            bool phaseChanged = _boss.Damage(damage);
            hud.RefreshBoss(true);
            if (Time.unscaledTime - _lastBossHitSound > 0.15f)
            {
                _lastBossHitSound = Time.unscaledTime;
                if (Audio != null) Audio.Play(SugarSound.Biscuit, 0.6f, 0.6f + _boss.HealthFraction * 0.4f);
            }
            if (_boss.Defeated)
            {
                hud.ShowWord(SugarLoc.T("Boss învins!"), Palette.Tile(3));
                if (Audio != null) Audio.Play(SugarSound.Bomb, 1f, 0.8f);
                if (Vfx != null)
                {
                    Vfx.Impulse(0.5f);
                    Vfx.Shake(0.4f);
                }
                SugarAudio.Buzz(2);
                mascot.Cheer(2.5f);
            }
            else if (phaseChanged)
            {
                // Mega-Boss: intră în faza următoare (atacuri noi, mai dese).
                hud.ShowBanner(SugarLoc.F("Faza {0}!", _boss.PhaseIndex + 1));
                if (Audio != null) Audio.Play(SugarSound.Rainbow, 1f, 0.7f);
                if (Vfx != null) Vfx.Shake(0.35f);
                SugarAudio.Buzz(2);
            }
            return _goals.OnBossHealth(_boss.Health);
        }

        /// <summary>După fiecare mutare: numărătoarea scade; la zero, Boss-ul atacă (anunțat cu o mutare înainte).</summary>
        private void BossTurn()
        {
            if (_boss == null || _boss.Defeated || _goals.IsComplete || _moves <= 0) return;
            BossAttack? attack = _boss.EndOfMove();
            if (!attack.HasValue)
            {
                hud.RefreshBoss(false);
                if (_boss.AttackWarning) hud.ShowBanner(SugarLoc.T("Atac la mutarea următoare!"));
                return;
            }
            board.ApplyBossAttack(attack.Value, _boss.Phase.Strength);
            hud.BossAttacked();
            hud.ShowWord(SugarLoc.T(attack.Value switch
            {
                BossAttack.ChocolateSpread => "Ploaie de ciocolată!",
                BossAttack.Freeze => "Înghețare!",
                BossAttack.Lock => "Lanțuri!",
                BossAttack.TimeBomb => "Bombe cu ceas!",
                _ => "Amestecă tabla!",
            }), QuarryEmpire.UiRoot.Theme.NegativeColor);
            if (Audio != null) Audio.Play(SugarSound.Wrapped, 0.9f, 0.6f);
            if (Vfx != null) Vfx.Shake(0.3f);
            SugarAudio.Buzz(1);
            mascot.SetSad();
            StartCoroutine(MascotRecovers());
        }

        private IEnumerator MascotRecovers()
        {
            yield return new WaitForSeconds(1.2f);
            if (_state == State.Playing) mascot.SetIdle();
        }

        private void OnBossAttackLanded(Vector3 position, PieceInfo piece)
        {
            if (Vfx == null) return;
            if (piece.Kind == TileKind.Chocolate) Vfx.BiscuitCrumbs(position, false);
            else if (piece.Ice > 0) Vfx.IceShatter(position);
            else Vfx.SpecialCreated(position);
        }

        private void OnFrostingMelted(Vector3 position, int layersLeft)
        {
            if (Vfx != null) Vfx.IceShatter(position);
            if (Audio != null) Audio.Play(SugarSound.Ice, 0.6f, 1.25f);
        }

        /// <summary>O bomboană a ieșit de pe tablă: devine o bomboană cu fizică ce cade pe pâlnie în borcan.</summary>
        private void OnIngredientExited(Vector3 position, int color)
        {
            candies.SpawnFalling(position, color);
            if (Vfx != null) Vfx.SpecialCreated(position);
            if (Audio != null) Audio.Play(SugarSound.Star, 1f, 1.1f);
            SugarAudio.Buzz(1);
            mascot.Cheer(2f);
        }

        // ------------------------------------------------------------ Mecanicile ținuturilor

        private float _bonusTime;
        private int _bonusShown = -1;

        /// <summary>Sugar Rush: cronometrul scade (oprit în pauză); la ultimele 5 secunde se aude un „tic”.</summary>
        private void TickBonus(float dt)
        {
            if (_bonusTime <= 0f || _waitingBossIntro) return;
            _bonusTime = Mathf.Max(0f, _bonusTime - dt);
            int shown = Mathf.CeilToInt(_bonusTime);
            if (shown == _bonusShown) return;
            _bonusShown = shown;
            hud.SetMoves(shown, shown <= 5);
            if (shown <= 5 && shown > 0 && Audio != null) Audio.Play(SugarSound.Click, 0.8f, 1.4f);
            if (shown == 0) hud.ShowBanner(SugarLoc.T("Timpul a expirat!"));
        }

        private void OnPotWatered(Vector3 position)
        {
            if (Vfx != null) Vfx.WaterSplash(position);
            if (Audio != null) Audio.Play(SugarSound.Ice, 0.6f, 1.3f);
        }

        private void OnPotBloomed(Vector3 position)
        {
            if (Vfx != null) Vfx.FlowerBloom(position);
            if (Audio != null) Audio.Play(SugarSound.Star, 1f, 1.2f);
            SugarAudio.Buzz(1);
            mascot.Cheer(1.5f);
        }

        private void OnTimeBombExploded(Vector3 position)
        {
            if (Vfx != null)
            {
                Vfx.WrapBlast(position, 0, 2.2f);
                Vfx.Shake(0.5f);
            }
            if (Audio != null) Audio.Play(SugarSound.Bomb, 1f, 0.7f);
            SugarAudio.Buzz(2);
            mascot.SetSad();
        }

        private void OnPortalUsed(Vector3 entry, Vector3 exit)
        {
            if (Vfx != null)
            {
                Vfx.SpecialCreated(entry);
                Vfx.SpecialCreated(exit);
            }
        }

        private void OnChocolateGrew(Vector3 position)
        {
            if (Vfx != null) Vfx.BiscuitCrumbs(position, false);
            if (Audio != null) Audio.Play(SugarSound.Biscuit, 0.8f, 0.7f);
        }

        private void OnShuffled()
        {
            if (Audio != null) Audio.Play(SugarSound.Shuffle);
        }

        /// <summary>La final de mutare: cuvânt de laudă după câte capace au dispărut.</summary>
        private void OnMoveResolved(int count, Vector3 center)
        {
            if (_state != State.Playing && _state != State.Finale) return;
            // Punctele mutării sar din mijlocul pieselor eliminate.
            if (_movePoints > 0) hud.ShowPoints(center, _movePoints, count >= 9 ? QuarryEmpire.UiRoot.Theme.AccentColor : Color.white);
            _movePoints = 0;
            string word = null;
            Color color = Color.white;
            if (count >= 20) { word = "Fabulos!"; color = Palette.Tile(4); }
            else if (count >= 14) { word = "Super!"; color = Palette.Tile(3); }
            else if (count >= 9) { word = "Delicios!"; color = Palette.Tile(0); }
            else if (count >= 6) { word = "Dulce!"; color = Palette.Tile(2); }
            if (word == null) return;
            hud.ShowWord(SugarLoc.T(word), Color.Lerp(color, Color.white, 0.25f));
            mascot.Cheer(count >= 14 ? 2f : 1f);
        }

        private void OnCandyEntered(Vector3 position, Color color)
        {
            if (Vfx != null) Vfx.JarSparkle(position, color);
            if (Audio != null) Audio.Play(SugarSound.Candy, 0.5f);
            mascot.LookAt(position);
        }
    }
}
