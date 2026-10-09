using System.Collections.Generic;
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
        [Tooltip("The towers that can be built, in build bar order.")]
        [SerializeField] private GrayboxTowerCatalog _catalog;
        [Tooltip("Material for the placement ghost's range ring.")]
        [SerializeField] private Material _lineMaterial;
        [Tooltip("Holds the build and sell effects. Left empty, towers appear without them.")]
        [SerializeField] private SpriteAnimLibrary _animLibrary;

        private readonly List<Tower> _towers = new List<Tower>();
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

        private int TowerCount => _catalog != null ? _catalog.Count : 0;

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
                for (int i = 0; i < TowerCount; i++)
                {
                    if (GrayboxControls.Pressed(GrayboxControls.BuildId(i)))
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

                if (GrayboxControls.Pressed(GrayboxControls.Pause))
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
                if (_activePlacementTypeIndex >= 0 && _activePlacementTypeIndex < TowerCount)
                {
                    Vector3 buildPos = SnapToGrid(worldPoint);
                    if (IsPositionClearOfPath(buildPos, 1.2f) && FindTowerAt(buildPos, 0.8f) == null)
                    {
                        TryBuild(_catalog[_activePlacementTypeIndex], buildPos);
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
            float height = 84f + TowerCount * 34f;
            float x = Mathf.Clamp(screenPos.x - width * 0.5f, 10f, Screen.width - width - 10f);
            float y = Mathf.Clamp(Screen.height - screenPos.y - height * 0.5f, 10f, Screen.height - height - 10f);

            Rect menuRect = new Rect(x, y, width, height);
            GUI.Box(menuRect, GUIContent.none);

            GUILayout.BeginArea(new Rect(x + 8f, y + 8f, width - 16f, height - 16f));
            GUILayout.Label("<b>BUILD TOWER</b>", _cardHeaderStyle);
            GUILayout.Label($"Grid: ({_buildWorldPos.x}, {_buildWorldPos.y})", _cardBodyStyle);
            GUILayout.Space(4);

            for (int i = 0; i < TowerCount; i++)
            {
                DrawBuildOption(_catalog[i]);
            }

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
            int count = TowerCount;
            float totalWidth = count * cardWidth + 16f;
            float totalHeight = cardHeight + 16f;
            float startX = (Screen.width - totalWidth) * 0.5f;
            float startY = Screen.height - totalHeight - 6f;

            GUI.Box(new Rect(startX, startY, totalWidth, totalHeight), GUIContent.none);

            for (int i = 0; i < count; i++)
            {
                GrayboxTowerData info = _catalog[i];
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
                GUI.Label(new Rect(keyRect.x, keyRect.y, 12f, 10f), GrayboxControls.Name(GrayboxControls.BuildId(i)), _inkStyle);

                var tagRect = new Rect(cx + 5f, cy + slot + 3f, 40f, 12f);
                GrayboxIcons.DrawSliced(tagRect, canAfford ? "CostTag" : "CostTagCant", 8, 3, 3, 3);
                GUI.Label(new Rect(tagRect.x + 8f, tagRect.y, 30f, 12f), cost.ToString(), _inkStyle);

                if (GUI.Button(new Rect(cx, cy - 4f, cardWidth, cardHeight + 4f), new GUIContent(string.Empty, info.DisplayName), GUIStyle.none))
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
            int count = TowerCount;
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
                GrayboxTowerData info = _catalog[i];
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
                    ? new GUIContent($"[{GrayboxControls.Name(GrayboxControls.BuildId(i))}]\n${cost}", icon, info.DisplayName)
                    : new GUIContent($"[{GrayboxControls.Name(GrayboxControls.BuildId(i))}]\n${cost}", info.DisplayName);

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
            if (_activePlacementTypeIndex < 0 || _activePlacementTypeIndex >= TowerCount || _mainCamera == null)
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

            GrayboxTowerData info = _catalog[_activePlacementTypeIndex];
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

            string statusMsg = !isClear ? "[BLOCKED]" : (!canAfford ? "[NEED GOLD]" : $"BUILD {info.DisplayName.ToUpper()} (${cost})");
            GUI.Label(new Rect(screenBuildPos.x - 120f, labelY, 240f, 24f), statusMsg, ghostStyle);
        }

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
            bool active = index >= 0 && index < TowerCount && mouse != null;
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

            GrayboxTowerData info = _catalog[index];
            Vector3 world = _mainCamera.ScreenToWorldPoint(mouse.position.ReadValue());
            world.z = 0f;
            Vector3 buildPos = SnapToGrid(world);

            bool canAfford = GrayboxEconomy.Instance == null || GrayboxEconomy.Instance.CanAfford(GetDiscountedCost(info.BaseCost));
            bool isClear = IsPositionClearOfPath(buildPos, 1.2f) && FindTowerAt(buildPos, 0.8f) == null;
            Color tint = canAfford && isClear ? new Color(0.6f, 1f, 0.6f, 0.75f) : new Color(1f, 0.4f, 0.4f, 0.65f);

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
            if (GrayboxEconomy.Instance != null && !GrayboxEconomy.Instance.TrySpendGold(cost))
            {
                return;
            }

            Place(data, position, cost);
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

        private static Texture2D LoadIcon(string iconName)
        {
            return GrayboxIcons.Get(iconName);
        }

        private int GetDiscountedCost(int baseCost)
        {
            float mult = GrayboxSkillTree.Instance != null ? GrayboxSkillTree.Instance.CostMultiplier : 1.0f;
            return Mathf.RoundToInt(baseCost * mult);
        }

        private void DrawBuildOption(GrayboxTowerData data)
        {
            int cost = GetDiscountedCost(data.BaseCost);
            bool canAfford = GrayboxEconomy.Instance == null || GrayboxEconomy.Instance.CanAfford(cost);
            GUI.color = canAfford ? Color.white : new Color(0.6f, 0.6f, 0.6f, 0.7f);

            Texture2D icon = LoadIcon(data.IconName);
            GUIContent content = icon != null
                ? new GUIContent($" {data.DisplayName} (${cost})", icon)
                : new GUIContent($" {data.DisplayName} (${cost})");

            if (GUILayout.Button(content, _btnStyle, GUILayout.Height(24)))
            {
                TryBuild(data, _buildWorldPos);
            }
            GUI.color = Color.white;
        }
    }
}
