using UnityEditor;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// The Warden: a data asset and a prefab per ability, and the Warden prefab that holds the
    /// ability prefabs as children. Called from <see cref="GrayboxBuilder"/>.
    /// </summary>
    /// <remarks>
    /// Each prefab and data asset is only made when it is missing, so names, keys, cooldowns and
    /// the numbers on the abilities are tuned in the Inspector and survive a rebuild. Delete one
    /// and rebuild to start it over. The Warden prefab is put together again whenever an ability
    /// prefab had to be made.
    /// </remarks>
    internal static class GrayboxWardenBuilder
    {
        private const string GrayboxRoot = "Assets/_Sandbox/nilly-ctrl/Graybox";
        private const string PrefabFolder = "Warden";
        private const string DataRoot = GrayboxRoot + "/Data/Abilities";

        private static bool s_madeAnAbility;

        public static string PrefabPath(string name) => $"{GrayboxRoot}/Prefabs/{PrefabFolder}/{name}.prefab";

        public static void BuildPrefabs(Sprite square)
        {
            GrayboxBuilder.CreateFolderIfMissing(GrayboxRoot + "/Prefabs", PrefabFolder);
            GrayboxBuilder.CreateFolderIfMissing(GrayboxRoot, "Data");
            GrayboxBuilder.CreateFolderIfMissing(GrayboxRoot + "/Data", "Abilities");
            s_madeAnAbility = false;

            var crumb = AssetDatabase.LoadAssetAtPath<GameObject>(GrayboxPickupBuilder.PrefabPath("CrumbSmall"));

            GameObject[] abilities =
            {
                Ability<GrayboxWardenShove>("Shove", "Shove", "Push nearby raiders back down the trail and stagger them.",
                    GrayboxControls.Shove, cooldown: 0.6f, hold: 0f),
                Ability<GrayboxWardenRally>("Rally", "Rally", "Nearby ants attack half again as fast for 6 seconds.",
                    GrayboxControls.Rally, cooldown: 12f, hold: 0f),
                Ability<GrayboxWardenRepair>("Repair", "Repair", "Hold beside a stunned, webbed, cocooned or scalded ant to set it right.",
                    GrayboxControls.Repair, cooldown: 0f, hold: 1f),
                Ability<GrayboxWardenDig>("Dig", "Dig", "Hold to dig up a crumb of food.",
                    GrayboxControls.Dig, cooldown: 10f, hold: 2f,
                    so => so.FindProperty("_findPrefab").objectReferenceValue = crumb),
                Ability<GrayboxWardenCarry>("Carry", "Carry", "Pick up the nearest ant, then press again to set it down where you stand.",
                    GrayboxControls.Carry, cooldown: 0.3f, hold: 0f),
            };

            if (!s_madeAnAbility && AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath("Warden")) != null)
            {
                return;
            }

            var go = new GameObject("Warden");
            GrayboxBuilder.AddSprite(go, square, new Color(0.95f, 0.8f, 0.35f), 0.55f, sortingOrder: 5);
            go.AddComponent<GrayboxWarden>();
            go.AddComponent<GrayboxWardenHud>();
            foreach (GameObject ability in abilities)
            {
                // Nested: the Warden prefab holds a link to each ability prefab, not a copy.
                PrefabUtility.InstantiatePrefab(ability, go.transform);
            }

            GrayboxBuilder.SavePrefab(go, $"{PrefabFolder}/Warden");
        }

        /// <summary>Puts the Warden in the scene being built. It walks to the hill itself on Play.</summary>
        public static void BuildSceneObject()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath("Warden"));
            if (prefab == null)
            {
                throw new System.InvalidOperationException($"[Graybox] Warden prefab missing at {PrefabPath("Warden")}.");
            }

            PrefabUtility.InstantiatePrefab(prefab);
        }

        private static GameObject Ability<T>(
            string id, string label, string description, string controlId, float cooldown, float hold,
            System.Action<SerializedObject> extra = null)
            where T : GrayboxWardenAbility
        {
            string dataPath = $"{DataRoot}/{id}Data.asset";
            var data = AssetDatabase.LoadAssetAtPath<GrayboxAbilityData>(dataPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<GrayboxAbilityData>();
                AssetDatabase.CreateAsset(data, dataPath);
                GrayboxBuilder.Apply(data, so =>
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
            GrayboxBuilder.Apply(go.AddComponent<T>(), so =>
            {
                so.FindProperty("_data").objectReferenceValue = data;
                extra?.Invoke(so);
            });
            return GrayboxBuilder.SavePrefab(go, $"{PrefabFolder}/{name}");
        }
    }
}
