using System;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// The permanent layer of progression: a currency kept between runs and the upgrades bought
    /// with it at Home. Only the bookkeeping lives here; what the currency is called, how runs
    /// earn it and what the upgrades do are still open (docs/PLAN_MENUS_AND_FLOW.md).
    /// </summary>
    public static class GrayboxMetaProgress
    {
        /// <summary>Raised with the new balance whenever it changes.</summary>
        public static event Action<int> CurrencyChanged;

        public static int Currency => GrayboxSave.Data.metaCurrency;

        public static void AddCurrency(int amount)
        {
            if (amount <= 0) return;

            GrayboxSave.Data.metaCurrency += amount;
            GrayboxSave.MarkDirty();
            CurrencyChanged?.Invoke(Currency);
        }

        public static bool HasUpgrade(string upgradeId) => GrayboxSave.Data.purchasedUpgrades.Contains(upgradeId);

        /// <summary>Buys an upgrade once. False if it is already owned or costs too much.</summary>
        public static bool TryBuyUpgrade(string upgradeId, int cost)
        {
            if (string.IsNullOrEmpty(upgradeId) || cost < 0 || HasUpgrade(upgradeId) || Currency < cost)
            {
                return false;
            }

            GrayboxSave.Data.metaCurrency -= cost;
            GrayboxSave.Data.purchasedUpgrades.Add(upgradeId);
            GrayboxSave.MarkDirty();
            GrayboxSave.Save(); // a purchase should never be lost to a crash
            CurrencyChanged?.Invoke(Currency);
            return true;
        }
    }
}
