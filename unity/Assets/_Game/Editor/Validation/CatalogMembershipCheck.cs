using HoldTheHill.Features.PowerUps;
using HoldTheHill.Features.Progression;
using HoldTheHill.Features.Specials;
using HoldTheHill.Features.Species;
using HoldTheHill.Features.Hero;
using HoldTheHill.Features.Towers;
using HoldTheHill.Features.Economy;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


namespace HoldTheHill.EditorTools.Validation
{
    /// <summary>
    /// Data assets that no catalog lists (towers, achievements, skill nodes), and data assets of the
    /// other kinds that nothing in the sandbox references at all. Also empty or repeated catalog slots.
    /// </summary>
    public static class CatalogMembershipCheck
    {
        private const string Name = "Catalog";

        public static void Run(string root, ProjectTextIndex index, List<ValidationFinding> findings)
        {
            CheckTowers(root, findings);
            CheckAchievements(root, findings);
            CheckSkillNodes(root, findings);

            ReportUnreferenced<BossData>(root, index, findings, "boss data");
            ReportUnreferenced<PowerUpData>(root, index, findings, "power-up data");
            ReportUnreferenced<SpeciesData>(root, index, findings, "species data");
            ReportUnreferenced<ResourceData>(root, index, findings, "resource data");
            ReportUnreferenced<AbilityData>(root, index, findings, "ability data");
        }

        private static void CheckTowers(string root, List<ValidationFinding> findings)
        {
            var listed = new HashSet<HoldTheHill.Features.Towers.TowerData>();
            foreach (var (path, catalog) in ProjectValidator.FindAll<TowerCatalog>(root))
            {
                var inThisCatalog = new HashSet<HoldTheHill.Features.Towers.TowerData>();
                for (int i = 0; i < catalog.Count; i++)
                {
                    HoldTheHill.Features.Towers.TowerData tower = catalog[i];
                    if (tower == null)
                    {
                        ProjectValidator.Add(findings, ValidationSeverity.Error, Name,
                            $"Slot {i} is empty (the build bar would break).", path);
                    }
                    else if (!inThisCatalog.Add(tower))
                    {
                        ProjectValidator.Add(findings, ValidationSeverity.Warning, Name,
                            $"'{tower.name}' is listed twice (slot {i}).", path);
                    }
                    else
                    {
                        listed.Add(tower);
                    }
                }
            }

            foreach (var (path, tower) in ProjectValidator.FindAll<HoldTheHill.Features.Towers.TowerData>(root))
            {
                if (!listed.Contains(tower))
                {
                    ProjectValidator.Add(findings, ValidationSeverity.Warning, Name,
                        $"Tower data '{tower.name}' is not listed in any tower catalog, so it can never be built.", path);
                }
            }
        }

        private static void CheckAchievements(string root, List<ValidationFinding> findings)
        {
            var listed = new HashSet<AchievementData>();
            foreach (var (path, catalog) in ProjectValidator.FindAll<AchievementCatalog>(root))
            {
                for (int i = 0; i < catalog.Achievements.Count; i++)
                {
                    AchievementData item = catalog.Achievements[i];
                    if (item == null)
                    {
                        ProjectValidator.Add(findings, ValidationSeverity.Error, Name, $"Slot {i} is empty.", path);
                    }
                    else if (!listed.Add(item))
                    {
                        ProjectValidator.Add(findings, ValidationSeverity.Warning, Name,
                            $"'{item.name}' is listed twice.", path);
                    }
                }
            }

            foreach (var (path, item) in ProjectValidator.FindAll<AchievementData>(root))
            {
                if (!listed.Contains(item))
                {
                    ProjectValidator.Add(findings, ValidationSeverity.Warning, Name,
                        $"Achievement '{item.name}' is not listed in any achievement catalog, so it is never shown.", path);
                }
            }
        }

        private static void CheckSkillNodes(string root, List<ValidationFinding> findings)
        {
            var listed = new HashSet<SkillNodeData>();
            foreach (var (path, tree) in ProjectValidator.FindAll<SkillTreeData>(root))
            {
                for (int i = 0; i < tree.Nodes.Count; i++)
                {
                    SkillNodeData node = tree.Nodes[i];
                    if (node == null)
                    {
                        ProjectValidator.Add(findings, ValidationSeverity.Error, Name, $"Slot {i} is empty.", path);
                    }
                    else if (!listed.Add(node))
                    {
                        ProjectValidator.Add(findings, ValidationSeverity.Warning, Name,
                            $"'{node.name}' is listed twice.", path);
                    }
                }
            }

            foreach (var (path, node) in ProjectValidator.FindAll<SkillNodeData>(root))
            {
                if (!listed.Contains(node))
                {
                    ProjectValidator.Add(findings, ValidationSeverity.Warning, Name,
                        $"Skill node '{node.name}' is not listed in any skill tree, so it never appears.", path);
                }
            }
        }

        private static void ReportUnreferenced<T>(string root, ProjectTextIndex index,
            List<ValidationFinding> findings, string kind) where T : Object
        {
            foreach (var (path, asset) in ProjectValidator.FindAll<T>(root))
            {
                string guid = AssetDatabase.AssetPathToGUID(path);
                if (index.ReferencedBy(guid).Count == 0)
                {
                    ProjectValidator.Add(findings, ValidationSeverity.Warning, Name,
                        $"The {kind} '{asset.name}' is not referenced by any prefab, scene or asset in the sandbox. " +
                        "If code or a builder looks it up by path, ignore this.", path);
                }
            }
        }
    }
}
