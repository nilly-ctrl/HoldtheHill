using System.Collections.Generic;
using System.Linq;
using HoldTheHill.Features.Combat;
using HoldTheHill.Features.Enemies;
using HoldTheHill.Features.Towers;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HoldTheHill.Sandbox.NillyCtrl
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
    /// This lives in <c>_Sandbox/nilly-ctrl/Editor/</c> rather than <c>_Game/Editor/</c>
    /// on purpose. <c>_Game/Editor/</c> is the <c>HoldTheHill.Editor</c> assembly, which
    /// cannot see <c>EnemySpawner</c> (that compiles into <c>Assembly-CSharp</c>). A script
    /// here has no asmdef, so it lands in <c>Assembly-CSharp-Editor</c> and can reach both.
    /// </remarks>
    internal static class GrayboxBuilder
    {
        private const string SandboxRoot = "Assets/_Sandbox/nilly-ctrl";
        private const string GrayboxRoot = SandboxRoot + "/Graybox";
        private const string PrefabRoot = GrayboxRoot + "/Prefabs";
        private const string ScenePath = GrayboxRoot + "/GrayboxCombatTest.unity";
        private const string WaveAssetPath = GrayboxRoot + "/GrayboxWaves.asset";
        private const string LineMaterialPath = GrayboxRoot + "/GrayboxLine.mat";

        // The same S-shaped route the Tools menu builds, in grid cells.
        private static readonly Vector2Int[] Route =
        {
            new Vector2Int(-10, 2), new Vector2Int(-4, 2), new Vector2Int(-4, -3),
            new Vector2Int(2, -3), new Vector2Int(2, 3), new Vector2Int(9, 3),
        };

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

            // Baked from Animations/Aseprite; every tower, enemy and the mine use it below.
            s_anim = GrayboxAnimLibraryBuilder.Build();

            Material lineMaterial = CreateLineMaterial();
            Sprite circle = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            Sprite square = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            // Fragments must exist before the cluster shell that references them.
            GameObject fragment = BuildFragmentPrefab(circle);
            GameObject hazard = BuildHazardPrefab(circle);
            GameObject bullet = BuildBulletPrefab(circle);
            GameObject homing = BuildHomingPrefab(circle);
            GameObject mortar = BuildMortarPrefab(circle, fragment, hazard);
            GameObject ricochet = BuildRicochetPrefab(circle);
            GameObject mine = BuildMinePrefab(circle);

            // Fast and fragile, the baseline, and a slow wall. The spread is what makes
            // Strongest and Weakest targeting visibly different from Closest.
            GameObject runner = BuildEnemyPrefab(
                "GrayboxRunner", circle, health: 90f, speed: 2.4f, new Color(0.95f, 0.75f, 0.2f), 0.45f, bounty: 8);
            GameObject grunt = BuildEnemyPrefab(
                "GrayboxGrunt", circle, health: 240f, speed: 1.4f, new Color(0.85f, 0.25f, 0.25f), 0.6f, bounty: 10);
            GameObject brute = BuildEnemyPrefab(
                "GrayboxBrute", circle, health: 900f, speed: 0.85f, new Color(0.5f, 0.1f, 0.35f), 0.95f, bounty: 35);

            GameObject shielded = BuildShieldedEnemyPrefab(
                "GrayboxShielded", circle, health: 160f, shield: 120f, speed: 1.2f, new Color(0.3f, 0.7f, 0.9f), 0.65f, bounty: 25);
            GameObject swarm = BuildEnemyPrefab(
                "GrayboxSwarm", circle, health: 35f, speed: 2.8f, new Color(1f, 0.85f, 0.1f), 0.35f, bounty: 4);
            GameObject splitter = BuildSplitterEnemyPrefab(
                "GrayboxSplitter", circle, swarm, health: 180f, speed: 1.1f, new Color(0.7f, 0.2f, 0.7f), 0.75f, bounty: 20);
            GameObject healer = BuildHealerEnemyPrefab(
                "GrayboxHealer", circle, health: 210f, speed: 1.0f, new Color(0.2f, 0.9f, 0.4f), 0.65f, bounty: 30);

            // Bosses and special enemies (Graybox/Specials): their prefabs, then a wave for each.
            GrayboxSpecialsBuilder.BuildPrefabs(circle, grunt, s_anim);

            MapWaveDataSO waves = BuildWaveAsset(runner, grunt, brute, shielded, splitter, healer);
            GrayboxSpecialsBuilder.AppendWaves(WaveAssetPath, grunt);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // NewScene can unload the library (nothing in the empty scene holds it), so fetch it again.
            s_anim = AssetDatabase.LoadAssetAtPath<SpriteAnimLibrary>(GrayboxAnimLibraryBuilder.LibraryPath);
            BuildCamera();
            BuildEconomy();
            BuildSkillTree();
            BuildCombatSystems();
            BuildProceduralWaveGenerator();
            EnemyPath path = BuildPath();
            BuildGround(path);
            BuildDecor(path);
            BuildSfx();
            BuildUi();
            BuildIcons();
            BuildProps();
            BuildImpactFx();
            BuildSpawner(path, waves, runner, grunt, brute, shielded, swarm, splitter, healer);
            GrayboxSpecialsBuilder.BuildSceneObject(s_anim);
            BuildTowers(path, bullet, homing, mortar, ricochet, mine, lineMaterial, square);
            BuildTowerPlacer(bullet, homing, mortar, ricochet, mine, lineMaterial, square);
            BuildReadmeLabel();
            BuildFlow();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[Graybox] Built {ScenePath}. Open it and press Play.");
        }

        // Last, so the flow controller finds the spawner and placer built above.
        private static void BuildFlow()
        {
            var go = new GameObject("GrayboxGameFlow");
            go.AddComponent<GrayboxSaveHost>(); // attaches the save file; without it nothing is persisted
            go.AddComponent<GrayboxGameFlow>();
            go.AddComponent<GrayboxFlowPlaceholderUi>();
        }

        private static void BuildEconomy()
        {
            var go = new GameObject("GrayboxEconomy");
            go.AddComponent<GrayboxEconomy>();
        }

        private static void BuildSkillTree()
        {
            var go = new GameObject("GrayboxSkillTree");
            go.AddComponent<GrayboxSkillTree>();
            go.AddComponent<GrayboxSkillTreeUI>();
        }

        private static void BuildCombatSystems()
        {
            var go = new GameObject("GrayboxCombatSystems");
            // Pixel-font popups (DamageNumbers/).
            PixelFontBuilder.Build();
            PixelFontBuilder.Configure(go.AddComponent<DamageNumberSpawner>(), DamageNumberPixelScale);
            // One setting for which art theme's fonts the scene uses; empty is the base set.
            PixelFontBuilder.ConfigureTheme(go.AddComponent<PixelFontTheme>());
            // WAVE 3, WAVE CLEAR, VICTORY and DEFEAT in the theme's banner styles.
            go.AddComponent<GrayboxWaveBanner>();
            // F6 steps through the font themes while playing.
            go.AddComponent<GrayboxFontThemeKey>();
            // Food, wave and time in the pixel font, and a combo counter for quick kills.
            go.AddComponent<GrayboxPixelHud>();
            go.AddComponent<GrayboxKillCombo>();
            go.AddComponent<GrayboxStatusCombos>();
            go.AddComponent<GrayboxBaseHealth>();
            go.AddComponent<GrayboxBaseHealthUI>();
        }

        // 2 font pixels per art pixel: at 1 the numbers are too small to read at this camera size.
        private const int DamageNumberPixelScale = 2;
        private const string IconRoot = SandboxRoot + "/Icons/PNG";

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

            var go = new GameObject("GrayboxIcons");
            GrayboxIcons holder = go.AddComponent<GrayboxIcons>();
            Apply(holder, so =>
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
            var go = new GameObject("GrayboxProceduralWaveGenerator");
            go.AddComponent<GrayboxProceduralWaveGenerator>();
        }

        private static void EnsureFolders()
        {
            CreateFolderIfMissing(SandboxRoot, "Graybox");
            CreateFolderIfMissing(GrayboxRoot, "Prefabs");
        }

        private static void CreateFolderIfMissing(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder($"{parent}/{child}"))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        // ---------- prefabs ----------

        private static GameObject BuildEnemyPrefab(
            string name, Sprite sprite, float health, float speed, Color color, float size, int bounty = 10)
        {
            var go = new GameObject(name);
            AddSprite(go, sprite, color, size, sortingOrder: 2);
            Animate(go);

            var collider = go.AddComponent<CircleCollider2D>();
            collider.radius = size * 0.5f;

            var body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;

            EnemyHealth enemyHealth = go.AddComponent<EnemyHealth>();
            Apply(enemyHealth, so =>
            {
                so.FindProperty("_maxHealth").floatValue = health;
                so.FindProperty("_bountyValue").intValue = bounty;
                so.FindProperty("_despawnDelay").floatValue = 0f;
            });

            EnemyMover mover = go.AddComponent<EnemyMover>();
            Apply(mover, so =>
            {
                so.FindProperty("_speed").floatValue = speed;
                so.FindProperty("_onReachEnd").enumValueIndex = (int)EnemyMover.EndBehaviour.Despawn;
            });

            go.AddComponent<EnemyHealthBar>();

            return SavePrefab(go, name);
        }

        private static GameObject BuildShieldedEnemyPrefab(
            string name, Sprite sprite, float health, float shield, float speed, Color color, float size, int bounty = 25)
        {
            var go = BuildBaseEnemyObject(name, sprite, health, speed, color, size, bounty);
            EnemyShield s = go.AddComponent<EnemyShield>();
            Apply(s, so =>
            {
                so.FindProperty("_maxShield").floatValue = shield;
                so.FindProperty("_regenRate").floatValue = 12f;
                so.FindProperty("_regenDelay").floatValue = 4.0f;
            });
            return SavePrefab(go, name);
        }

        private static GameObject BuildSplitterEnemyPrefab(
            string name, Sprite sprite, GameObject swarmPrefab, float health, float speed, Color color, float size, int bounty = 20)
        {
            var go = BuildBaseEnemyObject(name, sprite, health, speed, color, size, bounty);
            EnemySplitter splitter = go.AddComponent<EnemySplitter>();
            Apply(splitter, so =>
            {
                so.FindProperty("_miniAntPrefab").objectReferenceValue = swarmPrefab;
                so.FindProperty("_splitCount").intValue = 3;
            });
            return SavePrefab(go, name);
        }

        private static GameObject BuildHealerEnemyPrefab(
            string name, Sprite sprite, float health, float speed, Color color, float size, int bounty = 30)
        {
            var go = BuildBaseEnemyObject(name, sprite, health, speed, color, size, bounty);
            EnemyHealer healer = go.AddComponent<EnemyHealer>();
            Apply(healer, so =>
            {
                so.FindProperty("_healRadius").floatValue = 2.8f;
                so.FindProperty("_healInterval").floatValue = 1.4f;
                so.FindProperty("_healAmount").floatValue = 20f;
            });
            return SavePrefab(go, name);
        }

        internal static GameObject BuildBaseEnemyObject(
            string name, Sprite sprite, float health, float speed, Color color, float size, int bounty = 10)
        {
            var go = new GameObject(name);
            AddSprite(go, sprite, color, size, sortingOrder: 2);
            Animate(go);

            var collider = go.AddComponent<CircleCollider2D>();
            collider.radius = size * 0.5f;

            var body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;

            EnemyHealth enemyHealth = go.AddComponent<EnemyHealth>();
            Apply(enemyHealth, so =>
            {
                so.FindProperty("_maxHealth").floatValue = health;
                so.FindProperty("_bountyValue").intValue = bounty;
                so.FindProperty("_despawnDelay").floatValue = 0f;
            });

            EnemyMover mover = go.AddComponent<EnemyMover>();
            Apply(mover, so =>
            {
                so.FindProperty("_speed").floatValue = speed;
                so.FindProperty("_onReachEnd").enumValueIndex = (int)EnemyMover.EndBehaviour.Despawn;
            });

            go.AddComponent<EnemyHealthBar>();
            return go;
        }

        private static GameObject BuildBulletPrefab(Sprite sprite)
        {
            var go = new GameObject("GrayboxBullet");
            AddSprite(go, sprite, new Color(1f, 0.9f, 0.3f), 0.22f, sortingOrder: 3);
            AnimateVisual(go, "ProjLinear", "Fly");

            Projectile p = go.AddComponent<Projectile>();
            Apply(p, so =>
            {
                so.FindProperty("_flightMode").enumValueIndex = (int)FlightMode.Linear;
                so.FindProperty("_speed").floatValue = 9f;
                so.FindProperty("_damage").floatValue = 7f;
                so.FindProperty("_lifetime").floatValue = 3f;
                so.FindProperty("_hitRadius").floatValue = 0.28f;
            });

            return SavePrefab(go, "GrayboxBullet");
        }

        private static GameObject BuildHomingPrefab(Sprite sprite)
        {
            var go = new GameObject("GrayboxHomingBullet");
            AddSprite(go, sprite, new Color(0.4f, 1f, 0.6f), 0.24f, sortingOrder: 3);
            AnimateVisual(go, "ProjHoming", "Fly");

            Projectile p = go.AddComponent<Projectile>();
            Apply(p, so =>
            {
                so.FindProperty("_flightMode").enumValueIndex = (int)FlightMode.Homing;
                so.FindProperty("_speed").floatValue = 5.5f;
                // Deliberately low, so the shot visibly overshoots and loops back around.
                so.FindProperty("_turnRate").floatValue = 110f;
                so.FindProperty("_damage").floatValue = 9f;
                so.FindProperty("_lifetime").floatValue = 7f;
                so.FindProperty("_hitRadius").floatValue = 0.3f;
            });

            return SavePrefab(go, "GrayboxHomingBullet");
        }

        private static GameObject BuildFragmentPrefab(Sprite sprite)
        {
            var go = new GameObject("GrayboxFragment");
            AddSprite(go, sprite, new Color(1f, 0.6f, 0.2f), 0.16f, sortingOrder: 3);
            AnimateVisual(go, "ProjFragment", "Fly");

            Projectile p = go.AddComponent<Projectile>();
            Apply(p, so =>
            {
                so.FindProperty("_flightMode").enumValueIndex = (int)FlightMode.Linear;
                so.FindProperty("_speed").floatValue = 5f;
                so.FindProperty("_damage").floatValue = 3f;
                so.FindProperty("_lifetime").floatValue = 1.1f;
                so.FindProperty("_hitRadius").floatValue = 0.22f;
            });

            return SavePrefab(go, "GrayboxFragment");
        }

        private static GameObject BuildMortarPrefab(Sprite sprite, GameObject fragment, GameObject hazard)
        {
            var go = new GameObject("GrayboxMortar");
            AddSprite(go, sprite, new Color(0.9f, 0.4f, 0.1f), 0.3f, sortingOrder: 3);
            AnimateVisual(go, "ProjMortar", "Fly");

            ClusterProjectile p = go.AddComponent<ClusterProjectile>();
            Apply(p, so =>
            {
                so.FindProperty("_flightMode").enumValueIndex = (int)FlightMode.Lobbed;
                so.FindProperty("_speed").floatValue = 4.5f;
                so.FindProperty("_arcHeight").floatValue = 2.5f;
                so.FindProperty("_damage").floatValue = 6f;
                so.FindProperty("_lifetime").floatValue = 6f;
                so.FindProperty("_splashRadius").floatValue = 1.2f;
                so.FindProperty("_splashDamage").floatValue = 5f;
                so.FindProperty("_hitRadius").floatValue = 0.3f;
                so.FindProperty("_fragmentCount").intValue = 8;
                so.FindProperty("_spreadAngle").floatValue = 360f;
                so.FindProperty("_splitOn").enumValueIndex = (int)ClusterProjectile.SplitTrigger.OnImpact;
                so.FindProperty("_fragmentPrefab").objectReferenceValue = fragment.GetComponent<Projectile>();
                so.FindProperty("_groundHazardPrefab").objectReferenceValue = hazard.GetComponent<GroundHazard>();
                so.FindProperty("_groundHazardRadius").floatValue = 1.1f;
                so.FindProperty("_groundHazardDuration").floatValue = 4f;
                so.FindProperty("_groundHazardDamagePerTick").floatValue = 2f;
            });

            return SavePrefab(go, "GrayboxMortar");
        }

        private static GameObject BuildRicochetPrefab(Sprite sprite)
        {
            var go = new GameObject("GrayboxRicochet");
            AddSprite(go, sprite, new Color(0.6f, 0.8f, 1f), 0.24f, sortingOrder: 3);
            AnimateVisual(go, "ProjRicochet", "Fly");

            RicochetProjectile p = go.AddComponent<RicochetProjectile>();
            Apply(p, so =>
            {
                so.FindProperty("_flightMode").enumValueIndex = (int)FlightMode.Linear;
                so.FindProperty("_speed").floatValue = 10f;
                so.FindProperty("_damage").floatValue = 8f;
                so.FindProperty("_lifetime").floatValue = 4f;
                so.FindProperty("_hitRadius").floatValue = 0.28f;
                so.FindProperty("_maxBounces").intValue = 3;
                so.FindProperty("_bounceRange").floatValue = 3.5f;
                so.FindProperty("_damageFalloff").floatValue = 0.75f;
            });

            return SavePrefab(go, "GrayboxRicochet");
        }

        private static GameObject BuildHazardPrefab(Sprite sprite)
        {
            var go = new GameObject("GrayboxHazard");
            AddSprite(go, sprite, new Color(1f, 0.45f, 0.1f, 0.45f), 2.2f, sortingOrder: 0);
            // The puddle art's rim is 29 px from its centre; match the hazard's 1.1 radius.
            AnimateVisual(go, "FxHazard", "Burn", visualScale: 1.1f * 32f / 29f);

            go.AddComponent<CircleCollider2D>();
            GroundHazard hazard = go.AddComponent<GroundHazard>();
            Apply(hazard, so =>
            {
                so.FindProperty("_radius").floatValue = 1.1f;
                so.FindProperty("_damagePerTick").floatValue = 2f;
                so.FindProperty("_tickInterval").floatValue = 0.5f;
                so.FindProperty("_duration").floatValue = 4f;
            });

            return SavePrefab(go, "GrayboxHazard");
        }

        private static GameObject BuildMinePrefab(Sprite sprite)
        {
            var go = new GameObject("GrayboxMine");
            AddSprite(go, sprite, new Color(1f, 0.9f, 0.2f), 0.35f, sortingOrder: 2);
            if (s_anim != null)
            {
                GrayboxMineAnimator.Attach(go, s_anim);
            }

            go.AddComponent<CircleCollider2D>();
            ProximityMine mine = go.AddComponent<ProximityMine>();
            Apply(mine, so =>
            {
                so.FindProperty("_triggerRadius").floatValue = 0.7f;
                so.FindProperty("_splashRadius").floatValue = 1.6f;
                so.FindProperty("_damage").floatValue = 35f;
                so.FindProperty("_armDelay").floatValue = 0.4f;
            });

            return SavePrefab(go, "GrayboxMine");
        }

        private static MapWaveDataSO BuildWaveAsset(
            GameObject runner,
            GameObject grunt,
            GameObject brute,
            GameObject shielded,
            GameObject splitter,
            GameObject healer)
        {
            var asset = ScriptableObject.CreateInstance<MapWaveDataSO>();
            asset.mapId = "Graybox";
            asset.waves = new List<Wave>();

            const float Gap = 1.1f;

            // Wave 1: Eases in with Runners, Grunts, and Shielded Ants.
            // Wave 2: Introduces Splitter Ants (which split into 3 Swarm Ants on death).
            // Wave 3: Features Healer Support Ants protecting Brutes and Shielded Ants.
            // Wave 4: Combined assault with all enemy traits.
            var compositions = new[]
            {
                new[] { runner, runner, grunt, shielded, runner, grunt, shielded },
                new[] { grunt, runner, splitter, grunt, splitter, runner, grunt, splitter },
                new[] { brute, healer, shielded, brute, healer, grunt, shielded, brute },
                new[] { brute, splitter, healer, shielded, brute, splitter, healer, runner, brute, shielded },
            };

            for (int w = 0; w < compositions.Length; w++)
            {
                var wave = new Wave { waveName = $"Graybox Wave {w + 1}", enemies = new List<EnemySpawnEntry>() };
                foreach (GameObject prefab in compositions[w])
                {
                    wave.enemies.Add(new EnemySpawnEntry { enemyPrefab = prefab, delayBeforeNext = Gap });
                }

                asset.waves.Add(wave);
            }

            AssetDatabase.DeleteAsset(WaveAssetPath);
            AssetDatabase.CreateAsset(asset, WaveAssetPath);

            // Flush, force the import, then re-load. Assigning a freshly created
            // instance straight into a scene field serialized as {fileID: 0}: the
            // asset had no GUID registered yet, the reference came out null, and the
            // spawner silently found no waves. Nothing errored — the scene just did
            // nothing when you pressed Play.
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(WaveAssetPath, ImportAssetOptions.ForceSynchronousImport);

            var loaded = AssetDatabase.LoadAssetAtPath<MapWaveDataSO>(WaveAssetPath);
            if (loaded == null)
            {
                throw new System.InvalidOperationException(
                    $"[Graybox] Could not load the wave asset back from {WaveAssetPath}. " +
                    "Refusing to build a scene whose spawner would be silently empty.");
            }

            return loaded;
        }

        // ---------- scene ----------

        private static void BuildCamera()
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";

            // Framed on the path rather than the world origin. The route spans y -2.5
            // to 3.5, so its centre is 0.5; at size 8 more than half the view was empty
            // background and the projectiles were too small to follow.
            go.transform.position = new Vector3(0f, 0.5f, -10f);

            Camera camera = go.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5.5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.16f, 0.17f, 0.19f);

            go.AddComponent<AudioListener>();
        }

        private const string TileRoot = SandboxRoot + "/Tiles";

        /// <summary>
        /// Grass under everything and a dirt trail along the route. EnemyPath still draws its
        /// own flat road (sorting -10); this sits just above it with the same geometry.
        /// </summary>
        private static void BuildGround(EnemyPath path)
        {
            var grass = AssetDatabase.LoadAssetAtPath<Sprite>(TileRoot + "/GroundGrass.png");
            var road = AssetDatabase.LoadAssetAtPath<Sprite>(TileRoot + "/RoadDirt.png");
            var fill = AssetDatabase.LoadAssetAtPath<Sprite>(TileRoot + "/RoadDirtFill.png");
            if (grass == null || road == null || fill == null)
            {
                Debug.LogWarning($"[Graybox] Ground tiles missing in {TileRoot}; keeping the plain background.");
                return;
            }

            var root = new GameObject("Ground");
            Tiled(root.transform, "Grass", grass, new Vector3(0f, 0.5f, 0f), 0f, new Vector2(40f, 24f), -20);

            float width = new SerializedObject(path).FindProperty("_roadWidth").floatValue;
            for (int i = 0; i < Route.Length - 1; i++)
            {
                Vector3 a = new Vector3(Route[i].x + 0.5f, Route[i].y + 0.5f, 0f);
                Vector3 b = new Vector3(Route[i + 1].x + 0.5f, Route[i + 1].y + 0.5f, 0f);
                Vector3 delta = b - a;
                float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
                Tiled(root.transform, $"Road {i}", road, (a + b) * 0.5f, angle, new Vector2(delta.magnitude + width, width), -9);
            }

            // Plain dirt over each bend, so one segment's edge line doesn't run across the next.
            for (int i = 1; i < Route.Length - 1; i++)
            {
                Vector3 corner = new Vector3(Route[i].x + 0.5f, Route[i].y + 0.5f, 0f);
                Tiled(root.transform, $"Bend {i}", fill, corner, 0f, new Vector2(width, width), -8);
            }
        }

        /// <summary>Just the grass, for scenes with no trail (the weapon picker).</summary>
        internal static void BuildGrass(Vector3 centre, Vector2 size)
        {
            var grass = AssetDatabase.LoadAssetAtPath<Sprite>(TileRoot + "/GroundGrass.png");
            if (grass != null)
            {
                Tiled(new GameObject("Ground").transform, "Grass", grass, centre, 0f, size, -20);
            }
        }

        // How many of each piece to scatter, and which clips (variants) it has.
        private static readonly (string Key, string[] Clips, int Count)[] DecorTable =
        {
            ("DecorGrass", new[] { "A", "B", "C" }, 46),
            ("DecorFlower", new[] { "A", "B", "C" }, 12),
            ("DecorPebble", new[] { "A", "B", "C" }, 14),
            ("DecorLeaf", new[] { "A", "B", "C" }, 9),
            ("DecorTwig", new[] { "A", "B" }, 6),
            ("DecorMushroom", new[] { "A", "B" }, 5),
            ("DecorRoot", new[] { "A", "B" }, 3),
        };

        /// <summary>
        /// Scatters grass tufts, flowers, pebbles, leaves, twigs, mushrooms and roots over the
        /// grass, clear of the trail, and breaks up the trail's edges. Everything sits below the
        /// towers and enemies, so anything placed later simply draws over it. Seeded, so a
        /// rebuild lays the same map.
        /// </summary>
        private static void BuildDecor(EnemyPath path)
        {
            if (s_anim == null || s_anim.Find("DecorGrass") == null)
            {
                return;
            }

            var rng = new System.Random(7);
            float Range(float min, float max) => min + (float)rng.NextDouble() * (max - min);

            float width = new SerializedObject(path).FindProperty("_roadWidth").floatValue;
            var points = new Vector2[Route.Length];
            for (int i = 0; i < Route.Length; i++)
            {
                points[i] = new Vector2(Route[i].x + 0.5f, Route[i].y + 0.5f);
            }

            float DistanceToTrail(Vector2 p)
            {
                float best = float.PositiveInfinity;
                for (int i = 0; i < points.Length - 1; i++)
                {
                    Vector2 a = points[i], ab = points[i + 1] - points[i];
                    float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
                    best = Mathf.Min(best, Vector2.Distance(p, a + ab * t));
                }

                return best;
            }

            var root = new GameObject("Decor");
            foreach ((string key, string[] clips, int count) in DecorTable)
            {
                SpriteAnimSet set = s_anim.Find(key);
                if (set == null)
                {
                    continue;
                }

                int placed = 0;
                for (int attempt = 0; attempt < count * 20 && placed < count; attempt++)
                {
                    var at = new Vector2(Range(-10.5f, 10.5f), Range(-4.8f, 5.8f));
                    if (DistanceToTrail(at) < width * 0.5f + 0.6f)
                    {
                        continue;
                    }

                    string clip = clips[rng.Next(clips.Length)];
                    PlaceDecor(root.transform, set, key, clip, at, 0f, rng.Next(2) == 0, -6);
                    placed++;
                }
            }

            // Trail edges: a piece every unit or so along both sides of every stretch.
            SpriteAnimSet edges = s_anim.Find("DecorTrailEdge");
            if (edges == null)
            {
                return;
            }

            string[] edgeClips = { "A", "A", "B", "C" }; // grass overhang twice as often
            for (int i = 0; i < points.Length - 1; i++)
            {
                Vector2 delta = points[i + 1] - points[i];
                Vector2 along = delta.normalized;
                var normal = new Vector2(-along.y, along.x);
                float angle = Mathf.Atan2(along.y, along.x) * Mathf.Rad2Deg;

                for (float d = 1f; d < delta.magnitude - 0.5f; d += 1f)
                {
                    foreach (int side in new[] { 1, -1 })
                    {
                        if (rng.NextDouble() < 0.35)
                        {
                            continue;
                        }

                        // The art has the trail under it; on the far side turn it round.
                        Vector2 at = points[i] + along * (d + Range(-0.2f, 0.2f)) + normal * (side * width * 0.5f);
                        PlaceDecor(root.transform, edges, "DecorTrailEdge", edgeClips[rng.Next(edgeClips.Length)],
                            at, side > 0 ? angle : angle + 180f, false, -7);
                    }
                }
            }
        }

        private static void PlaceDecor(
            Transform parent, SpriteAnimSet set, string key, string clipName, Vector2 at, float angle, bool flip, int order)
        {
            SpriteAnimClip clip = set.Find(clipName);
            if (clip == null || clip.Frames.Length == 0)
            {
                return;
            }

            var go = new GameObject($"{key} {clipName}");
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, 0f, angle));
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = clip.Frames[0];
            renderer.flipX = flip;
            renderer.sortingOrder = order;

            // Only the pieces that move (grass, flowers) need a player.
            if (clip.Frames.Length > 1)
            {
                go.AddComponent<SpriteClipPlayer>().Configure(s_anim, key, clipName);
            }
        }

        private static void Tiled(Transform parent, string name, Sprite sprite, Vector3 position, float angle, Vector2 size, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, angle));
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.size = size;
            renderer.sortingOrder = order;
        }

        private const string UiRoot = SandboxRoot + "/Ui";
        private const string FontPath = SandboxRoot + "/Fonts/TTF/HoldTheHillPixel-Regular.ttf";

        /// <summary>The pixel skin every HUD script switches to (see GrayboxUi).</summary>
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

            var go = new GameObject("GrayboxUi");
            GrayboxUi ui = go.AddComponent<GrayboxUi>();
            Apply(ui, so =>
            {
                so.FindProperty("_font").objectReferenceValue = font;
                so.FindProperty("_panel").objectReferenceValue = panel;
                so.FindProperty("_button").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(UiRoot + "/Button.png");
                so.FindProperty("_buttonHover").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(UiRoot + "/ButtonHover.png");
                so.FindProperty("_buttonPressed").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(UiRoot + "/ButtonPressed.png");
            });
        }

        /// <summary>The hill at the end of the trail and the burrow at the start.</summary>
        private static void BuildProps()
        {
            if (s_anim == null)
            {
                return;
            }

            Vector2Int first = Route[0];
            Vector2Int last = Route[Route.Length - 1];

            // Hill: drawn above the enemies so they vanish into it; nudged in from the screen edge.
            MakeProp("Hill", "PropHill", "Healthy", new Vector3(last.x - 0.3f, last.y + 0.5f, 0f), 4, GrayboxPropVisuals.Kind.Hill);
            // Burrow: under the enemies, so they climb out of it.
            MakeProp("Burrow", "PropBurrow", "Idle", new Vector3(first.x + 0.5f, first.y + 0.5f, 0f), 1, GrayboxPropVisuals.Kind.Burrow);
        }

        private static void MakeProp(string name, string key, string clip, Vector3 position, int order, GrayboxPropVisuals.Kind kind)
        {
            if (s_anim.Find(key) == null)
            {
                return;
            }

            var go = new GameObject(name);
            go.transform.position = position;
            go.AddComponent<SpriteRenderer>().sortingOrder = order;
            go.AddComponent<SpriteClipPlayer>().Configure(s_anim, key, clip);
            go.AddComponent<GrayboxPropVisuals>().Configure(kind);
        }

        private static void BuildImpactFx()
        {
            if (s_anim == null)
            {
                return;
            }

            new GameObject("GrayboxImpactFx").AddComponent<GrayboxImpactFx>().Configure(s_anim);
        }

        private static void BuildSfx()
        {
            GrayboxSfxBank bank = GrayboxSfxBankBuilder.Build();
            var go = new GameObject("GrayboxSfx");
            GrayboxSfx sfx = go.AddComponent<GrayboxSfx>();
            Apply(sfx, so => so.FindProperty("_bank").objectReferenceValue = bank);
        }

        private static EnemyPath BuildPath()
        {
            var go = new GameObject("Enemy Path");
            EnemyPath path = go.AddComponent<EnemyPath>();

            for (int i = 0; i < Route.Length; i++)
            {
                var waypoint = new GameObject($"Waypoint {i}");
                waypoint.transform.SetParent(go.transform);
                waypoint.transform.position = new Vector3(Route[i].x + 0.5f, Route[i].y + 0.5f, 0f);
            }

            return path;
        }

        private static void BuildSpawner(
            EnemyPath path,
            MapWaveDataSO waves,
            GameObject runner,
            GameObject grunt,
            GameObject brute,
            GameObject shielded,
            GameObject swarm,
            GameObject splitter,
            GameObject healer)
        {
            // Re-load rather than trusting the reference passed in. The asset is created
            // before EditorSceneManager.NewScene, and opening a new scene unloads assets
            // nothing is holding onto - which quietly turns that reference into null.
            waves = AssetDatabase.LoadAssetAtPath<MapWaveDataSO>(WaveAssetPath);
            if (waves == null)
            {
                throw new System.InvalidOperationException(
                    $"[Graybox] Wave asset missing at {WaveAssetPath} when wiring the spawner.");
            }

            var go = new GameObject("Enemy Spawner");
            go.transform.position = new Vector3(Route[0].x + 0.5f, Route[0].y + 0.5f, 0f);

            var container = new GameObject("Spawned Enemies");

            EnemySpawner spawner = go.AddComponent<EnemySpawner>();
            Apply(spawner, so =>
            {
                so.FindProperty("defaultSpawnPoint").objectReferenceValue = go.transform;
                so.FindProperty("enemyContainer").objectReferenceValue = container.transform;
                so.FindProperty("activeMapId").stringValue = "Graybox";
                so.FindProperty("activeMapConfigOverride").objectReferenceValue = waves;
                so.FindProperty("autoStartFirstWave").boolValue = true;
                so.FindProperty("requireEnemiesClearedBeforeNextWave").boolValue = false;
            });

            // Read it straight back. An object reference that fails to resolve writes
            // {fileID: 0} without complaining, which produces a scene that loads fine
            // and then does absolutely nothing.
            var check = new SerializedObject(spawner);
            if (check.FindProperty("activeMapConfigOverride").objectReferenceValue == null)
            {
                throw new System.InvalidOperationException(
                    "[Graybox] The wave asset did not survive assignment to the spawner. " +
                    "The scene would spawn no enemies.");
            }

            // Custom horde spawner for stress-testing setups
            GrayboxCustomSpawner customSpawner = go.AddComponent<GrayboxCustomSpawner>();
            Apply(customSpawner, so =>
            {
                so.FindProperty("_runnerPrefab").objectReferenceValue = runner;
                so.FindProperty("_gruntPrefab").objectReferenceValue = grunt;
                so.FindProperty("_brutePrefab").objectReferenceValue = brute;
                so.FindProperty("_shieldedPrefab").objectReferenceValue = shielded;
                so.FindProperty("_swarmPrefab").objectReferenceValue = swarm;
                so.FindProperty("_splitterPrefab").objectReferenceValue = splitter;
                so.FindProperty("_healerPrefab").objectReferenceValue = healer;
                so.FindProperty("_spawnPoint").objectReferenceValue = go.transform;
                so.FindProperty("_enemyContainer").objectReferenceValue = container.transform;
            });

            // Without this the spawner's living-enemy count never goes down.
            go.AddComponent<EnemySpawnerBridge>();

            // The spawner waits to be prompted between waves, so the scene needs something
            // that can prompt it. The HUD also reports kills and leaks while tuning.
            go.AddComponent<GrayboxHud>();
            go.AddComponent<GrayboxMenuManager>();
            go.AddComponent<GrayboxAchievements>();
        }

        private static void BuildTowers(
            EnemyPath path,
            GameObject bullet,
            GameObject homing,
            GameObject mortar,
            GameObject ricochet,
            GameObject mine,
            Material lineMaterial,
            Sprite square)
        {
            // Positions are all at least 2 units clear of the road so TowerPlacer would
            // also consider them buildable.
            // Fire intervals are tighter than a shipping game would want. In the first
            // build nothing died for the opening six seconds, which reads as "the towers
            // are broken" even though they were working. Enemy health goes up to
            // compensate, so the defence is not made easier overall.
            MakeProjectileTower("Tower_Linear_First", new Vector2(-7f, 4.5f), square,
                bullet, TargetingPriority.First, 3.6f, 0.45f, new Color(0.4f, 0.7f, 1f), lineMaterial);

            MakeProjectileTower("Tower_Homing_Closest", new Vector2(-6f, 0.5f), square,
                homing, TargetingPriority.Closest, 3.8f, 0.9f, new Color(0.4f, 1f, 0.6f), lineMaterial);

            MakeProjectileTower("Tower_Mortar_First", new Vector2(-1.5f, 0f), square,
                mortar, TargetingPriority.First, 4.2f, 1.6f, new Color(0.9f, 0.5f, 0.2f), lineMaterial);

            MakeProjectileTower("Tower_Ricochet_Strongest", new Vector2(-1.5f, -5f), square,
                ricochet, TargetingPriority.Strongest, 3.8f, 1f, new Color(0.6f, 0.8f, 1f), lineMaterial);

            GameObject chainTower = MakeProjectileTower("Tower_Chain_Weakest", new Vector2(5f, 1f), square,
                null, TargetingPriority.Weakest, 3.8f, 1.2f, new Color(0.5f, 0.85f, 1f), lineMaterial);
            AddChainLightning(chainTower, lineMaterial);

            GameObject beamTower = MakeProjectileTower("Tower_Beam_Closest", new Vector2(6f, 5.5f), square,
                null, TargetingPriority.Closest, 4f, 1f, new Color(1f, 0.5f, 0.3f), lineMaterial);
            AddBeam(beamTower, lineMaterial);

            // The orbiting field hurts things by touching them, so what matters is whether
            // the wisps physically overlap the road — the tower's own range is irrelevant.
            // At the old spot (-1.5,-5) the path was 2.5 away and the wisps orbited at 1.5,
            // so they fell a full unit short and this tower could never hit anything.
            // 1.5 out from the road, with a 1.6 orbit, puts them just over it.
            GameObject orbitTower = MakeProjectileTower("Tower_Orbit", new Vector2(0f, -4f), square,
                null, TargetingPriority.Closest, 2.5f, 1f, new Color(0.7f, 0.6f, 1f), lineMaterial);

            OrbitingDamageField field = orbitTower.AddComponent<OrbitingDamageField>();
            Apply(field, so =>
            {
                so.FindProperty("_radius").floatValue = 1.6f;
                so.FindProperty("_orbiterCount").intValue = 3;
                so.FindProperty("_angularSpeed").floatValue = 150f;
                so.FindProperty("_contactDamage").floatValue = 6f;
                so.FindProperty("_hitCooldown").floatValue = 0.4f;
                so.FindProperty("_orbiterRadius").floatValue = 0.35f;
            });

            GameObject frostTower = MakeProjectileTower("Tower_Frost_Closest", new Vector2(-4f, 4.5f), square,
                null, TargetingPriority.Closest, 3.5f, 1.2f, new Color(0.4f, 0.85f, 1f), lineMaterial);
            frostTower.AddComponent<FrostAuraTower>();

            GameObject knockbackTower = MakeProjectileTower("Tower_Knockback_Closest", new Vector2(2f, -5.5f), square,
                null, TargetingPriority.Closest, 3.0f, 2.0f, new Color(1f, 0.6f, 0.2f), lineMaterial);
            knockbackTower.AddComponent<KnockbackTower>();

            GameObject mineTower = MakeProjectileTower("Tower_MineLayer", new Vector2(0f, 5f), square,
                null, TargetingPriority.Closest, 4.0f, 2.8f, new Color(0.9f, 0.9f, 0.3f), lineMaterial);
            MineLayerTower mineLayer = mineTower.AddComponent<MineLayerTower>();
            Apply(mineLayer, so => so.FindProperty("_minePrefab").objectReferenceValue = mine);
        }

        private static GameObject MakeProjectileTower(
            string name,
            Vector2 position,
            Sprite sprite,
            GameObject projectile,
            TargetingPriority priority,
            float range,
            float fireInterval,
            Color color,
            Material lineMaterial)
        {
            var go = new GameObject(name);
            go.transform.position = position;
            AddSprite(go, sprite, color, 0.8f, sortingOrder: 1);
            Animate(go);

            Tower tower = go.AddComponent<Tower>();
            Apply(tower, so =>
            {
                so.FindProperty("_range").floatValue = range;
                so.FindProperty("_fireInterval").floatValue = fireInterval;
                so.FindProperty("_priority").enumValueIndex = (int)priority;

                if (projectile != null)
                {
                    so.FindProperty("_projectilePrefab").objectReferenceValue = projectile.GetComponent<Projectile>();
                }
            });

            go.AddComponent<TowerTargetVisualizer>();

            return go;
        }

        private static void AddChainLightning(GameObject tower, Material lineMaterial)
        {
            ChainLightning chain = tower.AddComponent<ChainLightning>();
            ConfigureLine(tower.GetComponent<LineRenderer>(), lineMaterial, new Color(0.6f, 0.9f, 1f), 0.08f);
            GrayboxLineScroll.Dress(tower.GetComponent<LineRenderer>(), LineMaterial("GrayboxBolt", "LineBolt"), 0.25f, -6f, 1f);

            Apply(chain, so =>
            {
                so.FindProperty("_maxTargets").intValue = 4;
                so.FindProperty("_jumpRange").floatValue = 3.5f;
                so.FindProperty("_damage").floatValue = 10f;
            });

            // The tower still needs a firing cadence even with no projectile prefab.
            Apply(tower.GetComponent<Tower>(), so => so.FindProperty("_chainLightning").objectReferenceValue = chain);
        }

        private static void AddBeam(GameObject tower, Material lineMaterial)
        {
            ContinuousBeam beam = tower.AddComponent<ContinuousBeam>();
            ConfigureLine(tower.GetComponent<LineRenderer>(), lineMaterial, new Color(1f, 0.55f, 0.25f), 0.15f);
            GrayboxLineScroll.Dress(tower.GetComponent<LineRenderer>(), LineMaterial("GrayboxBeam", "LineBeam"), 0.25f, -3f, 0f);

            Apply(beam, so =>
            {
                so.FindProperty("_range").floatValue = 4f;
                so.FindProperty("_baseDamagePerSecond").floatValue = 5f;
                so.FindProperty("_rampPerSecond").floatValue = 5f;
                so.FindProperty("_maxDamagePerSecond").floatValue = 25f;
            });

            Apply(tower.GetComponent<Tower>(), so => so.FindProperty("_continuousBeam").objectReferenceValue = beam);
        }

        /// <summary>
        /// A material that tiles one of the Tiles/Line*.png strips along a LineRenderer. Returns
        /// null if the texture is missing, and the line then keeps its flat colour.
        /// </summary>
        private static Material LineMaterial(string materialName, string textureName)
        {
            string path = $"{GrayboxRoot}/{materialName}.mat";
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TileRoot}/{textureName}.png");
            if (texture == null)
            {
                return null;
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit");
                material = new Material(shader) { name = materialName };
                AssetDatabase.CreateAsset(material, path);
            }

            material.mainTexture = texture;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ConfigureLine(LineRenderer line, Material material, Color color, float width)
        {
            if (line == null)
            {
                return;
            }

            line.material = material;
            line.startColor = color;
            line.endColor = color;
            line.startWidth = width;
            line.endWidth = width;
            line.numCapVertices = 2;
            line.sortingOrder = 4;
        }

        private static void BuildTowerPlacer(
            GameObject bullet,
            GameObject homing,
            GameObject mortar,
            GameObject ricochet,
            GameObject mine,
            Material lineMaterial,
            Sprite square)
        {
            var go = new GameObject("Tower Placer & Inspector");
            GrayboxTowerPlacer placer = go.AddComponent<GrayboxTowerPlacer>();
            Apply(placer, so =>
            {
                so.FindProperty("_bulletPrefab").objectReferenceValue = bullet;
                so.FindProperty("_homingPrefab").objectReferenceValue = homing;
                so.FindProperty("_mortarPrefab").objectReferenceValue = mortar;
                so.FindProperty("_ricochetPrefab").objectReferenceValue = ricochet;
                so.FindProperty("_minePrefab").objectReferenceValue = mine;
                so.FindProperty("_lineMaterial").objectReferenceValue = lineMaterial;
                so.FindProperty("_boltMaterial").objectReferenceValue = LineMaterial("GrayboxBolt", "LineBolt");
                so.FindProperty("_beamMaterial").objectReferenceValue = LineMaterial("GrayboxBeam", "LineBeam");
                so.FindProperty("_squareSprite").objectReferenceValue = square;
                so.FindProperty("_animLibrary").objectReferenceValue = s_anim;
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

        private static Material CreateLineMaterial()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(LineMaterialPath);
            if (existing != null)
            {
                return existing;
            }

            Shader shader = Shader.Find("Sprites/Default")
                            ?? Shader.Find("Universal Render Pipeline/Unlit")
                            ?? Shader.Find("Unlit/Color");

            var material = new Material(shader) { name = "GrayboxLine" };
            AssetDatabase.CreateAsset(material, LineMaterialPath);
            return material;
        }

        private static void AddSprite(GameObject go, Sprite sprite, Color color, float size, int sortingOrder)
        {
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = new Vector2(size, size);
        }

        internal static GameObject SavePrefab(GameObject instance, string fileName)
        {
            // Two of the same script on one prefab is always a mistake here, and a quiet one: a
            // [RequireComponent] auto-added a default EnemyHealth ahead of the configured one, and
            // every enemy ran on 30 HP. Refuse to save rather than build a broken graybox.
            foreach (IGrouping<System.Type, MonoBehaviour> group in instance.GetComponents<MonoBehaviour>().GroupBy(c => c.GetType()))
            {
                if (group.Count() > 1)
                {
                    throw new System.InvalidOperationException(
                        $"[Graybox] {fileName} has {group.Count()} {group.Key.Name} components. One was probably auto-added by a [RequireComponent].");
                }
            }

            string path = $"{PrefabRoot}/{fileName}.prefab";
            GameObject asset = PrefabUtility.SaveAsPrefabAsset(instance, path);
            Object.DestroyImmediate(instance);
            return asset;
        }

        // Which .aseprite each generated object animates with. Anything missing here (or a
        // missing library) keeps its flat placeholder shape, so the scene still builds.
        private static readonly Dictionary<string, string> AnimKeys = new Dictionary<string, string>
        {
            { "GrayboxRunner", "EnemyRunner" },
            { "GrayboxGrunt", "EnemyGrunt" },
            { "GrayboxBrute", "EnemyBrute" },
            { "GrayboxShielded", "EnemyShielded" },
            { "GrayboxSwarm", "EnemySwarm" },
            { "GrayboxSplitter", "EnemySplitter" },
            { "GrayboxHealer", "EnemyHealer" },
            { "GrayboxTitanBeetle", "EnemyBoss" },
            { "GrayboxMantisQueen", "EnemyMantisQueen" },
            { "GrayboxHornet", "EnemyHornet" },
            { "GrayboxOrbWeaver", "EnemyOrbWeaver" },
            { "GrayboxBoulderBug", "EnemyBoulderBug" },
            { "GrayboxRivalQueen", "EnemyRivalQueen" },
            { "GrayboxWasp", "EnemyWasp" },
            { "GrayboxSpider", "EnemySpider" },
            { "GrayboxSilverfish", "EnemySilverfish" },
            { "GrayboxThiefAnt", "EnemyThiefAnt" },
            { "GrayboxBombardier", "EnemyBombardier" },
            { "Tower_Linear_First", "TowerLinear" },
            { "Tower_Homing_Closest", "TowerHoming" },
            { "Tower_Mortar_First", "TowerMortar" },
            { "Tower_Ricochet_Strongest", "TowerRicochet" },
            { "Tower_Chain_Weakest", "TowerChain" },
            { "Tower_Beam_Closest", "TowerBeam" },
            { "Tower_Orbit", "TowerOrbit" },
            { "Tower_Frost_Closest", "TowerFrostAura" },
            { "Tower_Knockback_Closest", "TowerKnockback" },
            { "Tower_MineLayer", "TowerMineLayer" },
        };

        private static SpriteAnimLibrary s_anim;

        /// <summary>
        /// Swaps a prefab's placeholder circle for an animated sprite. With a visual scale the
        /// sprite goes on a child, so scaling it never touches the root's collider.
        /// </summary>
        private static void AnimateVisual(GameObject go, string key, string clip, float visualScale = 1f)
        {
            if (s_anim == null || s_anim.Find(key) == null)
            {
                return;
            }

            var rootRenderer = go.GetComponent<SpriteRenderer>();
            GameObject target = go;
            if (!Mathf.Approximately(visualScale, 1f))
            {
                target = new GameObject("Visual");
                target.transform.SetParent(go.transform, false);
                target.transform.localScale = Vector3.one * visualScale;
                var childRenderer = target.AddComponent<SpriteRenderer>();
                childRenderer.sortingOrder = rootRenderer != null ? rootRenderer.sortingOrder : 0;
                if (rootRenderer != null)
                {
                    Object.DestroyImmediate(rootRenderer);
                }
            }
            else if (rootRenderer != null)
            {
                rootRenderer.drawMode = SpriteDrawMode.Simple;
                rootRenderer.color = Color.white;
                go.transform.localScale = Vector3.one; // undo the rescale Unity applies when leaving Sliced
            }

            target.AddComponent<SpriteClipPlayer>().Configure(s_anim, key, clip);
        }

        private static void Animate(GameObject go)
        {
            if (s_anim == null || !AnimKeys.TryGetValue(go.name, out string key))
            {
                return;
            }

            if (go.GetComponent<Tower>() != null || go.name.StartsWith("Tower_"))
            {
                GrayboxTowerAnimator.Attach(go, s_anim, key);
            }
            else
            {
                GrayboxEnemyAnimator.Attach(go, s_anim, key);
            }
        }

        internal static void Apply(Object target, System.Action<SerializedObject> edit)
        {
            var so = new SerializedObject(target);
            edit(so);
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
