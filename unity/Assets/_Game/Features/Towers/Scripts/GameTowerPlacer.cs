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
    /// cancel and the build keys 1 to 0), pays from the <see cref="Wallet"/> it finds once, follows
    /// <see cref="RunFlow"/>, takes its towers from a <see cref="TowerCatalog"/>
    /// (a species swaps it), and plays the build and sell sounds and effects. Its events are the
    /// base class's instance events.
    /// </summary>
    [AddComponentMenu("Hold the Hill/Graybox Tower Placer & Inspector")]
    public class GameTowerPlacer : TowerPlacer
    {
        [Tooltip("The towers that can be built, in build bar order.")]
        [SerializeField] private TowerCatalog _catalog;
        [Tooltip("Holds the build and sell effects. Left empty, towers appear without them.")]
        [SerializeField] private SpriteAnimLibrary _animLibrary;

        /// <summary>The towers on offer. A species swaps this for its own.</summary>
        public TowerCatalog Catalog => _catalog;

        /// <summary>Changes the towers on offer and drops any placement in progress.</summary>
        public void SetCatalog(TowerCatalog catalog)
        {
            _catalog = catalog;
            ClosePlacement();
        }

        protected override IReadOnlyList<TowerData> Towers => _catalog != null ? _catalog.Towers : System.Array.Empty<TowerData>();

        private WalletBehaviour _economy;

        protected override Wallet WalletNow
        {
            get
            {
                if (_economy == null) _economy = FindAnyObjectByType<WalletBehaviour>();
                return _economy != null ? _economy.Wallet : null;
            }
        }

        private RunFlow _flow;
        private bool _flowSought;

        // Found once, kept: no flow in the scene (a bare test scene) counts as playing.
        protected override bool GameplayActive
        {
            get
            {
                if (!_flowSought)
                {
                    _flow = FindAnyObjectByType<RunFlow>();
                    _flowSought = true;
                }

                return _flow == null || _flow.IsPlaying;
            }
        }

        protected override bool TryReadPointer(out Vector2 position) => GameControls.TryPointerPosition(out position);

        protected override bool ConfirmPressed() => GameControls.Pressed(GameControls.Confirm);

        // Esc is both pause and cancel in the graybox: the flow pauses, the placer drops what it holds.
        protected override bool CancelPressed() => GameControls.Pressed(GameControls.Pause) || GameControls.Pressed(GameControls.Cancel);

        protected override int BuildKeyPressed()
        {
            for (int i = 0; i < Towers.Count; i++)
            {
                if (GameControls.Pressed(GameControls.BuildId(i)))
                {
                    return i;
                }
            }

            return -1;
        }

        protected override void OnPlaced(Tower tower)
        {
            Sfx.PlayCue("Place", tower.transform.position);
            SpriteClipPlayer.SpawnOneShot(_animLibrary, "FxBuild", "Rise", tower.transform.position, Quaternion.identity, 1f, 3);
        }

        protected override void OnUpgraded(Tower tower)
        {
            Sfx.PlayCue("Upgrade", tower.transform.position);
        }

        protected override void OnSold(Tower tower)
        {
            Sfx.PlayCue("Sell", tower.transform.position);
            SpriteClipPlayer.SpawnOneShot(_animLibrary, "FxBuild", "Sell", tower.transform.position, Quaternion.identity, 1f, 3);
        }
    }
}
