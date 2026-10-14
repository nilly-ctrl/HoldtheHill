#if UNITY_EDITOR || DEVELOPMENT_BUILD
using HoldTheHill.Core.Save;
using HoldTheHill.Features.Progression;
using HoldTheHill.Features.Economy;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using HoldTheHill.Features.Combat;
using HoldTheHill.Features.Enemies;
using UnityEngine;

namespace HoldTheHill.Features.DevTools
{
    /// <summary>
    /// The dev console's resource commands: setfood and give. Registered by <see cref="DevBuiltinCommands"/>.
    /// </summary>
    internal static class DevResourceCommands
    {
        internal static string SetFood(DevCommandArgs a)
        {
            int amount = a.Int(0, "n");
            if (amount < 0) throw new DevCommandException("<n> cannot be negative.");

            FoodWallet economy = RequireEconomy();
            economy.ResetFood(amount);
            return "Gold is " + economy.CurrentFood + ".";
        }

        internal static FoodWallet RequireEconomy()
        {
            FoodWallet economy = UnityEngine.Object.FindAnyObjectByType<FoodWallet>();
            if (economy == null) throw new DevCommandException("No economy in the scene.");
            return economy;
        }

        internal static string Give(DevCommandArgs a)
        {
            string what = a.String(0, "resource");
            int amount = a.Int(1, "n");
            if (amount <= 0) throw new DevCommandException("<n> must be above zero.");

            string key = what.ToLowerInvariant();
            FoodWallet economy = UnityEngine.Object.FindAnyObjectByType<FoodWallet>();

            if (key == "gold" || key == "main" || (economy != null && economy.MainResource != null && MatchesResource(economy.MainResource, key)))
            {
                RequireEconomy().EarnFood(amount);
                return "Gold is " + economy.CurrentFood + ".";
            }

            if (key == "honeydew" || key == "hd")
            {
                if (!SaveStore.IsOpen) throw new DevCommandException("No save file is open, so there is no honeydew balance.");
                MetaProgress.AddCurrency(amount);
                return "Honeydew is " + MetaProgress.Currency + ".";
            }

            if (key == "skillpoints" || key == "sp" || key == "skill")
            {
                SkillTree tree = SkillTree.Instance;
                if (tree == null) throw new DevCommandException("No skill tree in the scene.");
                tree.AddSkillPoints(amount);
                return "Skill points: " + tree.SkillPoints + ".";
            }

            foreach (ResourceData data in Resources.FindObjectsOfTypeAll<ResourceData>())
            {
                if (data != null && MatchesResource(data, key))
                {
                    RequireEconomy().Earn(data, amount);
                    return data.DisplayName + " is " + economy.Get(data) + ".";
                }
            }

            throw new DevCommandException("No resource '" + what + "'. Try: list resources");
        }

        internal static bool MatchesResource(ResourceData data, string lowerKey)
        {
            return string.Equals(data.Id, lowerKey, StringComparison.OrdinalIgnoreCase)
                || string.Equals(data.DisplayName, lowerKey, StringComparison.OrdinalIgnoreCase);
        }
    }
}
#endif
