using System.Collections.Generic;
using HoldTheHill.Features.Towers;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// The towers the player can build, in build bar order. Slot i uses build hotkey i.
    /// </summary>
    [CreateAssetMenu(menuName = "Hold the Hill/Sandbox/Graybox Tower Catalog", fileName = "GrayboxTowerCatalog")]
    public class GrayboxTowerCatalog : ScriptableObject
    {
        [SerializeField] private List<TowerData> _towers = new List<TowerData>();

        public int Count => _towers.Count;

        /// <summary>The towers in build bar order, as the shared placer reads them.</summary>
        public IReadOnlyList<TowerData> Towers => _towers;

        public TowerData this[int index] => _towers[index];
    }
}
