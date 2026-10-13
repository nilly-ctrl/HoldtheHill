using HoldTheHill.Features.Economy;
using System;
using System.Collections.Generic;
using HoldTheHill.Features.Combat;
using HoldTheHill.Features.Towers;
using UnityEditor;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl.Editor
{
    /// <summary>
    /// Makes new graybox content the way the builders do: a prefab variant of the base prefab and a
    /// data asset, each made only when missing, so nothing already tuned is overwritten.
    /// </summary>
    /// <remarks>
    /// Saving and field edits go through <see cref="GrayboxBuilder"/> (SavePrefab records the
    /// instance's property modifications before saving the variant). Every Create method returns
    /// the paths it made, or throws before touching anything if a path is already taken.
    /// </remarks>
    internal static class ContentWizardBuilder
    {
        private const string GrayboxRoot = "Assets/_Sandbox/nilly-ctrl/Graybox";
        private const string PrefabRoot = GrayboxRoot + "/Prefabs";
        private const string DataRoot = GrayboxRoot + "/Data";

        public const string EnemyBasePath = PrefabRoot + "/GrayboxEnemyBase.prefab";
        public const string BossBasePath = PrefabRoot + "/GrayboxBossBase.prefab";

        public static string TowerDataPath(string id) => $"{DataRoot}/Towers/{id}Data.asset";

        public static string EnemyPrefabPath(string id) => $"{PrefabRoot}/Graybox{id}.prefab";

        public static string BossDataPath(string id) => $"{DataRoot}/Bosses/{id}Data.asset";

        public static string PickupPrefabPath(string id) => $"{PrefabRoot}/Pickups/{id}.prefab";

        public static string PowerUpEffectPath(string id) => $"{PrefabRoot}/PowerUps/Effect{id}.prefab";

        public static string PowerUpPickupPath(string id) => $"{PrefabRoot}/PowerUps/{id}.prefab";

        public sealed class TowerSpec
        {
            public string Id;
            public string DisplayName;
            public int Cost = 100;
            public TowerArchetype Archetype = TowerArchetype.Gunner;
            public string IconName;
            public float Range = 3.5f;
            public float FireInterval = 1f;
            public TargetingPriority Priority = TargetingPriority.First;
            public Color Color = Color.white;
            public GameObject BasePrefab;
            public Projectile Projectile;
            public bool AddToCatalog = true;
        }

        public sealed class EnemySpec
        {
            public string Id;
            public bool Boss;
            public string DisplayName;
            public string BannerText;
            public float Health = 100f;
            public float Speed = 1.5f;
            public float Size = 0.5f;
            public int Bounty = 10;
            public Color Color = Color.white;
            public bool FaceTravelDirection = true;
        }

        public sealed class PickupSpec
        {
            public string Id;
            public ResourceData Resource;
            public int Amount = 5;
            public float Size = 0.3f;
            public float Lifetime = 12f;
            public Color Color = new Color(0.85f, 0.7f, 0.45f);
        }

        public sealed class PowerUpSpec
        {
            public string Id;
            public string DisplayName;
            public string Description;
            public Color Color = Color.white;
            public float Duration = 8f;
            public Type EffectType;
            public bool AddToSceneSpawner = true;
        }

        /// <summary>Why an id cannot be used, or null if it is fine.</summary>
        public static string CheckId(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return "Give it an id.";
            }

            if (!char.IsLetter(id[0]))
            {
                return "The id starts with a letter.";
            }

            foreach (char c in id)
            {
                if (!char.IsLetterOrDigit(c) || c >= 128)
                {
                    return "Use only letters and digits in the id (no spaces).";
                }
            }

            return null;
        }

        /// <summary>The existing paths a creation would collide with.</summary>
        public static List<string> Collisions(params string[] paths)
        {
            var taken = new List<string>();
            foreach (string path in paths)
            {
                if (AssetDatabase.LoadMainAssetAtPath(path) != null)
                {
                    taken.Add(path);
                }
            }

            return taken;
        }

        // ---------- tower ----------

        public static List<string> CreateTower(TowerSpec spec)
        {
            string prefabPath = GrayboxTowerBuilder.PrefabPath(spec.Id);
            string dataPath = TowerDataPath(spec.Id);
            Refuse(prefabPath, dataPath);

            GameObject basePrefab = spec.BasePrefab != null
                ? spec.BasePrefab
                : AssetDatabase.LoadAssetAtPath<GameObject>(GrayboxTowerBuilder.PrefabPath("TowerBase"));
            if (basePrefab == null)
            {
                throw new InvalidOperationException("TowerBase.prefab is missing. Run Build Graybox Combat Test first.");
            }

            GrayboxBuildKit.CreateFolderIfMissing(GrayboxRoot + "/Prefabs", "Towers");
            GrayboxBuildKit.CreateFolderIfMissing(GrayboxRoot, "Data");
            GrayboxBuildKit.CreateFolderIfMissing(DataRoot, "Towers");

            var go = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
            go.name = spec.Id;
            go.GetComponent<SpriteRenderer>().color = spec.Color;

            GrayboxBuildKit.Apply(go.GetComponent<Tower>(), so =>
            {
                so.FindProperty("_range").floatValue = spec.Range;
                so.FindProperty("_fireInterval").floatValue = spec.FireInterval;
                so.FindProperty("_priority").enumValueIndex = (int)spec.Priority;
                if (spec.Projectile != null)
                {
                    so.FindProperty("_projectilePrefab").objectReferenceValue = spec.Projectile;
                }
            });

            GameObject prefab = GrayboxBuildKit.SavePrefab(go, $"Towers/{spec.Id}");

            var data = ScriptableObject.CreateInstance<GrayboxTowerData>();
            AssetDatabase.CreateAsset(data, dataPath);
            GrayboxBuildKit.Apply(data, so =>
            {
                so.FindProperty("_displayName").stringValue = string.IsNullOrEmpty(spec.DisplayName) ? spec.Id : spec.DisplayName;
                so.FindProperty("_archetype").enumValueIndex = (int)spec.Archetype;
                so.FindProperty("_iconName").stringValue = string.IsNullOrEmpty(spec.IconName) ? spec.Id + "Icon" : spec.IconName;
                so.FindProperty("_baseCost").intValue = spec.Cost;
                so.FindProperty("_prefab").objectReferenceValue = prefab.GetComponent<Tower>();
            });
            EditorUtility.SetDirty(data);

            var made = new List<string> { prefabPath, dataPath };
            if (spec.AddToCatalog)
            {
                var catalog = AssetDatabase.LoadAssetAtPath<GrayboxTowerCatalog>(GrayboxTowerBuilder.CatalogPath);
                if (catalog != null)
                {
                    var so = new SerializedObject(catalog);
                    SerializedProperty list = so.FindProperty("_towers");
                    int at = list.arraySize;
                    list.arraySize = at + 1;
                    list.GetArrayElementAtIndex(at).objectReferenceValue = data;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(catalog);
                    made.Add(GrayboxTowerBuilder.CatalogPath + " (entry added)");
                }
            }

            Finish(data);
            return made;
        }

        // ---------- enemy / boss ----------

        public static List<string> CreateEnemy(EnemySpec spec)
        {
            string prefabPath = EnemyPrefabPath(spec.Id);
            string dataPath = BossDataPath(spec.Id);
            if (spec.Boss)
            {
                Refuse(prefabPath, dataPath);
            }
            else
            {
                Refuse(prefabPath);
            }

            string basePath = spec.Boss ? BossBasePath : EnemyBasePath;
            var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(basePath);
            if (basePrefab == null)
            {
                throw new InvalidOperationException($"{basePath} is missing. Run Build Graybox Combat Test first.");
            }

            // The same helper the builders use: a variant with its own look and numbers.
            GameObject go = GrayboxEnemyPrefabBuilder.BuildBaseEnemyObject(
                "Graybox" + spec.Id, null, spec.Health, spec.Speed, spec.Color, spec.Size, spec.Bounty, basePrefab);

            GrayboxBuildKit.Apply(go.GetComponent<HoldTheHill.Features.Enemies.EnemyMover>(),
                so => so.FindProperty("_faceTravelDirection").boolValue = spec.FaceTravelDirection);

            var made = new List<string> { prefabPath };
            GrayboxBossData data = null;
            if (spec.Boss)
            {
                GrayboxBuildKit.CreateFolderIfMissing(GrayboxRoot, "Data");
                GrayboxBuildKit.CreateFolderIfMissing(DataRoot, "Bosses");
                data = ScriptableObject.CreateInstance<GrayboxBossData>();
                AssetDatabase.CreateAsset(data, dataPath);
                string label = string.IsNullOrEmpty(spec.DisplayName) ? spec.Id : spec.DisplayName;
                GrayboxBuildKit.Apply(data, so =>
                {
                    so.FindProperty("_id").stringValue = spec.Id;
                    so.FindProperty("_displayName").stringValue = label;
                    so.FindProperty("_bannerText").stringValue =
                        (string.IsNullOrEmpty(spec.BannerText) ? label : spec.BannerText).ToUpperInvariant();
                });
                GrayboxBuildKit.Apply(go.GetComponent<GrayboxBoss>(),
                    so => so.FindProperty("_data").objectReferenceValue = data);
                made.Add(dataPath);
            }

            GameObject prefab = GrayboxBuildKit.SavePrefab(go, "Graybox" + spec.Id);
            if (data != null)
            {
                GrayboxBuildKit.Apply(data, so => so.FindProperty("_prefab").objectReferenceValue = prefab);
                EditorUtility.SetDirty(data);
            }

            Finish(prefab);
            return made;
        }

        // ---------- pickup ----------

        public static List<string> CreatePickup(PickupSpec spec)
        {
            string path = PickupPrefabPath(spec.Id);
            Refuse(path);
            if (spec.Resource == null)
            {
                throw new InvalidOperationException("Pick the resource this pickup pays.");
            }

            var pickupBase = AssetDatabase.LoadAssetAtPath<GameObject>(GrayboxPickupBuilder.PrefabPath("PickupBase"));
            if (pickupBase == null)
            {
                throw new InvalidOperationException("PickupBase.prefab is missing. Run Build Graybox Combat Test first.");
            }

            GrayboxBuildKit.CreateFolderIfMissing(GrayboxRoot + "/Prefabs", "Pickups");

            var go = (GameObject)PrefabUtility.InstantiatePrefab(pickupBase);
            go.name = spec.Id;

            var renderer = go.GetComponent<SpriteRenderer>();
            renderer.color = spec.Color;
            renderer.size = new Vector2(spec.Size, spec.Size);

            GrayboxBuildKit.Apply(go.GetComponent<GrayboxPickup>(), so =>
            {
                so.FindProperty("_reach").floatValue = Mathf.Max(0.35f, spec.Size);
                so.FindProperty("_lifetime").floatValue = spec.Lifetime;
            });

            GrayboxBuildKit.Apply(go.AddComponent<GrayboxResourceGain>(), so =>
            {
                so.FindProperty("_resource").objectReferenceValue = spec.Resource;
                so.FindProperty("_amount").intValue = spec.Amount;
            });

            GameObject prefab = GrayboxBuildKit.SavePrefab(go, $"Pickups/{spec.Id}");
            Finish(prefab);
            return new List<string> { path };
        }

        // ---------- power-up ----------

        /// <summary>The concrete effect components a power-up can run, for the wizard's picker.</summary>
        public static List<Type> EffectTypes()
        {
            var found = new List<Type>();
            foreach (Type type in TypeCache.GetTypesDerivedFrom<GrayboxPowerUpEffect>())
            {
                if (!type.IsAbstract && !type.IsGenericType)
                {
                    found.Add(type);
                }
            }

            found.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
            return found;
        }

        public static List<string> CreatePowerUp(PowerUpSpec spec)
        {
            string dataPath = GrayboxPowerUpBuilder.DataPath(spec.Id);
            string effectPath = PowerUpEffectPath(spec.Id);
            string pickupPath = PowerUpPickupPath(spec.Id);
            Refuse(dataPath, effectPath, pickupPath);
            if (spec.EffectType == null)
            {
                throw new InvalidOperationException("Pick the effect component the power-up runs.");
            }

            var pickupBase = AssetDatabase.LoadAssetAtPath<GameObject>(GrayboxPickupBuilder.PrefabPath("PickupBase"));
            if (pickupBase == null)
            {
                throw new InvalidOperationException("PickupBase.prefab is missing. Run Build Graybox Combat Test first.");
            }

            GrayboxBuildKit.CreateFolderIfMissing(GrayboxRoot + "/Prefabs", "PowerUps");
            GrayboxBuildKit.CreateFolderIfMissing(GrayboxRoot, "Data");
            GrayboxBuildKit.CreateFolderIfMissing(DataRoot, "PowerUps");

            var data = ScriptableObject.CreateInstance<GrayboxPowerUpData>();
            AssetDatabase.CreateAsset(data, dataPath);
            GrayboxBuildKit.Apply(data, so =>
            {
                so.FindProperty("_id").stringValue = spec.Id;
                so.FindProperty("_displayName").stringValue = string.IsNullOrEmpty(spec.DisplayName) ? spec.Id : spec.DisplayName;
                so.FindProperty("_description").stringValue = spec.Description ?? string.Empty;
                so.FindProperty("_color").colorValue = spec.Color;
                so.FindProperty("_duration").floatValue = spec.Duration;
            });

            var effectObject = new GameObject("Effect" + spec.Id);
            effectObject.AddComponent(spec.EffectType);
            GameObject effect = GrayboxBuildKit.SavePrefab(effectObject, $"PowerUps/Effect{spec.Id}");

            var pickupObject = (GameObject)PrefabUtility.InstantiatePrefab(pickupBase);
            pickupObject.name = spec.Id;
            var renderer = pickupObject.GetComponent<SpriteRenderer>();
            renderer.color = spec.Color;
            renderer.size = new Vector2(0.5f, 0.5f);
            GrayboxBuildKit.Apply(pickupObject.GetComponent<GrayboxPickup>(), so =>
            {
                so.FindProperty("_reach").floatValue = 0.5f;
                so.FindProperty("_lifetime").floatValue = 25f;
            });
            GrayboxBuildKit.Apply(pickupObject.AddComponent<GrayboxPowerUpGain>(),
                so => so.FindProperty("_powerUp").objectReferenceValue = data);
            GameObject pickup = GrayboxBuildKit.SavePrefab(pickupObject, $"PowerUps/{spec.Id}");

            GrayboxBuildKit.Apply(data, so =>
            {
                so.FindProperty("_pickupPrefab").objectReferenceValue = pickup;
                so.FindProperty("_effectPrefab").objectReferenceValue = effect.GetComponent<GrayboxPowerUpEffect>();
            });
            EditorUtility.SetDirty(data);

            var made = new List<string> { dataPath, effectPath, pickupPath };
            if (spec.AddToSceneSpawner)
            {
                var spawner = UnityEngine.Object.FindAnyObjectByType<GrayboxPowerUpSpawner>();
                if (spawner != null)
                {
                    var so = new SerializedObject(spawner);
                    SerializedProperty list = so.FindProperty("_powerUps");
                    int at = list.arraySize;
                    list.arraySize = at + 1;
                    list.GetArrayElementAtIndex(at).objectReferenceValue = data;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(spawner);
                    made.Add($"{spawner.name} in the open scene (power-up added; save the scene)");
                }
            }

            Finish(data);
            return made;
        }

        // ---------- helpers ----------

        private static void Refuse(params string[] paths)
        {
            List<string> taken = Collisions(paths);
            if (taken.Count > 0)
            {
                throw new InvalidOperationException(
                    "Already exists, left untouched: " + string.Join(", ", taken) +
                    ". Pick another id, or delete it first.");
            }
        }

        private static void Finish(UnityEngine.Object made)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = made;
            EditorGUIUtility.PingObject(made);
        }
    }
}
