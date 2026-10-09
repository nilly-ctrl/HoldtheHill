using System.Collections.Generic;
using HoldTheHill.Sandbox.UiKit;
using TMPro;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// The menu that opens beside an empty cell when it is clicked: every tower in the catalog,
    /// with its price, to build there. Stays next to the cell it was opened over.
    /// </summary>
    [AddComponentMenu("Hold the Hill/Graybox/HUD/Build Menu")]
    public class GrayboxBuildMenu : GrayboxHudPanel
    {
        [SerializeField] private TMP_Text _cell;
        [SerializeField] private UiButton _cancel;

        [Tooltip("Made once per tower in the catalog.")]
        [SerializeField] private GrayboxBuildOption _optionPrefab;

        [SerializeField] private RectTransform _optionRoot;

        [Tooltip("The menu's own rectangle, anchored at the centre of the canvas.")]
        [SerializeField] private RectTransform _frame;

        private readonly List<GrayboxBuildOption> _options = new List<GrayboxBuildOption>();
        private GrayboxTowerPlacer _placer;
        private GrayboxTowerCatalog _builtFor;
        private Vector3 _cellShown = new Vector3(float.NaN, 0f, 0f);

        public override bool WantsToShow => FindPlacer() != null && _placer.BuildMenuOpen;

        private void Awake()
        {
            OnClick(_cancel.Button, () => _placer.CloseBuildMenu());
        }

        public override void Refresh()
        {
            GrayboxTowerCatalog catalog = _placer.Catalog;
            if (catalog != _builtFor || _options.Count != catalog.Count)
            {
                Rebuild(catalog);
            }

            for (int i = 0; i < _options.Count; i++)
            {
                GrayboxTowerData data = catalog[i];
                _options[i].Show(_placer.CostOf(data), _placer.CanAfford(data));
            }

            Vector3 cell = _placer.BuildMenuCell;
            if (cell != _cellShown)
            {
                _cellShown = cell;
                _cell.text = $"Grid ({cell.x:0}, {cell.y:0})";
            }

            Follow(cell);
        }

        // To the right of the cell so the cell stays in view, and kept on the screen.
        private void Follow(Vector3 cell)
        {
            if (!PlaceAtWorld(_frame, cell, new Vector2(_frame.rect.width * 0.5f + 12f, 0f)))
            {
                return;
            }

            RectTransform canvas = CanvasRect;
            Vector2 half = (canvas.rect.size - _frame.rect.size) * 0.5f - Vector2.one * 4f;
            Vector2 position = _frame.anchoredPosition;
            position.x = Mathf.Clamp(position.x, -half.x, half.x);
            position.y = Mathf.Clamp(position.y, -half.y, half.y);
            _frame.anchoredPosition = position;
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
            foreach (GrayboxBuildOption option in _options)
            {
                Destroy(option.gameObject);
            }

            _options.Clear();
            _builtFor = catalog;

            for (int i = 0; i < catalog.Count; i++)
            {
                GrayboxTowerData data = catalog[i];
                GrayboxBuildOption option = Instantiate(_optionPrefab, _optionRoot);
                option.Bind(data, () =>
                {
                    _placer.BuildAtMenu(data);
                    ReleaseFocus();
                });
                _options.Add(option);
            }
        }
    }
}
