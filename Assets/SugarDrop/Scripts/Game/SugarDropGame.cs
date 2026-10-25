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
        private bool _tutorialActive;
        private int _popChain;
        private int _score;
        private int _movePoints;
        private bool _scoreDirty;
        private float _lastBuzz;

        private static SugarSaveData Data => SugarSave.Data;
        private static SugarVfx Vfx => SugarVfx.Instance;
        private static SugarAudio Audio => SugarAudio.Instance;

        // ------------------------------------------------------------ Pornire

        private void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            SugarSave.Load();
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
            board.InvalidSwap += OnInvalidSwap;
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
            jar.CandyEntered += OnCandyEntered;
            input.Touched += OnTouched;
            hud.PauseClicked += ShowPause;
            boosters.Clicked += OnBooster;
            map.PlayClicked += TryPlay;
            map.LevelClicked += TryPlayLevel;
            map.SettingsClicked += ShowMapSettings;
            menu.PlayClicked += OpenMapAnimated;
        }

        private void OnDisable()
        {
            board.MoveMade -= OnMoveMade;
            board.InvalidSwap -= OnInvalidSwap;
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
            jar.CandyEntered -= OnCandyEntered;
            input.Touched -= OnTouched;
            hud.PauseClicked -= ShowPause;
            boosters.Clicked -= OnBooster;
            map.PlayClicked -= TryPlay;
            map.LevelClicked -= TryPlayLevel;
            map.SettingsClicked -= ShowMapSettings;
            menu.PlayClicked -= OpenMapAnimated;
        }

        private void Start()
        {
            _state = State.Menu;
            input.enabled = false;
            result.Hide();
            map.Hide();
            menu.Show();
            SetGameplayUi(false);
            RefreshBoosters();
        }

        /// <summary>HUD-ul și ajutoarele se ascund cât timp meniul sau harta acoperă ecranul (mai puțin de desenat).</summary>
        private void SetGameplayUi(bool visible)
        {
            hud.gameObject.SetActive(visible);
            boosters.gameObject.SetActive(visible);
        }

        /// <summary>Schimbă ecranul sub o tranziție (care blochează și apăsările repetate).</summary>
        private static void Go(System.Action action) => UITransitionController.Run(action);

        private void OpenMapAnimated() => Go(OpenMap);

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
            if (_state != State.Map || dialog.IsOpen) return;
            level = Mathf.Clamp(level, 1, Mathf.Max(1, Data.level));
            SugarSave.UpdateLives();
            if (Data.lives <= 0)
            {
                ShowNoLives();
                return;
            }
            Go(() => StartLevel(level));
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
            board.Build(_config, new Vector2(0f, boardBottomY), Random.Range(1, int.MaxValue));

            if (!_playfieldBuilt)
            {
                playfield.Build(board.Width * 0.5f, board.Bottom, board.Top, board.Top + 0.35f);
                mascot.transform.position = playfield.JarCenter + (Vector3)mascotOffset;
                _playfieldBuilt = true;
            }

            candies.Clear();
            jar.ResetJar(_config.Ingredients);
            _goals = new LevelGoals(_config, board.FrostingLeft);
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
            hud.SetMoves(_moves, false);
            hud.SetGoals(_goals);
            _score = 0;
            _movePoints = 0;
            _scoreDirty = false;
            hud.SetScoreTargets(ScoreRules.TwoStars(_config), ScoreRules.ThreeStars(_config));
            mascot.SetIdle();
            hud.ShowBanner(_config.IsBoss ? SugarLoc.T("Nivel Boss!") : IntroText(_config.Type));

            // Tutorialul: doar la primul nivel, până la prima mutare reușită.
            _tutorialActive = _level == 1 && !Data.tutorialDone && board.TryFindBestHint(null, out _, out _);
            if (_tutorialActive && board.TryFindBestHint(null, out Vector2Int a, out Vector2Int b))
            {
                hud.SetTutorial(SugarLoc.T("Glisează un capac ca să aliniezi 3 la fel!"));
                hintGuide.Show(board.CellCenter(a), board.CellCenter(b));
            }
            else
            {
                hud.SetTutorial(null);
                hintGuide.Hide();
            }
        }

        private static string IntroText(LevelType type) => type switch
        {
            LevelType.Ingredients => SugarLoc.T("Adu bomboanele jos, în borcan!"),
            LevelType.Collect => SugarLoc.T("Strânge piesele cerute!"),
            LevelType.Frosting => SugarLoc.T("Topește toată glazura!"),
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

            // Indiciu după o pauză lungă: două capace care se pot schimba tremură.
            if (!board.Busy && input.TapTarget == null && !_tutorialActive)
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
            bool complete = _goals.IsComplete;
            bool outOfMoves = _moves <= 0 || board.Stuck;
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

            // Progresul se salvează imediat (dacă aplicația se închide în timpul finalului, nivelul rămâne trecut).
            int coins = 20 + _moves * 5;
            if (_config.IsBoss)
            {
                coins *= 2;
                Data.bombs++;
                Data.extraMoves++;
                Data.shuffles++;
            }
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
            SugarSave.Save();

            if (Audio != null) Audio.Play(SugarSound.Win);
            SugarAudio.Buzz(1);
            if (Vfx != null) Vfx.Confetti(Vector3.zero, 9.2f, 10.5f);
            if (_config.IsBoss) hud.ShowBanner(SugarLoc.T("Boss învins! +1 la fiecare ajutor"));
            result.ShowWin(_level, stars, coins, () => Go(OpenMap));
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
            _state = State.Result;
            input.enabled = false;
            CancelBomb();
            if (Audio != null) Audio.Play(SugarSound.Lose);
            SugarAudio.Buzz(1);
            mascot.SetSad();
            bool canContinue = !_continuedWithAd && !board.Stuck;
            System.Action onAd = canContinue ? () => SugarUi.RewardedAd("extra_moves", ContinueWithMoves) : null;
            result.ShowLose(SugarLoc.T("Mai aveai puțin!"), () => Go(RetryLevel), onAd, () => Go(LeaveToMap));
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
            buttons.Add(Toggle("LIMBA: ROMÂNĂ", () =>
            {
                Data.language = SugarLoc.IsEnglish ? "ro" : "en";
                RefreshTexts();
            }, inGame));
            if (!inGame) buttons.Add(new DialogButton(SugarLoc.T("ÎNCHIDE"), positive, null));

            string message = inGame ? SugarLoc.T("Nivelul costă o viață.") : null;
            dialog.Show(SugarLoc.T(inGame ? "PAUZĂ" : "SETĂRI"), message, buttons.ToArray());
            if (inGame) dialog.DismissAction = Resume;
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
            if (_state == State.Playing || _state == State.Result) hud.SetLevel(_level, _config.IsBoss);
            if (map.IsOpen) map.Refresh();
            if (menu.IsOpen) menu.Show();
            if (_tutorialActive) hud.SetTutorial(SugarLoc.T("Glisează un capac ca să aliniezi 3 la fel!"));
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
                    _moves += extraMoves;
                    _endTimer = 0f;
                    hud.SetMoves(_moves, true);
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
            _moves = Mathf.Max(0, _moves - 1);
            _idleTime = 0f;
            _popChain = 0;
            hud.SetMoves(_moves, true);
            if (Audio != null) Audio.Play(SugarSound.Swap);

            if (_tutorialActive)
            {
                _tutorialActive = false;
                Data.tutorialDone = true;
                SugarSave.Save();
                hintGuide.Hide();
                hud.SetTutorial(null);
            }
            if (_moves == 3 && !_warnedLastMoves)
            {
                _warnedLastMoves = true;
                hud.ShowBanner(SugarLoc.T("Ultimele mutări!"));
            }
        }

        private void OnInvalidSwap()
        {
            if (Audio != null) Audio.Play(SugarSound.Invalid, 0.7f);
            SugarAudio.Buzz(0);
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
            mascot.LookAt(position);
            if (Time.unscaledTime - _lastBuzz > 0.09f)
            {
                _lastBuzz = Time.unscaledTime;
                SugarAudio.Buzz(0);
            }
        }

        private void OnSpecialActivated(TileKind kind, Vector3 position, int color, List<Vector3> targets)
        {
            switch (kind)
            {
                case TileKind.StripedRow:
                case TileKind.StripedColumn:
                    if (Vfx != null) Vfx.StripeBlast(position, kind == TileKind.StripedRow, board.Width + 0.6f, color);
                    if (Audio != null) Audio.Play(SugarSound.Striped);
                    SugarAudio.Buzz(1);
                    break;
                case TileKind.Wrapped:
                    if (Vfx != null) Vfx.WrapBlast(position, color);
                    if (Audio != null) Audio.Play(color < 0 ? SugarSound.Bomb : SugarSound.Wrapped);
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
            bool changed = _goals.Apply(step);
            changed |= _goals.OnScore(_score);
            if (changed) hud.RefreshGoals(true);
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
