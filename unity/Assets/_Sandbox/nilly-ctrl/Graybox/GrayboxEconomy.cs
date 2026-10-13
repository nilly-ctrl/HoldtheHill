using System;
using HoldTheHill.Features.Economy;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// The graybox's wallet: a <see cref="WalletBehaviour"/> with the static <c>Instance</c>,
    /// events and Food shorthands the sandbox still reaches for. A shim until the scenes hold a
    /// reference to the wallet themselves (step 4b of <c>docs/CODEBASE_CONVERGENCE.md</c>).
    /// </summary>
    public class GrayboxEconomy : WalletBehaviour
    {
        public static GrayboxEconomy Instance { get; private set; }

        public static event Action<int> OnFoodChanged;

        /// <summary>Food paid for a kill, after skill bonuses. Refunds and cheats do not raise it.</summary>
        public static event Action<int> OnBountyPaid;

        /// <summary>Raised when the amount of any resource changes, with the resource and its new amount.</summary>
        public static event Action<ResourceData, int> OnResourceChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
            OnFoodChanged = null;
            OnBountyPaid = null;
            OnResourceChanged = null;
        }

        public int CurrentFood => Wallet.MainAmount;

        private void Awake()
        {
            Instance = this;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Wallet.Changed += ForwardChanged;
            BountyPaid += ForwardBounty;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Wallet.Changed -= ForwardChanged;
            BountyPaid -= ForwardBounty;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void ForwardChanged(ResourceData resource, int amount)
        {
            if (resource == null || resource == MainResource)
            {
                OnFoodChanged?.Invoke(amount);
            }

            OnResourceChanged?.Invoke(resource, amount);
        }

        private static void ForwardBounty(int amount) => OnBountyPaid?.Invoke(amount);

        /// <summary>How much of a resource the player holds. Null means Food.</summary>
        public int Get(ResourceData resource) => Wallet.Get(resource);

        public void Earn(ResourceData resource, int amount) => Wallet.Earn(resource, amount);

        public bool CanAfford(ResourceData resource, int amount) => Wallet.CanAfford(resource, amount);

        public bool TrySpend(ResourceData resource, int amount) => Wallet.TrySpend(resource, amount);

        public void EarnFood(int amount) => Wallet.Earn(null, amount);

        public bool CanAfford(int amount) => Wallet.CanAfford(null, amount);

        public bool TrySpendFood(int amount) => Wallet.TrySpend(null, amount);

        public void ResetFood(int amount = 500) => Wallet.Set(null, amount);
    }
}
