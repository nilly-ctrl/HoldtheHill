using HoldTheHill.Core.Input;
using HoldTheHill.Core.Save;
using HoldTheHill.Features.Economy;
using System;
using System.Collections.Generic;
using HoldTheHill.Features.Enemies;
using HoldTheHill.Features.GameFlow;
using HoldTheHill.Features.Towers;
using HoldTheHill.Core.UiKit;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HoldTheHill.Features.GameFlow
{
    /// <summary>What kind of run is being played.</summary>
    public enum RunMode
    {
        /// <summary>The map's own waves, in order, ending in victory.</summary>
        Campaign,

        /// <summary>Procedural waves until the hill falls (<see cref="EndlessMode"/>).</summary>
        Endless,
    }

    /// <summary>
    /// The graybox's run on top of <see cref="GameFlowController"/> (which screen is up, whether
    /// time runs and how fast): what a run is, the checkpoints that "restart" and "retry current
    /// wave" rewind to, records and honeydew.
    /// </summary>
    /// <remarks>
    /// Gameplay scripts ask <see cref="GameFlowController.IsPlaying"/> before reading input or drawing IMGUI,
    /// because IMGUI always draws over a Canvas and would cover the menus.
    ///
    /// Runs before other scripts so that on the frame Escape is pressed it can see whether the
    /// tower placer still has something to cancel, before the placer cancels it.
    /// </remarks>
    [DefaultExecutionOrder(-100)]
    [AddComponentMenu("Hold the Hill/Graybox Game Flow")]
    public class RunFlow : GameFlowController
    {
        /// <summary>Raised after the field has been rewound to a checkpoint (new run or wave retry).</summary>
        public event Action FieldReset;

        // Pause is GameControls.Pause (Esc, or Start on a gamepad). The key first cancels a tower
        // placement or selection if there is any.

        [Tooltip("With auto-start on, seconds between a cleared wave and the next one starting.")]
        [SerializeField, Min(0f)] private float _autoStartDelay = 3f;

        private EnemySpawner _spawner;
        private GameTowerPlacer _placer;
        private Queen _hill;
        private WalletBehaviour _economy;
        private readonly List<Tower> _towers = new List<Tower>();
        private CustomSpawner _custom;
        private float _autoStartTimer;

        // Honeydew this run has already been paid, so a retried run is not paid twice.
        private int _honeydewPaid;
        private RunCheckpoint _runStart;
        private RunCheckpoint _waveStart;

        // What this run has already added to the lifetime totals; null until its first ending.
        private RunStats _counted;

        public RunStats Stats { get; private set; } = new RunStats();
        public bool LastRunWasVictory { get; private set; }

        /// <summary>Campaign or endless. Set by <see cref="StartNewRun(RunMode)"/>.</summary>
        public RunMode Mode { get; private set; }

        /// <summary>Honeydew the run that just ended has earned in total (see <see cref="PermanentUpgrades"/>).</summary>
        public int LastRunHoneydew { get; private set; }

        /// <summary>The id records are kept under: the map, and "-Endless" for an endless run.</summary>
        public string LevelId => LevelIdFor(Mode);

        public string LevelIdFor(RunMode mode)
        {
            return LevelList.RecordId(MapId, mode);
        }

        /// <summary>The level this scene is: its spawner's map id.</summary>
        public string MapId => _spawner != null ? _spawner.ActiveMapId : "Unknown";

        /// <summary>What the last finished run changed in the records ("new best" and so on).</summary>
        public RunRecordResult LastRunResult { get; private set; }

        /// <summary>True once a wave has started this run, so there is a wave to retry.</summary>
        public bool CanRetryWave => _waveStart != null;

        /// <summary>1-based wave "retry current wave" goes back to, or 0 when there is none.</summary>
        public int RetryWaveNumber => _waveStart != null ? _waveStart.WaveIndex + 1 : 0;

        protected override void Awake()
        {
            _spawner = FindAnyObjectByType<EnemySpawner>();
            _placer = FindAnyObjectByType<GameTowerPlacer>();
            _custom = FindAnyObjectByType<CustomSpawner>();
            _hill = FindAnyObjectByType<Queen>();
            _economy = FindAnyObjectByType<WalletBehaviour>();
            base.Awake();

            // Home loaded this scene to play it (LevelList.LoadAndPlay): skip the menus.
            RunMode? pending = LevelList.TakePendingRun();
            if (pending.HasValue)
            {
                Mode = pending.Value;
                ForceState(GameFlowState.Playing);
            }
        }

        private void OnEnable()
        {
            EnemyHealth.Defeated += OnEnemyDefeated;
            if (_economy == null) _economy = FindAnyObjectByType<WalletBehaviour>();
            if (_economy != null) _economy.BountyPaid += OnBountyPaid;
            if (_hill == null) _hill = FindAnyObjectByType<Queen>();
            if (_hill != null) _hill.Finished += OnGameFinished;
            if (_spawner != null) _spawner.onWaveStarted.AddListener(OnWaveStarted);
        }

        private void OnDisable()
        {
            EnemyHealth.Defeated -= OnEnemyDefeated;
            if (_economy != null) _economy.BountyPaid -= OnBountyPaid;
            if (_hill != null) _hill.Finished -= OnGameFinished;
            if (_spawner != null) _spawner.onWaveStarted.RemoveListener(OnWaveStarted);
        }

        private void Start()
        {
            // The scene as built, before anything has been bought or lost. Every new run starts here.
            _runStart = RunCheckpoint.Capture(0, new RunStats(), transform);
            Stats = NewStats();

            GameSettings.Apply();
            // A scene that starts in play has no "new run" moment to hand out the upgrades at.
            if (State == GameFlowState.Playing) ApplyRunStartBonuses();
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused && GameSettings.PauseOnFocusLoss && !Application.isEditor) Pause();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        private void Update()
        {
            bool pausePressed = GameControls.Pressed(GameControls.Pause);
            bool startPressed = pausePressed && GameControls.Find(GameControls.Pause).activeControl?.device is Gamepad;
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
        public void StartNewRun(RunMode mode)
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
            SetSpeedIndex(0);
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

        /// <summary>Called by <see cref="EndlessMode"/> as each endless wave starts.</summary>
        public void ReportEndlessWave(int waveNumber)
        {
            Stats.WaveReached = Mathf.Max(Stats.WaveReached, waveNumber);
            Stats.TotalWaves = 0; // no last wave to count towards
        }

        // ---------- Internals ----------

        protected override void Enter(GameFlowState next)
        {
            GameFlowState previous = State;
            base.Enter(next);

            // Leaving play (pause, run end, quit) is the natural moment to write progress.
            if (previous == GameFlowState.Playing && next != GameFlowState.Playing) SaveStore.SaveIfDirty();
        }

        private void LeaveRun()
        {
            if (State == GameFlowState.Title || State == GameFlowState.Home) return;

            DiscardWaveCheckpoint();
            _counted = null;
            _honeydewPaid = 0;
            if (_custom != null) _custom.StopCustomSpawning();
            if (_spawner != null) _spawner.RestartFromWave(0);
            RunCheckpoint.ClearField();
            if (_placer != null) _placer.CancelInteraction();
        }

        private void DiscardWaveCheckpoint()
        {
            if (_waveStart == null) return;

            _waveStart.Discard();
            _waveStart = null;
        }

        private RunStats NewStats()
        {
            bool counted = Mode == RunMode.Campaign && _spawner != null;
            return new RunStats { TotalWaves = counted ? _spawner.TotalWaves : 0 };
        }

        // The permanent upgrades bought at Home, handed out on top of the scene's own numbers.
        private void ApplyRunStartBonuses()
        {
            if (_hill != null) _hill.SetBonusHealth(PermanentUpgrades.HillHealthBonus);

            WalletBehaviour economy = _economy;
            int food = PermanentUpgrades.StartingFoodBonus;
            if (economy != null && food > 0) economy.ResetFood(economy.CurrentFood + food);
        }

        // The "auto-start waves" setting: campaign waves follow each other without the next-wave key.
        private void AutoStartNextWave()
        {
            if (!GameSettings.AutoStartWaves || Mode != RunMode.Campaign || _spawner == null) return;

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
            if (Mode != RunMode.Campaign) return; // endless waves report themselves

            Stats.WaveReached = Mathf.Max(Stats.WaveReached, waveNumber);
            Stats.TotalWaves = totalWaves;

            DiscardWaveCheckpoint();
            _waveStart = RunCheckpoint.Capture(waveNumber - 1, Stats, transform);
        }

        private void OnEnemyDefeated(GameObject enemy)
        {
            if (State == GameFlowState.Playing) Stats.Kills++;
        }

        private void OnBountyPaid(int gold)
        {
            if (State == GameFlowState.Playing) Stats.FoodEarned += gold;
        }

        // Nothing else declares victory: the spawner only knows when it has finished spawning.
        private void CheckForVictory()
        {
            if (Mode != RunMode.Campaign) return; // an endless run ends only in defeat
            if (_spawner == null || _spawner.CurrentState != EnemySpawner.SpawnerState.Completed) return;
            if (_spawner.LivingEnemyCount > 0) return;

            if (_hill == null || _hill.IsFinished) return;

            _hill.TriggerVictory();
        }

        private void OnGameFinished(bool victory)
        {
            if (State != GameFlowState.Playing) return;

            LastRunWasVictory = victory;
            Tower.GetActive(_towers);
            Stats.TowersStanding = _towers.Count;

            LastRunResult = Records.Submit(LevelId, Stats, victory, _counted);
            _counted = Stats.Clone();

            // Only what this ending adds: a run retried after a defeat has been paid for its earlier waves.
            int earned = PermanentUpgrades.HoneydewFor(Stats.WaveReached, victory);
            MetaProgress.AddCurrency(earned - _honeydewPaid);
            _honeydewPaid = Mathf.Max(_honeydewPaid, earned);
            LastRunHoneydew = _honeydewPaid;

            Enter(GameFlowState.RunEnd);
        }
    }
}
