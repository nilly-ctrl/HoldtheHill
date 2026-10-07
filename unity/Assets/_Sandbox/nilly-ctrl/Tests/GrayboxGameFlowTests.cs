using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HoldTheHill.Features.Towers;
using HoldTheHill.Sandbox.NillyCtrl;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;

namespace HoldTheHill.Sandbox.Graybox.Tests
{
    /// <summary>
    /// Checks the flow controller: pausing owns time, defeat and victory end the run, and
    /// "retry current wave" / "restart" rewind towers, gold, hill health, skills and the wave.
    /// </summary>
    /// <remarks>
    /// Builds its own spawner, hill and towers in code, so it does not depend on the graybox scene.
    /// </remarks>
    public class GrayboxGameFlowTests
    {
        private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;

        private readonly List<Object> _made = new List<Object>();
        private readonly List<GameObject> _spawnedEnemies = new List<GameObject>();
        private EnemySpawner _spawner;
        private GrayboxEconomy _economy;
        private GrayboxBaseHealth _hill;
        private GrayboxSkillTree _skills;
        private GrayboxGameFlow _flow;

        [SetUp]
        public void SetUp()
        {
            // An inactive template stands in for an enemy prefab; the spawner only needs something to copy.
            var template = new GameObject("TestEnemyTemplate");
            template.SetActive(false);
            _made.Add(template);

            var waves = ScriptableObject.CreateInstance<MapWaveDataSO>();
            _made.Add(waves);
            for (int i = 1; i <= 3; i++)
            {
                var wave = new Wave { waveName = $"Wave {i}" };
                wave.enemies.Add(new EnemySpawnEntry { enemyPrefab = template, delayBeforeNext = 0f });
                waves.waves.Add(wave);
            }

            var systems = new GameObject("TestSystems");
            _made.Add(systems);
            _economy = systems.AddComponent<GrayboxEconomy>();
            _hill = systems.AddComponent<GrayboxBaseHealth>();
            _skills = systems.AddComponent<GrayboxSkillTree>();

            var spawnerObject = new GameObject("TestSpawner");
            spawnerObject.SetActive(false);
            _made.Add(spawnerObject);
            _spawner = spawnerObject.AddComponent<EnemySpawner>();
            typeof(EnemySpawner).GetField("activeMapConfigOverride", PrivateInstance).SetValue(_spawner, waves);
            // Unity only creates these for a spawner loaded from a scene, not one added in code.
            _spawner.onEnemySpawned = new UnityEvent<GameObject>();
            _spawner.onWaveStarted = new UnityEvent<int, int>();
            _spawner.onWaveCompleted = new UnityEvent<int>();
            _spawner.onAllWavesCompleted = new UnityEvent();
            _spawner.onMapChanged = new UnityEvent<string>();
            spawnerObject.SetActive(true);
            _spawner.onEnemySpawned.AddListener(_spawnedEnemies.Add);

            // After the spawner, which the flow controller looks up in Awake.
            var flowObject = new GameObject("TestFlow");
            _made.Add(flowObject);
            _flow = flowObject.AddComponent<GrayboxGameFlow>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Tower tower in Object.FindObjectsByType<Tower>())
            {
                Object.DestroyImmediate(tower.gameObject);
            }

            foreach (Object o in _made.Concat(_spawnedEnemies))
            {
                if (o != null) Object.DestroyImmediate(o);
            }

            _made.Clear();
            _spawnedEnemies.Clear();
            Time.timeScale = 1f;
            GrayboxSave.Close();
        }

        // ---------- pausing and speed ----------

        [UnityTest]
        public IEnumerator Pause_StopsTime_AndResumeBringsBackTheChosenSpeed()
        {
            yield return null;
            Assert.AreEqual(GameFlowState.Playing, _flow.State);

            _flow.SetSpeedIndex(1);
            Assert.AreEqual(2f, Time.timeScale);

            _flow.Pause();
            Assert.AreEqual(GameFlowState.Paused, _flow.State);
            Assert.AreEqual(0f, Time.timeScale);
            Assert.IsFalse(GrayboxGameFlow.GameplayActive);

            _flow.CycleSpeed(); // changing speed on a menu must not restart time
            Assert.AreEqual(0f, Time.timeScale);

            _flow.Resume();
            Assert.AreEqual(GameFlowState.Playing, _flow.State);
            Assert.AreEqual(4f, Time.timeScale);
            Assert.IsTrue(GrayboxGameFlow.GameplayActive);
        }

        [UnityTest]
        public IEnumerator DestroyingTheFlow_RestoresNormalTime()
        {
            yield return null;
            _flow.Pause();
            Object.DestroyImmediate(_flow.gameObject);

            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsTrue(GrayboxGameFlow.GameplayActive, "scenes without a flow controller play as before");
        }

        // ---------- run end ----------

        [UnityTest]
        public IEnumerator Defeat_EndsTheRun_AndFreezesTime()
        {
            yield return null;
            _spawner.StartNextWave();
            yield return null;

            _hill.TakeBaseDamage(_hill.MaxHealth, Vector3.zero);

            Assert.AreEqual(GameFlowState.RunEnd, _flow.State);
            Assert.IsFalse(_flow.LastRunWasVictory);
            Assert.AreEqual(0f, Time.timeScale);
            Assert.AreEqual(1, _flow.Stats.WaveReached);
            Assert.AreEqual(3, _flow.Stats.TotalWaves);
        }

        [UnityTest]
        public IEnumerator Victory_IsDeclared_WhenTheLastWaveIsSpawnedAndCleared()
        {
            yield return null;
            for (int i = 0; i < 3; i++)
            {
                _spawner.StartNextWave();
                yield return null;
            }

            Assert.AreEqual(EnemySpawner.SpawnerState.Completed, _spawner.CurrentState);
            yield return null;
            Assert.AreEqual(GameFlowState.Playing, _flow.State, "enemies are still alive");

            foreach (GameObject enemy in _spawnedEnemies)
            {
                _spawner.NotifyEnemyDefeated(enemy);
            }

            yield return null;
            Assert.AreEqual(GameFlowState.RunEnd, _flow.State);
            Assert.IsTrue(_flow.LastRunWasVictory);
        }

        [UnityTest]
        public IEnumerator RunEnd_WritesRecordsToTheSaveFile_AndARetriedRunCountsOnce()
        {
            string dir = System.IO.Path.Combine(Application.temporaryCachePath, "GrayboxFlowSave_" + System.Guid.NewGuid().ToString("N"));
            string path = System.IO.Path.Combine(dir, "save.json");
            try
            {
                GrayboxSave.Open(path);
                yield return null;
                _spawner.StartNextWave();
                yield return null;
                _spawner.StartNextWave(); // wave 2
                yield return null;

                _hill.TakeBaseDamage(_hill.MaxHealth, Vector3.zero);

                Assert.IsTrue(_flow.LastRunResult.NewBestWave);
                Assert.AreEqual(2, _flow.LastRunResult.BestWave);
                Assert.IsTrue(System.IO.File.Exists(path), "leaving play writes the save");

                _flow.RetryWave();
                yield return null;
                _spawner.StartNextWave();
                yield return null;
                _hill.TakeBaseDamage(_hill.MaxHealth, Vector3.zero);
                Assert.IsFalse(_flow.LastRunResult.NewBestWave, "same wave, one more retry");

                GrayboxSave.Close();
                GrayboxSave.Open(path);
                Assert.AreEqual(1, GrayboxSave.Data.records.runsFinished);
                Assert.AreEqual(2, GrayboxSave.Data.FindLevel("Map_01").bestWave);
                Assert.AreEqual(0, GrayboxSave.Data.FindLevel("Map_01").bestWaveRetries);

                // A restart is a new run.
                _flow.StartNewRun();
                yield return null;
                _spawner.StartNextWave();
                yield return null;
                _hill.TakeBaseDamage(_hill.MaxHealth, Vector3.zero);
                Assert.AreEqual(2, GrayboxSave.Data.records.runsFinished);
            }
            finally
            {
                GrayboxSave.Close();
                if (System.IO.Directory.Exists(dir)) System.IO.Directory.Delete(dir, true);
            }
        }

        // ---------- rewinding ----------

        [UnityTest]
        public IEnumerator RetryWave_RewindsToTheStartOfTheCurrentWave()
        {
            yield return null;
            _spawner.StartNextWave(); // wave 1
            yield return null;

            _economy.TrySpendGold(200);
            _skills.TryUnlock(SkillNodeId.Ballistics_HeavyCaliber);
            int pointsAtWave2 = _skills.SkillPoints;
            _hill.TakeBaseDamage(30f, Vector3.zero);

            _spawner.StartNextWave(); // wave 2: checkpoint taken here
            yield return null;
            Assert.IsTrue(_flow.CanRetryWave);
            Assert.AreEqual(2, _flow.RetryWaveNumber);

            _economy.TrySpendGold(100);
            _skills.AddSkillPoints(7);
            _hill.TakeBaseDamage(_hill.MaxHealth, Vector3.zero);
            Assert.AreEqual(GameFlowState.RunEnd, _flow.State);

            _flow.RetryWave();
            yield return null;

            Assert.AreEqual(GameFlowState.Playing, _flow.State);
            Assert.AreEqual(1f, Time.timeScale);
            Assert.AreEqual(300, _economy.CurrentGold);
            Assert.AreEqual(_hill.MaxHealth - 30f, _hill.CurrentHealth);
            Assert.IsFalse(_hill.IsFinished);
            Assert.AreEqual(pointsAtWave2, _skills.SkillPoints);
            Assert.IsTrue(_skills.IsUnlocked(SkillNodeId.Ballistics_HeavyCaliber));
            Assert.AreEqual(2, _spawner.CurrentWaveNumber, "wave 2 is next");
            Assert.AreEqual(EnemySpawner.SpawnerState.WaitingForNextWave, _spawner.CurrentState, "waits for the player");
            Assert.AreEqual(1, _flow.Stats.WaveRetries);

            // Retries are unlimited: the same checkpoint restores again.
            _economy.TrySpendGold(250);
            _flow.RetryWave();
            Assert.AreEqual(300, _economy.CurrentGold);
            Assert.AreEqual(2, _flow.Stats.WaveRetries);
        }

        [UnityTest]
        public IEnumerator StartNewRun_GoesBackToWaveOne_WithStartingGoldAndSkills()
        {
            yield return null;
            int startingPoints = _skills.SkillPoints;

            _spawner.StartNextWave();
            yield return null;
            _spawner.StartNextWave();
            yield return null;
            _economy.TrySpendGold(400);
            _skills.TryUnlock(SkillNodeId.Ballistics_HeavyCaliber);
            _hill.TakeBaseDamage(_hill.MaxHealth, Vector3.zero);
            _flow.RetryWave();

            _flow.StartNewRun();
            yield return null;

            Assert.AreEqual(GameFlowState.Playing, _flow.State);
            Assert.AreEqual(500, _economy.CurrentGold);
            Assert.AreEqual(_hill.MaxHealth, _hill.CurrentHealth);
            Assert.AreEqual(startingPoints, _skills.SkillPoints);
            Assert.IsFalse(_skills.IsUnlocked(SkillNodeId.Ballistics_HeavyCaliber));
            Assert.AreEqual(1, _spawner.CurrentWaveNumber);
            Assert.AreEqual(EnemySpawner.SpawnerState.Idle, _spawner.CurrentState);
            Assert.AreEqual(0, _flow.Stats.WaveRetries);
            Assert.IsFalse(_flow.CanRetryWave, "no wave has started in the new run");
        }

        [UnityTest]
        public IEnumerator RetryWave_PutsTowersBack_AsTheyWereAtWaveStart()
        {
            yield return null;

            Tower kept = MakeTower("Tower_Kept", new Vector3(2f, 1f, 0f));
            Tower sold = MakeTower("Tower_Sold", new Vector3(-3f, 1f, 0f));
            yield return null;
            kept.Upgrade(); // level 2 at the checkpoint
            kept.TotalGoldInvested = 250;

            _spawner.StartNextWave();
            yield return null;

            // During the wave: one tower upgraded again, one sold, one new one built.
            kept.Upgrade();
            Object.Destroy(sold.gameObject);
            MakeTower("Tower_BuiltLater", new Vector3(5f, 5f, 0f));
            yield return null;

            _hill.TakeBaseDamage(_hill.MaxHealth, Vector3.zero);
            _flow.RetryWave();
            yield return null;
            yield return null;

            Tower[] towers = Object.FindObjectsByType<Tower>();
            CollectionAssert.AreEquivalent(new[] { "Tower_Kept", "Tower_Sold" }, towers.Select(t => t.name).ToArray());

            Tower restored = towers.First(t => t.name == "Tower_Kept");
            Assert.AreEqual(2, restored.Level);
            Assert.AreEqual(250, restored.TotalGoldInvested);
            Assert.AreEqual(new Vector3(2f, 1f, 0f), restored.transform.position);
            Assert.AreEqual(1, ChildCount(restored, "RangeCircle"), "one range ring, not one per restore");

            // A second retry must not stack copies or helper children either.
            _flow.RetryWave();
            yield return null;
            yield return null;

            towers = Object.FindObjectsByType<Tower>();
            Assert.AreEqual(2, towers.Length);
            Assert.AreEqual(1, ChildCount(towers.First(t => t.name == "Tower_Kept"), "RangeCircle"));
        }

        [UnityTest]
        public IEnumerator QuitToHome_ClearsTheField_AndPlayStartsClean()
        {
            yield return null;
            MakeTower("Tower_BuiltThisRun", Vector3.one);
            _spawner.StartNextWave();
            yield return null;

            _flow.QuitToHome();
            yield return null;

            Assert.AreEqual(GameFlowState.Home, _flow.State);
            Assert.AreEqual(0f, Time.timeScale);
            Assert.AreEqual(0, Object.FindObjectsByType<Tower>().Length);

            _flow.StartNewRun();
            yield return null;

            Assert.AreEqual(GameFlowState.Playing, _flow.State);
            Assert.AreEqual(0, Object.FindObjectsByType<Tower>().Length, "the run-start checkpoint had no towers");
            Assert.AreEqual(1, _spawner.CurrentWaveNumber);
        }

        private static Tower MakeTower(string name, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.position = position;
            Tower tower = go.AddComponent<Tower>();
            go.AddComponent<TowerTargetVisualizer>();
            return tower;
        }

        private static int ChildCount(Tower tower, string childName)
        {
            return tower.transform.Cast<Transform>().Count(child => child.name == childName);
        }
    }
}
