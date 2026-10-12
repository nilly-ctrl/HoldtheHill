using System;
using System.Collections.Generic;
using HoldTheHill.Features.Enemies;
using UnityEngine;
using UnityEngine.Serialization;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Economy manager tracking Gold earnings from enemy bounties, spendings on tower builds/upgrades,
    /// and refunds on tower dismantling.
    /// </summary>
    /// <remarks>
    /// This is also the run's wallet. "Gold" is the main resource (the Food asset, when one is
    /// assigned); <see cref="Get"/>, <see cref="Earn"/>, <see cref="CanAfford(GrayboxResourceData, int)"/>
    /// and <see cref="TrySpend"/> take any <see cref="GrayboxResourceData"/>, and treat null as
    /// the main one.
    /// </remarks>
    public class GrayboxEconomy : MonoBehaviour
    {
        public static GrayboxEconomy Instance { get; private set; }

        public static event Action<int> OnFoodChanged;

        /// <summary>Gold paid for a kill, after skill bonuses. Refunds and cheats do not raise it.</summary>
        public static event Action<int> OnBountyPaid;

        /// <summary>Raised when the amount of any resource changes, with the resource and its new amount.</summary>
        public static event Action<GrayboxResourceData, int> OnResourceChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
            OnFoodChanged = null;
            OnBountyPaid = null;
            OnResourceChanged = null;
        }

        [Header("Starting Economy")]
        [Tooltip("The main resource: what bounties pay and towers cost by default. Its starting amount is used when set.")]
        [SerializeField] private GrayboxResourceData _mainResource;
        [Tooltip("Starting amount when no main resource is assigned.")]
        [SerializeField, FormerlySerializedAs("_startingGold")] private int _startingFood = 500;

        // Resources other than the main one.
        private readonly Dictionary<GrayboxResourceData, int> _others = new Dictionary<GrayboxResourceData, int>();

        public int CurrentFood { get; private set; }

        public GrayboxResourceData MainResource => _mainResource;

        private void Awake()
        {
            Instance = this;
            CurrentFood = _mainResource != null ? _mainResource.StartingAmount : _startingFood;
        }

        private bool IsMain(GrayboxResourceData resource) => resource == null || resource == _mainResource;

        /// <summary>How much of a resource the player holds. A resource never earned starts at its starting amount.</summary>
        public int Get(GrayboxResourceData resource)
        {
            if (IsMain(resource))
            {
                return CurrentFood;
            }

            return _others.TryGetValue(resource, out int amount) ? amount : resource.StartingAmount;
        }

        public void Earn(GrayboxResourceData resource, int amount)
        {
            if (amount <= 0) return;

            if (IsMain(resource))
            {
                EarnFood(amount);
                return;
            }

            Set(resource, Get(resource) + amount);
        }

        public bool CanAfford(GrayboxResourceData resource, int amount)
        {
            return Get(resource) >= amount;
        }

        public bool TrySpend(GrayboxResourceData resource, int amount)
        {
            if (IsMain(resource))
            {
                return TrySpendFood(amount);
            }

            if (amount <= 0) return true;
            if (!CanAfford(resource, amount)) return false;

            Set(resource, Get(resource) - amount);
            return true;
        }

        private void Set(GrayboxResourceData resource, int amount)
        {
            _others[resource] = amount;
            OnResourceChanged?.Invoke(resource, amount);
        }

        private void OnEnable()
        {
            EnemyHealth.Defeated += OnEnemyDefeated;
        }

        private void OnDisable()
        {
            EnemyHealth.Defeated -= OnEnemyDefeated;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnEnemyDefeated(GameObject enemyObj)
        {
            if (enemyObj != null)
            {
                var health = enemyObj.GetComponent<EnemyHealth>();
                if (health != null)
                {
                    float mult = GrayboxSkillTree.Instance != null ? GrayboxSkillTree.Instance.BountyMultiplier : 1.0f;
                    mult *= GrayboxUpgrades.BountyMultiplier; // permanent upgrade bought at Home
                    int bounty = Mathf.RoundToInt(health.BountyValue * mult);
                    EarnFood(bounty);
                    if (bounty > 0) OnBountyPaid?.Invoke(bounty);
                }
            }
        }

        public void EarnFood(int amount)
        {
            if (amount <= 0) return;
            CurrentFood += amount;
            OnFoodChanged?.Invoke(CurrentFood);
            OnResourceChanged?.Invoke(_mainResource, CurrentFood);
        }

        public bool CanAfford(int amount)
        {
            return Instance == null || CurrentFood >= amount;
        }

        public bool TrySpendFood(int amount)
        {
            if (amount <= 0) return true;
            if (!CanAfford(amount)) return false;

            CurrentFood -= amount;
            OnFoodChanged?.Invoke(CurrentFood);
            OnResourceChanged?.Invoke(_mainResource, CurrentFood);
            return true;
        }

        public void ResetFood(int amount = 500)
        {
            CurrentFood = amount;
            OnFoodChanged?.Invoke(CurrentFood);
            OnResourceChanged?.Invoke(_mainResource, CurrentFood);
        }
    }
}
