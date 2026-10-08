using System;
using HoldTheHill.Features.Enemies;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Economy manager tracking Gold earnings from enemy bounties, spendings on tower builds/upgrades,
    /// and refunds on tower dismantling.
    /// </summary>
    public class GrayboxEconomy : MonoBehaviour
    {
        public static GrayboxEconomy Instance { get; private set; }

        public static event Action<int> OnGoldChanged;

        /// <summary>Gold paid for a kill, after skill bonuses. Refunds and cheats do not raise it.</summary>
        public static event Action<int> OnBountyPaid;

        [Header("Starting Economy")]
        [SerializeField] private int _startingGold = 500;

        public int CurrentGold { get; private set; }

        private void Awake()
        {
            Instance = this;
            CurrentGold = _startingGold;
        }

        private void OnEnable()
        {
            EnemyHealth.Defeated += OnEnemyDefeated;
        }

        private void OnDisable()
        {
            EnemyHealth.Defeated -= OnEnemyDefeated;
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
                    EarnGold(bounty);
                    if (bounty > 0) OnBountyPaid?.Invoke(bounty);
                }
            }
        }

        public void EarnGold(int amount)
        {
            if (amount <= 0) return;
            CurrentGold += amount;
            OnGoldChanged?.Invoke(CurrentGold);
        }

        public bool CanAfford(int amount)
        {
            return Instance == null || CurrentGold >= amount;
        }

        public bool TrySpendGold(int amount)
        {
            if (amount <= 0) return true;
            if (!CanAfford(amount)) return false;

            CurrentGold -= amount;
            OnGoldChanged?.Invoke(CurrentGold);
            return true;
        }

        public void ResetGold(int amount = 500)
        {
            CurrentGold = amount;
            OnGoldChanged?.Invoke(CurrentGold);
        }
    }
}
