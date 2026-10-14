using System;
using HoldTheHill.Features.Enemies;
using HoldTheHill.Features.Progression;
using UnityEngine;
using UnityEngine.Serialization;

namespace HoldTheHill.Features.Economy
{
    /// <summary>
    /// Puts a <see cref="Wallet"/> in the scene and pays enemy bounties into it. Thin: the rules
    /// are in <see cref="Wallet"/>.
    /// </summary>
    /// <remarks>
    /// Bounties come from <see cref="EnemyHealth.Defeated"/>, multiplied by
    /// <see cref="IUpgradeModifiers.BountyMultiplier"/>. Refunds and cheats are not bounties and
    /// do not raise <see cref="BountyPaid"/>.
    /// </remarks>
    [AddComponentMenu("Hold the Hill/Economy/Wallet")]
    public class WalletBehaviour : MonoBehaviour
    {
        [Header("Starting Economy")]
        [Tooltip("The main resource: what bounties pay and towers cost by default. Its starting amount is used when set.")]
        [SerializeField] private ResourceData _mainResource;

        [Tooltip("Starting amount when no main resource is assigned.")]
        [SerializeField, Min(0), FormerlySerializedAs("_startingFood"), FormerlySerializedAs("_startingGold")]
        private int _startingAmount = 500;

        private Wallet _wallet;

        /// <summary>Raised when a bounty is paid, with the amount after multipliers.</summary>
        public event Action<int> BountyPaid;

        /// <summary>
        /// The wallet. Made on first use, so it is there before Awake for anything that asks early;
        /// follows the main resource if that is assigned later (a builder, a test).
        /// </summary>
        public Wallet Wallet
        {
            get
            {
                if (_wallet == null)
                {
                    int start = _mainResource != null ? _mainResource.StartingAmount : _startingAmount;
                    _wallet = new Wallet(_mainResource, start);
                }
                else if (_wallet.Main != _mainResource)
                {
                    _wallet.Main = _mainResource;
                }

                return _wallet;
            }
        }

        /// <summary>The main resource, or null when none is assigned.</summary>
        public ResourceData MainResource => _mainResource;

        /// <summary>How much of the main resource (Food) the player holds.</summary>
        public int CurrentFood => Wallet.MainAmount;

        /// <summary>How much of a resource the player holds. Null means the main resource.</summary>
        public int Get(ResourceData resource) => Wallet.Get(resource);

        public void Earn(ResourceData resource, int amount) => Wallet.Earn(resource, amount);

        public bool CanAfford(ResourceData resource, int amount) => Wallet.CanAfford(resource, amount);

        public bool TrySpend(ResourceData resource, int amount) => Wallet.TrySpend(resource, amount);

        public void EarnFood(int amount) => Wallet.Earn(null, amount);

        public bool CanAfford(int amount) => Wallet.CanAfford(null, amount);

        public bool TrySpendFood(int amount) => Wallet.TrySpend(null, amount);

        public void ResetFood(int amount = 500) => Wallet.Set(null, amount);

        protected virtual void OnEnable()
        {
            EnemyHealth.Defeated += OnEnemyDefeated;
        }

        protected virtual void OnDisable()
        {
            EnemyHealth.Defeated -= OnEnemyDefeated;
        }

        private void OnEnemyDefeated(GameObject enemy)
        {
            if (enemy == null)
            {
                return;
            }

            var health = enemy.GetComponent<EnemyHealth>();
            if (health == null)
            {
                return;
            }

            int bounty = Mathf.RoundToInt(health.BountyValue * UpgradeModifiers.Current.BountyMultiplier);
            if (bounty <= 0)
            {
                return;
            }

            Wallet.Earn(null, bounty);
            BountyPaid?.Invoke(bounty);
        }
    }
}
