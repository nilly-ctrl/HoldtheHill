using HoldTheHill.Core.Audio;
using HoldTheHill.Core.Input;
using HoldTheHill.Features.GameFlow;
using HoldTheHill.Core.Animation;
using System.Collections.Generic;
using HoldTheHill.Features.Economy;
using HoldTheHill.Features.Towers;
using UnityEngine;

namespace HoldTheHill.Features.Towers
{
    /// <summary>
    /// The graybox's <see cref="TowerPlacer"/>: reads the graybox controls (pointer, confirm,
    /// cancel and the build keys 1 to 0), pays from the <see cref="GrayboxEconomy"/> it finds once, follows
    /// <see cref="GrayboxGameFlow"/>, takes its towers from a <see cref="GrayboxTowerCatalog"/>
    /// (a species swaps it), and plays the build and sell sounds and effects. Its events are the
    /// base class's instance events.
    /// </summary>
    [AddComponentMenu("Hold the Hill/Graybox/Graybox Tower Placer & Inspector")]
    public class GrayboxTowerPlacer : TowerPlacer
    {
        [Tooltip("The towers that can be built, in build bar order.")]
        [SerializeField] private GrayboxTowerCatalog _catalog;
        [Tooltip("Holds the build and sell effects. Left empty, towers appear without them.")]
        [SerializeField] private SpriteAnimLibrary _animLibrary;

        /// <summary>The towers on offer. A species swaps this for its own.</summary>
        public GrayboxTowerCatalog Catalog => _catalog;

        /// <summary>Changes the towers on offer and drops any placement in progress.</summary>
        public void SetCatalog(GrayboxTowerCatalog catalog)
        {
            _catalog = catalog;
            ClosePlacement();
        }

        protected override IReadOnlyList<TowerData> Towers => _catalog != null ? _catalog.Towers : System.Array.Empty<TowerData>();

        private GrayboxEconomy _economy;

        protected override Wallet WalletNow
        {
            get
            {
                if (_economy == null) _economy = FindAnyObjectByType<GrayboxEconomy>();
                return _economy != null ? _economy.Wallet : null;
            }
        }

        private GrayboxGameFlow _flow;
        private bool _flowSought;

        // Found once, kept: no flow in the scene (a bare test scene) counts as playing.
        protected override bool GameplayActive
        {
            get
            {
                if (!_flowSought)
                {
                    _flow = FindAnyObjectByType<GrayboxGameFlow>();
                    _flowSought = true;
                }

                return _flow == null || _flow.IsPlaying;
            }
        }

        protected override bool TryReadPointer(out Vector2 position) => GrayboxControls.TryPointerPosition(out position);

        protected override bool ConfirmPressed() => GrayboxControls.Pressed(GrayboxControls.Confirm);

        // Esc is both pause and cancel in the graybox: the flow pauses, the placer drops what it holds.
        protected override bool CancelPressed() => GrayboxControls.Pressed(GrayboxControls.Pause) || GrayboxControls.Pressed(GrayboxControls.Cancel);

        protected override int BuildKeyPressed()
        {
            for (int i = 0; i < Towers.Count; i++)
            {
                if (GrayboxControls.Pressed(GrayboxControls.BuildId(i)))
                {
                    return i;
                }
            }

            return -1;
        }

        protected override void OnPlaced(Tower tower)
        {
            GrayboxSfx.PlayCue("Place", tower.transform.position);
            SpriteClipPlayer.SpawnOneShot(_animLibrary, "FxBuild", "Rise", tower.transform.position, Quaternion.identity, 1f, 3);
        }

        protected override void OnUpgraded(Tower tower)
        {
            GrayboxSfx.PlayCue("Upgrade", tower.transform.position);
        }

        protected override void OnSold(Tower tower)
        {
            GrayboxSfx.PlayCue("Sell", tower.transform.position);
            SpriteClipPlayer.SpawnOneShot(_animLibrary, "FxBuild", "Sell", tower.transform.position, Quaternion.identity, 1f, 3);
        }
    }
}
