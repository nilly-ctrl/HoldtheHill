using System;
using System.Collections.Generic;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>A number the skill tree can change. These are the ones towers and the economy read.</summary>
    public enum SkillStat
    {
        DamageMultiplier,
        FireRateMultiplier,
        BlastRadiusMultiplier,
        FrostSlowBonus,
        KnockbackBonus,
        FrostDebuffDamageMultiplier,
        BountyMultiplier,
        CostMultiplier,
        RefundPercentage
    }

    /// <summary>How a skill effect combines with the stat's current value.</summary>
    public enum SkillEffectMode
    {
        /// <summary>The stat is multiplied by the value (1.2 = +20%).</summary>
        Multiply,

        /// <summary>The value is added to the stat.</summary>
        Add,

        /// <summary>The stat becomes the value, whatever it was.</summary>
        Set
    }

    /// <summary>One change a skill node makes to one stat.</summary>
    [Serializable]
    public struct SkillEffect
    {
        public SkillStat stat;
        public SkillEffectMode mode;
        public float value;
    }

    /// <summary>
    /// One node of the skill tree: its name, price, the node it needs first, and the stats it
    /// changes. Add a node by making one of these and listing it in the tree's catalog.
    /// </summary>
    [CreateAssetMenu(menuName = "Hold the Hill/Sandbox/Graybox Skill Node Data", fileName = "SkillNodeData")]
    public class GrayboxSkillNodeData : ScriptableObject
    {
        [Tooltip("Short unique id, e.g. Ballistics_HeavyCaliber. Saved with a run, so don't rename it later.")]
        [SerializeField] private string _id;

        [Tooltip("Name shown to the player.")]
        [SerializeField] private string _displayName;

        [Tooltip("One line on what it does.")]
        [SerializeField, TextArea(1, 3)] private string _description;

        [Tooltip("Column the node sits in, e.g. Ballistics. Nodes with the same branch share a column, in catalog order.")]
        [SerializeField] private string _branch;

        [Tooltip("Skill points it costs.")]
        [SerializeField, Min(0)] private int _cost = 1;

        [Tooltip("Must be unlocked first. Leave empty for a root node.")]
        [SerializeField] private GrayboxSkillNodeData _prerequisite;

        [Tooltip("Icon file name without the extension. Empty uses 'Skill' + the id without underscores + 'Icon'.")]
        [SerializeField] private string _iconName;

        [Tooltip("What owning the node changes.")]
        [SerializeField] private List<SkillEffect> _effects = new List<SkillEffect>();

        public string Id => string.IsNullOrEmpty(_id) ? name : _id;

        public string DisplayName => string.IsNullOrEmpty(_displayName) ? Id : _displayName;

        public string Description => _description;

        public string Branch => _branch;

        public int Cost => _cost;

        public GrayboxSkillNodeData Prerequisite => _prerequisite;

        public IReadOnlyList<SkillEffect> Effects => _effects;

        public string IconName => string.IsNullOrEmpty(_iconName)
            ? $"Skill{Id.Replace("_", string.Empty)}Icon"
            : _iconName;

        /// <summary>Makes a node in code, for tests and trees built at run time.</summary>
        public static GrayboxSkillNodeData Create(string id, string displayName, string description, string branch,
            int cost, GrayboxSkillNodeData prerequisite, params SkillEffect[] effects)
        {
            var data = CreateInstance<GrayboxSkillNodeData>();
            data.name = id;
            data._id = id;
            data._displayName = displayName;
            data._description = description;
            data._branch = branch;
            data._cost = cost;
            data._prerequisite = prerequisite;
            data._effects = new List<SkillEffect>(effects);
            return data;
        }
    }
}
