using HoldTheHill.Features.Economy;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl.Editor
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

            ReportUnreferenced<GrayboxBossData>(root, index, findings, "boss data");
            ReportUnreferenced<GrayboxPowerUpData>(root, index, findings, "power-up data");
            ReportUnreferenced<GrayboxSpeciesData>(root, index, findings, "species data");
            ReportUnreferenced<ResourceData>(root, index, findings, "resource data");
            ReportUnreferenced<GrayboxAbilityData>(root, index, findings, "ability data");
        }

        private static void CheckTowers(string root, List<ValidationFinding> findings)
        {
            var listed = new HashSet<GrayboxTowerData>();
            foreach (var (path, catalog) in GrayboxProjectValidator.FindAll<GrayboxTowerCatalog>(root))
            {
                var inThisCatalog = new HashSet<GrayboxTowerData>();
                for (int i = 0; i < catalog.Count; i++)
                {
                    GrayboxTowerData tower = catalog[i];
                    if (tower == null)
                    {
                        GrayboxProjectValidator.Add(findings, ValidationSeverity.Error, Name,
                            $"Slot {i} is empty (the build bar would break).", path);
                    }
                    else if (!inThisCatalog.Add(tower))
                    {
                        GrayboxProjectValidator.Add(findings, ValidationSeverity.Warning, Name,
                            $"'{tower.name}' is listed twice (slot {i}).", path);
                    }
                    else
                    {
                        listed.Add(tower);
                    }
                }
            }

            foreach (var (path, tower) in GrayboxProjectValidator.FindAll<GrayboxTowerData>(root))
            {
                if (!listed.Contains(tower))
                {
                    GrayboxProjectValidator.Add(findings, ValidationSeverity.Warning, Name,
                        $"Tower data '{tower.name}' is not listed in any tower catalog, so it can never be built.", path);
                }
            }
        }

        private static void CheckAchievements(string root, List<ValidationFinding> findings)
        {
            var listed = new HashSet<GrayboxAchievementData>();
            foreach (var (path, catalog) in GrayboxProjectValidator.FindAll<GrayboxAchievementCatalog>(root))
            {
                for (int i = 0; i < catalog.Achievements.Count; i++)
                {
                    GrayboxAchievementData item = catalog.Achievements[i];
                    if (item == null)
                    {
                        GrayboxProjectValidator.Add(findings, ValidationSeverity.Error, Name, $"Slot {i} is empty.", path);
                    }
                    else if (!listed.Add(item))
                    {
                        GrayboxProjectValidator.Add(findings, ValidationSeverity.Warning, Name,
                            $"'{item.name}' is listed twice.", path);
                    }
                }
            }

            foreach (var (path, item) in GrayboxProjectValidator.FindAll<GrayboxAchievementData>(root))
            {
                if (!listed.Contains(item))
                {
                    GrayboxProjectValidator.Add(findings, ValidationSeverity.Warning, Name,
                        $"Achievement '{item.name}' is not listed in any achievement catalog, so it is never shown.", path);
                }
            }
        }

        private static void CheckSkillNodes(string root, List<ValidationFinding> findings)
        {
            var listed = new HashSet<GrayboxSkillNodeData>();
            foreach (var (path, tree) in GrayboxProjectValidator.FindAll<GrayboxSkillTreeData>(root))
            {
                for (int i = 0; i < tree.Nodes.Count; i++)
                {
                    GrayboxSkillNodeData node = tree.Nodes[i];
                    if (node == null)
                    {
                        GrayboxProjectValidator.Add(findings, ValidationSeverity.Error, Name, $"Slot {i} is empty.", path);
                    }
                    else if (!listed.Add(node))
                    {
                        GrayboxProjectValidator.Add(findings, ValidationSeverity.Warning, Name,
                            $"'{node.name}' is listed twice.", path);
                    }
                }
            }

            foreach (var (path, node) in GrayboxProjectValidator.FindAll<GrayboxSkillNodeData>(root))
            {
                if (!listed.Contains(node))
                {
                    GrayboxProjectValidator.Add(findings, ValidationSeverity.Warning, Name,
                        $"Skill node '{node.name}' is not listed in any skill tree, so it never appears.", path);
                }
            }
        }

        private static void ReportUnreferenced<T>(string root, ProjectTextIndex index,
            List<ValidationFinding> findings, string kind) where T : Object
        {
            foreach (var (path, asset) in GrayboxProjectValidator.FindAll<T>(root))
            {
                string guid = AssetDatabase.AssetPathToGUID(path);
                if (index.ReferencedBy(guid).Count == 0)
                {
                    GrayboxProjectValidator.Add(findings, ValidationSeverity.Warning, Name,
                        $"The {kind} '{asset.name}' is not referenced by any prefab, scene or asset in the sandbox. " +
                        "If code or a builder looks it up by path, ignore this.", path);
                }
            }
        }
    }
}
