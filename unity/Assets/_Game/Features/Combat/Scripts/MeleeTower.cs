using UnityEngine;

namespace HoldTheHill.Features.Combat
{
    /// <summary>
    /// A weapon that strikes at close range whenever its tower fires (the Brawler archetype).
    /// </summary>
    public abstract class MeleeTower : TowerWeapon
    {
        public override TowerArchetype Archetype => TowerArchetype.Brawler;
    }
}
