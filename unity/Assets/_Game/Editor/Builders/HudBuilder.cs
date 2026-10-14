using HoldTheHill.Features.Hud;
using HoldTheHill.Features.Towers;
using HoldTheHill.Core.UiKit;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HoldTheHill.EditorTools.Builders
{
    /// <summary>
    /// Builds the in-game HUD as a prefab (a variant of the UI kit's UiRoot canvas) from small part
    /// prefabs: a build slot, a build option, a wave marker and a chip. Called from
    /// <see cref="CombatTestSceneBuilder"/> and from Tools > Hold the Hill > Build HUD.
    /// </summary>
    /// <remarks>
    /// Everything is regenerated on each build, so layout is changed here rather than by hand on
    /// the prefab. Needs the UI kit prefabs (Tools > Hold the Hill > Build UI HudLayout.Kit).
    /// </remarks>
    public static class HudBuilder
    {
        public const string PrefabPath = FeatureRoot + "/Prefabs/Hud.prefab";

        internal const string FeatureRoot = "Assets/_Game/Features/Hud";
        internal const string PartsFolder = "Parts";
        internal const string HudSprites = "Assets/_Game/Shared/Art/Ui/Hud";
        internal const string KitSprites = UiKitBuilder.Root + "/Sprites";
        internal const string IconSprites = "Assets/_Game/Shared/Art/Icons/PNG";

        // Below the menus, which sit at 100.
        internal const int SortingOrder = 90;

        internal static readonly Color Ink = new Color32(0x1b, 0x11, 0x0b, 0xff);

        internal static TMP_FontAsset s_font;

        // Columns sit this far in from the screen edge, in canvas units.
        internal const float Edge = 6f;

        [MenuItem("Tools/Hold the Hill/Build HUD")]
        public static GameObject BuildPrefab()
        {
            BuildKit.CreateFolderIfMissing(FeatureRoot, "Prefabs");
            BuildKit.CreateFolderIfMissing(FeatureRoot + "/Prefabs", PartsFolder);

            BuildSlot slot = HudPartsBuilder.BuildSlot();
            BuildOption option = HudPartsBuilder.BuildOption();
            Image marker = HudPartsBuilder.BuildWaveMarker();
            HudChip chip = HudPartsBuilder.BuildChip();
            SkillNodeRow skillRow = HudWindowsBuilder.BuildSkillNodeRow();

            GameObject root = UiKitBuilder.InstantiatePrefab("UiRoot", null);
            if (root == null)
            {
                return null;
            }

            root.name = "Hud";
            root.GetComponent<Canvas>().sortingOrder = SortingOrder;
            root.AddComponent<HudRoot>();
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
            HudWindowsBuilder.BuildSkillTreePanel(canvas, skillRow);
            HudWindowsBuilder.BuildAchievementsPanel(canvas);
            HudWindowsBuilder.BuildSpecialsPanel(canvas);
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
                Debug.LogError("[Build] No HUD: build the UI kit first (Tools > Hold the Hill > Build UI Kit).");
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
            RectTransform column = HudLayout.Column("LeftColumn", canvas, new Vector2(0f, 1f), new Vector2(Edge, -Edge));

            // The tuning readout.
            RectTransform statsPanel = HudLayout.HudPanel(column, "StatsPanel", 124f);
            TextMeshProUGUI title = HudLayout.Line(statsPanel, "GRAYBOX COMBAT TEST");
            title.color = UiKitStyle.Gold;
            TextMeshProUGUI food = HudLayout.Line(statsPanel, "$500  1/4");
            TextMeshProUGUI skills = HudLayout.Line(statsPanel, "Skills  0 SP");
            TextMeshProUGUI state = HudLayout.Line(statsPanel, "State  Idle");
            UiKitBuilder.AddDivider(statsPanel);
            TextMeshProUGUI spawned = HudLayout.Line(statsPanel, "Spawned  0");
            TextMeshProUGUI killed = HudLayout.Line(statsPanel, "Killed  0");
            TextMeshProUGUI leaked = HudLayout.Line(statsPanel, "Leaked  0");
            TextMeshProUGUI alive = HudLayout.Line(statsPanel, "Alive  0");
            UiKitBuilder.AddDivider(statsPanel);
            TextMeshProUGUI time = HudLayout.Line(statsPanel, "Time  0s  (x1)");
            TextMeshProUGUI keys = HudLayout.Line(statsPanel, "N next wave");
            keys.color = UiKitStyle.Dim;

            var stats = statsPanel.gameObject.AddComponent<StatsPanel>();
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
            RectTransform hordePanel = HudLayout.HudPanel(column, "HordePanel", 124f);
            TextMeshProUGUI hordeTitle = HudLayout.Line(hordePanel, "HORDE SPAWNER");
            hordeTitle.color = UiKitStyle.Gold;
            var horde = hordePanel.gameObject.AddComponent<HordePanel>();
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
            RectTransform bar = HudLayout.NewRect("HealthBar", canvas);
            HudLayout.Anchor(bar, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -Edge), new Vector2(width, 24f));

            TextMeshProUGUI label = HudLayout.AddText(bar, "Label", "HILL BASE HP: 100 / 100", TextAlignmentOptions.Top, UiKitStyle.Cream);
            HudLayout.Anchor(label.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 10f));

            RectTransform frame = HudLayout.NewRect("Frame", bar);
            HudLayout.Anchor(frame, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(0f, 10f));
            HudLayout.AddImage(frame, HudLayout.Hud("BarFrame")).raycastTarget = false;

            RectTransform well = HudLayout.NewRect("Well", frame);
            HudLayout.Stretch(well, 3f, 3f, 3f, 3f);

            RectTransform fill = HudLayout.NewRect("Fill", well);
            HudLayout.Stretch(fill, 0f, 0f, 0f, 0f);
            Image fillImage = HudLayout.AddImage(fill, HudLayout.Hud("BarFillHealth"));
            fillImage.raycastTarget = false;

            // Quarter marks.
            for (int i = 1; i < 4; i++)
            {
                RectTransform tick = HudLayout.NewRect("Tick" + i, well);
                HudLayout.Anchor(tick, new Vector2(i / 4f, 0.5f), new Vector2(i / 4f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1f, 4f));
                HudLayout.AddImage(tick, HudLayout.Hud("BarTick")).raycastTarget = false;
            }

            RectTransform heart = HudLayout.NewRect("Heart", bar);
            HudLayout.Anchor(heart, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(-4f, -8f), new Vector2(16f, 16f));
            HudLayout.AddImage(heart, HudLayout.Icon("ResQueenHealthIcon")).raycastTarget = false;

            var health = bar.gameObject.AddComponent<BaseHealthBar>();
            UiKitBuilder.Wire(health, "_fill", fill);
            UiKitBuilder.Wire(health, "_fillImage", fillImage);
            UiKitBuilder.Wire(health, "_label", label);
            UiKitBuilder.Wire(health, "_healthy", HudLayout.Hud("BarFillHealth"));
            UiKitBuilder.Wire(health, "_hurt", HudLayout.Hud("BarFillFood"));
            UiKitBuilder.Wire(health, "_danger", HudLayout.Hud("BarFillDanger"));
        }

        // Wave progress under the health bar.
        internal static void BuildWaveTrack(Transform canvas, Image marker)
        {
            const float width = 200f;
            RectTransform track = HudLayout.NewRect("WaveTrack", canvas);
            HudLayout.Anchor(track, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(width, 6f));
            HudLayout.AddImage(track, HudLayout.Hud("WaveTrack")).raycastTarget = false;

            RectTransform fill = HudLayout.NewRect("Fill", track);
            HudLayout.Anchor(fill, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(2f, 0f), new Vector2(0f, 2f));
            HudLayout.AddImage(fill, HudLayout.Hud("WaveFill")).raycastTarget = false;

            RectTransform now = HudLayout.NewRect("Now", track);
            HudLayout.Anchor(now, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0.5f, 1f), new Vector2(2f, 0f), new Vector2(7f, 7f));
            HudLayout.AddImage(now, HudLayout.Hud("WaveMarkerNow")).raycastTarget = false;

            var wave = track.gameObject.AddComponent<WaveTrack>();
            UiKitBuilder.Wire(wave, "_track", track);
            UiKitBuilder.Wire(wave, "_fill", fill);
            UiKitBuilder.Wire(wave, "_now", now);
            UiKitBuilder.Wire(wave, "_markerPrefab", marker);
            UiKitBuilder.Wire(wave, "_upcoming", HudLayout.Hud("WaveMarker"));
            UiKitBuilder.Wire(wave, "_cleared", HudLayout.Hud("WaveMarkerDone"));
            UiKitBuilder.Wire(wave, "_boss", HudLayout.Hud("WaveMarkerBoss"));
        }

        // The selected tower's card, down the right edge below the pixel readouts.
        internal static void BuildRightColumn(Transform canvas)
        {
            RectTransform column = HudLayout.Column("RightColumn", canvas, new Vector2(1f, 1f), new Vector2(-Edge, -112f));

            RectTransform panel = HudLayout.HudPanel(column, "TowerCard", 164f);
            TextMeshProUGUI title = HudLayout.Line(panel, "Tower");
            title.color = new Color32(0x80, 0xd0, 0xff, 0xff);
            TextMeshProUGUI role = HudLayout.Line(panel, "Gunner");
            UiStepper priority = UiKitBuilder.AddStepper(panel, "Target", new[] { "First" }, 0);
            TextMeshProUGUI level = HudLayout.Line(panel, "Range 3.0m  Level 1/3");
            TextMeshProUGUI damage = HudLayout.Line(panel, "Damage dealt  0");
            TextMeshProUGUI record = HudLayout.Line(panel, "Kills 0   Shots 0");
            UiKitBuilder.AddDivider(panel);
            UiButton upgrade = UiKitBuilder.AddButton(panel, "Upgrade");
            RectTransform row = UiKitBuilder.AddButtonRow(panel);
            UiButton sell = UiKitBuilder.AddButton(row, "Dismantle");
            UiButton close = UiKitBuilder.AddButton(row, "Close");

            var card = panel.gameObject.AddComponent<TowerCard>();
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
        internal static void BuildWardenStrip(Transform canvas, HudChip chip)
        {
            RectTransform strip = HudLayout.NewRect("WardenStrip", canvas);
            HudLayout.Anchor(strip, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 78f), Vector2.zero);

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

            var warden = strip.gameObject.AddComponent<WardenPanel>();
            UiKitBuilder.Wire(warden, "_chipPrefab", chip);
            UiKitBuilder.Wire(warden, "_chipRoot", strip);
        }

        // The towers on offer, along the bottom edge. The slots are made at run time from the catalog.
        internal static void BuildBuildBar(Transform canvas, BuildSlot slot)
        {
            RectTransform bar = HudLayout.NewRect("BuildBar", canvas);
            HudLayout.Anchor(bar, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 4f), Vector2.zero);
            HudLayout.AddImage(bar, HudLayout.Kit("Panel"));

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

            var buildBar = bar.gameObject.AddComponent<BuildBar>();
            UiKitBuilder.Wire(buildBar, "_slotPrefab", slot);
            UiKitBuilder.Wire(buildBar, "_slotRoot", bar);
        }

        // Opens beside an empty cell. Two columns of towers, so a long catalog still fits the screen.
        internal static void BuildBuildMenu(Transform canvas, BuildOption option)
        {
            RectTransform panel = UiKitBuilder.AddPanel(canvas, 240f);
            panel.name = "BuildMenu";

            TextMeshProUGUI title = HudLayout.Line(panel, "BUILD TOWER");
            title.color = UiKitStyle.Gold;
            TextMeshProUGUI cell = HudLayout.Line(panel, "Grid (0, 0)");
            cell.color = UiKitStyle.Dim;

            RectTransform options = HudLayout.NewRect("Options", panel);
            var grid = options.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(112f, 34f);
            grid.spacing = new Vector2(2f, 2f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.childAlignment = TextAnchor.UpperCenter;

            UiButton cancel = UiKitBuilder.AddButton(panel, "Cancel");

            var menu = panel.gameObject.AddComponent<BuildMenu>();
            UiKitBuilder.Wire(menu, "_cell", cell);
            UiKitBuilder.Wire(menu, "_cancel", cancel);
            UiKitBuilder.Wire(menu, "_optionPrefab", option);
            UiKitBuilder.Wire(menu, "_optionRoot", options);
            UiKitBuilder.Wire(menu, "_frame", panel);
        }

        // "BUILD DRONE $120" over the ghost.
        internal static void BuildPlacementLabel(Transform canvas)
        {
            RectTransform root = HudLayout.NewRect("PlacementLabel", canvas);
            HudLayout.Anchor(root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(160f, 12f));
            TextMeshProUGUI text = HudLayout.AddText(root, "Text", "BUILD", TextAlignmentOptions.Center, UiKitStyle.Cream);
            HudLayout.Stretch(text.rectTransform, 0f, 0f, 0f, 0f);

            var label = root.gameObject.AddComponent<PlacementLabel>();
            UiKitBuilder.Wire(label, "_text", text);
        }

        // ---------- helpers ----------

    }
}
