using System.Collections.Generic;
using HoldTheHill.Features.Enemies;
using UnityEngine;

namespace HoldTheHill.Features.Combat
{
    /// <summary>
    /// Tower that periodically lays proximity landmines onto nearby road segments.
    /// </summary>
    [AddComponentMenu("Hold the Hill/Combat/Mine Layer Tower")]
    public class MineLayerTower : TimedTowerWeapon
    {
        [Header("Mine Layer Parameters")]
        [SerializeField, Min(1f)] private float _range = 4.0f;
        [SerializeField, Min(0.5f)] private float _layInterval = 2.8f;
        [SerializeField] private GameObject _minePrefab;

        private EnemyPath _path;

        /// <summary>Raised after each mine is placed. For visuals.</summary>
        public event System.Action<ProximityMine> MineLaid;

        private void Awake()
        {
            _path = FindAnyObjectByType<EnemyPath>();
        }

        protected override float Interval => _layInterval;

        protected override void Trigger() => LayMine();

        private void LayMine()
        {
            if (_minePrefab == null)
            {
                return;
            }

            Vector3 spot = PickPathLocationInRange();
            GameObject mineObj = Instantiate(_minePrefab, spot, Quaternion.identity);

            var mine = mineObj.GetComponent<ProximityMine>();
            if (mine != null)
            {
                mine.SourceTower = gameObject;
            }

            MineLaid?.Invoke(mine);
        }

        private Vector3 PickPathLocationInRange()
        {
            if (_path == null)
            {
                _path = FindAnyObjectByType<EnemyPath>();
                if (_path == null)
                {
                    return transform.position;
                }
            }

            var waypoints = _path.Waypoints;
            if (waypoints == null || waypoints.Count < 2)
            {
                return transform.position;
            }

            List<Vector3> validSpots = new List<Vector3>();
            for (int i = 0; i < waypoints.Count - 1; i++)
            {
                Vector3 a = waypoints[i];
                Vector3 b = waypoints[i + 1];

                for (float t = 0f; t <= 1f; t += 0.25f)
                {
                    Vector3 sample = Vector3.Lerp(a, b, t);
                    if (Vector3.Distance(transform.position, sample) <= _range)
                    {
                        validSpots.Add(sample);
                    }
                }
            }

            if (validSpots.Count > 0)
            {
                return validSpots[Random.Range(0, validSpots.Count)];
            }

            return transform.position;
        }

        public override void OnUpgraded(int level)
        {
            _range *= 1.2f;
            _layInterval = Mathf.Max(0.8f, _layInterval * 0.75f);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _range);
        }
    }
}
