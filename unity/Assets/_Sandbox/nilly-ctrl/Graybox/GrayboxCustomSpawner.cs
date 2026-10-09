using System.Collections;
using System.Collections.Generic;
using HoldTheHill.Features.Enemies;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Custom horde spawner for stress-testing tower defense setups on demand.
    /// Allows spawning custom enemy hordes (e.g. Swarm Rush, Brute Parade, Hydra Splitters, 100-enemy Mega Horde).
    /// </summary>
    [AddComponentMenu("Hold the Hill/Graybox/Graybox Custom Spawner")]
    public class GrayboxCustomSpawner : MonoBehaviour
    {
        /// <summary>One enemy the presets and endless waves can use, looked up by id.</summary>
        [System.Serializable]
        public class Entry
        {
            public string Id;
            public GameObject Prefab;
            [Tooltip("Included in the 100-enemy mixed horde.")]
            public bool Mixed = true;
        }

        [Header("Prefabs")]
        [Tooltip("Ids the presets ask for: Runner, Grunt, Brute, Shielded, Swarm, Splitter, Healer.")]
        [SerializeField] private List<Entry> _enemies = new List<Entry>();

        [Header("Spawn Settings")]
        [SerializeField] private Transform _spawnPoint;
        [SerializeField] private Transform _enemyContainer;

        private EnemySpawner _spawner;
        private Coroutine _customHordeCoroutine;

        public bool IsCustomSpawning { get; private set; }

        public void Configure(List<Entry> enemies, Transform spawnPoint, Transform container)
        {
            _enemies = enemies;
            _spawnPoint = spawnPoint;
            _enemyContainer = container;
        }

        /// <summary>The prefab with this id, or the fallback id's prefab, or null.</summary>
        public GameObject Find(string id, string fallbackId = null)
        {
            foreach (Entry entry in _enemies)
            {
                if (entry.Id == id && entry.Prefab != null)
                {
                    return entry.Prefab;
                }
            }

            return fallbackId != null ? Find(fallbackId) : null;
        }

        private void Awake()
        {
            _spawner = GetComponent<EnemySpawner>();
            if (_spawnPoint == null)
            {
                _spawnPoint = transform;
            }
        }

        /// <summary>
        /// Spawns a custom sequence of enemies with a given delay between spawns.
        /// </summary>
        public void SpawnCustomHorde(List<GameObject> prefabs, float delayBetween = 0.5f)
        {
            if (prefabs == null || prefabs.Count == 0)
            {
                return;
            }

            if (_customHordeCoroutine != null)
            {
                StopCoroutine(_customHordeCoroutine);
            }

            _customHordeCoroutine = StartCoroutine(RunCustomHorde(prefabs, delayBetween));
        }

        /// <summary>Stops a custom horde that is still spawning.</summary>
        public void StopCustomSpawning()
        {
            if (_customHordeCoroutine != null)
            {
                StopCoroutine(_customHordeCoroutine);
                _customHordeCoroutine = null;
            }

            IsCustomSpawning = false;
        }

        private IEnumerator RunCustomHorde(List<GameObject> prefabs, float delayBetween)
        {
            IsCustomSpawning = true;
            Debug.Log($"[GrayboxCustomSpawner] Launching custom horde with {prefabs.Count} enemies!");

            for (int i = 0; i < prefabs.Count; i++)
            {
                GameObject prefab = prefabs[i];
                if (prefab != null)
                {
                    Vector3 pos = _spawnPoint != null ? _spawnPoint.position : transform.position;
                    GameObject enemy = Instantiate(prefab, pos, Quaternion.identity, _enemyContainer);

                    var mover = enemy.GetComponent<EnemyMover>();
                    if (mover != null)
                    {
                        mover.SnapToStart();
                    }

                    if (_spawner != null)
                    {
                        _spawner.onEnemySpawned?.Invoke(enemy);
                    }
                }

                if (delayBetween > 0f)
                {
                    yield return new WaitForSeconds(delayBetween);
                }
            }

            IsCustomSpawning = false;
            _customHordeCoroutine = null;
        }

        // ---------- Preset Horde Launchers ----------

        public void SpawnProceduralWave(int waveNumber)
        {
            if (GrayboxProceduralWaveGenerator.Instance == null) return;

            Wave waveData = GrayboxProceduralWaveGenerator.Instance.GenerateWave(
                waveNumber, Find("Runner"), Find("Grunt"), Find("Brute"), Find("Shielded"), Find("Splitter"), Find("Healer"));

            List<GameObject> prefabs = new List<GameObject>();
            float delay = 0.5f;
            foreach (var entry in waveData.enemies)
            {
                if (entry != null && entry.enemyPrefab != null)
                {
                    prefabs.Add(entry.enemyPrefab);
                    delay = entry.delayBeforeNext;
                }
            }

            SpawnCustomHorde(prefabs, delay);
        }

        public void SpawnPreset_SwarmRush()
        {
            List<GameObject> prefabs = BuildRepeatList(Find("Swarm", "Runner"), 40);
            SpawnCustomHorde(prefabs, 0.25f);
        }

        public void SpawnPreset_BruteParade()
        {
            List<GameObject> prefabs = BuildRepeatList(Find("Brute"), 12);
            SpawnCustomHorde(prefabs, 0.85f);
        }

        public void SpawnPreset_Phalanx()
        {
            List<GameObject> list = new List<GameObject>();
            GameObject shielded = Find("Shielded", "Grunt");
            GameObject healer = Find("Healer", "Grunt");

            for (int i = 0; i < 4; i++)
            {
                list.Add(shielded);
                list.Add(shielded);
                list.Add(shielded);
                list.Add(healer);
            }
            SpawnCustomHorde(list, 0.7f);
        }

        public void SpawnPreset_HydraSplitters()
        {
            List<GameObject> prefabs = BuildRepeatList(Find("Splitter", "Grunt"), 10);
            SpawnCustomHorde(prefabs, 0.85f);
        }

        public void SpawnPreset_MegaHorde()
        {
            List<GameObject> list = new List<GameObject>();
            var pool = new List<GameObject>();
            foreach (Entry entry in _enemies)
            {
                if (entry.Mixed && entry.Prefab != null)
                {
                    pool.Add(entry.Prefab);
                }
            }

            if (pool.Count == 0)
            {
                return;
            }

            for (int i = 0; i < 100; i++)
            {
                list.Add(pool[Random.Range(0, pool.Count)]);
            }

            SpawnCustomHorde(list, 0.35f);
        }

        private static List<GameObject> BuildRepeatList(GameObject prefab, int count)
        {
            List<GameObject> list = new List<GameObject>();
            if (prefab == null)
            {
                return list;
            }

            for (int i = 0; i < count; i++)
            {
                list.Add(prefab);
            }
            return list;
        }
    }
}
