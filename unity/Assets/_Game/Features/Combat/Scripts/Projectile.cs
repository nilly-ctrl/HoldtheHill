using System.Collections.Generic;
using UnityEngine;

namespace HoldTheHill.Features.Combat
{
    /// <summary>How a projectile travels once it leaves the tower.</summary>
    public enum FlightMode
    {
        /// <summary>Straight line in the direction it was fired.</summary>
        Linear,

        /// <summary>Steers toward its target. Overshoots and loops back if the target dodges.</summary>
        Homing,

        /// <summary>Parabolic arc to a landing point, ignoring whatever is in between.</summary>
        Lobbed,

        /// <summary>Resolves the moment it is fired, with no travel time.</summary>
        Instant,

        /// <summary>Hovers in place, picks a target, then dives into it at speed.</summary>
        SeekAndDestroy
    }

    /// <summary>
    /// One shot in flight. Handles travel, hit detection, damage, splash, status
    /// effects and ground hazards, then returns itself to its pool.
    /// </summary>
    /// <remarks>
    /// Hit detection is an explicit overlap check each frame rather than physics
    /// triggers. Projectiles are pooled and move fast, and trigger callbacks on
    /// recycled objects are easy to get wrong; polling keeps the lifecycle obvious.
    /// </remarks>
    [AddComponentMenu("Hold the Hill/Combat/Projectile")]
    public class Projectile : MonoBehaviour
    {
        [Header("Flight")]
        [SerializeField] protected FlightMode _flightMode = FlightMode.Linear;

        [Tooltip("Travel speed in world units per second.")]
        [SerializeField, Min(0.01f)] protected float _speed = 10f;

        [Tooltip("Homing only: how fast it can turn, in degrees per second. Low values make it loop around.")]
        [SerializeField, Min(0f)] protected float _turnRate = 180f;

        [Tooltip("Seconds before the shot gives up and despawns.")]
        [SerializeField, Min(0.1f)] protected float _lifetime = 5f;

        [Header("Lobbed")]
        [Tooltip("Peak height of the arc above the straight line to the landing point.")]
        [SerializeField, Min(0f)] protected float _arcHeight = 2f;

        [Header("Seek and Destroy")]
        [Tooltip("Seconds spent hovering before picking a target and diving.")]
        [SerializeField, Min(0f)] protected float _loiterDuration = 1f;

        [Tooltip("Speed multiplier applied during the dive.")]
        [SerializeField, Min(1f)] protected float _diveSpeedMultiplier = 2.5f;

        [Tooltip("How far the shot drifts while loitering.")]
        [SerializeField, Min(0f)] protected float _loiterBobAmplitude = 0.25f;

        [Header("Damage")]
        [SerializeField, Min(0f)] protected float _damage = 10f;
        [SerializeField] protected DamageType _damageType = DamageType.Physical;

        [Tooltip("Radius of the damage dealt around the impact point. Zero for single-target.")]
        [SerializeField, Min(0f)] protected float _splashRadius = 0f;

        [Tooltip("Damage dealt to everything caught in the splash.")]
        [SerializeField, Min(0f)] protected float _splashDamage = 5f;

        [Tooltip("Status effect applied on hit. Duration 0 means none.")]
        [SerializeField] protected StatusEffectData _status;

        [Header("Targeting")]
        [Tooltip("Which layers count as valid targets.")]
        [SerializeField] protected LayerMask _targetMask = ~0;

        [Tooltip("How close the shot must get to count as a hit.")]
        [SerializeField, Min(0.01f)] protected float _hitRadius = 0.25f;

        [Header("Piercing")]
        [Tooltip("If on, the shot carries on through targets instead of stopping at the first.")]
        [SerializeField] protected bool _isPiercing = false;

        [Tooltip("How many targets it can pass through before despawning.")]
        [SerializeField, Min(1)] protected int _maxPierceHits = 3;

        [Tooltip("Seconds before the same target can be hit again by this shot.")]
        [SerializeField, Min(0.01f)] protected float _pierceHitCooldown = 0.25f;

        [Header("On Impact")]
        [Tooltip("Optional lingering hazard left where the shot lands.")]
        [SerializeField] protected GroundHazard _groundHazardPrefab;

        [Tooltip("Radius of the hazard left behind. Zero keeps the prefab's own radius.")]
        [SerializeField, Min(0f)] protected float _groundHazardRadius = 0f;

        [Tooltip("Seconds the hazard lasts.")]
        [SerializeField, Min(0.1f)] protected float _groundHazardDuration = 4f;

        [Tooltip("Damage the hazard deals per tick.")]
        [SerializeField, Min(0f)] protected float _groundHazardDamagePerTick = 2f;

        // Targets already hit by this shot, and when, so piercing does not re-hit instantly.
        private readonly Dictionary<IDamageable, float> _recentHits = new Dictionary<IDamageable, float>();
        private readonly List<IDamageable> _overlapBuffer = new List<IDamageable>();

        private ProjectilePool _pool;
        private IDamageable _target;
        private Vector2 _direction = Vector2.right;
        private float _age;
        private int _pierceCount;
        private bool _isReleased;

        // Lobbed state.
        private Vector2 _launchPoint;
        private Vector2 _landingPoint;
        private float _arcProgress;
        private float _arcDuration = 1f;

        // Seek-and-destroy state.
        private Vector2 _loiterOrigin;
        private bool _isDiving;

        /// <summary>The damage this shot deals on a direct hit.</summary>
        public float Damage
        {
            get => _damage;
            set => _damage = Mathf.Max(0f, value);
        }

        /// <summary>Which layers this shot can hit.</summary>
        public LayerMask TargetMask
        {
            get => _targetMask;
            set => _targetMask = value;
        }

        /// <summary>How this shot travels.</summary>
        public FlightMode Mode => _flightMode;

        /// <summary>Tells the shot which pool to go back to. Set by the pool itself.</summary>
        public void SetPool(ProjectilePool pool)
        {
            _pool = pool;
        }

        /// <summary>
        /// Fires the shot. <paramref name="target"/> may be null for unguided modes,
        /// in which case <paramref name="direction"/> is used.
        /// </summary>
        public virtual void Launch(Vector2 origin, IDamageable target, Vector2 direction)
        {
            transform.position = origin;

            _target = target;
            _age = 0f;
            _pierceCount = 0;
            _isReleased = false;
            _isDiving = false;
            _arcProgress = 0f;
            _recentHits.Clear();

            Vector2 toTarget = IsTargetAlive(target)
                ? (Vector2)target.Transform.position - origin
                : direction;

            _direction = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : Vector2.right;
            FaceDirection(_direction);

            switch (_flightMode)
            {
                case FlightMode.Lobbed:
                    _launchPoint = origin;
                    _landingPoint = IsTargetAlive(target) ? (Vector2)target.Transform.position : origin + direction;
                    _arcDuration = Mathf.Max(0.1f, Vector2.Distance(_launchPoint, _landingPoint) / _speed);
                    break;

                case FlightMode.SeekAndDestroy:
                    _loiterOrigin = origin;
                    break;

                case FlightMode.Instant:
                    ResolveInstant(origin, target, direction);
                    break;
            }
        }

        protected virtual void Update()
        {
            if (_isReleased)
            {
                return;
            }

            float dt = Time.deltaTime;
            _age += dt;
            if (_age >= _lifetime)
            {
                Expire();
                return;
            }

            switch (_flightMode)
            {
                case FlightMode.Linear:
                    MoveLinear(dt);
                    break;
                case FlightMode.Homing:
                    MoveHoming(dt);
                    break;
                case FlightMode.Lobbed:
                    MoveLobbed(dt);
                    return; // Lobbed only detonates on landing, not on contact.
                case FlightMode.SeekAndDestroy:
                    MoveSeekAndDestroy(dt);
                    break;
                case FlightMode.Instant:
                    return; // Already resolved in Launch.
            }

            CheckForHits();
        }

        private void MoveLinear(float dt)
        {
            transform.position += (Vector3)(_direction * (_speed * dt));
        }

        // Turns toward the target at a limited rate while moving at constant speed. A slow
        // turn rate means the shot sails past a dodging target and curves back for another pass.
        private void MoveHoming(float dt)
        {
            if (!IsTargetAlive(_target))
            {
                _target = FindNearestTarget(transform.position, float.PositiveInfinity);
            }

            if (IsTargetAlive(_target))
            {
                Vector2 desired = ((Vector2)_target.Transform.position - (Vector2)transform.position).normalized;
                float maxRadians = _turnRate * Mathf.Deg2Rad * dt;
                _direction = ((Vector2)Vector3.RotateTowards(_direction, desired, maxRadians, 0f)).normalized;
                FaceDirection(_direction);
            }

            transform.position += (Vector3)(_direction * (_speed * dt));
        }

        // Flies a parabola to a fixed landing point. Nothing in between is hit; the shot
        // detonates where it lands, which is what makes mortars feel different from arrows.
        private void MoveLobbed(float dt)
        {
            _arcProgress += dt / _arcDuration;

            if (_arcProgress >= 1f)
            {
                transform.position = _landingPoint;
                Detonate(_landingPoint, null);
                return;
            }

            Vector2 flat = Vector2.Lerp(_launchPoint, _landingPoint, _arcProgress);
            float height = _arcHeight * 4f * _arcProgress * (1f - _arcProgress);
            var next = new Vector2(flat.x, flat.y + height);

            FaceDirection(next - (Vector2)(Vector3)transform.position);
            transform.position = next;
        }

        // Hovers for a moment, then commits to a target and accelerates into it.
        private void MoveSeekAndDestroy(float dt)
        {
            if (!_isDiving)
            {
                if (_age < _loiterDuration)
                {
                    float bob = Mathf.Sin(_age * Mathf.PI * 2f) * _loiterBobAmplitude;
                    transform.position = _loiterOrigin + new Vector2(0f, bob);
                    return;
                }

                _target = IsTargetAlive(_target) ? _target : FindNearestTarget(transform.position, float.PositiveInfinity);
                _isDiving = true;
            }

            if (IsTargetAlive(_target))
            {
                _direction = ((Vector2)_target.Transform.position - (Vector2)transform.position).normalized;
                FaceDirection(_direction);
            }

            transform.position += (Vector3)(_direction * (_speed * _diveSpeedMultiplier * dt));
        }

        private void ResolveInstant(Vector2 origin, IDamageable target, Vector2 direction)
        {
            Vector2 point = IsTargetAlive(target) ? (Vector2)target.Transform.position : origin + direction;

            if (IsTargetAlive(target))
            {
                ApplyHit(target, point);
            }

            Detonate(point, target);
        }

        private void CheckForHits()
        {
            CombatUtil.OverlapDamageables(transform.position, _hitRadius, _targetMask, _overlapBuffer);
            if (_overlapBuffer.Count == 0)
            {
                return;
            }

            for (int i = 0; i < _overlapBuffer.Count; i++)
            {
                IDamageable hit = _overlapBuffer[i];
                if (!CanHit(hit))
                {
                    continue;
                }

                ApplyHit(hit, transform.position);

                if (!_isPiercing)
                {
                    Detonate(transform.position, hit);
                    return;
                }

                _pierceCount++;
                if (_pierceCount >= _maxPierceHits)
                {
                    Detonate(transform.position, hit);
                    return;
                }
            }
        }

        private bool CanHit(IDamageable target)
        {
            if (!IsTargetAlive(target))
            {
                return false;
            }

            if (!_isPiercing)
            {
                return !_recentHits.ContainsKey(target);
            }

            // Piercing shots may re-hit, but only after a cooldown, so a target standing
            // inside the shot's path does not take a hit every single frame.
            return !_recentHits.TryGetValue(target, out float lastHit)
                   || Time.time - lastHit >= _pierceHitCooldown;
        }

        /// <summary>Deals this shot's damage and status effect to one target.</summary>
        protected virtual void ApplyHit(IDamageable target, Vector2 point)
        {
            _recentHits[target] = Time.time;

            target.TakeDamage(new DamageInfo(_damage, gameObject, point, _damageType));
            if (_status.IsValid)
            {
                target.ApplyStatusEffect(_status);
            }
        }

        /// <summary>
        /// Everything that happens where the shot ends: splash, ground hazard, then recycle.
        /// Subclasses override to scatter children, bounce onward, and so on.
        /// </summary>
        protected virtual void Detonate(Vector2 point, IDamageable directHit)
        {
            CombatUtil.ApplySplash(
                point, _splashRadius, _targetMask, _splashDamage, _damageType, gameObject, _status, directHit);

            SpawnGroundHazard(point);
            Release();
        }

        /// <summary>Called when the shot runs out of time without hitting anything.</summary>
        protected virtual void Expire()
        {
            Release();
        }

        protected void SpawnGroundHazard(Vector2 point)
        {
            if (_groundHazardPrefab == null)
            {
                return;
            }

            GroundHazard hazard = Instantiate(_groundHazardPrefab, point, Quaternion.identity);
            hazard.Configure(
                _groundHazardRadius > 0f ? _groundHazardRadius : hazard.Radius,
                _groundHazardDamagePerTick,
                _groundHazardDuration,
                _damageType,
                _status,
                _targetMask);
        }

        /// <summary>Returns the shot to its pool, or destroys it if it has none.</summary>
        protected void Release()
        {
            if (_isReleased)
            {
                return;
            }

            _isReleased = true;
            _target = null;
            _recentHits.Clear();

            if (_pool != null)
            {
                _pool.Release(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>Points the sprite along the direction of travel.</summary>
        protected void FaceDirection(Vector2 direction)
        {
            if (direction.sqrMagnitude < 0.0001f)
            {
                return;
            }

            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        }

        /// <summary>Nearest live target within a radius, or null.</summary>
        protected IDamageable FindNearestTarget(Vector2 from, float radius)
        {
            float searchRadius = float.IsPositiveInfinity(radius) ? 50f : radius;
            CombatUtil.OverlapDamageables(from, searchRadius, _targetMask, _overlapBuffer);

            IDamageable best = null;
            float bestSqr = float.PositiveInfinity;

            for (int i = 0; i < _overlapBuffer.Count; i++)
            {
                IDamageable candidate = _overlapBuffer[i];
                float sqr = ((Vector2)candidate.Transform.position - from).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = candidate;
                }
            }

            return best;
        }

        /// <summary>True if the target still exists and has health left.</summary>
        protected static bool IsTargetAlive(IDamageable target)
        {
            return CombatUtil.IsAlive(target);
        }

        /// <summary>Current direction of travel.</summary>
        protected Vector2 Direction => _direction;

        /// <summary>The target this shot is chasing, if any.</summary>
        protected IDamageable CurrentTarget => _target;

        /// <summary>Progress along a lobbed arc, 0 at launch and 1 on landing.</summary>
        protected float ArcProgress => _arcProgress;

        /// <summary>Seconds since this shot was launched.</summary>
        protected float Age => _age;

        /// <summary>True once the shot has been returned to its pool. Nothing should act on it after this.</summary>
        protected bool IsReleased => _isReleased;
    }
}
