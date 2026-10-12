using HoldTheHill.Features.Combat;

namespace HoldTheHill.Features.Enemies
{
    /// <summary>
    /// A component on an enemy that changes how much of a hit gets through: armour that only
    /// covers one side, a ward, immunity while rolled up.
    /// </summary>
    /// <remarks>
    /// <see cref="EnemyHealth.TakeDamage"/> asks every modifier on the enemy, in component order,
    /// after skill-tree multipliers and before any <see cref="EnemyShield"/>. Returning zero or
    /// less cancels the hit outright: no Damaged event, no shield loss.
    /// </remarks>
    public interface IIncomingDamageModifier
    {
        /// <summary>Returns the damage that should carry on, given the hit as it stands.</summary>
        float ModifyIncomingDamage(EnemyHealth health, DamageInfo info);
    }
}
