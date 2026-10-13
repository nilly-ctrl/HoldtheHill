using UnityEngine;

namespace HoldTheHill.Features.Towers
{
    /// <summary>What the cursor would build right now, for a placement label.</summary>
    public struct PlacementPreview
    {
        /// <summary>True while a catalog tower is picked and the cursor is over the field.</summary>
        public bool Active;

        public TowerData Tower;

        /// <summary>The grid cell under the cursor.</summary>
        public Vector3 Cell;

        /// <summary>The cell is off the trail, not blocked and has no tower.</summary>
        public bool Clear;

        public bool CanAfford;

        public int Cost;
    }
}
