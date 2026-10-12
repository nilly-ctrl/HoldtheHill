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
    /// One prefab per widget: button, panel, slider and toggle fields, stepper, tab bar and confirm dialog. Each is saved under UiKit/Prefabs by <see cref="Build"/>.
    /// </summary>
    internal static class UiKitWidgetBuilder
    {
        internal const float LabelSplit = 0.42f; // where a row's label ends and its control begins

        internal static GameObject MakeButton(string name, string normal, string hover, string pressed, string disabled, Vector2 size)
        {
            RectTransform root = UiKitBuilder.NewRect(name);
            root.sizeDelta = size;

            Image image = UiKitBuilder.AddImage(root, normal);
            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState
            {
                highlightedSprite = UiKitBuilder.Sprite(hover),
                selectedSprite = UiKitBuilder.Sprite(hover),
                pressedSprite = UiKitBuilder.Sprite(pressed),
                disabledSprite = UiKitBuilder.Sprite(disabled),
            };

            UiKitBuilder.AddRowLayout(root, minWidth: 48f, flexibleWidth: 1f);

            TextMeshProUGUI label = UiKitBuilder.AddText(root, "Label", name == "TabButton" ? "Tab" : "Button", UiKitStyle.BodySize, TextAlignmentOptions.Center);
            UiKitBuilder.Stretch(label.rectTransform, 6f, 0f, 6f, 0f);

            GameObject ring = UiKitBuilder.AddFocusRing(root);
            UiKitBuilder.Set(root.gameObject.AddComponent<UiFocusable>(), "_focusRing", ring);
            UiKitBuilder.Set(root.gameObject.AddComponent<UiButton>(), "_label", label);
            return root.gameObject;
        }

        internal static GameObject MakePanel()
        {
            RectTransform root = UiKitBuilder.NewRect("Panel");
            root.sizeDelta = new Vector2(240f, 120f);
            UiKitBuilder.AddImage(root, "Panel");

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
            RectTransform root = UiKitBuilder.NewRow("SliderField");

            TextMeshProUGUI label = UiKitBuilder.AddText(root, "Label", "Slider", UiKitStyle.BodySize, TextAlignmentOptions.Left);
            UiKitBuilder.Anchor(label.rectTransform, new Vector2(0f, 0f), new Vector2(LabelSplit, 1f), new Vector2(2f, 0f), Vector2.zero);

            RectTransform sliderRect = UiKitBuilder.NewRect("Slider", root);
            UiKitBuilder.Anchor(sliderRect, new Vector2(LabelSplit, 0f), new Vector2(0.84f, 1f), Vector2.zero, Vector2.zero);

            RectTransform track = UiKitBuilder.NewRect("Track", sliderRect);
            UiKitBuilder.Anchor(track, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, -5f), new Vector2(0f, 5f));
            UiKitBuilder.AddImage(track, "Well");

            RectTransform fillArea = UiKitBuilder.NewRect("Fill Area", sliderRect);
            UiKitBuilder.Anchor(fillArea, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(2f, -3f), new Vector2(-2f, 3f));
            RectTransform fill = UiKitBuilder.NewRect("Fill", fillArea);
            fill.sizeDelta = Vector2.zero;
            UiKitBuilder.AddImage(fill, "SliderFill", raycast: false);

            RectTransform handleArea = UiKitBuilder.NewRect("Handle Slide Area", sliderRect);
            UiKitBuilder.Anchor(handleArea, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(4f, -7f), new Vector2(-4f, 7f));
            RectTransform handle = UiKitBuilder.NewRect("Handle", handleArea);
            handle.sizeDelta = new Vector2(8f, 0f);
            Image handleImage = UiKitBuilder.AddImage(handle, "SliderHandle");

            var slider = sliderRect.gameObject.AddComponent<Slider>();
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;
            slider.transition = Selectable.Transition.SpriteSwap;
            slider.spriteState = new SpriteState
            {
                highlightedSprite = UiKitBuilder.Sprite("SliderHandleHover"),
                selectedSprite = UiKitBuilder.Sprite("SliderHandleHover"),
                pressedSprite = UiKitBuilder.Sprite("SliderHandleHover"),
            };
            slider.value = 0.8f;

            TextMeshProUGUI value = UiKitBuilder.AddText(root, "Value", "80%", UiKitStyle.BodySize, TextAlignmentOptions.Right);
            UiKitBuilder.Anchor(value.rectTransform, new Vector2(0.84f, 0f), new Vector2(1f, 1f), Vector2.zero, new Vector2(-2f, 0f));

            GameObject ring = UiKitBuilder.AddFocusRing(root);
            var focusable = sliderRect.gameObject.AddComponent<UiFocusable>();
            UiKitBuilder.Set(focusable, "_focusRing", ring);
            UiKitBuilder.Set(focusable, "_pressCue", false);

            var field = root.gameObject.AddComponent<UiSliderField>();
            UiKitBuilder.Set(field, "_slider", slider);
            UiKitBuilder.Set(field, "_label", label);
            UiKitBuilder.Set(field, "_valueText", value);
            return root.gameObject;
        }

        internal static GameObject MakeToggleField()
        {
            RectTransform root = UiKitBuilder.NewRow("ToggleField");
            UiKitBuilder.AddHitArea(root); // so a click anywhere on the row flips it

            TextMeshProUGUI label = UiKitBuilder.AddText(root, "Label", "Toggle", UiKitStyle.BodySize, TextAlignmentOptions.Left);
            UiKitBuilder.Stretch(label.rectTransform, 2f, 0f, 20f, 0f);

            RectTransform box = UiKitBuilder.NewRect("Box", root);
            box.anchorMin = box.anchorMax = new Vector2(1f, 0.5f);
            box.pivot = new Vector2(1f, 0.5f);
            box.anchoredPosition = new Vector2(-2f, 0f);
            box.sizeDelta = new Vector2(14f, 14f);
            Image boxImage = UiKitBuilder.AddImage(box, "ToggleBox", raycast: false);

            RectTransform check = UiKitBuilder.NewRect("Check", box);
            UiKitBuilder.Stretch(check);
            Image checkImage = UiKitBuilder.AddImage(check, "ToggleCheck", raycast: false);

            var toggle = root.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = boxImage;
            toggle.graphic = checkImage;
            toggle.transition = Selectable.Transition.SpriteSwap;
            toggle.spriteState = new SpriteState
            {
                highlightedSprite = UiKitBuilder.Sprite("ToggleBoxHover"),
                selectedSprite = UiKitBuilder.Sprite("ToggleBoxHover"),
                pressedSprite = UiKitBuilder.Sprite("ToggleBoxHover"),
            };
            toggle.isOn = true;

            GameObject ring = UiKitBuilder.AddFocusRing(root);
            var focusable = root.gameObject.AddComponent<UiFocusable>();
            UiKitBuilder.Set(focusable, "_focusRing", ring);
            UiKitBuilder.Set(focusable, "_pressCue", false);

            var field = root.gameObject.AddComponent<UiToggleField>();
            UiKitBuilder.Set(field, "_toggle", toggle);
            UiKitBuilder.Set(field, "_label", label);
            return root.gameObject;
        }

        internal static GameObject MakeStepper()
        {
            RectTransform root = UiKitBuilder.NewRow("Stepper");
            Image hit = UiKitBuilder.AddHitArea(root);

            TextMeshProUGUI label = UiKitBuilder.AddText(root, "Label", "Stepper", UiKitStyle.BodySize, TextAlignmentOptions.Left);
            UiKitBuilder.Anchor(label.rectTransform, new Vector2(0f, 0f), new Vector2(LabelSplit, 1f), new Vector2(2f, 0f), Vector2.zero);

            RectTransform group = UiKitBuilder.NewRect("Choice", root);
            UiKitBuilder.Anchor(group, new Vector2(LabelSplit, 0f), new Vector2(1f, 1f), Vector2.zero, new Vector2(-2f, 0f));

            Button previous = MakeArrowButton("Previous", group, left: true);
            Button next = MakeArrowButton("Next", group, left: false);

            RectTransform well = UiKitBuilder.NewRect("Well", group);
            UiKitBuilder.Stretch(well, 16f, 3f, 16f, 3f);
            UiKitBuilder.AddImage(well, "Well", raycast: false);
            TextMeshProUGUI value = UiKitBuilder.AddText(well, "Value", "Option A", UiKitStyle.BodySize, TextAlignmentOptions.Center);
            UiKitBuilder.Stretch(value.rectTransform, 2f, 0f, 2f, 0f);

            var stepper = root.gameObject.AddComponent<UiStepper>();
            stepper.targetGraphic = hit;
            stepper.transition = Selectable.Transition.None; // the focus ring shows focus
            UiKitBuilder.Set(stepper, "_label", label);
            UiKitBuilder.Set(stepper, "_valueText", value);
            UiKitBuilder.Set(stepper, "_previous", previous);
            UiKitBuilder.Set(stepper, "_next", next);
            UiKitBuilder.SetStrings(stepper, "_options", new[] { "Option A", "Option B", "Option C" });

            GameObject ring = UiKitBuilder.AddFocusRing(root);
            var focusable = root.gameObject.AddComponent<UiFocusable>();
            UiKitBuilder.Set(focusable, "_focusRing", ring);
            UiKitBuilder.Set(focusable, "_pressCue", false);
            return root.gameObject;
        }

        // Mouse-only: a keyboard or gamepad changes the stepper with left/right instead.
        internal static Button MakeArrowButton(string name, RectTransform parent, bool left)
        {
            RectTransform rect = UiKitBuilder.NewRect(name, parent);
            rect.anchorMin = new Vector2(left ? 0f : 1f, 0f);
            rect.anchorMax = new Vector2(left ? 0f : 1f, 1f);
            rect.pivot = new Vector2(left ? 0f : 1f, 0.5f);
            rect.sizeDelta = new Vector2(14f, 0f);
            UiKitBuilder.AddHitArea(rect);

            RectTransform arrow = UiKitBuilder.NewRect("Arrow", rect);
            arrow.sizeDelta = new Vector2(6f, 9f);
            arrow.localScale = new Vector3(left ? -1f : 1f, 1f, 1f);
            Image arrowImage = UiKitBuilder.AddImage(arrow, "Arrow", raycast: false);
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
            RectTransform root = UiKitBuilder.NewRow("TabBar");
            var layout = root.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 2f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            var bar = root.gameObject.AddComponent<UiTabBar>();
            UiKitBuilder.Set(bar, "_activeSprite", UiKitBuilder.Sprite("TabActive"));
            UiKitBuilder.Set(bar, "_inactiveSprite", UiKitBuilder.Sprite("TabInactive"));
            return root.gameObject;
        }

        internal static GameObject MakeConfirmDialog()
        {
            RectTransform root = UiKitBuilder.NewRect("ConfirmDialog");
            UiKitBuilder.Stretch(root);
            root.gameObject.AddComponent<CanvasGroup>();
            var screen = root.gameObject.AddComponent<UiScreen>();

            // Dims the screen underneath and swallows clicks aimed at it.
            RectTransform dim = UiKitBuilder.NewRect("Dim", root);
            UiKitBuilder.Stretch(dim);
            dim.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

            RectTransform panel = UiKitBuilder.AddPanel(root, 260f);
            TextMeshProUGUI title = UiKitBuilder.AddTitle(panel, "Are you sure?");
            TextMeshProUGUI message = UiKitBuilder.AddBody(panel, "This cannot be undone.");
            RectTransform buttons = UiKitBuilder.AddButtonRow(panel);
            UiButton cancel = UiKitBuilder.AddButton(buttons, "Cancel");
            UiButton confirm = UiKitBuilder.AddButton(buttons, "Confirm");

            var dialog = root.gameObject.AddComponent<UiConfirmDialog>();
            UiKitBuilder.Set(dialog, "_title", title);
            UiKitBuilder.Set(dialog, "_message", message);
            UiKitBuilder.Set(dialog, "_confirm", confirm);
            UiKitBuilder.Set(dialog, "_cancel", cancel);
            UiKitBuilder.Set(screen, "_firstSelected", cancel.Button);

            root.gameObject.SetActive(false); // closed until Open() is called
            return root.gameObject;
        }
    }
}
