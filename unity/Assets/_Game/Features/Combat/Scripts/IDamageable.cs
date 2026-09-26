using UnityEngine;

namespace HoldTheHill.Features.Combat
{
    /// <summary>
    /// Anything a tower, projectile or hazard can hurt. Enemies implement this;
    /// destructible scenery and the Queen can implement it later without the
    /// combat code needing to know the difference.
    /// </summary>
    public interface IDamageable
    {
        /// <summary>Apply one hit. Implementations decide how type and resistances interact.</summary>
        void TakeDamage(DamageInfo info);

        /// <summary>Apply a damage-over-time or slow. Re-applying the same effect refreshes it.</summary>
        void ApplyStatusEffect(StatusEffectData status);

        /// <summary>True once health has run out. Targeting skips these.</summary>
        bool IsDead { get; }

        /// <summary>Where this target is, for aiming and distance checks.</summary>
        Transform Transform { get; }
    }
}
