using HoldTheHill.Sandbox.UiKit;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Builds the in-game HUD as a prefab (a variant of the UI kit's UiRoot canvas) from small part
    /// prefabs: a build slot, a build option, a wave marker and a chip. Called from
    /// <see cref="GrayboxBuilder"/> and from Tools > Hold the Hill > Build Graybox HUD.
    /// </summary>
    /// <remarks>
    /// Everything is regenerated on each build, so layout is changed here rather than by hand on
    /// the prefab. Needs the UI kit prefabs (Tools > Hold the Hill > Build UI Kit).
    /// </remarks>
    public static class GrayboxHudBuilder
    {
        public const string PrefabPath = GrayboxRoot + "/Prefabs/GrayboxHud.prefab";

        private const string GrayboxRoot = "Assets/_Sandbox/nilly-ctrl/Graybox";
        private const string PartsFolder = "Hud";
        private const string HudSprites = "Assets/_Sandbox/nilly-ctrl/Ui/Hud";
        private const string KitSprites = UiKitBuilder.Root + "/Sprites";
        private const string IconSprites = "Assets/_Sandbox/nilly-ctrl/Icons/PNG";

        // Below the menus, which sit at 100.
        private const int SortingOrder = 90;

        private static readonly Color Ink = new Color32(0x1b, 0x11, 0x0b, 0xff);

        private static TMP_FontAsset s_font;

        // Columns sit this far in from the screen edge, in canvas units.
        private const float Edge = 6f;

        [MenuItem("Tools/Hold the Hill/Build Graybox HUD")]
        public static GameObject BuildPrefab()
        {
            GrayboxBuilder.CreateFolderIfMissing(GrayboxRoot, "Prefabs");
            GrayboxBuilder.CreateFolderIfMissing(GrayboxRoot + "/Prefabs", PartsFolder);

            GrayboxBuildSlot slot = BuildSlot();
            GrayboxBuildOption option = BuildOption();
            Image marker = BuildWaveMarker();
            GrayboxHudChip chip = BuildChip();

            GameObject root = UiKitBuilder.InstantiatePrefab("UiRoot", null);
            if (root == null)
            {
                return null;
            }

            root.name = "GrayboxHud";
            root.GetComponent<Canvas>().sortingOrder = SortingOrder;
            root.AddComponent<GrayboxHudRoot>();
            Transform canvas = root.transform;

            // Order is draw order: the menu and the label come last so they sit over the rest.
            BuildLeftColumn(canvas);
            BuildHealthBar(canvas);
            BuildWaveTrack(canvas, marker);
            BuildRightColumn(canvas);
            BuildWardenStrip(canvas, chip);
            BuildBuildBar(canvas, slot);
            BuildBuildMenu(canvas, option);
            BuildPlacementLabel(canvas);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        /// <summary>Puts the HUD, and an event system if the scene has none, into the open scene.</summary>
        public static void AddToScene()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                prefab = BuildPrefab();
            }

            if (prefab == null)
            {
                Debug.LogError("[Graybox] No HUD: build the UI kit first (Tools > Hold the Hill > Build UI Kit).");
                return;
            }

            if (Object.FindAnyObjectByType<EventSystem>() == null)
            {
                UiKitBuilder.InstantiatePrefab("UiEventSystem", null);
            }

            PrefabUtility.InstantiatePrefab(prefab);
        }

        // ---------- part prefabs ----------

        // One tower on the build bar: icon in a slot frame, hotkey on a keycap, price on a tag.
        private static GrayboxBuildSlot BuildSlot()
        {
            RectTransform root = NewRect("HudBuildSlot", null);
            root.sizeDelta = new Vector2(48f, 58f);
            Layout(root, 48f, 58f);

            // The whole cell takes the click; nothing inside it does.
            Image hit = root.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = hit;
            button.transition = Selectable.Transition.None;

            RectTransform frame = NewRect("Frame", root);
            Corner(frame, new Vector2(8f, -4f), new Vector2(36f, 36f));
            Image frameImage = AddImage(frame, Hud("Slot"));

            RectTransform icon = NewRect("Icon", frame);
            Centre(icon, new Vector2(32f, 32f));
            Image iconImage = AddImage(icon, null);

            RectTransform keycap = NewRect("Keycap", root);
            Corner(keycap, new Vector2(2f, 0f), new Vector2(12f, 13f));
            AddImage(keycap, Hud("Keycap"));
            TextMeshProUGUI key = AddText(keycap, "Key", "1", TextAlignmentOptions.Top, Ink);
            Stretch(key.rectTransform, 0f, 0f, 0f, 1f);

            RectTransform tag = NewRect("CostTag", root);
            Corner(tag, new Vector2(5f, -43f), new Vector2(40f, 12f));
            Image tagImage = AddImage(tag, Hud("CostTag"));
            TextMeshProUGUI cost = AddText(tag, "Cost", "100", TextAlignmentOptions.Center, Ink);
            Stretch(cost.rectTransform, 8f, 0f, 0f, 0f);

            var slot = root.gameObject.AddComponent<GrayboxBuildSlot>();
            UiKitBuilder.Wire(slot, "_button", button);
            UiKitBuilder.Wire(slot, "_frame", frameImage);
            UiKitBuilder.Wire(slot, "_icon", iconImage);
            UiKitBuilder.Wire(slot, "_key", key);
            UiKitBuilder.Wire(slot, "_cost", cost);
            UiKitBuilder.Wire(slot, "_costTag", tagImage);
            UiKitBuilder.Wire(slot, "_frameNormal", Hud("Slot"));
            UiKitBuilder.Wire(slot, "_frameSelected", Hud("SlotSelected"));
            UiKitBuilder.Wire(slot, "_frameDisabled", Hud("SlotDisabled"));
            UiKitBuilder.Wire(slot, "_tagAffordable", Hud("CostTag"));
            UiKitBuilder.Wire(slot, "_tagTooDear", Hud("CostTagCant"));

            return SavePart<GrayboxBuildSlot>(root.gameObject, "HudBuildSlot");
        }

        // One tower in the build menu: a kit button, taller than usual, with the icon on the left.
        private static GrayboxBuildOption BuildOption()
        {
            GameObject root = UiKitBuilder.InstantiatePrefab("Button", null);
            root.name = "HudBuildOption";

            var layout = root.GetComponent<LayoutElement>();
            layout.minHeight = layout.preferredHeight = 34f;
            ((RectTransform)root.transform).sizeDelta = new Vector2(112f, 34f);

            Transform label = root.transform.Find("Label");
            var labelRect = (RectTransform)label;
            labelRect.offsetMin = new Vector2(38f, 0f);
            var labelText = label.GetComponent<TextMeshProUGUI>();
            labelText.alignment = TextAlignmentOptions.Left;

            RectTransform icon = NewRect("Icon", root.transform);
            Corner(icon, new Vector2(3f, -1f), new Vector2(32f, 32f));
            Image iconImage = AddImage(icon, null);

            var option = root.AddComponent<GrayboxBuildOption>();
            UiKitBuilder.Wire(option, "_button", root.GetComponent<UiButton>());
            UiKitBuilder.Wire(option, "_icon", iconImage);
            return SavePart<GrayboxBuildOption>(root, "HudBuildOption");
        }

        // A flag on the wave track. Sits on the track's top edge, centred on its wave.
        private static Image BuildWaveMarker()
        {
            RectTransform root = NewRect("HudWaveMarker", null);
            root.anchorMin = root.anchorMax = new Vector2(0f, 1f);
            root.pivot = new Vector2(0.5f, 0f);
            root.anchoredPosition = new Vector2(0f, -1f);
            Image image = AddImage(root, Hud("WaveMarker"));
            image.raycastTarget = false;
            FitToSprite(image);
            return SavePart<Image>(root.gameObject, "HudWaveMarker");
        }

        // A small framed label for the Warden strip.
        private static GrayboxHudChip BuildChip()
        {
            RectTransform root = NewRect("HudChip", null);
            AddImage(root, Kit("Well")).raycastTarget = false;

            var layout = root.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(4, 4, 2, 2);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var fitter = root.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            TextMeshProUGUI text = AddText(root, "Text", "Chip", TextAlignmentOptions.Left, UiKitStyle.Cream);
            var chip = root.gameObject.AddComponent<GrayboxHudChip>();
            UiKitBuilder.Wire(chip, "_text", text);
            return SavePart<GrayboxHudChip>(root.gameObject, "HudChip");
        }

        // ---------- panels ----------

        // Stats and the horde buttons, stacked down the left edge.
        private static void BuildLeftColumn(Transform canvas)
        {
            RectTransform column = Column("LeftColumn", canvas, new Vector2(0f, 1f), new Vector2(Edge, -Edge));

            // The tuning readout.
            RectTransform statsPanel = HudPanel(column, "StatsPanel", 124f);
            TextMeshProUGUI title = Line(statsPanel, "GRAYBOX COMBAT TEST");
            title.color = UiKitStyle.Gold;
            TextMeshProUGUI food = Line(statsPanel, "$500  1/4");
            TextMeshProUGUI skills = Line(statsPanel, "Skills  0 SP");
            TextMeshProUGUI state = Line(statsPanel, "State  Idle");
            UiKitBuilder.AddDivider(statsPanel);
            TextMeshProUGUI spawned = Line(statsPanel, "Spawned  0");
            TextMeshProUGUI killed = Line(statsPanel, "Killed  0");
            TextMeshProUGUI leaked = Line(statsPanel, "Leaked  0");
            TextMeshProUGUI alive = Line(statsPanel, "Alive  0");
            UiKitBuilder.AddDivider(statsPanel);
            TextMeshProUGUI time = Line(statsPanel, "Time  0s  (x1)");
            TextMeshProUGUI keys = Line(statsPanel, "N next wave");
            keys.color = UiKitStyle.Dim;

            var stats = statsPanel.gameObject.AddComponent<GrayboxStatsPanel>();
            UiKitBuilder.Wire(stats, "_food", food);
            UiKitBuilder.Wire(stats, "_skills", skills);
            UiKitBuilder.Wire(stats, "_state", state);
            UiKitBuilder.Wire(stats, "_spawned", spawned);
            UiKitBuilder.Wire(stats, "_killed", killed);
            UiKitBuilder.Wire(stats, "_leaked", leaked);
            UiKitBuilder.Wire(stats, "_alive", alive);
            UiKitBuilder.Wire(stats, "_time", time);
            UiKitBuilder.Wire(stats, "_keys", keys);

            // Preset hordes.
            RectTransform hordePanel = HudPanel(column, "HordePanel", 124f);
            TextMeshProUGUI hordeTitle = Line(hordePanel, "HORDE SPAWNER");
            hordeTitle.color = UiKitStyle.Gold;
            var horde = hordePanel.gameObject.AddComponent<GrayboxHordePanel>();
            UiKitBuilder.Wire(horde, "_swarmRush", UiKitBuilder.AddButton(hordePanel, "Swarm rush"));
            UiKitBuilder.Wire(horde, "_bruteParade", UiKitBuilder.AddButton(hordePanel, "Brute parade"));
            UiKitBuilder.Wire(horde, "_phalanx", UiKitBuilder.AddButton(hordePanel, "Phalanx"));
            UiKitBuilder.Wire(horde, "_hydraSplitters", UiKitBuilder.AddButton(hordePanel, "Hydra splitters"));
            UiKitBuilder.Wire(horde, "_megaHorde", UiKitBuilder.AddButton(hordePanel, "Mega horde"));
        }

        // The hill's health, top centre.
        private static void BuildHealthBar(Transform canvas)
        {
            const float width = 200f;
            RectTransform bar = NewRect("HealthBar", canvas);
            Anchor(bar, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -Edge), new Vector2(width, 24f));

            TextMeshProUGUI label = AddText(bar, "Label", "HILL BASE HP: 100 / 100", TextAlignmentOptions.Top, UiKitStyle.Cream);
            Anchor(label.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 10f));

            RectTransform frame = NewRect("Frame", bar);
            Anchor(frame, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(0f, 10f));
            AddImage(frame, Hud("BarFrame")).raycastTarget = false;

            RectTransform well = NewRect("Well", frame);
            Stretch(well, 3f, 3f, 3f, 3f);

            RectTransform fill = NewRect("Fill", well);
            Stretch(fill, 0f, 0f, 0f, 0f);
            Image fillImage = AddImage(fill, Hud("BarFillHealth"));
            fillImage.raycastTarget = false;

            // Quarter marks.
            for (int i = 1; i < 4; i++)
            {
                RectTransform tick = NewRect("Tick" + i, well);
                Anchor(tick, new Vector2(i / 4f, 0.5f), new Vector2(i / 4f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1f, 4f));
                AddImage(tick, Hud("BarTick")).raycastTarget = false;
            }

            RectTransform heart = NewRect("Heart", bar);
            Anchor(heart, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(-4f, -8f), new Vector2(16f, 16f));
            AddImage(heart, Icon("ResQueenHealthIcon")).raycastTarget = false;

            var health = bar.gameObject.AddComponent<GrayboxBaseHealthBar>();
            UiKitBuilder.Wire(health, "_fill", fill);
            UiKitBuilder.Wire(health, "_fillImage", fillImage);
            UiKitBuilder.Wire(health, "_label", label);
            UiKitBuilder.Wire(health, "_healthy", Hud("BarFillHealth"));
            UiKitBuilder.Wire(health, "_hurt", Hud("BarFillFood"));
            UiKitBuilder.Wire(health, "_danger", Hud("BarFillDanger"));
        }

        // Wave progress under the health bar.
        private static void BuildWaveTrack(Transform canvas, Image marker)
        {
            const float width = 200f;
            RectTransform track = NewRect("WaveTrack", canvas);
            Anchor(track, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(width, 6f));
            AddImage(track, Hud("WaveTrack")).raycastTarget = false;

            RectTransform fill = NewRect("Fill", track);
            Anchor(fill, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(2f, 0f), new Vector2(0f, 2f));
            AddImage(fill, Hud("WaveFill")).raycastTarget = false;

            RectTransform now = NewRect("Now", track);
            Anchor(now, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0.5f, 1f), new Vector2(2f, 0f), new Vector2(7f, 7f));
            AddImage(now, Hud("WaveMarkerNow")).raycastTarget = false;

            var wave = track.gameObject.AddComponent<GrayboxWaveTrack>();
            UiKitBuilder.Wire(wave, "_track", track);
            UiKitBuilder.Wire(wave, "_fill", fill);
            UiKitBuilder.Wire(wave, "_now", now);
            UiKitBuilder.Wire(wave, "_markerPrefab", marker);
            UiKitBuilder.Wire(wave, "_upcoming", Hud("WaveMarker"));
            UiKitBuilder.Wire(wave, "_cleared", Hud("WaveMarkerDone"));
            UiKitBuilder.Wire(wave, "_boss", Hud("WaveMarkerBoss"));
        }

        // The selected tower's card, down the right edge below the pixel readouts.
        private static void BuildRightColumn(Transform canvas)
        {
            RectTransform column = Column("RightColumn", canvas, new Vector2(1f, 1f), new Vector2(-Edge, -112f));

            RectTransform panel = HudPanel(column, "TowerCard", 164f);
            TextMeshProUGUI title = Line(panel, "Tower");
            title.color = new Color32(0x80, 0xd0, 0xff, 0xff);
            UiStepper priority = UiKitBuilder.AddStepper(panel, "Target", new[] { "First" }, 0);
            TextMeshProUGUI level = Line(panel, "Range 3.0m  Level 1/3");
            TextMeshProUGUI damage = Line(panel, "Damage dealt  0");
            TextMeshProUGUI record = Line(panel, "Kills 0   Shots 0");
            UiKitBuilder.AddDivider(panel);
            UiButton upgrade = UiKitBuilder.AddButton(panel, "Upgrade");
            RectTransform row = UiKitBuilder.AddButtonRow(panel);
            UiButton sell = UiKitBuilder.AddButton(row, "Dismantle");
            UiButton close = UiKitBuilder.AddButton(row, "Close");

            var card = panel.gameObject.AddComponent<GrayboxTowerCard>();
            UiKitBuilder.Wire(card, "_title", title);
            UiKitBuilder.Wire(card, "_priority", priority);
            UiKitBuilder.Wire(card, "_levelLine", level);
            UiKitBuilder.Wire(card, "_damage", damage);
            UiKitBuilder.Wire(card, "_record", record);
            UiKitBuilder.Wire(card, "_upgrade", upgrade);
            UiKitBuilder.Wire(card, "_sell", sell);
            UiKitBuilder.Wire(card, "_close", close);
        }

        // The Warden's health, abilities and running power-ups, just above the build bar.
        private static void BuildWardenStrip(Transform canvas, GrayboxHudChip chip)
        {
            RectTransform strip = NewRect("WardenStrip", canvas);
            Anchor(strip, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 78f), Vector2.zero);

            var layout = strip.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 3f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var fitter = strip.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var warden = strip.gameObject.AddComponent<GrayboxWardenPanel>();
            UiKitBuilder.Wire(warden, "_chipPrefab", chip);
            UiKitBuilder.Wire(warden, "_chipRoot", strip);
        }

        // The towers on offer, along the bottom edge. The slots are made at run time from the catalog.
        private static void BuildBuildBar(Transform canvas, GrayboxBuildSlot slot)
        {
            RectTransform bar = NewRect("BuildBar", canvas);
            Anchor(bar, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 4f), Vector2.zero);
            AddImage(bar, Kit("Panel"));

            var layout = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(6, 6, 6, 6);
            layout.spacing = 0f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var fitter = bar.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var buildBar = bar.gameObject.AddComponent<GrayboxBuildBar>();
            UiKitBuilder.Wire(buildBar, "_slotPrefab", slot);
            UiKitBuilder.Wire(buildBar, "_slotRoot", bar);
        }

        // Opens beside an empty cell. Two columns of towers, so a long catalog still fits the screen.
        private static void BuildBuildMenu(Transform canvas, GrayboxBuildOption option)
        {
            RectTransform panel = UiKitBuilder.AddPanel(canvas, 240f);
            panel.name = "BuildMenu";

            TextMeshProUGUI title = Line(panel, "BUILD TOWER");
            title.color = UiKitStyle.Gold;
            TextMeshProUGUI cell = Line(panel, "Grid (0, 0)");
            cell.color = UiKitStyle.Dim;

            RectTransform options = NewRect("Options", panel);
            var grid = options.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(112f, 34f);
            grid.spacing = new Vector2(2f, 2f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.childAlignment = TextAnchor.UpperCenter;

            UiButton cancel = UiKitBuilder.AddButton(panel, "Cancel");

            var menu = panel.gameObject.AddComponent<GrayboxBuildMenu>();
            UiKitBuilder.Wire(menu, "_cell", cell);
            UiKitBuilder.Wire(menu, "_cancel", cancel);
            UiKitBuilder.Wire(menu, "_optionPrefab", option);
            UiKitBuilder.Wire(menu, "_optionRoot", options);
            UiKitBuilder.Wire(menu, "_frame", panel);
        }

        // "BUILD DRONE $120" over the ghost.
        private static void BuildPlacementLabel(Transform canvas)
        {
            RectTransform root = NewRect("PlacementLabel", canvas);
            Anchor(root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(160f, 12f));
            TextMeshProUGUI text = AddText(root, "Text", "BUILD", TextAlignmentOptions.Center, UiKitStyle.Cream);
            Stretch(text.rectTransform, 0f, 0f, 0f, 0f);

            var label = root.gameObject.AddComponent<GrayboxPlacementLabel>();
            UiKitBuilder.Wire(label, "_text", text);
        }

        // ---------- helpers ----------

        // A column that stacks its panels from a corner of the screen and shrinks to fit.
        private static RectTransform Column(string name, Transform parent, Vector2 corner, Vector2 offset)
        {
            RectTransform column = NewRect(name, parent);
            Anchor(column, corner, corner, corner, offset, Vector2.zero);

            var layout = column.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = UiKitStyle.Spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var fitter = column.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return column;
        }

        // A kit panel of a fixed width inside a column.
        private static RectTransform HudPanel(Transform parent, string name, float width)
        {
            var panel = (RectTransform)UiKitBuilder.InstantiatePrefab("Panel", parent).transform;
            panel.name = name;
            var layout = panel.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = width;
            return panel;
        }

        // One line of text in a panel, left aligned.
        private static TextMeshProUGUI Line(Transform panel, string text)
        {
            TextMeshProUGUI line = UiKitBuilder.AddBody(panel, text);
            line.alignment = TextAlignmentOptions.Left;
            return line;
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = LayerMask.NameToLayer("UI") };
            var rect = (RectTransform)go.transform;
            if (parent != null)
            {
                rect.SetParent(parent, false);
            }

            return rect;
        }

        private static void Anchor(RectTransform rect, Vector2 min, Vector2 max, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        // Pinned to the top left of its parent by its own top left corner.
        private static void Corner(RectTransform rect, Vector2 position, Vector2 size)
        {
            Anchor(rect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), position, size);
        }

        private static void Centre(RectTransform rect, Vector2 size)
        {
            Anchor(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
        }

        private static void Stretch(RectTransform rect, float left, float bottom, float right, float top)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void Layout(RectTransform rect, float width, float height)
        {
            var layout = rect.gameObject.AddComponent<LayoutElement>();
            layout.minWidth = layout.preferredWidth = width;
            layout.minHeight = layout.preferredHeight = height;
        }

        // Does not take clicks unless the caller turns that back on.
        private static Image AddImage(RectTransform rect, Sprite sprite)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = sprite != null && sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            image.raycastTarget = false;

            // The HUD art is imported at 32 pixels per unit for the world; here one art pixel is one canvas unit.
            if (sprite != null)
            {
                image.pixelsPerUnitMultiplier = 100f / sprite.pixelsPerUnit;
            }

            return image;
        }

        // Draws the sprite at its own pixel size, one art pixel to a canvas unit.
        private static void FitToSprite(Image image)
        {
            image.rectTransform.sizeDelta = image.sprite.rect.size;
        }

        private static TextMeshProUGUI AddText(Transform parent, string name, string text, TextAlignmentOptions alignment, Color colour)
        {
            RectTransform rect = NewRect(name, parent);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = Font();
            label.fontSize = UiKitStyle.BodySize;
            label.color = colour;
            label.alignment = alignment;
            label.text = text;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            return label;
        }

        private static TMP_FontAsset Font()
        {
            if (s_font == null)
            {
                s_font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiKitBuilder.FontPath);
            }

            return s_font;
        }

        private static Sprite Hud(string name) => LoadSprite($"{HudSprites}/{name}.png");

        private static Sprite Kit(string name) => LoadSprite($"{KitSprites}/{name}.png");

        private static Sprite Icon(string name) => LoadSprite($"{IconSprites}/{name}.png");

        private static Sprite LoadSprite(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                Debug.LogError($"[Graybox] Missing HUD sprite {path}.");
            }

            return sprite;
        }

        // Saves the object as a prefab asset and hands back the component of that name on the asset.
        private static T SavePart<T>(GameObject instance, string name) where T : Component
        {
            string path = $"{GrayboxRoot}/Prefabs/{PartsFolder}/{name}.prefab";
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(instance, path);
            Object.DestroyImmediate(instance);
            return saved.GetComponent<T>();
        }
    }
}
