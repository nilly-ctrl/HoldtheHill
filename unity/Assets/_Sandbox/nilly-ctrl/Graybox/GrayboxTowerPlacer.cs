using System.Collections.Generic;
using HoldTheHill.Features.Combat;
using HoldTheHill.Features.Enemies;
using HoldTheHill.Features.Towers;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Handles interactive mouse clicking in PlayMode to inspect placed towers, adjust their
    /// priority rules live, view performance metrics (Kills/Damage/Shots), and place new towers on empty grid spaces.
    /// </summary>
    [AddComponentMenu("Hold the Hill/Graybox/Graybox Tower Placer & Inspector")]
    public class GrayboxTowerPlacer : MonoBehaviour
    {
        [Header("Templates / Prefabs")]
        [SerializeField] private GameObject _bulletPrefab;
        [SerializeField] private GameObject _homingPrefab;
        [SerializeField] private GameObject _mortarPrefab;
        [SerializeField] private GameObject _ricochetPrefab;
        [SerializeField] private GameObject _minePrefab;
        [SerializeField] private Material _lineMaterial;
        [Tooltip("Textured line materials for chain lightning and the beam. Empty keeps the flat lines.")]
        [SerializeField] private Material _boltMaterial;
        [SerializeField] private Material _beamMaterial;
        [SerializeField] private Sprite _squareSprite;
        [Tooltip("Animated tower sprites. Left empty, placed towers stay as coloured squares.")]
        [SerializeField] private SpriteAnimLibrary _animLibrary;

        // Build-menu type names -> .aseprite file the tower animates with.
        private static readonly Dictionary<string, string> AnimKeys = new Dictionary<string, string>
        {
            { "Linear Bullet", "TowerLinear" },
            { "Homing Missile", "TowerHoming" },
            { "Mortar Shell", "TowerMortar" },
            { "Ricochet Disc", "TowerRicochet" },
            { "Frost_Aura", "TowerFrostAura" },
            { "Knockback_Pulse", "TowerKnockback" },
            { "MineLayer", "TowerMineLayer" },
            { "Chain_Lightning", "TowerChain" },
            { "Beam", "TowerBeam" },
            { "Orbit", "TowerOrbit" },
            { "Worker", "TowerWorker" },
            { "Soldier", "TowerSoldier" },
            { "Major", "TowerMajor" },
            { "Nurse", "TowerNurse" },
        };

        private Camera _mainCamera;
        private EnemyPath _path;
        private Tower _selectedTower;

        private bool _showBuildMenu;
        private Vector3 _buildWorldPos;

        private int _activePlacementTypeIndex = -1; // -1 when not in active build bar placement mode

        private GUIStyle _cardHeaderStyle;
        private GUIStyle _cardBodyStyle;
        private GUIStyle _btnStyle;
        private GUIStyle _barBtnStyle;

        private struct TowerBuildInfo
        {
            public string Name;
            public string IconName;
            public int BaseCost;
            public Key Hotkey;
            public string ShortcutLabel;
            public float Range;
        }

        private static readonly TowerBuildInfo[] TowerCatalog = new TowerBuildInfo[]
        {
            new TowerBuildInfo { Name = "Bullet Tower", IconName = "TowerLinearIcon", BaseCost = 100, Hotkey = Key.Digit1, ShortcutLabel = "1", Range = 3.6f },
            new TowerBuildInfo { Name = "Homing Tower", IconName = "TowerHomingIcon", BaseCost = 150, Hotkey = Key.Digit2, ShortcutLabel = "2", Range = 3.8f },
            new TowerBuildInfo { Name = "Mortar Tower", IconName = "TowerMortarIcon", BaseCost = 200, Hotkey = Key.Digit3, ShortcutLabel = "3", Range = 4.2f },
            new TowerBuildInfo { Name = "Ricochet Tower", IconName = "TowerRicochetIcon", BaseCost = 175, Hotkey = Key.Digit4, ShortcutLabel = "4", Range = 3.8f },
            new TowerBuildInfo { Name = "Frost Aura", IconName = "TowerFrostAuraIcon", BaseCost = 225, Hotkey = Key.Digit5, ShortcutLabel = "5", Range = 3.5f },
            new TowerBuildInfo { Name = "Knockback Pulse", IconName = "TowerKnockbackIcon", BaseCost = 200, Hotkey = Key.Digit6, ShortcutLabel = "6", Range = 3.0f },
            new TowerBuildInfo { Name = "Mine Layer", IconName = "TowerMineLayerIcon", BaseCost = 250, Hotkey = Key.Digit7, ShortcutLabel = "7", Range = 4.0f },
            new TowerBuildInfo { Name = "Chain Lightning", IconName = "TowerChainIcon", BaseCost = 200, Hotkey = Key.Digit8, ShortcutLabel = "8", Range = 3.8f },
            new TowerBuildInfo { Name = "Beam Tower", IconName = "TowerBeamIcon", BaseCost = 225, Hotkey = Key.Digit9, ShortcutLabel = "9", Range = 4.0f },
            new TowerBuildInfo { Name = "Swarm Nest", IconName = "TowerOrbitIcon", BaseCost = 175, Hotkey = Key.Digit0, ShortcutLabel = "0", Range = 2.5f },
            // The close-range castes (GrayboxMeleeTower). Ranges match GrayboxMeleeTower.StatsFor.
            new TowerBuildInfo { Name = "Worker", IconName = "TowerWorkerIcon", BaseCost = 75, Hotkey = Key.Q, ShortcutLabel = "Q", Range = 1.8f },
            new TowerBuildInfo { Name = "Soldier", IconName = "TowerSoldierIcon", BaseCost = 125, Hotkey = Key.W, ShortcutLabel = "W", Range = 2.0f },
            new TowerBuildInfo { Name = "Major", IconName = "TowerMajorIcon", BaseCost = 200, Hotkey = Key.E, ShortcutLabel = "E", Range = 2.0f },
            new TowerBuildInfo { Name = "Nurse", IconName = "TowerNurseIcon", BaseCost = 150, Hotkey = Key.Y, ShortcutLabel = "Y", Range = 0.5f },
        };

        public Tower SelectedTower => _selectedTower;

        /// <summary>True while a tower is selected, the build menu is open, or a tower is being placed.</summary>
        public bool IsBusy => _selectedTower != null || _showBuildMenu || _activePlacementTypeIndex >= 0;

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
                for (int i = 0; i < TowerCatalog.Length; i++)
                {
                    if (keyboard[TowerCatalog[i].Hotkey].wasPressedThisFrame)
                    {
                        if (_activePlacementTypeIndex == i)
                        {
                            _activePlacementTypeIndex = -1; // Toggle off if already selected
                        }
                        else
                        {
                            _activePlacementTypeIndex = i;
                            _selectedTower = null;
                            _showBuildMenu = false;
                        }
                    }
                }

                if (keyboard.escapeKey.wasPressedThisFrame)
                {
                    _selectedTower = null;
                    _showBuildMenu = false;
                    _activePlacementTypeIndex = -1;
                }
            }

            if (mouse != null && mouse.rightButton.wasPressedThisFrame)
            {
                _activePlacementTypeIndex = -1;
                _selectedTower = null;
                _showBuildMenu = false;
            }

            // Detect mouse clicks on 2D world
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                Vector3 mouseScreen = mouse.position.ReadValue();
                float guiY = Screen.height - mouseScreen.y;

                // Ignore click if inside bottom build bar area (Screen.height - 90px down)
                if (guiY >= Screen.height - 90f)
                {
                    return;
                }

                // Ignore click if over right-side Inspector Card area
                if (_selectedTower != null && mouseScreen.x >= Screen.width - 280f && guiY <= 350f)
                {
                    return;
                }

                Vector3 worldPoint = _mainCamera.ScreenToWorldPoint(mouseScreen);
                worldPoint.z = 0f;

                // If currently in active placement mode from the bottom build bar
                if (_activePlacementTypeIndex >= 0 && _activePlacementTypeIndex < TowerCatalog.Length)
                {
                    Vector3 buildPos = SnapToGrid(worldPoint);
                    if (IsPositionClearOfPath(buildPos, 1.2f) && FindTowerAt(buildPos, 0.8f) == null)
                    {
                        ExecuteBuildByIndex(_activePlacementTypeIndex, buildPos);
                    }
                    return;
                }

                Tower clickedTower = FindTowerAt(worldPoint, 0.8f);

                if (clickedTower != null)
                {
                    _selectedTower = clickedTower;
                    _showBuildMenu = false;
                    _activePlacementTypeIndex = -1;
                }
                else
                {
                    // Clicked empty ground
                    if (IsPositionClearOfPath(worldPoint, 1.2f))
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

        private Tower FindTowerAt(Vector3 point, float radius)
        {
            foreach (Tower t in FindObjectsByType<Tower>())
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
            if (_path == null)
            {
                _path = FindAnyObjectByType<EnemyPath>();
                if (_path == null)
                {
                    return true;
                }
            }

            IReadOnlyList<Vector3> waypoints = _path.Waypoints;
            if (waypoints == null || waypoints.Count < 2)
            {
                return true;
            }

            Vector2 p = point;
            for (int i = 0; i < waypoints.Count - 1; i++)
            {
                Vector2 a = waypoints[i];
                Vector2 b = waypoints[i + 1];
                Vector2 ab = b - a;
                float t = ab.sqrMagnitude > 0f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude) : 0f;
                Vector2 closest = a + ab * t;
                if (Vector2.Distance(p, closest) < minDistance)
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

        private void OnGUI()
        {
            if (!GrayboxGameFlow.GameplayActive) return;

            HoldTheHill.Sandbox.NillyCtrl.GrayboxUi.Apply(); // pixel skin; a no-op without a GrayboxUi in the scene
            InitStyles();

            DrawInspectorCard();
            DrawBuildMenu();
            DrawBottomBuildBar();
            DrawPlacementGhost();
        }

        private void InitStyles()
        {
            if (_cardHeaderStyle != null)
            {
                return;
            }

            _cardHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                richText = true
            };

            _cardBodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                richText = true
            };

            _btnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold
            };

            _barBtnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
        }

        private void DrawInspectorCard()
        {
            if (_selectedTower == null)
            {
                return;
            }

            float width = 260f;
            float height = 330f;
            float x = Screen.width - width - 16f;
            float y = 16f;

            Rect cardRect = new Rect(x, y, width, height);
            GUI.Box(cardRect, GUIContent.none);

            GUILayout.BeginArea(new Rect(x + 10f, y + 10f, width - 20f, height - 20f));

            GUILayout.Label($"<b><color=#80d0ff>{_selectedTower.name}</color></b>", _cardHeaderStyle);
            GUILayout.Space(4);

            // Priority Selector Buttons
            GUILayout.Label("Targeting Priority:", _cardBodyStyle);
            GUILayout.BeginHorizontal();

            TargetingPriority currentPrio = _selectedTower.Priority;
            foreach (TargetingPriority prio in System.Enum.GetValues(typeof(TargetingPriority)))
            {
                GUI.color = prio == currentPrio ? new Color(0.4f, 0.9f, 1f) : Color.white;
                Texture2D prioIcon = LoadIcon($"Target{prio}Icon");
                GUIContent prioContent = prioIcon != null ? new GUIContent(prioIcon, prio.ToString()) : new GUIContent(prio.ToString().Substring(0, 1));
                if (GUILayout.Button(prioContent, _btnStyle, GUILayout.Width(36), GUILayout.Height(24)))
                {
                    _selectedTower.Priority = prio;
                }
            }
            GUI.color = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            GUILayout.Label($"<b>Priority Active:</b> <color=#ff8080>{_selectedTower.Priority}</color>", _cardBodyStyle);
            GUILayout.Label($"<b>Range:</b> {_selectedTower.Range:0.0}m | <b>Level:</b> {_selectedTower.Level}/{_selectedTower.MaxLevel}", _cardBodyStyle);

            GUILayout.Space(6);
            GUILayout.Label("<b>Combat Performance:</b>", _cardBodyStyle);
            GUILayout.Label($"  Damage Dealt: <color=#7fdd7f>{_selectedTower.TotalDamageDealt:0}</color>", _cardBodyStyle);
            GUILayout.Label($"  Kills: <color=#7fdd7f>{_selectedTower.TotalKills}</color>", _cardBodyStyle);
            GUILayout.Label($"  Shots Fired: {_selectedTower.ShotsFired}", _cardBodyStyle);

            GUILayout.Space(6);
            GUILayout.Space(6);
            if (_selectedTower.CanUpgrade)
            {
                int baseUpgradeCost = _selectedTower.Level == 1 ? 150 : 300;
                int upgradeCost = GetDiscountedCost(baseUpgradeCost);
                bool canAfford = GrayboxEconomy.Instance == null || GrayboxEconomy.Instance.CanAfford(upgradeCost);

                GUI.color = canAfford ? new Color(0.4f, 1f, 0.5f) : new Color(0.6f, 0.6f, 0.6f);
                Texture2D upgIcon = LoadIcon("UiUpgradeIcon");
                GUIContent upgContent = upgIcon != null
                    ? new GUIContent($" Upgrade (${upgradeCost} | Lvl {_selectedTower.Level} \u2192 {_selectedTower.Level + 1})", upgIcon)
                    : new GUIContent($"Upgrade (${upgradeCost} | Lvl {_selectedTower.Level} \u2192 {_selectedTower.Level + 1})");

                if (GUILayout.Button(upgContent, _btnStyle, GUILayout.Height(26)))
                {
                    if (GrayboxEconomy.Instance == null || GrayboxEconomy.Instance.TrySpendGold(upgradeCost))
                    {
                        _selectedTower.Upgrade();
                        _selectedTower.TotalGoldInvested += upgradeCost;
                        GrayboxSfx.PlayCue("Upgrade", _selectedTower.transform.position);
                    }
                }
                GUI.color = Color.white;
            }
            else
            {
                GUI.color = new Color(0.7f, 0.7f, 0.7f);
                GUILayout.Button("MAX LEVEL REACHED", _btnStyle, GUILayout.Height(26));
                GUI.color = Color.white;
            }

            GUILayout.Space(8);
            GUILayout.BeginHorizontal();
            Texture2D sellIcon = LoadIcon("UiSellIcon");
            GUIContent sellContent = sellIcon != null
                ? new GUIContent($" Dismantle (+${_selectedTower.RefundValue})", sellIcon)
                : new GUIContent($"Dismantle (+${_selectedTower.RefundValue})");

            if (GUILayout.Button(sellContent, _btnStyle, GUILayout.Height(26)))
            {
                GrayboxEconomy.Instance?.EarnGold(_selectedTower.RefundValue);
                GrayboxSfx.PlayCue("Sell", _selectedTower.transform.position);
                SpriteClipPlayer.SpawnOneShot(_animLibrary, "FxBuild", "Sell", _selectedTower.transform.position, Quaternion.identity, 1f, 3);
                Destroy(_selectedTower.gameObject);
                _selectedTower = null;
            }
            if (GUILayout.Button("Close Card", _btnStyle, GUILayout.Height(26)))
            {
                _selectedTower = null;
            }
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        private void DrawBuildMenu()
        {
            if (!_showBuildMenu)
            {
                return;
            }

            Vector3 screenPos = _mainCamera.WorldToScreenPoint(_buildWorldPos);
            float width = 230f;
            float height = 560f; // 14 options
            float x = Mathf.Clamp(screenPos.x - width * 0.5f, 10f, Screen.width - width - 10f);
            float y = Mathf.Clamp(Screen.height - screenPos.y - height * 0.5f, 10f, Screen.height - height - 10f);

            Rect menuRect = new Rect(x, y, width, height);
            GUI.Box(menuRect, GUIContent.none);

            GUILayout.BeginArea(new Rect(x + 8f, y + 8f, width - 16f, height - 16f));
            GUILayout.Label("<b>BUILD TOWER</b>", _cardHeaderStyle);
            GUILayout.Label($"Grid: ({_buildWorldPos.x}, {_buildWorldPos.y})", _cardBodyStyle);
            GUILayout.Space(4);

            DrawBuildOption("Bullet Tower", "TowerLinearIcon", 100, () => PlaceTower("Linear Bullet", _bulletPrefab, TargetingPriority.First, 3.6f, 0.45f, new Color(0.4f, 0.7f, 1f), GetDiscountedCost(100)));
            DrawBuildOption("Homing Tower", "TowerHomingIcon", 150, () => PlaceTower("Homing Missile", _homingPrefab, TargetingPriority.Closest, 3.8f, 0.9f, new Color(0.4f, 1f, 0.6f), GetDiscountedCost(150)));
            DrawBuildOption("Mortar Tower", "TowerMortarIcon", 200, () => PlaceTower("Mortar Shell", _mortarPrefab, TargetingPriority.First, 4.2f, 1.6f, new Color(0.9f, 0.5f, 0.2f), GetDiscountedCost(200)));
            DrawBuildOption("Ricochet Tower", "TowerRicochetIcon", 175, () => PlaceTower("Ricochet Disc", _ricochetPrefab, TargetingPriority.Strongest, 3.8f, 1f, new Color(0.6f, 0.8f, 1f), GetDiscountedCost(175)));
            DrawBuildOption("Frost Aura", "TowerFrostAuraIcon", 225, () => PlaceSpecialTower<FrostAuraTower>("Frost_Aura", 3.5f, new Color(0.4f, 0.85f, 1f), GetDiscountedCost(225)));
            DrawBuildOption("Knockback Pulse", "TowerKnockbackIcon", 200, () => PlaceSpecialTower<KnockbackTower>("Knockback_Pulse", 3.0f, new Color(1f, 0.6f, 0.2f), GetDiscountedCost(200)));
            DrawBuildOption("Mine Layer", "TowerMineLayerIcon", 250, () => PlaceMineLayerTower(GetDiscountedCost(250)));
            DrawBuildOption("Chain Lightning", "TowerChainIcon", 200, () => PlaceChainTower(GetDiscountedCost(200)));
            DrawBuildOption("Beam Tower", "TowerBeamIcon", 225, () => PlaceBeamTower(GetDiscountedCost(225)));
            DrawBuildOption("Swarm Nest", "TowerOrbitIcon", 175, () => PlaceOrbitTower(GetDiscountedCost(175)));
            DrawBuildOption("Worker", "TowerWorkerIcon", 75, () => PlaceMeleeTower(GrayboxMeleeTower.Caste.Worker, GetDiscountedCost(75)));
            DrawBuildOption("Soldier", "TowerSoldierIcon", 125, () => PlaceMeleeTower(GrayboxMeleeTower.Caste.Soldier, GetDiscountedCost(125)));
            DrawBuildOption("Major", "TowerMajorIcon", 200, () => PlaceMeleeTower(GrayboxMeleeTower.Caste.Major, GetDiscountedCost(200)));
            DrawBuildOption("Nurse", "TowerNurseIcon", 150, () => PlaceMeleeTower(GrayboxMeleeTower.Caste.Nurse, GetDiscountedCost(150)));

            if (GUILayout.Button("Cancel", _btnStyle))
            {
                _showBuildMenu = false;
            }

            GUILayout.EndArea();
        }

        private static readonly Color Ink = new Color32(0x1b, 0x11, 0x0b, 0xff);
        private GUIStyle _inkStyle;

        // Build bar made of HUD pieces: each tower sits in an icon slot with its hotkey on a keycap
        // and its price on a tag. Falls back to the plain button bar when the pieces are missing.
        private void DrawBottomBuildBar()
        {
            if (GrayboxIcons.Get("Slot") == null)
            {
                DrawBottomBuildBarLegacy();
                return;
            }

            _inkStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 10, alignment = TextAnchor.MiddleCenter, richText = false, wordWrap = false,
                padding = new RectOffset(0, 0, 0, 0), margin = new RectOffset(0, 0, 0, 0),
            };
            _inkStyle.normal.textColor = Ink;

            const float slot = 36f;
            const float cardWidth = 48f;
            const float cardHeight = 54f;
            int count = TowerCatalog.Length;
            float totalWidth = count * cardWidth + 16f;
            float totalHeight = cardHeight + 16f;
            float startX = (Screen.width - totalWidth) * 0.5f;
            float startY = Screen.height - totalHeight - 6f;

            GUI.Box(new Rect(startX, startY, totalWidth, totalHeight), GUIContent.none);

            for (int i = 0; i < count; i++)
            {
                TowerBuildInfo info = TowerCatalog[i];
                int cost = GetDiscountedCost(info.BaseCost);
                bool canAfford = GrayboxEconomy.Instance == null || GrayboxEconomy.Instance.CanAfford(cost);
                bool isSelected = _activePlacementTypeIndex == i;

                float cx = startX + 8f + i * cardWidth;
                float cy = startY + 10f;
                var slotRect = new Rect(cx + 8f, cy, slot, slot);

                Texture2D frame = GrayboxIcons.Get(isSelected ? "SlotSelected" : (canAfford ? "Slot" : "SlotDisabled"));
                GUI.DrawTexture(slotRect, frame);

                Texture2D icon = LoadIcon(info.IconName);
                if (icon != null)
                {
                    GUI.color = canAfford ? Color.white : new Color(1f, 1f, 1f, 0.45f);
                    GUI.DrawTexture(new Rect(slotRect.x + 2f, slotRect.y + 2f, 32f, 32f), icon);
                    GUI.color = Color.white;
                }

                var keyRect = new Rect(cx + 2f, cy - 4f, 12f, 13f);
                GrayboxIcons.DrawSliced(keyRect, "Keycap", 4, 4, 4, 5);
                GUI.Label(new Rect(keyRect.x, keyRect.y, 12f, 10f), info.ShortcutLabel, _inkStyle);

                var tagRect = new Rect(cx + 5f, cy + slot + 3f, 40f, 12f);
                GrayboxIcons.DrawSliced(tagRect, canAfford ? "CostTag" : "CostTagCant", 8, 3, 3, 3);
                GUI.Label(new Rect(tagRect.x + 8f, tagRect.y, 30f, 12f), cost.ToString(), _inkStyle);

                if (GUI.Button(new Rect(cx, cy - 4f, cardWidth, cardHeight + 4f), new GUIContent(string.Empty, info.Name), GUIStyle.none))
                {
                    if (_activePlacementTypeIndex == i)
                    {
                        _activePlacementTypeIndex = -1;
                    }
                    else
                    {
                        _activePlacementTypeIndex = i;
                        _selectedTower = null;
                        _showBuildMenu = false;
                    }
                }
            }
        }

        private void DrawBottomBuildBarLegacy()
        {
            float cardWidth = 70f;
            float cardHeight = 62f;
            float gap = 5f;
            int count = TowerCatalog.Length;
            float totalWidth = count * cardWidth + (count - 1) * gap + 16f;
            float totalHeight = cardHeight + 16f;

            float startX = (Screen.width - totalWidth) * 0.5f;
            float startY = Screen.height - totalHeight - 6f;

            Rect barRect = new Rect(startX, startY, totalWidth, totalHeight);
            GUI.Box(barRect, GUIContent.none);

            GUILayout.BeginArea(new Rect(startX + 8f, startY + 8f, totalWidth - 16f, cardHeight));
            GUILayout.BeginHorizontal();

            for (int i = 0; i < count; i++)
            {
                TowerBuildInfo info = TowerCatalog[i];
                int cost = GetDiscountedCost(info.BaseCost);
                bool canAfford = GrayboxEconomy.Instance == null || GrayboxEconomy.Instance.CanAfford(cost);
                bool isSelected = _activePlacementTypeIndex == i;

                if (isSelected)
                {
                    GUI.color = new Color(0.3f, 0.95f, 1.0f, 1.0f);
                }
                else if (!canAfford)
                {
                    GUI.color = new Color(0.5f, 0.5f, 0.5f, 0.65f);
                }
                else
                {
                    GUI.color = Color.white;
                }

                Texture2D icon = LoadIcon(info.IconName);
                GUIContent btnContent = icon != null
                    ? new GUIContent($"[{info.ShortcutLabel}]\n${cost}", icon, info.Name)
                    : new GUIContent($"[{info.ShortcutLabel}]\n${cost}", info.Name);

                if (GUILayout.Button(btnContent, _barBtnStyle, GUILayout.Width(cardWidth), GUILayout.Height(cardHeight)))
                {
                    if (_activePlacementTypeIndex == i)
                    {
                        _activePlacementTypeIndex = -1;
                    }
                    else
                    {
                        _activePlacementTypeIndex = i;
                        _selectedTower = null;
                        _showBuildMenu = false;
                    }
                }
            }
            GUI.color = Color.white;
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawPlacementGhost()
        {
            if (_activePlacementTypeIndex < 0 || _activePlacementTypeIndex >= TowerCatalog.Length || _mainCamera == null)
            {
                return;
            }

            Mouse mouse = Mouse.current;
            if (mouse == null) return;

            Vector3 mouseScreen = mouse.position.ReadValue();
            float guiY = Screen.height - mouseScreen.y;
            if (guiY >= Screen.height - 90f) return;

            Vector3 worldPoint = _mainCamera.ScreenToWorldPoint(mouseScreen);
            worldPoint.z = 0f;
            Vector3 buildPos = SnapToGrid(worldPoint);

            TowerBuildInfo info = TowerCatalog[_activePlacementTypeIndex];
            int cost = GetDiscountedCost(info.BaseCost);
            bool canAfford = GrayboxEconomy.Instance == null || GrayboxEconomy.Instance.CanAfford(cost);
            bool isClear = IsPositionClearOfPath(buildPos, 1.2f) && FindTowerAt(buildPos, 0.8f) == null;

            Vector3 screenBuildPos = _mainCamera.WorldToScreenPoint(buildPos);
            float labelY = Screen.height - screenBuildPos.y - 25f;

            Color labelColor = (canAfford && isClear) ? new Color(0.4f, 1f, 0.4f) : new Color(1f, 0.35f, 0.35f);
            GUIStyle ghostStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = labelColor }
            };

            string statusMsg = !isClear ? "[BLOCKED]" : (!canAfford ? "[NEED GOLD]" : $"BUILD {info.Name.ToUpper()} (${cost})");
            GUI.Label(new Rect(screenBuildPos.x - 120f, labelY, 240f, 24f), statusMsg, ghostStyle);
        }

        // Sprite set for each TowerCatalog entry, in the same order, for the placement ghost.
        private static readonly string[] GhostKeys =
        {
            "TowerLinear", "TowerHoming", "TowerMortar", "TowerRicochet", "TowerFrostAura",
            "TowerKnockback", "TowerMineLayer", "TowerChain", "TowerBeam", "TowerOrbit",
            "TowerWorker", "TowerSoldier", "TowerMajor", "TowerNurse",
        };

        private const int GhostRingSegments = 48;
        private SpriteRenderer _ghost;
        private LineRenderer _ghostRing;

        /// <summary>
        /// While a build-bar tower is selected, shows that tower under the cursor, snapped to the
        /// grid, with its range ring: green where it can be built, red where it can't.
        /// </summary>
        private void UpdateGhost(Mouse mouse)
        {
            int index = _activePlacementTypeIndex;
            bool active = index >= 0 && index < TowerCatalog.Length && index < GhostKeys.Length
                          && _animLibrary != null && mouse != null;
            if (active)
            {
                Vector3 screen = mouse.position.ReadValue();
                active = Screen.height - screen.y < Screen.height - 90f; // not over the build bar
            }

            if (!active)
            {
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

            TowerBuildInfo info = TowerCatalog[index];
            Vector3 world = _mainCamera.ScreenToWorldPoint(mouse.position.ReadValue());
            world.z = 0f;
            Vector3 buildPos = SnapToGrid(world);

            bool canAfford = GrayboxEconomy.Instance == null || GrayboxEconomy.Instance.CanAfford(GetDiscountedCost(info.BaseCost));
            bool isClear = IsPositionClearOfPath(buildPos, 1.2f) && FindTowerAt(buildPos, 0.8f) == null;
            Color tint = canAfford && isClear ? new Color(0.6f, 1f, 0.6f, 0.75f) : new Color(1f, 0.4f, 0.4f, 0.65f);

            SpriteAnimClip idle = _animLibrary.Find(GhostKeys[index])?.Find("Idle");
            _ghost.sprite = idle != null && idle.Frames.Length > 0 ? idle.Frames[0] : null;
            _ghost.color = tint;
            _ghost.transform.position = buildPos;
            _ghost.gameObject.SetActive(true);

            for (int i = 0; i < GhostRingSegments; i++)
            {
                float angle = i * Mathf.PI * 2f / GhostRingSegments;
                _ghostRing.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * info.Range);
            }

            _ghostRing.startColor = _ghostRing.endColor = tint;
        }

        private void ExecuteBuildByIndex(int index, Vector3 position)
        {
            if (index < 0 || index >= TowerCatalog.Length) return;

            TowerBuildInfo info = TowerCatalog[index];
            int cost = GetDiscountedCost(info.BaseCost);

            if (GrayboxEconomy.Instance != null && !GrayboxEconomy.Instance.TrySpendGold(cost))
            {
                return;
            }

            _buildWorldPos = position;

            switch (index)
            {
                case 0: PlaceTower("Linear Bullet", _bulletPrefab, TargetingPriority.First, 3.6f, 0.45f, new Color(0.4f, 0.7f, 1f), cost); break;
                case 1: PlaceTower("Homing Missile", _homingPrefab, TargetingPriority.Closest, 3.8f, 0.9f, new Color(0.4f, 1f, 0.6f), cost); break;
                case 2: PlaceTower("Mortar Shell", _mortarPrefab, TargetingPriority.First, 4.2f, 1.6f, new Color(0.9f, 0.5f, 0.2f), cost); break;
                case 3: PlaceTower("Ricochet Disc", _ricochetPrefab, TargetingPriority.Strongest, 3.8f, 1f, new Color(0.6f, 0.8f, 1f), cost); break;
                case 4: PlaceSpecialTower<FrostAuraTower>("Frost_Aura", 3.5f, new Color(0.4f, 0.85f, 1f), cost); break;
                case 5: PlaceSpecialTower<KnockbackTower>("Knockback_Pulse", 3.0f, new Color(1f, 0.6f, 0.2f), cost); break;
                case 6: PlaceMineLayerTower(cost); break;
                case 7: PlaceChainTower(cost); break;
                case 8: PlaceBeamTower(cost); break;
                case 9: PlaceOrbitTower(cost); break;
                case 10: PlaceMeleeTower(GrayboxMeleeTower.Caste.Worker, cost); break;
                case 11: PlaceMeleeTower(GrayboxMeleeTower.Caste.Soldier, cost); break;
                case 12: PlaceMeleeTower(GrayboxMeleeTower.Caste.Major, cost); break;
                case 13: PlaceMeleeTower(GrayboxMeleeTower.Caste.Nurse, cost); break;
            }
        }

        private static Texture2D LoadIcon(string iconName)
        {
            return GrayboxIcons.Get(iconName);
        }

        private int GetDiscountedCost(int baseCost)
        {
            float mult = GrayboxSkillTree.Instance != null ? GrayboxSkillTree.Instance.CostMultiplier : 1.0f;
            return Mathf.RoundToInt(baseCost * mult);
        }

        private void DrawBuildOption(string nameLabel, string iconName, int baseCost, System.Action onBuild)
        {
            int cost = GetDiscountedCost(baseCost);
            bool canAfford = GrayboxEconomy.Instance == null || GrayboxEconomy.Instance.CanAfford(cost);
            GUI.color = canAfford ? Color.white : new Color(0.6f, 0.6f, 0.6f, 0.7f);

            Texture2D icon = LoadIcon(iconName);
            GUIContent content = icon != null
                ? new GUIContent($" {nameLabel} (${cost})", icon)
                : new GUIContent($" {nameLabel} (${cost})");

            if (GUILayout.Button(content, _btnStyle, GUILayout.Height(24)))
            {
                if (GrayboxEconomy.Instance == null || GrayboxEconomy.Instance.TrySpendGold(cost))
                {
                    onBuild?.Invoke();
                    GrayboxSfx.PlayCue("Place", _buildWorldPos);
                }
            }
            GUI.color = Color.white;
        }

        private void PlaceSpecialTower<T>(string typeName, float range, Color color, int cost) where T : Component
        {
            Tower tower = CreateBaseTower(typeName, range, 1.5f, color, cost);
            tower.gameObject.AddComponent<T>();
            _showBuildMenu = false;
            _selectedTower = tower;
        }

        /// <summary>A Worker, Soldier, Major or Nurse: a plain Tower with a GrayboxMeleeTower on it.</summary>
        public Tower PlaceMeleeTower(GrayboxMeleeTower.Caste caste, int cost)
        {
            GrayboxMeleeTower.Stats stats = GrayboxMeleeTower.StatsFor(caste);
            Tower tower = CreateBaseTower(caste.ToString(), stats.Range, stats.Interval, new Color(0.75f, 0.55f, 0.35f), cost);
            tower.gameObject.AddComponent<GrayboxMeleeTower>().Configure(caste);
            _showBuildMenu = false;
            _selectedTower = tower;
            return tower;
        }

        private void PlaceMineLayerTower(int cost)
        {
            Tower tower = CreateBaseTower("MineLayer", 4.0f, 2.8f, new Color(0.9f, 0.9f, 0.3f), cost);
            MineLayerTower layer = tower.gameObject.AddComponent<MineLayerTower>();
            SetSerializedProperty(layer, "_minePrefab", _minePrefab);
            _showBuildMenu = false;
            _selectedTower = tower;
        }

        private Tower CreateBaseTower(string typeName, float range, float fireInterval, Color color, int cost = 100)
        {
            var go = new GameObject($"Tower_{typeName}");
            go.transform.position = _buildWorldPos;

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = _squareSprite != null ? _squareSprite : AssetDatabase_GetSquareSprite();
            renderer.color = color;
            renderer.sortingOrder = 1;
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = new Vector2(0.8f, 0.8f);

            Tower tower = go.AddComponent<Tower>();
            tower.TotalGoldInvested = cost;
            SetSerializedProperty(tower, "_range", range);
            SetSerializedProperty(tower, "_fireInterval", fireInterval);
            SetSerializedProperty(tower, "_priority", (int)TargetingPriority.Closest);
            go.AddComponent<TowerTargetVisualizer>();

            if (_animLibrary != null && AnimKeys.TryGetValue(typeName, out string animKey))
            {
                GrayboxTowerAnimator.Attach(go, _animLibrary, animKey);
            }

            GrayboxSfx.PlayCue("Place", go.transform.position);
            SpriteClipPlayer.SpawnOneShot(_animLibrary, "FxBuild", "Rise", go.transform.position, Quaternion.identity, 1f, 3);
            return tower;
        }

        // The next three use the same numbers as GrayboxBuilder's scene towers.
        private void PlaceChainTower(int cost)
        {
            Tower tower = CreateBaseTower("Chain_Lightning", 3.8f, 1.2f, new Color(0.5f, 0.85f, 1f), cost);
            SetSerializedProperty(tower, "_priority", (int)TargetingPriority.Weakest);
            ChainLightning chain = tower.gameObject.AddComponent<ChainLightning>();
            ConfigureLine(tower.GetComponent<LineRenderer>(), new Color(0.6f, 0.9f, 1f), 0.08f);
            GrayboxLineScroll.Dress(tower.GetComponent<LineRenderer>(), _boltMaterial, 0.25f, -6f, 1f);
            SetSerializedProperty(chain, "_maxTargets", 4);
            SetSerializedProperty(chain, "_jumpRange", 3.5f);
            SetSerializedProperty(chain, "_damage", 10f);
            SetSerializedProperty(tower, "_chainLightning", chain);
            _showBuildMenu = false;
            _selectedTower = tower;
        }

        private void PlaceBeamTower(int cost)
        {
            Tower tower = CreateBaseTower("Beam", 4f, 1f, new Color(1f, 0.5f, 0.3f), cost);
            ContinuousBeam beam = tower.gameObject.AddComponent<ContinuousBeam>();
            ConfigureLine(tower.GetComponent<LineRenderer>(), new Color(1f, 0.55f, 0.25f), 0.15f);
            GrayboxLineScroll.Dress(tower.GetComponent<LineRenderer>(), _beamMaterial, 0.25f, -3f, 0f);
            SetSerializedProperty(beam, "_range", 4f);
            SetSerializedProperty(beam, "_baseDamagePerSecond", 5f);
            SetSerializedProperty(beam, "_rampPerSecond", 5f);
            SetSerializedProperty(beam, "_maxDamagePerSecond", 25f);
            SetSerializedProperty(tower, "_continuousBeam", beam);
            _showBuildMenu = false;
            _selectedTower = tower;
        }

        private void PlaceOrbitTower(int cost)
        {
            Tower tower = CreateBaseTower("Orbit", 2.5f, 1f, new Color(0.7f, 0.6f, 1f), cost);
            OrbitingDamageField field = tower.gameObject.AddComponent<OrbitingDamageField>();
            // Set before the field's Start builds its orbiters.
            SetSerializedProperty(field, "_radius", 1.6f);
            SetSerializedProperty(field, "_orbiterCount", 3);
            SetSerializedProperty(field, "_angularSpeed", 150f);
            SetSerializedProperty(field, "_contactDamage", 6f);
            SetSerializedProperty(field, "_hitCooldown", 0.4f);
            SetSerializedProperty(field, "_orbiterRadius", 0.35f);
            _showBuildMenu = false;
            _selectedTower = tower;
        }

        private void ConfigureLine(LineRenderer line, Color color, float width)
        {
            if (line == null)
            {
                return;
            }

            line.material = _lineMaterial;
            line.startColor = color;
            line.endColor = color;
            line.startWidth = width;
            line.endWidth = width;
            line.numCapVertices = 2;
            line.sortingOrder = 4;
        }

        private void PlaceTower(string typeName, GameObject projectilePrefab, TargetingPriority priority, float range, float fireInterval, Color color, int cost = 100)
        {
            Tower tower = CreateBaseTower(typeName, range, fireInterval, color, cost);
            SetSerializedProperty(tower, "_priority", (int)priority);

            if (projectilePrefab != null)
            {
                SetSerializedProperty(tower, "_projectilePrefab", projectilePrefab.GetComponent<Projectile>());
            }

            _showBuildMenu = false;
            _selectedTower = tower;
        }

        private static Sprite AssetDatabase_GetSquareSprite()
        {
            return Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
        }

        private static void SetSerializedProperty(Object target, string fieldName, object value)
        {
            System.Reflection.FieldInfo field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(target, value);
            }
        }
    }
}
