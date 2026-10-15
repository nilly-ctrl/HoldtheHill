using UnityEngine;

namespace HoldTheHill.Features.Combat
{
    /// <summary>
    /// A weapon that hits instantly and holds on to its target or jumps between targets (the Arc archetype). Subclasses say how.
    /// </summary>
    public abstract class ArcTower : TowerWeapon
    {
        public override TowerArchetype Archetype => TowerArchetype.Arc;
    }
}
