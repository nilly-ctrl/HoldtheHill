#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using HoldTheHill.Features.Towers;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Records a run as a seed plus the player's commands on a fixed-step clock, and writes it to
    /// <c>persistentDataPath/replays</c> as JSON (see <see cref="ReplayRecording"/>).
    /// </summary>
    /// <remarks>
    /// Nothing in the game is edited to feed this. Ability uses, skill unlocks, wave starts and
    /// field resets come from the events those classes already raise. Tower placing, selling and
    /// upgrading have no event, so each fixed step the active tower list is compared with the
    /// previous step: a new tower is a place, a destroyed one is a sell, a higher level is an
    /// upgrade. A tower removed some way other than selling would be recorded as a sell.
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

        private struct Snap
        {
            public Tower Tower;
            public int Level;
            public Vector2 Position;
            public string Name;
        }

        private readonly Dictionary<Tower, Snap> _known = new Dictionary<Tower, Snap>();
        private readonly HashSet<Tower> _seen = new HashSet<Tower>();
        private readonly List<Tower> _gone = new List<Tower>();
        private readonly List<Tower> _towers = new List<Tower>();
        private ReplayRecording _recording;
        private EnemySpawner _spawner;
        private int _tick;
        private bool _rebaseline;
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

        private void OnEnable()
        {
            if (Instance != this) return;

            GrayboxWardenAbility.Used += OnAbilityUsed;
            GrayboxSkillTree.OnSkillUnlocked += OnSkillUnlocked;
            GrayboxGameFlow.FieldReset += OnFieldReset;
            GrayboxGameFlow.StateChanged += OnStateChanged;
            DevCommandRegistry.Default.Executed += OnConsoleCommand;
            HookSpawner();
        }

        private void OnDisable()
        {
            GrayboxWardenAbility.Used -= OnAbilityUsed;
            GrayboxSkillTree.OnSkillUnlocked -= OnSkillUnlocked;
            GrayboxGameFlow.FieldReset -= OnFieldReset;
            GrayboxGameFlow.StateChanged -= OnStateChanged;
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
            GrayboxGameFlow flow = GrayboxGameFlow.Instance;

            int seed = unchecked((int)(DateTime.UtcNow.Ticks & 0x7FFFFFFF));
            UnityEngine.Random.InitState(seed);

            Tower.GetActive(_towers);
            _recording = new ReplayRecording
            {
                unityVersion = Application.unityVersion,
                mapId = flow != null ? flow.MapId : (_spawner != null ? _spawner.ActiveMapId : "Unknown"),
                mode = flow != null ? flow.Mode.ToString() : GrayboxRunMode.Campaign.ToString(),
                seed = seed,
                fixedDeltaTime = Time.fixedDeltaTime,
                startWave = _spawner != null ? _spawner.CurrentWaveIndex : 0,
                startGold = GrayboxEconomy.Instance != null ? GrayboxEconomy.Instance.CurrentGold : 0,
                startTowers = _towers.Count,
            };

            _tick = 0;
            _rebaseline = true;
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
            _known.Clear();

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
            if (_recording == null) return;

            _tick++;
            ObserveTowers();
        }

        private void ObserveTowers()
        {
            Tower.GetActive(_towers);

            if (_rebaseline)
            {
                _rebaseline = false;
                _known.Clear();
                foreach (Tower tower in _towers) _known[tower] = Snapshot(tower);
                return;
            }

            _seen.Clear();
            foreach (Tower tower in _towers)
            {
                _seen.Add(tower);
                if (_known.TryGetValue(tower, out Snap snap))
                {
                    if (tower.Level > snap.Level)
                    {
                        _recording.Add(_tick, ReplayCommandType.Upgrade, snap.Name, snap.Position.x, snap.Position.y, tower.Level);
                        snap.Level = tower.Level;
                        _known[tower] = snap;
                    }
                }
                else
                {
                    Snap added = Snapshot(tower);
                    _known[tower] = added;
                    _recording.Add(_tick, ReplayCommandType.Place, added.Name, added.Position.x, added.Position.y, added.Level);
                }
            }

            // A tower that is merely switched off (a hornet carried it away) is still known; one
            // that has been destroyed reads as null.
            _gone.Clear();
            foreach (KeyValuePair<Tower, Snap> pair in _known)
            {
                if (!_seen.Contains(pair.Key) && pair.Key == null) _gone.Add(pair.Key);
            }

            foreach (Tower gone in _gone)
            {
                Snap snap = _known[gone];
                _recording.Add(_tick, ReplayCommandType.Sell, snap.Name, snap.Position.x, snap.Position.y, snap.Level);
                _known.Remove(gone);
            }
        }

        private static Snap Snapshot(Tower tower)
        {
            Vector3 p = tower.transform.position;
            return new Snap { Tower = tower, Level = tower.Level, Position = new Vector2(p.x, p.y), Name = tower.name };
        }

        // ---------- events ----------

        private void OnAbilityUsed(GrayboxWardenAbility ability)
        {
            if (_recording == null || ability == null) return;

            string id = ability.Data != null ? ability.Data.Id : ability.GetType().Name;
            Vector3 at = GrayboxWarden.Instance != null ? GrayboxWarden.Instance.transform.position : Vector3.zero;
            _recording.Add(_tick, ReplayCommandType.Ability, id, at.x, at.y);
        }

        private void OnSkillUnlocked(string id)
        {
            _recording?.Add(_tick, ReplayCommandType.Skill, id);
        }

        private void OnWaveStarted(int waveNumber, int totalWaves)
        {
            if (_recording == null) return;

            // Endless waves are started by GrayboxEndlessMode itself and come back on their own.
            GrayboxGameFlow flow = GrayboxGameFlow.Instance;
            if (flow != null && flow.Mode != GrayboxRunMode.Campaign) return;

            _recording.Add(_tick, ReplayCommandType.Wave, null, 0f, 0f, waveNumber);
        }

        private void OnFieldReset()
        {
            GrayboxGameFlow flow = GrayboxGameFlow.Instance;
            int retries = flow != null ? flow.Stats.WaveRetries : 0;
            bool retry = retries > _retries;
            _retries = retries;

            if (_recording != null)
            {
                _recording.Add(_tick, ReplayCommandType.Reset, null, 0f, 0f, retry ? 1 : 0);
                _rebaseline = true;
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
                GrayboxGameFlow flow = GrayboxGameFlow.Instance;
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
                GrayboxGameFlow flow = GrayboxGameFlow.Instance;
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
