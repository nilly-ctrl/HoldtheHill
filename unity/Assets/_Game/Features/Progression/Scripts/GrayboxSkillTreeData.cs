using System.Collections.Generic;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// The nodes of a skill tree, in display order. The tree draws one column per branch name,
    /// in the order the branches first appear here.
    /// </summary>
    [CreateAssetMenu(menuName = "Hold the Hill/Sandbox/Graybox Skill Tree Data", fileName = "GrayboxSkillTreeData")]
    public class GrayboxSkillTreeData : ScriptableObject
    {
        [SerializeField] private List<GrayboxSkillNodeData> _nodes = new List<GrayboxSkillNodeData>();

        public IReadOnlyList<GrayboxSkillNodeData> Nodes => _nodes;

        /// <summary>Branch names in the order they first appear.</summary>
        public List<string> Branches()
        {
            var branches = new List<string>();
            foreach (GrayboxSkillNodeData node in _nodes)
            {
                if (node != null && !branches.Contains(node.Branch))
                {
                    branches.Add(node.Branch);
                }
            }

            return branches;
        }

        /// <summary>Makes a catalog in code, for tests and trees built at run time.</summary>
        public static GrayboxSkillTreeData Create(params GrayboxSkillNodeData[] nodes)
        {
            var data = CreateInstance<GrayboxSkillTreeData>();
            data._nodes = new List<GrayboxSkillNodeData>(nodes);
            return data;
        }
    }
}
