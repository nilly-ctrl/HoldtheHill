#if UNITY_EDITOR
using HoldTheHill.Core.Input;
using HoldTheHill.Core.Save;
using HoldTheHill.Features.Economy;
using HoldTheHill.Features.Hud;
using HoldTheHill.Features.Menus;
using HoldTheHill.Features.DamageNumbers;
using HoldTheHill.Features.GameFlow;
using System.Collections;
using System.IO;
using System.Reflection;
using HoldTheHill.Core.UiKit;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace HoldTheHill.Tests
{
    /// <summary>
    /// Title, home, settings and controls: the second half of <see cref="MenusTests"/>,
    /// sharing its set-up (a spawner with three waves, a hill, the flow controller and the menus
    /// prefab).
    /// </summary>
    public partial class MenusTests
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
            Assert.AreEqual(RunMode.Campaign, _flow.Mode);
            Assert.AreEqual(3, _flow.Stats.TotalWaves);
            Assert.AreEqual(0, UiScreen.OpenCount);

            _flow.QuitToHome();
            yield return null;
            _menus.Home.Mode.Step(1);
            _menus.Home.StartButton.Clicked.Invoke();
            yield return null;
            Assert.AreEqual(RunMode.Endless, _flow.Mode);
            Assert.AreEqual(0, _flow.Stats.TotalWaves, "an endless run has no last wave");

            _flow.ReportEndlessWave(4);
            Assert.AreEqual(4, _flow.Stats.WaveReached);
            Assert.AreEqual(_flow.LevelIdFor(RunMode.Campaign) + "-Endless", _flow.LevelId);
        }

        [UnityTest]
        public IEnumerator Home_BuyingAnUpgrade_SpendsHoneydew_AndTheNextRunHasIt()
        {
            yield return null;
            float builtHealth = _hill.MaxHealth;
            int builtFood = UnityEngine.Object.FindAnyObjectByType<WalletBehaviour>().CurrentFood;

            _flow.QuitToHome();
            yield return null;

            HomeScreen.UpgradeRow hillRow = FindUpgrade(PermanentUpgrades.TougherHill);
            HomeScreen.UpgradeRow foodRow = FindUpgrade(PermanentUpgrades.StartingFood);
            Assert.IsFalse(hillRow.Buy.Interactable, "nothing to spend yet");
            StringAssert.Contains("0 " + PermanentUpgrades.CurrencyName, _menus.Home.Honeydew.text);

            MetaProgress.AddCurrency(7);
            yield return null;
            Assert.IsTrue(hillRow.Buy.Interactable);
            StringAssert.Contains("0/3", hillRow.Name.text);

            hillRow.Buy.Clicked.Invoke();
            foodRow.Buy.Clicked.Invoke();
            yield return null;
            Assert.AreEqual(1, MetaProgress.Currency, "7 less two level-1 upgrades at 3 each");
            StringAssert.Contains("1/3", hillRow.Name.text);
            Assert.IsFalse(hillRow.Buy.Interactable, "level 2 costs more than is left");

            _menus.Home.StartButton.Clicked.Invoke();
            yield return null;
            Assert.AreEqual(builtHealth + 10f, _hill.MaxHealth);
            Assert.AreEqual(_hill.MaxHealth, _hill.CurrentHealth);
            Assert.AreEqual(builtFood + 50, UnityEngine.Object.FindAnyObjectByType<WalletBehaviour>().CurrentFood);

            // A restart hands the bonus out once, not on top of the last one.
            _flow.StartNewRun();
            yield return null;
            Assert.AreEqual(builtFood + 50, UnityEngine.Object.FindAnyObjectByType<WalletBehaviour>().CurrentFood);
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

            _hill.TakeDamage(_hill.MaxHealth, Vector3.zero);
            yield return null;
            Assert.AreEqual(1, MetaProgress.Currency);
            StringAssert.Contains("1", _menus.RunEnd.Honeydew.text);

            _menus.RunEnd.RetryWaveButton.Clicked.Invoke();
            yield return null;
            _spawner.StartNextWave();
            yield return null;
            _hill.TakeDamage(_hill.MaxHealth, Vector3.zero);
            yield return null;
            Assert.AreEqual(1, MetaProgress.Currency, "the same wave lost again pays nothing more");

            // A win pays every wave plus the bonus.
            Assert.AreEqual(3 * PermanentUpgrades.HoneydewPerWave + PermanentUpgrades.HoneydewForVictory, PermanentUpgrades.HoneydewFor(3, true));
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
            SaveStore.Data.records.levels.Add(new LevelRecord { levelId = "Graybox", bestWave = 7 });
            int graybox = -1;
            for (int i = 0; i < _menus.Home.LevelStepper.Options.Count; i++)
            {
                if (_menus.Home.LevelStepper.Options[i] == LevelList.Find("Graybox").Name) graybox = i;
            }

            Assert.GreaterOrEqual(graybox, 0, "the graybox scene exists, so it is offered");
            for (int i = 0; i < graybox; i++) _menus.Home.LevelStepper.Step(1);
            Assert.AreEqual("Graybox", _menus.Home.SelectedLevelId);
            StringAssert.Contains("wave 7", _menus.Home.Best.text);
            Assert.AreEqual("wave 7", _menus.Home.BestCampaign.text);

            Assert.AreEqual("Graybox-Endless", LevelList.RecordId("Graybox", RunMode.Endless));
            Assert.IsNull(LevelList.TakePendingRun(), "nothing asked for a run in another scene");
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

            SettingsScreen settings = _menus.Settings;
            Assert.IsTrue(settings.HealthBars.IsOn);

            settings.HealthBars.Toggle.isOn = false;
            settings.Mute.Toggle.isOn = true;
            settings.AutoStart.Toggle.isOn = true;
            Assert.IsFalse(EnemyHealthBar.ShowHealthBars, "applied without waiting for the screen to close");
            Assert.AreEqual(0f, AudioListener.volume);
            Assert.IsTrue(GameSettings.AutoStartWaves);
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
            Assert.AreEqual(1, SaveStore.Data.settings.uiScaleOffset);
        }

        [UnityTest]
        public IEnumerator Settings_AutoStart_SendsTheNextWaveWithoutTheKey()
        {
            typeof(RunFlow).GetField("_autoStartDelay", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_flow, 0.02f);
            SaveStore.Data.settings.autoStartWaves = true;
            GameSettings.Apply();
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

            ControlsScreen controls = _menus.Controls;
            Assert.AreEqual(GameControls.Entries.Count, controls.Rows.Count);
            Assert.AreEqual(30, controls.Rows.Count, "11 actions, 9 Warden actions and 10 build slots");

            Assert.AreEqual(PixelGlyphs.KeyN, controls.FindRow(GameControls.NextWave).Button.Label);
            Assert.AreEqual(PixelGlyphs.KeyEsc, controls.FindRow(GameControls.Pause).Button.Label);
            Assert.AreEqual(PixelGlyphs.KeyF6, controls.FindRow(GameControls.FontTheme).Button.Label);
            Assert.AreEqual(PixelGlyphs.Key0, controls.FindRow(GameControls.BuildId(9)).Button.Label);

            // On a gamepad only Pause has a button.
            controls.ShowGroup(GameControls.GamepadGroup);
            Assert.AreEqual(PixelGlyphs.PadStart, controls.FindRow(GameControls.Pause).Button.Label);
            Assert.IsFalse(controls.FindRow(GameControls.NextWave).Button.Interactable);
            Assert.AreEqual(BindingText.None, controls.FindRow(GameControls.NextWave).Button.Label);
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

            ControlsScreen controls = _menus.Controls;
            ControlsScreen.Row nextWave = controls.FindRow(GameControls.NextWave);
            Color plain = nextWave.Label.color;

            GameControls.SetBinding(GameControls.NextWave, GameControls.KeyboardGroup, "<Keyboard>/g");
            Assert.AreEqual(PixelGlyphs.KeyG, nextWave.Button.Label);
            Assert.AreEqual(plain, nextWave.Label.color);
            StringAssert.Contains("<Keyboard>/g", File.ReadAllText(_saveFile));

            // The same key on two actions is allowed, and both rows say so.
            GameControls.SetBinding(GameControls.Speed, GameControls.KeyboardGroup, "<Keyboard>/g");
            Assert.AreEqual(1, GameControls.Conflicts(GameControls.Speed, GameControls.KeyboardGroup).Count);
            Assert.AreNotEqual(plain, nextWave.Label.color);
            Assert.AreNotEqual(plain, controls.FindRow(GameControls.Speed).Label.color);

            // A saved file brings the bindings back.
            SaveStore.Close();
            GameControls.ResetToDefaults();
            Assert.AreEqual(PixelGlyphs.KeyN, nextWave.Button.Label);
            SaveStore.Open(_saveFile);
            Assert.AreEqual("<Keyboard>/g", GameControls.Path(GameControls.NextWave, GameControls.KeyboardGroup));
            Assert.AreEqual(PixelGlyphs.KeyG, nextWave.Button.Label);

            controls.ResetButton.Clicked.Invoke();
            Assert.AreEqual(PixelGlyphs.KeyN, nextWave.Button.Label);
            Assert.AreEqual(plain, nextWave.Label.color);
            Assert.AreEqual("<Keyboard>/t", GameControls.Path(GameControls.Speed, GameControls.KeyboardGroup));
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
            GameControls.SetBinding(GameControls.Pause, GameControls.KeyboardGroup, "<Keyboard>/p");
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

        private HomeScreen.UpgradeRow FindUpgrade(string id)
        {
            foreach (HomeScreen.UpgradeRow row in _menus.Home.Upgrades)
            {
                if (row.Id == id) return row;
            }

            Assert.Fail($"The home screen has no row for upgrade '{id}'.");
            return null;
        }

        private void OpenTempSave()
        {
            _saveFile = Path.Combine(Application.temporaryCachePath, "MenusTests_" + System.Guid.NewGuid().ToString("N") + ".json");
            SaveStore.Open(_saveFile);
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
