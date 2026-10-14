#if UNITY_EDITOR
using HoldTheHill.Core.Save;
using HoldTheHill.Features.Hud;
using HoldTheHill.Features.Menus;
using HoldTheHill.Features.Towers;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using HoldTheHill.Features.GameFlow;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace HoldTheHill.Tests
{
    /// <summary>
    /// A real pointer click on the HUD in the Kitchen Floor scene: the event system's raycast must
    /// land on a build slot, and a click there must pick that tower. Fails with the full raycast
    /// list when something else sits over the HUD.
    /// </summary>
    public class HudClickTests
    {
        private const string KitchenPath = "Assets/_Game/Scenes/Levels/KitchenFloor/KitchenFloor.unity";

        private InputSettings.BackgroundBehavior _backgroundBehavior;
        private InputSettings.EditorInputBehaviorInPlayMode _editorInputBehavior;
        private bool _inputSettingsChanged;

        [UnityTest]
        public IEnumerator Kitchen_AClickOnTheFirstBuildSlot_PicksThatTower()
        {
            // A batch run has no focused Game view, and the editor drops input without one.
            InputSettings input = InputSystem.settings;
            _backgroundBehavior = input.backgroundBehavior;
            _editorInputBehavior = input.editorInputBehaviorInPlayMode;
            _inputSettingsChanged = true;
            input.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            input.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;

            string saveFile = Path.Combine(Application.temporaryCachePath, "hud-click-save.json");
            if (File.Exists(saveFile)) File.Delete(saveFile);
            SaveStore.Open(saveFile);

            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(KitchenPath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;

            var menus = Object.FindAnyObjectByType<MenuScreens>();
            Assert.IsNotNull(menus, "the kitchen scene has no menus");

            // Control: the same mouse-driven click on the title screen's Play button. If this one
            // fails too, the harness cannot deliver pointer input here and the HUD is not to blame.
            var flowAtTitle = Object.FindAnyObjectByType<RunFlow>();
            Assert.AreEqual(GameFlowState.Title, flowAtTitle.State, "the scene did not open on the title screen");
            Vector2 playAt = ScreenPointOf((RectTransform)menus.Title.PlayButton.transform);
            Debug.Log("[HudClick] control: Play button at " + playAt + " on " + Screen.width + "x" + Screen.height);
            yield return VirtualClick(playAt);
            Assert.AreEqual(GameFlowState.Home, flowAtTitle.State, "control failed: a mouse click on the title screen's Play button at " + playAt + " did not reach it, so pointer input does not work in this run at all");

            menus.Home.StartButton.Clicked.Invoke();
            yield return null;
            yield return null;

            var flow = Object.FindAnyObjectByType<RunFlow>();
            Assert.IsNotNull(flow, "no flow in the scene");
            Assert.AreEqual(GameFlowState.Playing, flow.State, "the run did not start");

            EventSystem events = EventSystem.current;
            Assert.IsNotNull(events, "no EventSystem in the scene: nothing can be clicked");
            Assert.IsNotNull(events.currentInputModule, "the EventSystem has no input module");

            // The first slot that is on screen: a batch run has a small window and the bar may
            // run off its right edge.
            BuildSlot slot = null;
            Vector2 point = Vector2.zero;
            foreach (BuildSlot candidate in Object.FindObjectsByType<BuildSlot>(FindObjectsSortMode.None))
            {
                var rect = (RectTransform)candidate.transform;
                Canvas canvas = candidate.GetComponentInParent<Canvas>().rootCanvas;
                Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
                Vector2 at = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center));
                if (at.x > 0f && at.y > 0f && at.x < Screen.width && at.y < Screen.height && (slot == null || at.x < point.x))
                {
                    slot = candidate;
                    point = at;
                }
            }

            Assert.IsNotNull(slot, "no build slot is active and on screen");

            var data = new PointerEventData(events) { position = point };
            var hits = new List<RaycastResult>();
            events.RaycastAll(data, hits);

            var report = new StringBuilder();
            report.Append("screen ").Append(Screen.width).Append('x').Append(Screen.height)
                  .Append(", slot '").Append(slot.name).Append("' at ").Append(point).Append("; hits:");
            foreach (RaycastResult hit in hits)
            {
                report.Append("\n  ").Append(PathOf(hit.gameObject.transform)).Append(" (depth ").Append(hit.depth).Append(", order ").Append(hit.sortingOrder).Append(')');
            }

            Debug.Log("[HudClick] " + report);
            Assert.IsTrue(hits.Count > 0, "the raycast hit nothing at the slot: " + report);
            GameObject top = hits[0].gameObject;
            Assert.IsTrue(top.transform.IsChildOf(slot.transform), "something else is on top of the slot: " + report);

            GameObject handler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(top);
            Assert.IsNotNull(handler, "nothing under the pointer handles a click: " + report);

            var placer = Object.FindAnyObjectByType<TowerPlacer>();
            Assert.IsNotNull(placer, "no placer in the scene");
            int index = IndexOf(placer, slot.name);
            Assert.IsTrue(index >= 0, "the slot '" + slot.name + "' names no tower in the placer's catalog");

            // The real path: a mouse the Input System drives, so the UI input module, the event
            // system and the button all take part, not just the raycast.
            int clicks = 0;
            int placed = 0;
            slot.Button.onClick.AddListener(() => clicks++);
            placer.TowerPlaced += _ => placed++;
            var trace = new StringBuilder();
            Mouse mouse = InputSystem.AddDevice<Mouse>("HudClickMouse");
            try
            {
                InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
                InputSystem.Update();
                yield return null;
                trace.Append("\n  after move: clicks ").Append(clicks).Append(", index ").Append(placer.ActivePlacementIndex).Append(", over UI ").Append(events.IsPointerOverGameObject()).Append(", module ").Append(events.currentInputModule.GetType().Name);
                InputSystem.QueueStateEvent(mouse, new MouseState { position = point }.WithButton(MouseButton.Left, true));
                InputSystem.Update();
                yield return null;
                trace.Append("\n  after press: clicks ").Append(clicks).Append(", index ").Append(placer.ActivePlacementIndex).Append(", placed ").Append(placed).Append(", busy ").Append(placer.IsBusy);
                InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
                InputSystem.Update();
                yield return null;
                trace.Append("\n  after release: clicks ").Append(clicks).Append(", index ").Append(placer.ActivePlacementIndex).Append(", placed ").Append(placed).Append(", busy ").Append(placer.IsBusy);
                yield return null;
                trace.Append("\n  a frame later: clicks ").Append(clicks).Append(", index ").Append(placer.ActivePlacementIndex).Append(", placed ").Append(placed).Append(", busy ").Append(placer.IsBusy);
            }
            finally
            {
                InputSystem.RemoveDevice(mouse);
            }

            Debug.Log("[HudClick] trace:" + trace);
            Assert.AreEqual(index, placer.ActivePlacementIndex,
                "a mouse click on '" + slot.name + "' did not pick it: " + report + trace);
        }

        private static Vector2 ScreenPointOf(RectTransform rect)
        {
            Canvas canvas = rect.GetComponentInParent<Canvas>().rootCanvas;
            Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            return RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center));
        }

        private static IEnumerator VirtualClick(Vector2 at)
        {
            Mouse mouse = InputSystem.AddDevice<Mouse>("HudClickControlMouse");
            try
            {
                InputSystem.QueueStateEvent(mouse, new MouseState { position = at });
                InputSystem.Update();
                yield return null;
                InputSystem.QueueStateEvent(mouse, new MouseState { position = at }.WithButton(MouseButton.Left, true));
                InputSystem.Update();
                yield return null;
                InputSystem.QueueStateEvent(mouse, new MouseState { position = at });
                InputSystem.Update();
                yield return null;
                yield return null;
            }
            finally
            {
                InputSystem.RemoveDevice(mouse);
            }
        }

        private static int IndexOf(TowerPlacer placer, string towerName)
        {
            for (int i = 0; i < placer.TowerCount; i++)
            {
                if (placer.Catalog.Towers[i] != null && placer.Catalog.Towers[i].DisplayName == towerName)
                {
                    return i;
                }
            }

            return -1;
        }

        private static string PathOf(Transform t)
        {
            string path = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }

            return path;
        }

        [UnityTearDown]
        public IEnumerator LeaveAnEmptyScene()
        {
            if (_inputSettingsChanged)
            {
                InputSystem.settings.backgroundBehavior = _backgroundBehavior;
                InputSystem.settings.editorInputBehaviorInPlayMode = _editorInputBehavior;
                _inputSettingsChanged = false;
            }

            SaveStore.Close();
            Scene empty = SceneManager.CreateScene("HudClickTeardown");
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
    }
}
#endif
