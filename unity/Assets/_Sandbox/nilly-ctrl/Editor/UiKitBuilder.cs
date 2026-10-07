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
    /// Generates the UI kit: the TextMeshPro pixel font, one prefab per widget, and a gallery
    /// scene that shows them all. Menu: Tools > Hold the Hill > Build UI Kit.
    /// </summary>
    /// <remarks>
    /// The prefabs are generated rather than hand-made, like the graybox scene, so the kit can
    /// be rebuilt after a sprite or layout change and reviewed as a code diff. Rebuilding keeps
    /// each prefab's GUID, so screens that use them keep their links.
    ///
    /// The Add* methods at the bottom are for the screen builders (pause, settings, and so on):
    /// they place kit prefabs as linked instances, not copies.
    /// </remarks>
    public static class UiKitBuilder
    {
        public const string Root = "Assets/_Sandbox/nilly-ctrl/UiKit";
        public const string PrefabDir = Root + "/Prefabs";
        public const string FontPath = Root + "/Fonts/HoldTheHillPixel.asset";
        public const string GalleryPath = Root + "/UiKitGallery.unity";

        private const string SpriteDir = Root + "/Sprites";
        private const string TtfPath = "Assets/_Sandbox/nilly-ctrl/Fonts/TTF/HoldTheHillPixel-Regular.ttf";
        private const string InputActionsPath = "Assets/Settings/InputSystem_Actions.inputactions";

        // Every character the pixel font has.
        private const string Glyphs =
            " !\"#$%&'()*+,-./0123456789:<=>?ABCDEFGHIJKLMNOPQRSTUVWXYZ[]_abcdefghijklmnopqrstuvwxyz|×→●";

        private const float LabelSplit = 0.42f; // where a row's label ends and its control begins

        private static TMP_FontAsset s_font;

        [MenuItem("Tools/Hold the Hill/Build UI Kit")]
        public static void Build()
        {
            // Building opens a new scene, which would silently drop unsaved work in the open one.
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EnsureFolder(Root, "Prefabs");
            EnsureFolder(Root, "Fonts");
            AssetDatabase.ImportAsset(SpriteDir, ImportAssetOptions.ImportRecursive);

            // Prefabs are assembled in a scratch scene so nothing leaks into the user's scene.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            s_font = BuildFont();

            SavePrefab(MakeButton("Button", "Button", "ButtonHover", "ButtonPressed", "ButtonDisabled", new Vector2(120f, UiKitStyle.RowHeight)));
            SavePrefab(MakeButton("TabButton", "TabInactive", "TabHover", "TabActive", "ButtonDisabled", new Vector2(72f, UiKitStyle.RowHeight)));
            SavePrefab(MakePanel());
            SavePrefab(MakeSliderField());
            SavePrefab(MakeToggleField());
            SavePrefab(MakeStepper());
            SavePrefab(MakeTabBar());
            SavePrefab(MakeConfirmDialog());
            SavePrefab(MakeUiRoot());
            SavePrefab(MakeEventSystem());

            BuildGallery();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[UiKit] Built prefabs in {PrefabDir} and {GalleryPath}.");
        }

        [MenuItem("Tools/Hold the Hill/Open UI Kit Gallery")]
        public static void OpenGallery()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(GalleryPath) == null)
            {
                Build();
                return;
            }

            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(GalleryPath, OpenSceneMode.Single);
            }
        }

        // ---------- font ----------

        // The TTF as a TextMeshPro bitmap font: sampled at 10 px, where one font pixel is exactly
        // one texel, with no anti-aliasing and point filtering, so text scales like the sprites.
        private static TMP_FontAsset BuildFont()
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (existing != null)
            {
                return existing;
            }

            var ttf = AssetDatabase.LoadAssetAtPath<Font>(TtfPath);
            if (ttf == null)
            {
                Debug.LogError($"[UiKit] Pixel font not found at {TtfPath}.");
                return null;
            }

            TMP_FontAsset font = TMP_FontAsset.CreateFontAsset(
                ttf, UiKitStyle.BodySize, 1, GlyphRenderMode.RASTER, 256, 256, AtlasPopulationMode.Dynamic, false);
            font.name = "HoldTheHillPixel";
            AssetDatabase.CreateAsset(font, FontPath);

            font.material.name = "HoldTheHillPixel Material";
            AssetDatabase.AddObjectToAsset(font.material, font);
            font.atlasTexture.name = "HoldTheHillPixel Atlas";
            AssetDatabase.AddObjectToAsset(font.atlasTexture, font);

            if (!font.TryAddCharacters(Glyphs, out string missing) && !string.IsNullOrEmpty(missing))
            {
                Debug.LogWarning($"[UiKit] The pixel font has no glyph for: {missing}");
            }

            font.atlasTexture.filterMode = FilterMode.Point;
            // Static: the atlas is complete, and a dynamic one would rewrite the asset during Play.
            font.atlasPopulationMode = AtlasPopulationMode.Static;

            EditorUtility.SetDirty(font);
            EditorUtility.SetDirty(font.atlasTexture);
            AssetDatabase.SaveAssets();
            return font;
        }

        // ---------- widgets ----------

        private static GameObject MakeButton(string name, string normal, string hover, string pressed, string disabled, Vector2 size)
        {
            RectTransform root = NewRect(name);
            root.sizeDelta = size;

            Image image = AddImage(root, normal);
            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState
            {
                highlightedSprite = Sprite(hover),
                selectedSprite = Sprite(hover),
                pressedSprite = Sprite(pressed),
                disabledSprite = Sprite(disabled),
            };

            AddRowLayout(root, minWidth: 48f, flexibleWidth: 1f);

            TextMeshProUGUI label = AddText(root, "Label", name == "TabButton" ? "Tab" : "Button", UiKitStyle.BodySize, TextAlignmentOptions.Center);
            Stretch(label.rectTransform, 6f, 0f, 6f, 0f);

            GameObject ring = AddFocusRing(root);
            Set(root.gameObject.AddComponent<UiFocusable>(), "_focusRing", ring);
            Set(root.gameObject.AddComponent<UiButton>(), "_label", label);
            return root.gameObject;
        }

        private static GameObject MakePanel()
        {
            RectTransform root = NewRect("Panel");
            root.sizeDelta = new Vector2(240f, 120f);
            AddImage(root, "Panel");

            var layout = root.gameObject.AddComponent<VerticalLayoutGroup>();
            int pad = (int)UiKitStyle.Padding;
            layout.padding = new RectOffset(pad, pad, pad, pad);
            layout.spacing = UiKitStyle.Spacing;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return root.gameObject;
        }

        private static GameObject MakeSliderField()
        {
            RectTransform root = NewRow("SliderField");

            TextMeshProUGUI label = AddText(root, "Label", "Slider", UiKitStyle.BodySize, TextAlignmentOptions.Left);
            Anchor(label.rectTransform, new Vector2(0f, 0f), new Vector2(LabelSplit, 1f), new Vector2(2f, 0f), Vector2.zero);

            RectTransform sliderRect = NewRect("Slider", root);
            Anchor(sliderRect, new Vector2(LabelSplit, 0f), new Vector2(0.84f, 1f), Vector2.zero, Vector2.zero);

            RectTransform track = NewRect("Track", sliderRect);
            Anchor(track, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, -5f), new Vector2(0f, 5f));
            AddImage(track, "Well");

            RectTransform fillArea = NewRect("Fill Area", sliderRect);
            Anchor(fillArea, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(2f, -3f), new Vector2(-2f, 3f));
            RectTransform fill = NewRect("Fill", fillArea);
            fill.sizeDelta = Vector2.zero;
            AddImage(fill, "SliderFill", raycast: false);

            RectTransform handleArea = NewRect("Handle Slide Area", sliderRect);
            Anchor(handleArea, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(4f, -7f), new Vector2(-4f, 7f));
            RectTransform handle = NewRect("Handle", handleArea);
            handle.sizeDelta = new Vector2(8f, 0f);
            Image handleImage = AddImage(handle, "SliderHandle");

            var slider = sliderRect.gameObject.AddComponent<Slider>();
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;
            slider.transition = Selectable.Transition.SpriteSwap;
            slider.spriteState = new SpriteState
            {
                highlightedSprite = Sprite("SliderHandleHover"),
                selectedSprite = Sprite("SliderHandleHover"),
                pressedSprite = Sprite("SliderHandleHover"),
            };
            slider.value = 0.8f;

            TextMeshProUGUI value = AddText(root, "Value", "80%", UiKitStyle.BodySize, TextAlignmentOptions.Right);
            Anchor(value.rectTransform, new Vector2(0.84f, 0f), new Vector2(1f, 1f), Vector2.zero, new Vector2(-2f, 0f));

            GameObject ring = AddFocusRing(root);
            var focusable = sliderRect.gameObject.AddComponent<UiFocusable>();
            Set(focusable, "_focusRing", ring);
            Set(focusable, "_pressCue", false);

            var field = root.gameObject.AddComponent<UiSliderField>();
            Set(field, "_slider", slider);
            Set(field, "_label", label);
            Set(field, "_valueText", value);
            return root.gameObject;
        }

        private static GameObject MakeToggleField()
        {
            RectTransform root = NewRow("ToggleField");
            AddHitArea(root); // so a click anywhere on the row flips it

            TextMeshProUGUI label = AddText(root, "Label", "Toggle", UiKitStyle.BodySize, TextAlignmentOptions.Left);
            Stretch(label.rectTransform, 2f, 0f, 20f, 0f);

            RectTransform box = NewRect("Box", root);
            box.anchorMin = box.anchorMax = new Vector2(1f, 0.5f);
            box.pivot = new Vector2(1f, 0.5f);
            box.anchoredPosition = new Vector2(-2f, 0f);
            box.sizeDelta = new Vector2(14f, 14f);
            Image boxImage = AddImage(box, "ToggleBox", raycast: false);

            RectTransform check = NewRect("Check", box);
            Stretch(check);
            Image checkImage = AddImage(check, "ToggleCheck", raycast: false);

            var toggle = root.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = boxImage;
            toggle.graphic = checkImage;
            toggle.transition = Selectable.Transition.SpriteSwap;
            toggle.spriteState = new SpriteState
            {
                highlightedSprite = Sprite("ToggleBoxHover"),
                selectedSprite = Sprite("ToggleBoxHover"),
                pressedSprite = Sprite("ToggleBoxHover"),
            };
            toggle.isOn = true;

            GameObject ring = AddFocusRing(root);
            var focusable = root.gameObject.AddComponent<UiFocusable>();
            Set(focusable, "_focusRing", ring);
            Set(focusable, "_pressCue", false);

            var field = root.gameObject.AddComponent<UiToggleField>();
            Set(field, "_toggle", toggle);
            Set(field, "_label", label);
            return root.gameObject;
        }

        private static GameObject MakeStepper()
        {
            RectTransform root = NewRow("Stepper");
            Image hit = AddHitArea(root);

            TextMeshProUGUI label = AddText(root, "Label", "Stepper", UiKitStyle.BodySize, TextAlignmentOptions.Left);
            Anchor(label.rectTransform, new Vector2(0f, 0f), new Vector2(LabelSplit, 1f), new Vector2(2f, 0f), Vector2.zero);

            RectTransform group = NewRect("Choice", root);
            Anchor(group, new Vector2(LabelSplit, 0f), new Vector2(1f, 1f), Vector2.zero, new Vector2(-2f, 0f));

            Button previous = MakeArrowButton("Previous", group, left: true);
            Button next = MakeArrowButton("Next", group, left: false);

            RectTransform well = NewRect("Well", group);
            Stretch(well, 16f, 3f, 16f, 3f);
            AddImage(well, "Well", raycast: false);
            TextMeshProUGUI value = AddText(well, "Value", "Option A", UiKitStyle.BodySize, TextAlignmentOptions.Center);
            Stretch(value.rectTransform, 2f, 0f, 2f, 0f);

            var stepper = root.gameObject.AddComponent<UiStepper>();
            stepper.targetGraphic = hit;
            stepper.transition = Selectable.Transition.None; // the focus ring shows focus
            Set(stepper, "_label", label);
            Set(stepper, "_valueText", value);
            Set(stepper, "_previous", previous);
            Set(stepper, "_next", next);
            SetStrings(stepper, "_options", new[] { "Option A", "Option B", "Option C" });

            GameObject ring = AddFocusRing(root);
            var focusable = root.gameObject.AddComponent<UiFocusable>();
            Set(focusable, "_focusRing", ring);
            Set(focusable, "_pressCue", false);
            return root.gameObject;
        }

        // Mouse-only: a keyboard or gamepad changes the stepper with left/right instead.
        private static Button MakeArrowButton(string name, RectTransform parent, bool left)
        {
            RectTransform rect = NewRect(name, parent);
            rect.anchorMin = new Vector2(left ? 0f : 1f, 0f);
            rect.anchorMax = new Vector2(left ? 0f : 1f, 1f);
            rect.pivot = new Vector2(left ? 0f : 1f, 0.5f);
            rect.sizeDelta = new Vector2(14f, 0f);
            AddHitArea(rect);

            RectTransform arrow = NewRect("Arrow", rect);
            arrow.sizeDelta = new Vector2(6f, 9f);
            arrow.localScale = new Vector3(left ? -1f : 1f, 1f, 1f);
            Image arrowImage = AddImage(arrow, "Arrow", raycast: false);
            arrowImage.color = UiKitStyle.Cream;

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = arrowImage;
            ColorBlock colours = button.colors;
            colours.normalColor = Color.white;
            colours.highlightedColor = UiKitStyle.Gold;
            colours.pressedColor = UiKitStyle.Dim;
            colours.selectedColor = Color.white;
            colours.fadeDuration = 0f;
            button.colors = colours;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            return button;
        }

        private static GameObject MakeTabBar()
        {
            RectTransform root = NewRow("TabBar");
            var layout = root.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 2f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            var bar = root.gameObject.AddComponent<UiTabBar>();
            Set(bar, "_activeSprite", Sprite("TabActive"));
            Set(bar, "_inactiveSprite", Sprite("TabInactive"));
            return root.gameObject;
        }

        private static GameObject MakeConfirmDialog()
        {
            RectTransform root = NewRect("ConfirmDialog");
            Stretch(root);
            root.gameObject.AddComponent<CanvasGroup>();
            var screen = root.gameObject.AddComponent<UiScreen>();

            // Dims the screen underneath and swallows clicks aimed at it.
            RectTransform dim = NewRect("Dim", root);
            Stretch(dim);
            dim.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

            RectTransform panel = AddPanel(root, 260f);
            TextMeshProUGUI title = AddTitle(panel, "Are you sure?");
            TextMeshProUGUI message = AddBody(panel, "This cannot be undone.");
            RectTransform buttons = AddButtonRow(panel);
            UiButton cancel = AddButton(buttons, "Cancel");
            UiButton confirm = AddButton(buttons, "Confirm");

            var dialog = root.gameObject.AddComponent<UiConfirmDialog>();
            Set(dialog, "_title", title);
            Set(dialog, "_message", message);
            Set(dialog, "_confirm", confirm);
            Set(dialog, "_cancel", cancel);
            Set(screen, "_firstSelected", cancel.Button);

            root.gameObject.SetActive(false); // closed until Open() is called
            return root.gameObject;
        }

        private static GameObject MakeUiRoot()
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
        private static GameObject MakeEventSystem()
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
            void Wire(string field, string action)
            {
                if (refs.TryGetValue("UI/" + action, out InputActionReference reference))
                {
                    Set(module, field, reference);
                }
                else
                {
                    Debug.LogWarning($"[UiKit] {InputActionsPath} has no UI/{action} action.");
                }
            }

            Set(module, "m_ActionsAsset", actions);
            Wire("m_PointAction", "Point");
            Wire("m_LeftClickAction", "Click");
            Wire("m_RightClickAction", "RightClick");
            Wire("m_MiddleClickAction", "MiddleClick");
            Wire("m_ScrollWheelAction", "ScrollWheel");
            Wire("m_MoveAction", "Navigate");
            Wire("m_SubmitAction", "Submit");
            Wire("m_CancelAction", "Cancel");
            Wire("m_TrackedDevicePositionAction", "TrackedDevicePosition");
            Wire("m_TrackedDeviceOrientationAction", "TrackedDeviceOrientation");
            return go;
        }

        // ---------- gallery ----------

        private static void BuildGallery()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(0x2f, 0x4a, 0x2a, 0xff);
            cameraObject.AddComponent<AudioListener>();

            InstantiatePrefab("UiEventSystem", null);
            Transform canvas = InstantiatePrefab("UiRoot", null).transform;

            RectTransform screenRect = NewRect("GalleryScreen", canvas);
            Stretch(screenRect);
            screenRect.gameObject.AddComponent<CanvasGroup>();
            var screen = screenRect.gameObject.AddComponent<UiScreen>();
            Set(screen, "_closeOnBack", false);

            RectTransform panel = AddPanel(screenRect, 300f);
            AddTitle(panel, "UI Kit");

            UiTabBar tabs = AddTabBar(panel);
            RectTransform controls = AddPage(panel, "ControlsPage");
            RectTransform buttons = AddPage(panel, "ButtonsPage");
            RectTransform about = AddPage(panel, "AboutPage");
            UiButton firstTab = AddTab(tabs, "Controls", controls.gameObject);
            AddTab(tabs, "Buttons", buttons.gameObject);
            AddTab(tabs, "About", about.gameObject);

            AddSlider(controls, "Master volume", 0.8f);
            AddSlider(controls, "Music", 0.5f);
            AddToggle(controls, "Screen shake", true);
            AddToggle(controls, "Fullscreen", false);
            AddStepper(controls, "Game speed", new[] { "1x", "2x", "4x" }, 0);
            AddStepper(controls, "Resolution", new[] { "1280x720", "1920x1080", "2560x1440" }, 1);

            UiButton openDialog = AddButton(buttons, "Open confirm dialog");
            AddButton(buttons, "Plain button");
            UiButton disabled = AddButton(buttons, "Disabled button");
            disabled.Button.interactable = false;

            AddBody(about, "Arrows or stick: move. Enter or A: press.\nLeft and right: change a value.\nQ and E or shoulders: switch tab.\nEsc or B: back.");

            AddDivider(panel);
            TextMeshProUGUI status = AddBody(panel, "Last cue: none");
            status.color = UiKitStyle.Dim;

            Set(screen, "_firstSelected", firstTab.Button);

            var dialog = InstantiatePrefab("ConfirmDialog", canvas).GetComponent<UiConfirmDialog>();

            var demo = screenRect.gameObject.AddComponent<UiKitGalleryDemo>();
            Set(demo, "_openDialog", openDialog);
            Set(demo, "_dialog", dialog);
            Set(demo, "_status", status);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, GalleryPath);
        }

        // ---------- for screen builders ----------

        /// <summary>A kit panel centred in its parent, as tall as its contents.</summary>
        public static RectTransform AddPanel(Transform parent, float width)
        {
            var panel = (RectTransform)InstantiatePrefab("Panel", parent).transform;
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 0.5f);
            panel.anchoredPosition = Vector2.zero;
            panel.sizeDelta = new Vector2(width, 0f);

            var fitter = panel.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return panel;
        }

        public static TextMeshProUGUI AddTitle(Transform parent, string text)
        {
            TextMeshProUGUI title = AddText(parent, "Title", text, UiKitStyle.TitleSize, TextAlignmentOptions.Center);
            title.color = UiKitStyle.Gold;
            return title;
        }

        /// <summary>Body text that wraps to the panel's width.</summary>
        public static TextMeshProUGUI AddBody(Transform parent, string text)
        {
            TextMeshProUGUI body = AddText(parent, "Text", text, UiKitStyle.BodySize, TextAlignmentOptions.Center);
            body.textWrappingMode = TextWrappingModes.Normal;
            return body;
        }

        public static UiButton AddButton(Transform parent, string label)
        {
            var button = InstantiatePrefab("Button", parent).GetComponent<UiButton>();
            button.name = Tidy(label) + "Button";
            SetLabel(button.gameObject, label);
            return button;
        }

        /// <summary>A row that lays buttons out side by side at equal widths.</summary>
        public static RectTransform AddButtonRow(Transform parent)
        {
            RectTransform row = NewRow("Buttons");
            row.SetParent(parent, false);
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = UiKitStyle.Spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            return row;
        }

        /// <summary>A slider row over 0..1 in 5% steps, shown as a percentage.</summary>
        public static UiSliderField AddSlider(Transform parent, string label, float value01)
        {
            var field = InstantiatePrefab("SliderField", parent).GetComponent<UiSliderField>();
            field.name = Tidy(label) + "Slider";
            SetLabel(field.gameObject, label);
            field.Slider.value = value01;
            SetText(field.transform.Find("Value"), $"{Mathf.RoundToInt(value01 * 100f)}%");
            return field;
        }

        public static UiToggleField AddToggle(Transform parent, string label, bool isOn)
        {
            var field = InstantiatePrefab("ToggleField", parent).GetComponent<UiToggleField>();
            field.name = Tidy(label) + "Toggle";
            SetLabel(field.gameObject, label);
            field.Toggle.isOn = isOn;
            return field;
        }

        public static UiStepper AddStepper(Transform parent, string label, string[] options, int index)
        {
            var stepper = InstantiatePrefab("Stepper", parent).GetComponent<UiStepper>();
            stepper.name = Tidy(label) + "Stepper";
            SetLabel(stepper.gameObject, label);
            SetStrings(stepper, "_options", options);
            Set(stepper, "_index", index);
            SetText(stepper.transform.Find("Choice/Well/Value"), options.Length > 0 ? options[Mathf.Clamp(index, 0, options.Length - 1)] : string.Empty);
            return stepper;
        }

        public static UiTabBar AddTabBar(Transform parent)
        {
            return InstantiatePrefab("TabBar", parent).GetComponent<UiTabBar>();
        }

        /// <summary>An empty page for a tab: a column that stacks rows like a panel does.</summary>
        public static RectTransform AddPage(Transform parent, string name)
        {
            RectTransform page = NewRect(name, parent);
            var layout = page.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = UiKitStyle.Spacing;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return page;
        }

        /// <summary>Adds a tab to a bar and links it to its page.</summary>
        public static UiButton AddTab(UiTabBar bar, string label, GameObject page)
        {
            var button = InstantiatePrefab("TabButton", bar.transform).GetComponent<UiButton>();
            button.name = Tidy(label) + "Tab";
            SetLabel(button.gameObject, label);

            var so = new SerializedObject(bar);
            SerializedProperty tabs = so.FindProperty("_tabs");
            int index = tabs.arraySize;
            tabs.arraySize = index + 1;
            SerializedProperty tab = tabs.GetArrayElementAtIndex(index);
            tab.FindPropertyRelative("Button").objectReferenceValue = button;
            tab.FindPropertyRelative("Page").objectReferenceValue = page;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Saved the way it will look on open: first tab's page showing, the rest hidden.
            page.SetActive(index == 0);
            button.GetComponent<Image>().sprite = Sprite(index == 0 ? "TabActive" : "TabInactive");
            return button;
        }

        public static void AddDivider(Transform parent)
        {
            RectTransform divider = NewRect("Divider", parent);
            AddImage(divider, "Divider", raycast: false);
            var element = divider.gameObject.AddComponent<LayoutElement>();
            element.minHeight = element.preferredHeight = 2f;
        }

        public static GameObject InstantiatePrefab(string name, Transform parent)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/{name}.prefab");
            if (prefab == null)
            {
                Debug.LogError($"[UiKit] Missing prefab '{name}'. Run Tools > Hold the Hill > Build UI Kit.");
                return null;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (parent != null) instance.transform.SetParent(parent, false);
            return instance;
        }

        // ---------- plumbing ----------

        private static Sprite Sprite(string name)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteDir}/{name}.png");
            if (sprite == null)
            {
                Debug.LogError($"[UiKit] Missing sprite '{name}'. Run UiKit/Sprites/Source~/build_ui_sprites.py.");
            }

            return sprite;
        }

        private static TMP_FontAsset Font()
        {
            if (s_font == null) s_font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            return s_font;
        }

        private static RectTransform NewRect(string name, Transform parent = null)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = LayerMask.NameToLayer("UI") };
            var rect = (RectTransform)go.transform;
            if (parent != null) rect.SetParent(parent, false);
            return rect;
        }

        // A full-width row of the standard height, for use inside a panel or page.
        private static RectTransform NewRow(string name)
        {
            RectTransform row = NewRect(name);
            row.sizeDelta = new Vector2(240f, UiKitStyle.RowHeight);
            AddRowLayout(row, minWidth: 0f, flexibleWidth: 1f);
            return row;
        }

        private static void AddRowLayout(RectTransform rect, float minWidth, float flexibleWidth)
        {
            var element = rect.gameObject.AddComponent<LayoutElement>();
            element.minHeight = element.preferredHeight = UiKitStyle.RowHeight;
            element.minWidth = minWidth;
            element.flexibleWidth = flexibleWidth;
        }

        private static void Stretch(RectTransform rect, float left = 0f, float bottom = 0f, float right = 0f, float top = 0f)
        {
            Anchor(rect, Vector2.zero, Vector2.one, new Vector2(left, bottom), new Vector2(-right, -top));
        }

        private static void Anchor(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static Image AddImage(RectTransform rect, string spriteName, bool raycast = true)
        {
            var image = rect.gameObject.AddComponent<Image>();
            Sprite sprite = Sprite(spriteName);
            image.sprite = sprite;
            image.type = sprite != null && sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            image.raycastTarget = raycast;
            return image;
        }

        // An invisible graphic: uGUI only delivers clicks to something that draws.
        private static Image AddHitArea(RectTransform rect)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.color = Color.clear;
            return image;
        }

        private static TextMeshProUGUI AddText(Transform parent, string name, string text, int size, TextAlignmentOptions alignment)
        {
            RectTransform rect = NewRect(name, parent);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = Font();
            label.fontSize = size;
            label.color = UiKitStyle.Cream;
            label.alignment = alignment;
            label.text = text;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            return label;
        }

        // Sits just outside the control and is ignored by layout groups.
        private static GameObject AddFocusRing(RectTransform parent)
        {
            RectTransform ring = NewRect("FocusRing", parent);
            Stretch(ring, -3f, -3f, -3f, -3f);
            AddImage(ring, "FocusRing", raycast: false);
            ring.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            ring.gameObject.SetActive(false);
            return ring.gameObject;
        }

        private static void SetLabel(GameObject widget, string text) => SetText(widget.transform.Find("Label"), text);

        private static void SetText(Transform target, string text)
        {
            if (target != null && target.TryGetComponent(out TMP_Text label)) label.text = text;
        }

        private static string Tidy(string label) => new string(label.Where(char.IsLetterOrDigit).ToArray());

        private static void Set(Object target, string property, object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(property);
            if (prop == null)
            {
                Debug.LogError($"[UiKit] {target.GetType().Name} has no serialized field '{property}'.");
                return;
            }

            switch (value)
            {
                case bool b: prop.boolValue = b; break;
                case int i: prop.intValue = i; break;
                case float f: prop.floatValue = f; break;
                case string s: prop.stringValue = s; break;
                case Object o: prop.objectReferenceValue = o; break;
                default: Debug.LogError($"[UiKit] Unsupported value for '{property}'."); return;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetStrings(Object target, string property, string[] values)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(property);
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                prop.GetArrayElementAtIndex(i).stringValue = values[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SavePrefab(GameObject instance)
        {
            PrefabUtility.SaveAsPrefabAsset(instance, $"{PrefabDir}/{instance.name}.prefab");
            Object.DestroyImmediate(instance);
        }

        private static void EnsureFolder(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder($"{parent}/{child}"))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }
    }
}
