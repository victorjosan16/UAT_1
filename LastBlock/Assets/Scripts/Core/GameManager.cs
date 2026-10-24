using System;
using System.Collections;
using LastBlock.Effects;
using LastBlock.Gameplay;
using LastBlock.UI;
using UnityEngine;

namespace LastBlock.Core
{
    public enum GameState
    {
        Idle,
        /// <summary>Tower just spawned; physics settles before input and collapse checks start.</summary>
        Settling,
        Playing,
        /// <summary>Objective done; the tower must stay up for the hold duration.</summary>
        Holding,
        Won,
        Lost
    }

    /// <summary>
    /// Owns the game loop: start level → settle → play → hold 5 s → win, or collapse → lose.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("References (auto-found when empty)")]
        [SerializeField] TowerGenerator towerGenerator;
        [SerializeField] TowerStabilityChecker stabilityChecker;
        [SerializeField] BlockDragController dragController;
        [SerializeField] LevelManager levelManager;
        [SerializeField] ScoreManager scoreManager;
        [SerializeField] UIManager uiManager;
        [SerializeField] CameraController cameraController;
        [SerializeField] SimpleEffects effects;

        [Header("Rules")]
        [SerializeField] float settleDuration = 1f;
        [SerializeField] float holdDuration = 5f;
        [Tooltip("After the hold timer, wait up to this long for the tower to stop moving.")]
        [SerializeField] float maxExtraSettleTime = 3f;
        [Tooltip("An extraction is 'perfect' if no other block moved more than this (world units).")]
        [SerializeField] float perfectDisplacementThreshold = 0.05f;
        [SerializeField] float gameOverPanelDelay = 1.2f;

        [Header("Feedback")]
        [SerializeField] float shakeImpactSpeed = 2.5f;

        public GameState State { get; private set; } = GameState.Idle;
        public LevelConfig CurrentConfig { get; private set; }
        public int CurrentLevel => CurrentConfig.levelNumber;
        public int BlocksRemoved { get; private set; }
        public int BlocksRemaining => Mathf.Max(0, CurrentConfig.blocksToRemove - BlocksRemoved);

        public event Action<GameState> StateChanged;

        float stateTimer;
        float holdTimer;

        /// <summary>Physics settings tuned for stable stacking. Also used by the play-mode tests.</summary>
        public static void ApplyPhysicsSettings()
        {
            Time.fixedDeltaTime = 1f / 100f;
            Physics.gravity = new Vector3(0f, -9.81f, 0f);
            Physics.defaultSolverIterations = 12;
            Physics.defaultSolverVelocityIterations = 4;
            Physics.defaultMaxDepenetrationVelocity = 1f;
            Physics.bounceThreshold = 2f;
            Physics.sleepThreshold = 0.005f;
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Duplicate GameManager found; destroying the new one.");
                Destroy(gameObject);
                return;
            }
            Instance = this;
            ApplyPhysicsSettings();
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            ResolveReferences();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void ResolveReferences()
        {
            if (towerGenerator == null) towerGenerator = FindFirstObjectByType<TowerGenerator>();
            if (stabilityChecker == null) stabilityChecker = GetOrAdd<TowerStabilityChecker>();
            if (dragController == null) dragController = GetOrAdd<BlockDragController>();
            if (levelManager == null) levelManager = GetOrAdd<LevelManager>();
            if (scoreManager == null) scoreManager = GetOrAdd<ScoreManager>();
            if (uiManager == null) uiManager = FindFirstObjectByType<UIManager>();
            if (cameraController == null) cameraController = FindFirstObjectByType<CameraController>();
            if (effects == null) effects = FindFirstObjectByType<SimpleEffects>();

            if (towerGenerator == null)
            {
                Debug.LogWarning("No TowerGenerator in the scene; creating one at the origin. Run Tools > Last Block > Setup Game.");
                towerGenerator = new GameObject("Tower").AddComponent<TowerGenerator>();
            }
            if (uiManager == null) Debug.LogWarning("No UIManager found: the game runs without UI.");
        }

        T GetOrAdd<T>() where T : Component
        {
            T found = FindFirstObjectByType<T>();
            return found != null ? found : gameObject.AddComponent<T>();
        }

        void Start()
        {
            if (uiManager != null) uiManager.Bind(this);
            if (scoreManager != null) scoreManager.ScoreChanged += OnScoreChanged;
            dragController.DragStarted += OnDragStarted;
            dragController.BlockExtracted += OnBlockExtracted;
            dragController.LockedBlockTapped += OnLockedBlockTapped;
            towerGenerator.BlockImpact += OnBlockImpact;

            StartLevel(levelManager.HighestUnlockedLevel, false);
        }

        void OnDisable()
        {
            if (scoreManager != null) scoreManager.ScoreChanged -= OnScoreChanged;
            if (dragController != null)
            {
                dragController.DragStarted -= OnDragStarted;
                dragController.BlockExtracted -= OnBlockExtracted;
                dragController.LockedBlockTapped -= OnLockedBlockTapped;
            }
            if (towerGenerator != null) towerGenerator.BlockImpact -= OnBlockImpact;
        }

        // ---------------------------------------------------------------- level flow

        public void RestartLevel() => StartLevel(CurrentLevel, true);

        public void NextLevel()
        {
            if (levelManager.IsLastLevel(CurrentLevel))
            {
                scoreManager.ResetAll();
                StartLevel(1, false);
            }
            else
            {
                StartLevel(CurrentLevel + 1, false);
            }
        }

        /// <summary>Builds the tower for <paramref name="level"/>. A retry restores the score from the level start.</summary>
        public void StartLevel(int level, bool isRetry)
        {
            StopAllCoroutines();
            dragController.CancelDrag();
            dragController.SelectionEnabled = false;
            stabilityChecker.Stop();

            if (isRetry) scoreManager.ResetToLevelStart();
            else scoreManager.BeginLevel();

            CurrentConfig = levelManager.SetCurrentLevel(level);
            BlocksRemoved = 0;

            var blocks = towerGenerator.Generate(CurrentConfig);
            stabilityChecker.Begin(blocks, towerGenerator.BlockHeight, CurrentConfig.allowedFallenBlocks);

            if (cameraController != null)
            {
                cameraController.FrameTower(towerGenerator.TowerCenter, towerGenerator.GetTowerHeight(CurrentConfig.layers),
                    towerGenerator.BlockLength, true);
            }

            if (uiManager != null)
            {
                uiManager.HidePanels();
                uiManager.SetLevel(CurrentLevel);
                uiManager.SetScore(scoreManager.Score);
                uiManager.SetRemaining(BlocksRemaining);
            }

            stateTimer = 0f;
            SetState(GameState.Settling);
        }

        void Update()
        {
            switch (State)
            {
                case GameState.Settling:
                    stateTimer += Time.deltaTime;
                    if (stateTimer >= settleDuration)
                    {
                        stabilityChecker.CaptureBaseline();
                        SetState(GameState.Playing);
                    }
                    break;

                case GameState.Playing:
                    if (stabilityChecker.IsCollapsed) Lose();
                    break;

                case GameState.Holding:
                    if (stabilityChecker.IsCollapsed)
                    {
                        Lose();
                        break;
                    }
                    holdTimer -= Time.deltaTime;
                    if (holdTimer > 0f)
                    {
                        if (uiManager != null) uiManager.ShowHold(holdTimer);
                    }
                    else if (stabilityChecker.IsSettled || -holdTimer >= maxExtraSettleTime)
                    {
                        Win();
                    }
                    break;
            }
        }

        void SetState(GameState newState)
        {
            State = newState;
            dragController.SelectionEnabled = newState == GameState.Playing;
            dragController.OrbitEnabled = newState != GameState.Won && newState != GameState.Lost;
            StateChanged?.Invoke(newState);
        }

        // ---------------------------------------------------------------- events

        void OnDragStarted(BlockController block)
        {
            stabilityChecker.TakeSnapshot();
            if (effects != null) effects.PlaySelect();
        }

        void OnLockedBlockTapped(BlockController block)
        {
            if (effects != null) effects.PlayDeny();
            if (uiManager != null) uiManager.ShowToast("TOP LAYER IS LOCKED", new Color(0.9f, 0.3f, 0.3f));
        }

        void OnBlockExtracted(BlockController block)
        {
            if (State != GameState.Playing) return;

            BlocksRemoved++;
            bool perfect = stabilityChecker.MaxDisplacementSinceSnapshot(block) < perfectDisplacementThreshold;
            int points = scoreManager.AddExtraction(perfect);

            if (effects != null) effects.PlayExtraction(block.transform.position, block.BaseColor);
            if (uiManager != null)
            {
                uiManager.SetRemaining(BlocksRemaining);
                uiManager.ShowToast(perfect ? $"PERFECT! +{points}" : $"+{points}",
                    perfect ? new Color(1f, 0.6f, 0.1f) : new Color(0.15f, 0.25f, 0.45f));
            }

            if (BlocksRemaining == 0)
            {
                holdTimer = holdDuration;
                SetState(GameState.Holding);
            }
        }

        void OnBlockImpact(BlockController block, float speed)
        {
            if (State == GameState.Settling || State == GameState.Idle) return;
            if (effects != null) effects.PlayImpact(speed);
            if (cameraController != null && speed >= shakeImpactSpeed)
                cameraController.Shake(Mathf.Clamp01((speed - shakeImpactSpeed) / 6f) * 0.5f + 0.15f);
        }

        void OnScoreChanged(int score)
        {
            if (uiManager != null) uiManager.SetScore(score);
        }

        // ---------------------------------------------------------------- outcomes

        void Win()
        {
            SetState(GameState.Won);
            stabilityChecker.Stop();
            scoreManager.AddLevelComplete();
            bool newBest = scoreManager.SaveBestScore();
            levelManager.MarkLevelCompleted(CurrentLevel);

            if (effects != null)
            {
                effects.PlayVictory(towerGenerator.TowerCenter +
                                    Vector3.up * towerGenerator.GetTowerHeight(CurrentConfig.layers));
            }
            if (uiManager != null) uiManager.ShowVictory(scoreManager.Score, newBest, levelManager.IsLastLevel(CurrentLevel));
        }

        void Lose()
        {
            SetState(GameState.Lost);
            dragController.CancelDrag();
            stabilityChecker.Stop();
            if (effects != null) effects.PlayLose();
            if (cameraController != null) cameraController.Shake(0.6f);
            StartCoroutine(ShowGameOverAfterDelay());
        }

        IEnumerator ShowGameOverAfterDelay()
        {
            // Let the player watch the collapse before covering it.
            yield return new WaitForSeconds(gameOverPanelDelay);
            if (uiManager != null) uiManager.ShowGameOver(scoreManager.Score);
        }
    }
}
