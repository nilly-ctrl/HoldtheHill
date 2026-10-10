using System;
using System.Collections.Generic;

namespace HoldTheHill.Sandbox.NillyCtrl.Editor
{
    /// <summary>Prerequisite checks for one skill tree. Pure C#.</summary>
    public static class SkillGraphRules
    {
        public static List<SkillGraphProblem> Analyse(IReadOnlyList<SkillNodeRecord> nodes)
        {
            var problems = new List<SkillGraphProblem>();
            if (nodes == null)
            {
                return problems;
            }

            var byId = new Dictionary<string, SkillNodeRecord>(StringComparer.Ordinal);
            foreach (SkillNodeRecord node in nodes)
            {
                if (node != null && node.Id != null && !byId.ContainsKey(node.Id))
                {
                    byId[node.Id] = node;
                }
            }

            foreach (SkillNodeRecord node in nodes)
            {
                if (node == null)
                {
                    continue;
                }

                if (node.PrerequisiteMissing)
                {
                    problems.Add(new SkillGraphProblem(SkillGraphProblemKind.PrerequisiteMissing, node.Id,
                        "its prerequisite asset has been deleted"));
                    continue;
                }

                if (string.IsNullOrEmpty(node.PrerequisiteId))
                {
                    continue;
                }

                if (node.PrerequisiteId == node.Id)
                {
                    problems.Add(new SkillGraphProblem(SkillGraphProblemKind.SelfPrerequisite, node.Id,
                        "it lists itself as its prerequisite"));
                    continue;
                }

                if (!byId.ContainsKey(node.PrerequisiteId))
                {
                    problems.Add(new SkillGraphProblem(SkillGraphProblemKind.PrerequisiteNotInTree, node.Id,
                        $"prerequisite '{node.PrerequisiteId}' is not in this tree"));
                    continue;
                }

                if (LeadsBackTo(node, byId))
                {
                    problems.Add(new SkillGraphProblem(SkillGraphProblemKind.Cycle, node.Id,
                        "its prerequisites loop back to it"));
                }
            }

            return problems;
        }

        // Walks the prerequisite chain from the node; a repeat of the start (or any loop) is a cycle.
        private static bool LeadsBackTo(SkillNodeRecord start, Dictionary<string, SkillNodeRecord> byId)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal) { start.Id };
            string next = start.PrerequisiteId;

            while (!string.IsNullOrEmpty(next))
            {
                if (next == start.Id)
                {
                    return true;
                }

                if (!seen.Add(next))
                {
                    // A loop that does not include the start; it is reported on its own members.
                    return false;
                }

                if (!byId.TryGetValue(next, out SkillNodeRecord node))
                {
                    return false;
                }

                next = node.PrerequisiteId;
            }

            return false;
        }
    }
}
