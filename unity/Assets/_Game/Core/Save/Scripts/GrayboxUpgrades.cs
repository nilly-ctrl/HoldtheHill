using System.Collections.Generic;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// The permanent upgrades sold at Home and the currency that buys them. What is owned lives in
    /// the save file through <see cref="GrayboxMetaProgress"/>; this is the catalogue and the sums.
    /// </summary>
    /// <remarks>
    /// Decided with Nilly on 2026-10-07: the currency is Honeydew, a run pays 1 per wave cleared
    /// plus 5 for a win, and the first three upgrades are the ones below. Every number here is a
    /// first guess to tune.
    /// </remarks>
    public static class GrayboxUpgrades
    {
        public const string CurrencyName = "Honeydew";
        public const int HoneydewPerWave = 1;
        public const int HoneydewForVictory = 5;

        public const string StartingFood = "starting-food";
        public const string TougherHill = "tougher-hill";
        public const string BiggerBounties = "bigger-bounties";

        /// <summary>One upgrade with a level per entry in <see cref="Costs"/>.</summary>
        public sealed class Upgrade
        {
            public string Id;
            public string Name;

            /// <summary>What a level gives, with {0} for the value: "+{0} food at the start".</summary>
            public string Effect;
            public int[] Costs;
            public float[] Values;

            public int MaxLevel => Costs.Length;
        }

        private static readonly List<Upgrade> s_all = new List<Upgrade>
        {
            new Upgrade { Id = StartingFood, Name = "More starting food", Effect = "+{0} food at the start", Costs = new[] { 3, 6, 10 }, Values = new[] { 50f, 100f, 150f } },
            new Upgrade { Id = TougherHill, Name = "Tougher hill", Effect = "+{0} hill health", Costs = new[] { 3, 6, 10 }, Values = new[] { 10f, 20f, 30f } },
            new Upgrade { Id = BiggerBounties, Name = "Bigger bounties", Effect = "+{0}% food from kills", Costs = new[] { 5, 10 }, Values = new[] { 5f, 10f } },
        };

        public static IReadOnlyList<Upgrade> All => s_all;

        public static Upgrade Find(string id) => s_all.Find(u => u.Id == id);

        /// <summary>Levels owned, 0 to <see cref="Upgrade.MaxLevel"/>.</summary>
        public static int Level(Upgrade upgrade)
        {
            int level = 0;
            while (level < upgrade.MaxLevel && GrayboxMetaProgress.HasUpgrade(LevelId(upgrade, level + 1))) level++;
            return level;
        }

        /// <summary>Price of the next level, or -1 when every level is owned.</summary>
        public static int NextCost(Upgrade upgrade)
        {
            int level = Level(upgrade);
            return level < upgrade.MaxLevel ? upgrade.Costs[level] : -1;
        }

        /// <summary>What the upgrade gives now: 0 at level 0.</summary>
        public static float Value(Upgrade upgrade) => ValueAt(upgrade, Level(upgrade));

        public static float ValueAt(Upgrade upgrade, int level) => level <= 0 ? 0f : upgrade.Values[Mathf.Min(level, upgrade.MaxLevel) - 1];

        public static string Describe(Upgrade upgrade, int level) => string.Format(upgrade.Effect, ValueAt(upgrade, level).ToString("0"));

        public static bool TryBuyNext(Upgrade upgrade)
        {
            int cost = NextCost(upgrade);
            return cost >= 0 && GrayboxMetaProgress.TryBuyUpgrade(LevelId(upgrade, Level(upgrade) + 1), cost);
        }

        /// <summary>Honeydew a run has earned so far. A win counts its last wave as cleared.</summary>
        public static int HoneydewFor(int waveReached, bool victory)
        {
            int cleared = victory ? waveReached : Mathf.Max(0, waveReached - 1);
            return cleared * HoneydewPerWave + (victory ? HoneydewForVictory : 0);
        }

        // ---------- what the game reads ----------

        public static int StartingFoodBonus => Mathf.RoundToInt(Value(Find(StartingFood)));
        public static float HillHealthBonus => Value(Find(TougherHill));
        public static float BountyMultiplier => 1f + Value(Find(BiggerBounties)) / 100f;

        private static string LevelId(Upgrade upgrade, int level) => upgrade.Id + "-" + level;
    }
}
