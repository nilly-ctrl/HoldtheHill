using HoldTheHill.Features.Enemies;
using HoldTheHill.Features.Progression;
using UnityEngine;

namespace HoldTheHill.Features.Combat
{
    /// <summary>
    /// Emits kinetic shockwave pulses pushing enemies backward along their path.
    /// </summary>
    [AddComponentMenu("Hold the Hill/Combat/Knockback Tower")]
    public class KnockbackTower : PulseTower
    {
        [Header("Shockwave Parameters")]
        [SerializeField, Min(0.5f)] private float _radius = 3.0f;
        [SerializeField, Min(0.2f)] private float _pulseInterval = 2.0f;
        [SerializeField] private float _damage = 8f;
        [SerializeField, Min(0.2f)] private float _knockbackDistance = 1.4f;
        [SerializeField] private LayerMask _targetMask = ~0;

        private DamageInfo _info;

        public override float Radius => _radius;

        protected override float Interval => _pulseInterval;

        protected override LayerMask TargetMask => _targetMask;

        protected override Color GizmoColor => new Color(1f, 0.6f, 0.2f, 0.5f);

        protected override void BeginPulse()
        {
            _info = new DamageInfo(_damage, gameObject, transform.position, DamageType.Physical);
        }

        protected override void Hit(IDamageable target)
        {
            target.TakeDamage(_info);

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

        public override void OnUpgraded(int level)
        {
            _radius *= 1.15f;
            _knockbackDistance *= 1.35f;
            _damage *= 1.4f;
        }
    }
}
