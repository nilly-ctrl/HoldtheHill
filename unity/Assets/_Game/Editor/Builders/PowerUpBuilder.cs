using HoldTheHill.Features.Economy;
using HoldTheHill.Features.PowerUps;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HoldTheHill.EditorTools.Builders
{
    /// <summary>
    /// The power-ups: for each one an effect prefab, a pickup that is a variant of the pickup
    /// base, and a data asset tying them together. Called from <see cref="CombatTestSceneBuilder"/>.
    /// </summary>
    /// <remarks>
    /// A data asset is only filled in when it is first made, so its name, colour and duration
    /// can be edited in the Inspector. The prefabs are rebuilt on every run.
    /// </remarks>
    internal static class PowerUpBuilder
    {
        private const string FeatureRoot = "Assets/_Game/Features/PowerUps";
        private const string PrefabFolder = "PowerUps";
        private const string DataRoot = FeatureRoot + "/Data";

        private static readonly string[] Ids = { "HoneyDrop", "BerserkBerry" };

        public static string DataPath(string id) => $"{DataRoot}/{id}Data.asset";

        public static void BuildPrefabs()
        {
            BuildKit.EnsureFolder(FeatureRoot + "/Prefabs");
            BuildKit.CreateFolderIfMissing(FeatureRoot, "Data");

            var pickupBase = AssetDatabase.LoadAssetAtPath<GameObject>(PickupBuilder.PrefabPath("PickupBase"));

            Build<HoneyEffect>(pickupBase, "HoneyDrop", "Honey Drop",
                "Every raider on the map is stuck in honey and walks at half speed.", new Color(1f, 0.7f, 0.1f), duration: 6f);
            Build<BerserkEffect>(pickupBase, "BerserkBerry", "Berserk Berry",
                "The Warden's Shove hits three times as hard, reaches further and strips shields.", new Color(0.9f, 0.15f, 0.25f), duration: 8f);
        }

        /// <summary>The scene object that offers a power-up every few waves.</summary>
        public static void BuildSceneObject()
        {
            var powerUps = new List<PowerUpData>();
            foreach (string id in Ids)
            {
                // Loaded by path here: references made before the new scene was opened may have been unloaded.
                var data = AssetDatabase.LoadAssetAtPath<PowerUpData>(DataPath(id));
                if (data != null)
                {
                    powerUps.Add(data);
                }
            }

            // Power-ups made some other way (the content wizard) are offered too: any data asset in
            // the power-up folder that the list above did not cover.
            foreach (string guid in AssetDatabase.FindAssets("t:PowerUpData", new[] { DataRoot }))
            {
                var extra = AssetDatabase.LoadAssetAtPath<PowerUpData>(AssetDatabase.GUIDToAssetPath(guid));
                if (extra != null && !powerUps.Contains(extra))
                {
                    powerUps.Add(extra);
                }
            }

            var go = new GameObject("PowerUps");
            go.AddComponent<PowerUpSpawner>().Configure(powerUps);
        }

        private static void Build<T>(GameObject pickupBase, string id, string label, string description, Color color, float duration)
            where T : PowerUpEffect
        {
            var data = AssetDatabase.LoadAssetAtPath<PowerUpData>(DataPath(id));
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<PowerUpData>();
                AssetDatabase.CreateAsset(data, DataPath(id));
                BuildKit.Apply(data, so =>
                {
                    so.FindProperty("_id").stringValue = id;
                    so.FindProperty("_displayName").stringValue = label;
                    so.FindProperty("_description").stringValue = description;
                    so.FindProperty("_color").colorValue = color;
                    so.FindProperty("_duration").floatValue = duration;
                });
            }

            var effectObject = new GameObject("Effect" + id);
            effectObject.AddComponent<T>();
            GameObject effect = BuildKit.SavePrefab(effectObject, $"{PrefabFolder}/Effect{id}");

            var pickupObject = (GameObject)PrefabUtility.InstantiatePrefab(pickupBase);
            pickupObject.name = id;
            var renderer = pickupObject.GetComponent<SpriteRenderer>();
            renderer.color = color;
            renderer.size = new Vector2(0.5f, 0.5f);
            BuildKit.Apply(pickupObject.GetComponent<Pickup>(), so =>
            {
                so.FindProperty("_reach").floatValue = 0.5f;
                so.FindProperty("_lifetime").floatValue = 25f;
            });
            BuildKit.Apply(pickupObject.AddComponent<PowerUpGain>(),
                so => so.FindProperty("_powerUp").objectReferenceValue = data);
            GameObject pickup = BuildKit.SavePrefab(pickupObject, $"{PrefabFolder}/{id}");

            BuildKit.Apply(data, so =>
            {
                so.FindProperty("_pickupPrefab").objectReferenceValue = pickup;
                so.FindProperty("_effectPrefab").objectReferenceValue = effect.GetComponent<PowerUpEffect>();
            });
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
        }
    }
}
