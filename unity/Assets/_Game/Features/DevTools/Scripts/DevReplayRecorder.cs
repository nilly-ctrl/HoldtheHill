#if UNITY_EDITOR || DEVELOPMENT_BUILD
using HoldTheHill.Features.Economy;
using HoldTheHill.Features.Progression;
using HoldTheHill.Features.Hero;
using HoldTheHill.Features.GameFlow;
using HoldTheHill.Features.Enemies;
using System;
using System.Collections.Generic;
using HoldTheHill.Features.Towers;
using UnityEngine;

namespace HoldTheHill.Features.DevTools
{
    /// <summary>
    /// Records a run as a seed plus the player's commands on a fixed-step clock, and writes it to
    /// <c>persistentDataPath/replays</c> as JSON (see <see cref="ReplayRecording"/>).
    /// </summary>
    /// <remarks>
    /// Ability uses, skill unlocks, wave starts and field resets come from the events those classes
    /// already raise. Tower placing, selling and upgrading come from the static events on
    /// <see cref="GameTowerPlacer"/>, so only what the player did is recorded. Towers removed or
    /// reset some other way (a field reset, a hornet) are not commands.
    ///
    /// Commands are accurate to one fixed step. Console commands are recorded as "dev" commands
    /// so a replay that used cheats stays in step.
    /// </remarks>
    [AddComponentMenu("Hold the Hill/DevTools/Dev Replay Recorder")]
    public class DevReplayRecorder : MonoBehaviour
    {
        // Console commands that do not change the run, so are not worth replaying.
        private static readonly HashSet<string> NotRecorded = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "help", "?", "list", "rec", "replay", "tune", "perf", "speed", "god",
        };

        private readonly List<Tower> _towers = new List<Tower>();
        private ReplayRecording _recording;
        private EnemySpawner _spawner;
        private int _tick;
        private int _retries;
        private int _frames;

        public static DevReplayRecorder Instance { get; private set; }

        /// <summary>Start a recording automatically whenever the field is reset and none is running.</summary>
        public bool AutoRecord { get; set; }

        public bool IsRecording => _recording != null;

        /// <summary>The recording in progress, or null.</summary>
        public ReplayRecording Current => _recording;

        /// <summary>Fixed steps since the recording began.</summary>
        public int Tick => _tick;

        /// <summary>Path of the last file written, or null.</summary>
        public string LastSavedPath { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        // The placer in the scene, found once when recording starts; its events are instance events.
        private RunFlow _flow;
        private TowerPlacer _placer;

        private void OnEnable()
        {
            if (Instance != this) return;

            WardenAbility.Used += OnAbilityUsed;
            _placer = FindAnyObjectByType<TowerPlacer>();
            if (_placer != null)
            {
                _placer.TowerPlaced += OnTowerPlaced;
                _placer.TowerUpgraded += OnTowerUpgraded;
                _placer.TowerSold += OnTowerSold;
            }
            SkillTree.OnSkillUnlocked += OnSkillUnlocked;
            _flow = FindAnyObjectByType<RunFlow>();
            if (_flow != null)
            {
                _flow.FieldReset += OnFieldReset;
                _flow.StateChanged += OnStateChanged;
            }
            DevCommandRegistry.Default.Executed += OnConsoleCommand;
            HookSpawner();
        }

        private void OnDisable()
        {
            WardenAbility.Used -= OnAbilityUsed;
            if (_placer != null)
            {
                _placer.TowerPlaced -= OnTowerPlaced;
                _placer.TowerUpgraded -= OnTowerUpgraded;
                _placer.TowerSold -= OnTowerSold;
            }
            SkillTree.OnSkillUnlocked -= OnSkillUnlocked;
            if (_flow != null)
            {
                _flow.FieldReset -= OnFieldReset;
                _flow.StateChanged -= OnStateChanged;
            }
            DevCommandRegistry.Default.Executed -= OnConsoleCommand;
            if (_spawner != null) _spawner.onWaveStarted.RemoveListener(OnWaveStarted);
            _spawner = null;
        }

        private void OnDestroy()
        {
            if (IsRecording) StopRecording("Stopped");
            if (Instance == this) Instance = null;
        }

        private void HookSpawner()
        {
            if (_spawner != null) return;

            _spawner = FindAnyObjectByType<EnemySpawner>();
            if (_spawner != null) _spawner.onWaveStarted.AddListener(OnWaveStarted);
        }

        private void Start()
        {
            HookSpawner();
        }

        // ---------- start and stop ----------

        /// <summary>Starts recording from this moment. False, with a reason, if it cannot.</summary>
        public bool StartRecording(out string error)
        {
            error = null;
            if (IsRecording)
            {
                error = "Already recording.";
                return false;
            }

            if (DevReplayPlayer.Instance != null && DevReplayPlayer.Instance.IsPlaying)
            {
                error = "A replay is playing; stop it first.";
                return false;
            }

            HookSpawner();
            RunFlow flow = _flow;

            int seed = unchecked((int)(DateTime.UtcNow.Ticks & 0x7FFFFFFF));
            UnityEngine.Random.InitState(seed);

            Tower.GetActive(_towers);
            WalletBehaviour economy = FindAnyObjectByType<WalletBehaviour>();
            _recording = new ReplayRecording
            {
                unityVersion = Application.unityVersion,
                mapId = flow != null ? flow.MapId : (_spawner != null ? _spawner.ActiveMapId : "Unknown"),
                mode = flow != null ? flow.Mode.ToString() : RunMode.Campaign.ToString(),
                seed = seed,
                fixedDeltaTime = Time.fixedDeltaTime,
                startWave = _spawner != null ? _spawner.CurrentWaveIndex : 0,
                startFood = economy != null ? economy.CurrentFood : 0,
                startTowers = _towers.Count,
            };

            _tick = 0;
            _retries = flow != null ? flow.Stats.WaveRetries : 0;
            _frames = 0;
            if (_recording.StartedMidRun)
            {
                Debug.LogWarning("[DevTools] Recording started mid-run: a replay cannot rebuild what was already built.");
            }

            return true;
        }

        /// <summary>Ends the recording, writes the file and returns its path (null if nothing was recording).</summary>
        public string StopRecording(string result)
        {
            if (_recording == null) return null;

            ReplayRecording finished = _recording;
            _recording = null;
            finished.endTick = _tick;
            finished.result = string.IsNullOrEmpty(result) ? "Stopped" : result;
            try
            {
                LastSavedPath = DevReplayFiles.Save(finished);
                Debug.Log("[DevTools] Replay saved: " + LastSavedPath + " (" + finished.commands.Count + " commands, "
                    + finished.endTick + " fixed steps)");
                return LastSavedPath;
            }
            catch (Exception ex)
            {
                Debug.LogError("[DevTools] Could not save the replay: " + ex.Message);
                return null;
            }
        }

        // ---------- observing ----------

        private void FixedUpdate()
        {
            if (_recording != null) _tick++;
        }

        private void OnTowerPlaced(Tower tower)
        {
            if (_recording == null || tower == null) return;

            Vector3 p = tower.transform.position;
            _recording.Add(_tick, ReplayCommandType.Place, tower.name, p.x, p.y, tower.Level);
        }

        private void OnTowerUpgraded(Tower tower)
        {
            if (_recording == null || tower == null) return;

            Vector3 p = tower.transform.position;
            _recording.Add(_tick, ReplayCommandType.Upgrade, tower.name, p.x, p.y, tower.Level);
        }

        private void OnTowerSold(Tower tower)
        {
            if (_recording == null || tower == null) return;

            Vector3 p = tower.transform.position;
            _recording.Add(_tick, ReplayCommandType.Sell, tower.name, p.x, p.y, tower.Level);
        }

        // ---------- events ----------

        private void OnAbilityUsed(WardenAbility ability)
        {
            if (_recording == null || ability == null) return;

            string id = ability.Data != null ? ability.Data.Id : ability.GetType().Name;
            Vector3 at = Warden.Instance != null ? Warden.Instance.transform.position : Vector3.zero;
            _recording.Add(_tick, ReplayCommandType.Ability, id, at.x, at.y);
        }

        private void OnSkillUnlocked(string id)
        {
            _recording?.Add(_tick, ReplayCommandType.Skill, id);
        }

        private void OnWaveStarted(int waveNumber, int totalWaves)
        {
            if (_recording == null) return;

            // Endless waves are started by EndlessMode itself and come back on their own.
            RunFlow flow = _flow;
            if (flow != null && flow.Mode != RunMode.Campaign) return;

            _recording.Add(_tick, ReplayCommandType.Wave, null, 0f, 0f, waveNumber);
        }

        private void OnFieldReset()
        {
            RunFlow flow = _flow;
            int retries = flow != null ? flow.Stats.WaveRetries : 0;
            bool retry = retries > _retries;
            _retries = retries;

            if (_recording != null)
            {
                _recording.Add(_tick, ReplayCommandType.Reset, null, 0f, 0f, retry ? 1 : 0);
            }
            else if (AutoRecord && (DevReplayPlayer.Instance == null || !DevReplayPlayer.Instance.IsPlaying))
            {
                StartRecording(out _);
            }
        }

        private void OnStateChanged(GameFlowState previous, GameFlowState current)
        {
            if (_recording != null && (current == GameFlowState.RunEnd || current == GameFlowState.Home || current == GameFlowState.Title))
            {
                RunFlow flow = _flow;
                string result = current == GameFlowState.RunEnd && flow != null
                    ? (flow.LastRunWasVictory ? "Victory" : "Defeat")
                    : "Stopped";
                StopRecording(result);
            }
        }

        private void OnConsoleCommand(string line, DevCommandResult result)
        {
            if (_recording == null || !result.Ok) return;

            List<string> words = DevCommandRegistry.Tokenize(line);
            if (words.Count == 0 || NotRecorded.Contains(words[0])) return;
            if (DevReplayPlayer.Instance != null && DevReplayPlayer.Instance.IsExecutingCommand) return;

            _recording.Add(_tick, ReplayCommandType.Dev, line);
        }

        private void Update()
        {
            // A scene that starts straight into play has no field reset to start an automatic recording.
            if (AutoRecord && _recording == null && _frames >= 0 && ++_frames == 2)
            {
                RunFlow flow = _flow;
                if ((flow == null || flow.State == GameFlowState.Playing)
                    && (DevReplayPlayer.Instance == null || !DevReplayPlayer.Instance.IsPlaying))
                {
                    StartRecording(out _);
                }
            }
        }
    }
}
#endif
