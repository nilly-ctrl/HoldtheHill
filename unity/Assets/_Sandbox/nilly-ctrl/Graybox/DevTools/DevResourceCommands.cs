#if UNITY_EDITOR || DEVELOPMENT_BUILD
using HoldTheHill.Features.Economy;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using HoldTheHill.Features.Combat;
using HoldTheHill.Features.Enemies;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
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

            GrayboxEconomy economy = RequireEconomy();
            economy.ResetFood(amount);
            return "Gold is " + economy.CurrentFood + ".";
        }

        internal static GrayboxEconomy RequireEconomy()
        {
            if (GrayboxEconomy.Instance == null) throw new DevCommandException("No economy in the scene.");
            return GrayboxEconomy.Instance;
        }

        internal static string Give(DevCommandArgs a)
        {
            string what = a.String(0, "resource");
            int amount = a.Int(1, "n");
            if (amount <= 0) throw new DevCommandException("<n> must be above zero.");

            string key = what.ToLowerInvariant();
            GrayboxEconomy economy = GrayboxEconomy.Instance;

            if (key == "gold" || key == "main" || (economy != null && economy.MainResource != null && MatchesResource(economy.MainResource, key)))
            {
                RequireEconomy().EarnFood(amount);
                return "Gold is " + economy.CurrentFood + ".";
            }

            if (key == "honeydew" || key == "hd")
            {
                if (!GrayboxSave.IsOpen) throw new DevCommandException("No save file is open, so there is no honeydew balance.");
                GrayboxMetaProgress.AddCurrency(amount);
                return "Honeydew is " + GrayboxMetaProgress.Currency + ".";
            }

            if (key == "skillpoints" || key == "sp" || key == "skill")
            {
                GrayboxSkillTree tree = GrayboxSkillTree.Instance;
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
