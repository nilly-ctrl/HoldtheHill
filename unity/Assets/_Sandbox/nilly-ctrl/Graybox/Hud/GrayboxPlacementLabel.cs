using TMPro;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// The line above the placement ghost: what will be built and for how much, or why it can't be.
    /// Green where the tower can go, red where it can't.
    /// </summary>
    [AddComponentMenu("Hold the Hill/Graybox/HUD/Placement Label")]
    public class GrayboxPlacementLabel : GrayboxHudPanel
    {
        private static readonly Color Good = new Color(0.4f, 1f, 0.4f);
        private static readonly Color Bad = new Color(1f, 0.35f, 0.35f);

        [SerializeField] private TMP_Text _text;

        [Tooltip("Canvas units above the cell.")]
        [SerializeField] private float _lift = 16f;

        private GrayboxTowerPlacer _placer;
        private GrayboxTowerData _shownTower;
        private int _shownCost = -1;
        private int _shownState = -1;

        public override bool WantsToShow => FindPlacer() != null && _placer.Preview.Active;

        public override void Refresh()
        {
            PlacementPreview preview = _placer.Preview;

            // 0 = blocked, 1 = no gold, 2 = fine.
            int state = !preview.Clear ? 0 : (!preview.CanAfford ? 1 : 2);
            if (preview.Tower != _shownTower || preview.Cost != _shownCost || state != _shownState)
            {
                _shownTower = preview.Tower;
                _shownCost = preview.Cost;
                _shownState = state;
                _text.text = state == 0 ? "BLOCKED" : (state == 1 ? "NEED FOOD" : $"BUILD {preview.Tower.DisplayName.ToUpperInvariant()} ${preview.Cost}");
                _text.color = state == 2 ? Good : Bad;
            }

            PlaceAtWorld((RectTransform)transform, preview.Cell, new Vector2(0f, _lift));
        }

        private GrayboxTowerPlacer FindPlacer()
        {
            if (_placer == null)
            {
                _placer = FindAnyObjectByType<GrayboxTowerPlacer>();
            }

            return _placer;
        }
    }
}
