using HoldTheHill.Features.Towers;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// The graybox's tower data: a <see cref="TowerData"/> under its own asset menu, kept so the
    /// sandbox's assets, catalogs and builders keep working until they point at
    /// <see cref="TowerData"/> itself (step 6.0 of <c>docs/CODEBASE_CONVERGENCE.md</c>).
    /// </summary>
    [CreateAssetMenu(menuName = "Hold the Hill/Sandbox/Graybox Tower Data", fileName = "TowerData")]
    public class GrayboxTowerData : TowerData
    {
    }
}
