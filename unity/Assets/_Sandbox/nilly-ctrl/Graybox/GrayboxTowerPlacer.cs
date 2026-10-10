using System.Collections.Generic;
using HoldTheHill.Features.Combat;
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
        private const float PathClearance = 1.2f;
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
        private Camera _mainCamera;
        private EnemyPath _path;
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
            _path = FindAnyObjectByType<EnemyPath>();
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
        public int CostOf(GrayboxTowerData data)
        {
            return GetDiscountedCost(data.BaseCost);
        }

        /// <summary>True with no economy in the scene, so a bare test scene can still build.</summary>
        public bool CanAfford(GrayboxTowerData data)
        {
            return CanAfford(data, CostOf(data));
        }

        /// <summary>What upgrading this tower costs now, after Skill Tree discounts. 0 if it cannot be upgraded.</summary>
        public int UpgradeCost(Tower tower)
        {
            if (tower == null || !tower.CanUpgrade || _upgradeCosts == null || _upgradeCosts.Length == 0)
            {
                return 0;
            }

            int step = Mathf.Clamp(tower.Level - 1, 0, _upgradeCosts.Length - 1);
            return GetDiscountedCost(_upgradeCosts[step]);
        }

        /// <summary>Pays for and applies the next level of the selected tower. False if it cannot or it costs too much.</summary>
        public bool TryUpgradeSelected()
        {
            Tower tower = _selectedTower;
            if (tower == null || !tower.CanUpgrade)
            {
                return false;
            }

            int cost = UpgradeCost(tower);
            if (GrayboxEconomy.Instance != null && !GrayboxEconomy.Instance.TrySpendGold(cost))
            {
                return false;
            }

            tower.Upgrade();
            tower.TotalGoldInvested += cost;
            GrayboxSfx.PlayCue("Upgrade", tower.transform.position);
            return true;
        }

        /// <summary>Sells the selected tower for its refund and closes its card.</summary>
        public void SellSelected()
        {
            Tower tower = _selectedTower;
            if (tower == null)
            {
                return;
            }

            GrayboxEconomy.Instance?.EarnGold(tower.RefundValue);
            GrayboxSfx.PlayCue("Sell", tower.transform.position);
            SpriteClipPlayer.SpawnOneShot(_animLibrary, "FxBuild", "Sell", tower.transform.position, Quaternion.identity, 1f, 3);
            Destroy(tower.gameObject);
            _selectedTower = null;
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

            Mouse mouse = Mouse.current;
            Keyboard keyboard = Keyboard.current;

            UpdateGhost(mouse);

            // Handle hotkey numbers 1-0 for selecting placement mode
            if (keyboard != null)
            {
                for (int i = 0; i < TowerCount; i++)
                {
                    if (GrayboxControls.Pressed(GrayboxControls.BuildId(i)))
                    {
                        TogglePlacement(i);
                    }
                }

                if (GrayboxControls.Pressed(GrayboxControls.Pause))
                {
                    CancelInteraction();
                }
            }

            if (mouse != null && mouse.rightButton.wasPressedThisFrame)
            {
                CancelInteraction();
            }

            // Detect mouse clicks on 2D world
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                // A click on a HUD panel is the panel's, not the field's.
                if (PointerOverUi())
                {
                    return;
                }

                Vector3 worldPoint = _mainCamera.ScreenToWorldPoint(mouse.position.ReadValue());
                worldPoint.z = 0f;

                // If currently in active placement mode from the build bar
                if (_activePlacementTypeIndex >= 0 && _activePlacementTypeIndex < TowerCount)
                {
                    Vector3 buildPos = SnapToGrid(worldPoint);
                    if (IsPositionClearOfPath(buildPos, PathClearance) && FindTowerAt(buildPos, TowerSpacing) == null)
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
                    if (IsPositionClearOfPath(worldPoint, PathClearance))
                    {
                        _buildWorldPos = SnapToGrid(worldPoint);
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
            foreach (Tower t in _towers)
            {
                if (t != null && Vector2.Distance(point, t.transform.position) <= radius)
                {
                    return t;
                }
            }
            return null;
        }

        private bool IsPositionClearOfPath(Vector3 point, float minDistance)
        {
            // Hand-drawn levels mark cabinets, spills and the like as unbuildable.
            if (GrayboxBuildMask.Blocks(point))
            {
                return false;
            }

            if (_path == null)
            {
                _path = FindAnyObjectByType<EnemyPath>();
                if (_path == null)
                {
                    return true;
                }
            }

            return _path.DistanceToRoute(point) >= minDistance;
        }

        /// <summary>The centre of the grid cell a world position falls in.</summary>
        public static Vector3 Snap(Vector3 position) => SnapToGrid(position);

        /// <summary>
        /// True if a tower could stand on the cell at this position: clear of the trail and of
        /// blocked ground, and no other tower there. <paramref name="ignore"/> is left out of that
        /// check, for a tower that is being moved.
        /// </summary>
        public bool CanBuildAt(Vector3 position, Tower ignore = null)
        {
            Vector3 cell = SnapToGrid(position);
            if (!IsPositionClearOfPath(cell, PathClearance))
            {
                return false;
            }

            Tower.GetActive(_towers);
            foreach (Tower tower in _towers)
            {
                if (tower != ignore && Vector2.Distance(cell, tower.transform.position) <= TowerSpacing)
                {
                    return false;
                }
            }

            return true;
        }

        private static Vector3 SnapToGrid(Vector3 pos)
        {
            return new Vector3(Mathf.Round(pos.x), Mathf.Round(pos.y), 0f);
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
        private void UpdateGhost(Mouse mouse)
        {
            int index = _activePlacementTypeIndex;
            bool active = index >= 0 && index < TowerCount && mouse != null && !PointerOverUi();
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
            Vector3 world = _mainCamera.ScreenToWorldPoint(mouse.position.ReadValue());
            world.z = 0f;
            Vector3 buildPos = SnapToGrid(world);

            int cost = CostOf(info);
            bool canAfford = CanAfford(info, cost);
            bool isClear = IsPositionClearOfPath(buildPos, PathClearance) && FindTowerAt(buildPos, TowerSpacing) == null;
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

        // Pays for a tower and places it. Does nothing if the gold isn't there.
        private void TryBuild(GrayboxTowerData data, Vector3 position)
        {
            if (data == null || data.Prefab == null)
            {
                return;
            }

            int cost = GetDiscountedCost(data.BaseCost);
            if (GrayboxEconomy.Instance != null && !GrayboxEconomy.Instance.TrySpend(data.CostResource, cost))
            {
                return;
            }

            Place(data, position, cost);
        }

        private static bool CanAfford(GrayboxTowerData data, int cost)
        {
            return GrayboxEconomy.Instance == null || GrayboxEconomy.Instance.CanAfford(data.CostResource, cost);
        }

        /// <summary>Places a tower from its prefab, without charging for it, and selects it.</summary>
        public Tower Place(GrayboxTowerData data, Vector3 position, int cost)
        {
            Tower tower = Instantiate(data.Prefab, position, Quaternion.identity);
            tower.name = data.DisplayName;
            tower.TotalGoldInvested = cost;

            GrayboxSfx.PlayCue("Place", position);
            SpriteClipPlayer.SpawnOneShot(_animLibrary, "FxBuild", "Rise", position, Quaternion.identity, 1f, 3);

            _showBuildMenu = false;
            _selectedTower = tower;
            return tower;
        }

        private int GetDiscountedCost(int baseCost)
        {
            float mult = GrayboxSkillTree.Instance != null ? GrayboxSkillTree.Instance.CostMultiplier : 1.0f;
            return Mathf.RoundToInt(baseCost * mult);
        }
    }
}
