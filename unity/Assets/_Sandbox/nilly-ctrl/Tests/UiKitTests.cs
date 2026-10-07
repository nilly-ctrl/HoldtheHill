#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using HoldTheHill.Sandbox.UiKit;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace HoldTheHill.Sandbox.Graybox.Tests
{
    /// <summary>
    /// Checks the UI kit: each widget's value handling, the screen stack and its focus rules,
    /// keyboard/gamepad navigation links, and that the generated prefabs are wired up.
    /// </summary>
    /// <remarks>
    /// Uses the generated prefabs in UiKit/Prefabs, so run Tools > Hold the Hill > Build UI Kit
    /// first if they are missing. Navigation is driven by sending uGUI move events directly;
    /// real device input is covered by playing the gallery scene.
    /// </remarks>
    public class UiKitTests
    {
        private const string PrefabDir = "Assets/_Sandbox/nilly-ctrl/UiKit/Prefabs/";

        private readonly List<GameObject> _made = new List<GameObject>();
        private readonly List<UiCue> _cues = new List<UiCue>();
        private Transform _canvas;
        private EventSystem _events;

        [SetUp]
        public void SetUp()
        {
            _events = Spawn("UiEventSystem", null).GetComponent<EventSystem>();
            _canvas = Spawn("UiRoot", null).transform;
            UiFeedback.Cue += _cues.Add;
        }

        [TearDown]
        public void TearDown()
        {
            UiFeedback.Cue -= _cues.Add;
            foreach (GameObject go in _made)
            {
                if (go != null) Object.DestroyImmediate(go);
            }

            _made.Clear();
            _cues.Clear();
        }

        // ---------- scaling ----------

        [TestCase(640, 360, 0, 1)]
        [TestCase(809, 508, 0, 1)]
        [TestCase(1280, 720, 0, 2)]
        [TestCase(1920, 1080, 0, 3)]
        [TestCase(2560, 1440, 0, 4)]
        [TestCase(3840, 2160, 0, 6)]
        [TestCase(1920, 1200, 0, 3)]
        [TestCase(320, 200, 0, 1)]
        [TestCase(1920, 1080, 1, 4)]
        [TestCase(1280, 720, -5, 1)]
        public void PixelScale_IsTheLargestWholeNumberThatFits(int width, int height, int offset, int expected)
        {
            Assert.AreEqual(expected, UiPixelCanvasScaler.ComputeScale(width, height, offset));
        }

        // ---------- widgets ----------

        [UnityTest]
        public IEnumerator Slider_MovesInSteps_AndOnlyReportsPlayerChanges()
        {
            var field = Spawn("SliderField", _canvas).GetComponent<UiSliderField>();
            yield return null;
            float reported = -1f;
            field.ValueChanged.AddListener(v => reported = v);

            field.Value = 0.63f;
            Assert.AreEqual(0.65f, field.Value, 0.0001f, "snaps to the nearest 5%");
            Assert.AreEqual(-1f, reported, "setting from code is silent");
            Assert.AreEqual("65%", field.transform.Find("Value").GetComponent<TMPro.TMP_Text>().text);
            CollectionAssert.DoesNotContain(_cues, UiCue.Change);

            // One press to the right, as the UI module would send it.
            Move(field.Slider.gameObject, MoveDirection.Right);
            Assert.AreEqual(0.70f, field.Value, 0.0001f);
            Assert.AreEqual(0.70f, reported, 0.0001f);
            CollectionAssert.Contains(_cues, UiCue.Change);

            field.Configure(0f, 10f, 10, UiSliderField.ValueFormat.Number);
            field.Value = 7f;
            Assert.AreEqual("7", field.transform.Find("Value").GetComponent<TMPro.TMP_Text>().text);
        }

        [UnityTest]
        public IEnumerator Toggle_OnlyReportsPlayerChanges()
        {
            var field = Spawn("ToggleField", _canvas).GetComponent<UiToggleField>();
            yield return null;
            int reports = 0;
            field.ValueChanged.AddListener(_ => reports++);

            field.IsOn = false;
            Assert.IsFalse(field.IsOn);
            Assert.AreEqual(0, reports);

            ExecuteEvents.Execute(field.gameObject, new BaseEventData(_events), ExecuteEvents.submitHandler);
            Assert.IsTrue(field.IsOn);
            Assert.AreEqual(1, reports);
        }

        [UnityTest]
        public IEnumerator Stepper_LeftAndRightChangeTheChoice_UpAndDownDoNot()
        {
            var stepper = Spawn("Stepper", _canvas).GetComponent<UiStepper>();
            yield return null;
            int reported = -1;
            stepper.IndexChanged.AddListener(i => reported = i);

            stepper.SetOptions(new[] { "1x", "2x", "4x" }, 0);
            Assert.AreEqual("1x", stepper.Current);
            Assert.AreEqual(-1, reported, "setting from code is silent");

            Move(stepper.gameObject, MoveDirection.Right);
            Assert.AreEqual("2x", stepper.Current);
            Assert.AreEqual(1, reported);

            Move(stepper.gameObject, MoveDirection.Left);
            Move(stepper.gameObject, MoveDirection.Left);
            Assert.AreEqual("4x", stepper.Current, "wraps past the start");

            Move(stepper.gameObject, MoveDirection.Up);
            Move(stepper.gameObject, MoveDirection.Down);
            Assert.AreEqual("4x", stepper.Current);

            stepper.transform.Find("Choice/Next").GetComponent<Button>().onClick.Invoke();
            Assert.AreEqual("1x", stepper.Current, "the arrows work for the mouse");
        }

        [UnityTest]
        public IEnumerator TabBar_ShowsOnePageAtATime_AndWraps()
        {
            var bar = Spawn("TabBar", _canvas).GetComponent<UiTabBar>();
            var pages = new List<GameObject>();
            for (int i = 0; i < 3; i++)
            {
                var page = new GameObject($"Page{i}", typeof(RectTransform));
                page.transform.SetParent(_canvas, false);
                pages.Add(page);
                bar.AddTab(Spawn("TabButton", bar.transform).GetComponent<UiButton>(), page);
            }

            yield return null;
            int reported = -1;
            bar.TabChanged.AddListener(i => reported = i);

            Assert.AreEqual(0, bar.SelectedIndex);
            Assert.IsTrue(pages[0].activeSelf);
            Assert.IsFalse(pages[1].activeSelf);

            bar.Select(bar.SelectedIndex - 1);
            Assert.AreEqual(2, bar.SelectedIndex, "previous from the first tab is the last");
            Assert.IsTrue(pages[2].activeSelf);
            Assert.IsFalse(pages[0].activeSelf);
            Assert.AreEqual(2, reported);

            // Clicking a tab selects it.
            bar.transform.GetChild(1).GetComponent<Button>().onClick.Invoke();
            Assert.AreEqual(1, bar.SelectedIndex);
            Assert.IsTrue(pages[1].activeSelf);
            Assert.AreSame(bar.transform.GetChild(1).GetComponent<Button>(), bar.SelectedButton);
        }

        // ---------- screens ----------

        [UnityTest]
        public IEnumerator Screen_FocusesItsFirstControl_AndTakesFocusBackWhenItIsLost()
        {
            UiScreen screen = MakeScreen("Screen");
            UiButton first = AddButton(screen.transform, "First");
            UiButton second = AddButton(screen.transform, "Second");
            screen.FirstSelected = second.Button;
            yield return null;
            yield return null;

            Assert.AreSame(second.gameObject, _events.currentSelectedGameObject);
            CollectionAssert.DoesNotContain(_cues, UiCue.Focus, "focus set by the screen makes no sound");

            // A click on empty space clears the selection in stock uGUI.
            _events.SetSelectedGameObject(null);
            yield return null;
            Assert.AreSame(second.gameObject, _events.currentSelectedGameObject, "back to the last focused control");

            _events.SetSelectedGameObject(first.gameObject);
            yield return null;
            CollectionAssert.Contains(_cues, UiCue.Focus);
        }

        [UnityTest]
        public IEnumerator Screen_LinksRowsUpAndDown_AndSkipsDisabledControls()
        {
            UiScreen screen = MakeScreen("Screen");
            var slider = Spawn("SliderField", screen.transform).GetComponent<UiSliderField>();
            var toggle = Spawn("ToggleField", screen.transform).GetComponent<UiToggleField>();
            UiButton disabled = AddButton(screen.transform, "Disabled");
            disabled.Interactable = false;
            var row = new GameObject("Buttons", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(screen.transform, false);
            UiButton left = AddButton(row.transform, "Left");
            UiButton right = AddButton(row.transform, "Right");
            yield return null;
            yield return null;

            Navigation sliderNav = slider.Slider.navigation;
            Assert.AreEqual(Navigation.Mode.Explicit, sliderNav.mode);
            Assert.IsNull(sliderNav.selectOnUp);
            Assert.AreSame(toggle.Toggle, sliderNav.selectOnDown);
            Assert.IsNull(sliderNav.selectOnLeft, "left and right stay free to change the value");
            Assert.IsNull(sliderNav.selectOnRight);

            Selectable below = toggle.Toggle.navigation.selectOnDown;
            Assert.IsTrue(below == left.Button || below == right.Button, "the disabled button is skipped");
            Assert.AreSame(right.Button, left.Button.navigation.selectOnRight);
            Assert.AreSame(left.Button, right.Button.navigation.selectOnLeft);
            Assert.AreSame(toggle.Toggle, left.Button.navigation.selectOnUp);

            // Enabling the button puts it back in the chain.
            disabled.Interactable = true;
            yield return null;
            Assert.AreSame(disabled.Button, toggle.Toggle.navigation.selectOnDown);
        }

        [UnityTest]
        public IEnumerator Screens_Stack_OnlyTheTopTakesInput_AndFocusReturnsOnClose()
        {
            UiScreen under = MakeScreen("Under");
            UiButton opener = AddButton(under.transform, "Opener");
            yield return null;
            yield return null;
            Assert.AreSame(opener.gameObject, _events.currentSelectedGameObject);

            UiScreen over = MakeScreen("Over");
            UiButton inner = AddButton(over.transform, "Inner");
            yield return null;
            yield return null;

            Assert.AreSame(over, UiScreen.Top);
            Assert.AreEqual(2, UiScreen.OpenCount);
            Assert.IsFalse(under.GetComponent<CanvasGroup>().blocksRaycasts);
            Assert.IsTrue(over.GetComponent<CanvasGroup>().blocksRaycasts);
            Assert.AreSame(inner.gameObject, _events.currentSelectedGameObject);

            over.Back();
            yield return null;
            yield return null;

            Assert.IsFalse(over.gameObject.activeSelf, "back closes the screen");
            Assert.AreSame(under, UiScreen.Top);
            Assert.IsTrue(under.GetComponent<CanvasGroup>().blocksRaycasts);
            Assert.AreSame(opener.gameObject, _events.currentSelectedGameObject);
            CollectionAssert.Contains(_cues, UiCue.Back);
        }

        [UnityTest]
        public IEnumerator ConfirmDialog_StartsOnCancel_AndRunsTheRightCallback()
        {
            var dialog = Spawn("ConfirmDialog", _canvas).GetComponent<UiConfirmDialog>();
            Assert.IsFalse(dialog.gameObject.activeSelf, "closed until opened");
            int confirmed = 0;
            int cancelled = 0;

            dialog.Open("Quit?", "This run will be lost.", "Quit", "Stay", () => confirmed++, () => cancelled++);
            yield return null;
            yield return null;

            Assert.IsTrue(dialog.IsOpen);
            Assert.AreEqual("CancelButton", _events.currentSelectedGameObject.name, "a stray press must not confirm");
            Assert.AreEqual("Quit?", dialog.transform.Find("Panel/Title").GetComponent<TMPro.TMP_Text>().text);

            dialog.GetComponent<UiScreen>().Back();
            Assert.IsFalse(dialog.IsOpen);
            Assert.AreEqual(0, confirmed);
            Assert.AreEqual(1, cancelled, "back answers no");

            // A callback may ask the next question with the same dialog.
            bool second = false;
            dialog.Open("Sure?", "Really.", "Yes", "No", () => dialog.Open("Really sure?", "Last chance.", "Yes", "No", () => second = true));
            yield return null;
            dialog.transform.Find("Panel/Buttons/ConfirmButton").GetComponent<Button>().onClick.Invoke();
            Assert.IsTrue(dialog.IsOpen, "reopened by the callback");
            dialog.transform.Find("Panel/Buttons/ConfirmButton").GetComponent<Button>().onClick.Invoke();
            Assert.IsFalse(dialog.IsOpen);
            Assert.IsTrue(second);
        }

        // ---------- feedback ----------

        [Test]
        public void Feedback_CanBeMutedWhileCodeMovesThings()
        {
            UiFeedback.Raise(UiCue.Press);
            using (UiFeedback.Mute())
            {
                using (UiFeedback.Mute())
                {
                    UiFeedback.Raise(UiCue.Focus);
                }

                UiFeedback.Raise(UiCue.Focus);
            }

            UiFeedback.Raise(UiCue.Back);
            CollectionAssert.AreEqual(new[] { UiCue.Press, UiCue.Back }, _cues);
        }

        [UnityTest]
        public IEnumerator DisabledButton_RaisesDenied_NotPress()
        {
            UiButton button = AddButton(_canvas, "Button");
            yield return null;

            ExecuteEvents.Execute(button.gameObject, new BaseEventData(_events), ExecuteEvents.submitHandler);
            CollectionAssert.Contains(_cues, UiCue.Press);

            _cues.Clear();
            button.Interactable = false;
            ExecuteEvents.Execute(button.gameObject, new BaseEventData(_events), ExecuteEvents.submitHandler);
            CollectionAssert.AreEqual(new[] { UiCue.Denied }, _cues);
        }

        // ---------- generated prefabs ----------

        [Test]
        public void EventSystemPrefab_HasMenuActionsWired()
        {
            var module = _events.GetComponent<InputSystemUIInputModule>();

            Assert.IsNotNull(module.actionsAsset, "uses the project's input actions");
            Assert.IsNotNull(module.move, "navigate");
            Assert.IsNotNull(module.submit, "submit");
            Assert.IsNotNull(module.cancel, "cancel");
            Assert.IsNotNull(module.point, "pointer position");
            Assert.IsNotNull(module.leftClick, "click");
            Assert.AreEqual("Navigate", module.move.action.name);
        }

        [Test]
        public void WidgetPrefabs_UseThePixelFont_AndHaveAFocusRing()
        {
            foreach (string name in new[] { "Button", "TabButton", "SliderField", "ToggleField", "Stepper" })
            {
                GameObject widget = Spawn(name, _canvas);

                var focusable = widget.GetComponentInChildren<UiFocusable>(true);
                Assert.IsNotNull(focusable, $"{name}: focusable");
                Transform ring = widget.transform.Find("FocusRing");
                Assert.IsNotNull(ring, $"{name}: focus ring");
                Assert.IsFalse(ring.gameObject.activeSelf, $"{name}: ring hidden until focused");

                foreach (TMPro.TMP_Text text in widget.GetComponentsInChildren<TMPro.TMP_Text>(true))
                {
                    Assert.AreEqual("HoldTheHillPixel", text.font.name, $"{name}/{text.name}: font");
                    Assert.AreEqual(0f, text.fontSize % 10f, $"{name}/{text.name}: pixel-exact size");
                }
            }
        }

        // ---------- helpers ----------

        private GameObject Spawn(string prefabName, Transform parent)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + prefabName + ".prefab");
            Assert.IsNotNull(prefab, $"Missing {prefabName}.prefab: run Tools > Hold the Hill > Build UI Kit.");
            GameObject instance = Object.Instantiate(prefab, parent);
            instance.name = prefabName;
            if (parent == null) _made.Add(instance);
            return instance;
        }

        private UiButton AddButton(Transform parent, string name)
        {
            GameObject button = Spawn("Button", parent);
            button.name = name;
            return button.GetComponent<UiButton>();
        }

        private UiScreen MakeScreen(string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup), typeof(VerticalLayoutGroup));
            go.transform.SetParent(_canvas, false);
            return go.AddComponent<UiScreen>();
        }

        private void Move(GameObject target, MoveDirection direction)
        {
            var data = new AxisEventData(_events) { moveDir = direction };
            ExecuteEvents.Execute(target, data, ExecuteEvents.moveHandler);
        }
    }
}
#endif
