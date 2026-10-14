using HoldTheHill.Features.Economy;
using System;
using System.Collections.Generic;
using HoldTheHill.Features.Combat;
using HoldTheHill.Features.Towers;
using UnityEditor;
using UnityEngine;

namespace HoldTheHill.EditorTools.Authoring
{
    /// <summary>
    /// Makes new graybox content the way the builders do: a prefab variant of the base prefab and a
    /// data asset, each made only when missing, so nothing already tuned is overwritten.
    /// </summary>
    /// <remarks>
    /// Saving and field edits go through <see cref="CombatTestSceneBuilder"/> (SavePrefab records the
    /// instance's property modifications before saving the variant). Every Create method returns
    /// the paths it made, or throws before touching anything if a path is already taken.
    /// </remarks>
    internal static class ContentWizardBuilder
    {
        internal const string FeatureRoot = "Assets/_Game/Features";
        internal const string PrefabRoot = FeatureRoot + "/Enemies/Prefabs";
        internal const string DataRoot = FeatureRoot;

        public const string EnemyBasePath = PrefabRoot + "/EnemyBase.prefab";
        public const string BossBasePath = PrefabRoot + "/BossBase.prefab";

        public static string TowerDataPath(string id) => $"{DataRoot}/Towers/Data/{id}Data.asset";

        public static string EnemyPrefabPath(string id) => $"{PrefabRoot}/{id}.prefab";

        public static string BossDataPath(string id) => $"{DataRoot}/Specials/Data/{id}Data.asset";

        public static string PickupPrefabPath(string id) => $"{FeatureRoot}/Economy/Prefabs/{id}.prefab";

        public static string PowerUpEffectPath(string id) => $"{FeatureRoot}/PowerUps/Prefabs/Effect{id}.prefab";

        public static string PowerUpPickupPath(string id) => $"{FeatureRoot}/PowerUps/Prefabs/{id}.prefab";

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

        // ---------- enemy / boss ----------

        // ---------- pickup ----------

        // ---------- power-up ----------

        // ---------- helpers ----------

        internal static void Refuse(params string[] paths)
        {
            List<string> taken = Collisions(paths);
            if (taken.Count > 0)
            {
                throw new InvalidOperationException(
                    "Already exists, left untouched: " + string.Join(", ", taken) +
                    ". Pick another id, or delete it first.");
            }
        }

        internal static void Finish(UnityEngine.Object made)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = made;
            EditorGUIUtility.PingObject(made);
        }
    }
}
