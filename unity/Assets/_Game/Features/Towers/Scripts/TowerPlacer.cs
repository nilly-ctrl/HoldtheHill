using System;
using System.Collections.Generic;
using HoldTheHill.Features.Combat;
using HoldTheHill.Features.Economy;
using HoldTheHill.Features.GameFlow;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace HoldTheHill.Features.Towers
{
    /// <summary>
    /// Places, upgrades and sells towers: reads the pointer and the build keys through Input
    /// System actions, keeps the selection, the open build menu and the tower being placed, and
    /// shows the placement ghost under the cursor.
    /// </summary>
    /// <remarks>
    /// It draws nothing on screen apart from the ghost. A build bar, a tower card, a build menu
    /// and a placement label are UI that read the state here and call the commands here. The
    /// rules are elsewhere: <see cref="BuildRules"/> says where a tower may stand,
    /// <see cref="TowerCommands"/> pays for it. With no flow it is always active; with no wallet
    /// everything is free; with no actions assigned it reads no input, so a UI can still drive it.
    /// </remarks>
    [AddComponentMenu("Hold the Hill/Towers/Tower Placer")]
    public class TowerPlacer : MonoBehaviour
    {
        [Header("Towers")]
        [Tooltip("The towers that can be built, in build bar order.")]
        [SerializeField] private List<TowerData> _towers = new List<TowerData>();

        [Tooltip("What upgrading a tower costs, by the level it is at: the first for level 1 to 2, and so on. Before discounts.")]
        [SerializeField] private int[] _upgradeCosts = { 150, 300 };

        [Tooltip("How close two towers may stand, in world units.")]
        [SerializeField, Min(0f)] private float _towerSpacing = 0.8f;

        [Header("Scene")]
        [Tooltip("Pays for towers. Empty means everything is free.")]
        [SerializeField] private WalletBehaviour _wallet;

        [Tooltip("Gates input on the game playing. Empty means always active.")]
        [SerializeField] private GameFlowController _flow;

        [Tooltip("Material for the placement ghost's range ring.")]
        [SerializeField] private Material _lineMaterial;

        [Header("Input")]
        [Tooltip("The pointer position on screen (the UI map's Point).")]
        [SerializeField] private InputActionReference _point;

        [Tooltip("Confirms a cell or picks a tower (the UI map's Click).")]
        [SerializeField] private InputActionReference _confirm;

        [Tooltip("Drops the selection, build menu or placement (the UI map's Cancel).")]
        [SerializeField] private InputActionReference _cancel;

        private readonly List<Tower> _standing = new List<Tower>();
        private TowerCommands _commands;
        private PlacementGhost _ghost;
        private Camera _mainCamera;
        private Tower _selectedTower;
        private bool _showBuildMenu;
        private Vector3 _buildWorldPos;
        private int _activePlacementIndex = -1;

        /// <summary>Raised after a tower is placed by the placer (including <see cref="Place"/> calls from tools).</summary>
        public event Action<Tower> TowerPlaced;

        /// <summary>Raised after a paid upgrade, with the tower at its new level.</summary>
        public event Action<Tower> TowerUpgraded;

        /// <summary>Raised just before a tower is sold and destroyed.</summary>
        public event Action<Tower> TowerSold;

        /// <summary>The commands this placer pays through.</summary>
        public TowerCommands Commands => _commands;

        public Tower SelectedTower => _selectedTower;

        /// <summary>True while a tower is selected, the build menu is open, or a tower is being placed.</summary>
        public bool IsBusy => _selectedTower != null || _showBuildMenu || _activePlacementIndex >= 0;

        /// <summary>Index in the catalog of the tower picked for placement, or -1.</summary>
        public int ActivePlacementIndex => _activePlacementIndex;

        /// <summary>True while the build menu is open over an empty cell.</summary>
        public bool BuildMenuOpen => _showBuildMenu;

        /// <summary>The cell the build menu is open over.</summary>
        public Vector3 BuildMenuCell => _buildWorldPos;

        /// <summary>What the cursor would build now. <see cref="PlacementPreview.Active"/> is false when nothing is being placed.</summary>
        public PlacementPreview Preview { get; private set; }

        /// <summary>How many towers are on offer.</summary>
        public int TowerCount => Towers.Count;

        /// <summary>The centre of the grid cell a world position falls in.</summary>
        public static Vector3 Snap(Vector3 position) => BuildGrid.Snap(position);

        // ---------- what a subclass may swap ----------

        /// <summary>The towers on offer, in build bar order. The serialized list by default.</summary>
        protected virtual IReadOnlyList<TowerData> Towers => _towers;

        /// <summary>The wallet to pay from right now, or null for free.</summary>
        protected virtual Wallet WalletNow => _wallet != null ? _wallet.Wallet : null;

        /// <summary>True while the game is on screen and running, so input is read.</summary>
        protected virtual bool GameplayActive => _flow == null || _flow.IsPlaying;

        /// <summary>The pointer position on screen, if there is a pointer.</summary>
        protected virtual bool TryReadPointer(out Vector2 position)
        {
            if (_point != null && _point.action != null && _point.action.enabled)
            {
                position = _point.action.ReadValue<Vector2>();
                return true;
            }

            position = default;
            return false;
        }

        /// <summary>True on the frame the confirm action was pressed.</summary>
        protected virtual bool ConfirmPressed() => _confirm != null && _confirm.action != null && _confirm.action.WasPressedThisFrame();

        /// <summary>True on the frame the cancel action was pressed.</summary>
        protected virtual bool CancelPressed() => _cancel != null && _cancel.action != null && _cancel.action.WasPressedThisFrame();

        /// <summary>The catalog index whose build key was pressed this frame, or -1. None by default.</summary>
        protected virtual int BuildKeyPressed() => -1;

        /// <summary>After a tower is placed: a sound, an effect. The selection and events are already handled.</summary>
        protected virtual void OnPlaced(Tower tower)
        {
        }

        protected virtual void OnUpgraded(Tower tower)
        {
        }

        /// <summary>Just before a sold tower is destroyed.</summary>
        protected virtual void OnSold(Tower tower)
        {
        }

        // ---------- lifecycle ----------

        protected virtual void Awake()
        {
            _mainCamera = Camera.main;
            // The wallet may wake after the placer, so it is asked for each time.
            _commands = new TowerCommands(() => WalletNow, _upgradeCosts);
            _commands.Placed += HandlePlaced;
            _commands.Upgraded += HandleUpgraded;
            _commands.Sold += HandleSold;
            _ghost = new PlacementGhost(_lineMaterial);
        }

        protected virtual void OnEnable()
        {
            Enable(_point);
            Enable(_confirm);
            Enable(_cancel);
        }

        private static void Enable(InputActionReference reference)
        {
            if (reference != null && reference.action != null)
            {
                reference.action.Enable();
            }
        }

        private void HandlePlaced(Tower tower)
        {
            tower.Cell = BuildGrid.CellOf(tower.transform.position);
            _showBuildMenu = false;
            _selectedTower = tower;
            OnPlaced(tower);
            TowerPlaced?.Invoke(tower);
        }

        private void HandleUpgraded(Tower tower)
        {
            OnUpgraded(tower);
            TowerUpgraded?.Invoke(tower);
        }

        private void HandleSold(Tower tower)
        {
            OnSold(tower);
            TowerSold?.Invoke(tower);
        }

        // ---------- commands, for the UI and the build keys ----------

        /// <summary>Picks the catalog tower for placement, or puts it back if it was already picked.</summary>
        public void TogglePlacement(int index)
        {
            if (_activePlacementIndex == index)
            {
                _activePlacementIndex = -1;
            }
            else if (index >= 0 && index < Towers.Count)
            {
                _activePlacementIndex = index;
                _selectedTower = null;
                _showBuildMenu = false;
            }
        }

        /// <summary>Drops any selection, build menu or placement in progress.</summary>
        public void CancelInteraction()
        {
            _selectedTower = null;
            _showBuildMenu = false;
            _activePlacementIndex = -1;
        }

        /// <summary>Drops the build menu and any placement in progress, keeping the selection. For a catalog change.</summary>
        protected void ClosePlacement()
        {
            _activePlacementIndex = -1;
            _showBuildMenu = false;
        }

        /// <summary>Closes the tower card.</summary>
        public void Deselect()
        {
            _selectedTower = null;
        }

        /// <summary>Closes the build menu.</summary>
        public void CloseBuildMenu()
        {
            _showBuildMenu = false;
        }

        /// <summary>Builds a tower on the cell the build menu is open over.</summary>
        public void BuildAtMenu(TowerData data)
        {
            _commands.TryBuild(data, _buildWorldPos);
        }

        /// <summary>What a tower costs to build, after discounts.</summary>
        public int CostOf(TowerData data) => _commands.CostOf(data);

        /// <summary>True with no wallet in the scene, so a bare test scene can still build.</summary>
        public bool CanAfford(TowerData data) => _commands.CanAfford(data);

        /// <summary>What upgrading this tower costs now, after discounts. 0 if it cannot be upgraded.</summary>
        public int UpgradeCost(Tower tower) => _commands.UpgradeCost(tower);

        /// <summary>Pays for and applies the next level of the selected tower. False if it cannot or it costs too much.</summary>
        public bool TryUpgradeSelected()
        {
            return _selectedTower != null && _commands.TryUpgrade(_selectedTower);
        }

        /// <summary>Sells the selected tower for its refund and closes its card.</summary>
        public void SellSelected()
        {
            Tower tower = _selectedTower;
            if (tower == null)
            {
                return;
            }

            _selectedTower = null;
            _commands.Sell(tower);
        }

        /// <summary>Places a tower from its prefab, without charging for it, and selects it.</summary>
        public Tower Place(TowerData data, Vector3 position, int cost)
        {
            return _commands.Place(data, position, cost);
        }

        /// <summary>
        /// True if a tower could stand on the cell at this position: clear of the trail and of
        /// blocked ground, and no other tower there. <paramref name="ignore"/> is left out of that
        /// check, for a tower that is being moved.
        /// </summary>
        public bool CanBuildAt(Vector3 position, Tower ignore = null)
        {
            Tower.GetActive(_standing);
            return BuildRules.CanBuildAt(position, _standing, ignore, _towerSpacing, BuildBlockers.All);
        }

        /// <summary>The role of a tower in play, for a build bar that groups by role.</summary>
        public TowerArchetype ArchetypeOf(Tower tower) => TowerArchetypes.Of(tower, Towers);

        // ---------- input ----------

        private void Update()
        {
            if (!GameplayActive)
            {
                return; // paused or on a menu: no building
            }

            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
                if (_mainCamera == null)
                {
                    return;
                }
            }

            bool hasPointer = TryReadPointer(out Vector2 pointer);
            UpdateGhost(hasPointer, pointer);

            int key = BuildKeyPressed();
            if (key >= 0)
            {
                TogglePlacement(key);
            }

            if (CancelPressed())
            {
                CancelInteraction();
            }

            if (!hasPointer || !ConfirmPressed() || PointerOverUi())
            {
                return; // a click on a UI panel is the panel's, not the field's
            }

            Vector3 worldPoint = _mainCamera.ScreenToWorldPoint(pointer);
            worldPoint.z = 0f;
            Vector3 cell = BuildGrid.Snap(worldPoint);

            if (_activePlacementIndex >= 0 && _activePlacementIndex < Towers.Count)
            {
                if (CanBuildAt(cell))
                {
                    _commands.TryBuild(Towers[_activePlacementIndex], cell);
                }

                return;
            }

            Tower.GetActive(_standing);
            Tower clicked = BuildRules.TowerAt(worldPoint, _standing, _towerSpacing);
            if (clicked != null)
            {
                _selectedTower = clicked;
                _showBuildMenu = false;
                _activePlacementIndex = -1;
                return;
            }

            // Clicked empty ground.
            _selectedTower = null;
            _showBuildMenu = !BuildBlockers.Blocked(cell);
            if (_showBuildMenu)
            {
                _buildWorldPos = cell;
            }
        }

        // No event system (a bare test scene) means no panels to be over.
        private static bool PointerOverUi()
        {
            EventSystem events = EventSystem.current;
            return events != null && events.IsPointerOverGameObject();
        }

        // ---------- placement ghost ----------

        /// <summary>
        /// While a catalog tower is picked, shows its <see cref="PlacementGhost"/> under the cursor
        /// and fills in <see cref="Preview"/> for a placement label.
        /// </summary>
        private void UpdateGhost(bool hasPointer, Vector2 pointer)
        {
            int index = _activePlacementIndex;
            if (index < 0 || index >= Towers.Count || !hasPointer || PointerOverUi())
            {
                Preview = default;
                _ghost.Hide();
                return;
            }

            TowerData info = Towers[index];
            Vector3 world = _mainCamera.ScreenToWorldPoint(pointer);
            world.z = 0f;
            Vector3 cell = BuildGrid.Snap(world);

            int cost = CostOf(info);
            bool canAfford = CanAfford(info);
            bool isClear = CanBuildAt(cell);
            Preview = new PlacementPreview { Active = true, Tower = info, Cell = cell, Clear = isClear, CanAfford = canAfford, Cost = cost };
            _ghost.Show(info, cell, canAfford && isClear);
        }
    }
}
