using System.Collections.Generic;
using HoldTheHill.Features.Combat;
using HoldTheHill.Features.Enemies;
using HoldTheHill.Features.Progression;
using UnityEngine;

namespace HoldTheHill.Features.Towers
{
    /// <summary>
    /// A placed tower. Knows its grid cell, picks a target within range according to its
    /// <see cref="TargetingPriority"/>, and fires whichever weapon it has been given.
    /// </summary>
    /// <remarks>
    /// A tower can carry a projectile weapon, a chain-lightning arc, a continuous beam and
    /// an orbiting field at once; each is optional. The orbiting field runs itself and needs
    /// no firing logic here.
    /// </remarks>
    [AddComponentMenu("Hold the Hill/Towers/Tower")]
    public class Tower : MonoBehaviour
    {
        [Header("Range")]
        [Tooltip("How far this tower can reach, in world units.")]
        [SerializeField] private float _range = 2.5f;

        [Tooltip("Which layers count as enemies.")]
        [SerializeField] private LayerMask _targetMask = ~0;

        [Header("Targeting")]
        [Tooltip("Which enemy to shoot when several are in range.")]
        [SerializeField] private TargetingPriority _priority = TargetingPriority.First;

        [Tooltip("The path used by First and Last targeting. Found automatically if left empty.")]
        [SerializeField] private EnemyPath _path;

        [Tooltip("Seconds between shots. Ignored by the continuous beam.")]
        [SerializeField, Min(0.05f)] private float _fireInterval = 1f;

        [Header("Weapons (all optional)")]
        [Tooltip("Projectile fired on each shot.")]
        [SerializeField] private Projectile _projectilePrefab;

        [Tooltip("Where shots leave the tower. Defaults to the tower itself.")]
        [SerializeField] private Transform _muzzle;

        [Tooltip("Instant electric arc, fired on the same cooldown as projectiles.")]
        [SerializeField] private ChainLightning _chainLightning;

        [Tooltip("Held beam. Runs continuously while a target is in range.")]
        [SerializeField] private ContinuousBeam _continuousBeam;

        [Tooltip("Always-on ring of orbiting wisps. Needs no firing logic.")]
        [SerializeField] private OrbitingDamageField _orbitingField;

        [Header("Pooling")]
        [Tooltip("Pool that recycles this tower's projectiles. Created automatically if left empty.")]
        [SerializeField] private ProjectilePool _projectilePool;

        private readonly List<IDamageable> _inRange = new List<IDamageable>();
        private float _cooldown;

        /// <summary>How far this tower can reach, in world units.</summary>
        public float Range => _range;

        /// <summary>Which grid cell this tower occupies.</summary>
        public Vector2Int Cell { get; set; }

        /// <summary>Which enemy this tower prefers to shoot.</summary>
        public TargetingPriority Priority
        {
            get => _priority;
            set => _priority = value;
        }

        /// <summary>The enemy currently being shot at, or null.</summary>
        public IDamageable CurrentTarget { get; private set; }

        /// <summary>Total shots/attacks launched by this tower.</summary>
        public int ShotsFired { get; private set; }

        /// <summary>Total damage inflicted on enemies by this tower.</summary>
        public float TotalDamageDealt { get; private set; }

        /// <summary>Total enemy kills delivered by this tower.</summary>
        public int TotalKills { get; private set; }

        /// <summary>Current upgrade level of this tower (1 to 3).</summary>
        // Serialized so a tower copied with Instantiate (the run checkpoint does this) keeps its level.
        [field: SerializeField, HideInInspector]
        public int Level { get; private set; } = 1;

        /// <summary>Maximum upgrade level allowed.</summary>
        public int MaxLevel => 3;

        /// <summary>True if this tower can be upgraded further.</summary>
        public bool CanUpgrade => Level < MaxLevel;

        /// <summary>Total Gold spent to build and upgrade this tower.</summary>
        [field: SerializeField, HideInInspector]
        public int TotalGoldInvested { get; set; } = 100;

        /// <summary>Gold returned when dismantling this tower (75% to 90% return value based on Skill Tree).</summary>
        public int RefundValue
        {
            get
            {
                float pct = UpgradeModifiers.Current.RefundPercentage;
                return Mathf.RoundToInt(TotalGoldInvested * pct);
            }
        }

        /// <summary>Seconds between shots.</summary>
        public float FireInterval => _fireInterval;

        public void RecordDamage(float amount)
        {
            if (amount > 0f)
            {
                TotalDamageDealt += amount;
            }
        }

        public void RecordKill()
        {
            TotalKills++;
        }

        /// <summary>Upgrades the tower to the next level, boosting stats and visual scale.</summary>
        public bool Upgrade()
        {
            if (!CanUpgrade)
            {
                return false;
            }

            Level++;
            _range *= 1.18f;
            _fireInterval = Mathf.Max(0.1f, _fireInterval * 0.85f);

            transform.localScale *= 1.12f;

            SendMessage("OnTowerUpgraded", Level, SendMessageOptions.DontRequireReceiver);
            return true;
        }

        private void Awake()
        {
            if (_muzzle == null)
            {
                _muzzle = transform;
            }

            if (_path == null)
            {
                _path = FindAnyObjectByType<EnemyPath>();
            }

            // A tower with a projectile but no pool would allocate a bullet per shot,
            // so give it one of its own rather than leaving that to scene setup.
            if (_projectilePool == null && _projectilePrefab != null)
            {
                _projectilePool = ProjectilePool.For(_projectilePrefab);
            }
        }

        /// <summary>
        /// Fire-rate multiplier set by things done to this tower (a web halves it). 1 is normal.
        /// </summary>
        public float FireRateScale { get; set; } = 1f;

        /// <summary>Seconds between shots, accounting for Skill Tree bonuses and <see cref="FireRateScale"/>.</summary>
        public float EffectiveFireInterval
        {
            get
            {
                float mult = UpgradeModifiers.Current.FireRateMultiplier * Mathf.Max(0.05f, FireRateScale);
                return Mathf.Max(0.05f, _fireInterval / mult);
            }
        }

        private void Update()
        {
            CurrentTarget = AcquireTarget();

            UpdateBeam(CurrentTarget);

            _cooldown -= Time.deltaTime;
            if (CurrentTarget == null || _cooldown > 0f)
            {
                return;
            }

            _cooldown = EffectiveFireInterval;
            FireAt(CurrentTarget);
        }

        /// <summary>Picks the best enemy in range for this tower's priority, or null.</summary>
        public IDamageable AcquireTarget()
        {
            CombatUtil.OverlapDamageables(transform.position, _range, _targetMask, _inRange);
            if (_inRange.Count == 0)
            {
                return null;
            }

            IDamageable best = null;
            float bestScore = float.NegativeInfinity;

            for (int i = 0; i < _inRange.Count; i++)
            {
                IDamageable candidate = _inRange[i];
                float score = ScoreFor(candidate);

                if (score > bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            return best;
        }

        // Every priority is expressed as "higher is better" so one comparison loop covers them all.
        private float ScoreFor(IDamageable candidate)
        {
            switch (_priority)
            {
                case TargetingPriority.First:
                    return DistanceAlongPath(candidate.Transform.position);

                case TargetingPriority.Last:
                    return -DistanceAlongPath(candidate.Transform.position);

                case TargetingPriority.Closest:
                    return -CombatUtil.SqrDistance(transform, candidate.Transform);

                case TargetingPriority.Strongest:
                    return candidate is IHealthReadable strong
                        ? strong.CurrentHealth
                        : -CombatUtil.SqrDistance(transform, candidate.Transform);

                case TargetingPriority.Weakest:
                    return candidate is IHealthReadable weak
                        ? -weak.CurrentHealth
                        : -CombatUtil.SqrDistance(transform, candidate.Transform);

                default:
                    return -CombatUtil.SqrDistance(transform, candidate.Transform);
            }
        }

        /// <summary>
        /// How far along the enemy path a world position sits, in world units from the start.
        /// Falls back to zero when there is no path, which makes First and Last behave like
        /// Closest rather than throwing.
        /// </summary>
        private float DistanceAlongPath(Vector3 worldPoint)
        {
            if (_path == null)
            {
                return 0f;
            }

            IReadOnlyList<Vector3> waypoints = _path.Waypoints;
            if (waypoints == null || waypoints.Count < 2)
            {
                return 0f;
            }

            var point = (Vector2)worldPoint;
            float travelled = 0f;
            float bestDistanceToRoute = float.PositiveInfinity;
            float bestTravelled = 0f;

            for (int i = 0; i < waypoints.Count - 1; i++)
            {
                Vector2 a = waypoints[i];
                Vector2 b = waypoints[i + 1];
                Vector2 ab = b - a;

                float segmentLength = ab.magnitude;
                float t = ab.sqrMagnitude > 0f ? Mathf.Clamp01(Vector2.Dot(point - a, ab) / ab.sqrMagnitude) : 0f;
                Vector2 closest = a + ab * t;

                float distanceToRoute = Vector2.Distance(point, closest);
                if (distanceToRoute < bestDistanceToRoute)
                {
                    bestDistanceToRoute = distanceToRoute;
                    bestTravelled = travelled + segmentLength * t;
                }

                travelled += segmentLength;
            }

            return bestTravelled;
        }

        private void UpdateBeam(IDamageable target)
        {
            if (_continuousBeam == null)
            {
                return;
            }

            if (target == null)
            {
                _continuousBeam.StopFiring();
            }
            else
            {
                _continuousBeam.Fire(target);
            }
        }

        private void FireAt(IDamageable target)
        {
            ShotsFired++;

            if (_chainLightning != null)
            {
                _chainLightning.Fire(target);
            }

            if (_projectilePrefab == null || _projectilePool == null)
            {
                return;
            }

            Projectile shot = _projectilePool.Get();
            if (shot == null)
            {
                return;
            }

            Vector2 origin = _muzzle.position;
            Vector2 direction = ((Vector2)target.Transform.position - origin).normalized;

            shot.TargetMask = _targetMask;
            shot.Owner = gameObject;
            shot.Launch(origin, target, direction);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, _range);

            // Show what the tower has picked, so targeting priorities can be checked at a glance.
            if (Application.isPlaying && CurrentTarget != null && CurrentTarget.Transform != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, CurrentTarget.Transform.position);
            }
        }
    }
}
