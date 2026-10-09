using System.Collections.Generic;
using UnityEngine;

namespace HoldTheHill.Features.Combat
{
    /// <summary>
    /// A timed weapon that hits every enemy in a circle around the tower. Subclasses say
    /// what a hit does.
    /// </summary>
    public abstract class PulseTower : TimedTowerWeapon
    {
        private readonly List<IDamageable> _targets = new List<IDamageable>();

        /// <summary>Raised on each pulse that reaches at least one enemy. For visuals.</summary>
        public event System.Action Pulsed;

        /// <summary>How far the pulse reaches, in world units.</summary>
        public abstract float Radius { get; }

        /// <summary>Which layers the pulse can hit.</summary>
        protected abstract LayerMask TargetMask { get; }

        protected abstract Color GizmoColor { get; }

        /// <summary>Called once per pulse, before any enemy is hit. For values every hit shares.</summary>
        protected virtual void BeginPulse()
        {
        }

        /// <summary>Applies the pulse to one living enemy in range.</summary>
        protected abstract void Hit(IDamageable target);

        protected sealed override void Trigger()
        {
            CombatUtil.OverlapDamageables(transform.position, Radius, TargetMask, _targets);
            if (_targets.Count == 0)
            {
                return;
            }

            Pulsed?.Invoke();
            BeginPulse();

            for (int i = 0; i < _targets.Count; i++)
            {
                IDamageable target = _targets[i];
                if (target == null || target.IsDead)
                {
                    continue;
                }

                Hit(target);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = GizmoColor;
            Gizmos.DrawWireSphere(transform.position, Radius);
        }
    }
}
