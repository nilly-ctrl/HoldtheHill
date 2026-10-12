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
    /// the prefab. Needs the UI kit prefabs (Tools > Hold the Hill > Build UI GrayboxHudLayout.Kit).
    /// </remarks>
    public static class GrayboxHudBuilder
    {
        public const string PrefabPath = GrayboxRoot + "/Prefabs/GrayboxHud.prefab";

        internal const string GrayboxRoot = "Assets/_Sandbox/nilly-ctrl/Graybox";
        internal const string PartsFolder = "Hud";
        internal const string HudSprites = "Assets/_Sandbox/nilly-ctrl/Ui/Hud";
        internal const string KitSprites = UiKitBuilder.Root + "/Sprites";
        internal const string IconSprites = "Assets/_Sandbox/nilly-ctrl/Icons/PNG";

        // Below the menus, which sit at 100.
        internal const int SortingOrder = 90;

        internal static readonly Color Ink = new Color32(0x1b, 0x11, 0x0b, 0xff);

        internal static TMP_FontAsset s_font;

        // Columns sit this far in from the screen edge, in canvas units.
        internal const float Edge = 6f;

        [MenuItem("Tools/Hold the Hill/Build Graybox HUD")]
        public static GameObject BuildPrefab()
        {
            GrayboxBuildKit.CreateFolderIfMissing(GrayboxRoot, "Prefabs");
            GrayboxBuildKit.CreateFolderIfMissing(GrayboxRoot + "/Prefabs", PartsFolder);

            GrayboxBuildSlot slot = GrayboxHudPartsBuilder.BuildSlot();
            GrayboxBuildOption option = GrayboxHudPartsBuilder.BuildOption();
            Image marker = GrayboxHudPartsBuilder.BuildWaveMarker();
            GrayboxHudChip chip = GrayboxHudPartsBuilder.BuildChip();
            GrayboxSkillNodeRow skillRow = GrayboxHudWindowsBuilder.BuildSkillNodeRow();

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
            // The windows a key opens, over everything but the placement label.
            GrayboxHudWindowsBuilder.BuildSkillTreePanel(canvas, skillRow);
            GrayboxHudWindowsBuilder.BuildAchievementsPanel(canvas);
            GrayboxHudWindowsBuilder.BuildSpecialsPanel(canvas);
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

        // ---------- panels ----------

        // Stats and the horde buttons, stacked down the left edge.
        internal static void BuildLeftColumn(Transform canvas)
        {
            RectTransform column = GrayboxHudLayout.Column("LeftColumn", canvas, new Vector2(0f, 1f), new Vector2(Edge, -Edge));

            // The tuning readout.
            RectTransform statsPanel = GrayboxHudLayout.HudPanel(column, "StatsPanel", 124f);
            TextMeshProUGUI title = GrayboxHudLayout.Line(statsPanel, "GRAYBOX COMBAT TEST");
            title.color = UiKitStyle.Gold;
            TextMeshProUGUI food = GrayboxHudLayout.Line(statsPanel, "$500  1/4");
            TextMeshProUGUI skills = GrayboxHudLayout.Line(statsPanel, "Skills  0 SP");
            TextMeshProUGUI state = GrayboxHudLayout.Line(statsPanel, "State  Idle");
            UiKitBuilder.AddDivider(statsPanel);
            TextMeshProUGUI spawned = GrayboxHudLayout.Line(statsPanel, "Spawned  0");
            TextMeshProUGUI killed = GrayboxHudLayout.Line(statsPanel, "Killed  0");
            TextMeshProUGUI leaked = GrayboxHudLayout.Line(statsPanel, "Leaked  0");
            TextMeshProUGUI alive = GrayboxHudLayout.Line(statsPanel, "Alive  0");
            UiKitBuilder.AddDivider(statsPanel);
            TextMeshProUGUI time = GrayboxHudLayout.Line(statsPanel, "Time  0s  (x1)");
            TextMeshProUGUI keys = GrayboxHudLayout.Line(statsPanel, "N next wave");
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
            RectTransform hordePanel = GrayboxHudLayout.HudPanel(column, "HordePanel", 124f);
            TextMeshProUGUI hordeTitle = GrayboxHudLayout.Line(hordePanel, "HORDE SPAWNER");
            hordeTitle.color = UiKitStyle.Gold;
            var horde = hordePanel.gameObject.AddComponent<GrayboxHordePanel>();
            UiKitBuilder.Wire(horde, "_swarmRush", UiKitBuilder.AddButton(hordePanel, "Swarm rush"));
            UiKitBuilder.Wire(horde, "_bruteParade", UiKitBuilder.AddButton(hordePanel, "Brute parade"));
            UiKitBuilder.Wire(horde, "_phalanx", UiKitBuilder.AddButton(hordePanel, "Phalanx"));
            UiKitBuilder.Wire(horde, "_hydraSplitters", UiKitBuilder.AddButton(hordePanel, "Hydra splitters"));
            UiKitBuilder.Wire(horde, "_megaHorde", UiKitBuilder.AddButton(hordePanel, "Mega horde"));
        }

        // The hill's health, top centre.
        internal static void BuildHealthBar(Transform canvas)
        {
            const float width = 200f;
            RectTransform bar = GrayboxHudLayout.NewRect("HealthBar", canvas);
            GrayboxHudLayout.Anchor(bar, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -Edge), new Vector2(width, 24f));

            TextMeshProUGUI label = GrayboxHudLayout.AddText(bar, "Label", "HILL BASE HP: 100 / 100", TextAlignmentOptions.Top, UiKitStyle.Cream);
            GrayboxHudLayout.Anchor(label.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 10f));

            RectTransform frame = GrayboxHudLayout.NewRect("Frame", bar);
            GrayboxHudLayout.Anchor(frame, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(0f, 10f));
            GrayboxHudLayout.AddImage(frame, GrayboxHudLayout.Hud("BarFrame")).raycastTarget = false;

            RectTransform well = GrayboxHudLayout.NewRect("Well", frame);
            GrayboxHudLayout.Stretch(well, 3f, 3f, 3f, 3f);

            RectTransform fill = GrayboxHudLayout.NewRect("Fill", well);
            GrayboxHudLayout.Stretch(fill, 0f, 0f, 0f, 0f);
            Image fillImage = GrayboxHudLayout.AddImage(fill, GrayboxHudLayout.Hud("BarFillHealth"));
            fillImage.raycastTarget = false;

            // Quarter marks.
            for (int i = 1; i < 4; i++)
            {
                RectTransform tick = GrayboxHudLayout.NewRect("Tick" + i, well);
                GrayboxHudLayout.Anchor(tick, new Vector2(i / 4f, 0.5f), new Vector2(i / 4f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1f, 4f));
                GrayboxHudLayout.AddImage(tick, GrayboxHudLayout.Hud("BarTick")).raycastTarget = false;
            }

            RectTransform heart = GrayboxHudLayout.NewRect("Heart", bar);
            GrayboxHudLayout.Anchor(heart, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(-4f, -8f), new Vector2(16f, 16f));
            GrayboxHudLayout.AddImage(heart, GrayboxHudLayout.Icon("ResQueenHealthIcon")).raycastTarget = false;

            var health = bar.gameObject.AddComponent<GrayboxBaseHealthBar>();
            UiKitBuilder.Wire(health, "_fill", fill);
            UiKitBuilder.Wire(health, "_fillImage", fillImage);
            UiKitBuilder.Wire(health, "_label", label);
            UiKitBuilder.Wire(health, "_healthy", GrayboxHudLayout.Hud("BarFillHealth"));
            UiKitBuilder.Wire(health, "_hurt", GrayboxHudLayout.Hud("BarFillFood"));
            UiKitBuilder.Wire(health, "_danger", GrayboxHudLayout.Hud("BarFillDanger"));
        }

        // Wave progress under the health bar.
        internal static void BuildWaveTrack(Transform canvas, Image marker)
        {
            const float width = 200f;
            RectTransform track = GrayboxHudLayout.NewRect("WaveTrack", canvas);
            GrayboxHudLayout.Anchor(track, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(width, 6f));
            GrayboxHudLayout.AddImage(track, GrayboxHudLayout.Hud("WaveTrack")).raycastTarget = false;

            RectTransform fill = GrayboxHudLayout.NewRect("Fill", track);
            GrayboxHudLayout.Anchor(fill, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(2f, 0f), new Vector2(0f, 2f));
            GrayboxHudLayout.AddImage(fill, GrayboxHudLayout.Hud("WaveFill")).raycastTarget = false;

            RectTransform now = GrayboxHudLayout.NewRect("Now", track);
            GrayboxHudLayout.Anchor(now, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0.5f, 1f), new Vector2(2f, 0f), new Vector2(7f, 7f));
            GrayboxHudLayout.AddImage(now, GrayboxHudLayout.Hud("WaveMarkerNow")).raycastTarget = false;

            var wave = track.gameObject.AddComponent<GrayboxWaveTrack>();
            UiKitBuilder.Wire(wave, "_track", track);
            UiKitBuilder.Wire(wave, "_fill", fill);
            UiKitBuilder.Wire(wave, "_now", now);
            UiKitBuilder.Wire(wave, "_markerPrefab", marker);
            UiKitBuilder.Wire(wave, "_upcoming", GrayboxHudLayout.Hud("WaveMarker"));
            UiKitBuilder.Wire(wave, "_cleared", GrayboxHudLayout.Hud("WaveMarkerDone"));
            UiKitBuilder.Wire(wave, "_boss", GrayboxHudLayout.Hud("WaveMarkerBoss"));
        }

        // The selected tower's card, down the right edge below the pixel readouts.
        internal static void BuildRightColumn(Transform canvas)
        {
            RectTransform column = GrayboxHudLayout.Column("RightColumn", canvas, new Vector2(1f, 1f), new Vector2(-Edge, -112f));

            RectTransform panel = GrayboxHudLayout.HudPanel(column, "TowerCard", 164f);
            TextMeshProUGUI title = GrayboxHudLayout.Line(panel, "Tower");
            title.color = new Color32(0x80, 0xd0, 0xff, 0xff);
            TextMeshProUGUI role = GrayboxHudLayout.Line(panel, "Gunner");
            UiStepper priority = UiKitBuilder.AddStepper(panel, "Target", new[] { "First" }, 0);
            TextMeshProUGUI level = GrayboxHudLayout.Line(panel, "Range 3.0m  Level 1/3");
            TextMeshProUGUI damage = GrayboxHudLayout.Line(panel, "Damage dealt  0");
            TextMeshProUGUI record = GrayboxHudLayout.Line(panel, "Kills 0   Shots 0");
            UiKitBuilder.AddDivider(panel);
            UiButton upgrade = UiKitBuilder.AddButton(panel, "Upgrade");
            RectTransform row = UiKitBuilder.AddButtonRow(panel);
            UiButton sell = UiKitBuilder.AddButton(row, "Dismantle");
            UiButton close = UiKitBuilder.AddButton(row, "Close");

            var card = panel.gameObject.AddComponent<GrayboxTowerCard>();
            UiKitBuilder.Wire(card, "_title", title);
            UiKitBuilder.Wire(card, "_role", role);
            UiKitBuilder.Wire(card, "_priority", priority);
            UiKitBuilder.Wire(card, "_levelLine", level);
            UiKitBuilder.Wire(card, "_damage", damage);
            UiKitBuilder.Wire(card, "_record", record);
            UiKitBuilder.Wire(card, "_upgrade", upgrade);
            UiKitBuilder.Wire(card, "_sell", sell);
            UiKitBuilder.Wire(card, "_close", close);
        }

        // The Warden's health, abilities and running power-ups, just above the build bar.
        internal static void BuildWardenStrip(Transform canvas, GrayboxHudChip chip)
        {
            RectTransform strip = GrayboxHudLayout.NewRect("WardenStrip", canvas);
            GrayboxHudLayout.Anchor(strip, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 78f), Vector2.zero);

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
        internal static void BuildBuildBar(Transform canvas, GrayboxBuildSlot slot)
        {
            RectTransform bar = GrayboxHudLayout.NewRect("BuildBar", canvas);
            GrayboxHudLayout.Anchor(bar, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 4f), Vector2.zero);
            GrayboxHudLayout.AddImage(bar, GrayboxHudLayout.Kit("Panel"));

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
        internal static void BuildBuildMenu(Transform canvas, GrayboxBuildOption option)
        {
            RectTransform panel = UiKitBuilder.AddPanel(canvas, 240f);
            panel.name = "BuildMenu";

            TextMeshProUGUI title = GrayboxHudLayout.Line(panel, "BUILD TOWER");
            title.color = UiKitStyle.Gold;
            TextMeshProUGUI cell = GrayboxHudLayout.Line(panel, "Grid (0, 0)");
            cell.color = UiKitStyle.Dim;

            RectTransform options = GrayboxHudLayout.NewRect("Options", panel);
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
        internal static void BuildPlacementLabel(Transform canvas)
        {
            RectTransform root = GrayboxHudLayout.NewRect("PlacementLabel", canvas);
            GrayboxHudLayout.Anchor(root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(160f, 12f));
            TextMeshProUGUI text = GrayboxHudLayout.AddText(root, "Text", "BUILD", TextAlignmentOptions.Center, UiKitStyle.Cream);
            GrayboxHudLayout.Stretch(text.rectTransform, 0f, 0f, 0f, 0f);

            var label = root.gameObject.AddComponent<GrayboxPlacementLabel>();
            UiKitBuilder.Wire(label, "_text", text);
        }

        // ---------- helpers ----------

    }
}
