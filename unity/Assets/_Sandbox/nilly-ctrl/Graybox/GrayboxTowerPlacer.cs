using System;
using System.Collections.Generic;
using HoldTheHill.Features.Combat;
using HoldTheHill.Features.Economy;
using HoldTheHill.Features.Enemies;
using HoldTheHill.Features.Towers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>What the cursor would build right now, for the placement label.</summary>
    public struct PlacementPreview
    {
        /// <summary>True while a build-bar tower is picked and the cursor is over the field.</summary>
        public bool Active;

        public GrayboxTowerData Tower;

        /// <summary>The grid cell under the cursor.</summary>
        public Vector3 Cell;

        /// <summary>The cell is off the trail, not blocked and has no tower.</summary>
        public bool Clear;

        public bool CanAfford;

        public int Cost;
    }

    /// <summary>
    /// Places and sells towers: reads the mouse and the build keys, keeps the selection, the open
    /// build menu and the tower being placed, and shows the placement ghost under the cursor.
    /// </summary>
    /// <remarks>
    /// It draws nothing on screen apart from the ghost. The build bar, the tower card, the build
    /// menu and the placement label are uGUI panels (the Hud/ folder) that read the state here and
    /// call the commands here.
    /// </remarks>
    [AddComponentMenu("Hold the Hill/Graybox/Graybox Tower Placer & Inspector")]
    public class GrayboxTowerPlacer : MonoBehaviour
    {
        private const float TowerSpacing = 0.8f;

        [Tooltip("The towers that can be built, in build bar order.")]
        [SerializeField] private GrayboxTowerCatalog _catalog;
        [Tooltip("Material for the placement ghost's range ring.")]
        [SerializeField] private Material _lineMaterial;
        [Tooltip("Holds the build and sell effects. Left empty, towers appear without them.")]
        [SerializeField] private SpriteAnimLibrary _animLibrary;
        [Tooltip("What upgrading a tower costs, by the level it is at: the first for level 1 to 2, and so on. Before Skill Tree discounts.")]
        [SerializeField] private int[] _upgradeCosts = { 150, 300 };

        private readonly List<Tower> _towers = new List<Tower>();
        private TowerCommands _commands;
        private Camera _mainCamera;
        private Tower _selectedTower;

        private bool _showBuildMenu;
        private Vector3 _buildWorldPos;

        private int _activePlacementTypeIndex = -1; // -1 when not in active build bar placement mode

        private int TowerCount => _catalog != null ? _catalog.Count : 0;

        /// <summary>The towers on offer. A species swaps this for its own.</summary>
        public GrayboxTowerCatalog Catalog => _catalog;

        /// <summary>Changes the towers on offer and drops any placement in progress.</summary>
        public void SetCatalog(GrayboxTowerCatalog catalog)
        {
            _catalog = catalog;
            _activePlacementTypeIndex = -1;
            _showBuildMenu = false;
        }

        public Tower SelectedTower => _selectedTower;

        /// <summary>Raised after a tower is placed by the placer (including <see cref="Place"/> calls from tools).</summary>
        public static event Action<Tower> TowerPlaced;

        /// <summary>Raised after a paid upgrade, with the tower at its new level.</summary>
        public static event Action<Tower> TowerUpgraded;

        /// <summary>Raised just before a tower is sold and destroyed.</summary>
        public static event Action<Tower> TowerSold;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            TowerPlaced = null;
            TowerUpgraded = null;
            TowerSold = null;
        }

        /// <summary>True while a tower is selected, the build menu is open, or a tower is being placed.</summary>
        public bool IsBusy => _selectedTower != null || _showBuildMenu || _activePlacementTypeIndex >= 0;

        /// <summary>Index in the catalog of the tower picked on the build bar, or -1.</summary>
        public int ActivePlacementIndex => _activePlacementTypeIndex;

        /// <summary>True while the build menu is open over an empty cell.</summary>
        public bool BuildMenuOpen => _showBuildMenu;

        /// <summary>The cell the build menu is open over.</summary>
        public Vector3 BuildMenuCell => _buildWorldPos;

        /// <summary>What the cursor would build now. <see cref="PlacementPreview.Active"/> is false when nothing is being placed.</summary>
        public PlacementPreview Preview { get; private set; }

        /// <summary>Drops any selection, build menu or placement in progress.</summary>
        public void CancelInteraction()
        {
            _selectedTower = null;
            _showBuildMenu = false;
            _activePlacementTypeIndex = -1;
        }

        private void Awake()
        {
            _mainCamera = Camera.main;
            // The economy may wake after the placer, so the wallet is asked for each time.
            _commands = new TowerCommands(() => GrayboxEconomy.Instance != null ? GrayboxEconomy.Instance.Wallet : null, _upgradeCosts);
            _commands.Placed += OnPlaced;
            _commands.Upgraded += OnUpgraded;
            _commands.Sold += OnSold;
        }

        private void OnPlaced(Tower tower)
        {
            GrayboxSfx.PlayCue("Place", tower.transform.position);
            SpriteClipPlayer.SpawnOneShot(_animLibrary, "FxBuild", "Rise", tower.transform.position, Quaternion.identity, 1f, 3);
            _showBuildMenu = false;
            _selectedTower = tower;
            TowerPlaced?.Invoke(tower);
        }

        private void OnUpgraded(Tower tower)
        {
            GrayboxSfx.PlayCue("Upgrade", tower.transform.position);
            TowerUpgraded?.Invoke(tower);
        }

        private void OnSold(Tower tower)
        {
            TowerSold?.Invoke(tower);
            GrayboxSfx.PlayCue("Sell", tower.transform.position);
            SpriteClipPlayer.SpawnOneShot(_animLibrary, "FxBuild", "Sell", tower.transform.position, Quaternion.identity, 1f, 3);
        }

        // ---------- commands, for the HUD panels and the build keys ----------

        /// <summary>Picks the catalog tower for placement, or puts it back if it was already picked.</summary>
        public void TogglePlacement(int index)
        {
            if (_activePlacementTypeIndex == index)
            {
                _activePlacementTypeIndex = -1;
            }
            else if (index >= 0 && index < TowerCount)
            {
                _activePlacementTypeIndex = index;
                _selectedTower = null;
                _showBuildMenu = false;
            }
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
        public void BuildAtMenu(GrayboxTowerData data)
        {
            TryBuild(data, _buildWorldPos);
        }

        /// <summary>
        /// The role of a tower in play: its weapon's, else the catalog entry with its name (towers the
        /// placer made are named for their data), else Gunner.
        /// </summary>
        public TowerArchetype ArchetypeOf(Tower tower)
        {
            TowerWeapon weapon = tower.GetComponent<TowerWeapon>();
            if (weapon != null && weapon.Archetype != TowerArchetype.Unspecified)
            {
                return weapon.Archetype;
            }

            for (int i = 0; i < TowerCount; i++)
            {
                if (_catalog[i].DisplayName == tower.name)
                {
                    return _catalog[i].Archetype;
                }
            }

            return TowerArchetype.Gunner;
        }

        /// <summary>What a tower costs to build, after Skill Tree discounts.</summary>
        public int CostOf(GrayboxTowerData data) => _commands.CostOf(data);

        /// <summary>True with no economy in the scene, so a bare test scene can still build.</summary>
        public bool CanAfford(GrayboxTowerData data) => _commands.CanAfford(data);

        /// <summary>What upgrading this tower costs now, after Skill Tree discounts. 0 if it cannot be upgraded.</summary>
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

        // ---------- input ----------

        private void Update()
        {
            if (!GrayboxGameFlow.GameplayActive)
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

            bool hasPointer = GrayboxControls.TryPointerPosition(out Vector2 pointer);

            UpdateGhost(hasPointer, pointer);

            // Handle hotkey numbers 1-0 for selecting placement mode
            for (int i = 0; i < TowerCount; i++)
            {
                if (GrayboxControls.Pressed(GrayboxControls.BuildId(i)))
                {
                    TogglePlacement(i);
                }
            }

            if (GrayboxControls.Pressed(GrayboxControls.Pause) || GrayboxControls.Pressed(GrayboxControls.Cancel))
            {
                CancelInteraction();
            }

            // Detect pointer clicks on 2D world
            if (hasPointer && GrayboxControls.Pressed(GrayboxControls.Confirm))
            {
                // A click on a HUD panel is the panel's, not the field's.
                if (PointerOverUi())
                {
                    return;
                }

                Vector3 worldPoint = _mainCamera.ScreenToWorldPoint(pointer);
                worldPoint.z = 0f;

                // If currently in active placement mode from the build bar
                if (_activePlacementTypeIndex >= 0 && _activePlacementTypeIndex < TowerCount)
                {
                    Vector3 buildPos = BuildGrid.Snap(worldPoint);
                    if (!BuildBlockers.Blocked(buildPos) && FindTowerAt(buildPos, TowerSpacing) == null)
                    {
                        TryBuild(_catalog[_activePlacementTypeIndex], buildPos);
                    }
                    return;
                }

                Tower clickedTower = FindTowerAt(worldPoint, TowerSpacing);

                if (clickedTower != null)
                {
                    _selectedTower = clickedTower;
                    _showBuildMenu = false;
                    _activePlacementTypeIndex = -1;
                }
                else
                {
                    // Clicked empty ground
                    if (!BuildBlockers.Blocked(BuildGrid.Snap(worldPoint)))
                    {
                        _buildWorldPos = BuildGrid.Snap(worldPoint);
                        _showBuildMenu = true;
                        _selectedTower = null;
                    }
                    else
                    {
                        _showBuildMenu = false;
                    }
                }
            }
        }

        // No event system (a bare test scene) means no panels to be over.
        private static bool PointerOverUi()
        {
            EventSystem events = EventSystem.current;
            return events != null && events.IsPointerOverGameObject();
        }

        private Tower FindTowerAt(Vector3 point, float radius)
        {
            Tower.GetActive(_towers);
            return BuildRules.TowerAt(point, _towers, radius);
        }

        /// <summary>The centre of the grid cell a world position falls in.</summary>
        public static Vector3 Snap(Vector3 position) => BuildGrid.Snap(position);

        /// <summary>
        /// True if a tower could stand on the cell at this position: clear of the trail and of
        /// blocked ground, and no other tower there. <paramref name="ignore"/> is left out of that
        /// check, for a tower that is being moved.
        /// </summary>
        public bool CanBuildAt(Vector3 position, Tower ignore = null)
        {
            Tower.GetActive(_towers);
            return BuildRules.CanBuildAt(position, _towers, ignore, TowerSpacing, BuildBlockers.All);
        }

        // ---------- placement ghost ----------

        private const int GhostRingSegments = 48;
        private SpriteRenderer _ghost;
        private LineRenderer _ghostRing;

        /// <summary>
        /// While a build-bar tower is selected, shows that tower under the cursor, snapped to the
        /// grid, with its range ring: green where it can be built, red where it can't. Also fills
        /// in <see cref="Preview"/> for the placement label.
        /// </summary>
        private void UpdateGhost(bool hasPointer, Vector2 pointer)
        {
            int index = _activePlacementTypeIndex;
            bool active = index >= 0 && index < TowerCount && hasPointer && !PointerOverUi();
            if (!active)
            {
                Preview = default;
                if (_ghost != null) _ghost.gameObject.SetActive(false);
                return;
            }

            if (_ghost == null)
            {
                _ghost = new GameObject("Placement Ghost").AddComponent<SpriteRenderer>();
                _ghost.sortingOrder = 6;

                _ghostRing = new GameObject("Range").AddComponent<LineRenderer>();
                _ghostRing.transform.SetParent(_ghost.transform, false);
                _ghostRing.useWorldSpace = false;
                _ghostRing.loop = true;
                _ghostRing.positionCount = GhostRingSegments;
                _ghostRing.startWidth = _ghostRing.endWidth = 0.05f;
                _ghostRing.sharedMaterial = _lineMaterial;
                _ghostRing.sortingOrder = 6;
            }

            GrayboxTowerData info = _catalog[index];
            Vector3 world = _mainCamera.ScreenToWorldPoint(pointer);
            world.z = 0f;
            Vector3 buildPos = BuildGrid.Snap(world);

            int cost = CostOf(info);
            bool canAfford = _commands.CanAfford(info);
            bool isClear = !BuildBlockers.Blocked(buildPos) && FindTowerAt(buildPos, TowerSpacing) == null;
            Color tint = canAfford && isClear ? new Color(0.6f, 1f, 0.6f, 0.75f) : new Color(1f, 0.4f, 0.4f, 0.65f);

            Preview = new PlacementPreview { Active = true, Tower = info, Cell = buildPos, Clear = isClear, CanAfford = canAfford, Cost = cost };

            _ghost.sprite = info.GhostSprite;
            _ghost.color = tint;
            _ghost.transform.position = buildPos;
            _ghost.gameObject.SetActive(true);

            // The Nurse has almost no reach; keep the ring big enough to see.
            float ringRadius = Mathf.Max(info.Range, 0.5f);
            for (int i = 0; i < GhostRingSegments; i++)
            {
                float angle = i * Mathf.PI * 2f / GhostRingSegments;
                _ghostRing.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * ringRadius);
            }

            _ghostRing.startColor = _ghostRing.endColor = tint;
        }

        // Pays for a tower and places it. Does nothing if the cost is not there.
        private void TryBuild(GrayboxTowerData data, Vector3 position)
        {
            _commands.TryBuild(data, position);
        }

        /// <summary>Places a tower from its prefab, without charging for it, and selects it.</summary>
        public Tower Place(GrayboxTowerData data, Vector3 position, int cost)
        {
            return _commands.Place(data, position, cost);
        }
    }
}
