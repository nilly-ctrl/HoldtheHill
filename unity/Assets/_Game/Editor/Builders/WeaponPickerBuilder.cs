using HoldTheHill.Features.Towers;
using HoldTheHill.Core.Animation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HoldTheHill.EditorTools.Builders
{
    /// <summary>
    /// Builds the weapon picker scene: grass, a camera, the pixel HUD skin and a
    /// <see cref="WeaponPicker"/> pointed at the animation library.
    /// </summary>
    internal static class WeaponPickerBuilder
    {
        internal const string ScenePath = "Assets/_Game/Scenes/Dev/WeaponPicker.unity";

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
            AnimLibraryBuilder.Build();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // NewScene can unload the library (nothing in the empty scene holds it), so fetch it again.
            var library = AssetDatabase.LoadAssetAtPath<SpriteAnimLibrary>(AnimLibraryBuilder.LibraryPath);

            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            // The list panel covers the left of the screen, so the stage sits right of centre.
            cameraObject.transform.position = new Vector3(-0.9f, 0.4f, -10f);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 3.2f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.16f, 0.17f, 0.19f);
            cameraObject.AddComponent<AudioListener>();

            SceneDressingBuilder.BuildGrass(new Vector3(0f, 0f, 0f), new Vector2(40f, 24f));
            CombatTestSceneBuilder.BuildUi();

            var picker = new GameObject("WeaponPicker").AddComponent<WeaponPicker>();
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
