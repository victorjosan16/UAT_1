using System;
using System.Collections.Generic;
using QuarryEmpire;
using UnityEngine;
using UnityEngine.UI;

namespace SugarDrop
{
    /// <summary>
    /// HUD-ul de sus: nivelul, progresul bomboanelor în borcan (x/obiectiv), mutările rămase și pauza.
    /// Tot aici apar cuvintele de combo („Grozav!”), bannerele și textul tutorialului.
    /// </summary>
    public class SugarHud : MonoBehaviour
    {
        public event Action PauseClicked;
        public event Action SkipClicked;
        private GameObject _skipFinale;
        private UiLabel _livesCount;
        private int _livesShown = -1;
        private UiLabel _skipFinaleLabel;
        /// <summary>Jucătorul a atins butonul Puterii lui Bobo.</summary>
        public event Action PowerClicked;

        // Cardurile de sus (stilul Drop Quest): eticheta mică deasupra numărului.
        private UiLabel _levelCaption;
        private UiLabel _goalCaption;
        private Image _levelFlame;

        // Puterea lui Bobo (dreapta jos): cercul se umple jucând; plin, pulsează.
        private RectTransform _power;
        private Image _powerFill;
        private Image _powerGlow;
        private float _powerValue;

        private UiLabel _level;
        private Image _levelPill;
        private UiLabel _moves;
        private UiLabel _movesCaption;
        private PunchScale _movesPunch;
        /// <summary>De la câte mutări rămase contorul devine roșu și pulsează.</summary>
        public const int LowMoves = 5;
        private bool _lowMoves;
        private RectTransform _goalCard;
        private readonly List<GoalView> _goalViews = new();

        /// <summary>Un obiectiv afișat: iconiță, numărul rămas și bifa.</summary>
        private class GoalView
        {
            public GoalItem Item;
            public RectTransform Root;
            public UiLabel Count;
            public Image Check;
            public PunchScale Punch;
            public int Shown = -1;
        }
        private UiLabel _tutorial;

        private UiLabel _word;
        private float _wordT = -1f;
        private UiLabel _banner;
        private float _bannerT = -1f;

        // Scorul (bară cu praguri de stele) și punctele care apar unde au dispărut capacele.
        private UiLabel _score;
        private UiProgressBar _scoreBar;
        private readonly Image[] _scoreStars = new Image[2];
        private RectTransform _scoreBarRect;
        private int _twoStars, _threeStars;
        private PunchScale _scorePunch;
        private readonly UiLabel[] _popups = new UiLabel[10];
        private readonly float[] _popupT = new float[10];
        private readonly Vector2[] _popupStart = new Vector2[10];
        private int _nextPopup;
        private bool _anyPopup;
        private Camera _camera;

        // Lupta cu Boss-ul: portret, nume, bara de viață, numărătoarea până la atac; plus intrarea Boss-ului.
        private BossBattle _boss;
        private int _bossLook;
        private RectTransform _bossPanel;
        private Image _bossFace;
        private UiLabel _bossName;
        private UiLabel _bossHealth;
        private UiProgressBar _bossBar;
        private Image _bossChip;
        private UiLabel _bossCountdown;
        private UiLabel _bossCaption;
        private PunchScale _bossChipPunch;
        private float _bossHitT = -1f;
        private RectTransform _intro;
        private Image _introDim;
        private Image _introFace;
        private UiLabel _introTitle;
        private UiLabel _introName;
        private float _introT = -1f;
        private const float IntroDuration = 2.2f;

        /// <summary>Intrarea Boss-ului se joacă (tabla așteaptă).</summary>
        public bool BossIntroPlaying => _introT >= 0f;

        private void Awake()
        {
            UiTheme theme = UiRoot.Theme;

            // Sus, trei carduri crem: Nivel (stânga), Obiectiv (centru), Mutări (dreapta); pauza, mică, în colț.
            RectTransform levelCard = SugarUi.Card("LevelCard", transform, out _levelPill);
            UiFactory.Place(levelCard, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -20f), new Vector2(176f, 146f));
            _levelCaption = UiFactory.CreateText("Caption", _levelPill.transform, "", 28, TextAnchor.MiddleCenter, theme.TextDarkColor);
            UiFactory.Place(_levelCaption.RectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(170f, 40f));
            _level = UiFactory.CreateText("Level", _levelPill.transform, "", 58, TextAnchor.MiddleCenter, theme.TextDarkColor);
            UiFactory.Place(_level.RectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(170f, 80f));
            _levelFlame = UiFactory.CreateImage("Flame", levelCard, SugarSprites.Flame, Color.white);
            UiFactory.Place(_levelFlame.rectTransform, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-14f, -6f), new Vector2(54f, 54f));
            _levelFlame.gameObject.AddComponent<UiFlicker>();
            BuildPower(theme);

            RectTransform goalRoot = SugarUi.Card("GoalCard", transform, out Image goalFace);
            UiFactory.Place(goalRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-6f, -20f), new Vector2(500f, 146f));
            _goalCard = (RectTransform)goalFace.transform;
            _goalCaption = UiFactory.CreateText("Caption", _goalCard, "", 26, TextAnchor.MiddleCenter, theme.TextDarkColor);
            UiFactory.Place(_goalCaption.RectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -2f), new Vector2(300f, 36f));

            RectTransform movesRoot = SugarUi.Card("MovesCard", transform, out Image movesFace);
            MovesArea = UiFactory.Place(movesRoot, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-106f, -20f), new Vector2(176f, 146f));
            _movesPunch = movesRoot.gameObject.AddComponent<PunchScale>();
            _movesCaption = UiFactory.CreateText("Caption", movesFace.transform, "", 28, TextAnchor.MiddleCenter, theme.TextDarkColor);
            UiFactory.Place(_movesCaption.RectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(170f, 40f));
            _moves = UiFactory.CreateText("Moves", movesFace.transform, "", 60, TextAnchor.MiddleCenter, theme.TextDarkColor);
            UiFactory.Place(_moves.RectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 4f), new Vector2(170f, 84f));

            Button pause = SugarUi.IconButton("Pause", transform, ProceduralSprites.Pause, theme.PanelColor,
                () =>
                {
                    SugarUi.Click();
                    PauseClicked?.Invoke();
                }, 0.45f);
            UiFactory.Place((RectTransform)pause.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-14f, -16f), new Vector2(82f, 82f));

            // Viețile rămase, mereu la vedere în nivel (jos-stânga, deasupra ajutoarelor).
            Image lifeHeart = UiFactory.CreateImage("Lives", transform, SugarSprites.LifeHeart, Color.white);
            UiFactory.Place(lifeHeart.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(26f, 316f), new Vector2(130f, 130f));
            lifeHeart.raycastTarget = false;
            _livesCount = SugarUi.Outlined("Count", lifeHeart.transform, "", 54, Color.white);
            UiFactory.Stretch(_livesCount.RectTransform);

            // „Sari”: în finalul dulce, după câștig, grăbește animațiile (scorul rămâne același).
            Button skip = SugarUi.Button("SkipFinale", transform, "", QuestStyle.Blue, () => SkipClicked?.Invoke(), 40);
            UiFactory.Place((RectTransform)skip.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 330f), new Vector2(340f, 104f));
            _skipFinale = skip.gameObject;
            _skipFinaleLabel = UiFactory.GetButtonLabel(skip);
            _skipFinale.SetActive(false);

            BuildScore(theme);
            BuildBoss(theme);

            // Textul tutorialului (sub HUD).
            _tutorial = SugarUi.Outlined("Tutorial", transform, "", 40, Color.white);
            UiFactory.Place(_tutorial.RectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -246f), new Vector2(980f, 90f));
            _tutorial.gameObject.SetActive(false);

            // Cuvântul de combo (centru) și bannerul („Ultimele mutări!”).
            _word = SugarUi.Outlined("Word", transform, "", 110, Color.white, 5f);
            UiFactory.Place(_word.RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 200f), new Vector2(1000f, 160f));
            _word.gameObject.SetActive(false);
            _banner = SugarUi.Outlined("Banner", transform, "", 64, theme.AccentColor, 4f);
            UiFactory.Place(_banner.RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 420f), new Vector2(1000f, 110f));
            _banner.gameObject.SetActive(false);
        }

        private void BuildScore(UiTheme theme)
        {
            // Bara de scor: pistă închisă, umplere albastră, stelele pe pistă și scorul în dreapta.
            Image pill = UiFactory.CreatePanel("ScorePill", transform, QuestStyle.Board);
            var pillRect = UiFactory.Place((RectTransform)pill.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-6f, -180f), new Vector2(760f, 52f));
            _scorePunch = pill.gameObject.AddComponent<PunchScale>();
            _score = SugarUi.Outlined("Score", pillRect, "0", 34, Color.white, 2.5f);
            UiFactory.Place(_score.RectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-16f, 0f), new Vector2(170f, 50f));
            _scoreBar = UiFactory.CreateProgressBar("ScoreBar", pillRect, QuestStyle.Blue);
            _scoreBarRect = (RectTransform)_scoreBar.transform;
            _scoreBarRect.anchorMin = new Vector2(0f, 0.5f);
            _scoreBarRect.anchorMax = new Vector2(1f, 0.5f);
            _scoreBarRect.pivot = new Vector2(0.5f, 0.5f);
            _scoreBarRect.offsetMin = new Vector2(18f, -11f);
            _scoreBarRect.offsetMax = new Vector2(-196f, 11f);
            for (int i = 0; i < 2; i++)
            {
                _scoreStars[i] = UiFactory.CreateImage("Star" + (i + 2), _scoreBarRect, ProceduralSprites.Star, QuestStyle.Gold);
                UiFactory.Place(_scoreStars[i].rectTransform, new Vector2(i == 0 ? 0.62f : 1f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(46f, 46f));
            }

            for (int i = 0; i < _popups.Length; i++)
            {
                _popups[i] = SugarUi.Outlined("Points", transform, "", 46, Color.white, 3f);
                UiFactory.Place(_popups[i].RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300f, 70f));
                _popups[i].gameObject.SetActive(false);
                _popupT[i] = -1f;
            }
        }

        private void BuildBoss(UiTheme theme)
        {
            Image panel = UiFactory.CreatePanel("BossPanel", transform, new Color(QuestStyle.Text.r, QuestStyle.Text.g, QuestStyle.Text.b, 0.92f));
            _bossPanel = UiFactory.Place((RectTransform)panel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -246f), new Vector2(900f, 124f));
            _bossFace = UiFactory.CreateImage("Face", _bossPanel, SugarSprites.BossFace(0), Color.white);
            UiFactory.Place(_bossFace.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(72f, 6f), new Vector2(124f, 124f));
            _bossName = SugarUi.Outlined("Name", _bossPanel, "", 32, Color.white, 3f);
            UiFactory.Place(_bossName.RectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(146f, -6f), new Vector2(560f, 48f));
            _bossBar = UiFactory.CreateProgressBar("Health", _bossPanel, theme.NegativeColor);
            var bar = (RectTransform)_bossBar.transform;
            bar.anchorMin = new Vector2(0f, 0f);
            bar.anchorMax = new Vector2(1f, 0f);
            bar.pivot = new Vector2(0.5f, 0f);
            bar.offsetMin = new Vector2(146f, 20f);
            bar.offsetMax = new Vector2(-160f, 56f);
            _bossHealth = SugarUi.Outlined("HealthText", bar, "", 26, Color.white, 2.5f);
            UiFactory.Stretch(_bossHealth.RectTransform, 0f);

            _bossChip = UiFactory.CreateImage("Attack", _bossPanel, theme.Circle, theme.AccentColor);
            UiFactory.Place(_bossChip.rectTransform, new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-80f, 0f), new Vector2(108f, 108f));
            _bossChipPunch = _bossChip.gameObject.AddComponent<PunchScale>();
            _bossCountdown = UiFactory.CreateText("Count", _bossChip.transform, "", 46, TextAnchor.MiddleCenter, theme.TextDarkColor);
            UiFactory.Place(_bossCountdown.RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 8f), new Vector2(100f, 56f));
            _bossCaption = UiFactory.CreateText("Caption", _bossChip.transform, "", 19, TextAnchor.MiddleCenter, theme.TextDarkColor);
            UiFactory.Place(_bossCaption.RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -28f), new Vector2(100f, 26f));
            _bossPanel.gameObject.SetActive(false);

            // Intrarea: ecran întunecat, portretul mare care „cade” și numele.
            _intro = UiFactory.CreateRect("BossIntro", transform);
            UiFactory.Stretch(_intro, 0f);
            _introDim = UiFactory.CreateImage("Dim", _intro, null, new Color(0.08f, 0.02f, 0.1f, 0.75f));
            UiFactory.Stretch(_introDim.rectTransform, -400f);
            _introFace = UiFactory.CreateImage("Face", _intro, SugarSprites.BossFace(0), Color.white);
            UiFactory.Place(_introFace.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(460f, 460f));
            _introTitle = SugarUi.Outlined("Title", _intro, "", 96, theme.AccentColor, 5f);
            UiFactory.Place(_introTitle.RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 470f), new Vector2(1000f, 130f));
            _introName = SugarUi.Outlined("Name", _intro, "", 64, Color.white, 4f);
            UiFactory.Place(_introName.RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -190f), new Vector2(1000f, 170f));
            _intro.gameObject.SetActive(false);
        }

        /// <summary>Lupta curentă (null = nivel obișnuit, panoul se ascunde).</summary>
        public void SetBoss(BossBattle boss)
        {
            _boss = boss;
            _bossHitT = -1f;
            _bossPanel.gameObject.SetActive(boss != null);
            if (boss == null) return;
            _bossLook = boss.Definition.Look;
            _bossFace.sprite = SugarSprites.BossFace(_bossLook);
            _bossFace.rectTransform.localScale = Vector3.one;
            _bossName.Text = SugarLoc.T(boss.Definition.Name);
            _bossCaption.Text = SugarLoc.T("atac");
            RefreshBoss(false);
        }

        /// <summary>Viața și numărătoarea; hit = Boss-ul tocmai a primit daune (portretul tresare).</summary>
        public void RefreshBoss(bool hit)
        {
            if (_boss == null) return;
            _bossBar.SetValue(_boss.HealthFraction, !hit);
            _bossHealth.Text = _boss.Health + " / " + _boss.Definition.Health;
            _bossCountdown.Text = _boss.Defeated ? "-" : _boss.MovesUntilAttack.ToString();
            _bossChip.color = _boss.AttackWarning ? UiRoot.Theme.NegativeColor : UiRoot.Theme.AccentColor;
            _bossCountdown.Color = _boss.AttackWarning ? Color.white : UiRoot.Theme.TextDarkColor;
            _bossCaption.Color = _bossCountdown.Color;
            if (_boss.Definition.Mega)
                _bossName.Text = SugarLoc.T(_boss.Definition.Name) + "  " + SugarLoc.F("Faza {0}/3", _boss.PhaseIndex + 1);
            if (hit) _bossHitT = 0f;
        }

        /// <summary>Boss-ul atacă: numărătoarea sare.</summary>
        public void BossAttacked()
        {
            RefreshBoss(false);
            _bossChipPunch.Punch(2f);
            _bossHitT = -1f;
            if (_boss != null) _bossFace.rectTransform.localScale = Vector3.one * 1.25f;
        }

        public void ShowBossIntro(BossDefinition boss)
        {
            _introFace.sprite = SugarSprites.BossFace(boss.Look);
            _introTitle.Text = boss.Mega ? SugarLoc.T("MEGA BOSS!") : SugarLoc.T("BOSS!");
            _introName.Text = SugarLoc.T(boss.Name) + (boss.Mega ? "\n" + SugarLoc.T("3 faze") : "");
            _intro.gameObject.SetActive(true);
            _intro.SetAsLastSibling();
            _introT = 0f;
            if (SugarSave.Data.reducedMotion) _introT = 0.35f;
        }

        private void UpdateBoss(float dt)
        {
            if (_introT >= 0f)
            {
                _introT += dt / IntroDuration;
                float t = Mathf.Clamp01(_introT);
                // Portretul cade de sus cu un „boing”, numele apare, apoi totul se stinge.
                float drop = t < 0.25f ? UiEase.OutBack(t / 0.25f, 2.2f) : 1f;
                _introFace.rectTransform.anchoredPosition = new Vector2(0f, Mathf.LerpUnclamped(900f, 120f, drop));
                _introFace.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 30f) * 6f * Mathf.Clamp01(1f - t * 2f));
                float fade = t < 0.85f ? 1f : 1f - (t - 0.85f) / 0.15f;
                _introDim.color = new Color(0.08f, 0.02f, 0.1f, 0.75f * fade);
                _introFace.color = new Color(1f, 1f, 1f, fade);
                float title = Mathf.Clamp01((t - 0.18f) / 0.12f);
                _introTitle.RectTransform.localScale = Vector3.one * (title < 1f ? UiEase.OutBack(title, 3f) : 1f + Mathf.Sin(t * 20f) * 0.03f);
                Color tc = _introTitle.Color;
                tc.a = Mathf.Min(title, fade);
                _introTitle.Color = tc;
                Color nc = _introName.Color;
                nc.a = Mathf.Min(Mathf.Clamp01((t - 0.3f) / 0.12f), fade);
                _introName.Color = nc;
                if (t >= 1f)
                {
                    _introT = -1f;
                    _intro.gameObject.SetActive(false);
                }
            }

            if (_boss == null || !_bossPanel.gameObject.activeSelf) return;
            RectTransform face = _bossFace.rectTransform;
            if (_bossHitT >= 0f)
            {
                // Lovit: tresare, se strânge și se colorează în roșu o clipă.
                _bossHitT += dt / 0.35f;
                float t = Mathf.Clamp01(_bossHitT);
                face.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 40f) * 10f * (1f - t));
                face.localScale = new Vector3(1f + 0.12f * (1f - t), 1f - 0.1f * (1f - t), 1f);
                _bossFace.color = Color.Lerp(new Color(1f, 0.55f, 0.55f), Color.white, t);
                if (t >= 1f) _bossHitT = -1f;
            }
            else
            {
                // Respiră; după un atac revine la mărimea normală.
                float breathe = 1f + Mathf.Sin(Time.unscaledTime * 2.4f) * 0.03f;
                face.localScale = Vector3.Lerp(face.localScale, new Vector3(breathe, 2f - breathe, 1f), dt * 8f);
                face.localRotation = Quaternion.identity;
                _bossFace.color = _boss.Defeated ? new Color(1f, 1f, 1f, 0.5f) : Color.white;
            }
            if (_boss.AttackWarning)
                _bossChip.rectTransform.localScale = Vector3.one * (1f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * 6f)) * 0.12f);
            else _bossChip.rectTransform.localScale = Vector3.one;
        }

        /// <summary>Pragurile de 2 și 3 stele ale nivelului (bara se umple până la 3 stele).</summary>
        public void SetScoreTargets(int twoStars, int threeStars)
        {
            _twoStars = Mathf.Max(1, twoStars);
            _threeStars = Mathf.Max(_twoStars + 1, threeStars);
            float marker = (float)_twoStars / _threeStars;
            RectTransform first = _scoreStars[0].rectTransform;
            first.anchorMin = first.anchorMax = new Vector2(marker, 0.5f);
            SetScore(0, false);
        }

        public void SetScore(int score, bool punch)
        {
            _score.Text = score.ToString();
            _scoreBar.SetValue(_threeStars > 0 ? Mathf.Clamp01((float)score / _threeStars) : 0f, !punch);
            Color off = new(1f, 1f, 1f, 0.55f);
            _scoreStars[0].color = score >= _twoStars ? UiRoot.Theme.AccentColor : off;
            _scoreStars[1].color = score >= _threeStars ? UiRoot.Theme.AccentColor : off;
            if (punch) _scorePunch.Punch(0.15f);
        }

        /// <summary>Puncte care sar din locul unde au dispărut capacele (poziție din lume).</summary>
        public void ShowPoints(Vector3 world, int points, Color color)
        {
            if (_camera == null) _camera = Camera.main;
            if (_camera == null) return;
            Vector3 screen = _camera.WorldToScreenPoint(world);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, screen, null, out Vector2 local)) return;
            int i = _nextPopup;
            _nextPopup = (_nextPopup + 1) % _popups.Length;
            UiLabel label = _popups[i];
            label.Text = points.ToString();
            label.Color = color;
            label.gameObject.SetActive(true);
            label.RectTransform.SetAsLastSibling();
            _popupStart[i] = local;
            _popupT[i] = 0f;
            _anyPopup = true;
        }

        public void SetLevel(int level, bool boss)
        {
            _levelCaption.Text = SugarLoc.T(boss ? "BOSS" : "Nivel");
            _level.Text = level.ToString();
            _goalCaption.Text = SugarLoc.T("Obiectiv");
            _movesCaption.Text = SugarLoc.T("Mutări");
            // Nivelurile grele au o flăcăruie pe cardul nivelului.
            Difficulty difficulty = LevelConfig.DifficultyFor(level);
            _levelFlame.gameObject.SetActive(!boss && !LevelConfig.IsBonusLevel(level) && difficulty is Difficulty.Hard or Difficulty.VeryHard);
        }

        // ------------------------------------------------------------ Puterea lui Bobo

        private void BuildPower(UiTheme theme)
        {
            _power = UiFactory.CreateRect("BoboPower", transform);
            UiFactory.Place(_power, Vector2.right, new Vector2(0.5f, 0.5f), new Vector2(-120f, 120f), new Vector2(150f, 150f));
            _powerGlow = UiFactory.CreateImage("Glow", _power, SugarSprites.SoftDot, new Color(1f, 0.85f, 0.4f, 0f));
            UiFactory.Place(_powerGlow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * 240f);
            Image back = UiFactory.CreateImage("Back", _power, theme.Circle, QuestStyle.Scrim);
            UiFactory.Stretch(back.rectTransform);
            back.raycastTarget = true;
            _powerFill = UiFactory.CreateImage("Fill", _power, theme.Circle, theme.AccentColor);
            UiFactory.Stretch(_powerFill.rectTransform);
            _powerFill.type = Image.Type.Filled;
            _powerFill.fillMethod = Image.FillMethod.Radial360;
            _powerFill.fillOrigin = (int)Image.Origin360.Top;
            _powerFill.fillClockwise = true;
            Image face = UiFactory.CreateImage("Face", _power, theme.Circle, Color.white);
            UiFactory.Place(face.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * 112f);
            Image bobo = UiFactory.CreateImage("Bobo", face.transform, SugarSprites.BoboToken, Color.white);
            UiFactory.Place(bobo.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * 100f);
            var button = _power.gameObject.AddComponent<Button>();
            button.targetGraphic = back;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => PowerClicked?.Invoke());
        }

        /// <param name="value">0..1 (plin = gata de folosit).</param>
        public void SetPower(float value, bool visible)
        {
            _power.gameObject.SetActive(visible);
            _powerValue = Mathf.Clamp01(value);
            _powerFill.fillAmount = _powerValue;
        }

        /// <summary>Textul de sub numărul din disc („mutări”, sau „sec” în Sugar Rush).</summary>
        public void SetMovesCaption(string caption) => _movesCaption.Text = caption;

        public void SetMoves(int moves, bool punch)
        {
            _moves.Text = moves.ToString();
            _lowMoves = moves <= LowMoves;
            _moves.Color = _lowMoves ? UiRoot.Theme.NegativeColor : UiRoot.Theme.TextDarkColor;
            if (punch) _movesPunch.Punch(_lowMoves ? 2f : 1f);
            if (!_lowMoves) _moves.RectTransform.localScale = Vector3.one;
        }

        /// <summary>Construiește obiectivele nivelului (1–3 iconițe în cardul de sus).</summary>
        public void SetGoals(LevelGoals goals)
        {
            foreach (GoalView view in _goalViews) Destroy(view.Root.gameObject);
            _goalViews.Clear();
            int count = goals.Items.Count;
            const float spacing = 150f;
            for (int i = 0; i < count; i++)
            {
                GoalItem item = goals.Items[i];
                RectTransform root = UiFactory.CreateRect("Goal" + i, _goalCard);
                UiFactory.Place(root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2((i - (count - 1) * 0.5f) * spacing, -6f), new Vector2(120f, 110f));
                Image icon = UiFactory.CreateImage("Icon", root, GoalIcon(item), GoalColor(item));
                UiFactory.Place(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 14f), new Vector2(70f, 70f));
                UiLabel label = UiFactory.CreateText("Count", root, "", item.Kind is GoalKind.Score or GoalKind.Boss ? 28 : 34, TextAnchor.MiddleCenter, UiRoot.Theme.TextDarkColor);
                UiFactory.Place(label.RectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, -2f), new Vector2(150f, 44f));
                Image check = UiFactory.CreateImage("Check", root, SugarSprites.Check, UiRoot.Theme.PositiveColor);
                UiFactory.Place(check.rectTransform, new Vector2(1f, 0f), new Vector2(0.5f, 0.5f), new Vector2(-16f, 18f), new Vector2(54f, 54f));
                var view = new GoalView { Item = item, Root = root, Count = label, Check = check, Punch = root.gameObject.AddComponent<PunchScale>() };
                _goalViews.Add(view);
            }
            RefreshGoals(false);
        }

        /// <summary>Actualizează numerele rămase (cu un mic „puls” când scad).</summary>
        public void RefreshGoals(bool punch)
        {
            foreach (GoalView view in _goalViews)
            {
                int remaining = view.Item.Remaining;
                if (remaining == view.Shown) continue;
                bool decreased = view.Shown >= 0 && remaining < view.Shown;
                view.Shown = remaining;
                view.Count.Text = view.Item.Kind == GoalKind.Score ? view.Item.Target.ToString() : remaining.ToString();
                view.Count.gameObject.SetActive(!view.Item.Done || view.Item.Kind == GoalKind.Score);
                view.Check.gameObject.SetActive(view.Item.Done);
                if (punch && decreased) view.Punch.Punch(view.Item.Done ? 2.5f : 1f);
            }
        }

        public Sprite GoalIcon(GoalItem item) => item.Kind switch
        {
            GoalKind.Ingredients => SugarSprites.CandyWrapped,
            GoalKind.Collect => PieceSprites.Piece(item.Color),
            GoalKind.Frosting => SugarSprites.Glaze,
            GoalKind.Boss => SugarSprites.BossFace(_bossLook),
            GoalKind.Crates => SugarSprites.Crate,
            GoalKind.Ice => SugarSprites.Ice,
            GoalKind.Chains => SugarSprites.Chain,
            GoalKind.Chocolate => SugarSprites.Chocolate,
            GoalKind.Specials => SugarSprites.Rocket,
            GoalKind.Flowers => SugarSprites.Flower,
            GoalKind.TimeBombs => SugarSprites.Bomb,
            GoalKind.Bobo => SugarSprites.BoboToken,
            GoalKind.Fire => SugarSprites.Flame,
            GoalKind.Tanks => SugarSprites.Tank,
            _ => ProceduralSprites.Star,
        };

        public static Color GoalColor(GoalItem item) => item.Kind switch
        {
            GoalKind.Ingredients => Palette.Candy(0),
            GoalKind.Frosting => new Color(1f, 0.82f, 0.9f),
            GoalKind.Score => UiRoot.Theme.AccentColor,
            _ => Color.white,
        };

        /// <summary>Cardul cu obiectivele și discul cu mutări (pentru săgețile tutorialului).</summary>
        public RectTransform GoalArea => _goalCard;
        public RectTransform MovesArea { get; private set; }

        public void SetTutorial(string text)
        {
            _tutorial.gameObject.SetActive(!string.IsNullOrEmpty(text));
            if (!string.IsNullOrEmpty(text)) _tutorial.Text = text;
        }

        /// <summary>Cuvânt mare care sare în mijlocul ecranului (combo).</summary>
        public void ShowWord(string text, Color color)
        {
            _word.Text = text;
            _word.Color = color;
            _word.gameObject.SetActive(true);
            _wordT = 0f;
        }

        /// <summary>Butonul „Sari” din finalul dulce (grăbește animațiile până la ecranul de rezultat).</summary>
        public void ShowSkip(bool visible)
        {
            if (_skipFinale == null) return;
            if (visible) _skipFinaleLabel.Text = SugarLoc.T("SARI") + " ›";
            _skipFinale.SetActive(visible);
        }

        public void ShowBanner(string text)
        {
            _banner.Text = text;
            _banner.gameObject.SetActive(true);
            _bannerT = 0f;
        }

        public void HideTransient()
        {
            _introT = -1f;
            _intro.gameObject.SetActive(false);
            _word.gameObject.SetActive(false);
            _banner.gameObject.SetActive(false);
            _wordT = _bannerT = -1f;
            for (int i = 0; i < _popups.Length; i++)
            {
                _popupT[i] = -1f;
                _popups[i].gameObject.SetActive(false);
            }
            _anyPopup = false;
        }

        private void Update()
        {
            if (_livesCount != null && _livesShown != SugarSave.Data.lives)
            {
                _livesShown = SugarSave.Data.lives;
                _livesCount.Text = _livesShown.ToString();
            }
            // Puterea lui Bobo plină: cercul strălucește și pulsează ca să fie observată.
            if (_power != null && _power.gameObject.activeSelf)
            {
                bool ready = _powerValue >= 1f;
                float pulse = ready ? Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3.5f)) : 0f;
                _powerGlow.color = new Color(1f, 0.85f, 0.4f, ready ? 0.35f + pulse * 0.4f : 0f);
                _power.localScale = Vector3.one * (1f + pulse * 0.08f);
            }
            // Puține mutări: cifra bate ca o inimă, ca jucătorul să fie atent la ultimele mișcări.
            if (_lowMoves) _moves.RectTransform.localScale = Vector3.one * (1f + 0.12f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4f)));
            if (_wordT >= 0f)
            {
                _wordT += Time.unscaledDeltaTime / 1.1f;
                float t = Mathf.Clamp01(_wordT);
                // Apare cu „boing”, stă puțin, apoi urcă și se stinge.
                float scale = t < 0.2f ? Mathf.LerpUnclamped(0.3f, 1f, Overshoot(t / 0.2f)) : 1f;
                _word.RectTransform.localScale = Vector3.one * scale;
                _word.RectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 12f) * 4f * (1f - t));
                _word.RectTransform.anchoredPosition = new Vector2(0f, 200f + Mathf.Max(0f, t - 0.6f) * 300f);
                Color c = _word.Color;
                c.a = t < 0.7f ? 1f : 1f - (t - 0.7f) / 0.3f;
                _word.Color = c;
                if (t >= 1f)
                {
                    _wordT = -1f;
                    _word.gameObject.SetActive(false);
                }
            }

            if (_bannerT >= 0f)
            {
                _bannerT += Time.unscaledDeltaTime / 1.8f;
                float t = Mathf.Clamp01(_bannerT);
                _banner.RectTransform.localScale = Vector3.one * (1f + Mathf.Sin(t * Mathf.PI * 6f) * 0.06f);
                Color c = _banner.Color;
                c.a = t < 0.8f ? 1f : 1f - (t - 0.8f) / 0.2f;
                _banner.Color = c;
                if (t >= 1f)
                {
                    _bannerT = -1f;
                    _banner.gameObject.SetActive(false);
                }
            }

            if (_anyPopup) UpdatePopups();
            UpdateBoss(Time.unscaledDeltaTime);

            if (_tutorial.gameObject.activeSelf)
                _tutorial.RectTransform.localScale = Vector3.one * (1f + Mathf.Sin(Time.unscaledTime * 4f) * 0.03f);
        }

        private void UpdatePopups()
        {
            _anyPopup = false;
            for (int i = 0; i < _popups.Length; i++)
            {
                if (_popupT[i] < 0f) continue;
                _popupT[i] += Time.unscaledDeltaTime / 0.9f;
                float t = Mathf.Clamp01(_popupT[i]);
                UiLabel label = _popups[i];
                label.RectTransform.anchoredPosition = _popupStart[i] + new Vector2(0f, UiEase.OutCubic(t) * 130f);
                label.RectTransform.localScale = Vector3.one * (t < 0.2f ? UiEase.OutBack(t / 0.2f, 2.5f) : 1f);
                Color c = label.Color;
                c.a = t < 0.65f ? 1f : 1f - (t - 0.65f) / 0.35f;
                label.Color = c;
                if (t >= 1f)
                {
                    _popupT[i] = -1f;
                    label.gameObject.SetActive(false);
                }
                else _anyPopup = true;
            }
        }

        private static float Overshoot(float t)
        {
            const float s = 2.2f;
            t -= 1f;
            return t * t * ((s + 1f) * t + s) + 1f;
        }
    }
}
