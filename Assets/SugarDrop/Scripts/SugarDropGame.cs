using QuarryEmpire;
using UnityEngine;

namespace SugarDrop
{
    /// <summary>
    /// Fluxul jocului: pornește nivelul, numără mutările, verifică victoria (obiectivul din borcan atins)
    /// sau înfrângerea (fără mutări și bomboanele s-au liniștit), salvează nivelul curent.
    /// </summary>
    public class SugarDropGame : MonoBehaviour
    {
        private enum State { Playing, Won, Lost }

        private const string LevelKey = "sd_level";

        [SerializeField] private Board board;
        [SerializeField] private BoardInput input;
        [SerializeField] private CandySpawner candies;
        [SerializeField] private Jar jar;
        [SerializeField] private Playfield playfield;
        [SerializeField] private SugarHud hud;
        [SerializeField] private ResultPanel result;

        [Header("Așezare (unități din lume)")]
        [SerializeField] private float boardBottomY = -4.4f;
        [SerializeField] private float topY = 8.2f;

        [Header("Final de nivel")]
        [Tooltip("După ultima mutare, câte secunde fără bomboane noi în borcan până se declară înfrângerea.")]
        [SerializeField] private float settleTime = 2.5f;
        [SerializeField] private float winDelay = 0.8f;

        private LevelConfig _config;
        private int _level;
        private int _moves;
        private State _state;
        private float _quietTime;
        private float _winTimer = -1f;
        private bool _playfieldBuilt;

        private void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        private void OnEnable()
        {
            board.MoveMade += OnMoveMade;
            board.TilesCleared += OnTilesCleared;
            jar.CountChanged += OnJarChanged;
            hud.RestartClicked += RestartLevel;
        }

        private void OnDisable()
        {
            board.MoveMade -= OnMoveMade;
            board.TilesCleared -= OnTilesCleared;
            jar.CountChanged -= OnJarChanged;
            hud.RestartClicked -= RestartLevel;
        }

        private void Start()
        {
            _level = Mathf.Max(1, PlayerPrefs.GetInt(LevelKey, 1));
            StartLevel();
        }

        private void StartLevel()
        {
            _config = LevelConfig.For(_level);
            var bottomCenter = new Vector2(0f, boardBottomY);
            board.Build(_config.Columns, _config.Rows, _config.Colors, bottomCenter);

            if (!_playfieldBuilt)
            {
                playfield.Build(board.Width * 0.5f, board.Bottom, board.Top, topY);
                _playfieldBuilt = true;
            }

            float margin = 0.05f;
            var candyArea = new Rect(-board.Width * 0.5f + margin, board.Top + 0.1f, board.Width - margin * 2f, topY - board.Top - 0.2f);
            candies.Spawn(_config.Candies, candyArea);

            jar.ResetJar(_config.Goal);
            _moves = _config.Moves;
            _state = State.Playing;
            _quietTime = 0f;
            _winTimer = -1f;
            input.enabled = true;
            result.Hide();
            hud.SetLevel(_level);
            hud.SetMoves(_moves, false);
            hud.SetProgress(0, _config.Goal, false);
        }

        private void RestartLevel()
        {
            if (board.Busy) return;
            StartLevel();
        }

        private void NextLevel()
        {
            _level++;
            PlayerPrefs.SetInt(LevelKey, _level);
            PlayerPrefs.Save();
            StartLevel();
        }

        private void OnMoveMade()
        {
            if (_state != State.Playing) return;
            _moves = Mathf.Max(0, _moves - 1);
            _quietTime = 0f;
            hud.SetMoves(_moves, true);
        }

        private void OnTilesCleared(int count, Vector3 center)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.Play(ProceduralSfx.Break, 0.8f);
            Haptics.Light();
        }

        private void OnJarChanged(int count)
        {
            _quietTime = 0f;
            hud.SetProgress(count, jar.Goal, count > 0);
            if (count > 0 && AudioManager.Instance != null) AudioManager.Instance.Play(ProceduralSfx.Pop, 0.35f, 0.15f, 0.04f);
            if (_state == State.Playing && count >= jar.Goal && _winTimer < 0f) _winTimer = 0f;
        }

        private void Update()
        {
            if (_state != State.Playing) return;

            if (_winTimer >= 0f)
            {
                _winTimer += Time.deltaTime;
                if (_winTimer >= winDelay) Win();
                return;
            }

            // Fără mutări: așteptăm să se liniștească bomboanele, apoi verificăm.
            bool outOfMoves = _moves <= 0 || board.RemainingTiles == 0;
            if (!outOfMoves || board.Busy) return;
            _quietTime += Time.deltaTime;
            if (_quietTime >= settleTime) Lose();
        }

        private void Win()
        {
            _state = State.Won;
            input.enabled = false;
            float movesLeft = _config.Moves > 0 ? (float)_moves / _config.Moves : 0f;
            int stars = movesLeft >= 0.4f ? 3 : movesLeft >= 0.15f ? 2 : 1;
            if (AudioManager.Instance != null) AudioManager.Instance.Play(ProceduralSfx.LayerClear);
            Haptics.Medium();
            result.ShowWin(_level, stars, NextLevel);
        }

        private void Lose()
        {
            _state = State.Lost;
            input.enabled = false;
            if (AudioManager.Instance != null) AudioManager.Instance.Play(ProceduralSfx.Error);
            result.ShowLose(jar.Count, jar.Goal, StartLevel);
        }
    }
}
