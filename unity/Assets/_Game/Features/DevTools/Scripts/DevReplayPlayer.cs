#if UNITY_EDITOR || DEVELOPMENT_BUILD
using HoldTheHill.Features.Economy;
using HoldTheHill.Features.GameFlow;
using HoldTheHill.Features.Progression;
using HoldTheHill.Features.Hero;
using HoldTheHill.Features.Enemies;
using System;
using System.Collections.Generic;
using System.Text;
using HoldTheHill.Features.Towers;
using UnityEngine;

namespace HoldTheHill.Features.DevTools
{
    /// <summary>
    /// Plays a <see cref="ReplayRecording"/> back by giving the game the same commands at the same
    /// fixed-step ticks: the same calls the placer, the skill tree, the Warden's abilities and the
    /// spawner expose to the HUD.
    /// </summary>
    /// <remarks>
    /// This replays commands, not frames. The run is started fresh, UnityEngine.Random is set to
    /// the recorded seed, and each command is applied when the tick comes round. A command that
    /// cannot be applied (not enough gold, a cell now blocked, an ability on cooldown) is counted
    /// as a desync and skipped. See the README for what is not reproduced.
    /// </remarks>
    [AddComponentMenu("Hold the Hill/DevTools/Dev Replay Player")]
    public class DevReplayPlayer : MonoBehaviour
    {
        private const float CellTolerance = 0.5f;

        private readonly List<Tower> _towers = new List<Tower>();
        private readonly StringBuilder _problems = new StringBuilder();
        private ReplayRecording _recording;
        private GameTowerPlacer _placer;
        private RunFlow _flow;
        private WalletBehaviour _economy;
        private EnemySpawner _spawner;
        private int _tick;
        private int _next;

        public static DevReplayPlayer Instance { get; private set; }

        public bool IsPlaying => _recording != null;

        /// <summary>True while a recorded console command is being run, so the recorder does not record it twice.</summary>
        public bool IsExecutingCommand { get; private set; }

        /// <summary>Commands applied so far.</summary>
        public int Applied { get; private set; }

        /// <summary>Commands that could not be applied.</summary>
        public int Desyncs { get; private set; }

        public int Total => _recording != null ? _recording.commands.Count : 0;

        public int Tick => _tick;

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

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Starts a fresh run and plays the recording into it. False, with a reason, if it cannot.</summary>
        public bool Play(ReplayRecording recording, out string message)
        {
            message = null;
            if (recording == null)
            {
                message = "No replay to play.";
                return false;
            }

            string problem = ReplaySerializer.Validate(recording);
            if (problem != null)
            {
                message = problem;
                return false;
            }

            if (DevReplayRecorder.Instance != null && DevReplayRecorder.Instance.IsRecording)
            {
                message = "Stop recording first (rec stop).";
                return false;
            }

            _placer = FindAnyObjectByType<GameTowerPlacer>();
            _spawner = FindAnyObjectByType<EnemySpawner>();
            _economy = FindAnyObjectByType<WalletBehaviour>();
            _flow = FindAnyObjectByType<RunFlow>();
            if (_placer == null)
            {
                message = "No tower placer in the scene; there is nothing to replay into.";
                return false;
            }

            var notes = new StringBuilder();
            if (recording.StartedMidRun)
            {
                notes.Append(" Recorded mid-run (wave ").Append(recording.startWave + 1).Append(", ")
                    .Append(recording.startTowers).Append(" towers): the start is not rebuilt, so expect desyncs.");
            }

            if (Mathf.Abs(Time.fixedDeltaTime - recording.fixedDeltaTime) > 1e-6f)
            {
                notes.Append(" Fixed step is ").Append(Time.fixedDeltaTime).Append("s now but was ")
                    .Append(recording.fixedDeltaTime).Append("s when recorded.");
            }

            string mapNow = _flow != null ? _flow.MapId : (_spawner != null ? _spawner.ActiveMapId : null);
            if (!string.IsNullOrEmpty(recording.mapId) && !string.IsNullOrEmpty(mapNow) && recording.mapId != mapNow)
            {
                notes.Append(" Recorded on map ").Append(recording.mapId).Append(", this is ").Append(mapNow).Append('.');
            }

            _recording = recording;
            _tick = 0;
            _next = 0;
            Applied = 0;
            Desyncs = 0;
            _problems.Length = 0;

            RunFlow flow = _flow;
            if (flow != null)
            {
                RunMode mode = RunMode.Campaign;
                if (!string.IsNullOrEmpty(recording.mode)) Enum.TryParse(recording.mode, out mode);
                flow.StartNewRun(mode);
            }

            UnityEngine.Random.InitState(recording.seed);
            message = "Playing " + recording.commands.Count + " commands (seed " + recording.seed + ")." + notes;
            return true;
        }

        public void Stop()
        {
            if (_recording == null) return;

            Debug.Log("[DevTools] Replay stopped at tick " + _tick + ": " + Applied + " applied, " + Desyncs + " desyncs.");
            _recording = null;
        }

        public string Status()
        {
            if (_recording == null) return "No replay is playing.";
            return "Replay: tick " + _tick + "/" + _recording.endTick + ", " + Applied + " of " + Total
                + " commands applied, " + Desyncs + " desyncs.";
        }

        private void FixedUpdate()
        {
            if (_recording == null) return;

            _tick++;
            List<ReplayCommand> commands = _recording.commands;
            while (_next < commands.Count && commands[_next].tick <= _tick)
            {
                Execute(commands[_next]);
                _next++;
                if (_recording == null) return; // a command stopped the replay
            }

            if (_next >= commands.Count)
            {
                string tail = Desyncs == 0 ? "no desyncs" : Desyncs + " desyncs:" + _problems;
                Debug.Log("[DevTools] Replay finished: " + Applied + " commands applied, " + tail
                    + ". The run now continues under your control.");
                _recording = null;
            }
        }

        // ---------- commands ----------

        private void Execute(ReplayCommand c)
        {
            string failure;
            switch (c.type)
            {
                case ReplayCommandType.Place: failure = DoPlace(c); break;
                case ReplayCommandType.Sell: failure = DoSell(c); break;
                case ReplayCommandType.Upgrade: failure = DoUpgrade(c); break;
                case ReplayCommandType.Ability: failure = DoAbility(c); break;
                case ReplayCommandType.Skill: failure = DoSkill(c); break;
                case ReplayCommandType.Wave: failure = DoWave(c); break;
                case ReplayCommandType.Reset: failure = DoReset(c); break;
                case ReplayCommandType.Dev: failure = DoDev(c); break;
                default: failure = "unknown command type"; break;
            }

            if (failure == null)
            {
                Applied++;
                return;
            }

            Desyncs++;
            if (Desyncs <= 10)
            {
                _problems.Append("\n  tick ").Append(c.tick).Append(' ').Append(c.type).Append(' ').Append(c.name).Append(": ").Append(failure);
            }
        }

        private string DoPlace(ReplayCommand c)
        {
            TowerData data = FindTowerData(c.name);
            if (data == null) return "no tower called '" + c.name + "' in the catalog";

            var cell = new Vector3(c.x, c.y, 0f);
            if (!_placer.CanBuildAt(cell)) return "cell is blocked or occupied";

            int cost = _placer.CostOf(data);
            WalletBehaviour economy = _economy;
            if (economy != null && !economy.TrySpend(data.CostResource, cost)) return "cannot afford " + cost;

            _placer.Place(data, cell, cost);
            return null;
        }

        private string DoSell(ReplayCommand c)
        {
            Tower tower = FindTowerAt(c.x, c.y, c.name);
            if (tower == null) return "no tower on that cell";

            WalletBehaviour economy = _economy;
            if (economy != null) economy.EarnFood(tower.RefundValue);
            // Without the sell sound and puff the placer adds, which need its selection.
            Destroy(tower.gameObject);
            return null;
        }

        private string DoUpgrade(ReplayCommand c)
        {
            Tower tower = FindTowerAt(c.x, c.y, c.name);
            if (tower == null) return "no tower on that cell";
            if (!tower.CanUpgrade) return "tower is already at its top level";

            int cost = _placer.UpgradeCost(tower);
            WalletBehaviour economy = _economy;
            if (economy != null && !economy.TrySpendFood(cost)) return "cannot afford " + cost;

            tower.Upgrade();
            tower.TotalFoodInvested += cost;
            return null;
        }

        private static string DoAbility(ReplayCommand c)
        {
            Warden warden = Warden.Instance;
            if (warden == null) return "no Warden in the scene";
            if (warden.Health != null && warden.Health.IsDowned) return "the Warden is down";

            foreach (WardenAbility ability in warden.Abilities)
            {
                string id = ability.Data != null ? ability.Data.Id : ability.GetType().Name;
                if (id != c.name) continue;

                // Abilities act on what is near the Warden, so put it where it stood.
                warden.transform.position = new Vector3(c.x, c.y, warden.transform.position.z);
                return ability.TryUse() ? null : "ability not ready or has no target";
            }

            return "the Warden has no ability '" + c.name + "'";
        }

        private static string DoSkill(ReplayCommand c)
        {
            SkillTree tree = SkillTree.Instance;
            if (tree == null) return "no skill tree";
            if (tree.IsUnlocked(c.name)) return null;
            return tree.TryUnlock(c.name) ? null : "cannot unlock (points or prerequisite)";
        }

        private string DoWave(ReplayCommand c)
        {
            if (_spawner == null) return "no spawner";
            if (_spawner.CurrentWaveNumber > c.value) return null; // already started, e.g. by auto-start

            bool waiting = _spawner.CurrentState == EnemySpawner.SpawnerState.WaitingForNextWave
                || _spawner.CurrentState == EnemySpawner.SpawnerState.Idle;
            if (!waiting || _spawner.CurrentWaveNumber != c.value) return "spawner is not ready for wave " + c.value;

            _spawner.StartNextWave();
            return null;
        }

        private static string DoReset(ReplayCommand c)
        {
            RunFlow flow = FindAnyObjectByType<RunFlow>();
            if (flow == null) return "no game flow";

            if (c.value == 1) flow.RetryWave();
            else flow.StartNewRun();
            return null;
        }

        private string DoDev(ReplayCommand c)
        {
            IsExecutingCommand = true;
            try
            {
                DevCommandResult result = DevCommandRegistry.Default.Execute(c.name);
                return result.Ok ? null : result.Message;
            }
            finally
            {
                IsExecutingCommand = false;
            }
        }

        // ---------- lookups ----------

        private TowerData FindTowerData(string name)
        {
            TowerCatalog catalog = _placer.Catalog;
            if (catalog == null) return null;

            for (int i = 0; i < catalog.Count; i++)
            {
                if (catalog[i] != null && catalog[i].DisplayName == name) return catalog[i];
            }

            return null;
        }

        private Tower FindTowerAt(float x, float y, string name)
        {
            Tower.GetActive(_towers);
            Tower best = null;
            float bestDistance = CellTolerance;
            var point = new Vector2(x, y);
            foreach (Tower tower in _towers)
            {
                float distance = Vector2.Distance(point, tower.transform.position);
                if (distance > bestDistance) continue;
                if (!string.IsNullOrEmpty(name) && tower.name != name) continue;

                best = tower;
                bestDistance = distance;
            }

            return best;
        }
    }
}
#endif
