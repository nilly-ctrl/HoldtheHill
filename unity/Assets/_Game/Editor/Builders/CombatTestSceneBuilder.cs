using HoldTheHill.Core.Audio;
using HoldTheHill.Core.Icons;
using HoldTheHill.Core.Save;
using HoldTheHill.Features.Hud;
using HoldTheHill.Features.DamageNumbers;
using HoldTheHill.Core.Animation;
using HoldTheHill.Features.GameFlow;
using HoldTheHill.Features.Economy;
using System.Collections.Generic;
using System.Linq;
using HoldTheHill.Features.Combat;
using HoldTheHill.Features.Enemies;
using HoldTheHill.Features.Towers;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HoldTheHill.EditorTools.Builders
{
    /// <summary>
    /// Generates a gray-box scene for exercising the combat system: a path, a spawner,
    /// and one tower per weapon type, all built from primitives.
    /// </summary>
    /// <remarks>
    /// Everything here is generated rather than hand-placed so the scene can be rebuilt
    /// from scratch after a change instead of being repaired by hand, and so it can be
    /// produced in batch mode without the editor UI.
    ///
    /// This lived in the sandbox rather than <c>_Game/Editor/</c>
    /// on purpose. <c>_Game/Editor/</c> is the <c>HoldTheHill.Editor</c> assembly, which
    /// cannot see <c>EnemySpawner</c> (that compiles into <c>Assembly-CSharp</c>). A script
    /// here has no asmdef, so it lands in <c>Assembly-CSharp-Editor</c> and can reach both.
    /// </remarks>
    internal static class CombatTestSceneBuilder
    {
        private const string ScenePath = BuildKit.DevScenesRoot + "/CombatTest.unity";
        internal const string WaveAssetPath = BuildKit.FeaturesRoot + "/Enemies/Data/Waves.asset";

        /// <summary>Opens the gray-box scene, building it first if it is not there yet.</summary>
        [MenuItem("Tools/Hold the Hill/Open Graybox Combat Test")]
        public static void Open()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                Build();
                return;
            }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        [MenuItem("Tools/Hold the Hill/Build Graybox Combat Test")]
        public static void Build()
        {
            EnsureFolders();
            EnemyPrefabBuilder.Reset();

            // Baked from Animations/Aseprite; every tower, enemy and the mine use it below.
            BuildKit.Anim = AnimLibraryBuilder.Build();

            Material lineMaterial = ProjectilePrefabBuilder.CreateLineMaterial();
            Sprite circle = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            Sprite square = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            // Fragments must exist before the cluster shell that references them.
            GameObject fragment = ProjectilePrefabBuilder.BuildFragmentPrefab(circle);
            GameObject hazard = ProjectilePrefabBuilder.BuildHazardPrefab(circle);
            GameObject bullet = ProjectilePrefabBuilder.BuildBulletPrefab(circle);
            GameObject homing = ProjectilePrefabBuilder.BuildHomingPrefab(circle);
            GameObject mortar = ProjectilePrefabBuilder.BuildMortarPrefab(circle, fragment, hazard);
            GameObject ricochet = ProjectilePrefabBuilder.BuildRicochetPrefab(circle);
            GameObject mine = ProjectilePrefabBuilder.BuildMinePrefab(circle);

            // Fast and fragile, the baseline, and a slow wall. The spread is what makes
            // Strongest and Weakest targeting visibly different from Closest.
            // breach: hill health lost when one walks in (was a name table in HillHealth).
            GameObject runner = EnemyPrefabBuilder.BuildEnemyPrefab(
                "Runner", circle, health: 90f, speed: 2.4f, new Color(0.95f, 0.75f, 0.2f), 0.45f, bounty: 8, breach: 5f);
            GameObject grunt = EnemyPrefabBuilder.BuildEnemyPrefab(
                "Grunt", circle, health: 240f, speed: 1.4f, new Color(0.85f, 0.25f, 0.25f), 0.6f, bounty: 10, breach: 10f);
            GameObject brute = EnemyPrefabBuilder.BuildEnemyPrefab(
                "Brute", circle, health: 900f, speed: 0.85f, new Color(0.5f, 0.1f, 0.35f), 0.95f, bounty: 35, breach: 25f);

            GameObject shielded = EnemyPrefabBuilder.BuildShieldedEnemyPrefab(
                "Shielded", circle, health: 160f, shield: 120f, speed: 1.2f, new Color(0.3f, 0.7f, 0.9f), 0.65f, bounty: 25, breach: 12f);
            GameObject swarm = EnemyPrefabBuilder.BuildEnemyPrefab(
                "Swarm", circle, health: 35f, speed: 2.8f, new Color(1f, 0.85f, 0.1f), 0.35f, bounty: 4, breach: 2f);
            GameObject splitter = EnemyPrefabBuilder.BuildSplitterEnemyPrefab(
                "Splitter", circle, swarm, health: 180f, speed: 1.1f, new Color(0.7f, 0.2f, 0.7f), 0.75f, bounty: 20, breach: 8f);
            GameObject healer = EnemyPrefabBuilder.BuildHealerEnemyPrefab(
                "Healer", circle, health: 210f, speed: 1.0f, new Color(0.2f, 0.9f, 0.4f), 0.65f, bounty: 30, breach: 15f);

            // Food and the crumbs that pay it. Bosses drop the golden one.
            GameObject goldenCrumb = PickupBuilder.BuildPrefabs(circle);

            // The Warden and its five abilities. Dig turns up the small crumb.
            WardenBuilder.BuildPrefabs(square);

            // Honey Drop and Berserk Berry: an effect prefab, a pickup and a data asset each.
            PowerUpBuilder.BuildPrefabs();

            // Bosses and special enemies (Graybox/Specials): their prefabs, then a wave for each.
            SpecialsBuilder.BuildPrefabs(circle, grunt, BuildKit.Anim, goldenCrumb);

            // One prefab variant, data asset and catalog entry per tower (TowerBuilder).
            TowerBuilder.BuildPrefabs(square, bullet, homing, mortar, ricochet, mine, lineMaterial, BuildKit.Anim);

            // Species: which catalog and Warden a run uses. Needs the tower and Warden prefabs above.
            SpeciesBuilder.BuildAssets();
            ProgressionBuilder.BuildAssets();

            MapWaveDataSO waves = SpawnerBuilder.BuildWaveAsset(runner, grunt, brute, shielded, splitter, healer);
            SpecialsBuilder.AppendWaves(WaveAssetPath, grunt);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // NewScene can unload the library (nothing in the empty scene holds it), so fetch it again.
            BuildKit.Anim = AssetDatabase.LoadAssetAtPath<SpriteAnimLibrary>(AnimLibraryBuilder.LibraryPath);
            SceneDressingBuilder.BuildCamera();
            BuildEconomy();
            BuildSkillTree();
            BuildCombatSystems();
            BuildProceduralWaveGenerator();
            EnemyPath path = SceneDressingBuilder.BuildPath();
            SceneDressingBuilder.BuildGround(path);
            SceneDressingBuilder.BuildDecor(path);
            BuildSfx();
            BuildIcons();
            SceneDressingBuilder.BuildProps();
            BuildImpactFx();
            SpawnerBuilder.BuildSpawner(path, waves, runner, grunt, brute, shielded, swarm, splitter, healer);
            SpecialsBuilder.BuildSceneObject(BuildKit.Anim);
            TowerBuilder.BuildSceneTowers();
            BuildTowerPlacer(lineMaterial);
            // The species loader puts the Warden on the map on Play, so it is not placed here.
            SpeciesBuilder.BuildSceneObject();
            PowerUpBuilder.BuildSceneObject();
            BuildReadmeLabel();
            BuildFlow();
            // One prefab of managers; level scenes carry an instance of it instead of their own copies.
            SystemsBuilder.Collect(scene);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[Graybox] Built {ScenePath}. Open it and press Play.");
        }

        // Last, so the flow controller finds the spawner and placer built above.
        private static void BuildFlow()
        {
            var go = new GameObject("RunFlow");
            go.AddComponent<SaveHost>(); // attaches the save file; without it nothing is persisted
            var flow = go.AddComponent<RunFlow>();
            go.AddComponent<EndlessMode>();
            // Tuning counters for the HUD, and the next-wave, speed, restart and toggle keys.
            go.AddComponent<RunCounters>();
            go.AddComponent<RunHotkeys>();

            // The game opens on the title screen. Change "Initial State" on this object to Playing
            // to drop straight into the field while tuning.
            BuildKit.Apply(flow, so => so.FindProperty("_initialState").enumValueIndex = (int)GameFlowState.Title);

            // Every menu screen (uGUI), rebuilt with the scene so they match the code.
            MenusBuilder.BuildPrefab();
            MenusBuilder.AddToScene();

            // The in-game HUD (uGUI), under the menus.
            HudBuilder.BuildPrefab();
            HudBuilder.AddToScene();
        }

        private static void BuildEconomy()
        {
            var go = new GameObject("FoodWallet");
            ResourceData food = PickupBuilder.Food();
            BuildKit.Apply(go.AddComponent<FoodWallet>(), so => so.FindProperty("_mainResource").objectReferenceValue = food);
        }

        private static void BuildSkillTree()
        {
            ProgressionBuilder.BuildSkillTreeObject();
        }

        private static void BuildCombatSystems()
        {
            var go = new GameObject("CombatSystems");
            // Pixel-font popups (DamageNumbers/).
            PixelFontBuilder.Build();
            PixelFontBuilder.Configure(go.AddComponent<DamageNumberSpawner>(), DamageNumberPixelScale);
            // One setting for which art theme's fonts the scene uses; empty is the base set.
            PixelFontBuilder.ConfigureTheme(go.AddComponent<PixelFontTheme>());
            // WAVE 3, WAVE CLEAR, VICTORY and DEFEAT in the theme's banner styles.
            go.AddComponent<WaveBanner>();
            // F6 steps through the font themes while playing.
            go.AddComponent<FontThemeKey>();
            // Food, wave and time in the pixel font, and a combo counter for quick kills.
            go.AddComponent<PixelHud>();
            go.AddComponent<KillCombo>();
            // A word and icon when a status lands, and a name banner and label for bosses.
            go.AddComponent<StatusPopups>();
            go.AddComponent<BossBanner>();
            go.AddComponent<StatusCombos>();
            go.AddComponent<HillHealth>();
        }

        // 2 font pixels per art pixel: at 1 the numbers are too small to read at this camera size.
        private const int DamageNumberPixelScale = 2;
        private const string IconRoot = "Assets/_Game/Shared/Art/Icons/PNG";

        /// <summary>Every icon in Icons/PNG, held by the scene so the HUD can use them in a build too.</summary>
        private static void BuildIcons()
        {
            Texture2D[] icons = AssetDatabase.FindAssets("t:Texture2D", new[] { IconRoot, UiRoot + "/Hud", UiRoot + "/Markers" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Distinct()
                .OrderBy(p => p)
                .Select(AssetDatabase.LoadAssetAtPath<Texture2D>)
                .Where(t => t != null)
                .ToArray();
            if (icons.Length == 0)
            {
                Debug.LogWarning("[Graybox] No icons found in " + IconRoot + "; the HUD falls back to text.");
                return;
            }

            var go = new GameObject("IconLibrary");
            IconLibrary holder = go.AddComponent<IconLibrary>();
            BuildKit.Apply(holder, so =>
            {
                SerializedProperty list = so.FindProperty("_icons");
                list.arraySize = icons.Length;
                for (int i = 0; i < icons.Length; i++)
                {
                    list.GetArrayElementAtIndex(i).objectReferenceValue = icons[i];
                }
            });
        }

        private static void BuildProceduralWaveGenerator()
        {
            var go = new GameObject("ProceduralWaveGenerator");
            go.AddComponent<ProceduralWaveGenerator>();
        }

        private static void EnsureFolders()
        {
            BuildKit.EnsureFolder(BuildKit.DevScenesRoot);
            BuildKit.EnsureFolder(BuildKit.FeaturesRoot + "/Enemies/Data");
        }

        // ---------- prefabs ----------

        private const string UiRoot = "Assets/_Game/Shared/Art/Ui";
        private const string FontPath = "Assets/_Game/Shared/Fonts/Pixel/TTF/HoldTheHillPixel-Regular.ttf";

        /// <summary>The pixel skin every HUD script switches to (see ImguiSkin).</summary>
        internal static void BuildUi()
        {
            // Hinted raster keeps the pixel font's edges hard instead of anti-aliased.
            foreach (string path in new[] { FontPath, FontPath.Replace("-Regular", "-Bold") })
            {
                if (AssetImporter.GetAtPath(path) is TrueTypeFontImporter importer
                    && importer.fontRenderingMode != FontRenderingMode.HintedRaster)
                {
                    importer.fontRenderingMode = FontRenderingMode.HintedRaster;
                    importer.SaveAndReimport();
                }
            }

            var font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            var panel = AssetDatabase.LoadAssetAtPath<Texture2D>(UiRoot + "/Panel.png");
            if (font == null || panel == null)
            {
                Debug.LogWarning("[Graybox] Pixel font or Ui textures missing; the HUD keeps Unity's default skin.");
                return;
            }

            var go = new GameObject("ImguiSkin");
            ImguiSkin ui = go.AddComponent<ImguiSkin>();
            BuildKit.Apply(ui, so =>
            {
                so.FindProperty("_font").objectReferenceValue = font;
                so.FindProperty("_panel").objectReferenceValue = panel;
                so.FindProperty("_button").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(UiRoot + "/Button.png");
                so.FindProperty("_buttonHover").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(UiRoot + "/ButtonHover.png");
                so.FindProperty("_buttonPressed").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(UiRoot + "/ButtonPressed.png");
            });
        }

        private static void BuildImpactFx()
        {
            if (BuildKit.Anim == null)
            {
                return;
            }

            new GameObject("ImpactFx").AddComponent<ImpactFx>().Configure(BuildKit.Anim);
        }

        private static void BuildSfx()
        {
            SfxBank bank = SfxBankBuilder.Build();
            var go = new GameObject("Sfx");
            Sfx sfx = go.AddComponent<Sfx>();
            BuildKit.Apply(sfx, so => so.FindProperty("_bank").objectReferenceValue = bank);
        }

        private static void BuildTowerPlacer(Material lineMaterial)
        {
            // Loaded here, not passed in: assets made before NewScene can be unloaded by it.
            var catalog = AssetDatabase.LoadAssetAtPath<TowerCatalog>(TowerBuilder.CatalogPath);
            if (catalog == null || catalog.Count == 0)
            {
                throw new System.InvalidOperationException(
                    $"[Graybox] Tower catalog missing or empty at {TowerBuilder.CatalogPath}. Nothing could be built.");
            }

            var go = new GameObject("Tower Placer & Inspector");
            GameTowerPlacer placer = go.AddComponent<GameTowerPlacer>();
            BuildKit.Apply(placer, so =>
            {
                so.FindProperty("_catalog").objectReferenceValue = catalog;
                so.FindProperty("_lineMaterial").objectReferenceValue = lineMaterial;
                so.FindProperty("_animLibrary").objectReferenceValue = BuildKit.Anim;
            });
        }

        private static void BuildReadmeLabel()
        {
            // A plain marker object, so whoever opens the scene knows it is generated.
            var go = new GameObject("--- GENERATED BY Tools > Hold the Hill > Build Graybox Combat Test ---");
            go.transform.position = Vector3.zero;
            go.SetActive(false);
        }

        // ---------- helpers ----------

    }
}
