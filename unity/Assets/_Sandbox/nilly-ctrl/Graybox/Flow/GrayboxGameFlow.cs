using System;
using System.Collections.Generic;
using HoldTheHill.Features.Enemies;
using HoldTheHill.Features.Towers;
using HoldTheHill.Sandbox.UiKit;
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

    /// <summary>What kind of run is being played.</summary>
    public enum GrayboxRunMode
    {
        /// <summary>The map's own waves, in order, ending in victory.</summary>
        Campaign,

        /// <summary>Procedural waves until the hill falls (<see cref="GrayboxEndlessMode"/>).</summary>
        Endless,
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

        // Pause is GrayboxControls.Pause (Esc, or Start on a gamepad). The key first cancels a tower
        // placement or selection if there is any.

        [Tooltip("With auto-start on, seconds between a cleared wave and the next one starting.")]
        [SerializeField, Min(0f)] private float _autoStartDelay = 3f;

        private EnemySpawner _spawner;
        private GrayboxTowerPlacer _placer;
        private readonly List<Tower> _towers = new List<Tower>();
        private GrayboxCustomSpawner _custom;
        private float _autoStartTimer;

        // Honeydew this run has already been paid, so a retried run is not paid twice.
        private int _honeydewPaid;
        private GrayboxRunCheckpoint _runStart;
        private GrayboxRunCheckpoint _waveStart;

        // What this run has already added to the lifetime totals; null until its first ending.
        private GrayboxRunStats _counted;

        public GameFlowState State { get; private set; }
        public GrayboxRunStats Stats { get; private set; } = new GrayboxRunStats();
        public bool LastRunWasVictory { get; private set; }

        /// <summary>Campaign or endless. Set by <see cref="StartNewRun(GrayboxRunMode)"/>.</summary>
        public GrayboxRunMode Mode { get; private set; }

        /// <summary>Honeydew the run that just ended has earned in total (see <see cref="GrayboxUpgrades"/>).</summary>
        public int LastRunHoneydew { get; private set; }

        /// <summary>The id records are kept under: the map, and "-Endless" for an endless run.</summary>
        public string LevelId => LevelIdFor(Mode);

        public string LevelIdFor(GrayboxRunMode mode)
        {
            return GrayboxLevels.RecordId(MapId, mode);
        }

        /// <summary>The level this scene is: its spawner's map id.</summary>
        public string MapId => _spawner != null ? _spawner.ActiveMapId : "Unknown";

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
            _custom = FindAnyObjectByType<GrayboxCustomSpawner>();
            State = _initialState;

            // Home loaded this scene to play it (GrayboxLevels.LoadAndPlay): skip the menus.
            GrayboxRunMode? pending = GrayboxLevels.TakePendingRun();
            if (pending.HasValue)
            {
                Mode = pending.Value;
                State = GameFlowState.Playing;
            }

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

            GrayboxSettings.Apply();
            // A scene that starts in play has no "new run" moment to hand out the upgrades at.
            if (State == GameFlowState.Playing) ApplyRunStartBonuses();
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused && GrayboxSettings.PauseOnFocusLoss && !Application.isEditor) Pause();
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
            bool pausePressed = GrayboxControls.Pressed(GrayboxControls.Pause);
            bool startPressed = pausePressed && GrayboxControls.Find(GrayboxControls.Pause).activeControl?.device is Gamepad;
            bool keyPressed = pausePressed && !startPressed;

            if (State == GameFlowState.Playing)
            {
                // The key first cancels a tower placement or selection; Start always pauses.
                if (startPressed || (keyPressed && (_placer == null || !_placer.IsBusy))) Pause();
            }
            else if (State == GameFlowState.Paused)
            {
                // With a menu up, the key is that menu's "back" (it may be closing a dialog), so
                // the menu decides. Start resumes from the pause menu itself but not from a dialog.
                int menus = UiScreen.OpenCount;
                if ((keyPressed && menus == 0) || (startPressed && menus <= 1)) Resume();
            }

            if (State != GameFlowState.Playing) return;

            Stats.TimeSeconds += Time.deltaTime;
            AutoStartNextWave();
            CheckForVictory();
        }

        // ---------- Commands ----------

        /// <summary>Starts a fresh run of the given kind from wave 1.</summary>
        public void StartNewRun(GrayboxRunMode mode)
        {
            Mode = mode;
            StartNewRun();
        }

        /// <summary>Starts a fresh run from wave 1 in the current mode. Also what "restart" does.</summary>
        public void StartNewRun()
        {
            DiscardWaveCheckpoint();
            if (_custom != null) _custom.StopCustomSpawning();
            if (_runStart != null) _runStart.Restore(_spawner);
            if (_placer != null) _placer.CancelInteraction();
            ApplyRunStartBonuses();

            Stats = NewStats();
            _counted = null;
            _honeydewPaid = 0;
            _autoStartTimer = 0f;
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

        /// <summary>Called by <see cref="GrayboxEndlessMode"/> as each endless wave starts.</summary>
        public void ReportEndlessWave(int waveNumber)
        {
            Stats.WaveReached = Mathf.Max(Stats.WaveReached, waveNumber);
            Stats.TotalWaves = 0; // no last wave to count towards
        }

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
            _honeydewPaid = 0;
            if (_custom != null) _custom.StopCustomSpawning();
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
            bool counted = Mode == GrayboxRunMode.Campaign && _spawner != null;
            return new GrayboxRunStats { TotalWaves = counted ? _spawner.TotalWaves : 0 };
        }

        // The permanent upgrades bought at Home, handed out on top of the scene's own numbers.
        private static void ApplyRunStartBonuses()
        {
            if (GrayboxBaseHealth.Instance != null) GrayboxBaseHealth.Instance.SetBonusHealth(GrayboxUpgrades.HillHealthBonus);

            GrayboxEconomy economy = GrayboxEconomy.Instance;
            int food = GrayboxUpgrades.StartingFoodBonus;
            if (economy != null && food > 0) economy.ResetGold(economy.CurrentGold + food);
        }

        // The "auto-start waves" setting: campaign waves follow each other without the next-wave key.
        private void AutoStartNextWave()
        {
            if (!GrayboxSettings.AutoStartWaves || Mode != GrayboxRunMode.Campaign || _spawner == null) return;

            bool waiting = _spawner.CurrentState == EnemySpawner.SpawnerState.WaitingForNextWave
                || _spawner.CurrentState == EnemySpawner.SpawnerState.Idle;
            if (!waiting || _spawner.LivingEnemyCount > 0)
            {
                _autoStartTimer = 0f;
                return;
            }

            _autoStartTimer += Time.deltaTime;
            if (_autoStartTimer < _autoStartDelay) return;

            _autoStartTimer = 0f;
            _spawner.StartNextWave();
        }

        private void OnWaveStarted(int waveNumber, int totalWaves)
        {
            if (Mode != GrayboxRunMode.Campaign) return; // endless waves report themselves

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
            if (Mode != GrayboxRunMode.Campaign) return; // an endless run ends only in defeat
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
            Tower.GetActive(_towers);
            Stats.TowersStanding = _towers.Count;

            LastRunResult = GrayboxRecords.Submit(LevelId, Stats, victory, _counted);
            _counted = Stats.Clone();

            // Only what this ending adds: a run retried after a defeat has been paid for its earlier waves.
            int earned = GrayboxUpgrades.HoneydewFor(Stats.WaveReached, victory);
            GrayboxMetaProgress.AddCurrency(earned - _honeydewPaid);
            _honeydewPaid = Mathf.Max(_honeydewPaid, earned);
            LastRunHoneydew = _honeydewPaid;

            Enter(GameFlowState.RunEnd);
        }
    }
}
