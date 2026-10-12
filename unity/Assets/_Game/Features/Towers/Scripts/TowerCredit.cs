using HoldTheHill.Features.Combat;
using HoldTheHill.Features.Enemies;
using UnityEngine;

namespace HoldTheHill.Features.Towers
{
    /// <summary>
    /// Credits a tower with the damage and kills its shots and hazards cause, from the enemy's
    /// <see cref="EnemyHealth.Damaged"/> event. Keeps the enemy layer from knowing what a tower is.
    /// </summary>
    public static class TowerCredit
    {
        // After the enemy layer's own SubsystemRegistration reset has cleared the event.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Hook()
        {
            EnemyHealth.Damaged -= OnDamaged;
            EnemyHealth.Damaged += OnDamaged;
        }

        private static void OnDamaged(EnemyHealth enemy, DamageInfo info, float applied)
        {
            // Credit goes to the owner: a projectile is the source of its damage but is not a
            // child of the tower that fired it, so looking only at the source credited nobody.
            GameObject owner = CombatUtil.OwnerOf(info.Source);
            if (owner == null)
            {
                return;
            }

            var tower = owner.GetComponentInParent<Tower>();
            if (tower == null)
            {
                return;
            }

            tower.RecordDamage(applied);
            if (enemy.CurrentHealth <= 0f)
            {
                tower.RecordKill();
            }
        }
    }
}
