#if UNITY_EDITOR
using HoldTheHill.Core.Input;
using HoldTheHill.Core.Save;
using HoldTheHill.Features.Menus;
using HoldTheHill.Features.GameFlow;
using System.Collections;
using System.IO;
using HoldTheHill.Features.Enemies;
using HoldTheHill.Core.UiKit;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace HoldTheHill.Tests
{
    /// <summary>
    /// Not a test: walks the real graybox scene through every menu screen and saves a picture of
    /// each, so menu changes can be checked by eye from a batch run. Explicit, so it only runs
    /// when named:
    /// <c>-runTests -testPlatform PlayMode -testFilter MenuShots</c>, with the folder for
    /// the pictures in the HTH_SHOT_DIR environment variable (a temp folder otherwise).
    /// </summary>
    /// <remarks>
    /// Run it with the editor window showing, not with <c>-batchmode</c>: the menus are a
    /// screen-space overlay and the HUD is IMGUI, and neither is drawn without a Game view.
    /// The pictures are whatever size the Game view is. The scene is played with a throwaway
    /// save file in the pictures folder: nothing is read from or written to the player's own.
    /// </remarks>
    public class MenuShots
    {
        private const string ScenePath = "Assets/_Game/Scenes/Dev/CombatTest.unity";
        private string _dir;

        [UnityTest, Explicit, Category("Screenshots")]
        public IEnumerator SaveAPictureOfEveryMenuScreen()
        {
            _dir = System.Environment.GetEnvironmentVariable("HTH_SHOT_DIR");
            if (string.IsNullOrEmpty(_dir)) _dir = Path.Combine(Application.temporaryCachePath, "MenuShots");
            Directory.CreateDirectory(_dir);

            // A throwaway save, attached before the scene's save host looks for the player's own.
            string saveFile = Path.Combine(_dir, "shots-save.json");
            if (File.Exists(saveFile)) File.Delete(saveFile);
            SaveStore.Open(saveFile);

            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;


            RunFlow flow = Object.FindAnyObjectByType<RunFlow>();
            var menus = Object.FindAnyObjectByType<MenuScreens>();
            Assert.IsNotNull(flow, "the scene has no flow controller: rebuild it (Tools > Hold the Hill > Build Combat Test)");
            Assert.IsNotNull(menus, "the scene has no menus: rebuild it");

            Assert.AreEqual(GameFlowState.Title, flow.State, "the game opens on the title screen");
            yield return Shot("01-title");

            menus.Title.SettingsButton.Clicked.Invoke();
            yield return Shot("02-settings-audio");
            yield return Tab(menus.Settings, 1, "03-settings-display");
            yield return Tab(menus.Settings, 2, "04-settings-gameplay");
            yield return Tab(menus.Settings, 3, "05-settings-access");
            menus.Settings.Screen.Hide();

            menus.Title.ControlsButton.Clicked.Invoke();
            yield return Shot("06-controls-game");
            yield return Tab(menus.Controls, 1, "07-controls-panels");
            yield return Tab(menus.Controls, 2, "08-controls-build");
            GameControls.SetBinding(GameControls.Speed, GameControls.KeyboardGroup, "<Keyboard>/n");
            yield return Tab(menus.Controls, 0, "09-controls-clash");
            menus.Controls.ShowGroup(GameControls.GamepadGroup);
            yield return Shot("10-controls-gamepad");
            GameControls.ResetToDefaults();
            menus.Controls.Screen.Hide();

            menus.Title.QuitButton.Clicked.Invoke();
            yield return Shot("11-title-quit-dialog");
            menus.Dialog.Dismiss();

            menus.Title.PlayButton.Clicked.Invoke();
            MetaProgress.AddCurrency(8);
            yield return Shot("12-home-play");
            yield return Tab(menus.Home, 1, "13-home-upgrades");
            yield return Tab(menus.Home, 2, "14-home-awards");
            yield return Tab(menus.Home, 3, "15-home-records");

            menus.Home.StartButton.Clicked.Invoke();
            yield return null;
            Assert.AreEqual(GameFlowState.Playing, flow.State);
            var spawner = Object.FindAnyObjectByType<EnemySpawner>();
            spawner.StartNextWave();
            yield return new WaitForSeconds(2.5f);
            yield return Shot("16-playing-no-menu");

            flow.Pause();
            yield return Shot("17-pause");
            menus.Pause.SettingsButton.Clicked.Invoke();
            yield return Shot("18-pause-settings");
            menus.Settings.Screen.Hide();
            flow.Resume();
            yield return null;

            spawner.StartNextWave();
            yield return null;
            Queen hill = Object.FindAnyObjectByType<Queen>();
            hill.TakeDamage(hill.MaxHealth, Vector3.zero);
            yield return Shot("19-defeat-banner-first");
            yield return new WaitForSecondsRealtime(2f);
            yield return Shot("19-run-end-defeat");

            flow.QuitToHome();
            yield return Tab(menus.Home, 3, "20-home-records-after-a-run");

            // Another level: Home loads its scene and the run starts there.
            if (LevelList.CanLoad(LevelList.Find("KitchenFloor")))
            {
                yield return Tab(menus.Home, 0, "21-home-level-row");
                menus.Home.LevelStepper.Step(1);
                yield return Shot("22-home-kitchen-chosen");
                Assert.AreEqual("KitchenFloor", menus.Home.SelectedLevelId);
                menus.Home.StartButton.Clicked.Invoke();
                yield return new WaitForSecondsRealtime(1.5f);

                flow = Object.FindAnyObjectByType<RunFlow>();
                Assert.AreEqual("KitchenFloor", flow.MapId, "the kitchen scene is the one loaded");
                Assert.AreEqual(GameFlowState.Playing, flow.State, "and its run has started");
                Object.FindAnyObjectByType<EnemySpawner>().StartNextWave();
                yield return new WaitForSeconds(3f);
                yield return Shot("23-kitchen-playing");
                flow.Pause();
                yield return Shot("24-kitchen-paused");
            }

            SaveStore.Close();
            Debug.Log($"[MenuShots] Pictures saved in {_dir}");
        }

        private IEnumerator Tab(Component screen, int index, string name)
        {
            screen.GetComponentInChildren<UiTabBar>(true).Select(index);
            yield return Shot(name);
        }

        // Each shot loads a scene in Single mode and attaches a throwaway save, and Unity runs this
        // fixture in a full assembly run despite Explicit. Leave an empty scene and no save behind,
        // or every fixture after this one plays inside the Kitchen Floor.
        [UnityTearDown]
        public IEnumerator LeaveAnEmptyScene()
        {
            SaveStore.Close();
            Scene empty = SceneManager.CreateScene("ShotsTeardown");
            SceneManager.SetActiveScene(empty);
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene != empty && scene.isLoaded)
                {
                    yield return SceneManager.UnloadSceneAsync(scene);
                }
            }
        }

        private IEnumerator Shot(string name)
        {
            // Two frames: one for the screen to open and focus, one for its layout to settle.
            yield return null;
            yield return null;

            // Written at the end of the frame it is asked for in.
            ScreenCapture.CaptureScreenshot(Path.Combine(_dir, name + ".png"));
            yield return null;
            yield return null;
        }
    }
}
#endif
