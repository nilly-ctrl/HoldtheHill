using HoldTheHill.Features.Enemies;
using HoldTheHill.Features.Progression;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HoldTheHill.EditorTools.Builders
{
    /// <summary>
    /// The skill tree and the achievements: a data asset per node and per achievement, and one
    /// catalog each. Called from <see cref="CombatTestSceneBuilder"/>.
    /// </summary>
    /// <remarks>
    /// Assets are only made when they are missing, so names, costs and effects can be edited in
    /// the Inspector and survive a rebuild. To add a node or an achievement, make an asset of
    /// the right type and add it to the catalog; no code changes.
    /// </remarks>
    internal static class ProgressionBuilder
    {
        private const string GrayboxRoot = "Assets/_Game/Features/Progression";
        private const string DataRoot = GrayboxRoot + "/Data";
        private const string SkillFolder = "Skills";
        private const string AchievementFolder = "Achievements";

        public const string SkillCatalogPath = DataRoot + "/SkillTreeData.asset";
        public const string AchievementCatalogPath = DataRoot + "/AchievementCatalog.asset";

        private struct SkillDef
        {
            public string Id;
            public string Name;
            public string Description;
            public string Branch;
            public int Cost;
            public string Prerequisite;
            public SkillStat Stat;
            public SkillEffectMode Mode;
            public float Value;
        }

        private static readonly SkillDef[] Skills =
        {
            // Ballistics and firepower
            new SkillDef { Id = "Ballistics_HeavyCaliber", Name = "Heavy Caliber", Description = "+20% Damage for all towers.", Branch = "Ballistics", Cost = 1, Stat = SkillStat.DamageMultiplier, Mode = SkillEffectMode.Multiply, Value = 1.20f },
            new SkillDef { Id = "Ballistics_RapidCycling", Name = "Rapid Cycling", Description = "+25% Fire Rate for all towers.", Branch = "Ballistics", Cost = 2, Prerequisite = "Ballistics_HeavyCaliber", Stat = SkillStat.FireRateMultiplier, Mode = SkillEffectMode.Multiply, Value = 1.25f },
            new SkillDef { Id = "Ballistics_ExplosivePayload", Name = "Explosive Payload", Description = "+40% Blast Radius for Mortars & Mines.", Branch = "Ballistics", Cost = 3, Prerequisite = "Ballistics_RapidCycling", Stat = SkillStat.BlastRadiusMultiplier, Mode = SkillEffectMode.Multiply, Value = 1.40f },

            // Crowd control and frost
            new SkillDef { Id = "Control_DeepFreeze", Name = "Deep Freeze", Description = "+20% Slow strength for Frost Aura.", Branch = "Control", Cost = 1, Stat = SkillStat.FrostSlowBonus, Mode = SkillEffectMode.Add, Value = 0.20f },
            new SkillDef { Id = "Control_HeavyShockwave", Name = "Heavy Shockwave", Description = "+50% Knockback setback distance.", Branch = "Control", Cost = 2, Prerequisite = "Control_DeepFreeze", Stat = SkillStat.KnockbackBonus, Mode = SkillEffectMode.Add, Value = 0.50f },
            new SkillDef { Id = "Control_AbsoluteZero", Name = "Absolute Zero", Description = "+30% Damage dealt to slowed enemies.", Branch = "Control", Cost = 3, Prerequisite = "Control_HeavyShockwave", Stat = SkillStat.FrostDebuffDamageMultiplier, Mode = SkillEffectMode.Multiply, Value = 1.30f },

            // Logistics and economy
            new SkillDef { Id = "Economy_ScavengerBounties", Name = "Scavenger Bounties", Description = "+25% Gold earned from ant kills.", Branch = "Economy", Cost = 1, Stat = SkillStat.BountyMultiplier, Mode = SkillEffectMode.Multiply, Value = 1.25f },
            new SkillDef { Id = "Economy_BulkDiscounts", Name = "Bulk Discounts", Description = "20% Discount on tower builds & upgrades.", Branch = "Economy", Cost = 2, Prerequisite = "Economy_ScavengerBounties", Stat = SkillStat.CostMultiplier, Mode = SkillEffectMode.Multiply, Value = 0.80f },
            new SkillDef { Id = "Economy_SalvageMastery", Name = "Salvage Mastery", Description = "90% Gold refund when dismantling towers.", Branch = "Economy", Cost = 3, Prerequisite = "Economy_BulkDiscounts", Stat = SkillStat.RefundPercentage, Mode = SkillEffectMode.Set, Value = 0.90f },
        };

        private struct AchievementDef
        {
            public string Id;
            public string Title;
            public string Description;
            public string Icon;
            public int Required;
            public AchievementTrigger Trigger;
            public int EveryNthWave;
            public bool FullBaseHealth;
        }

        private static readonly AchievementDef[] Achievements =
        {
            new AchievementDef { Id = "first_blood", Title = "First Blood", Description = "Defeat your first enemy worker/grunt.", Icon = "AchFirstBloodIcon", Required = 1, Trigger = AchievementTrigger.EnemyDefeated },
            new AchievementDef { Id = "colony_defender", Title = "Colony Defender", Description = "Defeat 50 invading enemies.", Icon = "AchColonyDefenderIcon", Required = 50, Trigger = AchievementTrigger.EnemyDefeated },
            new AchievementDef { Id = "ant_terminator", Title = "Ant Terminator", Description = "Defeat 200 total enemies.", Icon = "AchAntTerminatorIcon", Required = 200, Trigger = AchievementTrigger.EnemyDefeated },
            new AchievementDef { Id = "gold_tycoon", Title = "Gold Tycoon", Description = "Accumulate $1,000 total Gold in treasury.", Icon = "AchGoldTycoonIcon", Required = 1000, Trigger = AchievementTrigger.FoodHeld },
            new AchievementDef { Id = "architect", Title = "Master Architect", Description = "Build or upgrade 10 defense towers.", Icon = "AchArchitectIcon", Required = 10, Trigger = AchievementTrigger.TowerBuilt },
            new AchievementDef { Id = "boss_slayer", Title = "Boss Slayer", Description = "Defeat a Wave 5 or Wave 10 Boss Horde.", Icon = "AchBossSlayerIcon", Required = 1, Trigger = AchievementTrigger.WaveCompleted, EveryNthWave = 5 },
            new AchievementDef { Id = "wave_survivor", Title = "Endless Survivor", Description = "Reach Procedural Wave 8.", Icon = "AchWaveSurvivorIcon", Required = 8, Trigger = AchievementTrigger.WaveReached },
            new AchievementDef { Id = "commander", Title = "Supreme Commander", Description = "Spend 5 Skill Points in the Commander Tree.", Icon = "AchCommanderIcon", Required = 5, Trigger = AchievementTrigger.SkillPointSpent },
            new AchievementDef { Id = "mine_master", Title = "Minefield Master", Description = "Detonate 10 Proximity Landmines.", Icon = "AchMineMasterIcon", Required = 10, Trigger = AchievementTrigger.MineDetonated },
            new AchievementDef { Id = "fortress", Title = "Impenetrable Fortress", Description = "Complete 3 waves with 100% Base HP.", Icon = "AchFortressIcon", Required = 3, Trigger = AchievementTrigger.WaveCompleted, FullBaseHealth = true },
        };

        /// <summary>Makes any missing data assets and catalogs. Safe to run on every build.</summary>
        public static void BuildAssets()
        {
            BuildKit.CreateFolderIfMissing(GrayboxRoot, "Data");
            BuildKit.CreateFolderIfMissing(DataRoot, SkillFolder);
            BuildKit.CreateFolderIfMissing(DataRoot, AchievementFolder);

            BuildSkillTree();
            BuildAchievements();
            AssetDatabase.SaveAssets();
        }

        /// <summary>The skill tree and its window, wired to the catalog.</summary>
        public static void BuildSkillTreeObject()
        {
            var go = new GameObject("SkillTree");
            var tree = go.AddComponent<SkillTree>();
            BuildKit.Apply(tree, so =>
                so.FindProperty("_catalog").objectReferenceValue = AssetDatabase.LoadAssetAtPath<SkillTreeData>(SkillCatalogPath));
        }

        /// <summary>Wires an achievements component to the catalog.</summary>
        public static void ConfigureAchievements(AchievementTracker achievements)
        {
            BuildKit.Apply(achievements, so =>
                so.FindProperty("_catalog").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AchievementCatalog>(AchievementCatalogPath));
        }

        private static string SkillPath(string id) => $"{DataRoot}/{SkillFolder}/{id}.asset";

        private static string AchievementPath(string id) => $"{DataRoot}/{AchievementFolder}/{id}.asset";

        private static void BuildSkillTree()
        {
            var nodes = new List<SkillNodeData>();
            var byId = new Dictionary<string, SkillNodeData>();

            foreach (SkillDef def in Skills)
            {
                var data = AssetDatabase.LoadAssetAtPath<SkillNodeData>(SkillPath(def.Id));
                if (data == null)
                {
                    data = ScriptableObject.CreateInstance<SkillNodeData>();
                    AssetDatabase.CreateAsset(data, SkillPath(def.Id));
                    SkillDef captured = def;
                    BuildKit.Apply(data, so =>
                    {
                        so.FindProperty("_id").stringValue = captured.Id;
                        so.FindProperty("_displayName").stringValue = captured.Name;
                        so.FindProperty("_description").stringValue = captured.Description;
                        so.FindProperty("_branch").stringValue = captured.Branch;
                        so.FindProperty("_cost").intValue = captured.Cost;
                        SerializedProperty effects = so.FindProperty("_effects");
                        effects.arraySize = 1;
                        SerializedProperty effect = effects.GetArrayElementAtIndex(0);
                        effect.FindPropertyRelative("stat").enumValueIndex = (int)captured.Stat;
                        effect.FindPropertyRelative("mode").enumValueIndex = (int)captured.Mode;
                        effect.FindPropertyRelative("value").floatValue = captured.Value;
                    });
                }

                nodes.Add(data);
                byId[def.Id] = data;
            }

            // Prerequisites are set once every node exists, and only on a node that has none yet.
            foreach (SkillDef def in Skills)
            {
                if (def.Prerequisite == null)
                {
                    continue;
                }

                SkillNodeData data = byId[def.Id];
                var so = new SerializedObject(data);
                SerializedProperty prerequisite = so.FindProperty("_prerequisite");
                if (prerequisite.objectReferenceValue == null)
                {
                    prerequisite.objectReferenceValue = byId[def.Prerequisite];
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            EnsureCatalog<SkillTreeData, SkillNodeData>(SkillCatalogPath, "_nodes", nodes);
        }

        private static void BuildAchievements()
        {
            var all = new List<AchievementData>();
            foreach (AchievementDef def in Achievements)
            {
                var data = AssetDatabase.LoadAssetAtPath<AchievementData>(AchievementPath(def.Id));
                if (data == null)
                {
                    data = ScriptableObject.CreateInstance<AchievementData>();
                    AssetDatabase.CreateAsset(data, AchievementPath(def.Id));
                    AchievementDef captured = def;
                    BuildKit.Apply(data, so =>
                    {
                        so.FindProperty("_id").stringValue = captured.Id;
                        so.FindProperty("_title").stringValue = captured.Title;
                        so.FindProperty("_description").stringValue = captured.Description;
                        so.FindProperty("_iconName").stringValue = captured.Icon;
                        so.FindProperty("_requiredProgress").intValue = captured.Required;
                        so.FindProperty("_trigger").enumValueIndex = (int)captured.Trigger;
                        so.FindProperty("_everyNthWave").intValue = captured.EveryNthWave;
                        so.FindProperty("_requireFullBaseHealth").boolValue = captured.FullBaseHealth;
                    });
                }

                all.Add(data);
            }

            EnsureCatalog<AchievementCatalog, AchievementData>(AchievementCatalogPath, "_achievements", all);
        }

        // A catalog is filled in only when it is first made; after that the list is the designer's to edit.
        private static void EnsureCatalog<TCatalog, TItem>(string path, string listField, List<TItem> items)
            where TCatalog : ScriptableObject
            where TItem : Object
        {
            if (AssetDatabase.LoadAssetAtPath<TCatalog>(path) != null)
            {
                return;
            }

            var catalog = ScriptableObject.CreateInstance<TCatalog>();
            AssetDatabase.CreateAsset(catalog, path);
            BuildKit.Apply(catalog, so =>
            {
                SerializedProperty list = so.FindProperty(listField);
                list.arraySize = items.Count;
                for (int i = 0; i < items.Count; i++)
                {
                    list.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
                }
            });
        }
    }
}
