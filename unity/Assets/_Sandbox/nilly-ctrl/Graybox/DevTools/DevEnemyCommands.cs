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
    /// The dev console's enemy and wave commands: spawn, nextwave, skipwave, kill. Registered by <see cref="DevBuiltinCommands"/>.
    /// </summary>
    internal static class DevEnemyCommands
    {
        // Used when the custom spawner's list cannot be read.
        private static readonly string[] KnownEnemyIds = { "Runner", "Grunt", "Brute", "Shielded", "Swarm", "Splitter", "Healer" };

        internal static List<string> EnemyIds()
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

        internal static string Spawn(DevCommandArgs a)
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

        internal static IEnumerator SpawnStaggered(Func<GameObject> spawnOne, int count)
        {
            var wait = new WaitForSeconds(0.2f);
            for (int i = 0; i < count; i++)
            {
                spawnOne();
                if (i < count - 1) yield return wait;
            }
        }

        // Through the spawner, so the cheat's enemies count as living; without one, the same steps by hand.
        internal static GameObject SpawnOnPath(GameObject prefab)
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

        internal static string NextWave(DevCommandArgs a)
        {
            EnemySpawner spawner = RequireSpawner();
            if (spawner.CurrentState == EnemySpawner.SpawnerState.Spawning) throw new DevCommandException("A wave is spawning.");
            if (spawner.CurrentState == EnemySpawner.SpawnerState.Completed) throw new DevCommandException("All waves are done.");

            spawner.StartNextWave();
            return "Started wave " + (spawner.CurrentWaveNumber) + " of " + spawner.TotalWaves + ".";
        }

        internal static string SkipWave(DevCommandArgs a)
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

        internal static EnemySpawner RequireSpawner()
        {
            var spawner = UnityEngine.Object.FindAnyObjectByType<EnemySpawner>();
            if (spawner == null) throw new DevCommandException("No wave spawner in the scene.");
            return spawner;
        }

        internal static string Kill(DevCommandArgs a)
        {
            if (!a.Has(0) || !string.Equals(a[0], "all", StringComparison.OrdinalIgnoreCase))
            {
                throw new DevCommandException("Only 'kill all' exists.");
            }

            return "Killed " + KillAllEnemies() + " enemies.";
        }

        // True damage in one hit. A boss with a damage cap or an immunity phase can survive it.
        internal static int KillAllEnemies()
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
    }
}
#endif
