using System;

namespace HoldTheHill.Core.Save
{
    /// <summary>
    /// The permanent layer of progression: a currency kept between runs and the upgrades bought
    /// with it at Home. Only the bookkeeping lives here; what the currency is called, how runs
    /// earn it and what the upgrades do are still open (docs/PLAN_MENUS_AND_FLOW.md).
    /// </summary>
    public static class MetaProgress
    {
        /// <summary>Raised with the new balance whenever it changes.</summary>
        public static event Action<int> CurrencyChanged;

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => CurrencyChanged = null;

        public static int Currency => SaveStore.Data.metaCurrency;

        public static void AddCurrency(int amount)
        {
            if (amount <= 0) return;

            SaveStore.Data.metaCurrency += amount;
            SaveStore.MarkDirty();
            CurrencyChanged?.Invoke(Currency);
        }

        public static bool HasUpgrade(string upgradeId) => SaveStore.Data.purchasedUpgrades.Contains(upgradeId);

        /// <summary>Buys an upgrade once. False if it is already owned or costs too much.</summary>
        public static bool TryBuyUpgrade(string upgradeId, int cost)
        {
            if (string.IsNullOrEmpty(upgradeId) || cost < 0 || HasUpgrade(upgradeId) || Currency < cost)
            {
                return false;
            }

            SaveStore.Data.metaCurrency -= cost;
            SaveStore.Data.purchasedUpgrades.Add(upgradeId);
            SaveStore.MarkDirty();
            SaveStore.Save(); // a purchase should never be lost to a crash
            CurrencyChanged?.Invoke(Currency);
            return true;
        }
    }
}
