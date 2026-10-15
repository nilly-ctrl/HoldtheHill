using System;
using System.Collections.Generic;
using HoldTheHill.Features.Economy;
using HoldTheHill.Features.Progression;
using UnityEngine;

namespace HoldTheHill.Features.Towers
{
    /// <summary>
    /// Place, upgrade and sell: the commands a build UI gives, paid from a <see cref="Wallet"/>.
    /// Plain C#, so the rules can be tested without a scene; the placer owns one and listens.
    /// </summary>
    /// <remarks>
    /// Costs are the data's base cost times <see cref="IUpgradeModifiers.CostMultiplier"/>.
    /// Building is paid in the tower's <see cref="TowerData.CostResource"/>; upgrades are paid and
    /// refunds returned in the wallet's main resource. With no wallet everything is free, so a bare
    /// test scene can still build.
    /// </remarks>
    public sealed class TowerCommands
    {
        private readonly Func<Wallet> _wallet;
        private readonly IReadOnlyList<int> _upgradeCosts;

        /// <summary>Raised after a tower is placed, paid or not.</summary>
        public event Action<Tower> Placed;

        /// <summary>Raised after a paid upgrade, with the tower at its new level.</summary>
        public event Action<Tower> Upgraded;

        /// <summary>Raised just before a sold tower is destroyed.</summary>
        public event Action<Tower> Sold;

        /// <summary>
        /// <paramref name="wallet"/> is asked each time, so it may become available after this is
        /// made; <paramref name="upgradeCosts"/> is the cost of each upgrade by the level it is at,
        /// before discounts (the first for level 1 to 2, and so on).
        /// </summary>
        public TowerCommands(Func<Wallet> wallet, IReadOnlyList<int> upgradeCosts)
        {
            _wallet = wallet;
            _upgradeCosts = upgradeCosts ?? Array.Empty<int>();
        }

        private Wallet Wallet => _wallet?.Invoke();

        /// <summary>What a tower costs to build now, after discounts.</summary>
        public int CostOf(TowerData data) => Discounted(data.BaseCost);

        /// <summary>True if the wallet holds the build cost, or there is no wallet.</summary>
        public bool CanAfford(TowerData data)
        {
            Wallet wallet = Wallet;
            return wallet == null || wallet.CanAfford(data.CostResource, CostOf(data));
        }

        /// <summary>What upgrading a tower costs now, after discounts. 0 if it cannot be upgraded.</summary>
        public int UpgradeCost(Tower tower)
        {
            if (tower == null || !tower.CanUpgrade || _upgradeCosts.Count == 0)
            {
                return 0;
            }

            int step = Mathf.Clamp(tower.Level - 1, 0, _upgradeCosts.Count - 1);
            return Discounted(_upgradeCosts[step]);
        }

        /// <summary>Pays for a tower and places it. Null if there is nothing to place or the cost is not there.</summary>
        public Tower TryBuild(TowerData data, Vector3 position)
        {
            if (data == null || data.Prefab == null)
            {
                return null;
            }

            int cost = CostOf(data);
            Wallet wallet = Wallet;
            if (wallet != null && !wallet.TrySpend(data.CostResource, cost))
            {
                return null;
            }

            return Place(data, position, cost);
        }

        /// <summary>Places a tower from its prefab without charging for it, recording <paramref name="cost"/> as invested.</summary>
        public Tower Place(TowerData data, Vector3 position, int cost)
        {
            Tower tower = UnityEngine.Object.Instantiate(data.Prefab, position, Quaternion.identity);
            tower.name = data.DisplayName;
            tower.TotalFoodInvested = cost;
            Placed?.Invoke(tower);
            return tower;
        }

        /// <summary>Pays for and applies the next level. False if it cannot be upgraded or the cost is not there.</summary>
        public bool TryUpgrade(Tower tower)
        {
            if (tower == null || !tower.CanUpgrade)
            {
                return false;
            }

            int cost = UpgradeCost(tower);
            Wallet wallet = Wallet;
            if (wallet != null && !wallet.TrySpend(null, cost))
            {
                return false;
            }

            tower.Upgrade();
            tower.TotalFoodInvested += cost;
            Upgraded?.Invoke(tower);
            return true;
        }

        /// <summary>Refunds a tower's <see cref="Tower.RefundValue"/> and destroys it. Returns the refund.</summary>
        public int Sell(Tower tower)
        {
            if (tower == null)
            {
                return 0;
            }

            int refund = tower.RefundValue;
            Sold?.Invoke(tower);
            Wallet?.Earn(null, refund);

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(tower.gameObject);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(tower.gameObject);
            }

            return refund;
        }

        private static int Discounted(int baseCost)
        {
            return Mathf.RoundToInt(baseCost * UpgradeModifiers.Current.CostMultiplier);
        }
    }
}
