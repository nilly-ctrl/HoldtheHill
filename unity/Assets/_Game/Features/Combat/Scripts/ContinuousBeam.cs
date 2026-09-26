using UnityEngine;

namespace HoldTheHill.Features.Combat
{
    /// <summary>
    /// A held beam — laser, flamethrower — that damages whatever it is pointed at for
    /// as long as it stays pointed there. Damage ramps up the longer the beam stays on
    /// one target and resets when it switches, which rewards leaving it on a tough enemy
    /// instead of sweeping across a crowd.
    /// </summary>
    [AddComponentMenu("Hold the Hill/Combat/Continuous Beam")]
    [RequireComponent(typeof(LineRenderer))]
    public class ContinuousBeam : MonoBehaviour
    {
        [Header("Damage")]
        [Tooltip("Damage per second when the beam first touches a target.")]
        [SerializeField, Min(0f)] private float _baseDamagePerSecond = 6f;

        [Tooltip("Damage per second added for every second held on the same target.")]
        [SerializeField, Min(0f)] private float _rampPerSecond = 4f;

        [Tooltip("Ceiling for the ramped damage per second.")]
        [SerializeField, Min(0f)] private float _maxDamagePerSecond = 30f;

        [Tooltip("Seconds between damage applications. Smaller is smoother but costs more.")]
        [SerializeField, Min(0.02f)] private float _tickInterval = 0.1f;

        [SerializeField] private DamageType _damageType = DamageType.Fire;

        [Tooltip("Status effect applied while the beam is on a target. Duration 0 means none.")]
        [SerializeField] private StatusEffectData _status;

        [Header("Reach")]
        [Tooltip("Maximum beam length in world units. The beam cuts out past this.")]
        [SerializeField, Min(0.1f)] private float _range = 6f;

        [Header("Look")]
        [Tooltip("Beam width at full ramp. It starts thinner and grows as damage ramps.")]
        [SerializeField, Min(0.01f)] private float _maxWidth = 0.25f;

        private LineRenderer _line;
        private IDamageable _target;
        private float _timeOnTarget;
        private float _tickTimer;

        /// <summary>True while the beam is actively burning something.</summary>
        public bool IsFiring => _target != null;

        /// <summary>Current damage per second, including the ramp.</summary>
        public float CurrentDamagePerSecond =>
            Mathf.Min(_baseDamagePerSecond + _rampPerSecond * _timeOnTarget, _maxDamagePerSecond);

        private void Awake()
        {
            _line = GetComponent<LineRenderer>();
            _line.useWorldSpace = true;
            _line.positionCount = 2;
            _line.enabled = false;
        }

        private void OnDisable()
        {
            StopFiring();
        }

        /// <summary>
        /// Points the beam at a target. Calling this with the same target each frame keeps
        /// the ramp going; a different target restarts it.
        /// </summary>
        public void Fire(IDamageable target)
        {
            if (target == null || target.IsDead || target.Transform == null)
            {
                StopFiring();
                return;
            }

            if (!ReferenceEquals(target, _target))
            {
                _target = target;
                _timeOnTarget = 0f;
                _tickTimer = 0f;
            }
        }

        /// <summary>Switches the beam off and clears the ramp.</summary>
        public void StopFiring()
        {
            _target = null;
            _timeOnTarget = 0f;
            _tickTimer = 0f;

            if (_line != null)
            {
                _line.enabled = false;
            }
        }

        private void Update()
        {
            if (_target == null)
            {
                return;
            }

            if (_target.IsDead || _target.Transform == null)
            {
                StopFiring();
                return;
            }

            Vector2 origin = transform.position;
            Vector2 targetPoint = _target.Transform.position;

            if (Vector2.Distance(origin, targetPoint) > _range)
            {
                StopFiring();
                return;
            }

            _timeOnTarget += Time.deltaTime;
            DrawBeam(origin, targetPoint);

            _tickTimer -= Time.deltaTime;
            if (_tickTimer > 0f)
            {
                return;
            }

            // Damage is scaled by the interval so the DPS figure holds regardless of tick rate.
            _tickTimer = _tickInterval;
            float amount = CurrentDamagePerSecond * _tickInterval;

            _target.TakeDamage(new DamageInfo(amount, gameObject, targetPoint, _damageType));
            if (_status.IsValid)
            {
                _target.ApplyStatusEffect(_status);
            }
        }

        private void DrawBeam(Vector2 origin, Vector2 end)
        {
            _line.enabled = true;
            _line.SetPosition(0, origin);
            _line.SetPosition(1, end);

            // Widen the beam as it ramps, so the player can see it getting stronger.
            float ramp = _maxDamagePerSecond > _baseDamagePerSecond
                ? Mathf.InverseLerp(_baseDamagePerSecond, _maxDamagePerSecond, CurrentDamagePerSecond)
                : 1f;

            float width = Mathf.Lerp(_maxWidth * 0.4f, _maxWidth, ramp);
            _line.startWidth = width;
            _line.endWidth = width;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, _range);
        }
    }
}
