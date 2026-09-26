using System.Collections.Generic;
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

            // Fast and fragile, the baseline, and a slow wall. The spread is what makes
            // Strongest and Weakest targeting visibly different from Closest.
            GameObject runner = BuildEnemyPrefab(
                "GrayboxRunner", circle, health: 90f, speed: 2.4f, new Color(0.95f, 0.75f, 0.2f), 0.45f);
            GameObject grunt = BuildEnemyPrefab(
                "GrayboxGrunt", circle, health: 240f, speed: 1.4f, new Color(0.85f, 0.25f, 0.25f), 0.6f);
            GameObject brute = BuildEnemyPrefab(
                "GrayboxBrute", circle, health: 900f, speed: 0.85f, new Color(0.5f, 0.1f, 0.35f), 0.95f);

            MapWaveDataSO waves = BuildWaveAsset(runner, grunt, brute);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildCamera();
            EnemyPath path = BuildPath();
            BuildSpawner(path, waves);
            BuildTowers(path, bullet, homing, mortar, ricochet, lineMaterial, square);
            BuildReadmeLabel();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[Graybox] Built {ScenePath}. Open it and press Play.");
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

        /// <summary>
        /// Builds one enemy archetype.
        /// </summary>
        /// <remarks>
        /// Three of these exist rather than one, for two reasons. Towers set to Strongest
        /// or Weakest cannot be told apart from Closest when every enemy has identical
        /// health, so a single archetype leaves two of the seven towers untestable. And a
        /// full pass of the path is worth roughly 290 single-target damage, so a 40 HP
        /// enemy dies about 14% along it — at the first tower, with the other six never
        /// firing a shot.
        /// </remarks>
        private static GameObject BuildEnemyPrefab(
            string name, Sprite sprite, float health, float speed, Color color, float size)
        {
            var go = new GameObject(name);
            AddSprite(go, sprite, color, size, sortingOrder: 2);

            var collider = go.AddComponent<CircleCollider2D>();
            collider.radius = size * 0.5f;

            // Kinematic body so Unity treats these as moving colliders rather than
            // rebuilding the static collider tree every frame.
            var body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;

            EnemyHealth enemyHealth = go.AddComponent<EnemyHealth>();
            Apply(enemyHealth, so =>
            {
                so.FindProperty("_maxHealth").floatValue = health;
                so.FindProperty("_despawnDelay").floatValue = 0f;
            });

            EnemyMover mover = go.AddComponent<EnemyMover>();
            Apply(mover, so =>
            {
                so.FindProperty("_speed").floatValue = speed;
                so.FindProperty("_onReachEnd").enumValueIndex = (int)EnemyMover.EndBehaviour.Despawn;
            });

            return SavePrefab(go, name);
        }

        private static GameObject BuildBulletPrefab(Sprite sprite)
        {
            var go = new GameObject("GrayboxBullet");
            AddSprite(go, sprite, new Color(1f, 0.9f, 0.3f), 0.22f, sortingOrder: 3);

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

        private static MapWaveDataSO BuildWaveAsset(GameObject runner, GameObject grunt, GameObject brute)
        {
            var asset = ScriptableObject.CreateInstance<MapWaveDataSO>();
            asset.mapId = "Graybox";
            asset.waves = new List<Wave>();

            // 1.5s apart rather than 0.8s. The seven towers put out roughly 50 damage a
            // second between them once they are all engaged, so a faster trickle than this
            // outruns the defence regardless of how much health anything has.
            const float Gap = 1.1f;

            // Wave 1 eases in, wave 2 mixes archetypes so the targeting priorities have
            // something to disagree about, wave 3 leans on the brutes.
            var compositions = new[]
            {
                new[] { runner, runner, grunt, runner, grunt, grunt },
                new[] { grunt, runner, brute, grunt, runner, grunt, brute, runner },
                new[] { brute, grunt, runner, brute, grunt, grunt, brute, runner, brute, grunt },
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

        private static void BuildSpawner(EnemyPath path, MapWaveDataSO waves)
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

            // Without this the spawner's living-enemy count never goes down.
            go.AddComponent<EnemySpawnerBridge>();

            // The spawner waits to be prompted between waves, so the scene needs something
            // that can prompt it. The HUD also reports kills and leaks while tuning.
            go.AddComponent<GrayboxHud>();
        }

        private static void BuildTowers(
            EnemyPath path,
            GameObject bullet,
            GameObject homing,
            GameObject mortar,
            GameObject ricochet,
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

            return go;
        }

        private static void AddChainLightning(GameObject tower, Material lineMaterial)
        {
            ChainLightning chain = tower.AddComponent<ChainLightning>();
            ConfigureLine(tower.GetComponent<LineRenderer>(), lineMaterial, new Color(0.6f, 0.9f, 1f), 0.08f);

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

            Apply(beam, so =>
            {
                so.FindProperty("_range").floatValue = 4f;
                so.FindProperty("_baseDamagePerSecond").floatValue = 5f;
                so.FindProperty("_rampPerSecond").floatValue = 5f;
                so.FindProperty("_maxDamagePerSecond").floatValue = 25f;
            });

            Apply(tower.GetComponent<Tower>(), so => so.FindProperty("_continuousBeam").objectReferenceValue = beam);
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

        private static GameObject SavePrefab(GameObject instance, string fileName)
        {
            string path = $"{PrefabRoot}/{fileName}.prefab";
            GameObject asset = PrefabUtility.SaveAsPrefabAsset(instance, path);
            Object.DestroyImmediate(instance);
            return asset;
        }

        private static void Apply(Object target, System.Action<SerializedObject> edit)
        {
            var so = new SerializedObject(target);
            edit(so);
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
