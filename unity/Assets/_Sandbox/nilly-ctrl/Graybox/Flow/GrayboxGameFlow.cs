using System;
using HoldTheHill.Features.Enemies;
using HoldTheHill.Features.Towers;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>Which screen the game is on.</summary>
    public enum GameFlowState
    {
        Title,
        Home,
        Playing,
        Paused,
        RunEnd,
    }

    /// <summary>
    /// The one owner of game state: which screen is up, whether time runs and how fast, and the
    /// checkpoints that "restart" and "retry current wave" rewind to.
    /// </summary>
    /// <remarks>
    /// Nothing else should set <see cref="Time.timeScale"/>. Gameplay scripts ask
    /// <see cref="GameplayActive"/> before reading input or drawing IMGUI, because IMGUI always
    /// draws over a Canvas and would cover the menus.
    ///
    /// Runs before other scripts so that on the frame Escape is pressed it can see whether the
    /// tower placer still has something to cancel, before the placer cancels it.
    /// </remarks>
    [DefaultExecutionOrder(-100)]
    [AddComponentMenu("Hold the Hill/Graybox/Graybox Game Flow")]
    public class GrayboxGameFlow : MonoBehaviour
    {
        private static readonly float[] Speeds = { 1f, 2f, 4f };

        public static GrayboxGameFlow Instance { get; private set; }

        /// <summary>Raised after every state change with (previous, current).</summary>
        public static event Action<GameFlowState, GameFlowState> StateChanged;

        /// <summary>Raised after the field has been rewound to a checkpoint (new run or wave retry).</summary>
        public static event Action FieldReset;

        /// <summary>
        /// True while the game itself is on screen and running. Also true in a scene with no flow
        /// controller, so gameplay scripts behave as before there.
        /// </summary>
        public static bool GameplayActive => Instance == null || Instance.State == GameFlowState.Playing;

        [Tooltip("State on Play. Playing drops straight into the graybox; Title goes through the menus.")]
        [SerializeField] private GameFlowState _initialState = GameFlowState.Playing;

        [Tooltip("Pauses and resumes. Cancels tower placement or selection first if there is any.")]
        [SerializeField] private Key _pauseKey = Key.Escape;

        private EnemySpawner _spawner;
        private GrayboxTowerPlacer _placer;
        private GrayboxRunCheckpoint _runStart;
        private GrayboxRunCheckpoint _waveStart;

        // What this run has already added to the lifetime totals; null until its first ending.
        private GrayboxRunStats _counted;

        public GameFlowState State { get; private set; }
        public GrayboxRunStats Stats { get; private set; } = new GrayboxRunStats();
        public bool LastRunWasVictory { get; private set; }

        /// <summary>What the last finished run changed in the records ("new best" and so on).</summary>
        public RunRecordResult LastRunResult { get; private set; }
        public int SpeedIndex { get; private set; }
        public int SpeedCount => Speeds.Length;

        /// <summary>The fast-forward multiplier chosen, whether or not time is running now.</summary>
        public float Speed => Speeds[SpeedIndex];

        /// <summary>True once a wave has started this run, so there is a wave to retry.</summary>
        public bool CanRetryWave => _waveStart != null;

        /// <summary>1-based wave "retry current wave" goes back to, or 0 when there is none.</summary>
        public int RetryWaveNumber => _waveStart != null ? _waveStart.WaveIndex + 1 : 0;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            _spawner = FindAnyObjectByType<EnemySpawner>();
            _placer = FindAnyObjectByType<GrayboxTowerPlacer>();
            State = _initialState;
            ApplyTimeScale();
        }

        private void OnEnable()
        {
            EnemyHealth.Defeated += OnEnemyDefeated;
            GrayboxEconomy.OnBountyPaid += OnBountyPaid;
            GrayboxBaseHealth.OnGameFinished += OnGameFinished;
            if (_spawner != null) _spawner.onWaveStarted.AddListener(OnWaveStarted);
        }

        private void OnDisable()
        {
            EnemyHealth.Defeated -= OnEnemyDefeated;
            GrayboxEconomy.OnBountyPaid -= OnBountyPaid;
            GrayboxBaseHealth.OnGameFinished -= OnGameFinished;
            if (_spawner != null) _spawner.onWaveStarted.RemoveListener(OnWaveStarted);
        }

        private void Start()
        {
            // The scene as built, before anything has been bought or lost. Every new run starts here.
            _runStart = GrayboxRunCheckpoint.Capture(0, new GrayboxRunStats(), transform);
            Stats = NewStats();
        }

        private void OnDestroy()
        {
            if (Instance != this) return;

            Instance = null;
            // Leaving Play mode paused or sped up would carry the scale into the editor.
            Time.timeScale = 1f;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard[_pauseKey].wasPressedThisFrame)
            {
                if (State == GameFlowState.Paused)
                {
                    Resume();
                }
                else if (State == GameFlowState.Playing && (_placer == null || !_placer.IsBusy))
                {
                    Pause();
                }
            }

            if (State != GameFlowState.Playing) return;

            Stats.TimeSeconds += Time.deltaTime;
            CheckForVictory();
        }

        // ---------- Commands ----------

        /// <summary>Starts a fresh run from wave 1. Also what "restart" does.</summary>
        public void StartNewRun()
        {
            DiscardWaveCheckpoint();
            if (_runStart != null) _runStart.Restore(_spawner);
            if (_placer != null) _placer.CancelInteraction();

            Stats = NewStats();
            _counted = null;
            SpeedIndex = 0;
            FieldReset?.Invoke();
            Enter(GameFlowState.Playing);
        }

        /// <summary>
        /// Rewinds to the start of the current wave: towers, gold, hill health, skills and stats.
        /// The wave waits to be started, so there is time to build differently.
        /// </summary>
        public void RetryWave()
        {
            if (_waveStart == null)
            {
                StartNewRun();
                return;
            }

            int retries = Stats.WaveRetries + 1;
            _waveStart.Restore(_spawner);
            if (_placer != null) _placer.CancelInteraction();

            Stats = _waveStart.Stats.Clone();
            Stats.WaveRetries = retries;
            FieldReset?.Invoke();
            Enter(GameFlowState.Playing);
        }

        public void Pause()
        {
            if (State == GameFlowState.Playing) Enter(GameFlowState.Paused);
        }

        public void Resume()
        {
            if (State == GameFlowState.Paused) Enter(GameFlowState.Playing);
        }

        /// <summary>Abandons the run and clears the field.</summary>
        public void QuitToHome()
        {
            LeaveRun();
            Enter(GameFlowState.Home);
        }

        public void GoToTitle()
        {
            LeaveRun();
            Enter(GameFlowState.Title);
        }

        public void SetSpeedIndex(int index)
        {
            SpeedIndex = Mathf.Clamp(index, 0, Speeds.Length - 1);
            ApplyTimeScale();
        }

        public void CycleSpeed() => SetSpeedIndex((SpeedIndex + 1) % Speeds.Length);

        public float SpeedAt(int index) => Speeds[Mathf.Clamp(index, 0, Speeds.Length - 1)];

        // ---------- Internals ----------

        private void Enter(GameFlowState next)
        {
            GameFlowState previous = State;
            State = next;
            ApplyTimeScale();

            // Leaving play (pause, run end, quit) is the natural moment to write progress.
            if (previous == GameFlowState.Playing && next != GameFlowState.Playing) GrayboxSave.SaveIfDirty();

            if (previous != next) StateChanged?.Invoke(previous, next);
        }

        private void ApplyTimeScale()
        {
            Time.timeScale = State == GameFlowState.Playing ? Speeds[SpeedIndex] : 0f;
        }

        private void LeaveRun()
        {
            if (State == GameFlowState.Title || State == GameFlowState.Home) return;

            DiscardWaveCheckpoint();
            _counted = null;
            if (_spawner != null) _spawner.RestartFromWave(0);
            GrayboxRunCheckpoint.ClearField();
            if (_placer != null) _placer.CancelInteraction();
        }

        private void DiscardWaveCheckpoint()
        {
            if (_waveStart == null) return;

            _waveStart.Discard();
            _waveStart = null;
        }

        private GrayboxRunStats NewStats()
        {
            return new GrayboxRunStats { TotalWaves = _spawner != null ? _spawner.TotalWaves : 0 };
        }

        private void OnWaveStarted(int waveNumber, int totalWaves)
        {
            Stats.WaveReached = Mathf.Max(Stats.WaveReached, waveNumber);
            Stats.TotalWaves = totalWaves;

            DiscardWaveCheckpoint();
            _waveStart = GrayboxRunCheckpoint.Capture(waveNumber - 1, Stats, transform);
        }

        private void OnEnemyDefeated(GameObject enemy)
        {
            if (State == GameFlowState.Playing) Stats.Kills++;
        }

        private void OnBountyPaid(int gold)
        {
            if (State == GameFlowState.Playing) Stats.GoldEarned += gold;
        }

        // Nothing else declares victory: the spawner only knows when it has finished spawning.
        private void CheckForVictory()
        {
            if (_spawner == null || _spawner.CurrentState != EnemySpawner.SpawnerState.Completed) return;
            if (_spawner.LivingEnemyCount > 0) return;

            GrayboxBaseHealth hill = GrayboxBaseHealth.Instance;
            if (hill == null || hill.IsFinished) return;

            // The spawner does not count split-off or custom-horde enemies.
            if (FindAnyObjectByType<EnemyHealth>() != null) return;

            hill.TriggerVictory();
        }

        private void OnGameFinished(bool victory)
        {
            if (State != GameFlowState.Playing) return;

            LastRunWasVictory = victory;
            Stats.TowersStanding = FindObjectsByType<Tower>().Length;

            string levelId = _spawner != null ? _spawner.ActiveMapId : "Unknown";
            LastRunResult = GrayboxRecords.Submit(levelId, Stats, victory, _counted);
            _counted = Stats.Clone();

            Enter(GameFlowState.RunEnd);
        }
    }
}
