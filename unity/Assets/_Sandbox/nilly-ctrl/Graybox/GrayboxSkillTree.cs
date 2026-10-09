using System;
using System.Collections.Generic;
using HoldTheHill.Features.Progression;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>A skill node in play: its data asset and whether the player owns it yet.</summary>
    public class SkillNode
    {
        public SkillNode(GrayboxSkillNodeData data)
        {
            Data = data;
        }

        public GrayboxSkillNodeData Data { get; }

        public bool isUnlocked;

        public string id => Data.Id;

        public string name => Data.DisplayName;

        public string description => Data.Description;

        public string branch => Data.Branch;

        public int cost => Data.Cost;

        /// <summary>The id of the node that must be unlocked first, or null for a root node.</summary>
        public string prerequisite => Data.Prerequisite != null ? Data.Prerequisite.Id : null;
    }

    /// <summary>
    /// The skill tree: spends skill points on the nodes of a <see cref="GrayboxSkillTreeData"/> and
    /// folds each owned node's <see cref="SkillEffect"/>s into the stats towers and the economy read.
    /// </summary>
    public class GrayboxSkillTree : MonoBehaviour, IUpgradeModifiers
    {
        // What each stat is worth with nothing unlocked, indexed by SkillStat.
        private static readonly float[] BaseValues = { 1f, 1f, 1f, 0f, 0f, 1f, 1f, 1f, 0.75f };

        public static GrayboxSkillTree Instance { get; private set; }

        public static event Action<string> OnSkillUnlocked;
        public static event Action<int> OnSkillPointsChanged;

        [Tooltip("The nodes of the tree.")]
        [SerializeField] private GrayboxSkillTreeData _catalog;

        [Header("Starting Economy")]
        [SerializeField] private int _startingSkillPoints = 5;

        public int SkillPoints { get; private set; }

        // Kept in catalog order, so a column draws top to bottom as authored.
        private readonly List<SkillNode> _nodes = new List<SkillNode>();
        private readonly Dictionary<string, SkillNode> _byId = new Dictionary<string, SkillNode>();
        private readonly float[] _stats = new float[BaseValues.Length];

        public float DamageMultiplier => _stats[(int)SkillStat.DamageMultiplier];
        public float FireRateMultiplier => _stats[(int)SkillStat.FireRateMultiplier];
        public float BlastRadiusMultiplier => _stats[(int)SkillStat.BlastRadiusMultiplier];

        public float FrostSlowBonus => _stats[(int)SkillStat.FrostSlowBonus];
        public float KnockbackBonus => _stats[(int)SkillStat.KnockbackBonus];
        public float FrostDebuffDamageMultiplier => _stats[(int)SkillStat.FrostDebuffDamageMultiplier];

        public float BountyMultiplier => _stats[(int)SkillStat.BountyMultiplier];
        public float CostMultiplier => _stats[(int)SkillStat.CostMultiplier];
        public float RefundPercentage => _stats[(int)SkillStat.RefundPercentage];

        private void Awake()
        {
            Instance = this;
            SkillPoints = _startingSkillPoints;
            InitNodes();
            UpgradeModifiers.Register(this);
        }

        private void OnDestroy()
        {
            UpgradeModifiers.Unregister(this);
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>Swaps the tree's nodes. Everything is locked again and skill points go back to the start.</summary>
        public void Configure(GrayboxSkillTreeData catalog)
        {
            _catalog = catalog;
            SkillPoints = _startingSkillPoints;
            InitNodes();
            OnSkillPointsChanged?.Invoke(SkillPoints);
        }

        private void InitNodes()
        {
            _nodes.Clear();
            _byId.Clear();

            if (_catalog != null)
            {
                foreach (GrayboxSkillNodeData data in _catalog.Nodes)
                {
                    if (data == null || _byId.ContainsKey(data.Id))
                    {
                        continue;
                    }

                    var node = new SkillNode(data);
                    _nodes.Add(node);
                    _byId[data.Id] = node;
                }
            }

            RecalculateStats();
        }

        public SkillNode GetNode(string id)
        {
            _byId.TryGetValue(id ?? string.Empty, out SkillNode node);
            return node;
        }

        public IReadOnlyList<SkillNode> GetAllNodes() => _nodes;

        /// <summary>Branch names in display order, one per column.</summary>
        public List<string> GetBranches() => _catalog != null ? _catalog.Branches() : new List<string>();

        /// <summary>The nodes of one branch, top to bottom.</summary>
        public List<SkillNode> GetBranchNodes(string branch)
        {
            var result = new List<SkillNode>();
            foreach (SkillNode node in _nodes)
            {
                if (node.branch == branch)
                {
                    result.Add(node);
                }
            }

            return result;
        }

        public bool IsUnlocked(string id)
        {
            return _byId.TryGetValue(id ?? string.Empty, out SkillNode node) && node.isUnlocked;
        }

        public bool CanUnlock(string id)
        {
            if (!_byId.TryGetValue(id ?? string.Empty, out SkillNode node)) return false;
            if (node.isUnlocked) return false;
            if (SkillPoints < node.cost) return false;
            if (node.prerequisite != null && !IsUnlocked(node.prerequisite)) return false;
            return true;
        }

        public bool TryUnlock(string id)
        {
            if (!CanUnlock(id)) return false;

            SkillNode node = _byId[id];
            node.isUnlocked = true;
            SkillPoints -= node.cost;

            RecalculateStats();

            OnSkillPointsChanged?.Invoke(SkillPoints);
            OnSkillUnlocked?.Invoke(id);
            return true;
        }

        public void AddSkillPoints(int amount)
        {
            SkillPoints += amount;
            OnSkillPointsChanged?.Invoke(SkillPoints);
        }

        // Every stat starts at its base value and each owned node's effects are applied in catalog order.
        private void RecalculateStats()
        {
            Array.Copy(BaseValues, _stats, BaseValues.Length);

            foreach (SkillNode node in _nodes)
            {
                if (!node.isUnlocked)
                {
                    continue;
                }

                foreach (SkillEffect effect in node.Data.Effects)
                {
                    int index = (int)effect.stat;
                    switch (effect.mode)
                    {
                        case SkillEffectMode.Multiply:
                            _stats[index] *= effect.value;
                            break;
                        case SkillEffectMode.Add:
                            _stats[index] += effect.value;
                            break;
                        default:
                            _stats[index] = effect.value;
                            break;
                    }
                }
            }
        }

        /// <summary>The nodes unlocked right now, for saving a checkpoint.</summary>
        public List<string> GetUnlockedIds()
        {
            var ids = new List<string>();
            foreach (SkillNode node in _nodes)
            {
                if (node.isUnlocked) ids.Add(node.id);
            }
            return ids;
        }

        /// <summary>Puts skill points and unlocked nodes back to a saved checkpoint.</summary>
        public void RestoreState(int skillPoints, ICollection<string> unlocked)
        {
            SkillPoints = skillPoints;
            foreach (SkillNode node in _nodes)
            {
                node.isUnlocked = unlocked != null && unlocked.Contains(node.id);
            }
            RecalculateStats();
            OnSkillPointsChanged?.Invoke(SkillPoints);
        }

        public void ResetSkills()
        {
            SkillPoints = _startingSkillPoints;
            foreach (SkillNode node in _nodes)
            {
                node.isUnlocked = false;
            }
            RecalculateStats();
            OnSkillPointsChanged?.Invoke(SkillPoints);
        }
    }
}
