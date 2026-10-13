using UnityEngine;

namespace HoldTheHill.Features.Towers
{
    /// <summary>
    /// Something in a level that forbids building on a cell: the trail and its clearance, a
    /// cabinet, a spill, the hill. Register with <see cref="BuildBlockers"/> while enabled.
    /// </summary>
    public interface IBuildBlocker
    {
        /// <summary>True if a tower cannot stand on the cell whose centre is at <paramref name="cellCentre"/>.</summary>
        bool Blocks(Vector3 cellCentre);
    }
}
