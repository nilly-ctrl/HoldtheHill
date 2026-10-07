using System.Collections.Generic;
using UnityEngine;

namespace HoldTheHill.Features.Combat
{
    /// <summary>
    /// A lingering patch of ground that hurts whatever stands in it: fire puddle,
    /// acid pool, tar. Spawned by a projectile on impact, ticks for a while, then
    /// despawns.
    /// </summary>
    [AddComponentMenu("Hold the Hill/Combat/Ground Hazard")]
    [RequireComponent(typeof(CircleCollider2D))]
    public class GroundHazard : MonoBehaviour
    {
        [Header("Area")]
        [Tooltip("Radius of the damaging patch, in world units.")]
        [SerializeField, Min(0.01f)] private float _radius = 1f;

        [Tooltip("Which layers can be hurt by standing in this.")]
        [SerializeField] private LayerMask _targetMask = ~0;

        [Header("Damage")]
        [Tooltip("Damage dealt to everything inside, once per tick.")]
        [SerializeField, Min(0f)] private float _damagePerTick = 2f;

        [Tooltip("Seconds between ticks.")]
        [SerializeField, Min(0.02f)] private float _tickInterval = 0.5f;

        [SerializeField] private DamageType _damageType = DamageType.Fire;

        [Tooltip("Status effect applied to anything caught in the patch. Duration 0 means none.")]
        [SerializeField] private StatusEffectData _status;

        [Header("Lifetime")]
        [Tooltip("Seconds before the patch fades and despawns.")]
        [SerializeField, Min(0.1f)] private float _duration = 4f;

        private readonly List<IDamageable> _inside = new List<IDamageable>();
        private CircleCollider2D _collider;
        private float _tickTimer;
        private float _remaining;

        /// <summary>Who gets credit for this hazard's damage: whoever fired the shot that left it.</summary>
        public GameObject Owner { get; set; }

        /// <summary>Radius of the damaging patch, in world units.</summary>
        public float Radius => _radius;

        private void Awake()
        {
            _collider = GetComponent<CircleCollider2D>();
            _collider.isTrigger = true;
            _collider.radius = _radius;
        }

        private void OnEnable()
        {
            _remaining = _duration;
            _tickTimer = 0f;
            _inside.Clear();
        }

        /// <summary>
        /// Overrides the inspector values, for hazards configured by the projectile
        /// that spawned them.
        /// </summary>
        public void Configure(
            float radius,
            float damagePerTick,
            float duration,
            DamageType type,
            StatusEffectData status,
            LayerMask mask)
        {
            _radius = Mathf.Max(0.01f, radius);
            _damagePerTick = Mathf.Max(0f, damagePerTick);
            _duration = Mathf.Max(0.1f, duration);
            _damageType = type;
            _status = status;
            _targetMask = mask;

            if (_collider != null)
            {
                _collider.radius = _radius;
            }

            _remaining = _duration;
            _tickTimer = 0f;
        }

        private void Update()
        {
            _remaining -= Time.deltaTime;
            if (_remaining <= 0f)
            {
                Despawn();
                return;
            }

            _tickTimer -= Time.deltaTime;
            if (_tickTimer > 0f)
            {
                return;
            }

            _tickTimer = _tickInterval;
            Tick();
        }

        // Re-queried every tick rather than tracked through trigger enter/exit: enemies
        // that die or get recycled mid-tick would otherwise leave stale entries behind.
        private void Tick()
        {
            CombatUtil.OverlapDamageables(transform.position, _radius, _targetMask, _inside);

            for (int i = 0; i < _inside.Count; i++)
            {
                IDamageable target = _inside[i];
                if (_damagePerTick > 0f)
                {
                    target.TakeDamage(new DamageInfo(_damagePerTick, gameObject, target.Transform.position, _damageType));
                }

                if (_status.IsValid)
                {
                    target.ApplyStatusEffect(_status);
                }
            }
        }

        private void Despawn()
        {
            // Hazards are spawned per impact, so plain destruction is fine for now.
            // Swap in a pool release here if these ever become hot.
            Destroy(gameObject);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.5f, 0.1f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, _radius);
        }
    }
}
