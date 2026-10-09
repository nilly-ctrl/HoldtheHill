using System.Collections.Generic;
using HoldTheHill.Features.Enemies;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Puts a power-up on the map at the start of every few waves, beside the trail and away
    /// from the hill, so fetching it means leaving the line. Takes the power-ups in turn.
    /// </summary>
    [AddComponentMenu("Hold the Hill/Graybox/Graybox Power-Up Spawner")]
    public class GrayboxPowerUpSpawner : MonoBehaviour
    {
        [Tooltip("Offered in this order, one each time.")]
        [SerializeField] private List<GrayboxPowerUpData> _powerUps = new List<GrayboxPowerUpData>();

        [Tooltip("A power-up appears on every this-many-th wave.")]
        [SerializeField, Min(1)] private int _everyWaves = 3;

        [Tooltip("How far to the side of the trail it lies, in world units.")]
        [SerializeField, Min(0f)] private float _besideTrail = 1.6f;

        private EnemySpawner _spawner;
        private EnemyPath _path;
        private int _next;

        public void Configure(List<GrayboxPowerUpData> powerUps)
        {
            _powerUps = powerUps;
        }

        private void Awake()
        {
            _spawner = FindAnyObjectByType<EnemySpawner>();
            _path = FindAnyObjectByType<EnemyPath>();
        }

        private void OnEnable()
        {
            if (_spawner != null)
            {
                _spawner.onWaveStarted.AddListener(OnWaveStarted);
            }
        }

        private void OnDisable()
        {
            if (_spawner != null)
            {
                _spawner.onWaveStarted.RemoveListener(OnWaveStarted);
            }
        }

        private void OnWaveStarted(int wave, int total)
        {
            if (wave % _everyWaves == 0)
            {
                SpawnNext();
            }
        }

        /// <summary>Places the next power-up in the list now. Returns the pickup, or null with nothing to place.</summary>
        public GameObject SpawnNext()
        {
            if (_powerUps.Count == 0)
            {
                return null;
            }

            GrayboxPowerUpData data = _powerUps[_next % _powerUps.Count];
            _next++;
            return data != null && data.PickupPrefab != null
                ? Instantiate(data.PickupPrefab, PickSpot(), Quaternion.identity)
                : null;
        }

        // Somewhere along the first two thirds of the trail, stepped off to one side.
        private Vector3 PickSpot()
        {
            if (_path == null || _path.Waypoints.Count < 2)
            {
                return transform.position;
            }

            float along = Random.Range(0.15f, 0.65f) * _path.Length;
            Vector3 on = _path.PointAt(along);
            Vector2 ahead = (Vector2)(_path.PointAt(along + 0.1f) - on);
            Vector2 side = new Vector2(-ahead.y, ahead.x).normalized;
            if (side == Vector2.zero)
            {
                side = Vector2.up;
            }

            return on + (Vector3)(side * (_besideTrail * (Random.value < 0.5f ? -1f : 1f)));
        }
    }
}
