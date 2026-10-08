#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HoldTheHill.Sandbox.NillyCtrl;
using HoldTheHill.Sandbox.UiKit;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace HoldTheHill.Sandbox.Graybox.Tests
{
    /// <summary>
    /// Checks the menu screens against the flow controller: they open and close with its state,
    /// their buttons drive it, and the destructive ones ask first. Title, home, settings and
    /// controls are in GrayboxMenuScreensTests.cs (the same class, split by file).
    /// </summary>
    /// <remarks>
    /// Uses the generated GrayboxMenus prefab (Tools > Hold the Hill > Build Graybox Menus) with a
    /// spawner and hill built in code, so it does not depend on the graybox scene.
    /// </remarks>
    public partial class GrayboxMenusTests
    {
        private const string MenusPrefab = "Assets/_Sandbox/nilly-ctrl/Graybox/Prefabs/GrayboxMenus.prefab";
        private const string EventSystemPrefab = "Assets/_Sandbox/nilly-ctrl/UiKit/Prefabs/UiEventSystem.prefab";

        private readonly List<Object> _made = new List<Object>();
        private readonly List<GameObject> _spawnedEnemies = new List<GameObject>();
        private EnemySpawner _spawner;
        private GrayboxBaseHealth _hill;
        private GrayboxGameFlow _flow;
        private GrayboxMenus _menus;
        private EventSystem _events;

        [SetUp]
        public void SetUp()
        {
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
            systems.AddComponent<GrayboxEconomy>();
            _hill = systems.AddComponent<GrayboxBaseHealth>();
            systems.AddComponent<GrayboxSkillTree>();

            var spawnerObject = new GameObject("TestSpawner");
            spawnerObject.SetActive(false);
            _made.Add(spawnerObject);
            _spawner = spawnerObject.AddComponent<EnemySpawner>();
            typeof(EnemySpawner).GetField("activeMapConfigOverride", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_spawner, waves);
            _spawner.onEnemySpawned = new UnityEvent<GameObject>();
            _spawner.onWaveStarted = new UnityEvent<int, int>();
            _spawner.onWaveCompleted = new UnityEvent<int>();
            _spawner.onAllWavesCompleted = new UnityEvent();
            _spawner.onMapChanged = new UnityEvent<string>();
            spawnerObject.SetActive(true);
            _spawner.onEnemySpawned.AddListener(_spawnedEnemies.Add);

            var flowObject = new GameObject("TestFlow");
            _made.Add(flowObject);
            _flow = flowObject.AddComponent<GrayboxGameFlow>();

            _events = Spawn(EventSystemPrefab).GetComponent<EventSystem>();
            _menus = Spawn(MenusPrefab).GetComponent<GrayboxMenus>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _made.Concat(_spawnedEnemies))
            {
                if (o != null) Object.DestroyImmediate(o);
            }

            _made.Clear();
            _spawnedEnemies.Clear();
            Time.timeScale = 1f;
            GrayboxSave.Close();
            GrayboxSettings.Apply(); // back to the defaults for the next test
            GrayboxControls.ResetToDefaults();
            if (_saveFile != null && System.IO.File.Exists(_saveFile)) System.IO.File.Delete(_saveFile);
            _saveFile = null;
            if (_keyboard != null) UnityEngine.InputSystem.InputSystem.RemoveDevice(_keyboard);
            _keyboard = null;
            if (_inputSettingsChanged)
            {
                UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior = _backgroundBehavior;
                UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode = _editorInputBehavior;
                _inputSettingsChanged = false;
            }
        }

        // ---------- pause ----------

        [UnityTest]
        public IEnumerator Pausing_OpensThePauseScreen_FocusedOnResume()
        {
            yield return null;
            Assert.AreEqual(0, UiScreen.OpenCount, "nothing open while playing");

            _spawner.StartNextWave();
            yield return null;
            _flow.Pause();
            yield return null;
            yield return null;

            Assert.IsTrue(_menus.Pause.Screen.IsTop);
            Assert.AreSame(_menus.Pause.ResumeButton.gameObject, _events.currentSelectedGameObject);
            Assert.AreEqual("Wave 1 of 3", _menus.Pause.transform.Find("Panel/Text").GetComponent<TMPro.TMP_Text>().text);

            _menus.Pause.ResumeButton.Clicked.Invoke();
            yield return null;

            Assert.AreEqual(GameFlowState.Playing, _flow.State);
            Assert.AreEqual(0, UiScreen.OpenCount);
        }

        [UnityTest]
        public IEnumerator Back_OnThePauseScreen_Resumes()
        {
            yield return null;
            _flow.Pause();
            yield return null;

            _menus.Pause.Screen.Back();
            yield return null;

            Assert.AreEqual(GameFlowState.Playing, _flow.State);
            Assert.IsFalse(_menus.Pause.gameObject.activeSelf);
        }

        [UnityTest]
        public IEnumerator SettingsAndControls_OpenOverThePauseScreen_AndBackReturnsToIt()
        {
            yield return null;
            _flow.Pause();
            yield return null;

            _menus.Pause.SettingsButton.Clicked.Invoke();
            yield return null;
            Assert.IsTrue(_menus.Settings.Screen.IsTop);
            Assert.IsTrue(_menus.Pause.Screen.IsOpen, "the pause screen stays underneath");

            _menus.Settings.Screen.Back();
            yield return null;
            Assert.IsTrue(_menus.Pause.Screen.IsTop);
            Assert.AreEqual(GameFlowState.Paused, _flow.State, "back closed settings, not the pause menu");

            _menus.Pause.ControlsButton.Clicked.Invoke();
            yield return null;
            Assert.IsTrue(_menus.Controls.Screen.IsTop);

            // Resuming from elsewhere closes whatever was open on top.
            _flow.Resume();
            yield return null;
            Assert.AreEqual(0, UiScreen.OpenCount);
        }

        [UnityTest]
        public IEnumerator QuitToHome_AsksFirst()
        {
            yield return null;
            _spawner.StartNextWave();
            yield return null;
            _flow.Pause();
            yield return null;
            yield return null;

            // A player has the button focused before pressing it; do the same.
            _events.SetSelectedGameObject(_menus.Pause.QuitButton.gameObject);
            yield return null;
            _menus.Pause.QuitButton.Clicked.Invoke();
            yield return null;
            yield return null;

            Assert.IsTrue(_menus.Dialog.IsOpen);
            Assert.AreSame(_menus.Dialog.GetComponent<UiScreen>(), UiScreen.Top);
            Assert.AreEqual(GameFlowState.Paused, _flow.State, "nothing happens until the answer");
            Assert.AreEqual("CancelButton", _events.currentSelectedGameObject.name);

            // No: back to the pause menu, still paused.
            _menus.Dialog.GetComponent<UiScreen>().Back();
            yield return null;
            yield return null;
            Assert.IsFalse(_menus.Dialog.IsOpen);
            Assert.AreEqual(GameFlowState.Paused, _flow.State);
            Assert.IsTrue(_menus.Pause.Screen.IsTop);
            Assert.AreSame(_menus.Pause.QuitButton.gameObject, _events.currentSelectedGameObject, "focus returns to the button that asked");

            // Yes: home, with every menu closed.
            _menus.Pause.QuitButton.Clicked.Invoke();
            yield return null;
            Confirm();
            yield return null;

            Assert.AreEqual(GameFlowState.Home, _flow.State);
            Assert.IsTrue(_menus.Home.Screen.IsTop, "only the hub is open");
            Assert.AreEqual(1, UiScreen.OpenCount);
        }

        [UnityTest]
        public IEnumerator Restart_AsksFirst_ThenStartsAFreshRun()
        {
            yield return null;
            _spawner.StartNextWave();
            yield return null;
            _spawner.StartNextWave();
            yield return null;
            _flow.Pause();
            yield return null;

            _menus.Pause.RestartButton.Clicked.Invoke();
            yield return null;
            Assert.IsTrue(_menus.Dialog.IsOpen);
            Confirm();
            yield return null;

            Assert.AreEqual(GameFlowState.Playing, _flow.State);
            Assert.AreEqual(1, _spawner.CurrentWaveNumber);
            Assert.AreEqual(0, UiScreen.OpenCount);
        }

        [UnityTest]
        public IEnumerator ResumingWithADialogOpen_ClosesTheDialogToo()
        {
            yield return null;
            _flow.Pause();
            yield return null;
            _menus.Pause.QuitButton.Clicked.Invoke();
            yield return null;

            _flow.Resume();
            yield return null;

            Assert.AreEqual(0, UiScreen.OpenCount);
            Assert.AreEqual(GameFlowState.Playing, _flow.State, "the unanswered question did not quit the run");
        }

        // ---------- run end ----------

        [UnityTest]
        public IEnumerator Defeat_ShowsTheRunEndScreen_WithStatsAndRetry()
        {
            yield return null;
            _spawner.StartNextWave();
            yield return null;
            _spawner.StartNextWave(); // wave 2
            yield return null;

            _hill.TakeBaseDamage(_hill.MaxHealth, Vector3.zero);
            yield return null;
            yield return null;

            GrayboxRunEndScreen screen = _menus.RunEnd;
            Assert.IsTrue(screen.Screen.IsTop);
            Assert.AreEqual("Hill overrun", screen.Title.text);
            Assert.AreEqual("2 of 3", screen.Wave.text);
            Assert.IsTrue(screen.NewBest.gameObject.activeSelf, "first run is a new best");
            Assert.AreEqual("Wave 2", screen.Best.text);
            Assert.IsTrue(screen.RetryWaveButton.gameObject.activeSelf);
            Assert.AreEqual("Retry wave 2", screen.RetryWaveButton.Label);
            Assert.AreSame(screen.RetryWaveButton.gameObject, _events.currentSelectedGameObject);

            // Back does nothing here: one of the buttons has to be chosen.
            screen.Screen.Back();
            yield return null;
            Assert.IsTrue(screen.Screen.IsTop);

            screen.RetryWaveButton.Clicked.Invoke();
            yield return null;

            Assert.AreEqual(GameFlowState.Playing, _flow.State);
            Assert.AreEqual(2, _spawner.CurrentWaveNumber);
            Assert.AreEqual(0, UiScreen.OpenCount);

            // Losing again on the same wave is not a new best, and shows the retry.
            _spawner.StartNextWave();
            yield return null;
            _hill.TakeBaseDamage(_hill.MaxHealth, Vector3.zero);
            yield return null;
            Assert.IsFalse(screen.NewBest.gameObject.activeSelf);
            Assert.AreEqual("1", screen.transform.Find("Panel/WaveretriesusedRow/Value").GetComponent<TMPro.TMP_Text>().text);
        }

        [UnityTest]
        public IEnumerator Victory_UsesTheSameScreen_WithoutRetry()
        {
            yield return null;
            for (int i = 0; i < 3; i++)
            {
                _spawner.StartNextWave();
                yield return null;
            }

            foreach (GameObject enemy in _spawnedEnemies) _spawner.NotifyEnemyDefeated(enemy);
            yield return null;
            yield return null;
            yield return null;

            GrayboxRunEndScreen screen = _menus.RunEnd;
            Assert.AreEqual(GameFlowState.RunEnd, _flow.State);
            Assert.IsTrue(screen.Screen.IsTop);
            Assert.AreEqual("Hill defended", screen.Title.text);
            Assert.AreEqual("3 of 3", screen.Wave.text);
            Assert.IsFalse(screen.RetryWaveButton.gameObject.activeSelf, "nothing to retry after a win");
            Assert.AreSame(screen.RestartButton.gameObject, _events.currentSelectedGameObject);

            screen.QuitButton.Clicked.Invoke();
            yield return null;
            Assert.AreEqual(GameFlowState.Home, _flow.State);
            Assert.IsTrue(_menus.Home.Screen.IsTop, "only the hub is open");
            Assert.AreEqual(1, UiScreen.OpenCount);
        }

        // ---------- helpers ----------

        private void Confirm()
        {
            _menus.Dialog.transform.Find("Panel/Buttons/ConfirmButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
        }

        private GameObject Spawn(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.IsNotNull(prefab, $"Missing {path}: run Tools > Hold the Hill > Build UI Kit, then Build Graybox Menus.");
            GameObject instance = Object.Instantiate(prefab);
            _made.Add(instance);
            return instance;
        }
    }
}
#endif
