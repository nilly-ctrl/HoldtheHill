using UnityEngine;

namespace HoldTheHill.Features.Combat
{
    /// <summary>
    /// A weapon that works through units it makes, so its strength is how many there are (the Summoner archetype).
    /// </summary>
    public abstract class SpawnerTower : TowerWeapon
    {
        public override TowerArchetype Archetype => TowerArchetype.Summoner;
    }
}
