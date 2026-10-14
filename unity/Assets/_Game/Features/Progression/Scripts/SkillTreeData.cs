using System.Collections.Generic;
using UnityEngine;

namespace HoldTheHill.Features.Progression
{
    /// <summary>
    /// The nodes of a skill tree, in display order. The tree draws one column per branch name,
    /// in the order the branches first appear here.
    /// </summary>
    [CreateAssetMenu(menuName = "Hold the Hill/Sandbox/Graybox Skill Tree Data", fileName = "SkillTreeData")]
    public class SkillTreeData : ScriptableObject
    {
        [SerializeField] private List<SkillNodeData> _nodes = new List<SkillNodeData>();

        public IReadOnlyList<SkillNodeData> Nodes => _nodes;

        /// <summary>Branch names in the order they first appear.</summary>
        public List<string> Branches()
        {
            var branches = new List<string>();
            foreach (SkillNodeData node in _nodes)
            {
                if (node != null && !branches.Contains(node.Branch))
                {
                    branches.Add(node.Branch);
                }
            }

            return branches;
        }

        /// <summary>Makes a catalog in code, for tests and trees built at run time.</summary>
        public static SkillTreeData Create(params SkillNodeData[] nodes)
        {
            var data = CreateInstance<SkillTreeData>();
            data._nodes = new List<SkillNodeData>(nodes);
            return data;
        }
    }
}
