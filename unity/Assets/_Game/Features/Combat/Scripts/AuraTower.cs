using UnityEngine;

namespace HoldTheHill.Features.Combat
{
    /// <summary>
    /// A timed weapon that acts on everything around the tower to change how enemies move (the Controller archetype).
    /// </summary>
    public abstract class AuraTower : TimedTowerWeapon
    {
        public override TowerArchetype Archetype => TowerArchetype.Controller;
    }
}
