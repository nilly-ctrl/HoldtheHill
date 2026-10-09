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
    /// A tower fires its own projectile, if it has one, and drives every
    /// <see cref="TowerWeapon"/> on the same object (chain lightning, a beam, an aura, a melee
    /// strike). Each is optional and they can be combined.
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

        [Header("Projectile (optional)")]
        [Tooltip("Projectile fired on each shot.")]
        [SerializeField] private Projectile _projectilePrefab;

        [Tooltip("Where shots leave the tower. Defaults to the tower itself.")]
        [SerializeField] private Transform _muzzle;

        [Header("Pooling")]
        [Tooltip("Pool that recycles this tower's projectiles. Created automatically if left empty.")]
        [SerializeField] private ProjectilePool _projectilePool;

        private static readonly List<Tower> s_all = new List<Tower>();

        private readonly List<IDamageable> _inRange = new List<IDamageable>();
        private readonly List<TowerWeapon> _weapons = new List<TowerWeapon>();
        private float _cooldown;
        private float _fireRateScale = 1f;

        /// <summary>Raised after an upgrade, with the new level. For visuals and sound.</summary>
        public event System.Action<int> Upgraded;

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

            RefreshWeapons();
            for (int i = 0; i < _weapons.Count; i++)
            {
                _weapons[i].OnUpgraded(Level);
            }

            Upgraded?.Invoke(Level);
            return true;
        }

        /// <summary>
        /// Fills <paramref name="results"/> with every tower on an active object, including
        /// ones whose Tower component is switched off (a stunned tower still stands). Use this
        /// instead of FindObjectsByType, which searches the whole scene.
        /// </summary>
        public static void GetActive(List<Tower> results)
        {
            results.Clear();
            for (int i = 0; i < s_all.Count; i++)
            {
                if (s_all[i].gameObject.activeInHierarchy)
                {
                    results.Add(s_all[i]);
                }
            }
        }

        private void OnDestroy()
        {
            s_all.Remove(this);
        }

        private void Awake()
        {
            s_all.Add(this);

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

            RefreshWeapons();
        }

        // Again in Start, for weapons added in code after this component.
        private void Start()
        {
            RefreshWeapons();
        }

        /// <summary>
        /// Finds the weapons on this object again. Call after adding or removing one at runtime.
        /// </summary>
        public void RefreshWeapons()
        {
            GetComponents(_weapons);
            for (int i = 0; i < _weapons.Count; i++)
            {
                _weapons[i].RateScale = _fireRateScale;
            }
        }

        /// <summary>
        /// Fire-rate multiplier set by things done to this tower (a web halves it). 1 is normal.
        /// Passed on to every weapon, so the ones that keep their own clock slow down too.
        /// </summary>
        public float FireRateScale
        {
            get => _fireRateScale;
            set
            {
                _fireRateScale = value;
                RefreshWeapons();
            }
        }

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

            for (int i = 0; i < _weapons.Count; i++)
            {
                _weapons[i].Track(CurrentTarget);
            }

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

        // Zero with no path, which makes First and Last behave like Closest rather than throwing.
        private float DistanceAlongPath(Vector3 worldPoint)
        {
            return _path != null ? _path.DistanceAlong(worldPoint) : 0f;
        }

        private void FireAt(IDamageable target)
        {
            ShotsFired++;

            for (int i = 0; i < _weapons.Count; i++)
            {
                _weapons[i].Shoot(target);
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
