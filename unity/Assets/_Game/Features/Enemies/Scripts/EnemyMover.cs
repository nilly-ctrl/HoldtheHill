using UnityEngine;

namespace HoldTheHill.Features.Enemies
{
    /// <summary>
    /// Walks an enemy along an <see cref="EnemyPath"/>, waypoint by waypoint, and
    /// reports how far it has travelled so towers can target by path position.
    /// </summary>
    /// <remarks>
    /// Speed is multiplied by <see cref="EnemyHealth.SpeedMultiplier"/> each frame, which
    /// is how slow effects take hold. Nothing else moves the enemy.
    /// </remarks>
    [AddComponentMenu("Hold the Hill/Enemies/Enemy Mover")]
    public class EnemyMover : MonoBehaviour
    {
        [Tooltip("Route to walk. Found automatically if left empty.")]
        [SerializeField] private EnemyPath _path;

        [Tooltip("Base movement speed in world units per second, before any slows.")]
        [SerializeField, Min(0.01f)] private float _speed = 1.5f;

        [Tooltip("How close counts as reaching a waypoint.")]
        [SerializeField, Min(0.01f)] private float _arriveDistance = 0.05f;

        [Tooltip("If on, the enemy turns to face the direction it is walking.")]
        [SerializeField] private bool _faceTravelDirection = false;

        [Tooltip("What happens on reaching the last waypoint.")]
        [SerializeField] private EndBehaviour _onReachEnd = EndBehaviour.Despawn;

        /// <summary>What an enemy does once it runs out of path.</summary>
        public enum EndBehaviour
        {
            /// <summary>Disappears, as if it reached the Queen.</summary>
            Despawn,

            /// <summary>Stops where it is. Handy in a test scene.</summary>
            Stop,

            /// <summary>Starts again from the first waypoint, for endless testing.</summary>
            Loop
        }

        private EnemyHealth _health;
        private int _targetIndex;
        private bool _finished;

        /// <summary>Distance walked along the route so far, in world units.</summary>
        public float DistanceTravelled { get; private set; }

        /// <summary>True once the enemy has run out of path.</summary>
        public bool HasFinished => _finished;

        /// <summary>Base movement speed in world units per second, before slows and <see cref="SpeedScale"/>.</summary>
        public float Speed => _speed;

        /// <summary>
        /// Extra speed multiplier owned by the enemy's own behaviour (a charge, a roll, standing
        /// still to dig). 1 is normal, 0 stops it. Slows from <see cref="EnemyHealth"/> still apply on top.
        /// </summary>
        public float SpeedScale { get; set; } = 1f;

        /// <summary>If on, <see cref="Knockback"/> does nothing. For flyers and anything braced.</summary>
        public bool IgnoresKnockback { get; set; }

        private void Awake()
        {
            _health = GetComponent<EnemyHealth>();
            if (_path == null)
            {
                _path = FindAnyObjectByType<EnemyPath>();
            }
        }

        private void OnEnable()
        {
            // Reset for pooled enemies, which come back through OnEnable rather than Awake.
            _targetIndex = 0;
            _finished = false;
            DistanceTravelled = 0f;
            SpeedScale = 1f;
            IgnoresKnockback = false;
        }

        private void Update()
        {
            if (_finished || _path == null)
            {
                return;
            }

            // A dead enemy should stop walking even while its death animation plays out.
            if (_health != null && _health.IsDead)
            {
                return;
            }

            var waypoints = _path.Waypoints;
            if (waypoints == null || _targetIndex >= waypoints.Count)
            {
                ReachEnd();
                return;
            }

            float slow = _health != null ? _health.SpeedMultiplier : 1f;
            float step = _speed * slow * SpeedScale * Time.deltaTime;

            Vector2 position = transform.position;
            Vector2 target = waypoints[_targetIndex];
            Vector2 next = Vector2.MoveTowards(position, target, step);

            DistanceTravelled += Vector2.Distance(position, next);
            transform.position = new Vector3(next.x, next.y, transform.position.z);

            if (_faceTravelDirection)
            {
                Vector2 delta = target - position;
                if (delta.sqrMagnitude > 0.0001f)
                {
                    transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
                }
            }

            if (Vector2.Distance(next, target) <= _arriveDistance)
            {
                _targetIndex++;
            }
        }

        /// <summary>Drops the enemy at the start of the route. Used when it is spawned.</summary>
        public void SnapToStart()
        {
            if (_path == null)
            {
                _path = FindAnyObjectByType<EnemyPath>();
            }

            var waypoints = _path != null ? _path.Waypoints : null;
            if (waypoints == null || waypoints.Count == 0)
            {
                return;
            }

            transform.position = waypoints[0];
            _targetIndex = Mathf.Min(1, waypoints.Count - 1);
            _finished = false;
            DistanceTravelled = 0f;
        }

        /// <summary>
        /// Heads straight for the last waypoint from wherever the enemy is, ignoring the route
        /// in between. For flyers.
        /// </summary>
        public void SkipToLastWaypoint()
        {
            if (_path == null)
            {
                _path = FindAnyObjectByType<EnemyPath>();
            }

            var waypoints = _path != null ? _path.Waypoints : null;
            if (waypoints != null && waypoints.Count > 0)
            {
                _targetIndex = waypoints.Count - 1;
            }
        }

        /// <summary>Pushes the enemy backward along its route by a given distance.</summary>
        public void Knockback(float distance)
        {
            if (_path == null || distance <= 0f || _finished || IgnoresKnockback)
            {
                return;
            }

            var waypoints = _path.Waypoints;
            if (waypoints == null || waypoints.Count < 2 || _targetIndex <= 0)
            {
                return;
            }

            float remainingBack = distance;
            Vector2 currentPos = transform.position;

            while (remainingBack > 0f && _targetIndex > 0)
            {
                Vector2 prevWaypoint = waypoints[_targetIndex - 1];
                float distToPrev = Vector2.Distance(currentPos, prevWaypoint);

                if (distToPrev >= remainingBack)
                {
                    currentPos = Vector2.MoveTowards(currentPos, prevWaypoint, remainingBack);
                    remainingBack = 0f;
                }
                else
                {
                    remainingBack -= distToPrev;
                    currentPos = prevWaypoint;
                    _targetIndex--;
                }
            }

            DistanceTravelled = Mathf.Max(0f, DistanceTravelled - distance);
            transform.position = new Vector3(currentPos.x, currentPos.y, transform.position.z);
        }

        /// <summary>Advances the enemy along the route to match a specified total distance travelled.</summary>
        public void UpdatePositionToMatchDistance(float targetDistance)
        {
            if (_path == null || targetDistance <= 0f)
            {
                return;
            }

            var waypoints = _path.Waypoints;
            if (waypoints == null || waypoints.Count < 2)
            {
                return;
            }

            float accumulated = 0f;
            for (int i = 0; i < waypoints.Count - 1; i++)
            {
                Vector2 a = waypoints[i];
                Vector2 b = waypoints[i + 1];
                float segLen = Vector2.Distance(a, b);

                if (accumulated + segLen >= targetDistance)
                {
                    float t = (targetDistance - accumulated) / segLen;
                    Vector2 pos = Vector2.Lerp(a, b, t);
                    transform.position = new Vector3(pos.x, pos.y, transform.position.z);
                    _targetIndex = i + 1;
                    DistanceTravelled = targetDistance;
                    return;
                }

                accumulated += segLen;
            }

            ReachEnd();
        }

        /// <summary>Raised whenever any enemy reaches the end of its path.</summary>
        public static event System.Action<GameObject> ReachedEnd;

        /// <summary>True if this enemy is removed when it reaches the end, rather than looping or stopping.</summary>
        public bool DespawnsAtEnd => _onReachEnd == EndBehaviour.Despawn;

        private void ReachEnd()
        {
            ReachedEnd?.Invoke(gameObject);

            switch (_onReachEnd)
            {
                case EndBehaviour.Loop:
                    SnapToStart();
                    break;

                case EndBehaviour.Stop:
                    _finished = true;
                    break;

                default:
                    _finished = true;
                    Destroy(gameObject);
                    break;
            }
        }
    }
}
