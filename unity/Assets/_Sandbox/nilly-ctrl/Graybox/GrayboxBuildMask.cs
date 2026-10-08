using System.Collections.Generic;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// The cells of a hand-drawn level where nothing can be built: cabinets, the fridge, table
    /// legs, spills, crumbs, the hill, and everything outside the map. Written into the scene by
    /// <c>GrayboxLevelBuilder</c> from the level's text map; <see cref="GrayboxTowerPlacer"/>
    /// asks it before placing. A scene without one (the graybox) blocks nothing.
    /// </summary>
    public class GrayboxBuildMask : MonoBehaviour
    {
        [Tooltip("Blocked cells. A cell's centre sits on whole world units, where towers snap.")]
        [SerializeField] private Vector2Int[] _blocked = new Vector2Int[0];

        [Tooltip("The map's cells. Anything outside is blocked too.")]
        [SerializeField] private RectInt _bounds;

        private static GrayboxBuildMask s_instance;
        private HashSet<Vector2Int> _lookup;

        public void Configure(IEnumerable<Vector2Int> blocked, RectInt bounds)
        {
            _blocked = new List<Vector2Int>(blocked).ToArray();
            _bounds = bounds;
            _lookup = null;
        }

        /// <summary>True if a tower cannot stand at <paramref name="point"/> in the current level.</summary>
        public static bool Blocks(Vector3 point)
        {
            if (s_instance == null)
            {
                return false;
            }

            s_instance._lookup ??= new HashSet<Vector2Int>(s_instance._blocked);
            var cell = new Vector2Int(Mathf.RoundToInt(point.x), Mathf.RoundToInt(point.y));
            return !s_instance._bounds.Contains(cell) || s_instance._lookup.Contains(cell);
        }

        private void Awake()
        {
            s_instance = this;
        }

        private void OnDestroy()
        {
            if (s_instance == this)
            {
                s_instance = null;
            }
        }
    }
}
