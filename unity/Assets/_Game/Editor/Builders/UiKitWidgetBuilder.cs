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

namespace HoldTheHill.EditorTools.Builders
{
    /// <summary>
    /// One prefab per widget: button, panel, slider and toggle fields, stepper, tab bar and confirm dialog. Each is saved under UiKit/Prefabs by <see cref="Build"/>.
    /// </summary>
    internal static class UiKitWidgetBuilder
    {
        internal const float LabelSplit = 0.42f; // where a row's label ends and its control begins

        internal static GameObject MakeButton(string name, string normal, string hover, string pressed, string disabled, Vector2 size)
        {
            RectTransform root = UiKitPrimitives.NewRect(name);
            root.sizeDelta = size;

            Image image = UiKitPrimitives.AddImage(root, normal);
            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState
            {
                highlightedSprite = UiKitPrimitives.Sprite(hover),
                selectedSprite = UiKitPrimitives.Sprite(hover),
                pressedSprite = UiKitPrimitives.Sprite(pressed),
                disabledSprite = UiKitPrimitives.Sprite(disabled),
            };

            UiKitPrimitives.AddRowLayout(root, minWidth: 48f, flexibleWidth: 1f);

            TextMeshProUGUI label = UiKitPrimitives.AddText(root, "Label", name == "TabButton" ? "Tab" : "Button", UiKitStyle.BodySize, TextAlignmentOptions.Center);
            UiKitPrimitives.Stretch(label.rectTransform, 6f, 0f, 6f, 0f);

            GameObject ring = UiKitPrimitives.AddFocusRing(root);
            UiKitPrimitives.Set(root.gameObject.AddComponent<UiFocusable>(), "_focusRing", ring);
            UiKitPrimitives.Set(root.gameObject.AddComponent<UiButton>(), "_label", label);
            return root.gameObject;
        }

        internal static GameObject MakePanel()
        {
            RectTransform root = UiKitPrimitives.NewRect("Panel");
            root.sizeDelta = new Vector2(240f, 120f);
            UiKitPrimitives.AddImage(root, "Panel");

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

        internal static GameObject MakeSliderField()
        {
            RectTransform root = UiKitPrimitives.NewRow("SliderField");

            TextMeshProUGUI label = UiKitPrimitives.AddText(root, "Label", "Slider", UiKitStyle.BodySize, TextAlignmentOptions.Left);
            UiKitPrimitives.Anchor(label.rectTransform, new Vector2(0f, 0f), new Vector2(LabelSplit, 1f), new Vector2(2f, 0f), Vector2.zero);

            RectTransform sliderRect = UiKitPrimitives.NewRect("Slider", root);
            UiKitPrimitives.Anchor(sliderRect, new Vector2(LabelSplit, 0f), new Vector2(0.84f, 1f), Vector2.zero, Vector2.zero);

            RectTransform track = UiKitPrimitives.NewRect("Track", sliderRect);
            UiKitPrimitives.Anchor(track, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, -5f), new Vector2(0f, 5f));
            UiKitPrimitives.AddImage(track, "Well");

            RectTransform fillArea = UiKitPrimitives.NewRect("Fill Area", sliderRect);
            UiKitPrimitives.Anchor(fillArea, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(2f, -3f), new Vector2(-2f, 3f));
            RectTransform fill = UiKitPrimitives.NewRect("Fill", fillArea);
            fill.sizeDelta = Vector2.zero;
            UiKitPrimitives.AddImage(fill, "SliderFill", raycast: false);

            RectTransform handleArea = UiKitPrimitives.NewRect("Handle Slide Area", sliderRect);
            UiKitPrimitives.Anchor(handleArea, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(4f, -7f), new Vector2(-4f, 7f));
            RectTransform handle = UiKitPrimitives.NewRect("Handle", handleArea);
            handle.sizeDelta = new Vector2(8f, 0f);
            Image handleImage = UiKitPrimitives.AddImage(handle, "SliderHandle");

            var slider = sliderRect.gameObject.AddComponent<Slider>();
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;
            slider.transition = Selectable.Transition.SpriteSwap;
            slider.spriteState = new SpriteState
            {
                highlightedSprite = UiKitPrimitives.Sprite("SliderHandleHover"),
                selectedSprite = UiKitPrimitives.Sprite("SliderHandleHover"),
                pressedSprite = UiKitPrimitives.Sprite("SliderHandleHover"),
            };
            slider.value = 0.8f;

            TextMeshProUGUI value = UiKitPrimitives.AddText(root, "Value", "80%", UiKitStyle.BodySize, TextAlignmentOptions.Right);
            UiKitPrimitives.Anchor(value.rectTransform, new Vector2(0.84f, 0f), new Vector2(1f, 1f), Vector2.zero, new Vector2(-2f, 0f));

            GameObject ring = UiKitPrimitives.AddFocusRing(root);
            var focusable = sliderRect.gameObject.AddComponent<UiFocusable>();
            UiKitPrimitives.Set(focusable, "_focusRing", ring);
            UiKitPrimitives.Set(focusable, "_pressCue", false);

            var field = root.gameObject.AddComponent<UiSliderField>();
            UiKitPrimitives.Set(field, "_slider", slider);
            UiKitPrimitives.Set(field, "_label", label);
            UiKitPrimitives.Set(field, "_valueText", value);
            return root.gameObject;
        }

        internal static GameObject MakeToggleField()
        {
            RectTransform root = UiKitPrimitives.NewRow("ToggleField");
            UiKitPrimitives.AddHitArea(root); // so a click anywhere on the row flips it

            TextMeshProUGUI label = UiKitPrimitives.AddText(root, "Label", "Toggle", UiKitStyle.BodySize, TextAlignmentOptions.Left);
            UiKitPrimitives.Stretch(label.rectTransform, 2f, 0f, 20f, 0f);

            RectTransform box = UiKitPrimitives.NewRect("Box", root);
            box.anchorMin = box.anchorMax = new Vector2(1f, 0.5f);
            box.pivot = new Vector2(1f, 0.5f);
            box.anchoredPosition = new Vector2(-2f, 0f);
            box.sizeDelta = new Vector2(14f, 14f);
            Image boxImage = UiKitPrimitives.AddImage(box, "ToggleBox", raycast: false);

            RectTransform check = UiKitPrimitives.NewRect("Check", box);
            UiKitPrimitives.Stretch(check);
            Image checkImage = UiKitPrimitives.AddImage(check, "ToggleCheck", raycast: false);

            var toggle = root.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = boxImage;
            toggle.graphic = checkImage;
            toggle.transition = Selectable.Transition.SpriteSwap;
            toggle.spriteState = new SpriteState
            {
                highlightedSprite = UiKitPrimitives.Sprite("ToggleBoxHover"),
                selectedSprite = UiKitPrimitives.Sprite("ToggleBoxHover"),
                pressedSprite = UiKitPrimitives.Sprite("ToggleBoxHover"),
            };
            toggle.isOn = true;

            GameObject ring = UiKitPrimitives.AddFocusRing(root);
            var focusable = root.gameObject.AddComponent<UiFocusable>();
            UiKitPrimitives.Set(focusable, "_focusRing", ring);
            UiKitPrimitives.Set(focusable, "_pressCue", false);

            var field = root.gameObject.AddComponent<UiToggleField>();
            UiKitPrimitives.Set(field, "_toggle", toggle);
            UiKitPrimitives.Set(field, "_label", label);
            return root.gameObject;
        }

        internal static GameObject MakeStepper()
        {
            RectTransform root = UiKitPrimitives.NewRow("Stepper");
            Image hit = UiKitPrimitives.AddHitArea(root);

            TextMeshProUGUI label = UiKitPrimitives.AddText(root, "Label", "Stepper", UiKitStyle.BodySize, TextAlignmentOptions.Left);
            UiKitPrimitives.Anchor(label.rectTransform, new Vector2(0f, 0f), new Vector2(LabelSplit, 1f), new Vector2(2f, 0f), Vector2.zero);

            RectTransform group = UiKitPrimitives.NewRect("Choice", root);
            UiKitPrimitives.Anchor(group, new Vector2(LabelSplit, 0f), new Vector2(1f, 1f), Vector2.zero, new Vector2(-2f, 0f));

            Button previous = MakeArrowButton("Previous", group, left: true);
            Button next = MakeArrowButton("Next", group, left: false);

            RectTransform well = UiKitPrimitives.NewRect("Well", group);
            UiKitPrimitives.Stretch(well, 16f, 3f, 16f, 3f);
            UiKitPrimitives.AddImage(well, "Well", raycast: false);
            TextMeshProUGUI value = UiKitPrimitives.AddText(well, "Value", "Option A", UiKitStyle.BodySize, TextAlignmentOptions.Center);
            UiKitPrimitives.Stretch(value.rectTransform, 2f, 0f, 2f, 0f);

            var stepper = root.gameObject.AddComponent<UiStepper>();
            stepper.targetGraphic = hit;
            stepper.transition = Selectable.Transition.None; // the focus ring shows focus
            UiKitPrimitives.Set(stepper, "_label", label);
            UiKitPrimitives.Set(stepper, "_valueText", value);
            UiKitPrimitives.Set(stepper, "_previous", previous);
            UiKitPrimitives.Set(stepper, "_next", next);
            UiKitPrimitives.SetStrings(stepper, "_options", new[] { "Option A", "Option B", "Option C" });

            GameObject ring = UiKitPrimitives.AddFocusRing(root);
            var focusable = root.gameObject.AddComponent<UiFocusable>();
            UiKitPrimitives.Set(focusable, "_focusRing", ring);
            UiKitPrimitives.Set(focusable, "_pressCue", false);
            return root.gameObject;
        }

        // Mouse-only: a keyboard or gamepad changes the stepper with left/right instead.
        internal static Button MakeArrowButton(string name, RectTransform parent, bool left)
        {
            RectTransform rect = UiKitPrimitives.NewRect(name, parent);
            rect.anchorMin = new Vector2(left ? 0f : 1f, 0f);
            rect.anchorMax = new Vector2(left ? 0f : 1f, 1f);
            rect.pivot = new Vector2(left ? 0f : 1f, 0.5f);
            rect.sizeDelta = new Vector2(14f, 0f);
            UiKitPrimitives.AddHitArea(rect);

            RectTransform arrow = UiKitPrimitives.NewRect("Arrow", rect);
            arrow.sizeDelta = new Vector2(6f, 9f);
            arrow.localScale = new Vector3(left ? -1f : 1f, 1f, 1f);
            Image arrowImage = UiKitPrimitives.AddImage(arrow, "Arrow", raycast: false);
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

        internal static GameObject MakeTabBar()
        {
            RectTransform root = UiKitPrimitives.NewRow("TabBar");
            var layout = root.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 2f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            var bar = root.gameObject.AddComponent<UiTabBar>();
            UiKitPrimitives.Set(bar, "_activeSprite", UiKitPrimitives.Sprite("TabActive"));
            UiKitPrimitives.Set(bar, "_inactiveSprite", UiKitPrimitives.Sprite("TabInactive"));
            return root.gameObject;
        }

        internal static GameObject MakeConfirmDialog()
        {
            RectTransform root = UiKitPrimitives.NewRect("ConfirmDialog");
            UiKitPrimitives.Stretch(root);
            root.gameObject.AddComponent<CanvasGroup>();
            var screen = root.gameObject.AddComponent<UiScreen>();

            // Dims the screen underneath and swallows clicks aimed at it.
            RectTransform dim = UiKitPrimitives.NewRect("Dim", root);
            UiKitPrimitives.Stretch(dim);
            dim.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

            RectTransform panel = UiKitBuilder.AddPanel(root, 260f);
            TextMeshProUGUI title = UiKitBuilder.AddTitle(panel, "Are you sure?");
            TextMeshProUGUI message = UiKitBuilder.AddBody(panel, "This cannot be undone.");
            RectTransform buttons = UiKitBuilder.AddButtonRow(panel);
            UiButton cancel = UiKitBuilder.AddButton(buttons, "Cancel");
            UiButton confirm = UiKitBuilder.AddButton(buttons, "Confirm");

            var dialog = root.gameObject.AddComponent<UiConfirmDialog>();
            UiKitPrimitives.Set(dialog, "_title", title);
            UiKitPrimitives.Set(dialog, "_message", message);
            UiKitPrimitives.Set(dialog, "_confirm", confirm);
            UiKitPrimitives.Set(dialog, "_cancel", cancel);
            UiKitPrimitives.Set(screen, "_firstSelected", cancel.Button);

            root.gameObject.SetActive(false); // closed until Open() is called
            return root.gameObject;
        }
    }
}
