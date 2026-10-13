using System;
using System.Collections.Generic;
using HoldTheHill.Features.Economy;
using HoldTheHill.Features.Towers;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// The graybox's <see cref="TowerPlacer"/>: reads the graybox controls (pointer, confirm,
    /// cancel and the build keys 1 to 0), pays from <see cref="GrayboxEconomy"/>, follows
    /// <see cref="GrayboxGameFlow"/>, takes its towers from a <see cref="GrayboxTowerCatalog"/>
    /// (a species swaps it), and plays the build and sell sounds and effects. Keeps the static
    /// events the HUD and the dev tools listen to.
    /// </summary>
    [AddComponentMenu("Hold the Hill/Graybox/Graybox Tower Placer & Inspector")]
    public class GrayboxTowerPlacer : TowerPlacer
    {
        [Tooltip("The towers that can be built, in build bar order.")]
        [SerializeField] private GrayboxTowerCatalog _catalog;
        [Tooltip("Holds the build and sell effects. Left empty, towers appear without them.")]
        [SerializeField] private SpriteAnimLibrary _animLibrary;

        /// <summary>Raised after a tower is placed by any graybox placer. The static twin of the base event.</summary>
        public static new event Action<Tower> TowerPlaced;

        /// <summary>Raised after a paid upgrade, with the tower at its new level.</summary>
        public static new event Action<Tower> TowerUpgraded;

        /// <summary>Raised just before a tower is sold and destroyed.</summary>
        public static new event Action<Tower> TowerSold;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            TowerPlaced = null;
            TowerUpgraded = null;
            TowerSold = null;
        }

        /// <summary>The towers on offer. A species swaps this for its own.</summary>
        public GrayboxTowerCatalog Catalog => _catalog;

        /// <summary>Changes the towers on offer and drops any placement in progress.</summary>
        public void SetCatalog(GrayboxTowerCatalog catalog)
        {
            _catalog = catalog;
            ClosePlacement();
        }

        protected override IReadOnlyList<TowerData> Towers => _catalog != null ? _catalog.Towers : System.Array.Empty<TowerData>();

        protected override Wallet WalletNow => GrayboxEconomy.Instance != null ? GrayboxEconomy.Instance.Wallet : null;

        protected override bool GameplayActive => GrayboxGameFlow.GameplayActive;

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

        protected override void OnEnable()
        {
            base.OnEnable();
            base.TowerPlaced += ForwardPlaced;
            base.TowerUpgraded += ForwardUpgraded;
            base.TowerSold += ForwardSold;
        }

        private void OnDisable()
        {
            base.TowerPlaced -= ForwardPlaced;
            base.TowerUpgraded -= ForwardUpgraded;
            base.TowerSold -= ForwardSold;
        }

        private static void ForwardPlaced(Tower tower) => TowerPlaced?.Invoke(tower);

        private static void ForwardUpgraded(Tower tower) => TowerUpgraded?.Invoke(tower);

        private static void ForwardSold(Tower tower) => TowerSold?.Invoke(tower);

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
