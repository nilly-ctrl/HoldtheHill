using HoldTheHill.Features.Species;
using HoldTheHill.Features.Hero;
using System.Collections.Generic;
using HoldTheHill.Features.Towers;
using UnityEditor;
using UnityEngine;


namespace HoldTheHill.EditorTools.Builders
{
    /// <summary>
    /// The species: the Garden Ant, which is the game as it stands (all 14 towers and the
    /// Warden), and a small sample second species to show how one is put together. Called from
    /// <see cref="CombatTestSceneBuilder"/>.
    /// </summary>
    /// <remarks>
    /// Everything here is only made when it is missing, so species assets, their catalogs and
    /// their tower variants are edited in the Inspector and survive a rebuild.
    ///
    /// The Fire Ant is a SAMPLE, not a design: three castes that are variants of the shared
    /// tower prefabs with a red tint, a little less reach and a faster attack. Nothing in the
    /// game selects it yet; assign it on the SpeciesLoader in a scene to try it.
    /// </remarks>
    internal static class SpeciesBuilder
    {
        private const string GrayboxRoot = "Assets/_Game/Features/Species";
        private const string DataRoot = GrayboxRoot + "/Data";

        public const string DefaultSpeciesPath = DataRoot + "/GardenAnt.asset";
        public const string SampleSpeciesPath = DataRoot + "/FireAnt.asset";

        public static void BuildAssets()
        {
            BuildKit.CreateFolderIfMissing(GrayboxRoot, "Data");

            var catalog = AssetDatabase.LoadAssetAtPath<TowerCatalog>(TowerBuilder.CatalogPath);
            var warden = AssetDatabase.LoadAssetAtPath<GameObject>(WardenBuilder.PrefabPath("Warden"));
            Warden wardenPrefab = warden != null ? warden.GetComponent<Warden>() : null;

            Species(DefaultSpeciesPath, "GardenAnt", "Garden Ant",
                "The all-rounders. Every caste, nothing special, nothing missing.",
                new Color(0.75f, 0.55f, 0.35f), catalog, wardenPrefab);

            if (AssetDatabase.LoadAssetAtPath<SpeciesData>(SampleSpeciesPath) == null)
            {
                TowerCatalog fireCatalog = BuildSampleCatalog(catalog);
                Species(SampleSpeciesPath, "FireAnt", "Fire Ant (sample)",
                    "A sample species: three castes that strike faster from closer in.",
                    new Color(0.9f, 0.3f, 0.2f), fireCatalog, wardenPrefab);
            }

            AssetDatabase.SaveAssets();
        }

        /// <summary>The scene object that sets the scene up as the Garden Ant on Play.</summary>
        public static void BuildSceneObject()
        {
            var species = AssetDatabase.LoadAssetAtPath<SpeciesData>(DefaultSpeciesPath);
            if (species == null)
            {
                throw new System.InvalidOperationException($"[Graybox] Species asset missing at {DefaultSpeciesPath}.");
            }

            var go = new GameObject("Species");
            BuildKit.Apply(go.AddComponent<SpeciesLoader>(),
                so => so.FindProperty("_species").objectReferenceValue = species);
        }

        private static void Species(
            string path, string id, string label, string description, Color color,
            TowerCatalog catalog, Warden warden)
        {
            if (AssetDatabase.LoadAssetAtPath<SpeciesData>(path) != null)
            {
                return;
            }

            var species = ScriptableObject.CreateInstance<SpeciesData>();
            AssetDatabase.CreateAsset(species, path);
            BuildKit.Apply(species, so =>
            {
                so.FindProperty("_id").stringValue = id;
                so.FindProperty("_displayName").stringValue = label;
                so.FindProperty("_description").stringValue = description;
                so.FindProperty("_color").colorValue = color;
                so.FindProperty("_towerCatalog").objectReferenceValue = catalog;
                so.FindProperty("_wardenPrefab").objectReferenceValue = warden;
            });
            EditorUtility.SetDirty(species);
        }

        // Worker, Soldier and Bullet Tower, each as a variant of the shared prefab.
        private static TowerCatalog BuildSampleCatalog(TowerCatalog shared)
        {
            BuildKit.EnsureFolder("Assets/_Game/Features/Towers/Prefabs/FireAnt");
            BuildKit.EnsureFolder("Assets/_Game/Features/Towers/Data/FireAnt");

            var towers = new List<HoldTheHill.Features.Towers.TowerData>();
            foreach (string id in new[] { "TowerWorker", "TowerSoldier", "TowerLinear" })
            {
                HoldTheHill.Features.Towers.TowerData source = Find(shared, id);
                if (source == null)
                {
                    continue;
                }

                var go = (GameObject)PrefabUtility.InstantiatePrefab(source.Prefab.gameObject);
                go.name = id + "_FireAnt";
                go.GetComponent<SpriteRenderer>().color = new Color(1f, 0.6f, 0.5f);

                Tower tower = go.GetComponent<Tower>();
                float range = tower.Range * 0.9f;
                float interval = tower.FireInterval * 0.8f;
                BuildKit.Apply(tower, so =>
                {
                    so.FindProperty("_range").floatValue = range;
                    so.FindProperty("_fireInterval").floatValue = interval;
                });
                GameObject prefab = BuildKit.SavePrefab(go, $"Towers/FireAnt/{id}_FireAnt");

                var data = ScriptableObject.CreateInstance<HoldTheHill.Features.Towers.TowerData>();
                AssetDatabase.CreateAsset(data, $"Assets/_Game/Features/Towers/Data/FireAnt/{id}_FireAntData.asset");
                BuildKit.Apply(data, so =>
                {
                    so.FindProperty("_displayName").stringValue = "Fire " + source.DisplayName;
                    so.FindProperty("_iconName").stringValue = source.IconName;
                    so.FindProperty("_baseCost").intValue = source.BaseCost;
                    so.FindProperty("_prefab").objectReferenceValue = prefab.GetComponent<Tower>();
                    so.FindProperty("_ghostSprite").objectReferenceValue = source.GhostSprite;
                });
                EditorUtility.SetDirty(data);
                towers.Add(data);
            }

            var catalog = ScriptableObject.CreateInstance<TowerCatalog>();
            AssetDatabase.CreateAsset(catalog, $"{DataRoot}/FireAntCatalog.asset");
            BuildKit.Apply(catalog, so =>
            {
                SerializedProperty list = so.FindProperty("_towers");
                list.arraySize = towers.Count;
                for (int i = 0; i < towers.Count; i++)
                {
                    list.GetArrayElementAtIndex(i).objectReferenceValue = towers[i];
                }
            });
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static HoldTheHill.Features.Towers.TowerData Find(TowerCatalog catalog, string prefabName)
        {
            for (int i = 0; catalog != null && i < catalog.Count; i++)
            {
                if (catalog[i] != null && catalog[i].Prefab != null && catalog[i].Prefab.name == prefabName)
                {
                    return catalog[i];
                }
            }

            return null;
        }
    }
}
