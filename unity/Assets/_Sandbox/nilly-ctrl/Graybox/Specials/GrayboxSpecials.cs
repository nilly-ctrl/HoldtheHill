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

        /// <summary>The scene's animation library, for effects made by code that has no sprite of its own.</summary>
        public static SpriteAnimLibrary Library => Instance != null ? Instance._library : null;

        [SerializeField] private SpriteAnimLibrary _library;
        [SerializeField] private List<Entry> _entries = new List<Entry>();
        [SerializeField] private Transform _spawnPoint;
        [SerializeField] private Transform _enemyContainer;

        private EnemySpawner _spawner;
        private bool _panelOpen;
        private GUIStyle _button;
        private GUIStyle _label;

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
            GameObject enemy = Instantiate(prefab, position, Quaternion.identity, _enemyContainer);
            var mover = enemy.GetComponent<EnemyMover>();
            if (mover != null)
            {
                mover.SnapToStart();
            }

            if (_spawner != null)
            {
                _spawner.onEnemySpawned?.Invoke(enemy);
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

        private void Update()
        {
            if (GrayboxGameFlow.GameplayActive && GrayboxControls.Pressed(GrayboxControls.SpecialsPanel))
            {
                _panelOpen = !_panelOpen;
            }
        }

        private void OnGUI()
        {
            if (!_panelOpen || !GrayboxGameFlow.GameplayActive)
            {
                return;
            }

            if (_button == null)
            {
                _button = new GUIStyle(GUI.skin.button) { fontSize = 13, alignment = TextAnchor.MiddleLeft };
                _label = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold };
            }

            const float Width = 210f;
            float height = 60f + _entries.Count * 26f;
            GUILayout.BeginArea(new Rect(Screen.width - Width - 12f, 120f, Width, height), GUI.skin.box);
            GUILayout.Label($"Spawn a special ({GrayboxControls.Name(GrayboxControls.SpecialsPanel)} closes)", _label);
            bool lastBoss = true;
            foreach (Entry entry in _entries)
            {
                if (lastBoss && !entry.Boss)
                {
                    GUILayout.Space(6f);
                }

                lastBoss = entry.Boss;
                if (GUILayout.Button((entry.Boss ? "BOSS  " : "") + entry.Label, _button, GUILayout.Height(22f)))
                {
                    Spawn(entry.Id);
                }
            }

            GUILayout.EndArea();
        }
    }
}
