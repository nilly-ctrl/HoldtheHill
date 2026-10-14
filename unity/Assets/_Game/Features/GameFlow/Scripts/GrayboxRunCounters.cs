using HoldTheHill.Features.Enemies;
using UnityEngine;

namespace HoldTheHill.Features.GameFlow
{
    /// <summary>
    /// The tuning numbers for the run in progress: enemies spawned, killed and leaked, and real
    /// seconds played. Read by <see cref="GrayboxStatsPanel"/> and <see cref="GrayboxPixelHud"/>.
    /// </summary>
    /// <remarks>
    /// Was half of <c>GrayboxHud</c>; the keys it also read live in <see cref="GrayboxRunHotkeys"/>.
    /// It draws nothing and sets nothing: the flow controller owns time and the run.
    /// </remarks>
    [AddComponentMenu("Hold the Hill/Graybox/Run Counters")]
    public class GrayboxRunCounters : MonoBehaviour
    {
        [Tooltip("The spawner to count from. Found automatically if left empty.")]
        [SerializeField] private EnemySpawner _spawner;

        private int _spawned;
        private int _killed;
        private float _elapsed;

        /// <summary>Real seconds while the game was playing (menus and pause excluded).</summary>
        public float ElapsedSeconds => _elapsed;

        /// <summary>Enemies spawned since the run (or the last field reset) began.</summary>
        public int Spawned => _spawned;

        /// <summary>Enemies killed since the run began.</summary>
        public int Killed => _killed;

        /// <summary>Enemies that got past the towers: spawned, and neither killed nor still alive.</summary>
        public int Leaked => _spawner != null ? Mathf.Max(0, _spawned - _killed - _spawner.LivingEnemyCount) : 0;

        private GrayboxGameFlow _flow;

        private void Awake()
        {
            if (_spawner == null) _spawner = FindAnyObjectByType<EnemySpawner>();
            _flow = GetComponent<GrayboxGameFlow>();
            if (_flow == null) _flow = FindAnyObjectByType<GrayboxGameFlow>();
        }

        private void OnEnable()
        {
            EnemyHealth.Defeated += OnEnemyDefeated;
            if (_flow != null) _flow.FieldReset += OnFieldReset;
            if (_spawner != null) _spawner.onEnemySpawned.AddListener(OnEnemySpawned);
        }

        private void OnDisable()
        {
            EnemyHealth.Defeated -= OnEnemyDefeated;
            if (_flow != null) _flow.FieldReset -= OnFieldReset;
            if (_spawner != null) _spawner.onEnemySpawned.RemoveListener(OnEnemySpawned);
        }

        // The flow controller rewound the field; enemies it removed were neither killed nor leaked.
        private void OnFieldReset()
        {
            _spawned = 0;
            _killed = 0;
            _elapsed = 0f;
        }

        private void OnEnemySpawned(GameObject enemy) => _spawned++;

        private void OnEnemyDefeated(GameObject enemy) => _killed++;

        private void Update()
        {
            if (_flow == null || _flow.IsPlaying) _elapsed += Time.unscaledDeltaTime;
        }
    }
}
