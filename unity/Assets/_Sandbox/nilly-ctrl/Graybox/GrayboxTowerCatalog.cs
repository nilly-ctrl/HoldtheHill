using System.Collections.Generic;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// The towers the player can build, in build bar order. Slot i uses build hotkey i.
    /// </summary>
    [CreateAssetMenu(menuName = "Hold the Hill/Sandbox/Graybox Tower Catalog", fileName = "GrayboxTowerCatalog")]
    public class GrayboxTowerCatalog : ScriptableObject
    {
        [SerializeField] private List<GrayboxTowerData> _towers = new List<GrayboxTowerData>();

        public int Count => _towers.Count;

        public GrayboxTowerData this[int index] => _towers[index];
    }
}
