using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Builds Fonts/FontDemo.unity: every baked font style drawn with <see cref="PixelText"/>, each
    /// beside its name, as a specimen inside the engine. The scene has a <see cref="PixelFontTheme"/>;
    /// type a theme name into it to see the whole sheet change. The scene is generated, so change
    /// this builder rather than the scene.
    /// </summary>
    internal static class PixelFontDemoBuilder
    {
        internal const string ScenePath = "Assets/_Sandbox/nilly-ctrl/Fonts/FontDemo.unity";
        private const string LibraryPath = "Assets/_Sandbox/nilly-ctrl/Fonts/Generated/PixelFontThemes.asset";
        private const string Sample = "Hold the Hill 128!";
        private const int PixelsPerUnit = 32;
        private const int Columns = 3;
        private const float ColumnWidth = 15f;
        private const float RowGap = 0.35f;

        [MenuItem("Tools/Hold the Hill/Build Font Demo")]
        public static void Build()
        {
            PixelFontBuilder.Build();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // NewScene can unload the library (nothing in the empty scene holds it), so load it after.
            var library = AssetDatabase.LoadAssetAtPath<PixelFontThemeLibrary>(LibraryPath);
            if (library == null)
            {
                Debug.LogError($"[Fonts] {LibraryPath} is missing; Build Pixel Fonts did not produce it.");
                return;
            }

            var names = new List<string>(library.BaseStyles);
            names.Sort(System.StringComparer.Ordinal);
            PixelFontStyle labelStyle = library.Find(null, "Hud");
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            var root = new GameObject("Font Demo");
            int perColumn = Mathf.CeilToInt(names.Count / (float)Columns);
            float deepest = 0f;
            for (int column = 0; column < Columns; column++)
            {
                float x = (column - (Columns - 1) / 2f) * ColumnWidth - ColumnWidth / 2f + 0.5f;
                float y = 0f;
                for (int i = column * perColumn; i < Mathf.Min(names.Count, (column + 1) * perColumn); i++)
                {
                    PixelFontStyle style = library.Find(null, names[i]);
                    if (style == null)
                    {
                        continue;
                    }

                    float height = style.LineHeight / (float)PixelsPerUnit;
                    y -= height;
                    // A name in the plain HUD style, then the sample in the style itself. Only the sample
                    // follows the theme switch, so the names stay readable in every theme.
                    AddText(root.transform, names[i], labelStyle, null, new Vector3(x, y + 0.1f, 0f), shader);
                    AddText(root.transform, Sample, style, names[i], new Vector3(x + 4.6f, y, 0f), shader);
                    y -= RowGap;
                }

                deepest = Mathf.Min(deepest, y);
            }

            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(0x4a, 0x3a, 0x2e, 0xff);
            camera.orthographicSize = Mathf.Max(-deepest / 2f + 1f, Columns * ColumnWidth / 2f / 1.7f);
            cameraObject.transform.position = new Vector3(0f, deepest / 2f, -10f);

            var themeObject = new GameObject("Pixel Font Theme");
            PixelFontBuilder.ConfigureTheme(themeObject.AddComponent<PixelFontTheme>());

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[Fonts] Built {ScenePath} with {names.Count} styles. Set a theme on 'Pixel Font Theme' to restyle it.");
        }

        private static void AddText(Transform parent, string text, PixelFontStyle style, string themeStyle,
            Vector3 position, Shader shader)
        {
            var go = new GameObject(string.IsNullOrEmpty(themeStyle) ? "Name " + text : themeStyle);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            var pixelText = go.AddComponent<PixelText>();
            var serialized = new SerializedObject(pixelText);
            serialized.FindProperty("_style").objectReferenceValue = style;
            serialized.FindProperty("_themeStyle").stringValue = themeStyle ?? string.Empty;
            serialized.FindProperty("_text").stringValue = text;
            serialized.FindProperty("_anchor").enumValueIndex = (int)PixelText.Anchor.Left;
            serialized.FindProperty("_shader").objectReferenceValue = shader;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            pixelText.Rebuild();
        }
    }
}
