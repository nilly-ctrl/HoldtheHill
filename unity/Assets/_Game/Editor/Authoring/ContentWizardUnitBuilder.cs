using HoldTheHill.EditorTools.Builders;
using HoldTheHill.Features.Enemies;
using HoldTheHill.Features.Specials;
using HoldTheHill.Features.Economy;
using System;
using System.Collections.Generic;
using HoldTheHill.Features.Combat;
using HoldTheHill.Features.Towers;
using UnityEditor;
using UnityEngine;

using TowerSpec = HoldTheHill.EditorTools.Authoring.ContentWizardBuilder.TowerSpec;
using EnemySpec = HoldTheHill.EditorTools.Authoring.ContentWizardBuilder.EnemySpec;
using PickupSpec = HoldTheHill.EditorTools.Authoring.ContentWizardBuilder.PickupSpec;
using PowerUpSpec = HoldTheHill.EditorTools.Authoring.ContentWizardBuilder.PowerUpSpec;


namespace HoldTheHill.EditorTools.Authoring
{
    /// <summary>
    /// The content wizard's towers and enemies: a prefab variant and a data asset each, made the way <see cref="ContentWizardBuilder"/> describes.
    /// </summary>
    internal static class ContentWizardUnitBuilder
    {
        public static List<string> CreateTower(TowerSpec spec)
        {
            string prefabPath = GrayboxTowerBuilder.PrefabPath(spec.Id);
            string dataPath = ContentWizardBuilder.TowerDataPath(spec.Id);
            ContentWizardBuilder.Refuse(prefabPath, dataPath);

            GameObject basePrefab = spec.BasePrefab != null
                ? spec.BasePrefab
                : AssetDatabase.LoadAssetAtPath<GameObject>(GrayboxTowerBuilder.PrefabPath("TowerBase"));
            if (basePrefab == null)
            {
                throw new InvalidOperationException("TowerBase.prefab is missing. Run Build Graybox Combat Test first.");
            }

            GrayboxBuildKit.CreateFolderIfMissing(ContentWizardBuilder.GrayboxRoot + "/Prefabs", "Towers");
            GrayboxBuildKit.CreateFolderIfMissing(ContentWizardBuilder.GrayboxRoot, "Data");
            GrayboxBuildKit.CreateFolderIfMissing(ContentWizardBuilder.DataRoot, "Towers");

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

            var data = ScriptableObject.CreateInstance<HoldTheHill.Features.Towers.TowerData>();
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

            ContentWizardBuilder.Finish(data);
            return made;
        }

        public static List<string> CreateEnemy(EnemySpec spec)
        {
            string prefabPath = ContentWizardBuilder.EnemyPrefabPath(spec.Id);
            string dataPath = ContentWizardBuilder.BossDataPath(spec.Id);
            if (spec.Boss)
            {
                ContentWizardBuilder.Refuse(prefabPath, dataPath);
            }
            else
            {
                ContentWizardBuilder.Refuse(prefabPath);
            }

            string basePath = spec.Boss ? ContentWizardBuilder.BossBasePath : ContentWizardBuilder.EnemyBasePath;
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
                GrayboxBuildKit.CreateFolderIfMissing(ContentWizardBuilder.GrayboxRoot, "Data");
                GrayboxBuildKit.CreateFolderIfMissing(ContentWizardBuilder.DataRoot, "Bosses");
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

            ContentWizardBuilder.Finish(prefab);
            return made;
        }
    }
}
