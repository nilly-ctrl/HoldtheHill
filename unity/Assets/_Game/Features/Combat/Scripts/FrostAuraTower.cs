using HoldTheHill.Features.Enemies;
using HoldTheHill.Features.Progression;
using UnityEngine;

namespace HoldTheHill.Features.Combat
{
    /// <summary>
    /// Emits periodic expanding frost auras slowing and chilling all enemies in radius.
    /// </summary>
    [AddComponentMenu("Hold the Hill/Combat/Frost Aura Tower")]
    public class FrostAuraTower : PulseTower
    {
        [Header("Frost Parameters")]
        [SerializeField, Min(0.5f)] private float _radius = 3.5f;
        [SerializeField, Min(0.2f)] private float _pulseInterval = 1.2f;
        [SerializeField] private float _damagePerPulse = 4f;
        [SerializeField, Range(0.1f, 0.9f)] private float _slowMultiplier = 0.45f;
        [SerializeField, Min(0.5f)] private float _slowDuration = 2.5f;
        [SerializeField] private LayerMask _targetMask = ~0;

        private StatusEffectData _status;
        private DamageInfo _info;

        public override float Radius => _radius;

        protected override float Interval => _pulseInterval;

        protected override LayerMask TargetMask => _targetMask;

        protected override Color GizmoColor => new Color(0.4f, 0.8f, 1f, 0.5f);

        protected override void BeginPulse()
        {
            float skillBonus = UpgradeModifiers.Current.FrostSlowBonus;
            float finalSlow = Mathf.Clamp(_slowMultiplier - skillBonus, 0.15f, 0.9f);

            _status = new StatusEffectData
            {
                EffectName = "FrostChill",
                DamagePerTick = _damagePerPulse * 0.2f,
                TickInterval = 0.5f,
                Duration = _slowDuration,
                SlowMultiplier = finalSlow
            };
            _info = new DamageInfo(_damagePerPulse, gameObject, transform.position, DamageType.Magic);
        }

        protected override void Hit(IDamageable target)
        {
            target.TakeDamage(_info);
            target.ApplyStatusEffect(_status);
        }

        public override void OnUpgraded(int level)
        {
            _radius *= 1.15f;
            _damagePerPulse *= 1.4f;
            _slowMultiplier = Mathf.Max(0.2f, _slowMultiplier - 0.1f);
        }
    }
}
