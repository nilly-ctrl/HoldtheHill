#if UNITY_EDITOR || DEVELOPMENT_BUILD
using HoldTheHill.Features.Progression;
using HoldTheHill.Features.Towers;
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
    /// The console's built-in commands. They work through the same public API the HUD uses
    /// (economy, spawner, skill tree, achievements, hill health) and fail with a message, not an
    /// exception, when the scene lacks the thing they need.
    /// </summary>
    public static class DevBuiltinCommands
    {
        public static void RegisterAll(DevCommandRegistry r)
        {
            r.Register("help", "help [command]", "Lists the commands, or shows one command's usage.", a => Help(r, a), "?");
            r.Register("list", "list [commands|enemies|towers|resources|skills|achievements]", "Lists what exists in this scene.", List);
            r.Register("spawn", "spawn <enemy> [n]", "Spawns n of an enemy (default 1) at the entrance. See list enemies.", DevEnemyCommands.Spawn);
            r.Register("nextwave", "nextwave", "Starts the next wave.", DevEnemyCommands.NextWave);
            r.Register("skipwave", "skipwave", "Clears the field and moves past the next wave without spawning it.", DevEnemyCommands.SkipWave);
            r.Register("setgold", "setgold <n>", "Sets gold (the main resource) to n.", DevResourceCommands.SetFood);
            r.Register("give", "give <resource> <n>", "Adds n of gold, honeydew, skillpoints or a resource by name.", DevResourceCommands.Give);
            r.Register("god", "god [on|off]", "The hill and the Warden cannot be hurt.", DevStateCommands.God);
            r.Register("speed", "speed <x|reset>", "Sets the game speed multiplier (0.1 to 20).", DevStateCommands.Speed);
            r.Register("kill", "kill all", "Kills every enemy on the field (they pay their bounty).", DevEnemyCommands.Kill);
            r.Register("win", "win", "Ends the run in victory.", DevStateCommands.Win);
            r.Register("lose", "lose", "Ends the run in defeat.", DevStateCommands.Lose);
            r.Register("unlock", "unlock [all|skills|achievements]", "Unlocks every skill node and/or achievement. Achievements are written to the save file.", DevStateCommands.Unlock);
            r.Register("tune", "tune [on|off]", "Shows or hides the live tuning panel (F2).", a => DevStateCommands.Toggle(a, v => DevTuningPanel.Visible = v, () => DevTuningPanel.Visible, "tuning panel"));
            r.Register("perf", "perf [on|off]", "Shows or hides the performance overlay (F4).", a => DevStateCommands.Toggle(a, v => DevPerfHud.Visible = v, () => DevPerfHud.Visible, "perf overlay"));
            r.Register("rec", "rec start|stop|status", "Records the run's commands to a replay file.", DevReplayCommands.Rec);
            r.Register("replay", "replay list|play <file|latest>|stop|status", "Plays a recorded replay into a fresh run.", DevReplayCommands.Replay);
        }

        // ---------- info ----------

        private static string Help(DevCommandRegistry registry, DevCommandArgs a)
        {
            if (a.Count > 0)
            {
                if (!registry.TryGet(a[0], out DevCommand command)) throw new DevCommandException("No command '" + a[0] + "'.");
                return command.Usage + "\n" + command.Help;
            }

            var text = new StringBuilder();
            foreach (DevCommand command in registry.Commands)
            {
                text.Append("<b>").Append(command.Usage).Append("</b>  ").Append(command.Help).Append('\n');
            }

            return text.ToString().TrimEnd();
        }

        private static string List(DevCommandArgs a)
        {
            string topic = a.Has(0) ? a[0].ToLowerInvariant() : "commands";
            var text = new StringBuilder();
            switch (topic)
            {
                case "commands":
                    foreach (DevCommand command in DevCommandRegistry.Default.Commands) text.Append(command.Name).Append("  ");
                    break;

                case "enemies":
                    foreach (string id in DevEnemyCommands.EnemyIds()) text.Append(id).Append("  ");
                    break;

                case "towers":
                    GrayboxTowerPlacer placer = UnityEngine.Object.FindAnyObjectByType<GrayboxTowerPlacer>();
                    if (placer == null || placer.Catalog == null) throw new DevCommandException("No tower catalog in the scene.");
                    for (int i = 0; i < placer.Catalog.Count; i++)
                    {
                        TowerData data = placer.Catalog[i];
                        if (data != null) text.Append(i + 1).Append(' ').Append(data.DisplayName).Append(" (").Append(data.BaseCost).Append(")\n");
                    }

                    break;

                case "resources":
                    text.Append("gold  honeydew  skillpoints");
                    GrayboxEconomy economy = UnityEngine.Object.FindAnyObjectByType<GrayboxEconomy>();
                    if (economy != null && economy.MainResource != null)
                    {
                        text.Append("  ").Append(economy.MainResource.Id).Append(" (main)");
                    }

                    foreach (ResourceData data in Resources.FindObjectsOfTypeAll<ResourceData>())
                    {
                        if (data != null && (economy == null || data != economy.MainResource)) text.Append("  ").Append(data.Id);
                    }

                    break;

                case "skills":
                    GrayboxSkillTree tree = GrayboxSkillTree.Instance;
                    if (tree == null) throw new DevCommandException("No skill tree in the scene.");
                    foreach (SkillNode node in tree.GetAllNodes())
                    {
                        text.Append(node.isUnlocked ? "[x] " : "[ ] ").Append(node.id).Append(" (").Append(node.cost).Append(")\n");
                    }

                    break;

                case "achievements":
                    GrayboxAchievements achievements = GrayboxAchievements.Instance;
                    if (achievements == null) throw new DevCommandException("No achievements in the scene.");
                    foreach (Achievement ach in achievements.Achievements)
                    {
                        text.Append(ach.IsUnlocked ? "[x] " : "[ ] ").Append(ach.Id).Append(' ')
                            .Append(ach.CurrentProgress).Append('/').Append(ach.RequiredProgress).Append('\n');
                    }

                    break;

                default:
                    throw new DevCommandException("Unknown list '" + topic + "'.");
            }

            string result = text.ToString().TrimEnd();
            return result.Length == 0 ? "(none)" : result;
        }

        // ---------- enemies and waves ----------

        // ---------- resources ----------

        // ---------- state ----------

        // ---------- replay ----------

    }
}
#endif
