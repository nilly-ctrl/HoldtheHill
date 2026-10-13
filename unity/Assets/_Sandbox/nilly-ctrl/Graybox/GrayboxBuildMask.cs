using System.Collections.Generic;
using HoldTheHill.Features.Towers;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// The cells of a hand-drawn level where nothing can be built: cabinets, the fridge, table
    /// legs, spills, crumbs, the hill, and everything outside the map. Written into the scene by
    /// <c>GrayboxLevelBuilder</c> from the level's text map; registered with
    /// <see cref="BuildBlockers"/> while enabled, so the placer asks it before placing. A scene
    /// without one (the graybox) blocks nothing.
    /// </summary>
    public class GrayboxBuildMask : MonoBehaviour, IBuildBlocker
    {
        [Tooltip("Blocked cells. A cell's centre sits on whole world units, where towers snap.")]
        [SerializeField] private Vector2Int[] _blocked = new Vector2Int[0];

        [Tooltip("The map's cells. Anything outside is blocked too.")]
        [SerializeField] private RectInt _bounds;

        private HashSet<Vector2Int> _lookup;

        public void Configure(IEnumerable<Vector2Int> blocked, RectInt bounds)
        {
            _blocked = new List<Vector2Int>(blocked).ToArray();
            _bounds = bounds;
            _lookup = null;
        }

        private void OnEnable()
        {
            BuildBlockers.Register(this);
        }

        private void OnDisable()
        {
            BuildBlockers.Unregister(this);
        }

        /// <summary>True if a tower cannot stand on the cell at <paramref name="cellCentre"/> in this level.</summary>
        public bool Blocks(Vector3 cellCentre)
        {
            _lookup ??= new HashSet<Vector2Int>(_blocked);
            Vector2Int cell = BuildGrid.CellOf(cellCentre);
            return !_bounds.Contains(cell) || _lookup.Contains(cell);
        }
    }
}
