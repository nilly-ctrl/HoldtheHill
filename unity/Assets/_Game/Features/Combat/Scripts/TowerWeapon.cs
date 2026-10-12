using UnityEngine;

namespace HoldTheHill.Features.Combat
{
    /// <summary>
    /// Something a tower attacks with. A tower finds every weapon on its own object and
    /// drives them all, so a new kind of attack is a new subclass rather than a change to
    /// the tower.
    /// </summary>
    /// <remarks>
    /// Override only what the weapon needs: <see cref="Shoot"/> for one attack per tower
    /// cooldown (chain lightning, a melee strike), <see cref="Track"/> for something held on
    /// the target every frame (a beam). A weapon that keeps its own clock and needs no target
    /// extends <see cref="TimedTowerWeapon"/> instead.
    /// </remarks>
    public abstract class TowerWeapon : MonoBehaviour
    {
        /// <summary>
        /// The role this weapon plays. The archetype base classes (ArcTower, AuraTower and so on)
        /// set it; a weapon with no archetype base leaves it Unspecified.
        /// </summary>
        public virtual TowerArchetype Archetype => TowerArchetype.Unspecified;

        /// <summary>
        /// Fire-rate multiplier handed down by the tower (a web halves it). 1 is normal.
        /// </summary>
        public float RateScale { get; set; } = 1f;

        /// <summary>Called each time the tower's cooldown ends with a target in range.</summary>
        public virtual void Shoot(IDamageable target)
        {
        }

        /// <summary>Called every frame with the tower's current target, or null when it has none.</summary>
        public virtual void Track(IDamageable target)
        {
        }

        /// <summary>Called after the tower reaches a new level (2 or 3).</summary>
        public virtual void OnUpgraded(int level)
        {
        }
    }
}
