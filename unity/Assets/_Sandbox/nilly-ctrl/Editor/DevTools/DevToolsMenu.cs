using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Editor menu for the runtime dev tools (<c>Graybox/DevTools</c>): add them to the open scene,
    /// or have them created on Play so they survive a rebuild of the generated scene.
    /// </summary>
    [InitializeOnLoad]
    public static class DevToolsMenu
    {
        private const string AutoCreatePref = "HoldTheHill.DevTools.AutoCreate";
        private const string AddPath = "Tools/Hold the Hill/DevTools/Add DevTools To Open Scene";
        private const string AutoPath = "Tools/Hold the Hill/DevTools/Create DevTools On Play";

        static DevToolsMenu()
        {
            DevToolsBootstrap.AutoCreate = EditorPrefs.GetBool(AutoCreatePref, false);
        }

        [MenuItem(AddPath)]
        public static void AddToOpenScene()
        {
            DevToolsBootstrap existing = Object.FindAnyObjectByType<DevToolsBootstrap>();
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                Debug.Log("[DevTools] This scene already has a DevToolsBootstrap on '" + existing.name + "'.");
                return;
            }

            var holder = new GameObject("DevTools");
            Undo.RegisterCreatedObjectUndo(holder, "Add DevTools");
            holder.AddComponent<DevToolsBootstrap>();
            EditorSceneManager.MarkSceneDirty(holder.scene);
            Selection.activeGameObject = holder;
            Debug.Log("[DevTools] Added DevTools to scene '" + holder.scene.name + "'. Save the scene to keep it. "
                + "Keys: ` or F1 console, F2 tuning, F3 pick enemy, F4 perf.");
        }

        [MenuItem(AutoPath)]
        public static void ToggleAutoCreate()
        {
            bool on = !EditorPrefs.GetBool(AutoCreatePref, false);
            EditorPrefs.SetBool(AutoCreatePref, on);
            DevToolsBootstrap.AutoCreate = on;
        }

        [MenuItem(AutoPath, true)]
        private static bool ToggleAutoCreateValidate()
        {
            Menu.SetChecked(AutoPath, EditorPrefs.GetBool(AutoCreatePref, false));
            return true;
        }
    }
}
