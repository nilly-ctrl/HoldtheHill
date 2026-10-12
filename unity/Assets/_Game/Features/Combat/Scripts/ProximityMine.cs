using System.Collections.Generic;
using UnityEngine;

namespace HoldTheHill.Features.Combat
{
    /// <summary>
    /// Proximity mine planted on the path that detonates when an enemy steps nearby.
    /// </summary>
    [AddComponentMenu("Hold the Hill/Combat/Proximity Mine")]
    public class ProximityMine : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float _triggerRadius = 0.6f;
        [SerializeField, Min(0.5f)] private float _splashRadius = 1.6f;
        [SerializeField] private float _damage = 35f;
        [SerializeField, Min(0.1f)] private float _armDelay = 0.4f;
        [SerializeField] private LayerMask _targetMask = ~0;

        private readonly List<IDamageable> _targets = new List<IDamageable>();
        private float _armTimer;
        private bool _isArmed;

        public GameObject SourceTower { get; set; }

        public float SplashRadius => _splashRadius;

        /// <summary>Raised just before the mine destroys itself on detonation. For visuals.</summary>
        public event System.Action Detonated;

        private void OnEnable()
        {
            _armTimer = _armDelay;
            _isArmed = false;
        }

        private void Update()
        {
            if (!_isArmed)
            {
                _armTimer -= Time.deltaTime;
                if (_armTimer <= 0f)
                {
                    _isArmed = true;
                }
                return;
            }

            CombatUtil.OverlapDamageables(transform.position, _triggerRadius, _targetMask, _targets);
            if (_targets.Count > 0)
            {
                Detonate();
            }
        }

        private void Detonate()
        {
            CombatUtil.OverlapDamageables(transform.position, _splashRadius, _targetMask, _targets);
            var info = new DamageInfo(_damage, SourceTower != null ? SourceTower : gameObject, transform.position, DamageType.Fire);

            for (int i = 0; i < _targets.Count; i++)
            {
                IDamageable target = _targets[i];
                if (target != null && !target.IsDead)
                {
                    target.TakeDamage(info);
                }
            }

            Detonated?.Invoke();
            Destroy(gameObject);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _triggerRadius);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _splashRadius);
        }
    }
}
