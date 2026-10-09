#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Reflection;
using HoldTheHill.Sandbox.NillyCtrl;
using HoldTheHill.Sandbox.UiKit;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace HoldTheHill.Sandbox.Graybox.Tests
{
    /// <summary>
    /// Title, home, settings and controls: the second half of <see cref="GrayboxMenusTests"/>,
    /// sharing its set-up (a spawner with three waves, a hill, the flow controller and the menus
    /// prefab).
    /// </summary>
    public partial class GrayboxMenusTests
    {
        private string _saveFile;
        private Keyboard _keyboard;
        private bool _inputSettingsChanged;
        private InputSettings.BackgroundBehavior _backgroundBehavior;
        private InputSettings.EditorInputBehaviorInPlayMode _editorInputBehavior;

        // ---------- title and home ----------

        [UnityTest]
        public IEnumerator Title_PlayGoesHome_AndBackFromHomeReturns()
        {
            yield return null;
            _flow.GoToTitle();
            yield return null;
            yield return null;

            Assert.IsTrue(_menus.Title.Screen.IsTop);
            Assert.AreSame(_menus.Title.PlayButton.gameObject, _events.currentSelectedGameObject);

            // Back has nowhere to go from the first screen.
            _menus.Title.Screen.Back();
            yield return null;
            Assert.IsTrue(_menus.Title.Screen.IsTop);

            _menus.Title.PlayButton.Clicked.Invoke();
            yield return null;
            Assert.AreEqual(GameFlowState.Home, _flow.State);
            Assert.IsTrue(_menus.Home.Screen.IsTop);
            Assert.IsFalse(_menus.Title.gameObject.activeSelf);

            _menus.Home.Screen.Back();
            yield return null;
            Assert.AreEqual(GameFlowState.Title, _flow.State);
            Assert.IsTrue(_menus.Title.Screen.IsTop);
        }

        [UnityTest]
        public IEnumerator Title_SettingsAndQuit_OpenOverIt()
        {
            yield return null;
            _flow.GoToTitle();
            yield return null;

            _menus.Title.SettingsButton.Clicked.Invoke();
            yield return null;
            Assert.IsTrue(_menus.Settings.Screen.IsTop);
            _menus.Settings.DoneButton.Clicked.Invoke();
            yield return null;
            Assert.IsTrue(_menus.Title.Screen.IsTop);

            _menus.Title.QuitButton.Clicked.Invoke();
            yield return null;
            Assert.IsTrue(_menus.Dialog.IsOpen, "quitting asks first");
            _menus.Dialog.Dismiss();
        }

        [UnityTest]
        public IEnumerator Home_StartsACampaignOrAnEndlessRun()
        {
            yield return null;
            _flow.QuitToHome();
            yield return null;

            _menus.Home.StartButton.Clicked.Invoke();
            yield return null;
            Assert.AreEqual(GameFlowState.Playing, _flow.State);
            Assert.AreEqual(GrayboxRunMode.Campaign, _flow.Mode);
            Assert.AreEqual(3, _flow.Stats.TotalWaves);
            Assert.AreEqual(0, UiScreen.OpenCount);

            _flow.QuitToHome();
            yield return null;
            _menus.Home.Mode.Step(1);
            _menus.Home.StartButton.Clicked.Invoke();
            yield return null;
            Assert.AreEqual(GrayboxRunMode.Endless, _flow.Mode);
            Assert.AreEqual(0, _flow.Stats.TotalWaves, "an endless run has no last wave");

            _flow.ReportEndlessWave(4);
            Assert.AreEqual(4, _flow.Stats.WaveReached);
            Assert.AreEqual(_flow.LevelIdFor(GrayboxRunMode.Campaign) + "-Endless", _flow.LevelId);
        }

        [UnityTest]
        public IEnumerator Home_BuyingAnUpgrade_SpendsHoneydew_AndTheNextRunHasIt()
        {
            yield return null;
            float builtHealth = _hill.MaxHealth;
            int builtFood = GrayboxEconomy.Instance.CurrentGold;

            _flow.QuitToHome();
            yield return null;

            GrayboxHomeScreen.UpgradeRow hillRow = FindUpgrade(GrayboxUpgrades.TougherHill);
            GrayboxHomeScreen.UpgradeRow foodRow = FindUpgrade(GrayboxUpgrades.StartingFood);
            Assert.IsFalse(hillRow.Buy.Interactable, "nothing to spend yet");
            StringAssert.Contains("0 " + GrayboxUpgrades.CurrencyName, _menus.Home.Honeydew.text);

            GrayboxMetaProgress.AddCurrency(7);
            yield return null;
            Assert.IsTrue(hillRow.Buy.Interactable);
            StringAssert.Contains("0/3", hillRow.Name.text);

            hillRow.Buy.Clicked.Invoke();
            foodRow.Buy.Clicked.Invoke();
            yield return null;
            Assert.AreEqual(1, GrayboxMetaProgress.Currency, "7 less two level-1 upgrades at 3 each");
            StringAssert.Contains("1/3", hillRow.Name.text);
            Assert.IsFalse(hillRow.Buy.Interactable, "level 2 costs more than is left");

            _menus.Home.StartButton.Clicked.Invoke();
            yield return null;
            Assert.AreEqual(builtHealth + 10f, _hill.MaxHealth);
            Assert.AreEqual(_hill.MaxHealth, _hill.CurrentHealth);
            Assert.AreEqual(builtFood + 50, GrayboxEconomy.Instance.CurrentGold);

            // A restart hands the bonus out once, not on top of the last one.
            _flow.StartNewRun();
            yield return null;
            Assert.AreEqual(builtFood + 50, GrayboxEconomy.Instance.CurrentGold);
            Assert.AreEqual(builtHealth + 10f, _hill.MaxHealth);
        }

        [UnityTest]
        public IEnumerator ARun_PaysHoneydewPerWaveCleared_AndNotTwiceAfterARetry()
        {
            yield return null;
            _spawner.StartNextWave();
            yield return null;
            _spawner.StartNextWave(); // wave 2: one wave cleared
            yield return null;

            _hill.TakeBaseDamage(_hill.MaxHealth, Vector3.zero);
            yield return null;
            Assert.AreEqual(1, GrayboxMetaProgress.Currency);
            StringAssert.Contains("1", _menus.RunEnd.Honeydew.text);

            _menus.RunEnd.RetryWaveButton.Clicked.Invoke();
            yield return null;
            _spawner.StartNextWave();
            yield return null;
            _hill.TakeBaseDamage(_hill.MaxHealth, Vector3.zero);
            yield return null;
            Assert.AreEqual(1, GrayboxMetaProgress.Currency, "the same wave lost again pays nothing more");

            // A win pays every wave plus the bonus.
            Assert.AreEqual(3 * GrayboxUpgrades.HoneydewPerWave + GrayboxUpgrades.HoneydewForVictory, GrayboxUpgrades.HoneydewFor(3, true));
        }

        [UnityTest]
        public IEnumerator Home_ListsTheLevels_WithThisSceneChosen()
        {
            yield return null;
            _flow.QuitToHome();
            yield return null;

            // The test scene is not in the catalogue, so it is listed under its map id, first.
            Assert.AreEqual(_flow.MapId, _menus.Home.SelectedLevelId);
            Assert.AreEqual(_flow.MapId, _menus.Home.LevelStepper.Current);
            Assert.GreaterOrEqual(_menus.Home.LevelStepper.Options.Count, 2, "this scene and the graybox");

            // Records follow the level chosen, not the scene loaded.
            GrayboxSave.Data.records.levels.Add(new LevelRecord { levelId = "Graybox", bestWave = 7 });
            int graybox = -1;
            for (int i = 0; i < _menus.Home.LevelStepper.Options.Count; i++)
            {
                if (_menus.Home.LevelStepper.Options[i] == GrayboxLevels.Find("Graybox").Name) graybox = i;
            }

            Assert.GreaterOrEqual(graybox, 0, "the graybox scene exists, so it is offered");
            for (int i = 0; i < graybox; i++) _menus.Home.LevelStepper.Step(1);
            Assert.AreEqual("Graybox", _menus.Home.SelectedLevelId);
            StringAssert.Contains("wave 7", _menus.Home.Best.text);
            Assert.AreEqual("wave 7", _menus.Home.BestCampaign.text);

            Assert.AreEqual("Graybox-Endless", GrayboxLevels.RecordId("Graybox", GrayboxRunMode.Endless));
            Assert.IsNull(GrayboxLevels.TakePendingRun(), "nothing asked for a run in another scene");
        }

        // ---------- settings ----------

        [UnityTest]
        public IEnumerator Settings_ApplyAtOnce_AndAreWrittenWhenTheScreenCloses()
        {
            OpenTempSave();
            yield return null;
            _flow.Pause();
            yield return null;
            _menus.Pause.SettingsButton.Clicked.Invoke();
            yield return null;

            GrayboxSettingsScreen settings = _menus.Settings;
            Assert.IsTrue(settings.HealthBars.IsOn);

            settings.HealthBars.Toggle.isOn = false;
            settings.Mute.Toggle.isOn = true;
            settings.AutoStart.Toggle.isOn = true;
            Assert.IsFalse(EnemyHealthBar.ShowHealthBars, "applied without waiting for the screen to close");
            Assert.AreEqual(0f, AudioListener.volume);
            Assert.IsTrue(GrayboxSettings.AutoStartWaves);
            Assert.IsFalse(File.Exists(_saveFile) && File.ReadAllText(_saveFile).Contains("\"showHealthBars\": false"), "not written yet");

            settings.DoneButton.Clicked.Invoke();
            yield return null;
            Assert.IsTrue(_menus.Pause.Screen.IsTop);
            StringAssert.Contains("\"showHealthBars\": false", File.ReadAllText(_saveFile));
            StringAssert.Contains("\"muted\": true", File.ReadAllText(_saveFile));

            // Reopened, the controls show what was saved; Defaults puts everything back.
            _menus.Pause.SettingsButton.Clicked.Invoke();
            yield return null;
            Assert.IsFalse(settings.HealthBars.IsOn);
            settings.DefaultsButton.Clicked.Invoke();
            Assert.IsTrue(settings.HealthBars.IsOn);
            Assert.IsFalse(settings.Mute.IsOn);
            Assert.IsTrue(EnemyHealthBar.ShowHealthBars);
            Assert.AreEqual(1f, AudioListener.volume);
        }

        [UnityTest]
        public IEnumerator Settings_MenuSize_ScalesTheCanvas()
        {
            yield return null;
            _flow.Pause();
            yield return null;
            _menus.Pause.SettingsButton.Clicked.Invoke();
            yield return null;

            var scaler = _menus.GetComponent<UiPixelCanvasScaler>();
            Assert.AreEqual(0, scaler.ScaleOffset);
            Assert.AreEqual("Normal", _menus.Settings.UiSize.Current);

            _menus.Settings.UiSize.Step(1);
            Assert.AreEqual("Large", _menus.Settings.UiSize.Current);
            Assert.AreEqual(1, scaler.ScaleOffset);
            Assert.AreEqual(1, GrayboxSave.Data.settings.uiScaleOffset);
        }

        [UnityTest]
        public IEnumerator Settings_AutoStart_SendsTheNextWaveWithoutTheKey()
        {
            typeof(GrayboxGameFlow).GetField("_autoStartDelay", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_flow, 0.02f);
            GrayboxSave.Data.settings.autoStartWaves = true;
            GrayboxSettings.Apply();
            yield return null;

            float giveUp = Time.realtimeSinceStartup + 5f;
            while (_flow.Stats.WaveReached < 1 && Time.realtimeSinceStartup < giveUp) yield return null;
            Assert.GreaterOrEqual(_flow.Stats.WaveReached, 1, "wave 1 started by itself");
        }

        // ---------- controls ----------

        [UnityTest]
        public IEnumerator Controls_ListsEveryAction_WithItsKey()
        {
            yield return null;
            _flow.Pause();
            yield return null;
            _menus.Pause.ControlsButton.Clicked.Invoke();
            yield return null;

            GrayboxControlsScreen controls = _menus.Controls;
            Assert.AreEqual(GrayboxControls.Entries.Count, controls.Rows.Count);
            Assert.AreEqual(20, controls.Rows.Count, "10 actions and 10 build slots");

            Assert.AreEqual(PixelGlyphs.KeyN, controls.FindRow(GrayboxControls.NextWave).Button.Label);
            Assert.AreEqual(PixelGlyphs.KeyEsc, controls.FindRow(GrayboxControls.Pause).Button.Label);
            Assert.AreEqual(PixelGlyphs.KeyF6, controls.FindRow(GrayboxControls.FontTheme).Button.Label);
            Assert.AreEqual(PixelGlyphs.Key0, controls.FindRow(GrayboxControls.BuildId(9)).Button.Label);

            // On a gamepad only Pause has a button.
            controls.ShowGroup(GrayboxControls.GamepadGroup);
            Assert.AreEqual(PixelGlyphs.PadStart, controls.FindRow(GrayboxControls.Pause).Button.Label);
            Assert.IsFalse(controls.FindRow(GrayboxControls.NextWave).Button.Interactable);
            Assert.AreEqual(GrayboxBindingText.None, controls.FindRow(GrayboxControls.NextWave).Button.Label);
        }

        [UnityTest]
        public IEnumerator Controls_ARebindIsShown_Saved_AndFlaggedWhenItClashes()
        {
            OpenTempSave();
            yield return null;
            _flow.Pause();
            yield return null;
            _menus.Pause.ControlsButton.Clicked.Invoke();
            yield return null;

            GrayboxControlsScreen controls = _menus.Controls;
            GrayboxControlsScreen.Row nextWave = controls.FindRow(GrayboxControls.NextWave);
            Color plain = nextWave.Label.color;

            GrayboxControls.SetBinding(GrayboxControls.NextWave, GrayboxControls.KeyboardGroup, "<Keyboard>/g");
            Assert.AreEqual(PixelGlyphs.KeyG, nextWave.Button.Label);
            Assert.AreEqual(plain, nextWave.Label.color);
            StringAssert.Contains("<Keyboard>/g", File.ReadAllText(_saveFile));

            // The same key on two actions is allowed, and both rows say so.
            GrayboxControls.SetBinding(GrayboxControls.Speed, GrayboxControls.KeyboardGroup, "<Keyboard>/g");
            Assert.AreEqual(1, GrayboxControls.Conflicts(GrayboxControls.Speed, GrayboxControls.KeyboardGroup).Count);
            Assert.AreNotEqual(plain, nextWave.Label.color);
            Assert.AreNotEqual(plain, controls.FindRow(GrayboxControls.Speed).Label.color);

            // A saved file brings the bindings back.
            GrayboxSave.Close();
            GrayboxControls.ResetToDefaults();
            Assert.AreEqual(PixelGlyphs.KeyN, nextWave.Button.Label);
            GrayboxSave.Open(_saveFile);
            Assert.AreEqual("<Keyboard>/g", GrayboxControls.Path(GrayboxControls.NextWave, GrayboxControls.KeyboardGroup));
            Assert.AreEqual(PixelGlyphs.KeyG, nextWave.Button.Label);

            controls.ResetButton.Clicked.Invoke();
            Assert.AreEqual(PixelGlyphs.KeyN, nextWave.Button.Label);
            Assert.AreEqual(plain, nextWave.Label.color);
            Assert.AreEqual("<Keyboard>/t", GrayboxControls.Path(GrayboxControls.Speed, GrayboxControls.KeyboardGroup));
        }

        [UnityTest]
        public IEnumerator Controls_TheGameListensOnTheNewKey()
        {
            // A batch run has no focused Game view, and the editor drops input without one.
            InputSettings input = InputSystem.settings;
            _backgroundBehavior = input.backgroundBehavior;
            _editorInputBehavior = input.editorInputBehaviorInPlayMode;
            _inputSettingsChanged = true;
            input.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            input.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;

            _keyboard = InputSystem.AddDevice<Keyboard>();
            GrayboxControls.SetBinding(GrayboxControls.Pause, GrayboxControls.KeyboardGroup, "<Keyboard>/p");
            yield return null;
            Assert.AreEqual(GameFlowState.Playing, _flow.State);

            // The old key no longer pauses.
            yield return Tap(Key.Escape);
            Assert.AreEqual(GameFlowState.Playing, _flow.State);

            yield return Tap(Key.P);
            Assert.AreEqual(GameFlowState.Paused, _flow.State);
            Assert.IsTrue(_menus.Pause.Screen.IsTop);
        }

        // ---------- helpers ----------

        private GrayboxHomeScreen.UpgradeRow FindUpgrade(string id)
        {
            foreach (GrayboxHomeScreen.UpgradeRow row in _menus.Home.Upgrades)
            {
                if (row.Id == id) return row;
            }

            Assert.Fail($"The home screen has no row for upgrade '{id}'.");
            return null;
        }

        private void OpenTempSave()
        {
            _saveFile = Path.Combine(Application.temporaryCachePath, "GrayboxMenusTests_" + System.Guid.NewGuid().ToString("N") + ".json");
            GrayboxSave.Open(_saveFile);
        }

        // Presses and releases a key on the test keyboard, a frame each, so scripts see it in Update.
        private IEnumerator Tap(Key key)
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(key));
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            yield return null;
            yield return null;
        }
    }
}
#endif
