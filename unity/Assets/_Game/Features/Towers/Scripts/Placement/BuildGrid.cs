using UnityEngine;

namespace HoldTheHill.Features.Towers
{
    /// <summary>
    /// The grid towers stand on: one cell per world unit, centred on whole units, so a tower at
    /// (3, 2) sits at world (3, 2). Every hand-drawn level and the build mask use the same
    /// convention.
    /// </summary>
    /// <remarks>
    /// Whether cells should instead be centred on half units (what a Tilemap gives) is open
    /// question 2 in <c>docs/CODEBASE_CONVERGENCE.md</c>; it is one function here, on purpose.
    /// </remarks>
    public static class BuildGrid
    {
        /// <summary>The centre of the cell a world position falls in. Z is dropped.</summary>
        public static Vector3 Snap(Vector3 position)
        {
            return new Vector3(Mathf.Round(position.x), Mathf.Round(position.y), 0f);
        }

        /// <summary>The cell a world position falls in.</summary>
        public static Vector2Int CellOf(Vector3 position)
        {
            return new Vector2Int(Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.y));
        }
    }
}
