using UnityEngine;

namespace HoldTheHill.Features.Combat
{
    /// <summary>
    /// A tower weapon that acts on its own clock, whether or not the tower has a target:
    /// an aura pulse, a mine being laid.
    /// </summary>
    public abstract class TimedTowerWeapon : TowerWeapon
    {
        private float _cooldown;

        /// <summary>Seconds between triggers at normal speed.</summary>
        protected abstract float Interval { get; }

        /// <summary>What the weapon does each time its clock runs out.</summary>
        protected abstract void Trigger();

        /// <summary>Makes the weapon wait a full interval before it next triggers.</summary>
        protected void ResetCooldown()
        {
            _cooldown = Interval;
        }

        protected virtual void Update()
        {
            _cooldown -= Time.deltaTime * Mathf.Max(0.05f, RateScale);
            if (_cooldown <= 0f)
            {
                _cooldown = Interval;
                Trigger();
            }
        }
    }
}
