using UnityEngine;

namespace HoldTheHill.Features.Combat
{
    /// <summary>
    /// A timed weapon that does area damage at a point, directly or by laying something down (the Artillery archetype).
    /// </summary>
    public abstract class SplashTower : TimedTowerWeapon
    {
        public override TowerArchetype Archetype => TowerArchetype.Artillery;
    }
}
