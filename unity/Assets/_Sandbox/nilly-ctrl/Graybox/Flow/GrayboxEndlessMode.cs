using HoldTheHill.Features.Enemies;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Runs an endless game: sends procedural waves one after another, each a few seconds after
    /// the field is clear, until the hill falls. Idle unless <see cref="GrayboxGameFlow.Mode"/> is
    /// <see cref="GrayboxRunMode.Endless"/>.
    /// </summary>
    /// <remarks>
    /// The waves come from <see cref="GrayboxCustomSpawner.SpawnProceduralWave"/>, not from the
    /// team's <c>EnemySpawner</c>, so an endless run has no wave checkpoint and no "retry wave"
    /// (docs/PLAN_MENUS_AND_FLOW.md, notes from theme 1).
    /// </remarks>
    [AddComponentMenu("Hold the Hill/Graybox/Graybox Endless Mode")]
    public class GrayboxEndlessMode : MonoBehaviour
    {
        [Tooltip("Seconds of calm between a cleared field and the next wave.")]
        [SerializeField, Min(0f)] private float _breakSeconds = 4f;

        private GrayboxCustomSpawner _custom;
        private int _wave;
        private float _break;

        /// <summary>The wave in progress, 1-based. Zero before the first.</summary>
        public int Wave => _wave;

        private void Awake() => _custom = FindAnyObjectByType<GrayboxCustomSpawner>();

        private void OnEnable() => GrayboxGameFlow.FieldReset += OnFieldReset;

        private void OnDisable() => GrayboxGameFlow.FieldReset -= OnFieldReset;

        private void OnFieldReset()
        {
            _wave = 0;
            _break = 1f; // a moment to look at the field before wave 1
        }

        private void Update()
        {
            GrayboxGameFlow flow = GrayboxGameFlow.Instance;
            if (flow == null || _custom == null || flow.Mode != GrayboxRunMode.Endless || flow.State != GameFlowState.Playing) return;
            if (GrayboxProceduralWaveGenerator.Instance == null) return; // nothing to make waves from

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
