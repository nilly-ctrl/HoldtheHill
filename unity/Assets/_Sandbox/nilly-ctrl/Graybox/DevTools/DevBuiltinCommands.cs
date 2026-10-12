#if UNITY_EDITOR || DEVELOPMENT_BUILD
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
    /// The console's built-in commands. They work through the same public API the HUD uses
    /// (economy, spawner, skill tree, achievements, hill health) and fail with a message, not an
    /// exception, when the scene lacks the thing they need.
    /// </summary>
    public static class DevBuiltinCommands
    {
        // Used when the custom spawner's list cannot be read.
        private static readonly string[] KnownEnemyIds = { "Runner", "Grunt", "Brute", "Shielded", "Swarm", "Splitter", "Healer" };

        public static void RegisterAll(DevCommandRegistry r)
        {
            r.Register("help", "help [command]", "Lists the commands, or shows one command's usage.", a => Help(r, a), "?");
            r.Register("list", "list [commands|enemies|towers|resources|skills|achievements]", "Lists what exists in this scene.", List);
            r.Register("spawn", "spawn <enemy> [n]", "Spawns n of an enemy (default 1) at the entrance. See list enemies.", Spawn);
            r.Register("nextwave", "nextwave", "Starts the next wave.", NextWave);
            r.Register("skipwave", "skipwave", "Clears the field and moves past the next wave without spawning it.", SkipWave);
            r.Register("setgold", "setgold <n>", "Sets gold (the main resource) to n.", SetGold);
            r.Register("give", "give <resource> <n>", "Adds n of gold, honeydew, skillpoints or a resource by name.", Give);
            r.Register("god", "god [on|off]", "The hill and the Warden cannot be hurt.", God);
            r.Register("speed", "speed <x|reset>", "Sets the game speed multiplier (0.1 to 20).", Speed);
            r.Register("kill", "kill all", "Kills every enemy on the field (they pay their bounty).", Kill);
            r.Register("win", "win", "Ends the run in victory.", Win);
            r.Register("lose", "lose", "Ends the run in defeat.", Lose);
            r.Register("unlock", "unlock [all|skills|achievements]", "Unlocks every skill node and/or achievement. Achievements are written to the save file.", Unlock);
            r.Register("tune", "tune [on|off]", "Shows or hides the live tuning panel (F2).", a => Toggle(a, v => DevTuningPanel.Visible = v, () => DevTuningPanel.Visible, "tuning panel"));
            r.Register("perf", "perf [on|off]", "Shows or hides the performance overlay (F4).", a => Toggle(a, v => DevPerfHud.Visible = v, () => DevPerfHud.Visible, "perf overlay"));
            r.Register("rec", "rec start|stop|status", "Records the run's commands to a replay file.", Rec);
            r.Register("replay", "replay list|play <file|latest>|stop|status", "Plays a recorded replay into a fresh run.", Replay);
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
                    foreach (string id in EnemyIds()) text.Append(id).Append("  ");
                    break;

                case "towers":
                    GrayboxTowerPlacer placer = UnityEngine.Object.FindAnyObjectByType<GrayboxTowerPlacer>();
                    if (placer == null || placer.Catalog == null) throw new DevCommandException("No tower catalog in the scene.");
                    for (int i = 0; i < placer.Catalog.Count; i++)
                    {
                        GrayboxTowerData data = placer.Catalog[i];
                        if (data != null) text.Append(i + 1).Append(' ').Append(data.DisplayName).Append(" (").Append(data.BaseCost).Append(")\n");
                    }

                    break;

                case "resources":
                    text.Append("gold  honeydew  skillpoints");
                    GrayboxEconomy economy = GrayboxEconomy.Instance;
                    if (economy != null && economy.MainResource != null)
                    {
                        text.Append("  ").Append(economy.MainResource.Id).Append(" (main)");
                    }

                    foreach (GrayboxResourceData data in Resources.FindObjectsOfTypeAll<GrayboxResourceData>())
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

        private static List<string> EnemyIds()
        {
            var ids = new List<string>();
            GrayboxSpecials specials = GrayboxSpecials.Instance;
            if (specials != null)
            {
                foreach (GrayboxSpecials.Entry entry in specials.Entries)
                {
                    if (entry != null && entry.Prefab != null && !ids.Contains(entry.Id)) ids.Add(entry.Id);
                }
            }

            GrayboxCustomSpawner custom = UnityEngine.Object.FindAnyObjectByType<GrayboxCustomSpawner>();
            var entries = DevReflect.Get<List<GrayboxCustomSpawner.Entry>>(custom, "_enemies");
            if (entries != null)
            {
                foreach (GrayboxCustomSpawner.Entry entry in entries)
                {
                    if (entry != null && entry.Prefab != null && !ids.Contains(entry.Id)) ids.Add(entry.Id);
                }
            }
            else if (custom != null)
            {
                foreach (string id in KnownEnemyIds)
                {
                    if (custom.Find(id) != null && !ids.Contains(id)) ids.Add(id);
                }
            }

            return ids;
        }

        private static string Spawn(DevCommandArgs a)
        {
            string wanted = a.String(0, "enemy");
            int count = Mathf.Clamp(a.OptionalInt(1, 1, "n"), 1, 200);

            string id = null;
            foreach (string known in EnemyIds())
            {
                if (string.Equals(known, wanted, StringComparison.OrdinalIgnoreCase))
                {
                    id = known;
                    break;
                }
            }

            if (id == null) throw new DevCommandException("No enemy '" + wanted + "'. Try: list enemies");

            GrayboxSpecials specials = GrayboxSpecials.Instance;
            GrayboxCustomSpawner custom = UnityEngine.Object.FindAnyObjectByType<GrayboxCustomSpawner>();
            Func<GameObject> spawnOne;
            if (specials != null && specials.Find(id) != null)
            {
                spawnOne = () => specials.Spawn(id);
            }
            else if (custom != null && custom.Find(id) != null)
            {
                GameObject prefab = custom.Find(id);
                spawnOne = () => SpawnOnPath(prefab);
            }
            else
            {
                throw new DevCommandException("'" + id + "' has no prefab in this scene.");
            }

            if (count == 1 || DevToolsCheats.Instance == null)
            {
                for (int i = 0; i < count; i++) spawnOne();
            }
            else
            {
                DevToolsCheats.Instance.StartCoroutine(SpawnStaggered(spawnOne, count));
            }

            return "Spawning " + count + " " + id + ".";
        }

        private static IEnumerator SpawnStaggered(Func<GameObject> spawnOne, int count)
        {
            var wait = new WaitForSeconds(0.2f);
            for (int i = 0; i < count; i++)
            {
                spawnOne();
                if (i < count - 1) yield return wait;
            }
        }

        // Through the spawner, so the cheat's enemies count as living; without one, the same steps by hand.
        private static GameObject SpawnOnPath(GameObject prefab)
        {
            var spawner = UnityEngine.Object.FindAnyObjectByType<EnemySpawner>();
            if (spawner != null) return spawner.Spawn(prefab);

            var path = UnityEngine.Object.FindAnyObjectByType<EnemyPath>();
            Vector3 position = path != null && path.Waypoints.Count > 0 ? path.Waypoints[0] : Vector3.zero;
            GameObject enemy = UnityEngine.Object.Instantiate(prefab, position, Quaternion.identity);
            EnemyMover mover = enemy.GetComponent<EnemyMover>();
            if (mover != null) mover.SnapToStart();
            return enemy;
        }

        private static string NextWave(DevCommandArgs a)
        {
            EnemySpawner spawner = RequireSpawner();
            if (spawner.CurrentState == EnemySpawner.SpawnerState.Spawning) throw new DevCommandException("A wave is spawning.");
            if (spawner.CurrentState == EnemySpawner.SpawnerState.Completed) throw new DevCommandException("All waves are done.");

            spawner.StartNextWave();
            return "Started wave " + (spawner.CurrentWaveNumber) + " of " + spawner.TotalWaves + ".";
        }

        private static string SkipWave(DevCommandArgs a)
        {
            EnemySpawner spawner = RequireSpawner();
            int next = spawner.CurrentWaveIndex + 1;
            if (next >= spawner.TotalWaves)
            {
                throw new DevCommandException("That is the last wave. Use win to end the run, or kill all to clear the field.");
            }

            int killed = KillAllEnemies();
            spawner.RestartFromWave(next);
            return "Skipped wave " + spawner.CurrentWaveIndex + "; " + killed + " enemies cleared. The next wave is " + spawner.CurrentWaveNumber + ".";
        }

        private static EnemySpawner RequireSpawner()
        {
            var spawner = UnityEngine.Object.FindAnyObjectByType<EnemySpawner>();
            if (spawner == null) throw new DevCommandException("No wave spawner in the scene.");
            return spawner;
        }

        private static string Kill(DevCommandArgs a)
        {
            if (!a.Has(0) || !string.Equals(a[0], "all", StringComparison.OrdinalIgnoreCase))
            {
                throw new DevCommandException("Only 'kill all' exists.");
            }

            return "Killed " + KillAllEnemies() + " enemies.";
        }

        // True damage in one hit. A boss with a damage cap or an immunity phase can survive it.
        private static int KillAllEnemies()
        {
            int killed = 0;
            for (int pass = 0; pass < 3; pass++)
            {
                int thisPass = 0;
                foreach (EnemyHealth enemy in UnityEngine.Object.FindObjectsByType<EnemyHealth>())
                {
                    if (enemy == null || enemy.IsDead) continue;

                    enemy.TakeDamage(new DamageInfo(1e9f, null, enemy.transform.position, DamageType.True));
                    thisPass++;
                }

                killed += thisPass;
                if (thisPass == 0) break;
            }

            return killed;
        }

        // ---------- resources ----------

        private static string SetGold(DevCommandArgs a)
        {
            int amount = a.Int(0, "n");
            if (amount < 0) throw new DevCommandException("<n> cannot be negative.");

            GrayboxEconomy economy = RequireEconomy();
            economy.ResetGold(amount);
            return "Gold is " + economy.CurrentGold + ".";
        }

        private static GrayboxEconomy RequireEconomy()
        {
            if (GrayboxEconomy.Instance == null) throw new DevCommandException("No economy in the scene.");
            return GrayboxEconomy.Instance;
        }

        private static string Give(DevCommandArgs a)
        {
            string what = a.String(0, "resource");
            int amount = a.Int(1, "n");
            if (amount <= 0) throw new DevCommandException("<n> must be above zero.");

            string key = what.ToLowerInvariant();
            GrayboxEconomy economy = GrayboxEconomy.Instance;

            if (key == "gold" || key == "main" || (economy != null && economy.MainResource != null && MatchesResource(economy.MainResource, key)))
            {
                RequireEconomy().EarnGold(amount);
                return "Gold is " + economy.CurrentGold + ".";
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

            foreach (GrayboxResourceData data in Resources.FindObjectsOfTypeAll<GrayboxResourceData>())
            {
                if (data != null && MatchesResource(data, key))
                {
                    RequireEconomy().Earn(data, amount);
                    return data.DisplayName + " is " + economy.Get(data) + ".";
                }
            }

            throw new DevCommandException("No resource '" + what + "'. Try: list resources");
        }

        private static bool MatchesResource(GrayboxResourceData data, string lowerKey)
        {
            return string.Equals(data.Id, lowerKey, StringComparison.OrdinalIgnoreCase)
                || string.Equals(data.DisplayName, lowerKey, StringComparison.OrdinalIgnoreCase);
        }

        // ---------- state ----------

        private static string God(DevCommandArgs a)
        {
            bool on = a.Has(0) ? ParseOnOff(a[0]) : !DevToolsCheats.God;
            DevToolsCheats.SetGod(on);
            return "God mode " + (on ? "on" : "off") + ".";
        }

        private static string Speed(DevCommandArgs a)
        {
            if (a.Has(0) && (string.Equals(a[0], "reset", StringComparison.OrdinalIgnoreCase) || string.Equals(a[0], "off", StringComparison.OrdinalIgnoreCase)))
            {
                DevToolsCheats.SetSpeed(0f);
                return "Speed is back to the game's own.";
            }

            float x = a.Float(0, "x");
            if (x < 0.1f || x > 20f) throw new DevCommandException("<x> must be between 0.1 and 20.");
            if (GrayboxGameFlow.Instance == null) throw new DevCommandException("No flow controller in the scene; time scale is its job.");

            DevToolsCheats.SetSpeed(x);
            return "Speed x" + x.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)
                + ". The pause menu and wave end keep their own scale.";
        }

        private static string Win(DevCommandArgs a)
        {
            GrayboxBaseHealth hill = GrayboxBaseHealth.Instance;
            if (hill == null) throw new DevCommandException("No hill in the scene.");
            if (hill.IsFinished) throw new DevCommandException("The run is already over.");

            hill.TriggerVictory();
            return "Victory.";
        }

        private static string Lose(DevCommandArgs a)
        {
            GrayboxBaseHealth hill = GrayboxBaseHealth.Instance;
            if (hill == null) throw new DevCommandException("No hill in the scene.");
            if (hill.IsFinished) throw new DevCommandException("The run is already over.");

            hill.TriggerDefeat();
            return "Defeat.";
        }

        private static string Unlock(DevCommandArgs a)
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

        private static string UnlockSkills()
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

        private static string UnlockAchievements()
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

        private static bool ParseOnOff(string word)
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

        private static string Toggle(DevCommandArgs a, Action<bool> set, Func<bool> get, string what)
        {
            bool on = a.Has(0) ? ParseOnOff(a[0]) : !get();
            set(on);
            return what + (on ? " on." : " off.");
        }

        // ---------- replay ----------

        private static string Rec(DevCommandArgs a)
        {
            DevReplayRecorder recorder = DevReplayRecorder.Instance;
            if (recorder == null) throw new DevCommandException("No replay recorder in the scene (add DevTools).");

            string verb = a.Has(0) ? a[0].ToLowerInvariant() : "status";
            switch (verb)
            {
                case "start":
                    if (!recorder.StartRecording(out string error)) throw new DevCommandException(error);
                    return "Recording. Seed " + recorder.Current.seed + ".";

                case "stop":
                    if (!recorder.IsRecording) throw new DevCommandException("Not recording.");
                    string path = recorder.StopRecording("Stopped");
                    return path != null ? "Saved " + path : "Stopped, but the file could not be written (see the console log).";

                case "status":
                    if (!recorder.IsRecording) return "Not recording." + (recorder.LastSavedPath != null ? " Last file: " + recorder.LastSavedPath : string.Empty);
                    return "Recording: " + recorder.Tick + " fixed steps, " + recorder.Current.commands.Count + " commands.";

                default:
                    throw new DevCommandException("Unknown '" + verb + "'.");
            }
        }

        private static string Replay(DevCommandArgs a)
        {
            DevReplayPlayer player = DevReplayPlayer.Instance;
            if (player == null) throw new DevCommandException("No replay player in the scene (add DevTools).");

            string verb = a.Has(0) ? a[0].ToLowerInvariant() : "status";
            switch (verb)
            {
                case "list":
                    List<string> names = DevReplayFiles.List();
                    return names.Count == 0 ? "No replays in " + DevReplayFiles.Directory : string.Join("\n", names);

                case "play":
                    string path = DevReplayFiles.Resolve(a.Has(1) ? a.Rest(1) : "latest");
                    if (path == null) throw new DevCommandException("No such replay. Try: replay list");

                    ReplayRecording recording = DevReplayFiles.Load(path, out string loadError);
                    if (recording == null) throw new DevCommandException(loadError);
                    if (!player.Play(recording, out string message)) throw new DevCommandException(message);
                    return message;

                case "stop":
                    player.Stop();
                    return "Stopped.";

                case "status":
                    return player.Status();

                default:
                    throw new DevCommandException("Unknown '" + verb + "'.");
            }
        }
    }
}
#endif
