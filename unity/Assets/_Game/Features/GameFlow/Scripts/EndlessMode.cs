using HoldTheHill.Features.GameFlow;
using HoldTheHill.Features.Enemies;
using UnityEngine;

namespace HoldTheHill.Features.GameFlow
{
    /// <summary>
    /// Runs an endless game: sends procedural waves one after another, each a few seconds after
    /// the field is clear, until the hill falls. Idle unless <see cref="RunFlow.Mode"/> is
    /// <see cref="RunMode.Endless"/>.
    /// </summary>
    /// <remarks>
    /// The waves come from <see cref="CustomSpawner.SpawnProceduralWave"/>, not from the
    /// team's <c>EnemySpawner</c>, so an endless run has no wave checkpoint and no "retry wave"
    /// (docs/PLAN_MENUS_AND_FLOW.md, notes from theme 1).
    /// </remarks>
    [AddComponentMenu("Hold the Hill/Endless Mode")]
    public class EndlessMode : MonoBehaviour
    {
        [Tooltip("Seconds of calm between a cleared field and the next wave.")]
        [SerializeField, Min(0f)] private float _breakSeconds = 4f;

        private CustomSpawner _custom;
        private int _wave;
        private float _break;

        /// <summary>The wave in progress, 1-based. Zero before the first.</summary>
        public int Wave => _wave;

        private RunFlow _flow;

        private void Awake()
        {
            _custom = FindAnyObjectByType<CustomSpawner>();
            _flow = GetComponent<RunFlow>();
            if (_flow == null) _flow = FindAnyObjectByType<RunFlow>();
        }

        private void OnEnable()
        {
            if (_flow != null) _flow.FieldReset += OnFieldReset;
        }

        private void OnDisable()
        {
            if (_flow != null) _flow.FieldReset -= OnFieldReset;
        }

        private void OnFieldReset()
        {
            _wave = 0;
            _break = 1f; // a moment to look at the field before wave 1
        }

        private void Update()
        {
            RunFlow flow = _flow;
            if (flow == null || _custom == null || flow.Mode != RunMode.Endless || flow.State != GameFlowState.Playing) return;
            if (ProceduralWaveGenerator.Instance == null) return; // nothing to make waves from

            if (_custom.IsCustomSpawning || FindAnyObjectByType<EnemyHealth>() != null)
            {
                _break = _breakSeconds;
                return;
            }

            _break -= Time.deltaTime;
            if (_break > 0f) return;

            _wave++;
            _break = _breakSeconds;
            _custom.SpawnProceduralWave(_wave);
            flow.ReportEndlessWave(_wave);
        }
    }
}
