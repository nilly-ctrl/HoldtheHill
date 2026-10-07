using System;
using System.Collections.Generic;
using HoldTheHill.Features.Progression;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    public enum SkillNodeId
    {
        Ballistics_HeavyCaliber,
        Ballistics_RapidCycling,
        Ballistics_ExplosivePayload,

        Control_DeepFreeze,
        Control_HeavyShockwave,
        Control_AbsoluteZero,

        Economy_ScavengerBounties,
        Economy_BulkDiscounts,
        Economy_SalvageMastery
    }

    [System.Serializable]
    public class SkillNode
    {
        public SkillNodeId id;
        public string name;
        public string description;
        public string branch;
        public int cost;
        public SkillNodeId prerequisite = (SkillNodeId)(-1);
        public bool isUnlocked;
    }

    /// <summary>
    /// Test skill tree system providing branch upgrades for combat damage, crowd control, and economy.
    /// </summary>
    public class GrayboxSkillTree : MonoBehaviour, IUpgradeModifiers
    {
        public static GrayboxSkillTree Instance { get; private set; }

        public static event Action<SkillNodeId> OnSkillUnlocked;
        public static event Action<int> OnSkillPointsChanged;

        [Header("Starting Economy")]
        [SerializeField] private int _startingSkillPoints = 5;

        public int SkillPoints { get; private set; }

        private readonly Dictionary<SkillNodeId, SkillNode> _nodes = new Dictionary<SkillNodeId, SkillNode>();

        public float DamageMultiplier { get; private set; } = 1.0f;
        public float FireRateMultiplier { get; private set; } = 1.0f;
        public float BlastRadiusMultiplier { get; private set; } = 1.0f;

        public float FrostSlowBonus { get; private set; } = 0.0f;
        public float KnockbackBonus { get; private set; } = 0.0f;
        public float FrostDebuffDamageMultiplier { get; private set; } = 1.0f;

        public float BountyMultiplier { get; private set; } = 1.0f;
        public float CostMultiplier { get; private set; } = 1.0f;
        public float RefundPercentage { get; private set; } = 0.75f;

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

        private void InitNodes()
        {
            _nodes.Clear();

            // Branch 1: Ballistics & Firepower
            AddNode(SkillNodeId.Ballistics_HeavyCaliber, "Heavy Caliber", "+20% Damage for all towers.", "Ballistics", 1);
            AddNode(SkillNodeId.Ballistics_RapidCycling, "Rapid Cycling", "+25% Fire Rate for all towers.", "Ballistics", 2, SkillNodeId.Ballistics_HeavyCaliber);
            AddNode(SkillNodeId.Ballistics_ExplosivePayload, "Explosive Payload", "+40% Blast Radius for Mortars & Mines.", "Ballistics", 3, SkillNodeId.Ballistics_RapidCycling);

            // Branch 2: Crowd Control & Frost
            AddNode(SkillNodeId.Control_DeepFreeze, "Deep Freeze", "+20% Slow strength for Frost Aura.", "Control", 1);
            AddNode(SkillNodeId.Control_HeavyShockwave, "Heavy Shockwave", "+50% Knockback setback distance.", "Control", 2, SkillNodeId.Control_DeepFreeze);
            AddNode(SkillNodeId.Control_AbsoluteZero, "Absolute Zero", "+30% Damage dealt to slowed enemies.", "Control", 3, SkillNodeId.Control_HeavyShockwave);

            // Branch 3: Logistics & Economy
            AddNode(SkillNodeId.Economy_ScavengerBounties, "Scavenger Bounties", "+25% Gold earned from ant kills.", "Economy", 1);
            AddNode(SkillNodeId.Economy_BulkDiscounts, "Bulk Discounts", "20% Discount on tower builds & upgrades.", "Economy", 2, SkillNodeId.Economy_ScavengerBounties);
            AddNode(SkillNodeId.Economy_SalvageMastery, "Salvage Mastery", "90% Gold refund when dismantling towers.", "Economy", 3, SkillNodeId.Economy_BulkDiscounts);
        }

        private void AddNode(SkillNodeId id, string name, string description, string branch, int cost, SkillNodeId prerequisite = (SkillNodeId)(-1))
        {
            _nodes[id] = new SkillNode
            {
                id = id,
                name = name,
                description = description,
                branch = branch,
                cost = cost,
                prerequisite = prerequisite,
                isUnlocked = false
            };
        }

        public SkillNode GetNode(SkillNodeId id)
        {
            _nodes.TryGetValue(id, out var node);
            return node;
        }

        public IEnumerable<SkillNode> GetAllNodes() => _nodes.Values;

        public bool IsUnlocked(SkillNodeId id)
        {
            return _nodes.TryGetValue(id, out var node) && node.isUnlocked;
        }

        public bool CanUnlock(SkillNodeId id)
        {
            if (!_nodes.TryGetValue(id, out var node)) return false;
            if (node.isUnlocked) return false;
            if (SkillPoints < node.cost) return false;
            if (node.prerequisite != (SkillNodeId)(-1) && !IsUnlocked(node.prerequisite)) return false;
            return true;
        }

        public bool TryUnlock(SkillNodeId id)
        {
            if (!CanUnlock(id)) return false;

            var node = _nodes[id];
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

        private void RecalculateStats()
        {
            DamageMultiplier = IsUnlocked(SkillNodeId.Ballistics_HeavyCaliber) ? 1.20f : 1.0f;
            FireRateMultiplier = IsUnlocked(SkillNodeId.Ballistics_RapidCycling) ? 1.25f : 1.0f;
            BlastRadiusMultiplier = IsUnlocked(SkillNodeId.Ballistics_ExplosivePayload) ? 1.40f : 1.0f;

            FrostSlowBonus = IsUnlocked(SkillNodeId.Control_DeepFreeze) ? 0.20f : 0.0f;
            KnockbackBonus = IsUnlocked(SkillNodeId.Control_HeavyShockwave) ? 0.50f : 0.0f;
            FrostDebuffDamageMultiplier = IsUnlocked(SkillNodeId.Control_AbsoluteZero) ? 1.30f : 1.0f;

            BountyMultiplier = IsUnlocked(SkillNodeId.Economy_ScavengerBounties) ? 1.25f : 1.0f;
            CostMultiplier = IsUnlocked(SkillNodeId.Economy_BulkDiscounts) ? 0.80f : 1.0f;
            RefundPercentage = IsUnlocked(SkillNodeId.Economy_SalvageMastery) ? 0.90f : 0.75f;
        }

        /// <summary>The nodes unlocked right now, for saving a checkpoint.</summary>
        public List<SkillNodeId> GetUnlockedIds()
        {
            var ids = new List<SkillNodeId>();
            foreach (var node in _nodes.Values)
            {
                if (node.isUnlocked) ids.Add(node.id);
            }
            return ids;
        }

        /// <summary>Puts skill points and unlocked nodes back to a saved checkpoint.</summary>
        public void RestoreState(int skillPoints, ICollection<SkillNodeId> unlocked)
        {
            SkillPoints = skillPoints;
            foreach (var node in _nodes.Values)
            {
                node.isUnlocked = unlocked != null && unlocked.Contains(node.id);
            }
            RecalculateStats();
            OnSkillPointsChanged?.Invoke(SkillPoints);
        }

        public void ResetSkills()
        {
            SkillPoints = _startingSkillPoints;
            foreach (var node in _nodes.Values)
            {
                node.isUnlocked = false;
            }
            RecalculateStats();
            OnSkillPointsChanged?.Invoke(SkillPoints);
        }
    }
}
