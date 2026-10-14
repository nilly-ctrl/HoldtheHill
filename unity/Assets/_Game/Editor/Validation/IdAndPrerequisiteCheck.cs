using HoldTheHill.Features.PowerUps;
using HoldTheHill.Features.Progression;
using HoldTheHill.Features.Specials;
using HoldTheHill.Features.Species;
using HoldTheHill.Features.Economy;
using System.Collections.Generic;
using UnityEngine;

namespace HoldTheHill.EditorTools.Validation
{
    /// <summary>
    /// Duplicate string ids (skill nodes, achievements and the other id-keyed data) and skill-node
    /// prerequisites that do not exist, sit outside the tree, point at themselves or loop.
    /// </summary>
    public static class IdAndPrerequisiteCheck
    {
        private const string Name = "Ids";

        public static void Run(string root, ProjectTextIndex index, List<ValidationFinding> findings)
        {
            var nodes = GrayboxProjectValidator.FindAll<GrayboxSkillNodeData>(root);
            var skillIds = new List<KeyValuePair<string, string>>();
            foreach (var (path, node) in nodes) skillIds.Add(new KeyValuePair<string, string>(node.Id, path));
            ReportDuplicates(findings, skillIds, "skill node");

            var achievements = new List<KeyValuePair<string, string>>();
            foreach (var (path, item) in GrayboxProjectValidator.FindAll<GrayboxAchievementData>(root))
                achievements.Add(new KeyValuePair<string, string>(item.Id, path));
            ReportDuplicates(findings, achievements, "achievement");

            var others = new List<KeyValuePair<string, string>>();
            foreach (var (path, item) in GrayboxProjectValidator.FindAll<GrayboxBossData>(root)) others.Add(new KeyValuePair<string, string>(item.Id, path));
            ReportDuplicates(findings, others, "boss");
            others.Clear();
            foreach (var (path, item) in GrayboxProjectValidator.FindAll<GrayboxPowerUpData>(root)) others.Add(new KeyValuePair<string, string>(item.Id, path));
            ReportDuplicates(findings, others, "power-up");
            others.Clear();
            foreach (var (path, item) in GrayboxProjectValidator.FindAll<GrayboxSpeciesData>(root)) others.Add(new KeyValuePair<string, string>(item.Id, path));
            ReportDuplicates(findings, others, "species");
            others.Clear();
            foreach (var (path, item) in GrayboxProjectValidator.FindAll<ResourceData>(root)) others.Add(new KeyValuePair<string, string>(item.Id, path));
            ReportDuplicates(findings, others, "resource");

            CheckPrerequisites(root, index, findings);
        }

        private static void ReportDuplicates(List<ValidationFinding> findings,
            List<KeyValuePair<string, string>> idsAndPaths, string kind)
        {
            foreach (KeyValuePair<string, List<string>> duplicate in DuplicateIdRules.Find(idsAndPaths))
            {
                string first = duplicate.Value[0];
                GrayboxProjectValidator.Add(findings, ValidationSeverity.Error, Name,
                    $"Duplicate {kind} id '{duplicate.Key}' used by {duplicate.Value.Count} assets: " +
                    string.Join(", ", duplicate.Value), first);
            }
        }

        private static void CheckPrerequisites(string root, ProjectTextIndex index, List<ValidationFinding> findings)
        {
            foreach (var (treePath, tree) in GrayboxProjectValidator.FindAll<GrayboxSkillTreeData>(root))
            {
                var records = new List<SkillNodeRecord>();
                var pathById = new Dictionary<string, string>();

                foreach (GrayboxSkillNodeData node in tree.Nodes)
                {
                    if (node == null)
                    {
                        continue;
                    }

                    string nodePath = UnityEditor.AssetDatabase.GetAssetPath(node);
                    YamlReference raw = index.FieldReference(nodePath, "_prerequisite");
                    bool missing = node.Prerequisite == null && raw != null;

                    records.Add(new SkillNodeRecord(node.Id, node.Prerequisite != null ? node.Prerequisite.Id : null, missing));
                    pathById[node.Id] = nodePath;
                }

                foreach (SkillGraphProblem problem in SkillGraphRules.Analyse(records))
                {
                    pathById.TryGetValue(problem.NodeId, out string nodePath);
                    GrayboxProjectValidator.Add(findings, ValidationSeverity.Error, Name,
                        $"Skill node '{problem.NodeId}': {problem.Detail}.", nodePath ?? treePath);
                }
            }

            // A node that sits in no tree is reported by the catalog check; its prerequisite is
            // still worth a look when the asset it points at has been deleted.
            foreach (var (path, node) in GrayboxProjectValidator.FindAll<GrayboxSkillNodeData>(root))
            {
                YamlReference raw = index.FieldReference(path, "_prerequisite");
                if (node.Prerequisite == null && raw != null && !AlreadyReported(findings, path))
                {
                    GrayboxProjectValidator.Add(findings, ValidationSeverity.Error, Name,
                        $"Skill node '{node.Id}': its prerequisite asset has been deleted.", path);
                }
            }
        }

        private static bool AlreadyReported(List<ValidationFinding> findings, string path)
        {
            foreach (ValidationFinding finding in findings)
            {
                if (finding.Check == Name && finding.AssetPath == path && finding.Message.Contains("prerequisite"))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
