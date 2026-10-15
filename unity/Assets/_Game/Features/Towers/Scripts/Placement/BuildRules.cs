using System.Collections.Generic;
using UnityEngine;

namespace HoldTheHill.Features.Towers
{
    /// <summary>
    /// Whether a tower may stand somewhere. Plain rules over the towers and blockers it is handed,
    /// so they can be tested without a scene; the placer passes the live lists.
    /// </summary>
    public static class BuildRules
    {
        /// <summary>
        /// True if a tower could stand on the cell at <paramref name="position"/>: no blocker
        /// forbids it, and no tower other than <paramref name="ignore"/> is within
        /// <paramref name="spacing"/> of the cell's centre.
        /// </summary>
        public static bool CanBuildAt(Vector3 position, IReadOnlyList<Tower> towers, Tower ignore, float spacing, IReadOnlyList<IBuildBlocker> blockers)
        {
            Vector3 cell = BuildGrid.Snap(position);
            for (int i = 0; i < blockers.Count; i++)
            {
                if (blockers[i].Blocks(cell))
                {
                    return false;
                }
            }

            return TowerAt(cell, towers, spacing, ignore) == null;
        }

        /// <summary>The first tower within <paramref name="radius"/> of a point, other than <paramref name="ignore"/>, or null.</summary>
        public static Tower TowerAt(Vector3 point, IReadOnlyList<Tower> towers, float radius, Tower ignore = null)
        {
            for (int i = 0; i < towers.Count; i++)
            {
                Tower tower = towers[i];
                if (tower != null && tower != ignore && Vector2.Distance(point, tower.transform.position) <= radius)
                {
                    return tower;
                }
            }

            return null;
        }
    }
}
