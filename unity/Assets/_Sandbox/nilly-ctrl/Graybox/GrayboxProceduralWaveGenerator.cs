using System.Collections.Generic;
using HoldTheHill.Features.Enemies;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Procedural wave generator dynamically creating infinite scaling waves beyond static wave assets.
    /// Ramps enemy counts, archetype distribution, health scaling, and boss horde encounters.
    /// </summary>
    public class GrayboxProceduralWaveGenerator : MonoBehaviour
    {
        public static GrayboxProceduralWaveGenerator Instance { get; private set; }

        [Header("Procedural Wave Scaling")]
        [SerializeField] private float _hpScalingPerWave = 0.12f;
        [SerializeField] private float _speedScalingPerWave = 0.02f;
        [SerializeField] private float _maxSpeedMultiplier = 1.5f;

        private EnemySpawner _spawner;

        private void Awake()
        {
            Instance = this;
            _spawner = FindAnyObjectByType<EnemySpawner>();
        }

        /// <summary>
        /// Generates a procedural wave configuration for the given wave number.
        /// </summary>
        public Wave GenerateWave(int waveNumber, GameObject runnerPrefab, GameObject gruntPrefab, GameObject brutePrefab, GameObject shieldedPrefab, GameObject splitterPrefab, GameObject healerPrefab)
        {
            float interval = Mathf.Max(0.3f, 1.2f - (waveNumber * 0.03f));
            var wave = new Wave
            {
                waveName = waveNumber % 5 == 0 ? $"Wave {waveNumber} — BOSS HORDE" : $"Wave {waveNumber} — Endless Swarm",
                enemies = new List<EnemySpawnEntry>()
            };

            int enemyCount = 14 + (waveNumber * 4);
            bool isBossWave = waveNumber % 5 == 0;

            for (int i = 0; i < enemyCount; i++)
            {
                GameObject prefab = SelectPrefabForWave(waveNumber, isBossWave, i, runnerPrefab, gruntPrefab, brutePrefab, shieldedPrefab, splitterPrefab, healerPrefab);
                if (prefab != null)
                {
                    wave.enemies.Add(new EnemySpawnEntry
                    {
                        enemyPrefab = prefab,
                        delayBeforeNext = interval
                    });
                }
            }

            return wave;
        }

        private GameObject SelectPrefabForWave(int waveNumber, bool isBossWave, int index, GameObject runner, GameObject grunt, GameObject brute, GameObject shielded, GameObject splitter, GameObject healer)
        {
            if (isBossWave && index % 4 == 0)
            {
                return brute != null ? brute : grunt;
            }

            if (waveNumber >= 10 && index % 6 == 0)
            {
                return healer != null ? healer : grunt;
            }

            if (waveNumber >= 8 && index % 5 == 0)
            {
                return splitter != null ? splitter : grunt;
            }

            if (waveNumber >= 6 && index % 4 == 0)
            {
                return shielded != null ? shielded : grunt;
            }

            if (index % 3 == 0)
            {
                return runner != null ? runner : grunt;
            }

            return grunt;
        }

        public float GetHealthMultiplier(int waveNumber)
        {
            return 1.0f + (waveNumber - 1) * _hpScalingPerWave;
        }

        public float GetSpeedMultiplier(int waveNumber)
        {
            return Mathf.Min(_maxSpeedMultiplier, 1.0f + (waveNumber - 1) * _speedScalingPerWave);
        }
    }
}
