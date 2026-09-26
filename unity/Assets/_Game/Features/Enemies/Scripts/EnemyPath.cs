using System.Collections.Generic;
using HoldTheHill.Core;
using UnityEngine;

namespace HoldTheHill.Features.Enemies
{
    /// <summary>
    /// The route enemies walk. Each child object is a waypoint, visited in Hierarchy order.
    /// Shows the route in the Scene view and lays down a road when the game starts.
    /// </summary>
    public class EnemyPath : MonoBehaviour
    {
        private const string RoadName = "Road";

        [Tooltip("Width of the road in world units. Towers can't be placed on it.")]
        [SerializeField] private float _roadWidth = 1f;

        [SerializeField] private Color _roadColor = new Color(0.55f, 0.42f, 0.3f);

        private readonly List<Vector3> _waypoints = new List<Vector3>();

        /// <summary>Waypoint positions in world space, in walking order.</summary>
        public IReadOnlyList<Vector3> Waypoints => _waypoints;

        private void Awake()
        {
            _waypoints.Clear();
            foreach (Transform child in transform)
            {
                _waypoints.Add(child.position);
            }

            CreateRoad();
        }

        /// <summary>True if a grid cell centered at <paramref name="cellCenter"/> overlaps the road.</summary>
        public bool Blocks(Vector3 cellCenter, float cellSize)
        {
            // Small margin so cells that only touch the road's edge stay buildable.
            float reach = (cellSize + _roadWidth) * 0.5f - 0.01f;
            return DistanceToRoute(cellCenter) < reach;
        }

        private float DistanceToRoute(Vector2 point)
        {
            float closest = float.PositiveInfinity;
            for (int i = 0; i < _waypoints.Count - 1; i++)
            {
                closest = Mathf.Min(closest, DistanceToSegment(point, _waypoints[i], _waypoints[i + 1]));
            }
            return closest;
        }

        private static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = ab.sqrMagnitude > 0f ? Mathf.Clamp01(Vector2.Dot(point - a, ab) / ab.sqrMagnitude) : 0f;
            return Vector2.Distance(point, a + ab * t);
        }

        // One stretched square per segment. Each is lengthened by the road width so corners overlap cleanly.
        private void CreateRoad()
        {
            Transform road = new GameObject(RoadName).transform;
            road.SetParent(transform, false);

            for (int i = 0; i < _waypoints.Count - 1; i++)
            {
                Vector3 a = _waypoints[i];
                Vector3 b = _waypoints[i + 1];
                Vector3 delta = b - a;

                var segment = new GameObject($"Segment {i}").AddComponent<SpriteRenderer>();
                segment.sprite = SpriteUtil.Square;
                segment.color = _roadColor;
                segment.sortingOrder = -10;

                segment.transform.SetParent(road, true);
                segment.transform.position = (a + b) * 0.5f;
                segment.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
                segment.transform.localScale = new Vector3(delta.magnitude + _roadWidth, _roadWidth, 1f);
            }
        }

        // Yellow route line and waypoint dots, visible in the Scene view even when not playing.
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Transform previous = null;
            foreach (Transform child in transform)
            {
                if (child.name == RoadName)
                {
                    continue;
                }

                Gizmos.DrawSphere(child.position, 0.15f);
                if (previous != null)
                {
                    Gizmos.DrawLine(previous.position, child.position);
                }
                previous = child;
            }
        }
    }
}
