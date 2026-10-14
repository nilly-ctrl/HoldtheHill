using HoldTheHill.Features.Economy;

namespace HoldTheHill.Features.Economy
{
    /// <summary>
    /// The graybox wallet: a <see cref="WalletBehaviour"/> with the Food shorthands the sandbox
    /// uses. No statics: the sandbox finds the wallet once and listens to <see cref="Wallet.Changed"/>
    /// and <see cref="WalletBehaviour.BountyPaid"/> themselves (step 4b of
    /// <c>docs/CODEBASE_CONVERGENCE.md</c>).
    /// </summary>
    public class GrayboxEconomy : WalletBehaviour
    {
        public int CurrentFood => Wallet.MainAmount;

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
