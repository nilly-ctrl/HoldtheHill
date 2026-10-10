using UnityEngine;

namespace HoldTheHill.Features.Combat
{
    /// <summary>
    /// A timed weapon that does no damage and helps the defence instead (the Support archetype).
    /// </summary>
    public abstract class SupportTower : TimedTowerWeapon
    {
        public override TowerArchetype Archetype => TowerArchetype.Support;
    }
}
