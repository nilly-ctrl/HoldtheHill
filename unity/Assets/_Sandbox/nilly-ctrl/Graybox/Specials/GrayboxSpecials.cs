using System;
using System.Collections.Generic;
using HoldTheHill.Features.Enemies;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Holds the boss and special-enemy prefabs for the graybox scene, spawns any of them on
    /// demand from a small panel (B), and puts towers back to rights as each wave starts.
    /// </summary>
    public class GrayboxSpecials : MonoBehaviour
    {
        [Serializable]
        public class Entry
        {
            public string Id;
            public string Label;
            public GameObject Prefab;
            public bool Boss;
        }

        public static GrayboxSpecials Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        /// <summary>The scene's animation library, for effects made by code that has no sprite of its own.</summary>
        public static SpriteAnimLibrary Library => Instance != null ? Instance._library : null;

        [SerializeField] private SpriteAnimLibrary _library;
        [SerializeField] private List<Entry> _entries = new List<Entry>();
        [SerializeField] private Transform _spawnPoint;
        [SerializeField] private Transform _enemyContainer;

        private EnemySpawner _spawner;

        public IReadOnlyList<Entry> Entries => _entries;

        public void Configure(SpriteAnimLibrary library, List<Entry> entries, Transform spawnPoint, Transform container)
        {
            _library = library;
            _entries = entries;
            _spawnPoint = spawnPoint;
            _enemyContainer = container;
        }

        private void Awake()
        {
            Instance = this;
            _spawner = FindAnyObjectByType<EnemySpawner>();
        }

        private void OnEnable()
        {
            if (_spawner != null)
            {
                _spawner.onWaveStarted.AddListener(OnWaveStarted);
            }
        }

        private void OnDisable()
        {
            if (_spawner != null)
            {
                _spawner.onWaveStarted.RemoveListener(OnWaveStarted);
            }

            if (Instance == this)
            {
                Instance = null;
            }
        }

        // A tower a hornet got away with comes back for the next wave.
        private void OnWaveStarted(int wave, int total)
        {
            GrayboxTowerAffliction.RestoreAll();
        }

        public GameObject Find(string id)
        {
            foreach (Entry entry in _entries)
            {
                if (entry.Id == id)
                {
                    return entry.Prefab;
                }
            }

            return null;
        }

        /// <summary>Spawns one special enemy at the burrow, as the wave spawner would.</summary>
        public GameObject Spawn(string id)
        {
            GameObject prefab = Find(id);
            if (prefab == null)
            {
                return null;
            }

            Vector3 position = _spawnPoint != null ? _spawnPoint.position : transform.position;
            if (_spawner != null)
            {
                return _spawner.Spawn(prefab, position, Quaternion.identity, true);
            }

            GameObject enemy = Instantiate(prefab, position, Quaternion.identity, _enemyContainer);
            var mover = enemy.GetComponent<EnemyMover>();
            if (mover != null)
            {
                mover.SnapToStart();
            }

            return enemy;
        }

        /// <summary>A looping effect from the library, as a new object. Null if the clip is missing.</summary>
        public static SpriteClipPlayer LoopFx(SpriteAnimLibrary library, string key, string loop, string intro, Vector3 position, Quaternion rotation, int order)
        {
            if (library == null || library.Find(key)?.Find(loop) == null)
            {
                return null;
            }

            var go = new GameObject($"{key} {loop}");
            go.transform.SetPositionAndRotation(position, rotation);
            go.AddComponent<SpriteRenderer>().sortingOrder = order;
            var player = go.AddComponent<SpriteClipPlayer>();
            player.Configure(library, key, loop);
            if (intro != null)
            {
                player.Play(intro);
            }

            return player;
        }
    }
}
