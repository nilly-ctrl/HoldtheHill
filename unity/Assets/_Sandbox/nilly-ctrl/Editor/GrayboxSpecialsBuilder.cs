using System.Collections.Generic;
using HoldTheHill.Features.Enemies;
using UnityEditor;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// The bosses and special enemies for the graybox scene: their prefabs, the waves that show
    /// them off, and the scene object that holds them. Called from <see cref="GrayboxBuilder"/>.
    /// </summary>
    /// <remarks>
    /// Kept apart from GrayboxBuilder so the original seven enemies and four waves are built
    /// exactly as before, and this can be read on its own.
    /// </remarks>
    internal static class GrayboxSpecialsBuilder
    {
        private const string PrefabRoot = "Assets/_Sandbox/nilly-ctrl/Graybox/Prefabs";

        // Order is the order of the spawn panel: bosses first.
        private static readonly (string Id, string Label, bool Boss)[] Catalog =
        {
            ("TitanBeetle", "Titan Beetle", true),
            ("MantisQueen", "Mantis Queen", true),
            ("Hornet", "Hornet", true),
            ("OrbWeaver", "Orb-Weaver", true),
            ("BoulderBug", "Boulder Bug", true),
            ("RivalQueen", "Rival Queen", true),
            ("Wasp", "Wasp", false),
            ("Spider", "Spider", false),
            ("Silverfish", "Silverfish", false),
            ("ThiefAnt", "Thief Ant", false),
            ("Bombardier", "Bombardier Beetle", false),
        };

        private const string BossDataRoot = "Assets/_Sandbox/nilly-ctrl/Graybox/Data/Bosses";
        private const string BossBaseName = "GrayboxBossBase";

        private static GameObject s_bossBase;

        // A boss is an enemy with a GrayboxBoss on it, which is what the banner listens for.
        private static GameObject BossBase(Sprite sprite)
        {
            if (s_bossBase != null)
            {
                return s_bossBase;
            }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(GrayboxBuilder.EnemyBase(sprite));
            go.name = BossBaseName;
            go.AddComponent<GrayboxBoss>();
            s_bossBase = GrayboxBuilder.SavePrefab(go, BossBaseName);
            return s_bossBase;
        }

        // Made once: the texts can then be edited in the Inspector and survive a rebuild.
        private static GrayboxBossData BossData(string id, string label)
        {
            GrayboxBuilder.CreateFolderIfMissing("Assets/_Sandbox/nilly-ctrl/Graybox", "Data");
            GrayboxBuilder.CreateFolderIfMissing("Assets/_Sandbox/nilly-ctrl/Graybox/Data", "Bosses");

            string path = $"{BossDataRoot}/{id}Data.asset";
            var data = AssetDatabase.LoadAssetAtPath<GrayboxBossData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<GrayboxBossData>();
                AssetDatabase.CreateAsset(data, path);
                GrayboxBuilder.Apply(data, so =>
                {
                    so.FindProperty("_id").stringValue = id;
                    so.FindProperty("_displayName").stringValue = label;
                    so.FindProperty("_bannerText").stringValue = label.ToUpperInvariant().Replace('-', ' ');
                });
            }

            return data;
        }

        private static string PathOf(string id) => $"{PrefabRoot}/Graybox{id}.prefab";

        private static GameObject Load(string id) => AssetDatabase.LoadAssetAtPath<GameObject>(PathOf(id));

        /// <summary>Builds every special prefab. The grunt is what eggs hatch into.</summary>
        public static void BuildPrefabs(Sprite sprite, GameObject grunt, SpriteAnimLibrary anim)
        {
            s_bossBase = null;

            // ---- regular enemies
            Special<GrayboxWasp>("Wasp", sprite, health: 110f, speed: 2.2f, size: 0.55f, bounty: 12, breach: 6f, facePath: false);
            Special<GrayboxSpider>("Spider", sprite, health: 200f, speed: 1.3f, size: 0.6f, bounty: 16, breach: 8f);
            Special<GrayboxSilverfish>("Silverfish", sprite, health: 1f, speed: 4.2f, size: 0.4f, bounty: 3, breach: 3f);
            GameObject thief = Special<GrayboxThiefAnt>("ThiefAnt", sprite, health: 120f, speed: 2.0f, size: 0.45f, bounty: 10, breach: 0f,
                end: EnemyMover.EndBehaviour.Stop);
            Special<GrayboxBombardier>("Bombardier", sprite, health: 260f, speed: 1.1f, size: 0.65f, bounty: 18, breach: 10f);

            // ---- bosses
            Special<GrayboxTitanBeetle>("TitanBeetle", sprite, health: 3200f, speed: 0.6f, size: 1.5f, bounty: 150, breach: 50f);

            GameObject egg = BuildEggSac(anim, grunt);
            Special<GrayboxMantisQueen>("MantisQueen", sprite, health: 2200f, speed: 0.7f, size: 1.4f, bounty: 150, breach: 45f,
                extra: so => so.FindProperty("_eggPrefab").objectReferenceValue = egg);

            Special<GrayboxHornet>("Hornet", sprite, health: 1800f, speed: 1.6f, size: 1.1f, bounty: 150, breach: 40f, facePath: false);
            Special<GrayboxOrbWeaver>("OrbWeaver", sprite, health: 2400f, speed: 0.75f, size: 1.3f, bounty: 150, breach: 45f);
            Special<GrayboxBoulderBug>("BoulderBug", sprite, health: 2600f, speed: 0.7f, size: 1.4f, bounty: 150, breach: 50f);
            Special<GrayboxRivalQueen>("RivalQueen", sprite, health: 2400f, speed: 0.8f, size: 1.3f, bounty: 150, breach: 45f,
                extra: so => so.FindProperty("_antPrefab").objectReferenceValue = thief);
        }

        private static GameObject Special<T>(
            string id, Sprite sprite, float health, float speed, float size, int bounty, float breach,
            bool facePath = true,
            EnemyMover.EndBehaviour end = EnemyMover.EndBehaviour.Despawn,
            System.Action<SerializedObject> extra = null)
            where T : GrayboxSpecialEnemy
        {
            string name = "Graybox" + id;
            string label = null;
            bool boss = false;
            foreach ((string entryId, string entryLabel, bool entryBoss) in Catalog)
            {
                if (entryId == id)
                {
                    label = entryLabel;
                    boss = entryBoss;
                }
            }

            GameObject go = GrayboxBuilder.BuildBaseEnemyObject(
                name, sprite, health, speed, Color.white, size, bounty, boss ? BossBase(sprite) : null);

            GrayboxBossData data = boss ? BossData(id, label) : null;
            if (boss)
            {
                GrayboxBuilder.Apply(go.GetComponent<GrayboxBoss>(), so => so.FindProperty("_data").objectReferenceValue = data);
            }

            GrayboxBuilder.Apply(go.GetComponent<EnemyMover>(), so =>
            {
                // Flyers flip instead of turning: their shadow is drawn into the sprite.
                so.FindProperty("_faceTravelDirection").boolValue = facePath;
                so.FindProperty("_onReachEnd").enumValueIndex = (int)end;
            });

            T behaviour = go.AddComponent<T>();
            GrayboxBuilder.Apply(behaviour, so =>
            {
                so.FindProperty("_breachDamage").floatValue = breach;
                extra?.Invoke(so);
            });

            GameObject prefab = GrayboxBuilder.SavePrefab(go, name);
            if (boss)
            {
                GrayboxBuilder.Apply(data, so => so.FindProperty("_prefab").objectReferenceValue = prefab);
                EditorUtility.SetDirty(data);
            }

            return prefab;
        }

        // No mover and no enemy animator: it sits where it was laid and plays its own clips.
        private static GameObject BuildEggSac(SpriteAnimLibrary anim, GameObject hatchling)
        {
            var go = new GameObject("GrayboxEggSac");
            go.AddComponent<SpriteRenderer>().sortingOrder = 2;
            if (anim != null && anim.Find("EnemyEggSac") != null)
            {
                go.AddComponent<SpriteClipPlayer>().Configure(anim, "EnemyEggSac", "Idle");
            }

            go.AddComponent<CircleCollider2D>().radius = 0.3f;
            var body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;

            GrayboxBuilder.Apply(go.AddComponent<EnemyHealth>(), so =>
            {
                so.FindProperty("_maxHealth").floatValue = 70f;
                so.FindProperty("_bountyValue").intValue = 3;
            });
            go.AddComponent<EnemyHealthBar>();
            GrayboxBuilder.Apply(go.AddComponent<GrayboxEggSac>(), so => so.FindProperty("_hatchPrefab").objectReferenceValue = hatchling);

            return GrayboxBuilder.SavePrefab(go, "GrayboxEggSac");
        }

        /// <summary>Adds one wave per boss, and a mixed wave of the regular specials, after the waves already in the asset.</summary>
        public static void AppendWaves(string waveAssetPath, GameObject grunt)
        {
            var asset = AssetDatabase.LoadAssetAtPath<MapWaveDataSO>(waveAssetPath);
            if (asset == null)
            {
                return;
            }

            var waves = new (string Name, string[] Order)[]
            {
                ("Titan Beetle", new[] { "Grunt", "Silverfish", "Silverfish", "Grunt", "TitanBeetle", "Silverfish", "Grunt", "Silverfish" }),
                ("Raiders", new[] { "ThiefAnt", "Wasp", "Spider", "Bombardier", "ThiefAnt", "Wasp", "Silverfish", "Spider", "Bombardier", "Wasp" }),
                ("Mantis Queen", new[] { "Grunt", "Grunt", "MantisQueen", "Spider", "Grunt" }),
                ("Hornet", new[] { "Wasp", "Wasp", "Hornet", "Grunt", "Grunt", "Wasp", "Grunt" }),
                ("Orb-Weaver", new[] { "Spider", "Spider", "OrbWeaver", "Bombardier", "Spider" }),
                ("Boulder Bug", new[] { "Silverfish", "Silverfish", "BoulderBug", "Grunt", "Silverfish", "Grunt" }),
                ("Rival Queen", new[] { "ThiefAnt", "ThiefAnt", "RivalQueen", "Bombardier", "ThiefAnt" }),
            };

            foreach ((string name, string[] order) in waves)
            {
                var wave = new Wave { waveName = $"Graybox Wave {asset.waves.Count + 1}: {name}", enemies = new List<EnemySpawnEntry>() };
                foreach (string id in order)
                {
                    GameObject prefab = id == "Grunt" ? grunt : Load(id);
                    if (prefab != null)
                    {
                        wave.enemies.Add(new EnemySpawnEntry { enemyPrefab = prefab, delayBeforeNext = 1.6f });
                    }
                }

                asset.waves.Add(wave);
            }

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
        }

        /// <summary>The scene object that holds the prefabs and draws the spawn panel. Call after the spawner exists.</summary>
        public static void BuildSceneObject(SpriteAnimLibrary anim)
        {
            var entries = new List<GrayboxSpecials.Entry>();
            foreach ((string id, string label, bool boss) in Catalog)
            {
                // Loaded by path here: references made before the new scene was opened may have been unloaded.
                GameObject prefab = Load(id);
                if (prefab != null)
                {
                    entries.Add(new GrayboxSpecials.Entry { Id = id, Label = label, Prefab = prefab, Boss = boss });
                }
            }

            GameObject spawner = GameObject.Find("Enemy Spawner");
            GameObject container = GameObject.Find("Spawned Enemies");
            var go = new GameObject("GrayboxSpecials");
            go.AddComponent<GrayboxSpecials>().Configure(
                anim, entries, spawner != null ? spawner.transform : null, container != null ? container.transform : null);
        }
    }
}
