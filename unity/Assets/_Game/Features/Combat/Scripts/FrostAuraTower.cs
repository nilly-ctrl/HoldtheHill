using System.Collections.Generic;
using HoldTheHill.Features.Enemies;
using HoldTheHill.Features.Progression;
using UnityEngine;

namespace HoldTheHill.Features.Combat
{
    /// <summary>
    /// Emits periodic expanding frost auras slowing and chilling all enemies in radius.
    /// </summary>
    [AddComponentMenu("Hold the Hill/Combat/Frost Aura Tower")]
    public class FrostAuraTower : MonoBehaviour
    {
        [Header("Frost Parameters")]
        [SerializeField, Min(0.5f)] private float _radius = 3.5f;
        [SerializeField, Min(0.2f)] private float _pulseInterval = 1.2f;
        [SerializeField] private float _damagePerPulse = 4f;
        [SerializeField, Range(0.1f, 0.9f)] private float _slowMultiplier = 0.45f;
        [SerializeField, Min(0.5f)] private float _slowDuration = 2.5f;
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
                PulseFrost();
            }
        }

        private void PulseFrost()
        {
            CombatUtil.OverlapDamageables(transform.position, _radius, _targetMask, _targets);
            if (_targets.Count == 0)
            {
                return;
            }

            Pulsed?.Invoke();

            float skillBonus = UpgradeModifiers.Current.FrostSlowBonus;
            float finalSlow = Mathf.Clamp(_slowMultiplier - skillBonus, 0.15f, 0.9f);

            var status = new StatusEffectData
            {
                EffectName = "FrostChill",
                DamagePerTick = _damagePerPulse * 0.2f,
                TickInterval = 0.5f,
                Duration = _slowDuration,
                SlowMultiplier = finalSlow
            };
            var info = new DamageInfo(_damagePerPulse, gameObject, transform.position, DamageType.Magic);

            for (int i = 0; i < _targets.Count; i++)
            {
                IDamageable target = _targets[i];
                if (target == null || target.IsDead)
                {
                    continue;
                }

                target.TakeDamage(info);
                target.ApplyStatusEffect(status);
            }
        }

        private void OnTowerUpgraded(int level)
        {
            _radius *= 1.15f;
            _damagePerPulse *= 1.4f;
            _slowMultiplier = Mathf.Max(0.2f, _slowMultiplier - 0.1f);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, _radius);
        }
    }
}
