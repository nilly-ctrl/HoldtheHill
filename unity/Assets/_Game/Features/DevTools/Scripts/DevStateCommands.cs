#if UNITY_EDITOR || DEVELOPMENT_BUILD
using HoldTheHill.Core.Save;
using HoldTheHill.Features.Progression;
using HoldTheHill.Features.GameFlow;
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
    /// The dev console's run-state commands: god, speed, win, lose, unlock. Registered by <see cref="DevBuiltinCommands"/>.
    /// </summary>
    internal static class DevStateCommands
    {
        internal static string God(DevCommandArgs a)
        {
            bool on = a.Has(0) ? ParseOnOff(a[0]) : !DevToolsCheats.God;
            DevToolsCheats.SetGod(on);
            return "God mode " + (on ? "on" : "off") + ".";
        }

        internal static string Speed(DevCommandArgs a)
        {
            if (a.Has(0) && (string.Equals(a[0], "reset", StringComparison.OrdinalIgnoreCase) || string.Equals(a[0], "off", StringComparison.OrdinalIgnoreCase)))
            {
                DevToolsCheats.SetSpeed(0f);
                return "Speed is back to the game's own.";
            }

            float x = a.Float(0, "x");
            if (x < 0.1f || x > 20f) throw new DevCommandException("<x> must be between 0.1 and 20.");
            if (UnityEngine.Object.FindAnyObjectByType<GrayboxGameFlow>() == null) throw new DevCommandException("No flow controller in the scene; time scale is its job.");

            DevToolsCheats.SetSpeed(x);
            return "Speed x" + x.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)
                + ". The pause menu and wave end keep their own scale.";
        }

        internal static string Win(DevCommandArgs a)
        {
            Queen hill = UnityEngine.Object.FindAnyObjectByType<Queen>();
            if (hill == null) throw new DevCommandException("No hill in the scene.");
            if (hill.IsFinished) throw new DevCommandException("The run is already over.");

            hill.TriggerVictory();
            return "Victory.";
        }

        internal static string Lose(DevCommandArgs a)
        {
            Queen hill = UnityEngine.Object.FindAnyObjectByType<Queen>();
            if (hill == null) throw new DevCommandException("No hill in the scene.");
            if (hill.IsFinished) throw new DevCommandException("The run is already over.");

            hill.TriggerDefeat();
            return "Defeat.";
        }

        internal static string Unlock(DevCommandArgs a)
        {
            string what = a.Has(0) ? a[0].ToLowerInvariant() : "all";
            if (what != "all" && what != "skills" && what != "achievements")
            {
                throw new DevCommandException("Unknown '" + what + "'.");
            }

            var result = new StringBuilder();
            if (what == "all" || what == "skills") result.Append(UnlockSkills());
            if (what == "all" || what == "achievements")
            {
                if (result.Length > 0) result.Append('\n');
                result.Append(UnlockAchievements());
            }

            return result.ToString();
        }

        internal static string UnlockSkills()
        {
            GrayboxSkillTree tree = GrayboxSkillTree.Instance;
            if (tree == null) return "No skill tree in the scene.";

            // Pay for everything that is locked, then unlock in passes so prerequisites go first.
            int cost = 0;
            foreach (SkillNode node in tree.GetAllNodes())
            {
                if (!node.isUnlocked) cost += node.cost;
            }

            if (cost > 0) tree.AddSkillPoints(cost);

            int unlocked = 0;
            bool progress = true;
            while (progress)
            {
                progress = false;
                foreach (SkillNode node in tree.GetAllNodes())
                {
                    if (tree.CanUnlock(node.id) && tree.TryUnlock(node.id))
                    {
                        unlocked++;
                        progress = true;
                    }
                }
            }

            return "Skills: unlocked " + unlocked + " (granted " + cost + " points for them).";
        }

        internal static string UnlockAchievements()
        {
            GrayboxAchievements achievements = GrayboxAchievements.Instance;
            if (achievements == null) return "No achievements in the scene.";

            var all = new List<Achievement>(achievements.Achievements);
            int unlocked = 0;
            foreach (Achievement ach in all)
            {
                if (ach.IsUnlocked) continue;

                achievements.AddProgress(ach.Id, Mathf.Max(1, ach.RequiredProgress - ach.CurrentProgress));
                unlocked++;
            }

            return "Achievements: unlocked " + unlocked + (GrayboxSave.IsOpen ? " (saved to the save file)." : ".");
        }

        internal static bool ParseOnOff(string word)
        {
            switch (word.ToLowerInvariant())
            {
                case "on":
                case "1":
                case "true":
                    return true;
                case "off":
                case "0":
                case "false":
                    return false;
                default:
                    throw new DevCommandException("Expected on or off, got '" + word + "'.");
            }
        }

        internal static string Toggle(DevCommandArgs a, Action<bool> set, Func<bool> get, string what)
        {
            bool on = a.Has(0) ? ParseOnOff(a[0]) : !get();
            set(on);
            return what + (on ? " on." : " off.");
        }
    }
}
#endif
