using UnityEngine;

namespace HoldTheHill.Features.Combat
{
    /// <summary>
    /// The role a tower plays, named for what it does for the defence. Weapon base classes say
    /// which one they are (<see cref="TowerWeapon.Archetype"/>); a tower data asset can name it
    /// directly for towers that have no weapon component, such as the projectile towers.
    /// </summary>
    public enum TowerArchetype
    {
        /// <summary>Not set: ask the tower's weapon, or treat it as a Gunner.</summary>
        Unspecified = 0,

        /// <summary>Fires single shots at one target: straight, homing or bouncing.</summary>
        Gunner,

        /// <summary>Area damage at a point, slowly delivered or laid ahead of time: mortars, mines.</summary>
        Artillery,

        /// <summary>Hits at once and holds on or jumps between targets: chain lightning, beams.</summary>
        Arc,

        /// <summary>Acts on everything in range to change how enemies move: slows, knockback.</summary>
        Controller,

        /// <summary>Fights at close range: the worker, soldier and major castes.</summary>
        Brawler,

        /// <summary>Does no damage; heals or repairs: the nurse.</summary>
        Support,

        /// <summary>Makes units that fight for it; its power is their number: the swarm nest.</summary>
        Summoner,
    }
}
