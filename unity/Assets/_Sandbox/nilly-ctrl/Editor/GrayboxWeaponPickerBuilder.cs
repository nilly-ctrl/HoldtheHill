using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Builds the weapon picker scene: grass, a camera, the pixel HUD skin and a
    /// <see cref="GrayboxWeaponPicker"/> pointed at the animation library.
    /// </summary>
    internal static class GrayboxWeaponPickerBuilder
    {
        internal const string ScenePath = "Assets/_Sandbox/nilly-ctrl/Graybox/GrayboxWeaponPicker.unity";

        [MenuItem("Tools/Hold the Hill/Open Weapon Picker")]
        public static void Open()
        {
            if (!System.IO.File.Exists(ScenePath))
            {
                Build();
                return;
            }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        [MenuItem("Tools/Hold the Hill/Build Weapon Picker")]
        public static void Build()
        {
            // Rebake first, so newly generated Wpn files are in the library.
            GrayboxAnimLibraryBuilder.Build();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // NewScene can unload the library (nothing in the empty scene holds it), so fetch it again.
            var library = AssetDatabase.LoadAssetAtPath<SpriteAnimLibrary>(GrayboxAnimLibraryBuilder.LibraryPath);

            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            // The list panel covers the left of the screen, so the stage sits right of centre.
            cameraObject.transform.position = new Vector3(-0.9f, 0.4f, -10f);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 3.2f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.16f, 0.17f, 0.19f);
            cameraObject.AddComponent<AudioListener>();

            GrayboxBuilder.BuildGrass(new Vector3(0f, 0f, 0f), new Vector2(40f, 24f));
            GrayboxBuilder.BuildUi();

            var picker = new GameObject("GrayboxWeaponPicker").AddComponent<GrayboxWeaponPicker>();
            picker.Configure(library);
            EditorUtility.SetDirty(picker);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();

            int weapons = 0;
            foreach (SpriteAnimSet set in library.Sets)
            {
                if (set.Key.StartsWith("Wpn"))
                {
                    weapons++;
                }
            }

            Debug.Log($"[Graybox] Built {ScenePath} with {weapons} weapons. Open it and press Play.");
        }
    }
}
