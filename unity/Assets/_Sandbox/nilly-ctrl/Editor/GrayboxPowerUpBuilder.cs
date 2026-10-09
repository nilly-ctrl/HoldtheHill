using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// The power-ups: for each one an effect prefab, a pickup that is a variant of the pickup
    /// base, and a data asset tying them together. Called from <see cref="GrayboxBuilder"/>.
    /// </summary>
    /// <remarks>
    /// A data asset is only filled in when it is first made, so its name, colour and duration
    /// can be edited in the Inspector. The prefabs are rebuilt on every run.
    /// </remarks>
    internal static class GrayboxPowerUpBuilder
    {
        private const string GrayboxRoot = "Assets/_Sandbox/nilly-ctrl/Graybox";
        private const string PrefabFolder = "PowerUps";
        private const string DataRoot = GrayboxRoot + "/Data/PowerUps";

        private static readonly string[] Ids = { "HoneyDrop", "BerserkBerry" };

        public static string DataPath(string id) => $"{DataRoot}/{id}Data.asset";

        public static void BuildPrefabs()
        {
            GrayboxBuilder.CreateFolderIfMissing(GrayboxRoot + "/Prefabs", PrefabFolder);
            GrayboxBuilder.CreateFolderIfMissing(GrayboxRoot, "Data");
            GrayboxBuilder.CreateFolderIfMissing(GrayboxRoot + "/Data", "PowerUps");

            var pickupBase = AssetDatabase.LoadAssetAtPath<GameObject>(GrayboxPickupBuilder.PrefabPath("PickupBase"));

            Build<GrayboxHoneyEffect>(pickupBase, "HoneyDrop", "Honey Drop",
                "Every raider on the map is stuck in honey and walks at half speed.", new Color(1f, 0.7f, 0.1f), duration: 6f);
            Build<GrayboxBerserkEffect>(pickupBase, "BerserkBerry", "Berserk Berry",
                "The Warden's Shove hits three times as hard, reaches further and strips shields.", new Color(0.9f, 0.15f, 0.25f), duration: 8f);
        }

        /// <summary>The scene object that offers a power-up every few waves.</summary>
        public static void BuildSceneObject()
        {
            var powerUps = new List<GrayboxPowerUpData>();
            foreach (string id in Ids)
            {
                // Loaded by path here: references made before the new scene was opened may have been unloaded.
                var data = AssetDatabase.LoadAssetAtPath<GrayboxPowerUpData>(DataPath(id));
                if (data != null)
                {
                    powerUps.Add(data);
                }
            }

            var go = new GameObject("GrayboxPowerUps");
            go.AddComponent<GrayboxPowerUpSpawner>().Configure(powerUps);
        }

        private static void Build<T>(GameObject pickupBase, string id, string label, string description, Color color, float duration)
            where T : GrayboxPowerUpEffect
        {
            var data = AssetDatabase.LoadAssetAtPath<GrayboxPowerUpData>(DataPath(id));
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<GrayboxPowerUpData>();
                AssetDatabase.CreateAsset(data, DataPath(id));
                GrayboxBuilder.Apply(data, so =>
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
            GameObject effect = GrayboxBuilder.SavePrefab(effectObject, $"{PrefabFolder}/Effect{id}");

            var pickupObject = (GameObject)PrefabUtility.InstantiatePrefab(pickupBase);
            pickupObject.name = id;
            var renderer = pickupObject.GetComponent<SpriteRenderer>();
            renderer.color = color;
            renderer.size = new Vector2(0.5f, 0.5f);
            GrayboxBuilder.Apply(pickupObject.GetComponent<GrayboxPickup>(), so =>
            {
                so.FindProperty("_reach").floatValue = 0.5f;
                so.FindProperty("_lifetime").floatValue = 25f;
            });
            GrayboxBuilder.Apply(pickupObject.AddComponent<GrayboxPowerUpGain>(),
                so => so.FindProperty("_powerUp").objectReferenceValue = data);
            GameObject pickup = GrayboxBuilder.SavePrefab(pickupObject, $"{PrefabFolder}/{id}");

            GrayboxBuilder.Apply(data, so =>
            {
                so.FindProperty("_pickupPrefab").objectReferenceValue = pickup;
                so.FindProperty("_effectPrefab").objectReferenceValue = effect.GetComponent<GrayboxPowerUpEffect>();
            });
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
        }
    }
}
