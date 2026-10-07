using System.Collections.Generic;
using HoldTheHill.Features.Combat;
using UnityEngine;

namespace HoldTheHill.Features.Enemies
{
    /// <summary>
    /// Path-healing support ant component that periodically emits a healing aura to restore HP to nearby damaged enemies.
    /// </summary>
    [AddComponentMenu("Hold the Hill/Enemies/Enemy Healer")]
    public class EnemyHealer : MonoBehaviour
    {
        [Header("Healer Parameters")]
        [SerializeField, Min(0.5f)] private float _healRadius = 2.5f;
        [SerializeField, Min(0.2f)] private float _healInterval = 1.5f;
        [SerializeField, Min(1f)] private float _healAmount = 18f;
        [SerializeField] private LayerMask _targetMask = ~0;

        private readonly List<IDamageable> _nearby = new List<IDamageable>();
        private EnemyHealth _selfHealth;
        private float _cooldown;

        /// <summary>Raised on every heal pulse, whether or not anyone needed healing. For visuals.</summary>
        public event System.Action Pulsed;

        public float HealRadius => _healRadius;

        private void Awake()
        {
            _selfHealth = GetComponent<EnemyHealth>();
        }

        private void Update()
        {
            if (_selfHealth != null && _selfHealth.IsDead)
            {
                return;
            }

            _cooldown -= Time.deltaTime;
            if (_cooldown <= 0f)
            {
                _cooldown = _healInterval;
                PulseHeal();
            }
        }

        private void PulseHeal()
        {
            Pulsed?.Invoke();
            CombatUtil.OverlapDamageables(transform.position, _healRadius, _targetMask, _nearby);

            for (int i = 0; i < _nearby.Count; i++)
            {
                IDamageable target = _nearby[i];
                if (target == null || target.IsDead)
                {
                    continue;
                }

                if (target.Transform != null)
                {
                    var health = target.Transform.GetComponent<EnemyHealth>();
                    if (health != null && !health.IsDead && health.HealthFraction < 1f)
                    {
                        health.Heal(_healAmount);
                    }
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.9f, 0.4f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, _healRadius);
        }
    }
}
