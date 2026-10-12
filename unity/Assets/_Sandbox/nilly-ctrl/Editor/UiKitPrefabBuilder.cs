using System.Collections.Generic;
using System.Linq;
using HoldTheHill.Sandbox.UiKit;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Builds the kit: the TextMeshPro pixel font, the widget prefabs, the UI root and event system, and the gallery scene. Menu: Tools > Hold the Hill > Build UI Kit.
    /// </summary>
    internal static class UiKitPrefabBuilder
    {
        internal const string TtfPath = "Assets/_Sandbox/nilly-ctrl/Fonts/TTF/HoldTheHillPixel-Regular.ttf";

        internal const string InputActionsPath = "Assets/Settings/InputSystem_Actions.inputactions";

        // Every character the pixel font has, as inclusive ranges (the TTF's character map; see
        // Fonts/Source~/build_pixel_fonts.py). E000 and up are the inline icons and the key, mouse
        // and gamepad prompts named in PixelGlyphs.cs.
        internal static readonly int[] GlyphRanges =
        {
            0x0020, 0x007E, 0x00A1, 0x00A1, 0x00B0, 0x00B1, 0x00B7, 0x00B7, 0x00BF, 0x00C4,
            0x00C7, 0x00CF, 0x00D1, 0x00D7, 0x00D9, 0x00DD, 0x00DF, 0x00E4, 0x00E7, 0x00EF,
            0x00F1, 0x00F6, 0x00F9, 0x00FD, 0x00FF, 0x00FF, 0x2013, 0x2014, 0x2018, 0x2019,
            0x201C, 0x201D, 0x2022, 0x2022, 0x2026, 0x2026, 0x2190, 0x2193, 0x221E, 0x221E,
            0x24B6, 0x24B7, 0x24C1, 0x24C1, 0x24C7, 0x24C7, 0x24CD, 0x24CE, 0x25B2, 0x25B2,
            0x25B6, 0x25B6, 0x25BC, 0x25BC, 0x25C0, 0x25C0, 0x25CF, 0x25CF, 0x2605, 0x2606,
            0x2665, 0x2665, 0x2713, 0x2713, 0x2715, 0x2715, 0xE000, 0xE00B, 0xE010, 0xE019,
            0xE020, 0xE039, 0xE040, 0xE04B, 0xE050, 0xE053, 0xE060, 0xE06F, 0xE080, 0xE0A0,
        };

        [MenuItem("Tools/Hold the Hill/Build UI Kit")]
        public static void Build()
        {
            // Building opens a new scene, which would silently drop unsaved work in the open one.
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EnsureFolder(UiKitBuilder.Root, "Prefabs");
            EnsureFolder(UiKitBuilder.Root, "Fonts");
            AssetDatabase.ImportAsset(UiKitBuilder.SpriteDir, ImportAssetOptions.ImportRecursive);

            // Prefabs are assembled in a scratch scene so nothing leaks into the user's scene.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            UiKitBuilder.s_font = BuildFont();

            SavePrefab(UiKitWidgetBuilder.MakeButton("Button", "Button", "ButtonHover", "ButtonPressed", "ButtonDisabled", new Vector2(120f, UiKitStyle.RowHeight)));
            SavePrefab(UiKitWidgetBuilder.MakeButton("TabButton", "TabInactive", "TabHover", "TabActive", "ButtonDisabled", new Vector2(72f, UiKitStyle.RowHeight)));
            SavePrefab(UiKitWidgetBuilder.MakePanel());
            SavePrefab(UiKitWidgetBuilder.MakeSliderField());
            SavePrefab(UiKitWidgetBuilder.MakeToggleField());
            SavePrefab(UiKitWidgetBuilder.MakeStepper());
            SavePrefab(UiKitWidgetBuilder.MakeTabBar());
            SavePrefab(UiKitWidgetBuilder.MakeConfirmDialog());
            SavePrefab(MakeUiRoot());
            SavePrefab(MakeEventSystem());

            BuildGallery();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[UiKit] Built prefabs in {UiKitBuilder.PrefabDir} and {UiKitBuilder.GalleryPath}.");
        }

        [MenuItem("Tools/Hold the Hill/Open UI Kit Gallery")]
        public static void OpenGallery()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(UiKitBuilder.GalleryPath) == null)
            {
                Build();
                return;
            }

            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(UiKitBuilder.GalleryPath, OpenSceneMode.Single);
            }
        }

        // The TTF as a TextMeshPro bitmap font: sampled at 10 px, where one font pixel is exactly
        // one texel, with no anti-aliasing and point filtering, so text scales like the sprites.
        internal static TMP_FontAsset BuildFont()
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiKitBuilder.FontPath);
            if (existing != null)
            {
                return existing;
            }

            return CreateFont();
        }

        /// <summary>
        /// Samples the TTF again into the font asset, after the TTF or its character list changed.
        /// The asset, its material and its atlas keep their ids, so prefabs and scenes keep their links.
        /// </summary>
        [MenuItem("Tools/Hold the Hill/Rebuild UI Kit Font")]
        public static void RebuildFont()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiKitBuilder.FontPath);
            if (font == null)
            {
                EnsureFolder(UiKitBuilder.Root, "Fonts");
                UiKitBuilder.s_font = CreateFont();
                return;
            }

            AssetDatabase.ImportAsset(TtfPath, ImportAssetOptions.ForceUpdate);
            font.atlasPopulationMode = AtlasPopulationMode.Dynamic; // brings the TTF back as the source
            font.ClearFontAssetData(false);
            Populate(font);
            UiKitBuilder.s_font = font;
            Debug.Log($"[UiKit] Rebuilt {UiKitBuilder.FontPath}: {font.characterTable.Count} characters.");
        }

        internal static TMP_FontAsset CreateFont()
        {

            var ttf = AssetDatabase.LoadAssetAtPath<Font>(TtfPath);
            if (ttf == null)
            {
                Debug.LogError($"[UiKit] Pixel font not found at {TtfPath}.");
                return null;
            }

            TMP_FontAsset font = TMP_FontAsset.CreateFontAsset(
                ttf, UiKitStyle.BodySize, 1, GlyphRenderMode.RASTER, 256, 256, AtlasPopulationMode.Dynamic, false);
            font.name = "HoldTheHillPixel";
            AssetDatabase.CreateAsset(font, UiKitBuilder.FontPath);

            font.material.name = "HoldTheHillPixel Material";
            AssetDatabase.AddObjectToAsset(font.material, font);
            font.atlasTexture.name = "HoldTheHillPixel Atlas";
            AssetDatabase.AddObjectToAsset(font.atlasTexture, font);

            Populate(font);
            return font;
        }

        internal static void Populate(TMP_FontAsset font)
        {
            var glyphs = new System.Text.StringBuilder();
            for (int i = 0; i < GlyphRanges.Length; i += 2)
            {
                for (int code = GlyphRanges[i]; code <= GlyphRanges[i + 1]; code++) glyphs.Append((char)code);
            }

            if (!font.TryAddCharacters(glyphs.ToString(), out string missing) && !string.IsNullOrEmpty(missing))
            {
                string codes = string.Join(" ", missing.Select(c => ((int)c).ToString("X4")));
                Debug.LogError($"[UiKit] The pixel font could not take {missing.Length} characters: {codes}");
            }

            font.atlasTexture.filterMode = FilterMode.Point;
            // Static: the atlas is complete, and a dynamic one would rewrite the asset during Play.
            font.atlasPopulationMode = AtlasPopulationMode.Static;

            EditorUtility.SetDirty(font);
            EditorUtility.SetDirty(font.atlasTexture);
            AssetDatabase.SaveAssets();
        }

        internal static GameObject MakeUiRoot()
        {
            var go = new GameObject("UiRoot", typeof(RectTransform));
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvas.pixelPerfect = true;
            go.AddComponent<CanvasScaler>();
            go.AddComponent<UiPixelCanvasScaler>();
            go.AddComponent<GraphicRaycaster>();
            return go;
        }

        // Wired to the project's own input actions (the UI map) rather than left on the module's
        // built-in defaults, so the controls screen can rebind menu input later.
        internal static GameObject MakeEventSystem()
        {
            var go = new GameObject("UiEventSystem");
            go.AddComponent<EventSystem>();
            var module = go.AddComponent<InputSystemUIInputModule>();

            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (actions == null)
            {
                Debug.LogWarning($"[UiKit] {InputActionsPath} not found; the UI falls back to Unity's default menu actions.");
                return go;
            }

            // The visible reference for each action (the importer also adds a hidden duplicate).
            Dictionary<string, InputActionReference> refs = AssetDatabase.LoadAllAssetsAtPath(InputActionsPath)
                .OfType<InputActionReference>()
                .Where(r => (r.hideFlags & HideFlags.HideInHierarchy) == 0)
                .GroupBy(r => r.name)
                .ToDictionary(g => g.Key, g => g.First());

            // Written straight to the serialized fields. The module's own setters skip a reference
            // that points at the same action as the temporary one it made for itself, and that
            // temporary one is not an asset, so it would save as "none".
            void WireAction(string field, string action)
            {
                if (refs.TryGetValue("UI/" + action, out InputActionReference reference))
                {
                    UiKitBuilder.Set(module, field, reference);
                }
                else
                {
                    Debug.LogWarning($"[UiKit] {InputActionsPath} has no UI/{action} action.");
                }
            }

            UiKitBuilder.Set(module, "m_ActionsAsset", actions);
            WireAction("m_PointAction", "Point");
            WireAction("m_LeftClickAction", "Click");
            WireAction("m_RightClickAction", "RightClick");
            WireAction("m_MiddleClickAction", "MiddleClick");
            WireAction("m_ScrollWheelAction", "ScrollWheel");
            WireAction("m_MoveAction", "Navigate");
            WireAction("m_SubmitAction", "Submit");
            WireAction("m_CancelAction", "Cancel");
            WireAction("m_TrackedDevicePositionAction", "TrackedDevicePosition");
            WireAction("m_TrackedDeviceOrientationAction", "TrackedDeviceOrientation");
            return go;
        }

        internal static void BuildGallery()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(0x2f, 0x4a, 0x2a, 0xff);
            cameraObject.AddComponent<AudioListener>();

            UiKitBuilder.InstantiatePrefab("UiEventSystem", null);
            Transform canvas = UiKitBuilder.InstantiatePrefab("UiRoot", null).transform;

            RectTransform screenRect = UiKitBuilder.NewRect("GalleryScreen", canvas);
            UiKitBuilder.Stretch(screenRect);
            screenRect.gameObject.AddComponent<CanvasGroup>();
            var screen = screenRect.gameObject.AddComponent<UiScreen>();
            UiKitBuilder.Set(screen, "_closeOnBack", false);

            RectTransform panel = UiKitBuilder.AddPanel(screenRect, 300f);
            UiKitBuilder.AddTitle(panel, "UI Kit");

            UiTabBar tabs = UiKitBuilder.AddTabBar(panel);
            RectTransform controls = UiKitBuilder.AddPage(panel, "ControlsPage");
            RectTransform buttons = UiKitBuilder.AddPage(panel, "ButtonsPage");
            RectTransform about = UiKitBuilder.AddPage(panel, "AboutPage");
            UiButton firstTab = UiKitBuilder.AddTab(tabs, "Controls", controls.gameObject);
            UiKitBuilder.AddTab(tabs, "Buttons", buttons.gameObject);
            UiKitBuilder.AddTab(tabs, "About", about.gameObject);

            UiKitBuilder.AddSlider(controls, "Master volume", 0.8f);
            UiKitBuilder.AddSlider(controls, "Music", 0.5f);
            UiKitBuilder.AddToggle(controls, "Screen shake", true);
            UiKitBuilder.AddToggle(controls, "Fullscreen", false);
            UiKitBuilder.AddStepper(controls, "Game speed", new[] { "1x", "2x", "4x" }, 0);
            UiKitBuilder.AddStepper(controls, "Resolution", new[] { "1280x720", "1920x1080", "2560x1440" }, 1);

            UiButton openDialog = UiKitBuilder.AddButton(buttons, "Open confirm dialog");
            UiKitBuilder.AddButton(buttons, "Plain button");
            UiButton disabled = UiKitBuilder.AddButton(buttons, "Disabled button");
            disabled.Button.interactable = false;

            UiKitBuilder.AddBody(about, "Arrows or stick: move. Enter or A: press.\nLeft and right: change a value.\nQ and E or shoulders: switch tab.\nEsc or B: back.");

            UiKitBuilder.AddDivider(panel);
            TextMeshProUGUI status = UiKitBuilder.AddBody(panel, "Last cue: none");
            status.color = UiKitStyle.Dim;

            UiKitBuilder.Set(screen, "_firstSelected", firstTab.Button);

            var dialog = UiKitBuilder.InstantiatePrefab("ConfirmDialog", canvas).GetComponent<UiConfirmDialog>();

            var demo = screenRect.gameObject.AddComponent<UiKitGalleryDemo>();
            UiKitBuilder.Set(demo, "_openDialog", openDialog);
            UiKitBuilder.Set(demo, "_dialog", dialog);
            UiKitBuilder.Set(demo, "_status", status);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, UiKitBuilder.GalleryPath);
        }

        internal static void SavePrefab(GameObject instance)
        {
            PrefabUtility.SaveAsPrefabAsset(instance, $"{UiKitBuilder.PrefabDir}/{instance.name}.prefab");
            Object.DestroyImmediate(instance);
        }

        internal static void EnsureFolder(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder($"{parent}/{child}"))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }
    }
}
