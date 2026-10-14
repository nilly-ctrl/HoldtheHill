using HoldTheHill.Core.Input;
using HoldTheHill.Features.Hero;
using UnityEditor;
using UnityEngine;

namespace HoldTheHill.EditorTools.Builders
{
    /// <summary>
    /// The Warden: a data asset and a prefab per ability, and the Warden prefab that holds the
    /// ability prefabs as children. Called from <see cref="CombatTestSceneBuilder"/>.
    /// </summary>
    /// <remarks>
    /// Each prefab and data asset is only made when it is missing, so names, keys, cooldowns and
    /// the numbers on the abilities are tuned in the Inspector and survive a rebuild. Delete one
    /// and rebuild to start it over. The Warden prefab is put together again whenever an ability
    /// prefab had to be made.
    /// </remarks>
    internal static class WardenBuilder
    {
        private const string FeatureRoot = "Assets/_Game/Features/Hero";
        private const string PrefabFolder = "Warden";
        private const string DataRoot = FeatureRoot + "/Data";

        private static bool s_madeAnAbility;

        public static string PrefabPath(string name) => $"{FeatureRoot}/Prefabs/{name}.prefab";

        public static void BuildPrefabs(Sprite square)
        {
            BuildKit.EnsureFolder(FeatureRoot + "/Prefabs");
            BuildKit.CreateFolderIfMissing(FeatureRoot, "Data");
            s_madeAnAbility = false;

            var crumb = AssetDatabase.LoadAssetAtPath<GameObject>(PickupBuilder.PrefabPath("CrumbSmall"));

            GameObject[] abilities =
            {
                Ability<WardenShove>("Shove", "Shove", "Push nearby raiders back down the trail and stagger them.",
                    GameControls.Shove, cooldown: 0.6f, hold: 0f),
                Ability<WardenRally>("Rally", "Rally", "Nearby ants attack half again as fast for 6 seconds.",
                    GameControls.Rally, cooldown: 12f, hold: 0f),
                Ability<WardenRepair>("Repair", "Repair", "Hold beside a stunned, webbed, cocooned or scalded ant to set it right.",
                    GameControls.Repair, cooldown: 0f, hold: 1f),
                Ability<WardenDig>("Dig", "Dig", "Hold to dig up a crumb of food.",
                    GameControls.Dig, cooldown: 10f, hold: 2f,
                    so => so.FindProperty("_findPrefab").objectReferenceValue = crumb),
                Ability<WardenCarry>("Carry", "Carry", "Pick up the nearest ant, then press again to set it down where you stand.",
                    GameControls.Carry, cooldown: 0.3f, hold: 0f),
            };

            if (!s_madeAnAbility && AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath("Warden")) != null)
            {
                EnsureHealth();
                return;
            }

            var go = new GameObject("Warden");
            BuildKit.AddSprite(go, square, new Color(0.95f, 0.8f, 0.35f), 0.55f, sortingOrder: 5);
            go.AddComponent<Warden>();
            go.AddComponent<WardenHealth>();
            foreach (GameObject ability in abilities)
            {
                // Nested: the Warden prefab holds a link to each ability prefab, not a copy.
                PrefabUtility.InstantiatePrefab(ability, go.transform);
            }

            BuildKit.SavePrefab(go, $"{PrefabFolder}/Warden");
        }

        // The Warden prefab is kept between builds so its tuning survives, so a part added later is put on
        // it here, and a part that has been removed from the code (the old IMGUI panel) is taken off.
        private static void EnsureHealth()
        {
            string path = PrefabPath("Warden");
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                bool changed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(root) > 0;
                if (root.GetComponent<WardenHealth>() == null)
                {
                    root.AddComponent<WardenHealth>();
                    changed = true;
                }

                if (changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>Puts the Warden in the scene being built. It walks to the hill itself on Play.</summary>
        public static void BuildSceneObject()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath("Warden"));
            if (prefab == null)
            {
                throw new System.InvalidOperationException($"[Build] Warden prefab missing at {PrefabPath("Warden")}.");
            }

            PrefabUtility.InstantiatePrefab(prefab);
        }

        private static GameObject Ability<T>(
            string id, string label, string description, string controlId, float cooldown, float hold,
            System.Action<SerializedObject> extra = null)
            where T : WardenAbility
        {
            string dataPath = $"{DataRoot}/{id}Data.asset";
            var data = AssetDatabase.LoadAssetAtPath<AbilityData>(dataPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<AbilityData>();
                AssetDatabase.CreateAsset(data, dataPath);
                BuildKit.Apply(data, so =>
                {
                    so.FindProperty("_id").stringValue = id;
                    so.FindProperty("_displayName").stringValue = label;
                    so.FindProperty("_description").stringValue = description;
                    so.FindProperty("_controlId").stringValue = controlId;
                    so.FindProperty("_cooldown").floatValue = cooldown;
                    so.FindProperty("_holdSeconds").floatValue = hold;
                });
                AssetDatabase.SaveAssets();
            }

            string name = "Ability" + id;
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(name));
            if (existing != null)
            {
                return existing;
            }

            s_madeAnAbility = true;
            var go = new GameObject(name);
            BuildKit.Apply(go.AddComponent<T>(), so =>
            {
                so.FindProperty("_data").objectReferenceValue = data;
                extra?.Invoke(so);
            });
            return BuildKit.SavePrefab(go, $"{PrefabFolder}/{name}");
        }
    }
}
