using UnityEngine;

namespace HoldTheHill.Features.Enemies
{
    /// <summary>
    /// Component attached to enemy prefabs that spawns multiple mini-ants upon death,
    /// inheriting current path progress.
    /// </summary>
    [AddComponentMenu("Hold the Hill/Enemies/Enemy Splitter")]
    public class EnemySplitter : MonoBehaviour
    {
        [Header("Split Parameters")]
        [SerializeField] private GameObject _miniAntPrefab;
        [SerializeField, Min(1)] private int _splitCount = 2;
        [SerializeField, Min(0f)] private float _spawnOffsetRadius = 0.25f;

        private EnemyMover _mover;
        private bool _hasSplit;

        private void Awake()
        {
            _mover = GetComponent<EnemyMover>();
        }

        private void OnEnable()
        {
            _hasSplit = false;
            EnemyHealth.Defeated += OnDefeated;
        }

        private void OnDisable()
        {
            EnemyHealth.Defeated -= OnDefeated;
        }

        // Split the moment death is announced. Polling IsDead in Update missed every kill that
        // landed after this object's Update in a frame (DoT ticks, later projectiles), because
        // EnemyHealth destroys the enemy at the end of that same frame.
        private void OnDefeated(GameObject enemy)
        {
            if (enemy == gameObject)
            {
                Split();
            }
        }

        public void Split()
        {
            if (_hasSplit || _miniAntPrefab == null)
            {
                return;
            }

            _hasSplit = true;
            Vector3 pos = transform.position;

            for (int i = 0; i < _splitCount; i++)
            {
                Vector3 offset = Random.insideUnitCircle * _spawnOffsetRadius;
                Vector3 spawnPos = pos + offset;

                GameObject mini = Instantiate(_miniAntPrefab, spawnPos, Quaternion.identity);

                var miniMover = mini.GetComponent<EnemyMover>();
                if (miniMover != null && _mover != null)
                {
                    // Inherit path progress from parent
                    float travel = _mover.DistanceTravelled;
                    miniMover.SnapToStart();

                    // Advance mover to match parent distance
                    if (travel > 0f)
                    {
                        miniMover.UpdatePositionToMatchDistance(travel);
                    }
                }
            }
        }
    }
}
