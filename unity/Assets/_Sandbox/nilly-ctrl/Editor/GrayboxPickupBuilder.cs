using UnityEditor;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// The run's resources and the pickups that pay them: the Food asset, a pickup base prefab
    /// and its crumb variants. Called from <see cref="GrayboxBuilder"/>.
    /// </summary>
    /// <remarks>
    /// The Food asset is only filled in when it is first made, so its name, colour and starting
    /// amount can be edited in the Inspector. The pickup prefabs are rebuilt on every run.
    /// </remarks>
    internal static class GrayboxPickupBuilder
    {
        private const string GrayboxRoot = "Assets/_Sandbox/nilly-ctrl/Graybox";
        private const string PrefabFolder = "Pickups";

        public const string FoodPath = GrayboxRoot + "/Data/Economy/Food.asset";

        public static string PrefabPath(string name) => $"{GrayboxRoot}/Prefabs/{PrefabFolder}/{name}.prefab";

        /// <summary>The run's main resource. Made on first use.</summary>
        public static GrayboxResourceData Food()
        {
            GrayboxBuilder.CreateFolderIfMissing(GrayboxRoot, "Data");
            GrayboxBuilder.CreateFolderIfMissing(GrayboxRoot + "/Data", "Economy");

            var food = AssetDatabase.LoadAssetAtPath<GrayboxResourceData>(FoodPath);
            if (food == null)
            {
                food = ScriptableObject.CreateInstance<GrayboxResourceData>();
                AssetDatabase.CreateAsset(food, FoodPath);
                GrayboxBuilder.Apply(food, so =>
                {
                    so.FindProperty("_id").stringValue = "Food";
                    so.FindProperty("_displayName").stringValue = "Food";
                    so.FindProperty("_color").colorValue = new Color(1f, 0.85f, 0.3f);
                    so.FindProperty("_startingAmount").intValue = 500;
                });
                AssetDatabase.SaveAssets();
            }

            return food;
        }

        /// <summary>Builds the pickup base and the three crumbs. Returns the golden crumb, which bosses drop.</summary>
        public static GameObject BuildPrefabs(Sprite circle)
        {
            GrayboxBuilder.CreateFolderIfMissing(GrayboxRoot + "/Prefabs", PrefabFolder);
            GrayboxResourceData food = Food();

            // What every pickup shares: something to see, and the component that collects it.
            var baseObject = new GameObject("PickupBase");
            GrayboxBuilder.AddSprite(baseObject, circle, Color.white, 0.3f, sortingOrder: 3);
            baseObject.AddComponent<GrayboxPickup>();
            GameObject pickupBase = GrayboxBuilder.SavePrefab(baseObject, $"{PrefabFolder}/PickupBase");

            Crumb(pickupBase, "CrumbSmall", food, 5, 0.25f, new Color(0.85f, 0.7f, 0.45f), lifetime: 12f);
            Crumb(pickupBase, "CrumbBig", food, 20, 0.4f, new Color(0.8f, 0.6f, 0.3f), lifetime: 12f);
            return Crumb(pickupBase, "CrumbGolden", food, 100, 0.5f, new Color(1f, 0.85f, 0.2f), lifetime: 20f);
        }

        private static GameObject Crumb(
            GameObject pickupBase, string name, GrayboxResourceData resource, int amount, float size, Color color, float lifetime)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(pickupBase);
            go.name = name;

            var renderer = go.GetComponent<SpriteRenderer>();
            renderer.color = color;
            renderer.size = new Vector2(size, size);

            GrayboxBuilder.Apply(go.GetComponent<GrayboxPickup>(), so =>
            {
                so.FindProperty("_reach").floatValue = Mathf.Max(0.35f, size);
                so.FindProperty("_lifetime").floatValue = lifetime;
            });

            GrayboxBuilder.Apply(go.AddComponent<GrayboxResourceGain>(), so =>
            {
                so.FindProperty("_resource").objectReferenceValue = resource;
                so.FindProperty("_amount").intValue = amount;
            });

            return GrayboxBuilder.SavePrefab(go, $"{PrefabFolder}/{name}");
        }
    }
}
