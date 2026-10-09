using System.Collections.Generic;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// The row of buildable towers along the bottom of the screen. It follows the tower placer's
    /// catalog, so a species with different towers gets a different bar without any change here.
    /// </summary>
    [AddComponentMenu("Hold the Hill/Graybox/HUD/Build Bar")]
    public class GrayboxBuildBar : GrayboxHudPanel
    {
        [Tooltip("Made once per tower in the catalog.")]
        [SerializeField] private GrayboxBuildSlot _slotPrefab;

        [Tooltip("Where the slots go. A horizontal layout group.")]
        [SerializeField] private RectTransform _slotRoot;

        private readonly List<GrayboxBuildSlot> _slots = new List<GrayboxBuildSlot>();
        private GrayboxTowerPlacer _placer;
        private GrayboxTowerCatalog _builtFor;

        public override bool WantsToShow => FindPlacer() != null && _placer.Catalog != null && _placer.Catalog.Count > 0;

        public override void Refresh()
        {
            GrayboxTowerCatalog catalog = _placer.Catalog;
            if (catalog != _builtFor || _slots.Count != catalog.Count)
            {
                Rebuild(catalog);
            }

            for (int i = 0; i < _slots.Count; i++)
            {
                GrayboxTowerData data = catalog[i];
                _slots[i].Show(GrayboxControls.Name(GrayboxControls.BuildId(i)), _placer.CostOf(data),
                    _placer.ActivePlacementIndex == i, _placer.CanAfford(data));
            }
        }

        private GrayboxTowerPlacer FindPlacer()
        {
            if (_placer == null)
            {
                _placer = FindAnyObjectByType<GrayboxTowerPlacer>();
            }

            return _placer;
        }

        private void Rebuild(GrayboxTowerCatalog catalog)
        {
            foreach (GrayboxBuildSlot slot in _slots)
            {
                Destroy(slot.gameObject);
            }

            _slots.Clear();
            _builtFor = catalog;

            for (int i = 0; i < catalog.Count; i++)
            {
                int index = i;
                GrayboxBuildSlot slot = Instantiate(_slotPrefab, _slotRoot);
                slot.Bind(catalog[i], () =>
                {
                    _placer.TogglePlacement(index);
                    ReleaseFocus();
                });
                _slots.Add(slot);
            }
        }
    }
}
