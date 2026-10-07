using System.Collections.Generic;
using HoldTheHill.Features.Enemies;
using HoldTheHill.Features.Progression;
using UnityEngine;

namespace HoldTheHill.Features.Combat
{
    /// <summary>
    /// Emits kinetic shockwave pulses pushing enemies backward along their path.
    /// </summary>
    [AddComponentMenu("Hold the Hill/Combat/Knockback Tower")]
    public class KnockbackTower : MonoBehaviour
    {
        [Header("Shockwave Parameters")]
        [SerializeField, Min(0.5f)] private float _radius = 3.0f;
        [SerializeField, Min(0.2f)] private float _pulseInterval = 2.0f;
        [SerializeField] private float _damage = 8f;
        [SerializeField, Min(0.2f)] private float _knockbackDistance = 1.4f;
        [SerializeField] private LayerMask _targetMask = ~0;

        private readonly List<IDamageable> _targets = new List<IDamageable>();
        private float _cooldown;

        /// <summary>Raised on each pulse that reaches at least one enemy. For visuals.</summary>
        public event System.Action Pulsed;

        public float Radius => _radius;

        private void Update()
        {
            _cooldown -= Time.deltaTime;
            if (_cooldown <= 0f)
            {
                _cooldown = _pulseInterval;
                PulseShockwave();
            }
        }

        private void PulseShockwave()
        {
            CombatUtil.OverlapDamageables(transform.position, _radius, _targetMask, _targets);
            if (_targets.Count == 0)
            {
                return;
            }

            Pulsed?.Invoke();

            var info = new DamageInfo(_damage, gameObject, transform.position, DamageType.Physical);

            for (int i = 0; i < _targets.Count; i++)
            {
                IDamageable target = _targets[i];
                if (target == null || target.IsDead)
                {
                    continue;
                }

                target.TakeDamage(info);

                if (target.Transform != null)
                {
                    var mover = target.Transform.GetComponent<EnemyMover>();
                    if (mover != null)
                    {
                        float skillMult = 1.0f + UpgradeModifiers.Current.KnockbackBonus;
                        mover.Knockback(_knockbackDistance * skillMult);
                    }
                }
            }
        }

        private void OnTowerUpgraded(int level)
        {
            _radius *= 1.15f;
            _knockbackDistance *= 1.35f;
            _damage *= 1.4f;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, _radius);
        }
    }
}
