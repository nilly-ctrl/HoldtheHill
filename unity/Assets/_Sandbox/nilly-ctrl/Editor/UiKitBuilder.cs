using System.Collections.Generic;
using System.Linq;
using HoldTheHill.Core.UiKit;
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
    /// scene that shows them all. Menu: Tools > Hold the Hill > UiKitPrefabBuilder.Build UI Kit.
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
        public const string Root = "Assets/_Game/Core/UiKit";
        public const string PrefabDir = Root + "/Prefabs";
        public const string FontPath = Root + "/Fonts/HoldTheHillPixel.asset";
        public const string GalleryPath = Root + "/UiKitGallery.unity";

        // ---------- font ----------

        // ---------- widgets ----------

        // ---------- gallery ----------

        // ---------- for screen builders ----------

        /// <summary>
        /// A full-screen <see cref="UiScreen"/> under a canvas, saved closed. The dim layer darkens
        /// whatever is behind it and swallows clicks aimed there.
        /// </summary>
        public static RectTransform AddScreen(Transform canvas, string name, bool dim = true)
        {
            RectTransform screen = UiKitPrimitives.NewRect(name, canvas);
            UiKitPrimitives.Stretch(screen);
            screen.gameObject.AddComponent<CanvasGroup>();
            screen.gameObject.AddComponent<UiScreen>();

            if (dim)
            {
                RectTransform layer = UiKitPrimitives.NewRect("Dim", screen);
                UiKitPrimitives.Stretch(layer);
                layer.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);
            }

            screen.gameObject.SetActive(false);
            return screen;
        }

        /// <summary>A "label ........ value" line for stats. Returns the value text.</summary>
        public static TextMeshProUGUI AddValueRow(Transform parent, string label, string value)
        {
            RectTransform row = UiKitPrimitives.NewRect(UiKitPrimitives.Tidy(label) + "Row", parent);
            var element = row.gameObject.AddComponent<LayoutElement>();
            element.minHeight = element.preferredHeight = 12f;

            TextMeshProUGUI left = UiKitPrimitives.AddText(row, "Label", label, UiKitStyle.BodySize, TextAlignmentOptions.Left);
            UiKitPrimitives.Stretch(left.rectTransform, 2f, 0f, 2f, 0f);
            left.color = UiKitStyle.Dim;

            TextMeshProUGUI right = UiKitPrimitives.AddText(row, "Value", value, UiKitStyle.BodySize, TextAlignmentOptions.Right);
            UiKitPrimitives.Stretch(right.rectTransform, 2f, 0f, 2f, 0f);
            return right;
        }

        /// <summary>A row with a label on the left and a kit button on the right, e.g. a key binding.</summary>
        public static UiButton AddButtonRow(Transform parent, string label, string buttonText, out TextMeshProUGUI labelText)
        {
            RectTransform row = UiKitPrimitives.NewRow(UiKitPrimitives.Tidy(label) + "Row");
            row.SetParent(parent, false);

            labelText = UiKitPrimitives.AddText(row, "Label", label, UiKitStyle.BodySize, TextAlignmentOptions.Left);
            UiKitPrimitives.Anchor(labelText.rectTransform, new Vector2(0f, 0f), new Vector2(0.6f, 1f), new Vector2(2f, 0f), Vector2.zero);

            var button = InstantiatePrefab("Button", row).GetComponent<UiButton>();
            button.name = "Button";
            UiKitPrimitives.SetLabel(button.gameObject, buttonText);
            UiKitPrimitives.Anchor((RectTransform)button.transform, new Vector2(0.6f, 0f), new Vector2(1f, 1f), Vector2.zero, new Vector2(-2f, 0f));
            return button;
        }

        /// <summary>A bare column that stacks rows, with no panel art. Position it with its anchors.</summary>
        public static RectTransform AddColumn(Transform parent, string name, float width)
        {
            RectTransform column = AddPage(parent, name);
            column.sizeDelta = new Vector2(width, 0f);
            var fitter = column.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return column;
        }

        /// <summary>A picture drawn whole at the given size, in canvas units. Does not take clicks.</summary>
        public static Image AddPicture(Transform parent, string name, Sprite sprite, Vector2 size)
        {
            RectTransform rect = UiKitPrimitives.NewRect(name, parent);
            rect.sizeDelta = size;
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>Sets a serialized field on a component by name, for wiring generated objects.</summary>
        public static void Wire(Object target, string property, Object value) => UiKitPrimitives.Set(target, property, value);

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
            TextMeshProUGUI title = UiKitPrimitives.AddText(parent, "Title", text, UiKitStyle.TitleSize, TextAlignmentOptions.Center);
            title.color = UiKitStyle.Gold;
            return title;
        }

        /// <summary>Body text that wraps to the panel's width.</summary>
        public static TextMeshProUGUI AddBody(Transform parent, string text)
        {
            TextMeshProUGUI body = UiKitPrimitives.AddText(parent, "Text", text, UiKitStyle.BodySize, TextAlignmentOptions.Center);
            body.textWrappingMode = TextWrappingModes.Normal;
            return body;
        }

        public static UiButton AddButton(Transform parent, string label)
        {
            var button = InstantiatePrefab("Button", parent).GetComponent<UiButton>();
            button.name = UiKitPrimitives.Tidy(label) + "Button";
            UiKitPrimitives.SetLabel(button.gameObject, label);
            return button;
        }

        /// <summary>A row that lays buttons out side by side at equal widths.</summary>
        public static RectTransform AddButtonRow(Transform parent)
        {
            RectTransform row = UiKitPrimitives.NewRow("Buttons");
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
            field.name = UiKitPrimitives.Tidy(label) + "Slider";
            UiKitPrimitives.SetLabel(field.gameObject, label);
            field.Slider.value = value01;
            UiKitPrimitives.SetText(field.transform.Find("Value"), $"{Mathf.RoundToInt(value01 * 100f)}%");
            return field;
        }

        public static UiToggleField AddToggle(Transform parent, string label, bool isOn)
        {
            var field = InstantiatePrefab("ToggleField", parent).GetComponent<UiToggleField>();
            field.name = UiKitPrimitives.Tidy(label) + "Toggle";
            UiKitPrimitives.SetLabel(field.gameObject, label);
            field.Toggle.isOn = isOn;
            return field;
        }

        public static UiStepper AddStepper(Transform parent, string label, string[] options, int index)
        {
            var stepper = InstantiatePrefab("Stepper", parent).GetComponent<UiStepper>();
            stepper.name = UiKitPrimitives.Tidy(label) + "Stepper";
            UiKitPrimitives.SetLabel(stepper.gameObject, label);
            UiKitPrimitives.SetStrings(stepper, "_options", options);
            UiKitPrimitives.Set(stepper, "_index", index);
            UiKitPrimitives.SetText(stepper.transform.Find("Choice/Well/Value"), options.Length > 0 ? options[Mathf.Clamp(index, 0, options.Length - 1)] : string.Empty);
            return stepper;
        }

        public static UiTabBar AddTabBar(Transform parent)
        {
            return InstantiatePrefab("TabBar", parent).GetComponent<UiTabBar>();
        }

        /// <summary>An empty page for a tab: a column that stacks rows like a panel does.</summary>
        public static RectTransform AddPage(Transform parent, string name)
        {
            RectTransform page = UiKitPrimitives.NewRect(name, parent);
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
            button.name = UiKitPrimitives.Tidy(label) + "Tab";
            UiKitPrimitives.SetLabel(button.gameObject, label);

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
            button.GetComponent<Image>().sprite = UiKitPrimitives.Sprite(index == 0 ? "TabActive" : "TabInactive");
            return button;
        }

        public static void AddDivider(Transform parent)
        {
            RectTransform divider = UiKitPrimitives.NewRect("Divider", parent);
            UiKitPrimitives.AddImage(divider, "Divider", raycast: false);
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

    }
}
