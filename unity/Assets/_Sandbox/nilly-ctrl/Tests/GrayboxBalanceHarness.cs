// PlayMode tests run inside the editor, so UnityEditor is available - but this
// assembly also compiles for players, where it is not. The guard keeps both happy.
#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HoldTheHill.Features.Enemies;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace HoldTheHill.Sandbox.Graybox.Tests
{
    /// <summary>
    /// Plays the gray-box scene headlessly and reports what actually happened:
    /// how many enemies each wave spawned, how many died, and how many walked the
    /// whole path. Tuning was arithmetic up to this point; this measures it.
    /// </summary>
    /// <remarks>
    /// Editor-only assembly on purpose. Loading a scene by asset path needs
    /// <c>EditorSceneManager</c>, and the normal PlayMode test assembly also builds
    /// for players where UnityEditor does not exist.
    ///
    /// The spawner is driven by reflection rather than a direct call. An asmdef
    /// cannot reference Assembly-CSharp, which is where <c>EnemySpawner</c> lives,
    /// so there is no way to type against it from here. Enemy counting goes through
    /// <see cref="EnemyHealth"/> instead, which is in HoldTheHill.Runtime and
    /// therefore reachable.
    /// </remarks>
    public class GrayboxBalanceHarness
    {
        private const string ScenePath =
            "Assets/_Sandbox/nilly-ctrl/Graybox/GrayboxCombatTest.unity";

        // Wall-clock ceiling per wave so a stall fails loudly instead of hanging.
        private const float WaveTimeoutSeconds = 150f;

        // Everything that has ever existed, so a vanished enemy can be classified.
        // Keyed on the GameObject rather than an id: Unity 6 made GetInstanceID an
        // error-level obsolete, and the managed wrapper stays valid as a dictionary
        // key after the object is destroyed, which is precisely what this needs.
        private readonly Dictionary<GameObject, string> _seen = new Dictionary<GameObject, string>();
        private readonly HashSet<GameObject> _killed = new HashSet<GameObject>();
        private readonly HashSet<GameObject> _accountedFor = new HashSet<GameObject>();

        private int _leaked;

        /// <summary>
        /// Puts an empty scene back afterwards.
        /// </summary>
        /// <remarks>
        /// Without this the gray-box scene stays loaded into whatever test runs next,
        /// and seven towers keep shooting. That is not hypothetical: it made
        /// LinearProjectile_DamagesEnemyItFliesInto report 49 damage instead of 25,
        /// because the gray-box towers were firing at its test enemy too.
        /// </remarks>
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            // An empty scene has to become active before the gray-box one can be
            // unloaded; Unity refuses to unload the only loaded scene.
            Scene cleanup = SceneManager.CreateScene(
                "GrayboxHarnessCleanup_" + Time.frameCount);
            SceneManager.SetActiveScene(cleanup);

            Scene graybox = SceneManager.GetSceneByPath(ScenePath);
            if (graybox.IsValid() && graybox.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(graybox);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator Graybox_PlayAllWaves_AndReportBalance()
        {
            EditorSceneManager.LoadSceneInPlayMode(
                ScenePath, new LoadSceneParameters(LoadSceneMode.Single));

            // The scene is not ready on the frame the load is requested.
            yield return null;
            yield return null;

            Component spawner = FindSpawner();
            Assert.NotNull(spawner, $"No EnemySpawner found in {ScenePath}.");

            // Wave data is loaded in Start, so give it a few frames rather than
            // reading TotalWaves on the frame the scene appears.
            for (int i = 0; i < 10 && GetInt(spawner, "TotalWaves") == 0; i++)
            {
                yield return null;
            }

            Assert.Greater(
                GetInt(spawner, "TotalWaves"), 0,
                "The spawner has no waves. Check that activeMapConfigOverride is wired " +
                "in the scene - it serializes as null if the asset was not saved first.");

            EnemyHealth.Defeated += OnDefeated;
            float previousScale = Time.timeScale;

            // 4x keeps a three-wave run to roughly a minute. Coroutines and
            // deltaTime movement both scale, so behaviour is unchanged.
            Time.timeScale = 4f;

            var report = new List<string>();

            try
            {
                int totalWaves = GetInt(spawner, "TotalWaves");
                report.Add($"waves in the asset: {totalWaves}");

                for (int wave = 1; wave <= totalWaves; wave++)
                {
                    int spawnedBefore = _seen.Count;
                    int killedBefore = _killed.Count;
                    int leakedBefore = _leaked;
                    float started = Time.realtimeSinceStartup;

                    // Wave 1 auto-starts; later waves wait to be prompted.
                    if (wave > 1)
                    {
                        Invoke(spawner, "StartNextWave");
                    }

                    yield return WaitForWaveToSettle(spawner, started);

                    float elapsed = Time.realtimeSinceStartup - started;
                    int spawned = _seen.Count - spawnedBefore;
                    int killed = _killed.Count - killedBefore;
                    int leaked = _leaked - leakedBefore;

                    report.Add(
                        $"wave {wave}: spawned {spawned}, killed {killed}, leaked {leaked}, " +
                        $"{elapsed:0.0}s wall");
                }
            }
            finally
            {
                EnemyHealth.Defeated -= OnDefeated;
                Time.timeScale = previousScale;
            }

            int totalSpawned = _seen.Count;
            int totalKilled = _killed.Count;
            float killRate = totalSpawned > 0 ? 100f * totalKilled / totalSpawned : 0f;

            report.Add($"TOTAL: spawned {totalSpawned}, killed {totalKilled}, leaked {_leaked}");
            report.Add($"kill rate: {killRate:0.0}%");
            report.Add(LeakBreakdown());

            Debug.Log("[Graybox balance]\n  " + string.Join("\n  ", report));

            Assert.Greater(totalSpawned, 0, "Nothing spawned - the scene is not running.");
            Assert.Greater(totalKilled, 0, "Nothing died - the towers are not shooting.");
        }

        /// <summary>
        /// Runs until the wave has finished spawning and the field has emptied, or
        /// the timeout trips.
        /// </summary>
        private IEnumerator WaitForWaveToSettle(Component spawner, float started)
        {
            // Give the spawner a frame to leave Idle before testing for "finished".
            yield return null;

            while (Time.realtimeSinceStartup - started < WaveTimeoutSeconds)
            {
                Track();

                bool stillSpawning = GetBool(spawner, "IsSpawning");
                int alive = CountAlive();

                if (!stillSpawning && alive == 0)
                {
                    // One more frame so the last death is recorded before moving on.
                    yield return null;
                    Track();
                    yield break;
                }

                yield return null;
            }

            Debug.LogWarning($"[Graybox balance] wave timed out after {WaveTimeoutSeconds}s");
        }

        // Enemies are destroyed both by dying and by reaching the end of the path.
        // Only the first raises an event, so anything that disappears unannounced
        // walked off the end.
        private void Track()
        {
            var live = new HashSet<GameObject>();

            foreach (EnemyHealth enemy in Object.FindObjectsByType<EnemyHealth>())
            {
                GameObject go = enemy.gameObject;
                live.Add(go);
                if (!_seen.ContainsKey(go))
                {
                    _seen[go] = go.name.Replace("(Clone)", string.Empty).Trim();
                }
            }

            // Collected first: _accountedFor is mutated below, and _seen must not be
            // touched while it is being enumerated.
            var vanished = new List<GameObject>();
            foreach (KeyValuePair<GameObject, string> entry in _seen)
            {
                if (!live.Contains(entry.Key) && !_accountedFor.Contains(entry.Key))
                {
                    vanished.Add(entry.Key);
                }
            }

            foreach (GameObject go in vanished)
            {
                _accountedFor.Add(go);
                if (!_killed.Contains(go))
                {
                    _leaked++;
                }
            }
        }

        private void OnDefeated(GameObject enemy)
        {
            if (enemy != null)
            {
                _killed.Add(enemy);
                _accountedFor.Add(enemy);
            }
        }

        private static int CountAlive()
        {
            int n = 0;
            foreach (EnemyHealth enemy in Object.FindObjectsByType<EnemyHealth>())
            {
                if (!enemy.IsDead)
                {
                    n++;
                }
            }

            return n;
        }

        private string LeakBreakdown()
        {
            var byType = new Dictionary<string, int>();
            foreach (KeyValuePair<GameObject, string> entry in _seen)
            {
                if (_killed.Contains(entry.Key))
                {
                    continue;
                }

                byType.TryGetValue(entry.Value, out int n);
                byType[entry.Value] = n + 1;
            }

            if (byType.Count == 0)
            {
                return "nothing leaked";
            }

            var parts = new List<string>();
            foreach (KeyValuePair<string, int> entry in byType)
            {
                parts.Add($"{entry.Key} x{entry.Value}");
            }

            return "leaked by type: " + string.Join(", ", parts);
        }

        // ---------- reflection onto EnemySpawner (Assembly-CSharp, unreferencable) ----------

        private static Component FindSpawner()
        {
            foreach (MonoBehaviour mb in Object.FindObjectsByType<MonoBehaviour>())
            {
                if (mb != null && mb.GetType().Name == "EnemySpawner")
                {
                    return mb;
                }
            }

            return null;
        }

        private static void Invoke(Component target, string method)
        {
            MethodInfo m = target.GetType().GetMethod(method, BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(m, $"EnemySpawner has no public method '{method}'.");
            m.Invoke(target, null);
        }

        private static int GetInt(Component target, string property) =>
            (int)GetProperty(target, property);

        private static bool GetBool(Component target, string property) =>
            (bool)GetProperty(target, property);

        private static object GetProperty(Component target, string property)
        {
            PropertyInfo p = target.GetType().GetProperty(property, BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(p, $"EnemySpawner has no public property '{property}'.");
            return p.GetValue(target);
        }
    }
}
#endif
