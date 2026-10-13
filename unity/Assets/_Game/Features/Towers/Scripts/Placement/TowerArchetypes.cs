using System.Collections.Generic;
using HoldTheHill.Features.Combat;

namespace HoldTheHill.Features.Towers
{
    /// <summary>The role a tower in play has, for a build bar that groups towers by role.</summary>
    public static class TowerArchetypes
    {
        /// <summary>
        /// A tower's role: its weapon's, else the catalog entry with its name (towers a placer made
        /// are named for their data), else Gunner.
        /// </summary>
        public static TowerArchetype Of(Tower tower, IReadOnlyList<TowerData> catalog)
        {
            TowerWeapon weapon = tower.GetComponent<TowerWeapon>();
            if (weapon != null && weapon.Archetype != TowerArchetype.Unspecified)
            {
                return weapon.Archetype;
            }

            for (int i = 0; i < catalog.Count; i++)
            {
                if (catalog[i].DisplayName == tower.name)
                {
                    return catalog[i].Archetype;
                }
            }

            return TowerArchetype.Gunner;
        }
    }
}
