using System.Collections.Generic;
using HoldTheHill.Features.Combat;
using HoldTheHill.Features.Towers;
using UnityEditor;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// The graybox towers: a base prefab, one prefab variant per tower, a
    /// <see cref="GrayboxTowerData"/> asset for each and the <see cref="GrayboxTowerCatalog"/>
    /// the build bar reads. Called from <see cref="GrayboxBuilder"/>.
    /// </summary>
    /// <remarks>
    /// The prefabs are rebuilt on every run, so a tower's combat numbers are changed here. A data
    /// asset is only filled in when it is first made: its name, icon and cost can be edited in
    /// the Inspector and survive a rebuild.
    /// </remarks>
    internal static class GrayboxTowerBuilder
    {
        private const string GrayboxRoot = "Assets/_Sandbox/nilly-ctrl/Graybox";
        private const string PrefabFolder = "Towers";
        private const string DataRoot = GrayboxRoot + "/Data";
        private const string TowerDataRoot = DataRoot + "/Towers";

        public const string CatalogPath = DataRoot + "/GrayboxTowerCatalog.asset";

        private static GameObject s_base;
        private static SpriteAnimLibrary s_anim;

        public static string PrefabPath(string id) => $"{GrayboxRoot}/Prefabs/{PrefabFolder}/{id}.prefab";

        /// <summary>Builds every tower prefab and data asset, and the catalog in build bar order.</summary>
        public static void BuildPrefabs(
            Sprite square,
            GameObject bullet,
            GameObject homing,
            GameObject mortar,
            GameObject ricochet,
            GameObject mine,
            Material lineMaterial,
            SpriteAnimLibrary anim)
        {
            GrayboxBuilder.CreateFolderIfMissing(GrayboxRoot + "/Prefabs", PrefabFolder);
            GrayboxBuilder.CreateFolderIfMissing(GrayboxRoot, "Data");
            GrayboxBuilder.CreateFolderIfMissing(DataRoot, "Towers");

            s_anim = anim;
            s_base = BuildBase(square);

            // Fire intervals are tighter than a shipping game would want. In the first
            // build nothing died for the opening six seconds, which reads as "the towers
            // are broken" even though they were working. Enemy health goes up to
            // compensate, so the defence is not made easier overall.
            var towers = new List<GrayboxTowerData>
            {
                Build("TowerLinear", "Bullet Tower", 100, TargetingPriority.First, 3.6f, 0.45f, new Color(0.4f, 0.7f, 1f),
                    go => SetProjectile(go, bullet)),
                Build("TowerHoming", "Homing Tower", 150, TargetingPriority.Closest, 3.8f, 0.9f, new Color(0.4f, 1f, 0.6f),
                    go => SetProjectile(go, homing)),
                Build("TowerMortar", "Mortar Tower", 200, TargetingPriority.First, 4.2f, 1.6f, new Color(0.9f, 0.5f, 0.2f),
                    go => SetProjectile(go, mortar)),
                Build("TowerRicochet", "Ricochet Tower", 175, TargetingPriority.Strongest, 3.8f, 1f, new Color(0.6f, 0.8f, 1f),
                    go => SetProjectile(go, ricochet)),
                Build("TowerFrostAura", "Frost Aura", 225, TargetingPriority.Closest, 3.5f, 1.5f, new Color(0.4f, 0.85f, 1f),
                    go => go.AddComponent<FrostAuraTower>()),
                Build("TowerKnockback", "Knockback Pulse", 200, TargetingPriority.Closest, 3.0f, 1.5f, new Color(1f, 0.6f, 0.2f),
                    go => go.AddComponent<KnockbackTower>()),
                Build("TowerMineLayer", "Mine Layer", 250, TargetingPriority.Closest, 4.0f, 2.8f, new Color(0.9f, 0.9f, 0.3f),
                    go => GrayboxBuilder.Apply(go.AddComponent<MineLayerTower>(),
                        so => so.FindProperty("_minePrefab").objectReferenceValue = mine)),
                Build("TowerChain", "Chain Lightning", 200, TargetingPriority.Weakest, 3.8f, 1.2f, new Color(0.5f, 0.85f, 1f),
                    go => GrayboxBuilder.AddChainLightning(go, lineMaterial)),
                Build("TowerBeam", "Beam Tower", 225, TargetingPriority.Closest, 4f, 1f, new Color(1f, 0.5f, 0.3f),
                    go => GrayboxBuilder.AddBeam(go, lineMaterial)),
                Build("TowerOrbit", "Swarm Nest", 175, TargetingPriority.Closest, 2.5f, 1f, new Color(0.7f, 0.6f, 1f),
                    AddOrbit),
                // The close-range castes. Range and interval come from GrayboxMeleeTower.StatsFor.
                BuildMelee("TowerWorker", "Worker", 75, GrayboxMeleeTower.Caste.Worker),
                BuildMelee("TowerSoldier", "Soldier", 125, GrayboxMeleeTower.Caste.Soldier),
                BuildMelee("TowerMajor", "Major", 200, GrayboxMeleeTower.Caste.Major),
                BuildMelee("TowerNurse", "Nurse", 150, GrayboxMeleeTower.Caste.Nurse),
            };

            var catalog = LoadOrCreate<GrayboxTowerCatalog>(CatalogPath, out _);
            GrayboxBuilder.Apply(catalog, so =>
            {
                SerializedProperty list = so.FindProperty("_towers");
                list.arraySize = towers.Count;
                for (int i = 0; i < towers.Count; i++)
                {
                    list.GetArrayElementAtIndex(i).objectReferenceValue = towers[i];
                }
            });
            EditorUtility.SetDirty(catalog);
            // Written out now: GrayboxBuilder opens a new scene next and reloads the catalog by path.
            AssetDatabase.SaveAssets();

            s_base = null;
            s_anim = null;
        }

        /// <summary>The graybox scene's showcase towers: one of each ranged type, already standing.</summary>
        public static void BuildSceneTowers()
        {
            // Positions are all at least 2 units clear of the road so TowerPlacer would
            // also consider them buildable.
            PlaceInScene("TowerLinear", "Tower_Linear_First", new Vector2(-7f, 4.5f));
            PlaceInScene("TowerHoming", "Tower_Homing_Closest", new Vector2(-6f, 0.5f));
            PlaceInScene("TowerMortar", "Tower_Mortar_First", new Vector2(-1.5f, 0f));
            PlaceInScene("TowerRicochet", "Tower_Ricochet_Strongest", new Vector2(-1.5f, -5f));
            PlaceInScene("TowerChain", "Tower_Chain_Weakest", new Vector2(5f, 1f));
            PlaceInScene("TowerBeam", "Tower_Beam_Closest", new Vector2(6f, 5.5f));

            // The orbiting field hurts things by touching them, so what matters is whether
            // the wisps physically overlap the road — the tower's own range is irrelevant.
            // At the old spot (-1.5,-5) the path was 2.5 away and the wisps orbited at 1.5,
            // so they fell a full unit short and this tower could never hit anything.
            // 1.5 out from the road, with a 1.6 orbit, puts them just over it.
            PlaceInScene("TowerOrbit", "Tower_Orbit", new Vector2(0f, -4f));

            PlaceInScene("TowerFrostAura", "Tower_Frost_Closest", new Vector2(-4f, 4.5f));
            PlaceInScene("TowerKnockback", "Tower_Knockback_Closest", new Vector2(2f, -5.5f));
            PlaceInScene("TowerMineLayer", "Tower_MineLayer", new Vector2(0f, 5f));
        }

        private static void PlaceInScene(string id, string name, Vector2 position)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(id));
            if (prefab == null)
            {
                throw new System.InvalidOperationException($"[Graybox] Tower prefab missing at {PrefabPath(id)}.");
            }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = name;
            go.transform.position = position;
        }

        // What every tower shares. A change here reaches all of them through the variants.
        private static GameObject BuildBase(Sprite square)
        {
            var go = new GameObject("TowerBase");
            GrayboxBuilder.AddSprite(go, square, Color.white, 0.8f, sortingOrder: 1);
            go.AddComponent<Tower>();
            go.AddComponent<TowerTargetVisualizer>();
            return GrayboxBuilder.SavePrefab(go, $"{PrefabFolder}/TowerBase");
        }

        private static GrayboxTowerData Build(
            string id,
            string label,
            int cost,
            TargetingPriority priority,
            float range,
            float fireInterval,
            Color color,
            System.Action<GameObject> addWeapon)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(s_base);
            go.name = id;
            go.GetComponent<SpriteRenderer>().color = color;

            GrayboxBuilder.Apply(go.GetComponent<Tower>(), so =>
            {
                so.FindProperty("_range").floatValue = range;
                so.FindProperty("_fireInterval").floatValue = fireInterval;
                so.FindProperty("_priority").enumValueIndex = (int)priority;
            });

            addWeapon?.Invoke(go);
            GrayboxTowerAnimator.Attach(go, s_anim, id);

            // Changes made straight to a component (not through a SerializedObject) only become
            // overrides on the variant once they are recorded.
            foreach (Component component in go.GetComponents<Component>())
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            }

            GameObject prefab = GrayboxBuilder.SavePrefab(go, $"{PrefabFolder}/{id}");
            return BuildData(id, label, cost, prefab);
        }

        private static GrayboxTowerData BuildMelee(string id, string label, int cost, GrayboxMeleeTower.Caste caste)
        {
            GrayboxMeleeTower.Stats stats = GrayboxMeleeTower.StatsFor(caste);
            return Build(id, label, cost, TargetingPriority.Closest, stats.Range, stats.Interval, new Color(0.75f, 0.55f, 0.35f),
                go => go.AddComponent<GrayboxMeleeTower>().Configure(caste));
        }

        private static void SetProjectile(GameObject tower, GameObject projectile)
        {
            GrayboxBuilder.Apply(tower.GetComponent<Tower>(),
                so => so.FindProperty("_projectilePrefab").objectReferenceValue = projectile.GetComponent<Projectile>());
        }

        private static void AddOrbit(GameObject tower)
        {
            GrayboxBuilder.Apply(tower.AddComponent<OrbitingDamageField>(), so =>
            {
                so.FindProperty("_radius").floatValue = 1.6f;
                so.FindProperty("_orbiterCount").intValue = 3;
                so.FindProperty("_angularSpeed").floatValue = 150f;
                so.FindProperty("_contactDamage").floatValue = 6f;
                so.FindProperty("_hitCooldown").floatValue = 0.4f;
                so.FindProperty("_orbiterRadius").floatValue = 0.35f;
            });
        }

        private static GrayboxTowerData BuildData(string id, string label, int cost, GameObject prefab)
        {
            var data = LoadOrCreate<GrayboxTowerData>($"{TowerDataRoot}/{id}Data.asset", out bool created);
            SpriteAnimClip idle = s_anim != null ? s_anim.Find(id)?.Find("Idle") : null;

            GrayboxBuilder.Apply(data, so =>
            {
                if (created)
                {
                    so.FindProperty("_displayName").stringValue = label;
                    so.FindProperty("_iconName").stringValue = id + "Icon";
                    so.FindProperty("_baseCost").intValue = cost;
                }

                so.FindProperty("_prefab").objectReferenceValue = prefab.GetComponent<Tower>();
                so.FindProperty("_ghostSprite").objectReferenceValue =
                    idle != null && idle.Frames.Length > 0 ? idle.Frames[0] : null;
            });
            EditorUtility.SetDirty(data);
            return data;
        }

        private static T LoadOrCreate<T>(string path, out bool created) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            created = asset == null;
            if (created)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }

            return asset;
        }
    }
}
